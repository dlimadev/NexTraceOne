# Final Blockers and Residual Risks

> **Phase**: 9 — Final Conformity Audit  
> **Document**: Final Blockers and Residual Risks  
> **Version**: 1.0 | **Date**: 2026-03-22  
> **Release**: ZR-6  

---

## Section 1 — Open Final Blockers

These items block **production** deployment. They do not block staging.

---

### BLOCKER-P0 — Production secrets not configured

| Field | Value |
|-------|-------|
| **ID** | BLOCKER-P0 |
| **Title** | GitHub Environment `production` secrets not configured |
| **Severity** | CRITICAL |
| **Impact** | Application refuses to start in Production environment — `StartupValidation.cs` throws `InvalidOperationException` if `Jwt:Secret` is absent or < 32 characters |
| **Recommended action** | Create GitHub Environment named `production`; set `JWT_SECRET` (min 48-char base64 from `openssl rand -base64 48`), `POSTGRES_PASSWORD`, all connection strings, `ASPNETCORE_ENVIRONMENT=Production` |
| **Suggested owner** | Platform / DevOps |
| **Urgency** | P0 — Required before any production deployment |
| **Blocks staging?** | ❌ No — staging has its own environment |
| **Evidence** | `StartupValidation.cs:MinimumJwtSecretLength = 32`; non-dev check enforced |

---

### BLOCKER-P1 — No automated database backup configured

| Field | Value |
|-------|-------|
| **ID** | BLOCKER-P1 |
| **Title** | Automated backup not configured for production databases |
| **Severity** | HIGH |
| **Impact** | Risk of unrecoverable data loss in `nextraceone_identity`, `nextraceone_catalog`, `nextraceone_operations`, `nextraceone_ai` in case of infrastructure failure |
| **Recommended action** | Configure daily automated backups (minimum) with 30-day retention. Document and test restore procedure against a backup copy before go-live |
| **Suggested owner** | Infrastructure / Platform |
| **Urgency** | P1 — Required before any production deployment; can be done in parallel with BLOCKER-P0 |
| **Blocks staging?** | ❌ No |
| **Evidence** | Phase 8 report Section 9 — explicitly listed as P1 pending |

---

## Section 2 — Residual Risks

These items do **not** block staging or production deployment, but carry non-zero risk that should be tracked and mitigated over time.

---

### RISK-01 — `GenerateDraftFromAi` uses template stub

| Field | Value |
|-------|-------|
| **ID** | RISK-01 |
| **Title** | AI-assisted contract draft generation returns protocol template, not AI-generated content |
| **Severity** | MEDIUM |
| **Module** | `NexTraceOne.Catalog.Application.Contracts.Features.GenerateDraftFromAi` |
| **Route** | `/contracts` (IN production scope) |
| **Impact** | Users invoking "Generate Draft from AI" receive a protocol-based template rather than an AI-enriched draft. Functional — template is useful and correctly structured — but does not deliver the AI value proposition |
| **Recommended action** | Integrate `SendAssistantMessage` handler or a dedicated AI contract generation model call into `GenerateDraftFromAi.Handler` once AI provider is configured for contract use cases |
| **Suggested owner** | Catalog module team |
| **Urgency** | P2 — Address in next sprint post-staging validation |

---

### RISK-02 — Telemetry retrieval service returns empty results

| Field | Value |
|-------|-------|
| **ID** | RISK-02 |
| **Title** | `TelemetryRetrievalService` stub — AI assistant has no live telemetry context |
| **Severity** | MEDIUM |
| **Module** | `NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services.TelemetryRetrievalService` |
| **Impact** | AI assistant cannot reference live traces, logs, or metrics when answering operational questions. Responses are based on knowledge base and conversation context only |
| **Recommended action** | Implement OpenTelemetry Collector integration in `TelemetryRetrievalService`; connect to configured OTLP endpoint |
| **Suggested owner** | AIKnowledge / Platform Observability team |
| **Urgency** | P2 — Track as known limitation in staging; implement before production if AI-assisted investigation is a key use case |

