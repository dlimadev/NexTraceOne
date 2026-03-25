# Operational Intelligence — ClickHouse Data Placement Review (Part 6)

> **Status:** DRAFT  
> **Date:** 2026-03-25  
> **Module:** 06 — Operational Intelligence  
> **Phase:** B1 — Module Consolidation  
> **Reference:** `docs/architecture/module-data-placement-matrix.md` (APPROVED, 2026-03-24)  
> **ClickHouse Level:** **RECOMMENDED**

---

## 1. Decision Summary

Per the approved `module-data-placement-matrix.md`, the Operational Intelligence module is classified as **RECOMMENDED** for ClickHouse integration. This document details which data stays in PostgreSQL, which data should migrate to ClickHouse, and which data must never move.

| Attribute | Value |
|-----------|-------|
| **ClickHouse Level** | RECOMMENDED |
| **Justification** | Runtime metrics, cost analytics, and telemetry data are natural fits for columnar analytical storage |
| **Current State** | ❌ No ClickHouse integration exists — all data in PostgreSQL |
| **Target State** | Analytical/time-series workloads offloaded to ClickHouse; domain state remains in PostgreSQL |

---

## 2. Data That Stays in PostgreSQL (Domain Entities)

All **domain state, workflow state machines, configurations, and reference data** remain in PostgreSQL. These require ACID guarantees, referential integrity, RLS enforcement, and transactional consistency.

### 2.1 Incidents Subdomain — All in PostgreSQL

| Table | Reason |
|---|---|
| `oi_incidents` | Active incident lifecycle state machine (Open → Closed); requires ACID for concurrent status updates, correlation refresh, mitigation coordination |
| `oi_mitigation_workflows` | Workflow state machine with approval gates (Draft → Completed); requires transactional consistency for state transitions |
| `oi_mitigation_workflow_actions` | Immutable action log linked to workflows; requires FK integrity with `oi_mitigation_workflows` |
| `oi_mitigation_validations` | Post-mitigation validation records; linked to workflows |
| `oi_runbooks` | Reference data (procedures, steps, prerequisites); low volume, requires full-text search |

### 2.2 Automation Subdomain — All in PostgreSQL

| Table | Reason |
|---|---|
| `oi_automation_workflows` | Workflow state machine with approval/execution gates; requires ACID for concurrent state transitions |
| `oi_automation_validations` | One-to-one validation per workflow; requires FK cascade with `oi_automation_workflows` |
| `oi_automation_audit_records` | Immutable audit trail; requires FK cascade, RLS, and compliance integrity |

### 2.3 Reliability Subdomain — Snapshots in PostgreSQL

| Table | Reason |
|---|---|
| `oi_reliability_snapshots` | Composite reliability scores per service; used for current state queries (dashboard health); moderate volume |

### 2.4 Runtime Subdomain — Config/State in PostgreSQL

| Table | Reason |
|---|---|
| `oi_runtime_baselines` | Reference data — expected metric values per service/environment; low volume, requires UNIQUE constraint |
| `oi_observability_profiles` | Assessment configuration per service/environment; low volume, requires UNIQUE constraint |

### 2.5 Cost Subdomain — Config/State in PostgreSQL

| Table | Reason |
|---|---|
| `oi_service_cost_profiles` | Budget configuration per service/environment; low volume, requires UNIQUE constraint |
| `oi_cost_import_batches` | Batch lifecycle state machine (Pending → Completed/Failed); requires ACID for status transitions |

---

## 3. Data Candidates for ClickHouse (Analytical/Time-Series)

These are **high-volume, append-mostly, time-series oriented** data sets where ClickHouse excels:

### 3.1 Runtime Metrics Time-Series

| PostgreSQL Table | ClickHouse Target | Volume Profile | Rationale |
|---|---|---|---|
| `oi_runtime_snapshots` | `ch_ops_runtime_snapshots` | **HIGH** — one row per service/environment per capture interval | Time-series metrics (latency, error rate, throughput, CPU, memory) captured continuously; ideal for columnar storage with time-based partitioning |
| `oi_drift_findings` | `ch_ops_drift_findings` | **MEDIUM** — one row per detected drift per service | Drift detection results accumulate over time; analytical queries (trend by severity, resolution rate) benefit from ClickHouse |

**ClickHouse Schema Recommendations:**

