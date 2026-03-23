using backend.Domain.Data;
using backend.Infrastructure.Application.Users;
using backend.ServiceDefaults;
using backend.Shared.Application.Users;
using Microsoft.EntityFrameworkCore;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(backend.Users.Requests.Users.CreateUserCommand).Assembly));

// Configure database connections - use dedicated connection strings per service
var authDbConnectionString = builder.Configuration.GetConnectionString("Auth");

if (string.IsNullOrWhiteSpace(authDbConnectionString))
{
    throw new InvalidOperationException(
        "Connection string 'Auth' is missing for backend.Users.Api.");
}

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
