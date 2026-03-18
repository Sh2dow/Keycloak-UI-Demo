using System.Security.Claims;
using backend.Application.Behaviors;
using backend.Application.Exceptions;
using backend.Application.Messaging;
using backend.Application.Security;
using backend.Application.Users;
using backend.Configuration;
using backend.Data;
using backend.Infrastructure.Messaging;
using backend.Infrastructure.Orders;
using backend.Infrastructure.Payments;
using backend.Requests.Orders;
using backend.Requests.Tasks;
using backend.Requests.Users;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var featureAssemblies = new[]
{
    typeof(Program).Assembly,
    typeof(CreateUserCommand).Assembly,
    typeof(CreateOrderCommand).Assembly,
    typeof(CreateTaskCommand).Assembly
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
builder.Services.Configure<backend.Configuration.RabbitMqOptions>(builder.Configuration.GetSection(backend.Configuration.RabbitMqOptions.SectionName));
builder.Services.Configure<PaymentsOptions>(builder.Configuration.GetSection(PaymentsOptions.SectionName));
builder.Services.Configure<AuthServiceOptions>(builder.Configuration.GetSection(AuthServiceOptions.SectionName));
builder.Services.AddHttpClient<IUserDirectory, HttpUserDirectory>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthServiceOptions>>().Value;
    if (string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        throw new InvalidOperationException(
            "AuthService:BaseUrl is missing. Configure it in appsettings.json or provide it via environment variables.");
    }

    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
});

// Register integration event outbox
builder.Services.AddTransient<IIntegrationEventOutbox, DbIntegrationEventOutbox>();

// Configure database connection
var defaultConnectionString = builder.Configuration.GetConnectionString("Default");

if (string.IsNullOrWhiteSpace(defaultConnectionString))
{
    throw new InvalidOperationException(
        "Connection string 'Default' is missing. Ensure appsettings.json is loaded or " +
        "backend.AppHost injects ConnectionStrings__Default for backend.Api.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(defaultConnectionString)
        .UseSnakeCaseNamingConvention());

// Configure RabbitMQ connection factory with environment variable support
builder.Services.AddSingleton<RabbitMqConnectionFactory>();
builder.Services.AddTransient<IClaimsTransformation, KeycloakRoleClaimsTransformation>();

// Override RabbitMQ URI from environment variable if available
builder.Services.PostConfigure<backend.Configuration.RabbitMqOptions>(options =>
{
    var aspireConnectionString = builder.Configuration.GetConnectionString("messaging");
    if (!string.IsNullOrWhiteSpace(aspireConnectionString))
    {
        options.Uri = aspireConnectionString;
    }
});
builder.Services.AddHostedService<RabbitMqOutboxDispatcher>();
builder.Services.AddHostedService<PaymentStubConsumer>();
builder.Services.AddHostedService<OrderSagaConsumer>();
builder.Services.AddHostedService<OrderExecutionDispatchConsumer>();

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

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.Authority = normalizedAuthority;
        options.MetadataAddress = normalizedMetadataAddress;
        options.RequireHttpsMetadata = false;
        options.MapInboundClaims = false;
        options.BackchannelHttpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = true,
            ValidIssuer = normalizedAuthority,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            NameClaimType = "sub",
            RoleClaimType = ClaimTypes.Role
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

// Get configuration values from strongly-typed options
var dbOptions = app.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>();
var rabbitMqOptions = app.Configuration.GetSection(backend.Configuration.RabbitMqOptions.SectionName).Get<backend.Configuration.RabbitMqOptions>();

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

app.Logger.LogInformation(
    "Startup config. Environment={Environment}; RabbitMq={RabbitMq}; KeycloakAuthority={KeycloakAuthority}; KeycloakMetadata={KeycloakMetadata}",
    app.Environment.EnvironmentName,
    FormatRabbitMqTarget(rabbitMqOptions?.Uri),
    normalizedAuthority,
    normalizedMetadataAddress);

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("dev");

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseMiddleware<ProblemDetailsExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
