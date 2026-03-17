cd ..\backend
dotnet ef database update --project backend.Domain\backend.Domain.csproj --startup-project backend.Api\backend.Api.csproj  --context AuthDbContext
dotnet ef database update --project backend.Domain\backend.Domain.csproj --startup-project backend.Api\backend.Api.csproj  --context AppDbContext
