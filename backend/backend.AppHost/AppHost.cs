using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddConnectionString("Default");
var rabbitmq = builder.AddConnectionString("messaging");

builder.AddProject<Projects.backend_Api>("api")
    .WithReference(database)
    .WithEnvironment("ConnectionStrings__Default", database.Resource.ConnectionStringExpression)
    .WithReference(rabbitmq)
    .WithEnvironment("ConnectionStrings__messaging", rabbitmq.Resource.ConnectionStringExpression)
    .WithEnvironment("RabbitMq__Uri", rabbitmq.Resource.ConnectionStringExpression);

builder.Build().Run();
