# NexTraceOne — Local Development Setup

## Prerequisites

- .NET 10.0 SDK
- Node.js 20+ / npm 10+
- PostgreSQL 16+
- (Optional) Ollama for local AI

## 1. Database Setup

Create the 4 databases in PostgreSQL:

```sql
CREATE DATABASE nextraceone_identity;
CREATE DATABASE nextraceone_catalog;
CREATE DATABASE nextraceone_operations;
CREATE DATABASE nextraceone_ai;
CREATE USER nextraceone WITH PASSWORD '<your-password>';
GRANT ALL PRIVILEGES ON DATABASE nextraceone_identity TO nextraceone;
GRANT ALL PRIVILEGES ON DATABASE nextraceone_catalog TO nextraceone;
GRANT ALL PRIVILEGES ON DATABASE nextraceone_operations TO nextraceone;
GRANT ALL PRIVILEGES ON DATABASE nextraceone_ai TO nextraceone;
```

## 2. Configure Secrets

**Never** put passwords in `appsettings.json`. Use:

```bash
cd src/platform/NexTraceOne.ApiHost
dotnet user-secrets set "ConnectionStrings:IdentityDatabase" "Host=localhost;Port=5432;Database=nextraceone_identity;Username=nextraceone;Password=<pw>;Maximum Pool Size=15"
# Repeat for all 17 connection strings (see appsettings.json for the keys)
```

The JWT secret for development is pre-configured in `appsettings.Development.json`.

## 3. Run Migrations

In Development mode, migrations run automatically on startup.
Alternatively, apply manually:

```bash
dotnet ef database update --project src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure --startup-project src/platform/NexTraceOne.ApiHost --context IdentityDbContext
# Repeat for each DbContext (16 total)
```

## 4. Start the Backend

```bash
cd src/platform/NexTraceOne.ApiHost
dotnet run
```

Health endpoints: `http://localhost:5000/health`, `/ready`, `/live`

## 5. Start the Frontend

```bash
cd src/frontend
npm install
npm run dev
```

UI: `http://localhost:5173`

## 6. Run Tests

```bash
# Backend unit tests (1658 tests)
dotnet test --filter "FullyQualifiedName!~E2E&FullyQualifiedName!~IntegrationTests"

# Frontend tests (398 tests)
cd src/frontend && npm test
```

## Configuration Reference

| File | Purpose |
|------|---------|
| `appsettings.json` | Base config — no secrets, pool size 10 |
| `appsettings.Development.json` | Dev overrides — JWT secret, pool size 15-20 |
| User Secrets | Database passwords, API keys |

## Environment Variables

| Variable | Purpose |
|----------|---------|
| `NEXTRACE_AUTO_MIGRATE` | Set `true` for staging auto-migrate (blocked in production) |
| `NEXTRACE_SKIP_INTEGRITY` | Set `true` to skip assembly integrity check |
| `NEXTRACE_IGNORE_PENDING_MODEL_CHANGES` | Set `true` to suppress EF model change warnings |