```sql
-- Runtime metrics time-series
CREATE TABLE ch_ops_runtime_snapshots (
    id UUID,
    tenant_id UUID,
    service_name LowCardinality(String),
    environment LowCardinality(String),
    source LowCardinality(String),
    avg_latency_ms Decimal64(3),
    p99_latency_ms Decimal64(3),
    error_rate Decimal64(3),
    requests_per_second Decimal64(3),
    cpu_usage_percent Decimal64(3),
    memory_usage_mb Decimal64(3),
    active_instances UInt32,
    health_status LowCardinality(String),
    captured_at DateTime64(3, 'UTC')
) ENGINE = MergeTree()
PARTITION BY toYYYYMM(captured_at)
ORDER BY (tenant_id, service_name, environment, captured_at)
TTL captured_at + INTERVAL 90 DAY;
```

### 3.2 Cost Analytics Aggregations

| PostgreSQL Table | ClickHouse Target | Volume Profile | Rationale |
|---|---|---|---|
| `oi_cost_snapshots` | `ch_ops_cost_snapshots` | **MEDIUM–HIGH** — one row per service/environment per capture period | Cost data with share decomposition (CPU, memory, network, storage); analytical queries for trends, comparisons, anomaly detection |
| `oi_cost_records` | `ch_ops_cost_records` | **HIGH** — imported batch records (N per batch) | Granular cost records from external sources; volume grows with import frequency and service count |
| `oi_cost_attributions` | `ch_ops_cost_attributions` | **MEDIUM** — one per API asset/period | Attribution data for cost-per-request analytics; time-range queries and aggregations |
| `oi_cost_trends` | `ch_ops_cost_trends` | **MEDIUM** — one per service/period | Pre-computed trends; ClickHouse can generate these from raw data instead |

**ClickHouse Schema Recommendations:**

```sql
-- Cost analytics
CREATE TABLE ch_ops_cost_snapshots (
    id UUID,
    tenant_id UUID,
    service_name LowCardinality(String),
    environment LowCardinality(String),
    currency LowCardinality(String),
    source LowCardinality(String),
    period LowCardinality(String),
    total_cost Decimal64(4),
    cpu_cost_share Decimal64(4),
    memory_cost_share Decimal64(4),
    network_cost_share Decimal64(4),
    storage_cost_share Decimal64(4),
    captured_at DateTime64(3, 'UTC')
) ENGINE = MergeTree()
PARTITION BY toYYYYMM(captured_at)
ORDER BY (tenant_id, service_name, environment, captured_at)
TTL captured_at + INTERVAL 365 DAY;
```

### 3.3 Incident Trend Analysis

| PostgreSQL Table | ClickHouse Target | Volume Profile | Rationale |
|---|---|---|---|
| `oi_incidents` (subset) | `ch_ops_incident_events` | **LOW–MEDIUM** — event stream from incident lifecycle | Trend analysis (incidents per week, MTTR, severity distribution, correlation rates) benefit from ClickHouse; projected snapshot/event stream rather than live state |

**Note:** The ClickHouse table for incidents should be a **projected event stream** (via outbox → event consumer), NOT a copy of the live state. The authoritative incident state remains in PostgreSQL.

### 3.4 SLA Compliance Time-Series

| Source | ClickHouse Target | Volume Profile | Rationale |
|---|---|---|---|
| `oi_reliability_snapshots` (projected) | `ch_ops_sla_compliance` | **MEDIUM** — periodic snapshots per service | SLA compliance tracking over time; long-range trend queries (30/60/90 day windows) are analytical |

---

## 4. Data That Must NOT Go to ClickHouse

The following data **requires ACID guarantees** and must never be stored in ClickHouse:

| Category | Tables | Reason |
|---|---|---|
| **Active incident state** | `oi_incidents` (live rows) | Concurrent status updates, correlation refresh, mitigation coordination require ACID transactions |
| **Workflow state machines** | `oi_mitigation_workflows`, `oi_automation_workflows` | Approval gates, state transitions, precondition evaluation require strict consistency |
| **Workflow audit trails** | `oi_automation_audit_records`, `oi_mitigation_workflow_actions` | Immutable audit records with FK cascade; compliance requirement for referential integrity |
| **Validation records** | `oi_automation_validations`, `oi_mitigation_validations` | One-to-one FK relationship with workflows; requires ACID for post-execution validation |
| **Runbook definitions** | `oi_runbooks` | Reference data with low volume; requires full-text search and ACID |
| **Configuration/profiles** | `oi_runtime_baselines`, `oi_observability_profiles`, `oi_service_cost_profiles` | Configuration data with UNIQUE constraints; low volume, ACID required |
| **Import batch state** | `oi_cost_import_batches` | Lifecycle state machine (Pending → Completed/Failed); requires ACID for status transitions |

