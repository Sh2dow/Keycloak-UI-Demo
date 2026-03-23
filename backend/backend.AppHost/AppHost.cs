var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddConnectionString("Default", "Host=localhost;Port=5432;Database=keycloak_demo;Username=keycloak;Password=123");
var authDbConnectionString = builder.AddConnectionString("Auth", "Host=localhost;Port=5432;Database=keycloak_demo_auth;Username=keycloak;Password=123");
var tasksDbConnectionString = builder.AddConnectionString("Tasks", "Host=localhost;Port=5432;Database=keycloak_demo_tasks;Username=keycloak;Password=123");
var ordersDbConnectionString = builder.AddConnectionString("Orders", "Host=localhost;Port=5432;Database=keycloak_demo_orders;Username=keycloak;Password=123");
var paymentsDbConnectionString = builder.AddConnectionString("Payments", "Host=localhost;Port=5432;Database=keycloak_demo_payments;Username=keycloak;Password=123");
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

var usersApi = builder.AddProject<Projects.backend_Users_Api>("users-api")
    .WithReference(authDbConnectionString)
    .WithReference(rabbitmq)
    .WithEnvironment("ConnectionStrings__Auth", authDbConnectionString.Resource.ConnectionStringExpression)
    .WithEnvironment("RabbitMq__Uri", rabbitmq.Resource.ConnectionStringExpression)
    .WithEnvironment("ASPNETCORE_URLS", "http://127.0.0.1:5005")
    .WaitFor(api)
    .WaitFor(authDbConnectionString)
    .WaitFor(rabbitmq);

var tasksApi = builder.AddProject<Projects.backend_Tasks_Api>("tasks-api")
    .WithReference(tasksDbConnectionString)
    .WithReference(authDbConnectionString)
    .WithReference(rabbitmq)
    .WithEnvironment("ConnectionStrings__Tasks", tasksDbConnectionString.Resource.ConnectionStringExpression)
    .WithEnvironment("ConnectionStrings__Auth", authDbConnectionString.Resource.ConnectionStringExpression)
    .WithEnvironment("RabbitMq__Uri", rabbitmq.Resource.ConnectionStringExpression)
    .WithEnvironment("ASPNETCORE_URLS", "http://127.0.0.1:5002")
    .WaitFor(api)
    .WaitFor(tasksDbConnectionString)
    .WaitFor(authDbConnectionString)
    .WaitFor(rabbitmq);

var ordersApi = builder.AddProject<Projects.backend_Orders_Api>("orders-api")
    .WithReference(ordersDbConnectionString)
    .WithReference(paymentsDbConnectionString)
    .WithReference(authDbConnectionString)
    .WithReference(rabbitmq)
    .WithEnvironment("ConnectionStrings__Orders", ordersDbConnectionString.Resource.ConnectionStringExpression)
    .WithEnvironment("ConnectionStrings__Payments", paymentsDbConnectionString.Resource.ConnectionStringExpression)
    .WithEnvironment("ConnectionStrings__Auth", authDbConnectionString.Resource.ConnectionStringExpression)
    .WithEnvironment("RabbitMq__Uri", rabbitmq.Resource.ConnectionStringExpression)
    .WithEnvironment("ASPNETCORE_URLS", "http://127.0.0.1:5003")
    .WaitFor(api)
    .WaitFor(ordersDbConnectionString)
    .WaitFor(paymentsDbConnectionString)
    .WaitFor(authDbConnectionString)
    .WaitFor(rabbitmq);

var paymentsApi = builder.AddProject<Projects.backend_Payments_Api>("payments-api")
    .WithReference(paymentsDbConnectionString)
    .WithReference(authDbConnectionString)
    .WithReference(rabbitmq)
    .WithEnvironment("ConnectionStrings__Payments", paymentsDbConnectionString.Resource.ConnectionStringExpression)
    .WithEnvironment("ConnectionStrings__Auth", authDbConnectionString.Resource.ConnectionStringExpression)
    .WithEnvironment("RabbitMq__Uri", rabbitmq.Resource.ConnectionStringExpression)
    .WithEnvironment("ASPNETCORE_URLS", "http://127.0.0.1:5004")
    .WaitFor(api)
    .WaitFor(paymentsDbConnectionString)
    .WaitFor(authDbConnectionString)
    .WaitFor(rabbitmq);

builder.Build().Run();