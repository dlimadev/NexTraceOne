# Contracts Module — Backend Functional Corrections

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 04 — Contracts  
> **Phase:** B1 — Module Consolidation

---

## 1. Endpoints Inventory

### ContractsEndpointModule (36 endpoints)

**File:** `src/modules/catalog/NexTraceOne.Catalog.API/Contracts/Endpoints/ContractsEndpointModule.cs`

| # | Endpoint | Method | Permission | Handler | Status |
|---|----------|--------|-----------|---------|--------|
| 1 | `/api/v1/contracts/list` | GET | contracts:read | ListContracts | ✅ |
| 2 | `/api/v1/contracts/summary` | GET | contracts:read | GetContractsSummary | ✅ |
| 3 | `/api/v1/contracts/by-service/{serviceId}` | GET | contracts:read | ListContractsByService | ✅ |
| 4 | `/api/v1/contracts/{id}` | GET | contracts:read | GetContractVersionDetail | ✅ |
| 5 | `/api/v1/contracts/{id}/history` | GET | contracts:read | GetContractHistory | ✅ |
| 6 | `/api/v1/contracts/{id}/violations` | GET | contracts:read | ListRuleViolations | ✅ |
| 7 | `/api/v1/contracts/` | POST | contracts:write | ImportContract | ✅ |
| 8 | `/api/v1/contracts/version` | POST | contracts:write | CreateContractVersion | ✅ |
| 9 | `/api/v1/contracts/diff` | POST | contracts:write | ComputeSemanticDiff | ✅ |
| 10 | `/api/v1/contracts/lifecycle-transition` | POST | contracts:write | TransitionLifecycleState | ✅ |
| 11 | `/api/v1/contracts/deprecate` | POST | contracts:write | DeprecateContractVersion | ✅ |
| 12 | `/api/v1/contracts/lock` | POST | contracts:write | LockContractVersion | ✅ |
| 13 | `/api/v1/contracts/sign` | POST | contracts:write | SignContractVersion | ✅ |
| 14 | `/api/v1/contracts/verify-signature` | POST | contracts:read | VerifySignature | ✅ |
| 15 | `/api/v1/contracts/search` | POST | contracts:read | SearchContracts | ✅ |
| 16 | `/api/v1/contracts/classify-breaking-change` | POST | contracts:read | ClassifyBreakingChange | ✅ |
| 17 | `/api/v1/contracts/compatibility-assessment` | POST | contracts:read | GetCompatibilityAssessment | ✅ |
| 18 | `/api/v1/contracts/suggest-version` | POST | contracts:read | SuggestSemanticVersion | ✅ |
| 19 | `/api/v1/contracts/validate` | POST | contracts:read | ValidateContractIntegrity | ✅ |
| 20 | `/api/v1/contracts/export` | POST | contracts:read | ExportContract | ✅ |
| 21 | `/api/v1/contracts/evaluate-rules` | POST | contracts:write | EvaluateContractRules | ✅ |
| 22 | `/api/v1/contracts/scorecard` | POST | contracts:read | GenerateScorecard | ✅ |
| 23 | `/api/v1/contracts/evidence-pack` | POST | contracts:read | GenerateEvidencePack | ✅ |
| 24 | `/api/v1/contracts/sync` | POST | contracts:write | SyncContracts | ✅ |

### ContractStudioEndpointModule (11 endpoints)

**File:** `src/modules/catalog/NexTraceOne.Catalog.API/Contracts/Endpoints/ContractStudioEndpointModule.cs`

| # | Endpoint | Method | Permission | Handler | Status |
|---|----------|--------|-----------|---------|--------|
| 25 | `/api/v1/contracts/drafts` | POST | contracts:write | CreateDraft | ✅ |
| 26 | `/api/v1/contracts/drafts/{draftId}` | GET | contracts:read | GetDraft | ✅ |
| 27 | `/api/v1/contracts/drafts/list` | GET | contracts:read | ListDrafts | ✅ |
| 28 | `/api/v1/contracts/drafts/{draftId}/content` | PATCH | contracts:write | UpdateDraftContent | ✅ |
| 29 | `/api/v1/contracts/drafts/{draftId}/metadata` | PATCH | contracts:write | UpdateDraftMetadata | ✅ |
| 30 | `/api/v1/contracts/drafts/{draftId}/submit-review` | POST | contracts:write | SubmitDraftForReview | ✅ |
| 31 | `/api/v1/contracts/drafts/{draftId}/approve` | POST | contracts:write | ApproveDraft | ✅ |
| 32 | `/api/v1/contracts/drafts/{draftId}/reject` | POST | contracts:write | RejectDraft | ✅ |
| 33 | `/api/v1/contracts/drafts/{draftId}/publish` | POST | contracts:write | PublishDraft | ✅ |
| 34 | `/api/v1/contracts/drafts/generate-from-ai` | POST | contracts:write | GenerateDraftFromAi | ✅ |
| 35 | `/api/v1/contracts/drafts/{draftId}/examples` | POST | contracts:write | AddDraftExample | ✅ |

