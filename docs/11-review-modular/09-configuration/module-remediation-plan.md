# Configuration Module — Remediation Plan

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 09 — Configuration  
> **Phase:** B1 — Module Consolidation

---

## A. Quick Wins

Small, high-value, low-effort items.

| # | Item | File(s) | Effort | Impact |
|---|------|---------|--------|--------|
| QW-01 | Add `IsEditable` check in `SetConfigurationValue` handler — reject writes to non-editable definitions | `Application/Features/SetConfigurationValue/SetConfigurationValue.cs` | 30min | Prevents modification of read-only configs |
| QW-02 | Create minimal module README | `src/modules/configuration/README.md` (new) | 3h | Onboarding from impossible to viable |
| QW-03 | Add XML docs to `ConfigurationResolutionService`, `ConfigurationCacheService`, `ConfigurationSecurityService` | `Infrastructure/Services/*.cs` | 1h | Code comprehensibility |
| QW-04 | Add `UseXminAsConcurrencyToken()` to `ConfigurationDefinitionConfiguration` and `ConfigurationEntryConfiguration` | `Infrastructure/Persistence/Configurations/*.cs` | 30min | Optimistic concurrency ready |
| QW-05 | Document inheritance model (System→Tenant→Environment→Role→Team→User) | `docs/11-review-modular/09-configuration/configuration-inheritance-model.md` (new) | 2h | Core mechanism documented |

**Total Quick Wins effort: ~7 hours**

---

## B. Functional Corrections (Mandatory)

Items the module needs to be considered functionally correct.

| # | Item | File(s) | Effort | Impact |
|---|------|---------|--------|--------|
| FC-01 | **Add value type validation** — ensure value matches `ValueType` (Boolean→true/false, Integer→parseable, Decimal→parseable, etc.) | `Application/Features/SetConfigurationValue/SetConfigurationValue.cs` | 2h | Prevents invalid values being stored |
| FC-02 | **Add `ValidationRules` enforcement** — apply JSON schema validation if `ValidationRules` is set on the definition | `Application/Features/SetConfigurationValue/SetConfigurationValue.cs` | 3h | Definition-level validation enforced at write time |
| FC-03 | **Enable seeder for all environments** — currently Development only; 345+ definitions required in Staging/Production | `Infrastructure/Seed/ConfigurationDefinitionSeeder.cs` + `ApiHost/DependencyInjection` or startup | 4h | Platform functional in non-dev environments |
| FC-04 | **Verify integration events are published** — events defined in `ConfigurationIntegrationEvents.cs` but publishing not confirmed in handlers | `Application/Features/SetConfigurationValue/`, `ToggleConfiguration/`, `RemoveOverride/` | 2h | Cross-module notification of config changes |
| FC-05 | **Add concurrency conflict handling** — catch `DbUpdateConcurrencyException` in write handlers (after QW-04 adds xmin) | All write handlers | 1h | Graceful handling of concurrent edits |
| FC-06 | **Verify i18n completeness** for pt-BR and es locales (configuration + advancedConfig namespaces) | `src/frontend/src/locales/pt-BR.json`, `es.json` | 1h | Non-English locales functional |

**Total Functional Corrections effort: ~13 hours**

---

## C. Structural Adjustments

Items related to the new persistence and modeling patterns.

| # | Item | File(s) | Effort | Impact |
|---|------|---------|--------|--------|
| SA-01 | **Add FK constraint** `cfg_entries.definition_id → cfg_definitions.id` | `ConfigurationEntryConfiguration.cs` | 30min | Referential integrity at DB level |
| SA-02 | **Add FK constraint** `cfg_audit_entries.entry_id → cfg_entries.id` | `ConfigurationAuditEntryConfiguration.cs` | 30min | Referential integrity at DB level |
| SA-03 | **Add check constraints** for `Category`, `ValueType`, and `Scope` enums | All 3 configuration files | 1h | Enum validation at DB level |
| SA-04 | **Add check constraint** `version >= 1` on `cfg_entries` | `ConfigurationEntryConfiguration.cs` | 15min | Data integrity |
| SA-05 | **Add index** on `cfg_definitions.sort_order` | `ConfigurationDefinitionConfiguration.cs` | 15min | Query performance for ordered lists |
| SA-06 | **Add index** on `cfg_entries.definition_id` | `ConfigurationEntryConfiguration.cs` | 15min | FK join performance |
| SA-07 | **Add filtered index** on `cfg_entries.is_active` | `ConfigurationEntryConfiguration.cs` | 15min | Active entry queries |
| SA-08 | **Add index** on `cfg_audit_entries.changed_by` | `ConfigurationAuditEntryConfiguration.cs` | 15min | User-based audit queries |
| SA-09 | **Evaluate** `is_deleted` on `cfg_audit_entries` — audit entries should never be soft-deleted | `ConfigurationAuditEntryConfiguration.cs` | 30min | Audit trail immutability |
| SA-10 | **Evaluate** duplicate audit columns (entity-level vs interceptor-level CreatedBy/UpdatedBy) | Domain entities + EF configurations | 1h | Eliminate redundancy |

**Total Structural Adjustments effort: ~5 hours**

---

## D. Pre-conditions for Migration Baseline

Items that must be completed before the baseline migration can be generated.

