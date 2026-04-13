using System.Security.Claims;
using System.Text;
using System.Text.Json;
using backend.Domain.Data;
using backend.Infrastructure.Application.Security;
using backend.Infrastructure.Application.Users;
using backend.Infrastructure.Infrastructure.Messaging;
using backend.Orders.Infrastructure.Orders;
using backend.Orders.Validation.Orders;
using backend.ServiceDefaults;
using backend.Shared.Application.Messaging;
using backend.Shared.Application.Users;
using backend.Shared.Configuration;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

builder.Services.AddScoped<CreateDigitalOrderCommandValidator>();
builder.Services.AddScoped<CreatePhysicalOrderCommandValidator>();
builder.Services.AddScoped<CreateOrderCommandValidator>();
builder.Services.AddScoped<UpdateOrderCommandValidator>();

var keycloakAuthority = builder.Configuration["Keycloak:Authority"];
if (string.IsNullOrWhiteSpace(keycloakAuthority))
{
    throw new InvalidOperationException("Keycloak:Authority is missing. Configure it in appsettings.json.");
}

var normalizedAuthority = keycloakAuthority.TrimEnd('/');
var metadataAddress = builder.Configuration["Keycloak:MetadataAddress"];
if (string.IsNullOrWhiteSpace(metadataAddress))
{
    metadataAddress = $"{normalizedAuthority}/.well-known/openid-configuration";
}

var jwkStore = new JwkStore();
var jwkRefresher = new KeycloakJwkRefresher(
    jwkStore,
    metadataAddress,
    Microsoft.Extensions.Logging.Abstractions.NullLogger<KeycloakJwkRefresher>.Instance);
try
{
    await jwkRefresher.RefreshAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: failed to fetch Keycloak JWKS at startup: {ex.Message}");
}

builder.Services.AddSingleton(jwkStore);
builder.Services.AddHostedService(_ => jwkRefresher);

// Add authentication with Keycloak (but allow anonymous by default)
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
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
                var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtBearer");
                logger.LogWarning(ctx.Exception, "JWT authentication failed for {Path}", ctx.Request.Path);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = null;
});

builder.Services.AddTransient<IClaimsTransformation, KeycloakRoleClaimsTransformation>();

// Register MediatR handlers from the feature assembly
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(backend.Orders.Requests.Orders.CreateDigitalOrderCommand).Assembly));

// Configure database connections - use dedicated connection strings per service
var ordersDbConnectionString = builder.Configuration.GetConnectionString("Orders");
var paymentsDbConnectionString = builder.Configuration.GetConnectionString("Payments");

if (string.IsNullOrWhiteSpace(ordersDbConnectionString))
{
    throw new InvalidOperationException(
        "Connection string 'Orders' is missing for backend.Orders.Api.");
}

if (string.IsNullOrWhiteSpace(paymentsDbConnectionString))
{
    throw new InvalidOperationException(
        "Connection string 'Payments' is missing for backend.Orders.Api.");
}

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(ordersDbConnectionString)
        .UseSnakeCaseNamingConvention());

builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseNpgsql(paymentsDbConnectionString)
        .UseSnakeCaseNamingConvention());

builder.Services.Configure<AuthServiceOptions>(builder.Configuration.GetSection(AuthServiceOptions.SectionName));

builder.Services.AddHttpClient<IUserDirectory, HttpUserDirectory>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<AuthServiceOptions>>().Value;
    if (string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        throw new InvalidOperationException(
            $"{AuthServiceOptions.SectionName}:BaseUrl is missing. Configure it in appsettings.json or provide it via environment variables.");
    }

    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<IEffectiveUserAccessor, EffectiveUserAccessor>();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddHttpContextAccessor();

// Register RabbitMQ connection factory
builder.Services.AddSingleton<RabbitMqConnectionFactory>();
builder.Services.Configure<backend.Shared.Configuration.RabbitMqOptions>(builder.Configuration.GetSection(backend.Shared.Configuration.RabbitMqOptions.SectionName));

// Override RabbitMQ URI from environment variable if available
builder.Services.PostConfigure<backend.Shared.Configuration.RabbitMqOptions>(options =>
{
    var aspireConnectionString = builder.Configuration.GetConnectionString("messaging");
    if (!string.IsNullOrWhiteSpace(aspireConnectionString))
    {
        options.Uri = aspireConnectionString;
    }
});

// Register outbox for orders service (uses OrdersDbContext)
builder.Services.AddScoped<IIntegrationEventOutbox, IntegrationEventOutbox<OrdersDbContext>>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

internal sealed class JwkStore
{
    private readonly ReaderWriterLockSlim _lock = new();
    private List<Microsoft.IdentityModel.Tokens.SecurityKey> _keys = new();

    public IReadOnlyList<Microsoft.IdentityModel.Tokens.SecurityKey> GetKeys()
    {
        _lock.EnterReadLock();
        try { return _keys.ToList(); }
        finally { _lock.ExitReadLock(); }
    }

    public void UpdateKeys(IEnumerable<Microsoft.IdentityModel.Tokens.SecurityKey> keys)
    {
        _lock.EnterWriteLock();
        try { _keys = keys.ToList(); }
        finally { _lock.ExitWriteLock(); }
    }
}

internal sealed class KeycloakJwkRefresher : BackgroundService
{
    private readonly JwkStore _store;
    private readonly string _metadataAddress;
    private readonly HttpClient _httpClient;
    private readonly ILogger<KeycloakJwkRefresher> _logger;
    private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(15);

    public KeycloakJwkRefresher(JwkStore store, string metadataAddress, ILogger<KeycloakJwkRefresher> logger)
    {
        _store = store;
        _metadataAddress = metadataAddress;
        _logger = logger;
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
