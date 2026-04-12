using backend.Domain.Data;
using backend.Infrastructure.Application.Users;
using backend.ServiceDefaults;
using backend.Shared.Application.Users;
using backend.Shared.Configuration;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

// Configure strongly-typed options from configuration
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));

// Configure database connection
var authConnectionString = builder.Configuration.GetConnectionString("Auth");
if (string.IsNullOrWhiteSpace(authConnectionString))
{
    throw new InvalidOperationException(
        "Connection string 'Auth' is missing for backend.Auth.Api.");
}

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(authConnectionString)
        .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IUserDirectory, EfUserDirectory>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
