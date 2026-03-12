#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."

echo "Applying auth database migrations..."
export ConnectionStrings__Default="${AUTH_DB_CONNECTION_STRING:?AUTH_DB_CONNECTION_STRING is required}"
dotnet ef database update --project backend.Domain/backend.Domain.csproj --context AuthDbContext

echo "Applying app database migrations..."
export ConnectionStrings__Default="${APP_DB_CONNECTION_STRING:?APP_DB_CONNECTION_STRING is required}"
dotnet ef database update --project backend.Domain/backend.Domain.csproj --context AppDbContext
