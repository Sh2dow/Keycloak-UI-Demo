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

// Configure database connections
var defaultConnectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(defaultConnectionString))
{
    throw new InvalidOperationException(
        "Connection string 'Default' is missing for backend.Orders.Api.");
}

builder.Services.AddDbContext<OrdersDbContext>(options =>
    options.UseNpgsql(defaultConnectionString)
        .UseSnakeCaseNamingConvention());

builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("Connection string 'Default' not found.")));

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Auth") ?? throw new InvalidOperationException("Connection string 'Auth' not found.")));

builder.Services.AddScoped<IUserDirectory, EfUserDirectory>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
