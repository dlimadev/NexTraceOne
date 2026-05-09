# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## Build & Test Commands

```bash
# Build the whole solution
dotnet build NexTraceOne.sln

# Build a single module (faster for iterating)
dotnet build src/modules/identityaccess/NexTraceOne.IdentityAccess.API/NexTraceOne.IdentityAccess.API.csproj

# Run all unit tests (excludes E2E, Selenium, Integration which require infra)
dotnet test --filter "FullyQualifiedName!~E2E&FullyQualifiedName!~Selenium&FullyQualifiedName!~IntegrationTests"

# Run all tests in one module
dotnet test tests/modules/identityaccess/NexTraceOne.IdentityAccess.Tests/NexTraceOne.IdentityAccess.Tests.csproj

# Run a specific test by name
dotnet test --filter "FullyQualifiedName~CreateShould"

# Add an EF Core migration for a specific module context
# (run from repo root; set NEXTRACEONE_CONNECTION_STRING env var or it falls back to localhost)
dotnet ef migrations add <MigrationName> \
  --project src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure \
  --startup-project src/platform/NexTraceOne.ApiHost \
  --context IdentityDbContext

# Apply migrations (per-module design-time factories handle connection strings)
dotnet ef database update \
  --project src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure \
  --startup-project src/platform/NexTraceOne.ApiHost \
  --context IdentityDbContext
```

**Local secrets setup (first-time only):**
```bash
dotnet user-secrets init --project src/platform/NexTraceOne.ApiHost
dotnet user-secrets set "Jwt:Secret" "your-local-dev-secret-minimum-32-chars" --project src/platform/NexTraceOne.ApiHost
dotnet user-secrets set "ConnectionStrings:NexTraceOne" "Host=localhost;Port=5432;Database=nextraceone;Username=nextraceone;Password=..." --project src/platform/NexTraceOne.ApiHost
```

**Useful environment variables:**
- `NEXTRACE_SKIP_INTEGRITY=true` — skip assembly hash check on startup (dev/CI)
- `NEXTRACE_IGNORE_PENDING_MODEL_CHANGES=true` — suppress EF pending-migrations warning (AIKnowledge contexts)
- `NEXTRACEONE_CONNECTION_STRING` — override for EF design-time factories during `dotnet ef migrations add`

---

## Architecture Overview

NexTraceOne is a **modular monolith** using DDD, Clean Architecture, and CQRS (via MediatR). There are three runnable hosts:

- `src/platform/NexTraceOne.ApiHost` — HTTP API; mounts all module endpoints
- `src/platform/NexTraceOne.BackgroundWorkers` — worker service; Outbox processors + background jobs
- `src/platform/NexTraceOne.Ingestion.Api` — dedicated telemetry/event ingestion endpoint

Every bounded context under `src/modules/<name>/` is layered as:
```
Domain/       ← Entities, AggregateRoot, Value Objects, Domain Events
Application/  ← Commands, Queries, Handlers, Validators, Abstractions (IRepositories)
Contracts/    ← Public DTOs, Integration Events, IXxxModule interface (cross-module API)
Infrastructure/ ← EF Core DbContext + Configurations + Repositories + DI registration
API/          ← Minimal API endpoint groups, endpoint DI
```

### The 5 Building Blocks

| Project | Key types |
|---|---|
| `BuildingBlocks.Core` | `AggregateRoot<T>`, `Entity<T>`, `AuditableEntity<T>`, `TypedIdBase`, `Result<T>`, `Error`, `ErrorType` |
| `BuildingBlocks.Application` | `ICurrentTenant`, `ICurrentUser`, `IDateTimeProvider`, `IUnitOfWork`, `IEventBus`, `ICommand`, `IQuery`, pipeline behaviors |
| `BuildingBlocks.Infrastructure` | `NexTraceDbContextBase`, `TenantRlsInterceptor`, `AuditInterceptor`, `EncryptionInterceptor`, `OutboxMessage`, `ModuleOutboxProcessorJob<TContext>` |
| `BuildingBlocks.Security` | `TenantResolutionMiddleware`, `CurrentTenantAccessor`, JWT generation, Break Glass |
| `BuildingBlocks.Observability` | OpenTelemetry wiring, health checks, `IIngestionMetricsCollector` |

