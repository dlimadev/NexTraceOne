# Contracts Module — Documentation and Onboarding Upgrade

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 04 — Contracts  
> **Phase:** B1 — Module Consolidation

---

## 1. Existing Documentation Status

| Document | Path | Status |
|----------|------|--------|
| `module-review.md` | `docs/11-review-modular/04-contracts/module-review.md` | ✅ Exists, 16-section format |
| `module-consolidated-review.md` | `docs/11-review-modular/04-contracts/module-consolidated-review.md` | ✅ Exists, comprehensive analysis |
| `SERVICE-CONTRACT-GOVERNANCE.md` | `docs/SERVICE-CONTRACT-GOVERNANCE.md` | ✅ Exists, product vision aligned |
| `CONTRACT-STUDIO-VISION.md` | `docs/CONTRACT-STUDIO-VISION.md` | ✅ Exists, broader than implementation |
| Frontend module README | `src/frontend/src/features/contracts/README.md` | ❌ Missing |
| Backend module README | `src/modules/catalog/README.md` (Contracts section) | ❌ Missing |
| API endpoint reference | — | ❌ Missing |

---

## 2. module-review.md Assessment

**Status:** Comprehensive and accurate. Covers all 16 standard sections. Main issues:
- References 3 broken routes (now fixed)
- Correctly identifies P0 blocker
- Good coverage of hooks, components, and business rules
- Action items are well-prioritized

**Recommendation:** Update to reflect P0 fix and add newly identified items.

---

## 3. module-consolidated-review.md Assessment

**Status:** Detailed and actionable. Includes:
- Maturity assessment (68%)
- Quick wins list
- Structural refactor recommendations
- Closure criteria

**Issues found:**
- Some entity names differ between reports (e.g., "ConfigurationValue" vs "ConfigurationEntry" pattern — but this is from a different module, not Contracts)
- Correctly identifies the physical extraction need (OI-01)

---

## 4. Missing Documentation

| # | Document | Priority | Content |
|---|----------|----------|---------|
| D-01 | **Frontend Module README** | HIGH | Architecture overview, component map, hook reference, page list, state management, i18n namespace |
| D-02 | **Backend Module README** | HIGH | Domain model, CQRS features list, endpoint reference, persistence overview |
| D-03 | **Contract Lifecycle Guide** | MEDIUM | State machine diagram, transition rules, who can do what |
| D-04 | **API Endpoint Reference** | MEDIUM | OpenAPI-style documentation for all 35 endpoints |
| D-05 | **Draft Studio Workflow Guide** | MEDIUM | End-to-end flow: create → edit → review → approve → publish |
| D-06 | **Spectral Validation Guide** | LOW | How rulesets work, how to create custom rules, enforcement modes |
| D-07 | **Module Architecture Decision Record** | LOW | Why Contracts is separate from Catalog, boundary rules |

---

## 5. Code Areas Needing Documentation

| Area | Files | Issue |
|------|-------|-------|
| ContractVersion lifecycle state machine | `Domain/Contracts/Entities/ContractVersion.cs` | No XML docs on transition rules |
| ContractDraft status flow | `Domain/Contracts/Entities/ContractDraft.cs` | No XML docs on status transitions |
| Semantic diff algorithm | `Application/Contracts/Features/ComputeSemanticDiff/` | Complex logic undocumented |
| Breaking change classification | `Application/Contracts/Features/ClassifyBreakingChange/` | Classification criteria undocumented |
| Score card generation | `Application/Contracts/Features/GenerateScorecard/` | Scoring metrics undocumented |
| AiDraftGeneratorService | `Infrastructure/Contracts/Services/AiDraftGeneratorService.cs` | AI integration logic undocumented |
| Spectral ruleset evaluation | `Application/Contracts/Features/EvaluateContractRules/` | Evaluation pipeline undocumented |
| Visual builders | `contracts/workspace/builders/*.tsx` | Complex UI components undocumented |

---

## 6. XML Docs Needed

