# 09 — Observability and AI Readiness

**Date:** 2026-03-22

---

## Observability Architecture

### Dual-Store Design

The platform implements a dual-store observability architecture:

| Store | Technology | Purpose | Data |
|-------|-----------|---------|------|
| **Product Store** | PostgreSQL | Aggregated insights, correlations, topology | ServiceMetricsSnapshot, ObservedTopologyEntry, AnomalySnapshot, TelemetryReference, InvestigationContext, ReleaseRuntimeCorrelation |
| **Telemetry Store** | Tempo (traces) + Loki (logs) | Raw telemetry signals | Traces, logs, raw metrics |

**Connection:** `TelemetryReference` entity creates pointers from Product Store entries to raw data in Telemetry Store (trace_id, log stream).

**Assessment:** ✅ Well-designed separation of concerns. Product Store optimized for aggregation/querying; Telemetry Store for high-volume raw signal storage.

---

## Observability Components

### OpenTelemetry Integration

| Component | Status | Evidence |
|-----------|--------|----------|
| Tracing via OTLP | ✅ Configured | `DependencyInjection.cs` — OpenTelemetry tracing with OTLP exporter |
| Activity Sources | ✅ Defined | `NexTraceActivitySources.cs` — Commands, Queries, Events, ExternalHttp, TelemetryPipeline, Integrations |
| Metrics via OTLP | ✅ Configured | `DependencyInjection.cs` — OpenTelemetry metrics export |
| Custom Business Meters | ✅ Defined | `NexTraceMeters.cs` — 12 custom meters (deployments, workflows, blast radius, anomalies, topology, etc.) |
| Structured Logging | ✅ Configured | `SerilogConfiguration.cs` — Serilog with enrichers (Environment, Machine, Thread), sinks (Console, File, Loki) |
| Loki Integration | ✅ Configured | Configurable via `Observability:Serilog:Loki` section |
| Context Enrichment | ✅ Implemented | `TelemetryContextEnricher.cs` — correlation IDs, service info, tenant, environment |

### Health Checks

| Service | Endpoints | Implementation |
|---------|----------|---------------|
| ApiHost | `/live`, `/ready`, `/health` | ✅ AllowAnonymous, DB connectivity, self check |
| BackgroundWorkers | `/live`, `/ready`, `/health` | ✅ AllowAnonymous, job health registry |
| Ingestion.Api | `/health`, `/ready`, `/live` | ✅ AllowAnonymous |

Health check response uses custom `HealthCheckResponseWriter` with JSON output including status, individual results, and total duration.

### Telemetry Models (Product Store)

| Model | Purpose | Key Fields | Status |
|-------|---------|-----------|--------|
| `ServiceMetricsSnapshot` | Aggregated service metrics | RequestCount, ErrorRate, Latency (p50/p95/p99), CPU/Memory | ✅ Defined |
| `ObservedTopologyEntry` | Service dependency graph | Source/Target services, volume, error rate, latency | ✅ Defined |
| `AnomalySnapshot` | Detected anomalies | Detection time, severity, affected service, threshold breach | ✅ Defined |
| `TelemetryReference` | Pointer to raw telemetry | SignalType, ExternalId, BackendType, AccessUri | ✅ Defined |
| `InvestigationContext` | AI investigation state | Correlation ID, timeline, symptoms, hypotheses | ✅ Defined |
| `ReleaseRuntimeCorrelation` | Deploy → runtime impact | Release ID, metric deltas (throughput, error rate, latency) | ✅ Defined |

---

## Product Store Interfaces

| Interface | Purpose | Status |
|-----------|---------|--------|
| `IProductStore` (aggregate) | Top-level abstraction | ✅ Defined |
| `ITopologyWriter` / `ITopologyReader` | Service topology graph | ✅ Interface defined |
| `IAnomalyWriter` / `IAnomalyReader` | Anomaly detection records | ✅ Interface defined |
| `ITelemetryReferenceWriter` / `ITelemetryReferenceReader` | Cross-store pointers | ✅ Interface defined |
| `IReleaseCorrelationWriter` / `IReleaseCorrelationReader` | Deploy impact correlation | ✅ Interface defined |
| `IInvestigationContextWriter` / `IInvestigationContextReader` | AI investigation state | ✅ Interface defined |
| `IServiceMetricsWriter` / `IServiceMetricsReader` | Aggregated metrics | ✅ Interface defined |
| `IDependencyMetricsWriter` / `IDependencyMetricsReader` | Dependency metrics | ✅ Interface defined |

**Critical Gap:** These interfaces are **defined but need verification of concrete implementations**. If implementations exist only as stubs or are not wired into the DI container, the entire product store pipeline would be non-functional.

---

## Drift Detection Pipeline

| Component | Status | Evidence |
|-----------|--------|----------|
| `DriftDetectionJob` | ✅ Implemented | `BackgroundWorkers/Jobs/DriftDetectionJob.cs` — 149 lines, configurable interval |
| `DetectRuntimeDrift` command | ✅ Dispatched via MediatR | From DriftDetectionJob to OperationalIntelligence |
| Environment Comparison | ✅ Frontend | `EnvironmentComparisonPage.tsx` |
| Runtime Intelligence API | ✅ Backend | `RuntimeIntelligenceEndpointModule.cs` |
| Drift documentation | ✅ | `docs/observability/DRIFT-DETECTION-PIPELINE.md` |

