using backend.Domain.Data;
using backend.Infrastructure.Application.Users;
using backend.Infrastructure.Infrastructure.Messaging;
using backend.ServiceDefaults;
using backend.Shared.Application.Messaging;
using backend.Shared.Application.Users;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register MediatR handlers from the feature assembly
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(backend.Tasks.Requests.Tasks.CreateTaskCommand).Assembly));

// Configure database connections - use dedicated connection strings per service
var tasksDbConnectionString = builder.Configuration.GetConnectionString("Tasks");
var authDbConnectionString = builder.Configuration.GetConnectionString("Auth");

if (string.IsNullOrWhiteSpace(tasksDbConnectionString))
{
    throw new InvalidOperationException(
        "Connection string 'Tasks' is missing for backend.Tasks.Api.");
}

if (string.IsNullOrWhiteSpace(authDbConnectionString))
{
    throw new InvalidOperationException(
        "Connection string 'Auth' is missing for backend.Tasks.Api.");
}

builder.Services.AddDbContext<TasksDbContext>(options =>
    options.UseNpgsql(tasksDbConnectionString)
        .UseSnakeCaseNamingConvention());

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(authDbConnectionString)
        .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IUserDirectory, EfUserDirectory>();
builder.Services.AddScoped<IEffectiveUserAccessor, EffectiveUserAccessor>();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();

// Register RabbitMQ connection factory
builder.Services.AddSingleton<RabbitMqConnectionFactory>();

// Override RabbitMQ URI from environment variable if available
builder.Services.PostConfigure<RabbitMqOptions>(options =>
{
    var aspireConnectionString = builder.Configuration.GetConnectionString("messaging");
    if (!string.IsNullOrWhiteSpace(aspireConnectionString))
    {
        options.Uri = aspireConnectionString;
    }
});

// Register outbox dispatcher for tasks (optional - requires RabbitMQ)
if (builder.Configuration.GetValue<bool>("RabbitMq:Enabled", true))
{
    builder.Services.AddHostedService<RabbitMqOutboxDispatcher>();
}

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();
