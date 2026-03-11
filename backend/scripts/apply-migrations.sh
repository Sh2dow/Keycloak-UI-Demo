#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."

echo "Applying auth database migrations..."
export ConnectionStrings__Default="Host=auth_db;Port=5432;Database=keycloak_demo_auth;Username=keycloak;Password=123"
dotnet ef database update --project backend.Domain/backend.Domain.csproj --context AuthDbContext

echo "Applying app database migrations..."
export ConnectionStrings__Default="Host=app_db;Port=5432;Database=keycloak_demo;Username=keycloak;Password=123"
dotnet ef database update --project backend.Domain/backend.Domain.csproj --context AppDbContext
