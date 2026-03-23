using backend.Domain.Data;
using backend.Infrastructure.Application.Users;
using backend.ServiceDefaults;
using backend.Shared.Application.Users;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure database connections - use dedicated connection strings per service
var ordersDbConnectionString = builder.Configuration.GetConnectionString("Orders");
var paymentsDbConnectionString = builder.Configuration.GetConnectionString("Payments");
var authDbConnectionString = builder.Configuration.GetConnectionString("Auth");

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

if (string.IsNullOrWhiteSpace(authDbConnectionString))
{
    throw new InvalidOperationException(
        "Connection string 'Auth' is missing for backend.Orders.Api.");
}

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(ordersDbConnectionString)
        .UseSnakeCaseNamingConvention());

builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseNpgsql(paymentsDbConnectionString)
        .UseSnakeCaseNamingConvention());

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(authDbConnectionString)
        .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IUserDirectory, EfUserDirectory>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
