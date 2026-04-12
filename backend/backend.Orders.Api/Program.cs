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
builder.Services.AddProblemDetails();

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
