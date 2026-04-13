using System.Security.Claims;
using System.Text.Json;
using backend.Api;
using backend.Api.Application.Exceptions;
using backend.Api.Controllers;
using backend.Infrastructure.Application.Behaviors;
using backend.Infrastructure.Application.Security;
using backend.Infrastructure.Application.Users;
using backend.Infrastructure.Infrastructure.Messaging;
using backend.ServiceDefaults;
using backend.Shared.Application.Users;
using backend.Shared.Configuration;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RabbitMqOptions = backend.Shared.Configuration.RabbitMqOptions;

var builder = WebApplication.CreateBuilder(args);
var featureAssemblies = new[]
{
    typeof(Program).Assembly
};

builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssemblies(featureAssemblies)
);
builder.Services.AddValidatorsFromAssemblies(featureAssemblies);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddScoped<IEffectiveUserAccessor, EffectiveUserAccessor>();

// Configure strongly-typed options from configuration
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection(KeycloakOptions.SectionName));
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection(RabbitMqOptions.SectionName));
builder.Services.Configure<PaymentsOptions>(builder.Configuration.GetSection(PaymentsOptions.SectionName));
builder.Services.Configure<AuthServiceOptions>(builder.Configuration.GetSection(AuthServiceOptions.SectionName));
builder.Services.Configure<DownstreamServicesOptions>(builder.Configuration.GetSection(DownstreamServicesOptions.SectionName));
builder.Services.AddHttpClient("Orders", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<DownstreamServicesOptions>>().Value;
    ConfigureDownstreamClient(client, options.OrdersBaseUrl, $"{DownstreamServicesOptions.SectionName}:OrdersBaseUrl");
});
builder.Services.AddHttpClient("Payments", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<DownstreamServicesOptions>>().Value;
    ConfigureDownstreamClient(client, options.PaymentsBaseUrl, $"{DownstreamServicesOptions.SectionName}:PaymentsBaseUrl");
});
builder.Services.AddHttpClient("Users", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<DownstreamServicesOptions>>().Value;
    ConfigureDownstreamClient(client, options.UsersBaseUrl, $"{DownstreamServicesOptions.SectionName}:UsersBaseUrl");
});
builder.Services.AddHttpClient("Tasks", (serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<DownstreamServicesOptions>>().Value;
    ConfigureDownstreamClient(client, options.TasksBaseUrl, $"{DownstreamServicesOptions.SectionName}:TasksBaseUrl");
});
builder.Services.AddHttpClient<IUserDirectory, HttpUserDirectory>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<AuthServiceOptions>>().Value;
    ConfigureDownstreamClient(client, options.BaseUrl, $"{AuthServiceOptions.SectionName}:BaseUrl");
});

// Register integration event outbox - removed since Users handlers are no longer registered in main API
// builder.Services.AddTransient<IIntegrationEventOutbox, DbIntegrationEventOutbox>();

// Note: Shared DbContext removed - each service (Tasks, Orders, Payments, Auth) now has its own DB
// The main API is now a gateway/BFF that routes to individual services via HTTP or reverse proxy

// Configure RabbitMQ connection factory with environment variable support
builder.Services.AddSingleton<RabbitMqConnectionFactory>();
builder.Services.AddTransient<IClaimsTransformation, KeycloakRoleClaimsTransformation>();

// Override RabbitMQ URI from environment variable if available
builder.Services.PostConfigure<RabbitMqOptions>(options =>
{
    var aspireConnectionString = builder.Configuration.GetConnectionString("messaging");
    if (!string.IsNullOrWhiteSpace(aspireConnectionString))
    {
        options.Uri = aspireConnectionString;
    }
});

// Note: RabbitMqOutboxDispatcher removed from backend.Api (gateway/BFF)
// Each service (Orders, Tasks, Payments, Auth) should register its own dispatcher if needed

builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", p => p
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Configure Keycloak authentication options
var keycloakOptions = builder.Configuration.GetSection(KeycloakOptions.SectionName).Get<KeycloakOptions>();
if (keycloakOptions == null || string.IsNullOrWhiteSpace(keycloakOptions.Authority))
{
    throw new InvalidOperationException(
        "Keycloak authority is missing. Configure 'Keycloak:Authority' in appsettings.json " +
        "or provide it via environment variables.");
}

var normalizedAuthority = keycloakOptions.Authority.TrimEnd('/');
var normalizedMetadataAddress = string.IsNullOrWhiteSpace(keycloakOptions.MetadataAddress)
    ? $"{normalizedAuthority}/.well-known/openid-configuration"
    : keycloakOptions.MetadataAddress;

var jwkStore = new JwkStore();
var jwkRefresher = new KeycloakJwkRefresher(
    jwkStore,
    normalizedMetadataAddress,
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

builder.Services.AddAuthentication(options =>
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

                logger.LogWarning(
                    ctx.Exception,
                    "JWT authentication failed for {Path}",
                    ctx.Request.Path
                );
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                var logger = ctx.HttpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("JwtBearer");

                logger.LogDebug(
                    "JWT challenge for {Path}. Error={Error}, Description={Description}",
                    ctx.Request.Path,
                    ctx.Error,
                    ctx.ErrorDescription
                );
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

var rabbitMqOptions = app.Configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>();

string FormatRabbitMqTarget(string? uriString)
{
    if (string.IsNullOrWhiteSpace(uriString))
    {
        return "missing";
    }

    if (!Uri.TryCreate(uriString, UriKind.Absolute, out var uri))
    {
        return "invalid";
    }

    return $"{uri.Host}:{uri.Port}";
}

static void ConfigureDownstreamClient(HttpClient client, string? baseUrl, string settingName)
{
    if (string.IsNullOrWhiteSpace(baseUrl))
    {
        throw new InvalidOperationException(
            $"{settingName} is missing. Configure it in appsettings.json or provide it via environment variables.");
    }

    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(10);
}

app.Logger.LogInformation(
    "Startup config. Environment={Environment}; RabbitMq={RabbitMq}; KeycloakAuthority={KeycloakAuthority}; KeycloakMetadata={KeycloakMetadata}",
    app.Environment.EnvironmentName,
    FormatRabbitMqTarget(rabbitMqOptions?.Uri),
    normalizedAuthority,
    normalizedMetadataAddress);

app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseMiddleware<ProblemDetailsExceptionMiddleware>();
app.UseRouting();
app.UseCors("dev");
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
