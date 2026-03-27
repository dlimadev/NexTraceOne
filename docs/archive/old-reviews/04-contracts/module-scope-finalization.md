# Contracts Module — Scope Finalization

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 04 — Contracts  
> **Phase:** B1 — Module Consolidation

---

## 1. Existing Functionality

### Fully Implemented (36 CQRS features + 8 frontend pages)

| Capability | Backend Features | Frontend Pages | Status |
|-----------|-----------------|---------------|--------|
| Contract catalog & listing | ListContracts, ListContractsByService, SearchContracts, GetContractsSummary | ContractCatalogPage | ✅ Complete |
| Contract import | ImportContract | CreateServicePage (import mode) | ✅ Complete |
| Contract creation (wizard) | CreateContractVersion | CreateServicePage (visual mode) | ✅ Complete |
| Contract detail & workspace | GetContractVersionDetail | ContractWorkspacePage (15 sections) | ✅ Complete |
| Version history | GetContractHistory | VersioningSection | ✅ Complete |
| Semantic diff | ComputeSemanticDiff | (via API) | ✅ Complete |
| Breaking change classification | ClassifyBreakingChange | (via API) | ✅ Complete |
| Compatibility assessment | GetCompatibilityAssessment | (via API) | ✅ Complete |
| Version suggestion | SuggestSemanticVersion | (via API) | ✅ Complete |
| Lifecycle transitions | TransitionLifecycleState | (via workspace actions) | ✅ Complete |
| Contract deprecation | DeprecateContractVersion | (via workspace actions) | ✅ Complete |
| Contract locking | LockContractVersion | (via workspace actions) | ✅ Complete |
| Digital signatures | SignContractVersion, VerifySignature | (via workspace actions) | ✅ Complete |
| Contract export | ExportContract | (via workspace actions) | ✅ Complete |
| Contract sync | SyncContracts | (via API) | ✅ Complete |
| Validation (integrity) | ValidateContractIntegrity | ValidationSection | ✅ Complete |
| Rule violations | EvaluateContractRules, ListRuleViolations | ValidationSection | ✅ Complete |
| Scorecard generation | GenerateScorecard | ComplianceSection | ✅ Complete |
| Evidence pack generation | GenerateEvidencePack | ComplianceSection | ✅ Complete |
| Draft Studio | CreateDraft, GetDraft, ListDrafts, UpdateDraftContent, UpdateDraftMetadata | DraftStudioPage | ✅ Complete |
| Draft review workflow | SubmitDraftForReview, ApproveDraft, RejectDraft | ApprovalsSection | ✅ Complete |
| Draft publication | PublishDraft | (via studio actions) | ✅ Complete |
| Draft examples | AddDraftExample | (via studio) | ✅ Complete |
| AI-assisted generation | GenerateDraftFromAi | CreateServicePage | ✅ Complete |
| Contract governance dashboard | — | ContractGovernancePage | ✅ Complete (route fixed) |
| Spectral ruleset management | — (frontend hooks exist) | SpectralRulesetManagerPage | ⚠️ Frontend only |
| Canonical entity management | — (frontend hooks exist) | CanonicalEntityCatalogPage | ⚠️ Frontend only |
| Contract portal (read-only) | — | ContractPortalPage | ⚠️ Frontend only |

---

## 2. Partially Implemented Functionality

| Feature | Frontend | Backend | Gap |
|---------|----------|---------|-----|
| Spectral ruleset CRUD | ✅ Page + hooks (`useSpectralRulesets`, `useCreateSpectralRuleset`, etc.) | ⚠️ Entity exists (`SpectralRuleset`), DbSet missing, no CQRS handlers | **Need backend CRUD endpoints** |
| Canonical entity CRUD | ✅ Page + hooks (`useCanonicalEntities`, `useCreateCanonicalEntity`, etc.) | ⚠️ Entity exists (`CanonicalEntity`), DbSet missing, no CQRS handlers | **Need backend CRUD endpoints** |
| Contract portal | ✅ Page (`ContractPortalPage`) | ⚠️ Read-only endpoint via `IContractsModule` | **Need dedicated read endpoint** |
| Contract locking | ⚠️ Lock entity exists | ✅ `LockContractVersion` handler exists | DbSet for `ContractLock` not in ContractsDbContext |
| Contract scorecard | ⚠️ Scorecard entity exists | ✅ `GenerateScorecard` handler exists | DbSet for `ContractScorecard` not in ContractsDbContext |
| Contract evidence pack | ⚠️ Evidence entity exists | ✅ `GenerateEvidencePack` handler exists | DbSet for `ContractEvidencePack` not in ContractsDbContext |