| # | Pre-condition | Dependencies | Status |
|---|-------------|-------------|--------|
| D-01 | Domain model finalized (approved in this review) | None | ✅ Done |
| D-02 | `UseXminAsConcurrencyToken()` added to Definition and Entry configurations | QW-04 | ⬜ Pending |
| D-03 | FK constraints added to Entry and AuditEntry configurations | SA-01, SA-02 | ⬜ Pending |
| D-04 | Check constraints added for enums | SA-03, SA-04 | ⬜ Pending |
| D-05 | Additional indexes added | SA-05, SA-06, SA-07, SA-08 | ⬜ Pending |
| D-06 | `is_deleted` policy decided for audit entries | SA-09 | ⬜ Pending |
| D-07 | Duplicate audit columns resolved | SA-10 | ⬜ Pending |
| D-08 | Table prefix `cfg_` confirmed on all tables | Already done | ✅ Done |
| D-09 | All EF configurations consistent with final persistence model | All SA items | ⬜ Pending |
| D-10 | Seeder adapted for all environments | FC-03 | ⬜ Pending |

**Once all D items are complete, a single `InitialCreate` baseline migration can be generated.**

---

## E. Module Closure Criteria

The Configuration module is considered **closed** when:

| # | Criterion | Status | Dependency |
|---|----------|--------|------------|
| E-01 | Module boundary clearly defined | ✅ | Part 1 (module-boundary-deep-dive.md) |
| E-02 | Domain model finalized | ✅ | Part 2 (domain-model-finalization.md) |
| E-03 | Persistence model finalized | ✅ | Part 3 (persistence-model-finalization.md) |
| E-04 | DbContext corrections identified and documented | ✅ | Part 4 (dbcontext-and-mapping-corrections.md) |
| E-05 | Seeds and defaults plan finalized | ✅ | Part 5 (seeds-and-defaults-finalization.md) |
| E-06 | Backend corrections identified | ✅ | Part 6 (backend-functional-corrections.md) |
| E-07 | Frontend corrections identified | ✅ | Part 7 (frontend-functional-corrections.md) |
| E-08 | Security and permissions reviewed | ✅ | Part 8 (security-and-permissions-review.md) |
| E-09 | Documentation plan created | ✅ | Part 9 (documentation-and-onboarding-upgrade.md) |
| E-10 | `IsEditable` enforcement added | ⬜ | QW-01 |
| E-11 | Value type validation added | ⬜ | FC-01 |
| E-12 | Seeder enabled for all environments | ⬜ | FC-03 |
| E-13 | `xmin` concurrency tokens added | ⬜ | QW-04 |
| E-14 | FK and check constraints added | ⬜ | SA-01 through SA-04 |
| E-15 | Module README created | ⬜ | QW-02 |
| E-16 | Baseline migration generated | ⬜ | All D items |
| E-17 | Test coverage maintained ≥95% | ✅ | Existing tests |
| E-18 | Module maturity ≥85% | ⬜ | All above items |

---

## Execution Priority

### Phase 1 — Quick Wins (1 day)

Execute QW-01 through QW-05. These require minimal code changes and provide immediate value.

### Phase 2 — Functional Corrections (2-3 days)

Execute FC-01 through FC-06. These make the module functionally correct and production-ready.

### Phase 3 — Structural Adjustments (1 day)

Execute SA-01 through SA-10. These prepare the EF configurations for the baseline migration.

### Phase 4 — Migration Baseline (1 day)

Once all D pre-conditions are met:
1. Generate the `InitialCreate` baseline migration
2. Validate migration applies cleanly on empty database
3. Validate migration works with existing data (if applicable)

### Total Estimated Effort

| Phase | Effort | Items |
|-------|--------|-------|
| Quick Wins | 7h | 5 items |
| Functional Corrections | 13h | 6 items |
| Structural Adjustments | 5h | 10 items |
| Migration Baseline | 8h | 1 item |
| **Total** | **~33h** (~4-5 days) | **22 items** |

---

## Reference Documents

| Document | Path |
|----------|------|
| Module Boundary | `docs/11-review-modular/09-configuration/module-boundary-deep-dive.md` |
| Domain Model | `docs/11-review-modular/09-configuration/domain-model-finalization.md` |
| Persistence Model | `docs/11-review-modular/09-configuration/persistence-model-finalization.md` |
| DbContext Corrections | `docs/11-review-modular/09-configuration/dbcontext-and-mapping-corrections.md` |
| Seeds Plan | `docs/11-review-modular/09-configuration/seeds-and-defaults-finalization.md` |
| Backend Corrections | `docs/11-review-modular/09-configuration/backend-functional-corrections.md` |
| Frontend Corrections | `docs/11-review-modular/09-configuration/frontend-functional-corrections.md` |
| Security Review | `docs/11-review-modular/09-configuration/security-and-permissions-review.md` |
| Documentation Plan | `docs/11-review-modular/09-configuration/documentation-and-onboarding-upgrade.md` |
| Architecture Decisions | `docs/architecture/architecture-decisions-final.md` |
| Module Boundary Matrix | `docs/architecture/module-boundary-matrix.md` |
| Persistence Strategy | `docs/architecture/persistence-strategy-final.md` |
| Table Prefixes | `docs/architecture/database-table-prefixes.md` |
