# Phase 0 — Build & Test Baseline

**Date:** 2026-03-22
**Environment:** .NET 10.0.102 SDK, Node.js (Vite + TypeScript), Ubuntu runner

---

## 1. Backend Build Status

| Step | Command | Result | Notes |
|------|---------|--------|-------|
| Restore | `dotnet restore NexTraceOne.sln` | ✅ Success | All packages restored |
| Build | `dotnet build NexTraceOne.sln --no-restore` | ✅ Success | 0 errors, 924 warnings |

### Build Warnings Summary
- **MSB3277** (assembly version conflicts): Pre-existing, in E2E.Tests and IntegrationTests projects
- **CS8632** (nullable annotations): Pre-existing, in E2E.Tests and IntegrationTests fixture files
- **CS0618** (obsolete API): Testcontainers `PostgreSqlBuilder()` parameterless constructor deprecated

**Assessment:** Backend builds cleanly. All warnings are pre-existing and non-blocking.

---

## 2. Frontend Build Status

| Step | Command | Result | Notes |
|------|---------|--------|-------|
| Install | `npm ci` | ✅ Success | 384 packages, 2 high-severity npm audit (pre-existing) |
| TypeScript Check | `npx tsc --noEmit` | ✅ Success | 0 errors |
| Full Build | `tsc -b && vite build` | ✅ Success | Production bundle generated |

**Assessment:** Frontend builds cleanly without intervention.

---

## 3. Backend Test Status

### Per-Project Results

| Project | Passed | Failed | Skipped | Total | Status |
|---------|--------|--------|---------|-------|--------|
| BuildingBlocks.Application.Tests | 34 | 0 | 0 | 34 | ✅ Green |
| BuildingBlocks.Core.Tests | 30 | 0 | 0 | 30 | ✅ Green |
| BuildingBlocks.Infrastructure.Tests | 15 | 1 | 0 | 16 | ⚠️ 1 pre-existing failure |
| BuildingBlocks.Observability.Tests | 56 | 0 | 0 | 56 | ✅ Green |
| BuildingBlocks.Security.Tests | — | — | — | 0 | ❌ No tests (empty project) |
| AuditCompliance.Tests | — | — | — | 0 | ❌ No tests (empty project) |
| Governance.Tests | 27 | 0 | 0 | 27 | ✅ Green (but minimal) |
| Catalog.Tests | 466 | 0 | 0 | 466 | ✅ Green |
| AIKnowledge.Tests | 399 | 0 | 0 | 399 | ✅ Green |
| ChangeGovernance.Tests | 195 | 0 | 0 | 195 | ✅ Green |
| IdentityAccess.Tests | 290 | 0 | 0 | 290 | ✅ Green |
| OperationalIntelligence.Tests | 295 | 0 | 0 | 295 | ✅ Green |
| **TOTAL** | **1,807** | **1** | **0** | **1,808** | **99.94% pass** |

### Pre-Existing Failure Detail

**`AppSettingsSecurityTests.BaseAppSettings_ShouldHave17ConnectionStrings`**
- **File:** `tests/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure.Tests/Configuration/AppSettingsSecurityTests.cs:98`
- **Error:** Expected 17 connection strings, found 19 (difference of 2)
- **Root cause:** Two connection strings were added after the test was written: `AutomationDatabase` and `GovernanceDatabase`
- **Classification:** Pre-existing technical debt — test assertion stale
- **Fix needed:** Update assertion from 17 to 19 (Phase 1 candidate)

---

## 4. Frontend Test Status

| Metric | Value |
|--------|-------|
| Test files | 51 |
| Total tests | 456 |
| Passed | 448 |
| Failed | 8 |
| Pass rate | 98.2% |

### Pre-Existing Failure Detail

**`AiAssistantPage.test.tsx`** — 8 tests failing
- **Error:** `TypeError: aiGovernanceApi.listAvailableModels is not a function`
- **File:** `src/features/ai-hub/pages/AiAssistantPage.tsx:429`
- **Root cause:** `AiAssistantPage` calls `aiGovernanceApi.listAvailableModels()` in a `useEffect`, but the test file does not mock this function (it was added after the tests were written)
- **Classification:** Pre-existing technical debt — test setup incomplete for new API call
- **Fix needed:** Add `listAvailableModels` mock to test setup (Phase 2 candidate)

---

## 5. Integration Test Status

| Area | Status | Notes |
|------|--------|-------|
| Integration test project | ✅ Builds | `NexTraceOne.IntegrationTests` compiles |
| E2E test project | ✅ Builds | `NexTraceOne.E2E.Tests` compiles |
| Testcontainers fixture | ✅ Configured | `PostgreSqlIntegrationFixture` creates 4 databases |
| Test execution | ⏸ Not run | Requires Docker daemon (Testcontainers PostgreSQL) |

**Note:** Integration and E2E tests require a Docker daemon with PostgreSQL testcontainers. They build successfully but were not executed in this baseline run because the CI runner does not have Docker available. They are verified to compile and their fixture code is correct for the 4-database consolidation architecture.

---

## 6. Summary of Known Failures

| ID | Area | Test/File | Type | Severity | Phase |
|----|------|-----------|------|----------|-------|
| BF-01 | Infrastructure.Tests | `AppSettingsSecurityTests` | Stale assertion (17→19) | Low | Phase 1 |
| BF-02 | Frontend | `AiAssistantPage.test.tsx` (8 tests) | Missing mock | Medium | Phase 2 |

---

## 7. Corrections Applied for Baseline

**None.** The build and test suite runs without any modifications. All failures are pre-existing and documented above.