---

## 3. Missing but Mandatory Functionality

| Feature | Priority | Rationale |
|---------|----------|-----------|
| Spectral ruleset backend CRUD | HIGH | Frontend page exists but calls non-existent endpoints |
| Canonical entity backend CRUD | HIGH | Frontend page exists but calls non-existent endpoints |
| SpectralRuleset DbSet in ContractsDbContext | HIGH | Entity exists in Domain but not mapped |
| CanonicalEntity DbSet in ContractsDbContext | HIGH | Entity exists in Domain but not mapped |
| ContractLock DbSet in ContractsDbContext | MEDIUM | Entity exists but not persisted |
| ContractScorecard DbSet in ContractsDbContext | MEDIUM | Entity exists but not persisted |
| ContractEvidencePack DbSet in ContractsDbContext | MEDIUM | Entity exists but not persisted |
| RowVersion/concurrency on ContractVersion | HIGH | Concurrent edits can silently overwrite |
| Contract portal read endpoint | MEDIUM | Portal page needs dedicated API |

---

## 4. Protocol Support Assessment

| Protocol | Import | Create | Diff | Validate | Export | Status |
|----------|--------|--------|------|----------|--------|--------|
| OpenAPI 3.x | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ Complete |
| Swagger 2.0 | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ Complete |
| AsyncAPI | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ Complete |
| WSDL/SOAP | ✅ | ✅ | ⚠️ | ⚠️ | ✅ | ⚠️ Partial |

---

## 5. Governance Capability Assessment

| Capability | Status | Notes |
|-----------|--------|-------|
| Contract diff | ✅ | Semantic diff with breaking/non-breaking/additive classification |
| Contract versioning | ✅ | Semantic versioning with history |
| Lifecycle management | ✅ | Draft → InReview → Approved → Locked → Deprecated → Sunset → Retired |
| Validation / lint / rulesets | ⚠️ | Spectral evaluation works, but ruleset management needs backend CRUD |
| Review / approval | ✅ | Full workflow: Submit → Approve/Reject → Publish |
| History / evidence | ✅ | Full version history + evidence pack generation |
| Digital signatures | ✅ | Sign and verify contract versions |
| Compliance scoring | ✅ | Scorecard generation with scoring metrics |

---

## 6. Minimum Complete Module Definition

For the Contracts module to be considered **functionally complete**, the following must be resolved:

### Must Have (blocks closure)

1. ✅ All 8 pages routed and accessible (FIXED in this phase)
2. ⬜ SpectralRuleset backend CRUD (create, read, update, delete, toggle)
3. ⬜ CanonicalEntity backend CRUD (create, read, update, promote)
4. ⬜ SpectralRuleset and CanonicalEntity added to ContractsDbContext
5. ⬜ RowVersion on ContractVersion and ContractDraft
6. ⬜ ContractLock, ContractScorecard, ContractEvidencePack added to ContractsDbContext

### Should Have (improves quality)

7. ⬜ Contract portal dedicated read endpoint
8. ⬜ Check constraints for enums at database level
9. ⬜ Filtered indexes with `WHERE IsDeleted = false`
10. ⬜ Rate limiting corrected for import endpoint

### Nice to Have (polish)

11. ⬜ Legacy pages in `catalog/pages/` removed
12. ⬜ i18n verified for all 4 locales on all pages
13. ⬜ Module documentation complete
