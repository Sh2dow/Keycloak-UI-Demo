using System.Security.Claims;
using backend.Application.Behaviors;
using backend.Application.Exceptions;
using backend.Application.Security;
using backend.Application.Users;
using backend.Data;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly)
);
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddScoped<IEffectiveUserAccessor, EffectiveUserAccessor>();
builder.Services.AddTransient<IClaimsTransformation, KeycloakRoleClaimsTransformation>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
            builder.Configuration.GetConnectionString("Default"))
        .UseSnakeCaseNamingConvention());

builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", p => p
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var authority = builder.Configuration["Keycloak:Authority"]!;

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.Authority = authority;
        options.RequireHttpsMetadata = false;
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = true,
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

app.Run();
