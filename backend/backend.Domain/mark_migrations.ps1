# PowerShell script to mark migrations as applied for new contexts
# Run this script manually to avoid EF trying to apply migrations

$connectionString = "Host=localhost;Port=5432;Database=keycloak_demo;Username=keycloak;Password=123"

Write-Host "Marking migrations as applied..."

# TasksDbContext
Write-Host "TasksDbContext..."
Invoke-PgQuery $connectionString "INSERT INTO ""__EFMigrationsHistory"" (migration_id, product_version) VALUES ('20260322183024_Initial', '10.0.1') ON CONFLICT (migration_id) DO NOTHING;"

# OrdersDbContext
Write-Host "OrdersDbContext..."
Invoke-PgQuery $connectionString "INSERT INTO ""__EFMigrationsHistory"" (migration_id, product_version) VALUES ('20260322183024_Initial', '10.0.1') ON CONFLICT (migration_id) DO NOTHING;"

# PaymentsDbContext
Write-Host "PaymentsDbContext..."
Invoke-PgQuery $connectionString "INSERT INTO ""__EFMigrationsHistory"" (migration_id, product_version) VALUES ('20260322183024_Initial', '10.0.1') ON CONFLICT (migration_id) DO NOTHING;"

Write-Host "Done!"

function Invoke-PgQuery {
    param(
        [string]$ConnectionString,
        [string]$Query
    )
    
    # Write SQL to a temp file
    $sqlFile = [System.IO.Path]::GetTempFileName() + ".sql"
    Set-Content -Path $sqlFile -Value $Query -NoNewline
    
    # Try to use psql
    $psqlPath = "C:\Program Files\PostgreSQL\17\bin\psql.exe"
    if (Test-Path $psqlPath) {
        $env:PGPASSWORD = "123"
        & $psqlPath -h localhost -U keycloak -d keycloak_demo -f $sqlFile 2>&1 | Write-Host
        Remove-Item $sqlFile -Force
        return
    }
    
    # Fallback to dotnet ef database update with a custom script
    Write-Host "psql not found. Using dotnet ef..."
    $env:ConnectionStrings__Default = $connectionString
    dotnet ef database update --context TasksDbContext
}
