* **Keycloak** (Docker)
* **PostgreSQL** for your app (Docker)
* **ASP.NET Core 10 Web API + EF Core (Npgsql)**
* **MediatR**
* **RabbitMQ**
* **React 18 + Vite + Bun** using **react-oidc-context**
* **JWT auth + role-based authorization** from Keycloak
* **Per-user data** in EF Core using Keycloak `sub` claim

---

## Initial project structure (already changed)

```text
myapp/
├─ docker-compose.yml
├─ backend/
│  ├─ backend.csproj
│  ├─ appsettings.json
│  ├─ Program.cs
│  ├─ Data/
│  │  └─ AppDbContext.cs
│  ├─ Models/
│  │  └─ TodoItem.cs
│  └─ Controllers/
│     └─ TodoController.cs
└─ frontend/
   ├─ package.json
   ├─ vite.config.ts
   ├─ index.html
   └─ src/
      ├─ main.tsx
      ├─ authConfig.ts
      ├─ api.ts
      └─ App.tsx
```

---

# 1) Docker: Keycloak + Postgres (Keycloak DB) + Postgres (App DB)

See [docker-compose.yml](docker-compose.yml) for the full configuration.

Run:

```bash
docker compose build backend_migrations
docker compose run --rm backend_migrations
docker compose up --build
```

Keycloak: `http://localhost:8080` (admin/admin)
App DB: `localhost:5432` (app/app, db=appdb)

---

# 2) Keycloak configuration (manual, 3 minutes)

In Keycloak Admin UI:

### Realm

* Create realm: `myrealm`

### Client (frontend)

* Create client: `react-client`
* Client type: **Public**
* Root URL: `http://localhost:5173`
* Valid redirect URIs: `http://localhost:5173/*`
* Web origins: `http://localhost:5173`
* Standard flow: **ON**
* PKCE: **ON**

### Client (backend API)

* Create client: `backend-api`
* Client type: **Confidential**
* Service accounts: optional (only if you need client-credentials later)
* This is mostly for clean separation; the API will validate tokens by issuer.

### Realm role

* Create realm role: `admin`

### User

* Create user `test`
* Set password (temporary OFF)
* Assign role `admin` (optional; only needed to test admin endpoint)

---

# 3) Backend: ASP.NET Core 10 + EF Core + JWT validation (Keycloak)

## For local EF Core migration + run

From `backend/`:

```bash
dotnet ef migrations add Initial
dotnet ef database update
dotnet run
```

Backend should be on `http://localhost:5000` or `https://localhost:7xxx` depending on your environment (Swagger will show it).

If you want it fixed to `http://localhost:5001`, tell me and I’ll give you the exact `launchSettings.json`.

---

# 4) Frontend: React + Vite + Bun + react-oidc-context

Create:

```bash
cd ..
bun create vite frontend --template react-ts
cd frontend
bun install
bun add react-oidc-context axios
```

## frontend/vite.config.ts (dev proxy to backend)

```ts
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/api": "http://localhost:5000"
    }
  }
});
```

## frontend/src/authConfig.ts

```ts
export const oidcConfig = {
  authority: "http://localhost:8080/realms/myrealm",
  client_id: "react-client",
  redirect_uri: "http://localhost:5173",
  response_type: "code",
  scope: "openid profile email",
};
```

Run frontend:

```bash
bun run dev
```

---

# 5) Run order (dev)

1. Infrastructure:

```bash
docker compose up -d
```

2. Backend:

```bash
cd backend
dotnet ef database update
dotnet run
```

3. Frontend:

```bash
cd ../frontend
bun run dev
```

Then go: `http://localhost:5173`

---

## Notes that matter in real projects

* **Refresh tokens**: for SPAs, refresh tokens are usually issued via “offline_access” scope + Keycloak client settings. If you want silent renew/refresh configured properly, tell me your Keycloak version settings screen you see and I’ll give the exact toggles. (Keycloak UI changes frequently.)
* **Audience validation**: I disabled it (`ValidateAudience = false`) because Keycloak often won’t set your API audience unless you configure “Audience” mappers. I can also show the *strict* setup (recommended for production).
* **Role mapping**: I mapped **realm roles** to ASP.NET roles. If you prefer **client roles** (better in many orgs), I’ll adjust the mapping.

---


  Run deploy.sh locally from your machine, not on the EC2 box. It uses your local AWS credentials to:

  - create/update IAM role policies
  - create/update RDS
  - launch/update EC2 bootstrap behavior