---

## 5. High-Volume Operational Events Analysis

### 5.1 Volume Projections

| Data Type | Entity | Estimated Volume (per 100 services) | Growth Pattern |
|---|---|---|---|
| Runtime snapshots | `RuntimeSnapshot` | ~100 × N environments × snapshots/hour | **Linear + high frequency** — depends on capture interval (e.g., every 5 min = 28,800/day for 100 services × 1 env) |
| Cost records | `CostRecord` | ~100 × batches/day × records/batch | **Batch + periodic** — depends on import frequency (daily/hourly) |
| Drift findings | `DriftFinding` | ~100 × drifts detected/assessment | **Sporadic** — spikes during deployments and degradations |
| Cost snapshots | `CostSnapshot` | ~100 × N environments × periods | **Periodic** — daily or hourly captures |
| Reliability snapshots | `ReliabilitySnapshot` | ~100 × N environments × computations/day | **Periodic** — recomputed after events or on schedule |
| Incident events | `IncidentRecord` (events) | Variable — depends on operational maturity | **Event-driven** — spikes during outages |

### 5.2 Hotspot Identification

| Risk Level | Entity | Concern |
|---|---|---|
| 🔴 High | `RuntimeSnapshot` | Highest write frequency; unbounded growth without TTL; main candidate for ClickHouse offload |
| 🟠 Medium | `CostRecord` | Batch imports can introduce large volumes; no retention policy defined |
| 🟠 Medium | `DriftFinding` | Accumulates over time; resolved findings have no purge policy |
| 🟡 Low | `CostSnapshot` | Moderate volume; periodic captures |
| 🟢 None | `RuntimeBaseline`, `ObservabilityProfile` | Low volume; upsert pattern keeps row count stable |

---

## 6. Aggregations and Time-Series Queries

### 6.1 Queries That Benefit from ClickHouse

| Query Pattern | Current Handler | Data Source | ClickHouse Benefit |
|---|---|---|---|
| Release health timeline | `GetReleaseHealthTimeline.Query` | `oi_runtime_snapshots` | Time-range aggregation over large snapshot volumes |
| Runtime comparison (before/after) | `CompareReleaseRuntime.Query` | `oi_runtime_snapshots` | Period-to-period metric aggregation |
| Cost delta between periods | `GetCostDelta.Query` | `oi_cost_snapshots` | Cross-period aggregation |
| Cost by release | `GetCostByRelease.Query` | `oi_cost_snapshots`, `oi_cost_records` | Release-scoped aggregation over cost data |
| Cost by route/service | `GetCostByRoute.Query` | `oi_cost_attributions` | Service-level cost aggregation |
| Cost trend computation | `ComputeCostTrend.Command` | `oi_cost_snapshots` | Statistical aggregation (avg, peak, trend direction) |
| Anomaly detection | `AlertCostAnomaly.Command` | `oi_cost_snapshots`, `oi_service_cost_profiles` | Threshold comparison over historical data |
| Drift trend analysis | `GetDriftFindings.Query` (filters) | `oi_drift_findings` | Severity/resolution aggregations over time |
| Reliability trend | `GetServiceReliabilityTrend.Query` | `oi_reliability_snapshots` | Score evolution over configurable time windows |
| Incident trend (future) | _(not yet implemented)_ | `oi_incidents` (projected) | MTTR, severity distribution, correlation rates over time |

### 6.2 Queries That Stay in PostgreSQL

| Query Pattern | Handler | Reason |
|---|---|---|
| Incident detail/list | `GetIncidentDetail`, `ListIncidents` | Current state queries; filtered by status, severity, team |
| Workflow detail/list | `GetAutomationWorkflow`, `ListAutomationWorkflows` | Current state with FK joins |
| Runbook list/detail | `ListRunbooks`, `GetRunbookDetail` | Reference data; full-text search |
| Audit trail | `GetAutomationAuditTrail` | FK-based queries with cascade integrity |
| Service reliability list | `ListServiceReliability` | Current snapshot with filters; joins with service catalog |
| Runtime health (current) | `GetRuntimeHealth` | Latest snapshot per service; single-row lookup |
| Observability score | `GetObservabilityScore` | Configuration lookup; single-row |

