using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddConnectionString("Default");
var rabbitmq = builder.AddConnectionString("messaging");

var api = builder.AddProject<Projects.backend_Api>("api")
    .WithReference(database)
    .WithReference(rabbitmq)
    .WithEnvironment("ConnectionStrings__Default", database.Resource.ConnectionStringExpression)
    .WithEnvironment("RabbitMq__Uri", rabbitmq.Resource.ConnectionStringExpression)
    .WaitFor(database)
    .WaitFor(rabbitmq);

builder.AddProject<Projects.backend_Auth_Api>("auth")
    .WithReference(database)
    .WithReference(rabbitmq)
    .WithEnvironment("ConnectionStrings__Default", database.Resource.ConnectionStringExpression)
    .WithEnvironment("RabbitMq__Uri", rabbitmq.Resource.ConnectionStringExpression)
    .WaitFor(api)
    .WaitFor(database)
    .WaitFor(rabbitmq);

builder.Build().Run();
