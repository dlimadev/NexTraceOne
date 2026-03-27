# Phase 8 — Test & Hardening Plan

## Overview

Phase 8 focuses on strengthening automated tests, hardening critical paths, and preparing the NexTraceOne platform for final consolidation.

## Build & Test Baseline (start of Phase 8)

| Metric | Value |
|---|---|
| Backend build errors | 0 |
| AIKnowledge tests | 220 pass |
| Frontend tests | 360 pass / 21 pre-existing failures |

## Test Coverage Strategy

### Priority 1 — AI Context Isolation (CRITICAL)
Validates that the AI cannot leak between tenants or mix environments.

Tests added:
- `AiAnalysisContextIsolationTests.cs` — 18 tests
- `AiAnalysisNonProdScenarioTests.cs` — 10 scenario tests

Scenarios covered:
- TenantId must be in AI grounding (never leaks to other tenants)
- CorrelationId is unique per execution (auditability)
- EnvironmentId is echoed in response (traceability)
- CompareEnvironments grounding mentions "same tenant" constraint
- Validator rejects same-environment comparison
- Validator rejects empty TenantId/EnvironmentId
- Safe failure when AI provider throws

### Priority 2 — Non-Prod Scenario Validation (CRITICAL)
Validates the core product mission: prevent production incidents.

Scenarios:
- QA risk analysis with contract drift → HIGH risk detected
- UAT vs PROD comparison → BLOCK_PROMOTION detected
- STAGING → PROD promotion blocked (contract breaking)
- STAGING → PROD promotion approved (healthy service)
- HML analysis with MEDIUM risk
- DEV analysis with LOW risk
- Promotion readiness with ReleaseId
- Multi-dimensional comparison

### Priority 3 — Validator Edge Cases (HIGH)
- Empty ServiceName/Version rejected
- ObservationWindowDays outside 1..90 rejected
- Source == Target rejected for promotion readiness
- Subject == Reference rejected for environment comparison

### Priority 4 — Frontend Hardening (MEDIUM)
- Tab switching renders correct content
- Run Analysis not shown for production-like environments
- Assess/Compare buttons disabled without required inputs
- No-context guard when tenant is null

## Hardening Areas

### Backend
| Area | Risk | Action |
|---|---|---|
| TenantId in grounding | CRITICAL | Validated in AiAnalysisContextIsolationTests |
| CorrelationId uniqueness | HIGH | Validated (each execution generates new Guid) |
| Validator coverage | HIGH | ObservationWindowDays, ServiceName, Version edge cases |
| Safe failure on provider exception | HIGH | All 3 features tested |
| Context in response | MEDIUM | TenantId/EnvironmentId echoed back |

### Frontend
| Area | Risk | Action |
|---|---|---|
| Production-like env protection | HIGH | Run Analysis not rendered for prod-like |
| No-context guard | HIGH | Shows message when tenant null |
| Tab navigation | MEDIUM | Switching tested |
| Form validation UX | MEDIUM | Buttons disabled without required inputs |

## Remaining Gaps (for Phase 9)

1. **Real LLM integration** — No AI SDK installed. All AI calls use `IExternalAIRoutingPort` stub.
2. **Persistent audit trail** — AI executions are logged but not persisted to DB yet.
3. **`GET /api/v1/identity/environments`** — `EnvironmentProvider` uses mock loader.
4. **E2E tests** — No end-to-end test covering full AI analysis flow.
5. **Integration tests** — AI endpoints not tested in `NexTraceOne.IntegrationTests`.
