using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace backend.ServiceDefaults;

public static class KeycloakJwtExtensions
{
    /// <summary>
    /// Adds JWT Bearer authentication using a manually refreshed JWKS store,
    /// bypassing the built-in ConfigurationManager which can cache empty keys.
    /// </summary>
    public static AuthenticationBuilder AddKeycloakJwtAuthentication(
        this IServiceCollection services,
        string authority,
        string? metadataAddress = null)
    {
        var normalizedAuthority = authority.TrimEnd('/');
        var actualMetadataAddress = string.IsNullOrWhiteSpace(metadataAddress)
            ? $"{normalizedAuthority}/.well-known/openid-configuration"
            : metadataAddress;

        var jwkStore = new JwkStore();
        var jwkRefresher = new KeycloakJwkRefresher(jwkStore, actualMetadataAddress);

        // Retry startup fetch with short backoff so a temporarily unavailable Keycloak
        // doesn't leave the store empty for 15 minutes.
        var startupRetries = 0;
        const int maxStartupRetries = 5;
        while (startupRetries < maxStartupRetries)
        {
            try
            {
                jwkRefresher.RefreshAsync(CancellationToken.None).GetAwaiter().GetResult();
                break;
            }
            catch (Exception ex)
            {
                startupRetries++;
                if (startupRetries >= maxStartupRetries)
                {
                    Console.WriteLine($"Warning: failed to fetch Keycloak JWKS at startup after {maxStartupRetries} attempts: {ex.Message}");
                    break;
                }
                Thread.Sleep(TimeSpan.FromSeconds(Math.Pow(2, startupRetries)));
            }
        }

        services.AddSingleton(jwkStore);
        services.AddHostedService(_ => jwkRefresher);

        return services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.RequireHttpsMetadata = false;
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = true,
                    ValidIssuer = normalizedAuthority,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2),
                    NameClaimType = "sub",
                    RoleClaimType = ClaimTypes.Role,
                    IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) => jwkStore.GetKeys()
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        var logger = ctx.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("JwtBearer");
                        logger.LogWarning(ctx.Exception, "JWT authentication failed for {Path}", ctx.Request.Path);
                        return Task.CompletedTask;
                    }
                };
            });
    }
}

public sealed class JwkStore
{
    private readonly ReaderWriterLockSlim _lock = new();
    private List<SecurityKey> _keys = new();

    public IReadOnlyList<SecurityKey> GetKeys()
    {
        _lock.EnterReadLock();
        try { return _keys.ToList(); }
        finally { _lock.ExitReadLock(); }
    }

    public void UpdateKeys(IEnumerable<SecurityKey> keys)
    {
        _lock.EnterWriteLock();
        try { _keys = keys.ToList(); }
        finally { _lock.ExitWriteLock(); }
    }
}

public sealed class KeycloakJwkRefresher : BackgroundService
{
    private readonly JwkStore _store;
    private readonly string _metadataAddress;
    private readonly HttpClient _httpClient;
    private readonly ILogger<KeycloakJwkRefresher> _logger;
    private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(15);

    public KeycloakJwkRefresher(JwkStore store, string metadataAddress, ILogger<KeycloakJwkRefresher>? logger = null)
    {
        _store = store;
        _metadataAddress = metadataAddress;
        _logger = logger ?? NullLogger<KeycloakJwkRefresher>.Instance;
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        _httpClient = new HttpClient(handler);
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var configJson = await _httpClient.GetStringAsync(_metadataAddress, cancellationToken);
        using var doc = JsonDocument.Parse(configJson);
        if (!doc.RootElement.TryGetProperty("jwks_uri", out var jwksUriProp))
        {
            _logger.LogWarning("OpenID configuration does not contain jwks_uri.");
            return;
        }

        var jwksUri = jwksUriProp.GetString();
        if (string.IsNullOrWhiteSpace(jwksUri))
        {
            _logger.LogWarning("jwks_uri is empty in OpenID configuration.");
            return;
        }

        var jwksJson = await _httpClient.GetStringAsync(jwksUri, cancellationToken);
        var jwks = new JsonWebKeySet(jwksJson);
        _store.UpdateKeys(jwks.GetSigningKeys());
        _logger.LogInformation("Refreshed Keycloak JWKS. KeyCount={KeyCount}", _store.GetKeys().Count);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh Keycloak JWKS.");
            }

            try
            {
                await Task.Delay(_refreshInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