---

## 7. Correlation Keys with PostgreSQL

When data exists in both PostgreSQL and ClickHouse, the following keys serve as correlation identifiers:

| Correlation Key | PostgreSQL Usage | ClickHouse Usage | Join Pattern |
|---|---|---|---|
| `ServiceId` / `ServiceName` | Service Catalog, Incidents, Automation, Reliability | Runtime snapshots, Cost snapshots, Drift findings | Application-level join via API |
| `EnvironmentId` / `Environment` | Incidents (`EnvironmentId` GUID), Runtime/Cost entities (`Environment` VARCHAR) | All ClickHouse tables | Application-level join; environment resolution in PostgreSQL |
| `TenantId` | All entities via RLS interceptor | All ClickHouse tables (partition/filter key) | Mandatory filter in all queries |
| `IncidentId` | Incidents, Mitigation workflows, Action logs | `ch_ops_incident_events` | Event projection from outbox |
| `ReleaseId` | `DriftFinding.ReleaseId` (nullable) | Cost by release queries | Optional correlation for change-linked analysis |

**Important:** ClickHouse queries must **always** include `TenantId` as a filter to maintain tenant isolation parity with PostgreSQL RLS.

---

## 8. Implementation Approach

### 8.1 Data Flow Architecture

```
PostgreSQL (Source of Truth)
    │
    ├─ Domain Events → Outbox Tables (oi_*_outbox_messages)
    │                      │
    │                      ▼
    │              Outbox Processor (background)
    │                      │
    │                      ▼
    │              Message Broker (events)
    │                      │
    │                      ▼
    │              ClickHouse Consumer
    │                      │
    │                      ▼
    │              ClickHouse (Analytical Store)
    │
    └─ API Queries ──┬─ PostgreSQL (current state, workflows, configs)
                     └─ ClickHouse (trends, aggregations, time-series)
```

### 8.2 Migration Priority

| Wave | Data | Tables | Effort | Impact |
|---|---|---|---|---|
| Wave 1 | Runtime metrics time-series | `oi_runtime_snapshots` | 2–3 days | Highest volume relief; enables timeline/comparison queries at scale |
| Wave 2 | Cost analytics | `oi_cost_snapshots`, `oi_cost_records`, `oi_cost_attributions` | 2–3 days | FinOps analytics; cost trend/delta queries at scale |
| Wave 3 | Incident trend projection | `oi_incidents` (event stream) | 1–2 days | Trend dashboards; MTTR analytics |
| Wave 4 | SLA compliance series | `oi_reliability_snapshots` (projected) | 1 day | Long-range SLA trend queries |
| Wave 5 | Drift analytics | `oi_drift_findings` | 1 day | Drift severity/resolution analytics |

### 8.3 Prerequisites

| # | Prerequisite | Status |
|---|---|---|
| 1 | ClickHouse cluster provisioned and accessible | ❌ Not yet |
| 2 | Outbox processor deployed and operational | ✅ Configured (4 outbox tables) |
| 3 | ClickHouse consumer service implemented | ❌ Not yet |
| 4 | Dual-read query pattern in handlers | ❌ Not yet |
| 5 | Retention/TTL policies defined | ❌ Not yet |

---

## 9. Action Backlog

| # | Action | Esforço | Prioridade |
|---|---|---|---|
| 1 | Define ClickHouse schema for `ch_ops_runtime_snapshots` with MergeTree + TTL | 1 day | 🟠 P2 |
| 2 | Implement outbox → ClickHouse consumer for runtime snapshots | 2–3 days | 🟠 P2 |
| 3 | Migrate `GetReleaseHealthTimeline` and `CompareReleaseRuntime` to dual-read pattern | 1–2 days | 🟠 P2 |
| 4 | Define ClickHouse schema for cost analytics tables | 1 day | 🟡 P3 |
| 5 | Implement cost data ClickHouse consumer | 2 days | 🟡 P3 |
| 6 | Define retention/TTL policies per table (PostgreSQL + ClickHouse) | 1 day | 🟠 P2 |
| 7 | Add `TenantId` filter enforcement in ClickHouse query layer | 1 day | 🔴 P1 |
| 8 | Define incident event projection schema for trend analytics | 1 day | 🟡 P3 |
