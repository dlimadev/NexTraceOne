# Reliability Data Model

## Architectural Decision: Dedicated ReliabilityDbContext

### Decision
A dedicated `ReliabilityDbContext` was created in the
`NexTraceOne.OperationalIntelligence.Infrastructure` project, pointing to the
`nextraceone_operations` database (connection string key: `ReliabilityDatabase`).

### Rationale for Dedicated Context
- Reliability has its own lifecycle: computed snapshots, historical trends, temporal queries
- Isolating the context keeps the module boundary clean for future growth
- Consistent with how `CostIntelligenceDbContext` and `IncidentDbContext` are structured
- Allows dedicated migrations without affecting Runtime or Incident schemas

### Why Not RuntimeIntelligenceDbContext Extension?
Extending `RuntimeIntelligenceDbContext` would tightly couple the reliability
snapshot lifecycle to the runtime snapshot pipeline. Reliability aggregates data
from multiple sources (Runtime + Incidents) and has its own persistence purpose.

## Entity: ReliabilitySnapshot

**Table**: `oi_reliability_snapshots`

| Column | Type | Purpose |
|--------|------|---------|
| `Id` | UUID (PK) | Strongly-typed `ReliabilitySnapshotId` |
| `TenantId` | UUID | Explicit tenant isolation for queries |
| `ServiceId` | varchar(200) | Service identifier (matches ServiceName in Runtime) |
| `Environment` | varchar(100) | Environment name where score was computed |
| `OverallScore` | decimal | Composite reliability score (0–100) |
| `RuntimeHealthScore` | decimal | Score component from RuntimeSnapshot |
| `IncidentImpactScore` | decimal | Score component from IncidentRecord |
| `ObservabilityScore` | decimal | Score component from ObservabilityProfile |
| `OpenIncidentCount` | int | Number of active incidents at compute time |
| `RuntimeHealthStatus` | varchar(50) | Raw HealthStatus string at compute time |
| `TrendDirection` | int | `TrendDirection` enum value |
| `ComputedAt` | timestamptz | UTC timestamp when score was computed |
| `CreatedAt` / `UpdatedAt` | timestamptz | Audit fields (AuditInterceptor) |

### Indexes
- `(TenantId, ServiceId, ComputedAt)` — history queries for a specific service
- `(TenantId, ComputedAt)` — all services for a tenant in a time window

## Cross-Module Surface Interfaces

Reliability does not access foreign DbContexts directly.
Instead, it uses surface interfaces defined in the Application layer and implemented
in Infrastructure within the same module:

### IReliabilityRuntimeSurface
- Queries `RuntimeIntelligenceDbContext` for latest `RuntimeSnapshot` per service
- Queries `ObservabilityProfile` for observability scores
- Implemented by `ReliabilityRuntimeSurface`

### IReliabilityIncidentSurface
- Queries `IncidentDbContext` for active incidents by service and team
- Queries `RunbookRecord` for runbook coverage
- Implemented by `ReliabilityIncidentSurface`

### IReliabilitySnapshotRepository
- Queries and persists `ReliabilitySnapshot` entities
- Implemented by `ReliabilitySnapshotRepository`

## Service Discovery Strategy

Since the Service Catalog is not yet fully populated, services are discovered dynamically
from the union of:
1. Distinct `ServiceName` values in `oi_runtime_snapshots` (via `IReliabilityRuntimeSurface`)
2. Distinct `ServiceId` values in `oi_incidents` (via `IReliabilityIncidentSurface`)

This is an honest approach: only services with actual operational data are listed.