| Class/Method | Priority | Purpose |
|-------------|----------|---------|
| `ContractVersion.TransitionTo(state)` | HIGH | Document valid transitions |
| `ContractDraft.Submit()/Approve()/Reject()/Publish()` | HIGH | Document status flow |
| `IContractVersionRepository` methods | MEDIUM | Document query semantics |
| `IContractDraftRepository` methods | MEDIUM | Document query semantics |
| `ContractsErrors` error catalog | MEDIUM | Document error codes and when they occur |
| All CQRS handler classes | LOW | Summarize use case |

---

## 7. Minimum Mandatory Documentation

| # | Document | Owner | Effort | Deliverable |
|---|----------|-------|--------|------------|
| 1 | Frontend module README | Developer | 2h | `features/contracts/README.md` |
| 2 | Backend module README | Developer | 3h | `modules/catalog/docs/contracts-module.md` or section in module README |
| 3 | Update module-review.md | Developer | 1h | Reflect P0 fix and new findings |
| 4 | XML docs on ContractVersion lifecycle | Developer | 1h | In-code documentation |
| 5 | XML docs on ContractDraft workflow | Developer | 1h | In-code documentation |

---

## 8. Onboarding Notes for New Developers

### What is the Contracts Module?

The Contracts module manages **API contract governance** in NexTraceOne. It is the source of truth for how services expose their APIs, including specifications (OpenAPI, WSDL, AsyncAPI), version history, breaking change analysis, and approval workflows.

### Key Concepts

1. **Contract Version** — A published, immutable API specification version with lifecycle management
2. **Contract Draft** — A work-in-progress specification being edited in Contract Studio
3. **Lifecycle States** — Draft → InReview → Approved → Locked → Deprecated → Sunset → Retired
4. **Spectral Rules** — Linting rules that validate contract quality
5. **Canonical Entities** — Standardized data models shared across contracts
6. **Semantic Diff** — Intelligent comparison between contract versions detecting breaking/non-breaking changes

### Architecture

- **Frontend:** `src/frontend/src/features/contracts/` — 8 pages, 12 hooks, 15+ workspace sections
- **Backend:** Currently inside `src/modules/catalog/*/Contracts/` (pending extraction to `src/modules/contracts/`)
- **Database:** `ContractsDbContext` with 7 tables (prefix will change from `ct_` to `ctr_`)
- **Communication:** Cross-module via `IContractsModule` interface + outbox integration events

### Key Files

| Purpose | File |
|---------|------|
| Domain entities | `NexTraceOne.Catalog.Domain/Contracts/Entities/*.cs` |
| CQRS handlers | `NexTraceOne.Catalog.Application/Contracts/Features/*.cs` (36 features) |
| DbContext | `NexTraceOne.Catalog.Infrastructure/Contracts/Persistence/ContractsDbContext.cs` |
| API endpoints | `NexTraceOne.Catalog.API/Contracts/Endpoints/*.cs` (2 endpoint modules) |
| Frontend API | `features/contracts/api/contracts.ts`, `contractStudio.ts` |
| Frontend hooks | `features/contracts/hooks/*.ts` |
| Frontend pages | `features/contracts/*/` (8 pages) |

### Tests

- Backend: Part of catalog module tests (~430 tests total)
- Frontend: ~82 tests covering components and hooks

---

## 9. Documentation Plan

### Phase 1 — Immediate (1 day)

| # | Action | Effort |
|---|--------|--------|
| 1 | Update module-review.md with P0 fix status | 1h |
| 2 | Create minimal frontend README | 2h |

### Phase 2 — Short-term (3 days)

| # | Action | Effort |
|---|--------|--------|
| 3 | Create backend module documentation | 3h |
| 4 | Add XML docs to ContractVersion and ContractDraft | 2h |
| 5 | Create lifecycle state machine diagram | 1h |

### Phase 3 — Follow-up

| # | Action | Effort |
|---|--------|--------|
| 6 | Create API endpoint reference | 3h |
| 7 | Create Draft Studio workflow guide | 2h |
| 8 | Document Spectral validation pipeline | 1h |