---

## CQRS Feature Pattern

Every handler lives in one file as a static class containing `Command`/`Query`, `Validator`, `Response`, and `Handler`:

```csharp
public static class ActivateAccount
{
    // Mark with IPublicRequest to bypass TenantIsolationBehavior (auth endpoints only)
    public sealed record Command(string Token, string Password) : ICommand<Response>, IPublicRequest;

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator() { RuleFor(x => x.Token).NotEmpty(); }
    }

    public sealed record Response(bool Activated);

    internal sealed class Handler(
        IAccountActivationTokenRepository tokenRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock) : ICommandHandler<Command, Response>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken cancellationToken)
        {
            // ...
            return new Response(true);   // implicit: Result.Success(new Response(true))
            // or:
            return Error.NotFound("code", "message");  // implicit: Result.Failure(...)
        }
    }
}
```

**MediatR pipeline order** (applied to every request):
1. `LoggingBehavior` — structured request/response logging
2. `PerformanceBehavior` — warns if over `NexTraceOne:PerformanceThresholdMs` (default 500ms)
3. `TenantIsolationBehavior` — rejects if no tenant unless `IPublicRequest`
4. `ValidationBehavior` — runs all `IValidator<TRequest>` via FluentValidation
5. `TransactionBehavior` — auto-commits `IUnitOfWork` after successful Commands (not Queries)

**Error → HTTP mapping** (via `result.ToHttpResult(localizer)` in endpoints):
`NotFound→404`, `Validation/Business→422`, `Conflict→409`, `Unauthorized→401`, `Forbidden→403`, `Security→500`

---

## Persistence & Tenant Isolation

### EF Core base context

All module DbContexts extend `NexTraceDbContextBase`. This base automatically:
- Converts `AggregateRoot<T>` domain events to `OutboxMessage` rows in the same `SaveChanges`
- Applies global soft-delete filter on all `AuditableEntity<T>` (filters `IsDeleted == false`)
- Applies `[EncryptedField]` convention (AES-256-GCM via `EncryptedStringConverter`)
- Tables are named with a module prefix (e.g., `iam_roles`, `aud_audit_events`) to avoid collisions

### Tenant isolation (two layers)

1. **`TenantRlsInterceptor`** — fires before every SQL command; calls `SELECT set_config('app.current_tenant_id', @id, false)` so PostgreSQL RLS policies can filter rows.
2. **Repository-level filter** — every read method must also add `.Where(e => e.TenantId == currentTenant.Id)` as defense-in-depth. Background jobs (retention, etc.) that run without a tenant context intentionally skip this filter.

### Strongly typed IDs

```csharp
public sealed record MyEntityId(Guid Value) : TypedIdBase(Value)
{
    public static MyEntityId New() => new(Guid.NewGuid());
    public static MyEntityId From(Guid id) => new(id);
}
// ID always needs .Value when comparing to plain Guid:
assets.Where(a => guids.Contains(a.Id.Value))   // ✓
assets.Where(a => guids.Contains(a.Id))          // ✗ compile error
```

### EF entity configuration

Each entity has a corresponding `IEntityTypeConfiguration<T>` in `Infrastructure/Persistence/Configurations/`. Use a module prefix for table names:

```csharp
internal sealed class MyEntityConfiguration : IEntityTypeConfiguration<MyEntity>
{
    public void Configure(EntityTypeBuilder<MyEntity> builder)
    {
        builder.ToTable("mod_my_entities");          // module prefix required
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, value => MyEntityId.From(value));
    }
}
```

`AuditableEntity<T>` subclasses get `CreatedAt/By`, `UpdatedAt/By`, and `IsDeleted` filled automatically by `AuditInterceptor` — no manual assignment needed.

