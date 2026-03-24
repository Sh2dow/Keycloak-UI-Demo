var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.backend_Api>("api");
api.WithEnvironment("ConnectionStrings__Default", "Host=localhost;Port=5432;Database=keycloak_demo;Username=keycloak;Password=123");
api.WithEnvironment("RabbitMq__Uri", "amqp://guest:guest@localhost:5672");

var authApi = builder.AddProject<Projects.backend_Auth_Api>("auth-api");
authApi.WithEnvironment("ConnectionStrings__Auth", "Host=localhost;Port=5432;Database=keycloak_demo_auth;Username=keycloak;Password=123");
authApi.WithEnvironment("RabbitMq__Uri", "amqp://guest:guest@localhost:5672");
authApi.WithEnvironment("ASPNETCORE_URLS", "http://127.0.0.1:5001");

var usersApi = builder.AddProject<Projects.backend_Users_Api>("users-api");
usersApi.WithEnvironment("ConnectionStrings__Auth", "Host=localhost;Port=5432;Database=keycloak_demo_auth;Username=keycloak;Password=123");
usersApi.WithEnvironment("RabbitMq__Uri", "amqp://guest:guest@localhost:5672");
usersApi.WithEnvironment("ASPNETCORE_URLS", "http://127.0.0.1:5005");

var tasksApi = builder.AddProject<Projects.backend_Tasks_Api>("tasks-api");
tasksApi.WithEnvironment("ConnectionStrings__Tasks", "Host=localhost;Port=5432;Database=keycloak_demo_tasks;Username=keycloak;Password=123");
tasksApi.WithEnvironment("ConnectionStrings__Auth", "Host=localhost;Port=5432;Database=keycloak_demo_auth;Username=keycloak;Password=123");
tasksApi.WithEnvironment("RabbitMq__Uri", "amqp://guest:guest@localhost:5672");
tasksApi.WithEnvironment("RabbitMq__Enabled", "true");
tasksApi.WithEnvironment("ASPNETCORE_URLS", "http://127.0.0.1:5002");

var ordersApi = builder.AddProject<Projects.backend_Orders_Api>("orders-api");
ordersApi.WithReference(authApi);
ordersApi.WithEnvironment("ConnectionStrings__Orders", "Host=localhost;Port=5432;Database=keycloak_demo_orders;Username=keycloak;Password=123");
ordersApi.WithEnvironment("ConnectionStrings__Payments", "Host=localhost;Port=5432;Database=keycloak_demo_payments;Username=keycloak;Password=123");
ordersApi.WithEnvironment("ConnectionStrings__Auth", "Host=localhost;Port=5432;Database=keycloak_demo_auth;Username=keycloak;Password=123");
ordersApi.WithEnvironment("RabbitMq__Uri", "amqp://guest:guest@localhost:5672");
ordersApi.WithEnvironment("RabbitMq__Enabled", "true");
ordersApi.WithEnvironment("ASPNETCORE_URLS", "http://127.0.0.1:5003");

var paymentsApi = builder.AddProject<Projects.backend_Payments_Api>("payments-api");
paymentsApi.WithEnvironment("ConnectionStrings__Payments", "Host=localhost;Port=5432;Database=keycloak_demo_payments;Username=keycloak;Password=123");
paymentsApi.WithEnvironment("ConnectionStrings__Auth", "Host=localhost;Port=5432;Database=keycloak_demo_auth;Username=keycloak;Password=123");
paymentsApi.WithEnvironment("RabbitMq__Uri", "amqp://guest:guest@localhost:5672");
paymentsApi.WithEnvironment("RabbitMq__Enabled", "true");
paymentsApi.WithEnvironment("ASPNETCORE_URLS", "http://127.0.0.1:5004");

builder.Build().Run();