**Total: 35 endpoints — all implemented with handlers**

---

## 2. Endpoint → Use Case Mapping

All 35 endpoints map 1:1 to CQRS handlers. No dead endpoints found.

---

## 3. Dead Endpoints

**None identified.** All endpoints have corresponding CQRS handlers.

---

## 4. Incomplete Endpoints / Missing Backend Features

| # | Gap | Priority | Details |
|---|-----|----------|---------|
| BE-01 | **No SpectralRuleset CRUD endpoints** | HIGH | Frontend hooks (useSpectralRulesets, useCreateSpectralRuleset, etc.) call endpoints that don't exist |
| BE-02 | **No CanonicalEntity CRUD endpoints** | HIGH | Frontend hooks (useCanonicalEntities, useCreateCanonicalEntity, etc.) call endpoints that don't exist |
| BE-03 | **No Contract Portal read endpoint** | MEDIUM | ContractPortalPage needs a dedicated read-only API |
| BE-04 | **Import endpoint rate limiting** | LOW | Uses global policy (100 req/60s) instead of `data-intensive` (50 req/60s) |

---

## 5. Validation Review

| Handler | Validation | Status |
|---------|-----------|--------|
| ImportContract | FluentValidation on spec content format | ✅ |
| CreateContractVersion | Validates apiAssetId, semVer, protocol, specContent | ✅ |
| CreateDraft | Validates title length (200), protocol | ✅ |
| UpdateDraftContent | Validates draftId, content not empty | ✅ |
| TransitionLifecycleState | Validates valid state transition | ✅ |
| All other handlers | FluentValidation validators present | ✅ |

---

## 6. Error Handling Review

| Aspect | Status |
|--------|--------|
| Domain error catalog | ✅ `ContractsErrors.cs` with i18n codes |
| Result pattern | ✅ Handlers return Result<T> |
| Validation pipeline | ✅ FluentValidation with MediatR pipeline |
| 404 handling | ✅ Entity not found returns proper error |
| Concurrency conflict | ❌ No `DbUpdateConcurrencyException` handling (no RowVersion yet) |

---

## 7. Audit Trail Review

| Operation | Audit | Status |
|-----------|-------|--------|
| Create version | ✅ CreatedAt/By via interceptor | ✅ |
| Update version | ✅ UpdatedAt/By via interceptor | ✅ |
| Lifecycle transition | ⚠️ Via interceptor only | Should have domain event |
| Draft approval/rejection | ✅ ContractReview record | ✅ |
| Digital signature | ✅ Embedded in ContractVersion | ✅ |
| Soft delete | ✅ IsDeleted flag | ✅ |

---

## 8. Corrections Backlog

### HIGH Priority

| # | Correction | File(s) | Effort |
|---|-----------|---------|--------|
| BE-01 | Create SpectralRuleset CQRS handlers (Create, List, Get, Update, Delete, Toggle) | New files in `Application/Contracts/Features/` | 4h |
| BE-02 | Create CanonicalEntity CQRS handlers (Create, List, Get, Update, Promote) | New files in `Application/Contracts/Features/` | 4h |
| BE-03 | Add SpectralRuleset + CanonicalEntity endpoint modules | New file in `API/Contracts/Endpoints/` | 2h |
| BE-04 | Add DbSets and EF configurations for 5 unmapped entities | `ContractsDbContext.cs` + 5 new config files | 3h |
| BE-05 | Add `UseXminAsConcurrencyToken()` to ContractVersion, ContractDraft, SpectralRuleset | 3 EF config files | 30min |
| BE-06 | Handle `DbUpdateConcurrencyException` in write handlers | All write handlers | 1h |

### MEDIUM Priority

| # | Correction | File(s) | Effort |
|---|-----------|---------|--------|
| BE-07 | Add Contract Portal read endpoint | New endpoint module | 2h |
| BE-08 | Move lifecycle transition validation into ContractVersion entity | `ContractVersion.cs` | 2h |
| BE-09 | Move draft status transition validation into ContractDraft entity | `ContractDraft.cs` | 2h |
| BE-10 | Change import endpoint rate limit to `data-intensive` | `ContractsEndpointModule.cs` | 15min |

### LOW Priority

| # | Correction | File(s) | Effort |
|---|-----------|---------|--------|
| BE-11 | Add domain events for lifecycle transitions | `ContractVersion.cs` | 1h |
| BE-12 | Add integration event publishing for key actions | Handlers | 2h |
| BE-13 | Verify all IContractsModule methods are implemented | `IContractsModule.cs` | 1h |