---

## Module DI Registration

Each module's `Infrastructure/DependencyInjection.cs` follows this exact pattern:

```csharp
public static IServiceCollection AddMyModuleInfrastructure(
    this IServiceCollection services, IConfiguration configuration)
{
    services.AddBuildingBlocksInfrastructure(configuration);

    var cs = configuration.GetRequiredConnectionString("MyModuleDatabase", "NexTraceOne");

    services.AddDbContext<MyDbContext>((sp, opts) =>
        opts.UseNpgsql(cs)
            .AddInterceptors(
                sp.GetRequiredService<AuditInterceptor>(),
                sp.GetRequiredService<TenantRlsInterceptor>()));

    services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<MyDbContext>());
    services.AddScoped<IMyModuleUnitOfWork>(sp => sp.GetRequiredService<MyDbContext>());
    services.AddScoped<IMyEntityRepository, MyEntityRepository>();
    // ...
    services.AddScoped<IMyModule, MyModuleService>(); // public cross-module contract
    return services;
}
```

Both `ApiHost/Program.cs` and `BackgroundWorkers/Program.cs` call these extension methods. `ModuleOutboxProcessorJob<TContext>` for each DbContext is registered in `BackgroundWorkers/Program.cs`.

---

## Cross-Module Communication

Modules never access each other's DbContext. Two options:

**1. Integration Events (async, via Outbox)**

```csharp
// Publisher — in a handler, after domain mutation:
await eventBus.PublishAsync(new MyIntegrationEvent(entityId, tenantId), cancellationToken);
// The event is written to OutboxMessage by NexTraceDbContextBase.SaveChanges.
// ModuleOutboxProcessorJob picks it up and delivers to subscribers.

// Subscriber — in consuming module's Infrastructure DI:
services.AddScoped<IIntegrationEventHandler<MyIntegrationEvent>, MyEventHandler>();
```

**2. IXxxModule interface (sync, in-process)**

```csharp
// Defined in: src/modules/catalog/NexTraceOne.Catalog.Contracts/Graph/ServiceInterfaces/ICatalogGraphModule.cs
// Implemented in: NexTraceOne.Catalog.Infrastructure/.../CatalogGraphModuleService.cs
// Registered in: Catalog Infrastructure DI
// Consumed in: other modules by injecting ICatalogGraphModule (not the DbContext)
```

---

## Honest-Null Pattern

`IXxxReader` interfaces (for analytics/reporting read models fed by cross-module data) are intentionally registered with `NullXxxReader` implementations that return empty collections. These are **phase-gated placeholders** — do not treat them as bugs. They allow the product to ship while cross-module bridges are built incrementally.

`IXxxRepository` (CRUD/persistence) interfaces always need a real EF Core implementation. If you see a `NullXxxRepository`, that is a bug.

---

## Adding a New Handler

1. Create `src/modules/<m>/NexTraceOne.<M>.Application/<Subdomain>/Features/<FeatureName>/<FeatureName>.cs` with the static class pattern above.
2. Add a `Validator` class if the command takes user input.
3. If it needs a new repository method, add to the `IXxxRepository` interface (Application layer) and implement in Infrastructure.
4. Register the repository in `Infrastructure/DependencyInjection.cs` if new.
5. Add the endpoint in `API/Endpoints/`.
6. Call `dotnet ef migrations add` if schema changes.

## Adding a New Endpoint

```csharp
internal static class MyEndpoints
{
    internal static void Map(RouteGroupBuilder group)
    {
        var g = group.MapGroup("/my-resource");

        g.MapPost("/", async (MyFeature.Command cmd, ISender sender, IErrorLocalizer l, CancellationToken ct) =>
        {
            var result = await sender.Send(cmd, ct);
            return result.ToCreatedResult($"/my-resource/{result.Value}", l);
        }).RequireAuthorization();
    }
}
```

Public/unauthenticated endpoints add `.AllowAnonymous()` and the Command implements `IPublicRequest`.

