# Audit Report — Module: OperationalIntelligence
**Date:** 2026-05-14  
**Branch:** `claude/code-review-audit-i0rFs`  
**Scope:** `src/modules/operationalintelligence/` · `tests/modules/operationalintelligence/`  
**Analyst:** Automated Code Review  

---

## Executive Summary

The OperationalIntelligence module is the largest in the platform (549 C# files, 113 test files) and covers 7 sub-contexts: Automation, Cost, FinOps, Incidents, Reliability, Runtime, and TelemetryStore. It correctly implements the ClickHouse/Elasticsearch dual-backend pattern for log search via `ITelemetrySearchService`. However, critical multi-tenant isolation gaps exist across multiple sub-contexts, a runtime-breaking SQL incompatibility was found in the ClickHouse log search path, and the analytics endpoints expose cross-tenant ClickHouse data. The frontend is almost entirely absent.

**Issue Summary:** 4 P0 · 6 P1 · 6 P2 · 3 P3 = **19 total issues**

---

## Issues

### P0 — Critical

---

#### OI-001 · ClickHouseLogSearchService missing tenant filter — cross-tenant log exposure
**File:** `Infrastructure/Runtime/Services/ClickHouseLogSearchService.cs:49-108`  
**Severity:** P0 — Data exposure  

`ClickHouseLogSearchService.SearchAsync` builds a WHERE clause from `from`, `to`, `service_name`, `severity`, `environment`, and `message` — but **never filters by `tenant_id`**. The `LogSearchRequest` includes a `TenantId` field (passed from `currentTenant.Id` in the handler at `SearchLogs.cs:84`), but `ClickHouseLogSearchService` ignores it entirely.

`ElasticsearchLogSearchService` (the alternative backend) correctly adds `term: { tenant_id: ... }` to its bool/must filter (line 207). The ClickHouse path is missing the equivalent.

**Impact:** When operators configure `BackendType: clickhouse`, every tenant can search and read every other tenant's log entries. This is a complete tenant isolation failure for the log search feature.

**Correction:**
1. Add `tenant_id` column to the ClickHouse `logs` table schema.
2. In `ClickHouseLogSearchService.SearchAsync`, add to `whereClauses`:
   ```csharp
   whereClauses.Add("tenant_id = @TenantId");
   parameters["@TenantId"] = request.TenantId.ToString();
   ```
3. In `IndexLogAsync`, include `tenant_id` in the INSERT statement.
4. Add a composite index `(tenant_id, timestamp)` to the ClickHouse schema.

---

#### OI-002 · ClickHouseRepository analytics queries have no tenant filter — cross-tenant metric exposure
**File:** `Infrastructure/Runtime/Persistence/ClickHouse/ClickHouseRepository.cs:90-324`  
**Severity:** P0 — Data exposure  

All 7 analytics methods (`GetRequestMetricsAsync`, `GetErrorAnalyticsAsync`, `GetUserActivityAsync`, `GetSystemHealthAsync`, `GetAverageResponseTimeAsync`, `GetTotalRequestsAsync`, `GetErrorRateAsync`) query the ClickHouse `events` table with only a time range filter. No `tenant_id` condition exists in any of these queries.

The 5 API endpoints at `RuntimeIntelligenceEndpointModule.cs:301-422` inject `IClickHouseRepository` directly and pass it the request's `from`/`to` parameters. There is no tenant context passed to the repository.

**Impact:** Any authenticated user (regardless of tenant) can access observability metrics for every service across all tenants. Latency, error rates, user activity and system health data leak across tenant boundaries.

**Correction:**
1. Add `tenant_id` field to the ClickHouse `events` table.
2. Add `Guid tenantId` parameter to all `IClickHouseRepository` method signatures.
3. Inject `ICurrentTenant` in the endpoint handlers and pass `currentTenant.Id` to the repository calls.
4. Add `WHERE tenant_id = @TenantId` to every query.

---

#### OI-003 · MitigationWorkflowRecord has no TenantId — cross-tenant workflow access
**File:** `Domain/Incidents/Entities/MitigationWorkflowRecord.cs:1-135`  
**Repository:** `Infrastructure/Incidents/Persistence/Repositories/EfMitigationWorkflowRepository.cs:28-35`  
**Severity:** P0 — Data exposure  

`MitigationWorkflowRecord` extends `AuditableEntity<MitigationWorkflowRecordId>` and has no `TenantId` property. The repository's `GetByIncidentIdAsync` method queries solely by `IncidentId` (a string) with no tenant filter. RLS via `TenantRlsInterceptor` requires the entity to carry a `TenantId` that maps to the PostgreSQL session variable — without it, RLS cannot enforce row-level isolation.

**Impact:** A user in tenant A can retrieve mitigation workflows belonging to tenant B by knowing or guessing an `IncidentId`.

**Correction:**
1. Add `public Guid TenantId { get; private set; }` to `MitigationWorkflowRecord`.
2. Add `tenantId` parameter to the `Create` factory method.
3. Add `.Where(w => w.TenantId == tenantId)` in `GetByIncidentIdAsync`.
4. Add `builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired()` to EF configuration.
5. Add `dotnet ef migrations add AddTenantIdToMitigationWorkflow`.

---

#### OI-004 · AutomationWorkflowRecord has no TenantId — cross-tenant automation access
**File:** `Domain/Automation/Entities/AutomationWorkflowRecord.cs:1-129`  
**Repository:** `Infrastructure/Automation/Persistence/Repositories/AutomationRepositories.cs` (AutomationWorkflowRepository)  
**Severity:** P0 — Data exposure  

`AutomationWorkflowRecord` extends `Entity<AutomationWorkflowRecordId>` with no `TenantId` field. `AutomationWorkflowRepository.ListAsync` filters only by `serviceId` and `status`, and `GetByIdAsync` has no tenant filter at all.

The EF configuration (`AutomationWorkflowRecordConfiguration.cs`) has no `tenant_id` column mapping. RLS cannot enforce isolation.

**Impact:** Automation workflows — including approvals, risk levels, and operational scope — are visible across tenant boundaries.

**Correction:**
1. Add `public Guid TenantId { get; private init; }` to `AutomationWorkflowRecord`.
2. Pass `tenantId` in the `Create` factory method.
3. Add `WHERE TenantId == tenantId` to all repository query methods.
4. Add the `tenant_id` column to the EF configuration.
5. Add migration.

---

### P1 — High

---

#### OI-005 · ClickHouseLogSearchService uses ILIKE — not supported in ClickHouse SQL
**File:** `Infrastructure/Runtime/Services/ClickHouseLogSearchService.cs:81`  
**Severity:** P1 — Runtime bug  

```csharp
whereClauses.Add("message ILIKE @SearchText");
```

`ILIKE` is a PostgreSQL extension and is **not valid ClickHouse SQL**. ClickHouse's SQL dialect uses `ilike(column, pattern)` as a function, or `lower(column) LIKE lower(pattern)`. Using `ILIKE` will throw a SQL parse error at runtime when any user performs a log search with the ClickHouse backend.

**Correction:**
```csharp
// Option A — ClickHouse ilike() function:
whereClauses.Add("ilike(message, @SearchText)");

// Option B — case-insensitive via lower():
whereClauses.Add("lower(message) LIKE lower(@SearchText)");
```

---

#### OI-006 · CostRecord has no TenantId — cost data readable across tenants
**File:** `Domain/Cost/Entities/CostRecord.cs:1-139`  
**Repository:** `Infrastructure/Cost/Persistence/Repositories/CostRecordRepository.cs:1-78`  
**Severity:** P1 — Data exposure  

`CostRecord` extends `AuditableEntity<CostRecordId>` with no `TenantId` field. All repository query methods (`ListByPeriodAsync`, `ListByServiceAsync`, `ListByTeamAsync`, `ListByDomainAsync`, `ListByReleaseAsync`) filter by business dimensions only — never by tenant. Note that `CarbonScoreRecord` (a sibling entity in the same sub-context) correctly has `TenantId` and the repository filters by it.

**Impact:** Cloud cost data (service, team, domain) is visible across tenant boundaries. A tenant could see a competitor's cost breakdown.

**Correction:**
1. Add `public Guid TenantId { get; private set; }` to `CostRecord`.
2. Add `tenantId` to the `Create` factory method signature.
3. Add `.Where(r => r.TenantId == tenantId)` to all repository methods.
4. Update EF configuration with the `tenant_id` column.
5. Add migration. Reference `CarbonScoreRecord` as the pattern to follow.

---

#### OI-007 · IncidentRecord.TenantId domain/persistence contradiction
**File:** `Domain/Incidents/Entities/IncidentRecord.cs:146`  
**EF Config:** `Infrastructure/Incidents/Persistence/Configurations/IncidentRecordConfiguration.cs:34`  
**Severity:** P1 — Data integrity  

The domain entity declares:
```csharp
// "Nullable por retrocompatibilidade"
public Guid? TenantId { get; private set; }
```

But the EF configuration maps it as:
```csharp
builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
```

`IsRequired()` maps to `NOT NULL` in PostgreSQL. EF Core will throw a `DbUpdateException` when trying to save an `IncidentRecord` with a null `TenantId`. The "backward compatibility" comment in the domain model is a design debt that creates a false expectation that null is allowed. Additionally, `SetTenantContext` uses `??=` semantics ("only set if null"), meaning a once-set TenantId can never be corrected.

**Correction:**
1. Remove the "retrocompatibilidade" nullable qualifier — make `TenantId` non-nullable: `public Guid TenantId { get; private set; }`.
2. Require `tenantId` in the `Create` factory method.
3. Remove `SetTenantContext` (which allowed post-creation patching) and enforce it at construction.
4. Run a data migration to backfill any legacy rows that have `NULL` in `tenant_id`.

---

#### OI-008 · Repositories call SaveChangesAsync directly — double-commit with TransactionBehavior
**Files:**  
- `Infrastructure/Incidents/Persistence/Repositories/EfMitigationWorkflowRepository.cs:19`  
- `Infrastructure/Incidents/Persistence/Repositories/EfIncidentCorrelationRepository.cs:24,32`  
- `Infrastructure/Reliability/Persistence/Repositories/SloDefinitionRepository.cs:30`  
**Severity:** P1 — Reliability  

Several repositories call `db.SaveChangesAsync()` or `context.CommitAsync()` inside individual repository methods:

```csharp
// EfMitigationWorkflowRepository.AddAsync
await db.SaveChangesAsync(cancellationToken);

// EfIncidentCorrelationRepository.AddAsync
await context.CommitAsync(cancellationToken);

// SloDefinitionRepository.AddAsync
await context.CommitAsync(ct);
```

When these methods are called from a CQRS handler, `TransactionBehavior` calls `IUnitOfWork.CommitAsync()` again after the handler returns successfully. This results in a second, empty `SaveChanges` call and undermines the atomic-per-command guarantee. It also prevents a handler from batching multiple operations atomically.

**Correction:**
Remove all direct `SaveChangesAsync`/`CommitAsync` calls from repository `AddAsync` methods. Repositories should only stage changes (e.g., `context.Set<T>().Add(entity)`). The `TransactionBehavior` in the MediatR pipeline owns the commit.

---

#### OI-009 · InsertEventsBatchAsync is sequential N+1 — not a real batch insert
**File:** `Infrastructure/Runtime/Persistence/ClickHouse/ClickHouseRepository.cs:70-84`  
**Severity:** P1 — Performance  

```csharp
public async Task InsertEventsBatchAsync(IEnumerable<ClickHouseEvent> events)
{
    // ...
    foreach (var evt in eventList)
    {
        await InsertEventAsync(evt);  // opens a new connection per event!
    }
}
```

`InsertEventsBatchAsync` is documented as using "bulk insert for alta performance", but it iterates through events sequentially, calling `InsertEventAsync` for each one. Every call opens a new `ClickHouseConnection`, executes a single-row INSERT, and disposes the connection. For 1000 events, this makes 1000 round-trips.

**Correction:**
Use `ClickHouse.Client`'s bulk insert API via `IClickHouseConnection.CreateColumnWriter()` or construct a multi-row VALUES string:
```csharp
using var writer = await connection.CreateColumnWriterAsync(
    "INSERT INTO events (timestamp, event_id, ...) FORMAT Values", cancellationToken);
// write all rows via the column writer
```
Alternatively, construct a single INSERT with multiple row values using parameterized tuples, or use the `ClickHouseBulkCopy` helper.

---

#### OI-010 · Analytics endpoints hardcoded to ClickHouseRepository — no Elasticsearch alternative
**File:** `API/Runtime/Endpoints/Endpoints/RuntimeIntelligenceEndpointModule.cs:299-422`  
**Severity:** P1 — Design inconsistency  

The 5 observability analytics endpoints (`/request-metrics`, `/error-analytics`, `/user-activity`, `/system-health`, `/stats`) inject `IClickHouseRepository` directly. When the operator configures `BackendType: elasticsearch`, these endpoints still require ClickHouse and will fail or return empty data because `IClickHouseRepository` has no Elasticsearch implementation.

The `ITelemetrySearchService` abstraction (which supports both backends) is not used here. This creates a two-tier system where log search supports both backends but metric analytics only works with ClickHouse.

**Correction:**
1. Extend `ITelemetrySearchService` with analytics methods (or create `IObservabilityAnalyticsService`).
2. Provide an `ElasticsearchObservabilityAnalyticsService` implementing the interface via Elasticsearch aggregations.
3. Register via the same `backendType` switch in DI.
4. Replace direct `IClickHouseRepository` injection in endpoints with the new abstraction.

---

### P2 — Medium

---

#### OI-011 · RowVersion public setter on multiple domain entities
**Files:**  
- `Domain/Incidents/Entities/IncidentRecord.cs:137` — `public uint RowVersion { get; set; }`  
- `Domain/Automation/Entities/AutomationWorkflowRecord.cs:65` — `public uint RowVersion { get; set; }`  
- `Domain/Reliability/Entities/SloDefinition.cs:57` — `public uint RowVersion { get; set; }`  
**Severity:** P2 — Encapsulation  

All three entities expose `RowVersion` with a public setter, allowing application code to mutate the concurrency token directly — undermining its purpose. EF Core's `IsRowVersion()` mapping reads the `xmin` column from PostgreSQL automatically; the setter must not be public.

**Correction:**
```csharp
// Change from:
public uint RowVersion { get; set; }
// To:
public uint RowVersion { get; private set; }
```
Apply to all affected entities. Grep for `RowVersion { get; set; }` across the module to find all occurrences.

---

#### OI-012 · IncidentRecord: 13 JSONB columns with no GIN indexes
**File:** `Infrastructure/Incidents/Persistence/Configurations/IncidentRecordConfiguration.cs:51-62`  
**Severity:** P2 — Performance / queryability  

`IncidentRecord` stores 13 collections as `jsonb` columns (`TimelineJson`, `LinkedServicesJson`, `CorrelatedChangesJson`, `CorrelatedServicesJson`, etc.). The EF configuration defines standard B-tree indexes on `ServiceId`, `Status`, `Severity`, `DetectedAt`, and `TenantId`, but **no GIN indexes** on any of the JSONB columns.

Any query that filters or searches within these JSON blobs (e.g., find incidents correlated to a specific change) requires a full table scan. Without GIN indexes, JSON containment queries (`@>`, `?`) cannot use an index.

**Correction:**
1. For columns likely to be queried (`CorrelatedChangesJson`, `LinkedServicesJson`, `ImpactedContractsJson`), add GIN indexes in the EF configuration:
   ```csharp
   builder.HasIndex(x => x.CorrelatedChangesJson)
       .HasMethod("gin")
       .HasDatabaseName("ix_ops_incidents_correlated_changes_gin");
   ```
2. Alternatively, refactor these into owned child entities or a separate child table for collections that need to be queried individually (e.g., `IncidentCorrelatedChange`).

---

#### OI-013 · IClickHouseRepository registered as Singleton with `new` — bypasses DI lifecycle
**File:** `Infrastructure/Runtime/DependencyInjection.cs:125-127`  
**Severity:** P2 — Design  

```csharp
var clickHouseConnectionString = configuration.GetConnectionString("ClickHouse") 
    ?? "http://localhost:8123/default";
services.AddSingleton<IClickHouseRepository>(new ClickHouseRepository(clickHouseConnectionString));
```

The repository is instantiated directly with `new`, hardcoding the localhost fallback URL into the running service. Issues:
1. The fallback `"http://localhost:8123/default"` will silently succeed in DI registration even if ClickHouse is not configured, causing runtime failures at the first analytics query.
2. Creating with `new` bypasses any DI-resolved logging, configuration validation, or factory injection.
3. Inconsistency: all other repositories use `services.AddScoped<>()`.

**Correction:**
```csharp
// Throw if ClickHouse is required but not configured:
var clickHouseCs = configuration.GetConnectionString("ClickHouse")
    ?? throw new InvalidOperationException("ClickHouse connection string 'ClickHouse' is required.");
services.AddSingleton<IClickHouseRepository>(_ => new ClickHouseRepository(clickHouseCs));
```
Or conditionally register only when configured, similar to how `ITelemetrySearchService` selects its backend.

---

#### OI-014 · /observability/stats makes 3 sequential ClickHouse round-trips
**File:** `API/Runtime/Endpoints/Endpoints/RuntimeIntelligenceEndpointModule.cs:395-422`  
**Severity:** P2 — Performance  

```csharp
var avgResponseTime = await repository.GetAverageResponseTimeAsync(from, to, endpoint);
var totalRequests = await repository.GetTotalRequestsAsync(from, to);
var errorRate = await repository.GetErrorRateAsync(from, to);
```

Three sequential ClickHouse connections and queries open for the same time range. This can be replaced with a single aggregation query:

```sql
SELECT 
    avg(duration_ms)                          AS AvgResponseTime,
    count()                                   AS TotalRequests,
    round(countIf(status_code >= 400) / count() * 100, 2) AS ErrorRate
FROM events
WHERE timestamp >= @from AND timestamp <= @to
```

**Correction:** Add `GetSummaryStatsAsync(DateTime from, DateTime to, string? endpoint)` to `IClickHouseRepository` returning a composite record, and use it in the stats endpoint.

---

#### OI-015 · Both ClickHouse.Client and ClickHouse.Driver packages referenced; only Client used
**File:** `NexTraceOne.OperationalIntelligence.Infrastructure.csproj:24-25`  
**Severity:** P2 — Dead dependency  

```xml
<PackageReference Include="ClickHouse.Client" />   <!-- used: ClickHouse.Client.ADO namespace -->
<PackageReference Include="ClickHouse.Driver" />   <!-- unused: no references found -->
```

`ClickHouse.Driver` (v1.2.0) is referenced but has zero usages in the codebase. These two packages have overlapping functionality and different ADO.NET implementations. Having both creates dependency bloat and risks version conflicts.

**Correction:** Remove `<PackageReference Include="ClickHouse.Driver" />` from the project file. Verify via `dotnet build` that no compilation errors arise.

---

#### OI-016 · Frontend coverage: 2 files for 549 C# files — module is backend-only
**Files:** `src/frontend/src/modules/operational-intelligence/` (2 files)  
**Severity:** P2 — Product completeness  

The OperationalIntelligence module has:
- 549 backend C# files covering 7 sub-contexts
- Only 2 frontend TypeScript files: `index.ts` and `OperationsFinOpsConfigurationPage.tsx`

The frontend E2E tests reference incidents, FinOps, and governance flows (`incident-business-flows.spec.ts`, `governance-finops.spec.ts`) but these appear to test external pages rather than dedicated OI module pages. The incident management UI, runtime intelligence dashboards, SLO management, reliability reports, and automation workflows have no corresponding React components.

**Correction:**
Create dedicated page components for:
- `/operations/incidents` — Incident list + detail + mitigation workflow
- `/operations/reliability` — SLO/SLA management + error budget dashboard
- `/operations/runtime` — Observability dashboards (latency, error rate, system health)
- `/operations/cost` — Cost management + FinOps + budget forecasts
- `/operations/automation` — Automation workflow management

---

### P3 — Low

---

#### OI-017 · ClickHouseRepository.Dispose does not hold nor dispose any resource
**File:** `Infrastructure/Runtime/Persistence/ClickHouse/ClickHouseRepository.cs:337-344`  
**Severity:** P3 — Code quality  

```csharp
public void Dispose()
{
    if (!_disposed)
    {
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
```

`ClickHouseRepository` implements `IDisposable` and is registered as a Singleton. Each method creates its own `ClickHouseConnection` and disposes it with `await using`. The class itself holds no unmanaged resources. The `IDisposable` implementation is a no-op and misleads readers into thinking there are resources to release.

**Correction:** Remove the `IDisposable` implementation from `ClickHouseRepository`.

---

#### OI-018 · AutomationRepositories.cs: 3 repository classes in one file
**File:** `Infrastructure/Automation/Persistence/Repositories/AutomationRepositories.cs`  
**Severity:** P3 — Maintainability  

`AutomationRepositories.cs` contains `AutomationWorkflowRepository`, `AutomationValidationRepository`, and `AutomationAuditRepository` in a single file. This violates the single-responsibility file convention used throughout the rest of the platform (one class per file).

**Correction:** Split into `AutomationWorkflowRepository.cs`, `AutomationValidationRepository.cs`, and `AutomationAuditRepository.cs`.

---

#### OI-019 · Eight null readers registered across Runtime DI — signal of incomplete cross-module bridges
**File:** `Infrastructure/Runtime/DependencyInjection.cs:63-90`  
**Severity:** P3 — Observability / technical debt tracking  

Eight `IXxxReader` interfaces are registered with null implementations:
- `IActiveServiceNamesReader` → `NullActiveServiceNamesReader`
- `ITeamOperationalMetricsReader` → `NullTeamOperationalMetricsReader`
- `IVulnerabilityAdvisoryReader` → `NullVulnerabilityAdvisoryReader`
- `IIncidentKnowledgeReader` → `NullIncidentKnowledgeReader`
- `IPlatformAdoptionReader` → `NullPlatformAdoptionReader`
- `IDeploymentRiskForecastReader` → `NullDeploymentRiskForecastReader`
- `IErrorBudgetReader` → `NullErrorBudgetReader`
- `IIncidentImpactScorecardReader` → `NullIncidentImpactScorecardReader`

Per CLAUDE.md, this is the intended "honest-null" pattern for cross-module readers. However, 8 null readers in a single DI registration file indicates a large surface of incomplete cross-module bridges. These should be tracked as explicit work items (Wave labels visible in comments are correct).

**Recommendation:** Add a structured tech-debt tracking comment block that lists each null reader, its wave, and its source module, to make the backlog visible to contributors.

---

## Architecture Assessment

### What Works Well

| Area | Status |
|---|---|
| `ITelemetrySearchService` dual-backend abstraction (ClickHouse / Elasticsearch) | ✅ Correct pattern |
| `TelemetryStoreOptions` config-driven backend selection | ✅ Correct |
| `IncidentDbContext`, `ReliabilityDbContext`, `AutomationDbContext` — separated per sub-context | ✅ Correct |
| `SloDefinition`, `CarbonScoreRecord`, `SloObservation` — TenantId correctly included | ✅ Correct |
| `SloDefinitionRepository` — all queries filter by tenant | ✅ Correct |
| `ElasticsearchLogSearchService` — filters by `tenant_id` in DSL | ✅ Correct |
| `IncidentChangeCorrelation` — correct use of typed IDs | ✅ Correct |
| 113 test files covering predictive, incident, cost, and reliability sub-contexts | ✅ Good coverage |

### ClickHouse vs Elasticsearch Database Placement

The placement decision is correct:

| Data Type | Backend | Justification |
|---|---|---|
| Incidents, SLOs, Reliability, Cost records | PostgreSQL (EF Core) | Transactional, requires ACID, tenant RLS |
| Observability events (metrics, traces, logs) | ClickHouse or Elasticsearch | Append-only, high-cardinality, time-series analytics |
| Log search | ITelemetrySearchService (either) | Correctly abstracted; user installs one |
| Observability analytics (latency, error rate) | Must also be abstracted (currently ClickHouse-only) | Gap identified in OI-010 |

---

## Priority Correction Plan

| Priority | Issue | Effort |
|---|---|---|
| P0 | OI-001 — Add tenant_id filter to ClickHouseLogSearchService | 1h |
| P0 | OI-002 — Add tenant_id to IClickHouseRepository analytics methods | 2h |
| P0 | OI-003 — Add TenantId to MitigationWorkflowRecord + repository + migration | 2h |
| P0 | OI-004 — Add TenantId to AutomationWorkflowRecord + repository + migration | 2h |
| P1 | OI-005 — Replace ILIKE with ClickHouse-compatible ilike() | 30min |
| P1 | OI-006 — Add TenantId to CostRecord + repository + migration | 2h |
| P1 | OI-007 — Resolve IncidentRecord.TenantId nullable/required contradiction | 1h + data migration |
| P1 | OI-008 — Remove direct SaveChangesAsync from repository methods | 1h |
| P1 | OI-009 — Implement real batch insert in InsertEventsBatchAsync | 2h |
| P1 | OI-010 — Abstract analytics behind ITelemetrySearchService | 4h |
| P2 | OI-011 — Fix RowVersion public setters on 3 entities | 30min |
| P2 | OI-012 — Add GIN indexes to JSONB columns on IncidentRecord | 1h |
| P2 | OI-013 — Fix IClickHouseRepository singleton registration | 30min |
| P2 | OI-014 — Consolidate /stats into single ClickHouse query | 1h |
| P2 | OI-015 — Remove unused ClickHouse.Driver package | 15min |
| P2 | OI-016 — Create OI frontend pages | 3–5 days |
| P3 | OI-017 — Remove no-op IDisposable from ClickHouseRepository | 15min |
| P3 | OI-018 — Split AutomationRepositories.cs into 3 files | 30min |
| P3 | OI-019 — Add tech-debt tracking comment block for null readers | 15min |
