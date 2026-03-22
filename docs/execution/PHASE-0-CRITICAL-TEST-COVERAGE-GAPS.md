# Phase 0 — Critical Test Coverage Gaps

**Date:** 2026-03-22
**Methodology:** Direct inspection of test project contents, build output, and test execution results.

---

## 1. Coverage Gap Matrix

| Area | Evidence | Gap Type | Risk | Priority | Recovery Phase |
|------|----------|----------|------|----------|----------------|
| **BuildingBlocks.Security** | Project exists, only `GlobalUsings.cs` — zero test classes | Complete absence | 🔴 Critical | P0 | Phase 1 |
| **AuditCompliance** | Project exists with `.csproj` only — zero test files | Complete absence | 🔴 Critical | P0 | Phase 1 |
| **Governance** | 27 tests / 4 files for 73+ backend features | Severe under-coverage | 🟠 High | P1 | Phase 2 |
| **Frontend AiAssistantPage** | 8 failing tests — `listAvailableModels` not mocked | Broken tests | 🟡 Medium | P2 | Phase 2 |
| **Infrastructure.Tests** | 1 failing test — stale assertion (17 vs 19 conn strings) | Stale assertion | 🟢 Low | P3 | Phase 1 |

---

## 2. Detailed Analysis

### 2.1 BuildingBlocks.Security — CRITICAL

**Path:** `tests/building-blocks/NexTraceOne.BuildingBlocks.Security.Tests/`

**Contents:**
```
NexTraceOne.BuildingBlocks.Security.Tests/
├── NexTraceOne.BuildingBlocks.Security.Tests.csproj
└── GlobalUsings.cs    ← only file, contains using statements
```

**What should be tested:**
The `BuildingBlocks.Security` library provides critical security infrastructure:
- JWT token validation/generation
- Permission-based authorization (391 `RequirePermission` decorators)
- `RequirePermissionAttribute` and its middleware
- Security middleware pipeline
- CORS configuration
- CSRF protection
- Cookie authentication configuration
- StartupValidation (JWT secret strength, connection string security)

**Risk:** Without tests, any change to security infrastructure could introduce authentication/authorization bypasses undetected.

**Minimum tests for Phase 1:**
1. `RequirePermissionAttribute` enforcement
2. JWT token validation edge cases
3. `StartupValidation` assertions
4. Permission resolution logic
5. Security middleware ordering

### 2.2 AuditCompliance — CRITICAL

**Path:** `tests/modules/auditcompliance/NexTraceOne.AuditCompliance.Tests/`

**Contents:**
```
NexTraceOne.AuditCompliance.Tests/
└── NexTraceOne.AuditCompliance.Tests.csproj    ← only file
```

**What should be tested:**
The AuditCompliance module handles:
- Audit trail capture and storage
- Compliance event recording
- Break-glass access auditing
- JIT access auditing
- Access review workflows
- Delegation auditing

**Risk:** Audit compliance is a regulatory requirement for enterprise deployments. Untested audit logic could miss events or record incorrect data, creating compliance liability.

**Minimum tests for Phase 1:**
1. Audit event creation and persistence
2. Break-glass audit trail completeness
3. JIT access audit trail
4. Audit query features
5. Tenant isolation in audit records

### 2.3 Governance — HIGH

**Path:** `tests/modules/governance/NexTraceOne.Governance.Tests/`

**Contents:**
```
NexTraceOne.Governance.Tests/
├── Application/Features/PlatformStatusFeatureTests.cs
├── Application/Features/IntegrationHubFeatureTests.cs
├── Application/Features/GovernanceSimulatedDataTests.cs
└── GlobalUsings.cs
```

**Test count:** 27 tests in 3 test classes

**Module scope:** The Governance module has 73+ backend features covering:
- Governance packs and policies
- Team management and ownership
- Platform status monitoring
- Integration hub
- Compliance rules
- Risk assessment

**Coverage ratio:** ~37% of features have test representation (27 tests / 73 features)

**Risk:** Governance is a central pillar of NexTraceOne. Under-tested governance logic means policy enforcement, team assignment, and compliance rules could malfunction.

**Minimum additional tests for Phase 2:**
1. Governance pack CRUD and assignment
2. Team creation and ownership
3. Policy validation and enforcement
4. Governance scope filtering

### 2.4 Frontend — MEDIUM

**Test files:** 51 test files with 456 individual tests
**Pages:** 96 frontend pages
**Failures:** 8 pre-existing failures (all in `AiAssistantPage.test.tsx`)

**Test-to-page ratio:** ~53% of pages have dedicated test coverage

**Production-scope pages without tests:** While many pages have tests, some production-visible pages may lack coverage. The immediate priority is fixing the 8 broken tests in AiAssistantPage.

**Failure details:**
- **Root cause:** `AiAssistantPage` added a `useEffect` that calls `aiGovernanceApi.listAvailableModels()`, but the test setup does not mock this function
- **Fix:** Add `listAvailableModels: vi.fn().mockResolvedValue({ models: [] })` to the mock setup

---

## 3. Test Distribution Analysis

### Backend Test Distribution

| Module | Tests | Features (est.) | Ratio | Assessment |
|--------|-------|-----------------|-------|------------|
| Catalog | 466 | ~80 | ~5.8 tests/feature | ✅ Strong |
| AIKnowledge | 399 | ~65 | ~6.1 tests/feature | ✅ Strong |
| OperationalIntelligence | 295 | ~55 | ~5.4 tests/feature | ✅ Good |
| IdentityAccess | 290 | ~50 | ~5.8 tests/feature | ✅ Strong |
| ChangeGovernance | 195 | ~40 | ~4.9 tests/feature | ✅ Good |
| BuildingBlocks (all) | 135 | ~30 | ~4.5 tests/feature | ✅ Good |
| Governance | 27 | ~73 | ~0.4 tests/feature | ❌ Critical gap |
| AuditCompliance | 0 | ~20 | 0 | ❌ Critical gap |
| Security | 0 | ~10 | 0 | ❌ Critical gap |

### Summary

| Category | Tests | % of Total |
|----------|-------|------------|
| Healthy coverage (>3 tests/feature) | 1,780 | 98.5% |
| Under-covered (<1 test/feature) | 27 | 1.5% |
| Zero coverage | 0 (2 projects) | 0% |
| **Total backend** | **1,807** | **100%** |

---

## 4. Priority Order for Remediation

| Priority | Area | Effort | Phase |
|----------|------|--------|-------|
| P0 | BuildingBlocks.Security tests | Medium (5-10 test classes) | Phase 1 |
| P0 | AuditCompliance tests | Medium (5-8 test classes) | Phase 1 |
| P1 | Governance test expansion | Medium (15-20 test classes) | Phase 2 |
| P2 | Frontend AiAssistantPage fix | Small (1 mock addition) | Phase 2 |
| P3 | Infrastructure.Tests assertion fix | Trivial (change 17→19) | Phase 1 |
