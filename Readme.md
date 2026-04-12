# Keycloak UI Demo

A microservices demo application showcasing modern .NET and React architecture with Keycloak authentication.

## Tech Stack

* **Keycloak** - Identity & Access Management (Docker)
* **PostgreSQL** - Separate databases per service (Docker/RDS)
* **ASP.NET Core 10** - Web API + EF Core (Npgsql)
* **.NET Aspire** - Service orchestration for local development
* **MediatR** - CQRS pattern implementation
* **RabbitMQ** - Async messaging with Outbox pattern
* **React 18 + Vite + Bun** - Frontend with Refine & Mantine UI
* **Keycloak Auth** - JWT auth + role-based authorization

---

## Project Structure

```text
Keycloak-UI-Demo/
├─ docker-compose.yml             # Production-like deployment
├─ Readme.md
├─ Docs/                          # Architecture & design docs
│  ├─ Changelog.md
│  ├─ 1. Startup.md
│  ├─ 2. MediatR.md
│  ├─ 3. Mapperly.md
│  ├─ 5. Microservices.md
│  ├─ 6. Saga pattern.md
│  └─ ...
├─ backend/
│  ├─ backend.slnx                # Solution file
│  ├─ backend.AppHost/            # .NET Aspire orchestrator
│  ├─ backend.ServiceDefaults/    # Shared Aspire defaults
│  ├─ backend.Api/                # Main API (Tasks legacy)
│  ├─ backend.Auth.Api/           # Auth microservice (port 5001)
│  ├─ backend.Users.Api/          # Users microservice (port 5005)
│  ├─ backend.Tasks.Api/          # Tasks microservice (port 5002)
│  ├─ backend.Tasks/              # Tasks business logic
│  ├─ backend.Orders.Api/         # Orders microservice (port 5003)
│  ├─ backend.Orders/             # Orders business logic + Saga
│  ├─ backend.Payments.Api/       # Payments microservice (port 5004)
│  ├─ backend.Payments/           # Payments business logic
│  ├─ backend.Users/              # Users business logic
│  ├─ backend.Domain/             # Shared domain models + EF migrations
│  ├─ backend.Infrastructure/     # Shared infrastructure (MediatR, etc.)
│  ├─ backend.Shared/             # Shared configuration & utilities
│  ├─ backend.Tests/              # Unit & integration tests
│  └─ scripts/                    # Migration & startup scripts
├─ frontend/
│  ├─ package.json                # Bun package manager
│  ├─ vite.config.ts
│  ├─ Dockerfile
│  ├─ nginx.conf
│  └─ src/
│     ├─ main.tsx
│     ├─ App.tsx
│     ├─ providers/
│     │  ├─ keycloakAuthProvider.ts
│     │  └─ keycloakDataProvider.ts
│     └─ pages/
│        ├─ tasks/
│        ├─ orders/
│        ├─ users/
│        ├─ clients/
│        ├─ groups/
│        └─ roles/
├─ keycloak/                      # Custom Keycloak config
├─ infra/                         # Infrastructure (Caddy, Nginx SSL)
├─ scripts/                       # Deployment & DB scripts
└─ memory/                        # Dev session notes
```

---

## Architecture

### Microservices

| Service | Port | Database | Description |
|---------|------|----------|-------------|
| backend.Api | 5000 | n/a | Main API gateway |
| backend.Auth.Api | 5001 | keycloak_demo_auth | Authentication service |
| backend.Tasks.Api | 5002 | keycloak_demo_tasks | Task management |
| backend.Orders.Api | 5003 | keycloak_demo_orders | Order processing + Saga |
| backend.Payments.Api | 5004 | keycloak_demo_payments | Payment processing |
| backend.Users.Api | 5005 | keycloak_demo_auth | User management |

### Key Patterns

- **CQRS** - Command/Query separation via MediatR
- **Outbox Pattern** - Reliable messaging with RabbitMQ
- **Saga Pattern** - Distributed transaction management for Orders→Payments flow
- **Domain Events** - OrderStatusChanged, PaymentCompleted, etc.

---

## Quick Start

### Option 1: Docker Compose (Production-like)

```bash
# Build and run migrations
docker compose build backend_migrations
docker compose run --rm backend_migrations

# Start all services
docker compose up --build
```

**Services:**
- Frontend: `http://localhost:5173`
- Keycloak: `http://localhost:8080` (admin/admin)
- API gateway: `http://localhost:5000`
- Auth API: `http://localhost:5001`
- Tasks API: `http://localhost:5002`
- Orders API: `http://localhost:5003`
- Payments API: `http://localhost:5004`
- Users API: `http://localhost:5005`
- RabbitMQ: `http://localhost:15672` (guest/guest)

### Option 2: .NET Aspire (Local Development)

```bash
cd backend
dotnet run --project backend.AppHost
```

This starts all microservices with the Aspire dashboard.

### Option 3: Individual Services

```bash
# Start infrastructure
docker compose up -d keycloak rabbitmq

# Run migrations
cd backend
dotnet ef database update --project backend.Domain

# Start backend
dotnet run --project backend.Api

# Start frontend
cd ../frontend
bun install
bun run dev
```

---

## Keycloak Configuration

In Keycloak Admin UI (`http://localhost:8080`):

### 1. Create Realm
- Name: `myrealm`

### 2. Create Client (Frontend)
- Client ID: `react-client`
- Client type: **Public**
- Root URL: `http://localhost:5173`
- Valid redirect URIs: `http://localhost:5173/*`
- Web origins: `http://localhost:5173`
- Standard flow: **ON**
- PKCE: **ON**

### 3. Create Role
- Realm role: `admin`

### 4. Create User
- Username: `test`
- Password: (set, temporary OFF)
- Assign role: `admin`

---

## Environment Variables

Docker Compose uses these environment variables (see `.env` or set directly):

```env
# Database
RDS_ENDPOINT=localhost
APP_DB_USERNAME=app
APP_DB_PASSWORD=app
AUTH_DB_USERNAME=auth
AUTH_DB_PASSWORD=auth

# Keycloak
KEYCLOAK_ADMIN_PASSWORD=admin
KEYCLOAK_REALM_URL=http://localhost:8080/realms/myrealm
```

---

## Frontend Stack

- **React 18** + TypeScript
- **Vite 7** - Build tool
- **Bun** - Package manager & runtime
- **Refine** - Admin panel framework
- **Mantine** - UI components
- **TanStack Query** - Data fetching
- **oidc-client-ts** - Keycloak authentication

---

## Development Notes

### Running Migrations

```bash
# All migrations
dotnet ef database update --project backend.Domain --startup-project backend.Api

# Specific context
dotnet ef database update --context OrdersDbContext --project backend.Domain
```

### Testing

```bash
cd backend/backend.Tests
dotnet test
```

### AWS Deployment

Run `scripts/deploy.sh` locally (uses AWS credentials):
- Creates/updates IAM role policies
- Provisions EC2 and RDS
- Creates the auth/tasks/orders/payments databases on the target RDS instance
- Starts the Docker Compose stack on EC2, including frontend, gateway, Keycloak, RabbitMQ, and the backend microservices

The `backend.AppHost` `aws` launch profile is not a deployment mechanism. It only starts the AppHost locally with AWS-oriented configuration.
