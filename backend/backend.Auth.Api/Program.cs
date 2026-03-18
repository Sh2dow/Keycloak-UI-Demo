using backend.Application.Users;
using backend.Configuration;
using backend.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
