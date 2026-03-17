using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddConnectionString("Default");
var authDbConnectionString = builder.AddConnectionString("Auth");
var rabbitmq = builder.AddConnectionString("messaging");

var api = builder.AddProject<Projects.backend_Api>("api")
    .WithReference(database)
    .WithReference(rabbitmq)
    .WithEnvironment("ConnectionStrings__Default", database.Resource.ConnectionStringExpression)
    .WithEnvironment("RabbitMq__Uri", rabbitmq.Resource.ConnectionStringExpression)
    .WaitFor(database)
    .WaitFor(rabbitmq);

builder.AddProject<Projects.backend_Auth_Api>("auth-api")
    .WithReference(authDbConnectionString)
    .WithReference(rabbitmq)
    .WithEnvironment("ConnectionStrings__Auth", authDbConnectionString.Resource.ConnectionStringExpression)
    .WithEnvironment("RabbitMq__Uri", rabbitmq.Resource.ConnectionStringExpression)
    .WaitFor(api)
    .WaitFor(authDbConnectionString)
    .WaitFor(rabbitmq);

builder.Build().Run();
