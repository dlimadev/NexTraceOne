# Phase 3 — Reliability Completion Report

## Executive Summary

Before Phase 3, the NexTraceOne Reliability module was a cosmetic prototype:
- All backend handlers returned `IsSimulated: true` with hardcoded data
- The frontend pages (`TeamReliabilityPage`, `ServiceReliabilityDetailPage`) used local mock arrays
- A "Demo Data" banner was visible in the UI, explicitly admitting the data was fake
- No persistence existed for reliability snapshots or trends
- The module was estimated at ~25% completion (backend) and ~20% (frontend)

After Phase 3, Reliability is a functional module:
- All 7 backend handlers use real data via surface interfaces
- A dedicated `ReliabilityDbContext` with `ReliabilitySnapshot` entity exists
- The frontend connects to the real API via React Query
- No mock data, demo banners, or simulated flags remain
- Scoring is deterministic, documented, and auditable

## Architectural Decision: ReliabilityDbContext

**Decision**: Dedicated `ReliabilityDbContext` (not an extension of `RuntimeIntelligenceDbContext`)

**Rationale**:
- Reliability has its own lifecycle: computed snapshots, historical trends, temporal analysis
- Score aggregates data from Runtime + Incidents (not purely Runtime-derived)
- Separate context keeps module boundaries clean — consistent with `CostIntelligenceDbContext`
- Avoids mixing reliability history with runtime snapshot lifecycle

**Database**: `nextraceone_operations` (same physical DB, different context)  
**Connection String Key**: `ReliabilityDatabase`

## Backend Implemented

### New Infrastructure
- `ReliabilitySnapshot` domain entity (strongly-typed ID, audit fields, TenantId)
- `ReliabilityDbContext` with `oi_reliability_snapshots` table
- `IReliabilityRuntimeSurface` + `ReliabilityRuntimeSurface` (reads RuntimeIntelligenceDbContext)
- `IReliabilityIncidentSurface` + `ReliabilityIncidentSurface` (reads IncidentDbContext)
- `IReliabilitySnapshotRepository` + `ReliabilitySnapshotRepository`
- `AddReliabilityModule` DI extension registered in `Program.cs`

### Handlers Rewritten (all 7)
1. `ListServiceReliability` — real data from runtime + incidents
2. `GetServiceReliabilityDetail` — real drilldown per service
3. `GetTeamReliabilitySummary` — real aggregation from incident surface
4. `GetDomainReliabilitySummary` — real aggregation from incident surface
5. `GetServiceReliabilityTrend` — real history from ReliabilitySnapshotRepository
6. `GetTeamReliabilityTrend` — real incident-based trend
7. `GetServiceReliabilityCoverage` — real signal/runbook/incident checks

### Removed
- `GenerateSimulatedItems()` from `ListServiceReliability`
- `BuildSimulatedResponse()` switch/case from `GetServiceReliabilityDetail`
- `IsSimulated: true` from all Response records
- `DataSource: "demo"` from all Response records

## Persistence and Migrations

### Entity: ReliabilitySnapshot
- Table: `oi_reliability_snapshots`
- Key fields: `TenantId`, `ServiceId`, `Environment`, `OverallScore`, `RuntimeHealthScore`, `IncidentImpactScore`, `ObservabilityScore`, `OpenIncidentCount`, `TrendDirection`, `ComputedAt`
- Indexes: `(TenantId, ServiceId, ComputedAt)` and `(TenantId, ComputedAt)`

### Migrations Created
- `20260322015633_InitialCreate` — creates `oi_reliability_snapshots` table

### Snapshot Purpose
Snapshots enable:
- Historical trend calculation (compare current to previous score)
- Time-series data for trend charts
- Temporal comparison and future analysis

## Reliability Score Formula

```
OverallScore = (RuntimeHealthScore × 0.50) + (IncidentImpactScore × 0.30) + (ObservabilityScore × 0.20)
```

| Component | Source | Weight |
|-----------|--------|--------|
| RuntimeHealthScore | Latest RuntimeSnapshot → HealthStatus | 50% |
| IncidentImpactScore | Active incidents in last 30 days, weighted by severity | 30% |
| ObservabilityScore | ObservabilityProfile.Score × 100, or 50 if none | 20% |

See `RELIABILITY-SCORING-MODEL.md` for full documentation.

## Cross-Module Integrations

| Module | Surface | Data Used |
|--------|---------|-----------|
| RuntimeIntelligence | `IReliabilityRuntimeSurface` | HealthStatus, ErrorRate, P99Latency, ObservabilityScore |
| Incidents | `IReliabilityIncidentSurface` | Active incidents, runbook coverage, team/domain |
| ChangeGovernance | Not integrated (deferred) | N/A |
| Catalog | Not integrated (deferred) | N/A |

## Frontend Corrected

| Page | Before | After |
|------|--------|-------|
| `TeamReliabilityPage.tsx` | `mockServices` local array, `DemoBanner` | `useQuery` → `reliabilityApi.listServices()` |
| `ServiceReliabilityDetailPage.tsx` | `mockDetails` Record, `DemoBanner` | `useQuery` → `reliabilityApi.getServiceDetail(serviceId)` |

## Tests Added/Updated

### Backend (OI module, 288 total — 5 new)
- `SimulatedDataHonestyTests.cs` — rewritten to verify real surface calls (not `IsSimulated`)
- `ReliabilityFeatureTests.cs` — rewritten with NSubstitute mocks for all surfaces

### Frontend (16 new tests)
- `TeamReliabilityPage.test.tsx` — 8 tests: API call, rendering, no DemoBanner, loading/error states
- `ServiceReliabilityDetailPage.test.tsx` — 8 tests: API call, rendering, no DemoBanner, 404 handling

## Limitations Remaining

1. **Service Catalog not integrated**: Services are discovered from RuntimeSnapshot + Incident data only.
   Services not yet ingesting runtime data or with no incidents will not appear.

2. **ChangeGovernance not integrated**: `RecentChanges` in `GetServiceReliabilityDetail` returns empty
   because ChangeGovernance surface integration is deferred.

3. **Dependencies not mapped**: The `Dependencies` section returns empty because dependency
   topology mapping is a future capability.

4. **Contracts not linked**: `LinkedContracts` returns empty — contract-to-service linkage
   is planned for a future Contract Governance integration phase.

5. **MTTR/SLO not computed**: These metrics require SLO definitions and resolved incident
   history. Omitted (not faked) until the data is available.

6. **Snapshot population**: `ReliabilitySnapshot` records will only populate when the platform
   has active runtime data or incident records. A background job to periodically compute and
   store snapshots is recommended for future phases.

## Next Steps Recommended

1. **Background worker**: Implement a periodic job to compute and persist `ReliabilitySnapshot`
   for all known services, enabling historical trends from day one.

2. **Catalog integration**: Connect to the Service Catalog to derive `teamName`, `domain`,
   and `criticality` from authoritative service metadata.

3. **ChangeGovernance surface**: Create `IReliabilityChangeGovernanceSurface` to include
   recent releases and their risk/impact in the reliability score.

4. **Dependency mapping**: Implement topology-aware dependency status in `GetServiceReliabilityDetail`.

5. **SLO profiles**: Add `ServiceSloProfile` entity for SLO definition and compliance tracking.
