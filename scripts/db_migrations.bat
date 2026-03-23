cd ..\backend

echo "Applying migrations to AuthDbContext..."
dotnet ef database update --project backend.Domain\backend.Domain.csproj --startup-project backend.Auth.Api\backend.Auth.Api.csproj --context AuthDbContext

echo "Applying migrations to TasksDbContext..."
dotnet ef database update --project backend.Domain\backend.Domain.csproj --startup-project backend.Tasks.Api\backend.Tasks.Api.csproj --context TasksDbContext

echo "Applying migrations to OrdersDbContext..."
dotnet ef database update --project backend.Domain\backend.Domain.csproj --startup-project backend.Orders.Api\backend.Orders.Api.csproj --context OrdersDbContext

echo "Applying migrations to PaymentsDbContext..."
dotnet ef database update --project backend.Domain\backend.Domain.csproj --startup-project backend.Payments.Api\backend.Payments.Api.csproj --context PaymentsDbContext

echo "All migrations applied successfully."
