# Migration Summary - Phase 0 Complete

## What's Been Done

### 1. New Contexts Created
- `TasksDbContext` - handles Tasks and TaskComments
- `OrdersDbContext` - handles Orders, OrderSagaStates, OutboxMessages, ConsumedMessages
- `PaymentsDbContext` - handles PaymentEventRecords

### 2. Context Factories Created
- `TasksDbContextFactory`
- `OrdersDbContextFactory`
- `PaymentsDbContextFactory`
- `AuthDbContextFactory`

### 3. Migrations Generated
- Tasks migrations in `backend.Domain/TasksMigrations/`
- Orders migrations in `backend.Domain/OrdersMigrations/`
- Payments migrations in `backend.Domain/PaymentsMigrations/`
- Auth migrations in `backend.Domain/AuthMigrations/`

### 4. AppDbContext Marked as Obsolete
- All DbSets marked with `[Obsolete]` attributes
- Migration history in `AppDbContext` removed

### 5. Backend.Api Updated
- Uses `TasksDbContext`, `OrdersDbContext`, `PaymentsDbContext` instead of `AppDbContext`
- Still references `backend.Domain` for context types

## What Needs to Be Done Next

### Phase 0: Decouple Internally (Remaining)
1. **Apply migrations for new contexts** - Currently stuck because tables already exist from old `AppDbContext`
   - Solution: Manually mark migrations as applied using SQL
2. **Remove `AppDbContext` registrations** - No longer needed in `backend.Api`
3. **Update domain services** - Replace `AppDbContext` references with specific contexts

### Phase 1: Auth Service (Ready)
- Auth API is already standalone
- Needs publish user events to RabbitMQ
- Need to remove `AppUser` from `AppDbContext` and use `AuthDbContext`

### Phase 2: Extract Services
- Tasks service
- Orders service
- Payments service

## Known Issues

1. **Migrations not applied** - EF can't apply migrations because tables already exist
   - Need to manually insert into `__EFMigrationsHistory` table for each context
2. **Build warnings** - 124 warnings about using obsolete `AppDbContext`
   - These need to be fixed by replacing `AppDbContext` with specific contexts

## Commands to Complete Phase 0

```bash
# 1. Manually mark migrations as applied (run SQL script)
# SQL script: D:\Repos\Interview\Keycloak-UI-Demo\backend\backend.Domain\mark_new_migrations.sql

# 2. Run migrations for each context
dotnet ef database update --context TasksDbContext
dotnet ef database update --context OrdersDbContext
dotnet ef database update --context PaymentsDbContext

# 3. Remove obsolete AppDbContext usage from codebase
# Replace all references to AppDbContext with specific contexts
```

## Files Created/Modified

### New Files
- `backend.Domain/Data/TasksDbContext.cs`
- `backend.Domain/Data/OrdersDbContext.cs`
- `backend.Domain/Data/PaymentsDbContext.cs`
- `backend.Domain/Design/TasksDbContextFactory.cs`
- `backend.Domain/Design/OrdersDbContextFactory.cs`
- `backend.Domain/Design/PaymentsDbContextFactory.cs`

### Modified Files
- `backend.Domain/Data/AppDbContext.cs` - marked as obsolete
- `backend.Api/Program.cs` - uses new contexts
- `backend.Domain/appsettings.json` - added Default connection string
