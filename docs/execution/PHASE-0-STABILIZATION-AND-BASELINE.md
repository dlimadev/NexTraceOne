# Phase 0 — Stabilization & Diagnostic Confirmation Baseline

**Date:** 2026-03-22
**Status:** Complete
**Tag:** `v0.9.0-assessment-baseline`

---

## 1. Scope of Phase 0

Phase 0 is the confirmatory gate between the second-wave assessment and the execution programme.
Its purpose is to transform the static audit reports into a verified, reproducible technical baseline from which Phases 1–N can depart with full confidence.

### What was done

| Block | Description | Status |
|-------|-------------|--------|
| A | Conferência da segunda onda de auditoria (13 relatórios) | ✅ Complete |
| B | Validação do build e suíte atual | ✅ Complete |
| C | Validação de integração e conectividade | ✅ Complete |
| D | Confirmação do gap do outbox | ✅ Confirmed |
| E | Confirmação da superfície funcional excluída | ✅ Mapped |
| F | Confirmação dos gaps críticos de testes | ✅ Confirmed |
| G | Geração do baseline versionado | ✅ Documented |
| H | Consolidação documental da fase | ✅ Complete |
| I | Recomendação executiva de partida para Fase 1 | ✅ Delivered |

---

## 2. What Was Confirmed

### 2.1 Build Health

| Area | Status | Details |
|------|--------|---------|
| Backend restore | ✅ Green | `dotnet restore` — 0 errors |
| Backend build | ✅ Green | `dotnet build` — 0 errors, 924 warnings (pre-existing) |
| Frontend install | ✅ Green | `npm ci` — 0 errors |
| Frontend TypeScript | ✅ Green | `tsc --noEmit` — 0 errors |
| Frontend Vite build | ✅ Green | `tsc -b && vite build` — 0 errors |

### 2.2 Test Health

| Suite | Passed | Failed | Notes |
|-------|--------|--------|-------|
| BuildingBlocks.Application | 34 | 0 | ✅ |
| BuildingBlocks.Core | 30 | 0 | ✅ |
| BuildingBlocks.Infrastructure | 15 | 1 | Pre-existing: connection string count assertion expects 17, actual is 19 |
| BuildingBlocks.Observability | 56 | 0 | ✅ |
| BuildingBlocks.Security | — | — | ❌ **Empty** — no test classes |
| AuditCompliance | — | — | ❌ **Empty** — no test classes |
| Governance | 27 | 0 | ⚠️ Minimal coverage for module size |
| Catalog | 466 | 0 | ✅ |
| AIKnowledge | 399 | 0 | ✅ |
| ChangeGovernance | 195 | 0 | ✅ |
| IdentityAccess | 290 | 0 | ✅ |
| OperationalIntelligence | 295 | 0 | ✅ |
| **Backend Total** | **1,807** | **1** | 99.94% pass rate |
| Frontend | 448 | 8 | Pre-existing: AiAssistantPage tests fail due to missing `listAvailableModels` mock |
| **Overall Total** | **2,255** | **9** | All failures pre-existing |

### 2.3 Outbox Gap

**Confirmed.** The `OutboxProcessorJob` processes only `IdentityDbContext`. All 17 other DbContexts inherit outbox support from `NexTraceDbContextBase`, meaning domain events are written to outbox tables on each `SaveChangesAsync()`, but those messages are **never read or dispatched**. Cross-module event propagation is silently broken for all modules except Identity.

### 2.4 Excluded Production Surface

**Confirmed.** 14 route prefixes are excluded in `releaseScope.ts`. All 14 have complete frontend pages, backend API endpoints, and persistence models. The exclusion mechanism is well-designed (whitelist + blacklist pattern) and exclusions appear justified by incomplete backend logic or demo-quality data, not by arbitrary hiding.

### 2.5 Critical Test Gaps

**Confirmed.**
- `BuildingBlocks.Security.Tests` — only `GlobalUsings.cs`, zero tests
- `AuditCompliance.Tests` — project exists, zero test classes
- `Governance.Tests` — 27 tests for a module with 73+ backend features
- Frontend — 51 test files covering 96 pages, with 8 pre-existing failures in AiAssistantPage

---

## 3. What Was Adjusted Minimally for Baseline

No code corrections were required to establish the baseline. The build and test suite runs cleanly without intervention. All failures found are pre-existing and documented:

1. **Infrastructure.Tests** — `BaseAppSettings_ShouldHave17ConnectionStrings` expects 17 but actual is 19 (2 connection strings were added after the test was written: `AutomationDatabase`, `GovernanceDatabase`)
2. **Frontend AiAssistantPage.test.tsx** — 8 tests fail because `aiGovernanceApi.listAvailableModels` is not mocked in the test setup (function was added to the page after the tests were written)

These are classified as **pre-existing technical debt**, not regressions introduced by Phase 0.

---

## 4. What Remains for Subsequent Phases

### Phase 1 — Security & Production Baseline
1. Outbox cross-module processing (GAP-001)
2. Rate limiting middleware
3. TenantId standardization in AIKnowledge
4. BuildingBlocks.Security tests
5. Authorization/CORS audit

### Phase 2+
- Recovery of excluded production surface (14 routes)
- AuditCompliance test coverage
- Governance test hardening
- Frontend AiAssistantPage test fix
- Infrastructure test assertion update (17 → 19)

---

## 5. Baseline Summary

| Attribute | Value |
|-----------|-------|
| Baseline tag | `v0.9.0-assessment-baseline` |
| Backend build | ✅ 0 errors |
| Frontend build | ✅ 0 errors |
| Backend tests | 1,807 passed / 1 pre-existing failure |
| Frontend tests | 448 passed / 8 pre-existing failures |
| DbContexts | 18 total across 4 databases |
| Outbox coverage | 1/18 DbContexts (Identity only) |
| Excluded routes | 14 route prefixes |
| Security tests | 0 |
| AuditCompliance tests | 0 |
| Assessment documents | 13/13 verified |
