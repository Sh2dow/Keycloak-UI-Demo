var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddConnectionString("Default", "Host=localhost;Port=5432;Database=keycloak_demo;Username=keycloak;Password=123");
var authDbConnectionString = builder.AddConnectionString("Auth", "Host=localhost;Port=5432;Database=keycloak_demo_auth;Username=keycloak;Password=123");
var rabbitmq = builder.AddConnectionString("messaging", "amqp://guest:guest@localhost:5672");

var api = builder.AddProject<Projects.backend_Api>("api")
    .WithReference(database)
    .WithReference(rabbitmq)
    .WithEnvironment("ConnectionStrings__Default", database.Resource.ConnectionStringExpression)
    .WithEnvironment("RabbitMq__Uri", rabbitmq.Resource.ConnectionStringExpression)
    .WaitFor(database)
    .WaitFor(rabbitmq);

var authApi = builder.AddProject<Projects.backend_Auth_Api>("auth-api")
    .WithReference(authDbConnectionString)
    .WithReference(rabbitmq)
    .WithEnvironment("ConnectionStrings__Auth", authDbConnectionString.Resource.ConnectionStringExpression)
    .WithEnvironment("RabbitMq__Uri", rabbitmq.Resource.ConnectionStringExpression)
    .WithEnvironment("ASPNETCORE_URLS", "http://127.0.0.1:5001")
    .WaitFor(api)
    .WaitFor(authDbConnectionString)
    .WaitFor(rabbitmq);

builder.Build().Run();