---

### RISK-03 — Document retrieval service returns empty results

| Field | Value |
|-------|-------|
| **ID** | RISK-03 |
| **Title** | `DocumentRetrievalService` stub — AI assistant has no document knowledge context |
| **Severity** | LOW |
| **Module** | `NexTraceOne.AIKnowledge.Infrastructure.Runtime.Services.DocumentRetrievalService` |
| **Impact** | AI assistant cannot reference indexed documentation, runbooks, or knowledge base when generating responses |
| **Recommended action** | Implement document indexing and retrieval; connect to knowledge source registry (`AiSourceRegistryService`) |
| **Suggested owner** | AIKnowledge team |
| **Urgency** | P3 — Lower priority than telemetry; address after RISK-02 |

---

### RISK-04 — AI provider not configured in CI

| Field | Value |
|-------|-------|
| **ID** | RISK-04 |
| **Title** | Real AI provider (LLM) not available in CI pipeline |
| **Severity** | MEDIUM |
| **Impact** | AI-path integration tests run against deterministic fallback only. Real LLM behavior (token limits, rate limits, model output variance) not tested before deployment |
| **Recommended action** | Configure a test-tier AI provider (e.g., local Ollama) in CI or staging environment; add an integration test that invokes `SendAssistantMessage` against the real provider |
| **Suggested owner** | CI / AIKnowledge team |
| **Urgency** | P2 — Important for staging confidence; required before production use of AI features |

---

### RISK-05 — Governance FinOps handlers return `IsSimulated: true`

| Field | Value |
|-------|-------|
| **ID** | RISK-05 |
| **Title** | 9 governance FinOps handlers still return `IsSimulated: true` |
| **Severity** | LOW |
| **Module** | `NexTraceOne.Governance.Application.Features.Get{FinOps|WasteSignals|DomainFinOps|ServiceFinOps|TeamFinOps|...}` |
| **Routes** | `/governance/*` — ALL excluded from production scope |
| **Impact** | FinOps pages display demo data. `DemoBanner` is shown. No production user is exposed to this because these routes are excluded via `releaseScope.ts` |
| **Recommended action** | Implement real cost data integration (cloud billing APIs) and remove `IsSimulated: true` when FinOps module is promoted to production scope |
| **Suggested owner** | Governance / FinOps team |
| **Urgency** | P4 — Not urgent; route excluded from production |

---

### RISK-06 — `ApplyGovernancePack` and `CreatePackVersion` are MVP stubs

| Field | Value |
|-------|-------|
| **ID** | RISK-06 |
| **Title** | Governance pack application and versioning have no DB persistence |
| **Severity** | LOW |
| **Module** | `NexTraceOne.Governance.Application.Features.{ApplyGovernancePack|CreatePackVersion}` |
| **Routes** | `/governance/packs` — excluded from production scope |
| **Impact** | GovernancePack apply and version create actions return success but do not persist to DB. No production user is affected |
| **Recommended action** | Implement full persistence when governance packs are promoted to production scope |
| **Suggested owner** | Governance team |
| **Urgency** | P4 — Not urgent; route excluded from production |

---

### RISK-07 — Refresh token E2E not covered

| Field | Value |
|-------|-------|
| **ID** | RISK-07 |
| **Title** | Refresh token flow not covered by E2E tests |
| **Severity** | LOW |
| **Module** | IdentityAccess |
| **Impact** | Token refresh behavior under expiry is not tested end-to-end. Functionality exists and is covered by unit tests |
| **Recommended action** | Add refresh token E2E test in `AuthApiFlowTests.cs` |
| **Suggested owner** | IdentityAccess team |
| **Urgency** | P3 — Address in sprint after go-live |

---

### RISK-08 — k6 load tests not implemented

