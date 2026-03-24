using System.Security.Claims;
using System.Text;
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

builder.Services.AddScoped<CreateDigitalOrderCommandValidator>();
builder.Services.AddScoped<CreatePhysicalOrderCommandValidator>();
builder.Services.AddScoped<CreateOrderCommandValidator>();

var keycloakAuthority = builder.Configuration["Keycloak:Authority"];
if (string.IsNullOrWhiteSpace(keycloakAuthority))
{
    throw new InvalidOperationException("Keycloak:Authority is missing. Configure it in appsettings.json.");
}

var normalizedAuthority = keycloakAuthority.TrimEnd('/');
var metadataAddress = $"{normalizedAuthority}/.well-known/openid-configuration";

// Add authentication with Keycloak (but allow anonymous by default)
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.Authority = normalizedAuthority;
        options.MetadataAddress = metadataAddress;
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

builder.Services.AddDbContextFactory<OrdersDbContext>();

// Use HTTP-based user directory to call Auth.Api instead of direct DB access
// Using direct URL to Auth.Api for local development
builder.Services.AddHttpClient<IUserDirectory, HttpUserDirectory>(client =>
{
    client.BaseAddress = new Uri("http://127.0.0.1:5001");
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

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