**Assessment:** ✅ The drift detection pipeline is architecturally sound with clear separation between detection (background job) and analysis (application layer).

---

## AI Readiness

### AI Knowledge Module Status

| Capability | Backend | Frontend | Status |
|-----------|---------|----------|--------|
| AI Agent Management | ✅ 68 features | ✅ AiAgentsPage, AgentDetailPage | ✅ In scope |
| AI Assistant (Chat) | ✅ AiRuntimeEndpointModule | ✅ AiAssistantPage, AssistantPanel | ✅ In scope |
| AI Analysis | ✅ AiOrchestrationEndpointModule | ✅ AiAnalysisPage | ✅ In scope |
| Model Registry | ✅ Full CRUD | ✅ ModelRegistryPage | ⚠️ Excluded |
| AI Policies | ✅ Full CRUD | ✅ AiPoliciesPage | ⚠️ Excluded |
| AI Token Budgets | ✅ Full CRUD | ✅ TokenBudgetPage | ⚠️ Excluded |
| AI Audit Trail | ✅ Full CRUD | ✅ AiAuditPage | ⚠️ Excluded |
| AI Routing Rules | ✅ Full CRUD | ✅ AiRoutingPage | ⚠️ Excluded |
| IDE Integrations | ✅ Endpoints | ✅ IdeIntegrationsPage | ⚠️ Excluded |
| External AI Integration | ✅ ExternalAiDbContext, endpoints | Internal | ✅ Infrastructure |
| Agent Execution/Orchestration | ✅ AgentExecution, ToolInvocation entities | Internal | ✅ Infrastructure |

### AI Governance Features

| Feature | Status | Notes |
|---------|--------|-------|
| Provider Registry | ✅ | AiProvider entity with health tracking |
| Model Registry | ✅ Backend | Excluded from production scope |
| Token Budget Management | ✅ Backend | Excluded from production scope |
| Policy Enforcement | ✅ Backend | Excluded from production scope |
| Usage Auditing | ✅ Backend | Excluded from production scope |
| Routing Rules | ✅ Backend | Excluded from production scope |

**Critical Assessment:** 6 of 10 AI features are excluded from production scope. The backend for all features exists, but the product cannot claim AI governance readiness when the governance features (model registry, policies, budgets, audit, routing) are hidden.

---

## Environment Comparison (Pre-Production Analysis)

| Component | Status | Evidence |
|-----------|--------|----------|
| Environment entity | ✅ | `Environment.cs` with `IsPrimaryProduction` field |
| Environment Context (Frontend) | ✅ | `EnvironmentContext.tsx` — environment selection, profile inference (production/staging/qa/dev/sandbox) |
| Environment Comparison Page | ✅ | `EnvironmentComparisonPage.tsx` |
| Runtime Intelligence API | ✅ | `RuntimeIntelligenceEndpointModule.cs` |
| `isProductionLike` flag | ✅ | Adapts UX for production vs non-production environments |
| Drift Detection Job | ✅ | Compares runtime snapshots vs baselines |

**Assessment:** ✅ The infrastructure for environment comparison is in place. The platform can compare behavior across environments, which is a key differentiator for production change confidence.

---

## Observability Gaps

| # | Gap | Severity | Impact | Recommendation |
|---|-----|----------|--------|---------------|
| OB-01 | Product Store interface implementations may be stubs | High | Telemetry pipeline non-functional if not wired | Verify implementations exist and are registered in DI |
| OB-02 | Telemetry Store (Tempo/Loki) integration untested | Medium | Raw signal storage unverified | Add integration tests for OTLP export |
| OB-03 | Metrics aggregation pipeline (1m/1h/1d) not verified | Medium | Historical metrics may not aggregate | Verify partitioned table strategy |
| OB-04 | AI governance features excluded from production | High | AI governance pillar incomplete | Include Model Registry, Policies, Budgets, Audit in production scope |
| OB-05 | No alerting integration | Medium | Anomaly detection without notification | Add alerting gateway (PagerDuty, OpsGenie, webhooks) |
| OB-06 | Grafana dashboards documented but provisioning unverified | Low | Monitoring dashboards may not auto-deploy | Verify dashboard provisioning in docker-compose |

---

## AI Readiness Score

| Criterion | Score | Notes |
|-----------|-------|-------|
| AI Agent Foundation | 85% | Agents, execution, orchestration entities well-defined |
| AI Governance | 40% | Features exist but excluded from production |
| AI Operational Assistance | 70% | AI Assistant, AI Analysis in scope |
| External AI Integration | 75% | Provider management, health tracking |
| IDE Integration | 30% | Page exists but excluded |
| Token/Budget Governance | 30% | Backend exists but excluded |
| **Overall AI Readiness** | **55%** | Strong foundation, governance delivery incomplete |