| Field | Value |
|-------|-------|
| **ID** | RISK-08 |
| **Title** | Formal load testing with k6 not implemented; performance baseline is documented but not validated under load |
| **Severity** | MEDIUM |
| **Impact** | Unknown system behavior under concurrent user load. Performance baseline exists (`docs/quality/PERFORMANCE-AND-RESILIENCE-BASELINE.md`) but is untested |
| **Recommended action** | Implement k6 load test scenarios for: service catalog list, contract detail fetch, incident creation, AI assistant message; run against staging |
| **Suggested owner** | Platform / QA team |
| **Urgency** | P2 — Should be completed during staging validation before production |

---

### RISK-09 — Fault injection in workers not tested

| Field | Value |
|-------|-------|
| **ID** | RISK-09 |
| **Title** | Worker resilience (`DriftDetectionJob`, `BackgroundWorkers`) not validated under failure conditions |
| **Severity** | LOW |
| **Impact** | If DB or downstream services become unavailable, worker behavior (retry, backoff, graceful degradation) is not empirically validated |
| **Recommended action** | Use Toxiproxy or similar to inject DB latency/failures and validate `DriftDetectionJob` error handling and `WorkerJobHealthRegistry` response |
| **Suggested owner** | Platform / QA team |
| **Urgency** | P3 — Lower priority given health check integration is present |

---

### RISK-10 — Basic test coverage in Governance and AuditCompliance

| Field | Value |
|-------|-------|
| **ID** | RISK-10 |
| **Title** | `Governance.Tests` has only 23 unit tests; `AuditCompliance.Tests` has ~30 |
| **Severity** | LOW |
| **Impact** | Changes to governance or audit modules carry higher regression risk than other modules |
| **Recommended action** | Expand test coverage to at least 80 handler-level unit tests per module before promoting to full production scope |
| **Suggested owner** | Governance / AuditCompliance teams |
| **Urgency** | P3 — Address incrementally; not urgent for current scope |

---

## Summary Table

| ID | Title | Severity | Blocks Production? | Blocks Staging? | Urgency |
|----|-------|----------|--------------------|-----------------|---------|
| BLOCKER-P0 | Production secrets not configured | CRITICAL | ✅ Yes | ❌ No | P0 |
| BLOCKER-P1 | No automated DB backup | HIGH | ✅ Yes | ❌ No | P1 |
| RISK-01 | `GenerateDraftFromAi` template stub | MEDIUM | ❌ No | ❌ No | P2 |
| RISK-02 | `TelemetryRetrievalService` empty | MEDIUM | ❌ No | ❌ No | P2 |
| RISK-03 | `DocumentRetrievalService` empty | LOW | ❌ No | ❌ No | P3 |
| RISK-04 | AI provider not in CI | MEDIUM | ❌ No | ❌ No | P2 |
| RISK-05 | Governance FinOps `IsSimulated` | LOW | ❌ No (excluded) | ❌ No | P4 |
| RISK-06 | GovernancePack stubs | LOW | ❌ No (excluded) | ❌ No | P4 |
| RISK-07 | Refresh token E2E missing | LOW | ❌ No | ❌ No | P3 |
| RISK-08 | k6 load tests not implemented | MEDIUM | ❌ No | ❌ No | P2 |
| RISK-09 | Worker fault injection untested | LOW | ❌ No | ❌ No | P3 |
| RISK-10 | Low test coverage in Governance/Audit | LOW | ❌ No | ❌ No | P3 |

---

## Recommended Resolution Order

**Before production deployment (P0–P1)**:
1. BLOCKER-P0 — Configure production secrets
2. BLOCKER-P1 — Configure database backups

**During staging validation (P2)**:
3. RISK-08 — k6 load tests on staging
4. RISK-04 — AI provider in staging/CI
5. RISK-01 — Evaluate GenerateDraftFromAi real implementation
6. RISK-02 — Start TelemetryRetrievalService implementation

**Post-go-live (P3–P4)**:
7. RISK-07 — Refresh token E2E
8. RISK-03 — DocumentRetrievalService
9. RISK-09 — Fault injection tests
10. RISK-10 — Expand module test coverage
11. RISK-05, RISK-06 — Governance FinOps implementation when route is promoted

---

*Document produced by Release Readiness Lead — Phase 9.*