---

## Outbox & Background Jobs

`ModuleOutboxProcessorJob<TContext>` runs once per module DbContext in `BackgroundWorkers`. Each cycle:
1. Acquires `pg_try_advisory_lock(key)` — skips if another instance holds it (multi-instance safe)
2. Reads up to 50 `OutboxMessage` rows where `ProcessedAt IS NULL AND RetryCount < 5`
3. Deserializes and publishes each via `IEventBus`
4. Saves `ProcessedAt` atomically per message
5. After 5 failures, moves to Dead Letter Queue via `IDeadLetterRepository`
6. Releases the advisory lock in `finally`

New background jobs go in `src/platform/NexTraceOne.BackgroundWorkers/Jobs/` and are registered as `AddHostedService<MyJob>()` in `BackgroundWorkers/Program.cs`.

---

## SaaS / Licensing

```csharp
// Check capability in a handler:
if (!currentTenant.HasCapability("contract_studio"))
    return Error.Forbidden("CapabilityRequired", "This feature requires the Contract Studio plan.");

// Plans: Starter, Professional, Enterprise, Trial
// Capabilities are embedded in the JWT and read by TenantResolutionMiddleware.
// If no TenantLicense exists → falls back to Enterprise (all capabilities enabled).
```

Available capabilities are defined in `TenantCapabilities.ForPlan(TenantPlan.X)`.

---

## Testing Conventions

- Framework: **xUnit** + **FluentAssertions** + **NSubstitute**
- Use `NSubstitute.ExceptionExtensions` namespace for `.ThrowsAsync()`
- In-memory EF Core (`UseInMemoryDatabase`) for repository tests
- `TestCurrentTenant`, `TestDateTimeProvider` test doubles are available in `tests/modules/identityaccess/TestDoubles/`
- `GlobalUsings.cs` per test project handles common imports (`System`, `FluentAssertions`, `NSubstitute`, `Xunit`)

Handler test pattern — inject substitutes, call `.Handle()` directly, assert `Result`:
```csharp
var handler = new MyFeature.Handler(
    Substitute.For<IMyRepository>(),
    Substitute.For<IUnitOfWork>(),
    new TestDateTimeProvider());

var result = await handler.Handle(new MyFeature.Command(...), CancellationToken.None);

result.IsSuccess.Should().BeTrue();
result.Value.Should().NotBeNull();
```

---

## Language Convention

| Context | Language |
|---|---|
| Code, identifiers, logs, exceptions | English |
| XML doc comments (`/// <summary>`) | Portuguese |
| Inline code comments | Portuguese |
| UI text | i18n keys only — never hardcoded strings |

---

## What Is and Is Not Built

| Feature | Status |
|---|---|
| 24 PostgreSQL databases (one per module) | ✅ Operational |
| Outbox + pg_advisory_lock | ✅ Operational |
| Row-Level Security via TenantRlsInterceptor | ✅ Operational |
| JWT multi-tenant auth + capability claims | ✅ Operational |
| Elasticsearch (observability/analytics) | ✅ Configurable |
| Hot Chocolate GraphQL (catalog + change) | ✅ Operational |
| Kafka | ⚠️ Optional — `NullKafkaEventProducer` by default |
| ClickHouse | ⚠️ Optional — disabled by default |
| Redis / distributed cache | ✅ `IDistributedCache` — Redis when `ConnectionStrings:Redis` set, else in-process fallback |
| HTTP resilience (retry + circuit breaker) | ✅ `AddStandardResilienceHandler()` on all 14 HttpClient registrations |
| `LicenseRecalculationJob` | ✅ Runs every 15 min in BackgroundWorkers — sums active host units per tenant |
| Tenant provisioning automation | ✅ `ProvisionTenant` seeds default roles + access policies after commit |
| Trial plan capabilities | ✅ Professional + 4 Enterprise teasers (not multi_region/air_gapped) |
| Schema-per-tenant (`TenantSchemaManager`) | ❌ Exists but unused |
