# CLAUDE.md — NexTraceOne Project Instructions

This file provides context for Claude Code when working in this repository.
For the full product vision, architecture rules, and product strategy, read `.github/copilot-instructions.md`.

---

## Project Summary

**NexTraceOne** is an enterprise-grade, self-hosted platform for service governance, contract governance, change intelligence, operational reliability, AI-assisted operations, and FinOps. It is a **modular monolith** built on .NET 10 with strong DDD, Clean Architecture, and CQRS principles.

The product is designed as a **SaaS platform** with multi-tenancy, licensing, and capability-based feature gating — but also ships as on-premises / self-hosted.

---

## Repository Layout

```
src/
  building-blocks/          # Shared infrastructure, primitives, abstractions
    NexTraceOne.BuildingBlocks.Core          # Result<T>, Error, AggregateRoot<T>, TypedIdBase
    NexTraceOne.BuildingBlocks.Application   # ICurrentTenant, ICurrentUser, IUnitOfWork, IEventBus, MediatR behaviors
    NexTraceOne.BuildingBlocks.Infrastructure # NexTraceDbContextBase, interceptors, Outbox, EF conventions
    NexTraceOne.BuildingBlocks.Security      # TenantResolutionMiddleware, CurrentTenantAccessor, JWT, Break Glass
    NexTraceOne.BuildingBlocks.Observability # Metrics, tracing, health checks

  modules/                  # Domain modules (bounded contexts)
    aiknowledge/             # AI governance, models, budgets, orchestration, agents, skills
    auditcompliance/         # Audit trail, compliance policies, retention
    catalog/                 # Service catalog, contracts, graph, developer portal
    changegovernance/        # Change intelligence, releases, promotion, blast radius
    configuration/           # Platform configuration
    governance/              # Reports, risk, compliance rules
    identityaccess/          # Auth, tenants, users, roles, SSO, break glass
    integrations/            # External system connectors
    knowledge/               # Knowledge base, runbooks, operational notes
    notifications/           # Alerts and notifications
    operationalintelligence/ # Incidents, reliability, cost intelligence, runtime
    productanalytics/        # Product usage analytics

  platform/
    NexTraceOne.ApiHost          # Main HTTP host; wires all modules
    NexTraceOne.BackgroundWorkers # Worker service; Outbox processors + background jobs
    NexTraceOne.Ingestion.Api    # Telemetry/event ingestion endpoint

tests/
  building-blocks/
  modules/<module-name>/
```

Each module follows: `Domain / Application / Contracts / Infrastructure / API`

---

## Core Technical Patterns

### Result<T> — no exceptions for business flow

```csharp
// Handlers always return Result<T>
return Result.Success(new MyDto(...));
return Result.Failure(Error.Validation("Code", "Human-readable message"));
// NEVER: throw new Exception("business error")
```

### Strongly Typed IDs — always use `.Value` when comparing to Guid

```csharp
public sealed record MyEntityId(Guid Value) : TypedIdBase(Value)
{
    public static MyEntityId New() => new(Guid.NewGuid());
}
// When comparing with a List<Guid>:
assets.Where(a => guids.Contains(a.Id.Value))  // ✓
assets.Where(a => guids.Contains(a.Id))         // ✗ compile error
```

### Honest-Null Pattern — intentional placeholder readers

`IXxxReader` interfaces for analytics/reporting are registered with `NullXxxReader` implementations that return empty collections. This is **intentional** — they are cross-module pipeline placeholders. Do NOT mistake them for bugs.

`IXxxRepository` interfaces (CRUD/persistence) MUST have real EF Core implementations.

### Outbox Pattern

1. Domain events accumulate in `AggregateRoot<T>.DomainEvents`
2. `NexTraceDbContextBase.SaveChangesAsync()` converts them → `OutboxMessage` in the same transaction
3. `ModuleOutboxProcessorJob<TContext>` (one per DbContext) publishes via `IEventBus` every 5s
4. Multi-instance safe: `pg_try_advisory_lock` prevents duplicate processing

### Tenant Isolation

- `ICurrentTenant` injected everywhere — resolved by `TenantResolutionMiddleware` from JWT claims
- `TenantRlsInterceptor` sets `app.current_tenant_id` in PostgreSQL before every query
- All repository read methods must filter by `currentTenant.Id` (defense in depth)
- Exception: background/retention jobs that run without tenant context (platform-level operations)

### SaaS Capabilities

```csharp
// License-based feature gating
if (!currentTenant.HasCapability("contract_studio"))
    return Result.Failure(Error.Forbidden("CapabilityRequired", "..."));

// Plans: Starter, Professional, Enterprise, Trial
// TenantCapabilities.ForPlan(TenantPlan.Professional) → HashSet<string>
// Fallback: no license → Enterprise (all capabilities enabled)
```

---

## DI Registration Pattern

Each module has `Infrastructure/DependencyInjection.cs` with `AddXxxInfrastructure(services, config)`:

```csharp
services.AddBuildingBlocksInfrastructure(configuration);
var cs = configuration.GetRequiredConnectionString("MyModuleDatabase", "NexTraceOne");
services.AddDbContext<MyDbContext>((sp, opts) =>
    opts.UseNpgsql(cs)
        .AddInterceptors(
            sp.GetRequiredService<AuditInterceptor>(),
            sp.GetRequiredService<TenantRlsInterceptor>()));
services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<MyDbContext>());
// ... repositories ...
```

---

## What Is Currently Enabled vs. Disabled

| Feature | Status |
|---|---|
| PostgreSQL (24 databases) | ✅ Operational |
| Outbox pattern (all modules) | ✅ Operational |
| pg_advisory_lock (outbox) | ✅ Operational |
| RLS via TenantRlsInterceptor | ✅ Operational |
| JWT multi-tenant auth | ✅ Operational |
| Elasticsearch (observability) | ✅ Operational (configurable) |
| In-memory cache | ✅ Operational |
| Kafka | ⚠️ Disabled by default (NullKafkaEventProducer) |
| ClickHouse analytics | ⚠️ Disabled by default |
| Redis (distributed cache) | ❌ Not implemented |
| Polly (retry/circuit breaker) | ❌ Not implemented |
| LicenseRecalculationJob | ❌ Not implemented |
| Schema-per-tenant | ❌ TenantSchemaManager exists but unused |

---

## Conventions and Rules

### Language
- **Code, logs, exceptions**: English
- **XML docs, inline comments**: Portuguese (project convention)
- **UI text**: i18n keys always (never hardcoded)

### Code style
- `sealed` on all final classes
- `CancellationToken` on every async method
- `IDateTimeProvider` — never `DateTime.Now` or `DateTime.UtcNow` directly
- Primary constructors preferred for services and handlers
- No `Task.Run`, no `.Result`, no `.Wait()`

### Module boundaries
- A module's DbContext is NEVER accessed directly by another module
- Cross-module communication: published events (Outbox) or `IXxxModule` contract interface
- `IXxxModule` is a thin public service registered in the consuming module's DI

### Testing
- Tests mirror source: `tests/modules/<module>/`
- NSubstitute for mocking (use `ExceptionExtensions` namespace for `.ThrowsAsync()`)
- Use `Result<T>` assertion helpers; never assume exceptions from handlers
- In-memory EF Core for repository tests

---

## Key Files for Common Tasks

| Task | File(s) |
|---|---|
| Add a new handler | `src/modules/<m>/NexTraceOne.<M>.Application/<Subdomain>/Features/<Feature>/` |
| Register a new repository | `src/modules/<m>/NexTraceOne.<M>.Infrastructure/<Subdomain>/DependencyInjection.cs` |
| Add background job | `src/platform/NexTraceOne.BackgroundWorkers/Jobs/` + register in `Program.cs` |
| Add tenant capability check | `src/building-blocks/NexTraceOne.BuildingBlocks.Security/MultiTenancy/CurrentTenantAccessor.cs` |
| Add new EF entity | Domain entity + `IEntityTypeConfiguration` in Infrastructure + `dotnet ef migrations add` |
| Modify JWT claims | `src/modules/identityaccess/.../Services/JwtTokenGenerator.cs` |
| Add API endpoint | `src/modules/<m>/NexTraceOne.<M>.API/Endpoints/` (minimal API endpoint groups) |

---

## Current Branch Context

Working branch: `claude/startup-validation-contract-fixes-o7zzp`

Recent work (this session):
- P0 fixes: audit tenant isolation + outbox pg_advisory_lock
- SaaS-01: `HasCapability()` backed by JWT capabilities claims
- Startup DI fixes: registered missing `IXxxReader`/`IXxxRepository` across 5 DI files
- Test coverage: `LoginResponseBuilderTests`, `SelectTenantTests`, `TenantResolutionMiddlewareTests`

---

## Development Workflow

1. Always push to `claude/startup-validation-contract-fixes-o7zzp`
2. `git push -u origin <branch>` — retry up to 4× with exponential backoff on network errors
3. Build: `dotnet build NexTraceOne.sln` from repo root
4. Tests: `dotnet test` (NUnit/xUnit mix; see individual test projects)
5. Migrations: per-module, run from the Infrastructure project with design-time factory

---

## Anti-Patterns to Avoid

- Do not access another module's DbContext directly
- Do not throw exceptions for business validation — use `Result.Failure(Error.Validation(...))`
- Do not hardcode tenant ID, user ID, or environment in business logic
- Do not use `DateTime.Now` — use `IDateTimeProvider`
- Do not compare strongly-typed IDs directly to `Guid` — use `.Value`
- Do not assume Null readers are bugs — they are intentional phase-gated placeholders
- Do not add Null readers for CRUD repositories — those need real EF implementations
- Do not add `using` directives that couple Infrastructure to Domain directly
- Do not create a new EF entity without a migration
