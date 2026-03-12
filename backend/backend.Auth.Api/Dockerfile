FROM amazon/aws-cli:2 AS awscli

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/backend.Auth.Api/backend.Auth.Api.csproj backend/backend.Auth.Api/
COPY backend/backend.Domain/backend.Domain.csproj backend/backend.Domain/
COPY backend/backend.Infrastructure/backend.Infrastructure.csproj backend/backend.Infrastructure/
COPY backend/backend.ServiceDefaults/backend.ServiceDefaults.csproj backend/backend.ServiceDefaults/
COPY backend/backend.Shared/backend.Shared.csproj backend/backend.Shared/

RUN dotnet restore backend/backend.Auth.Api/backend.Auth.Api.csproj

COPY backend/ backend/

RUN dotnet publish backend/backend.Auth.Api/backend.Auth.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends python3 ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY --from=awscli /usr/local/aws-cli /usr/local/aws-cli
COPY --from=awscli /usr/local/bin/aws /usr/local/bin/aws

COPY --from=build /app/publish .
COPY backend/scripts/start-backend-auth-api.sh /app/start-backend-auth-api.sh

RUN chmod +x /app/start-backend-auth-api.sh

EXPOSE 8080

ENTRYPOINT ["/app/start-backend-auth-api.sh"]
