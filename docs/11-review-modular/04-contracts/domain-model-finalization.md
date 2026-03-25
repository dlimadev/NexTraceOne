# Contracts Module — Domain Model Finalization

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 04 — Contracts  
> **Phase:** B1 — Module Consolidation

---

## 1. Aggregates

| Aggregate Root | File | Responsibility |
|---------------|------|---------------|
| `ContractVersion` | `Domain/Contracts/Entities/ContractVersion.cs` | Published contract versions with lifecycle, signatures, provenance |
| `ContractDraft` | `Domain/Contracts/Entities/ContractDraft.cs` | Draft contracts under editing in Contract Studio |
| `SpectralRuleset` | `Domain/Contracts/Entities/SpectralRuleset.cs` | Spectral linting rule configurations |
| `CanonicalEntity` | `Domain/Contracts/Entities/CanonicalEntity.cs` | Canonical/standardized entity models |

---

## 2. Entities (13 total)

| Entity | Type | Parent Aggregate | DbSet Mapped | File |
|--------|------|-----------------|-------------|------|
| `ContractVersion` | Aggregate Root | Self | ✅ `ContractVersions` | `Entities/ContractVersion.cs` |
| `ContractDraft` | Aggregate Root | Self | ✅ `Drafts` | `Entities/ContractDraft.cs` |
| `ContractDiff` | Entity | ContractVersion | ✅ `ContractDiffs` | `Entities/ContractDiff.cs` |
| `ContractReview` | Entity | ContractDraft | ✅ `Reviews` | `Entities/ContractReview.cs` |
| `ContractExample` | Entity | ContractDraft/Version | ✅ `Examples` | `Entities/ContractExample.cs` |
| `ContractArtifact` | Entity | ContractVersion | ✅ `ContractArtifacts` | `Entities/ContractArtifact.cs` |
| `ContractRuleViolation` | Entity | ContractVersion | ✅ `ContractRuleViolations` | `Entities/ContractRuleViolation.cs` |
| `SpectralRuleset` | Aggregate Root | Self | ❌ **MISSING** | `Entities/SpectralRuleset.cs` |
| `CanonicalEntity` | Aggregate Root | Self | ❌ **MISSING** | `Entities/CanonicalEntity.cs` |
| `ContractLock` | Entity | ContractVersion | ❌ **MISSING** | `Entities/ContractLock.cs` |
| `ContractScorecard` | Entity | ContractVersion | ❌ **MISSING** | `Entities/ContractScorecard.cs` |
| `ContractEvidencePack` | Entity | ContractVersion | ❌ **MISSING** | `Entities/ContractEvidencePack.cs` |
| `OpenApiSchema` | Entity | ContractVersion | ❌ **MISSING** | `Entities/OpenApiSchema.cs` |

**6 entities are not yet mapped in ContractsDbContext** — this is a key gap.

---

## 3. Value Objects (15 total)

| Value Object | Used By | Purpose |
|-------------|---------|---------|
| `SemanticVersion` | ContractVersion | Semantic version representation (Major.Minor.Patch) |
| `ContractSignature` | ContractVersion | Digital signature (owned entity embedded in ContractVersion) |
| `ContractProvenance` | ContractVersion | Origin tracking metadata (owned entity) |
| `ContractOperation` | ContractVersion | Operations/endpoints in a contract |
| `ContractSchemaElement` | ContractVersion | Schema elements in a contract |
| `ContractCanonicalModel` | ContractVersion | Normalized contract model |
| `InteroperabilityProfile` | ContractVersion | Interoperability capabilities |
| `ChangeEntry` | ContractDiff | Individual change record |
| `CompatibilityAssessment` | ContractDiff | Compatibility analysis result |
| `SchemaEvolutionRule` | SpectralRuleset | Rules for schema evolution |
| `SchemaRegistryBinding` | SpectralRuleset | Binding to schema registries |
| `SpectralBindingScope` | SpectralRuleset | Scope for rule binding |
| `ValidationIssue` | ContractRuleViolation | Individual validation issue |
| `ValidationSummary` | ContractVersion | Summary of validation results |
| `CanonicalUsageReference` | CanonicalEntity | References to canonical uses |

---

## 4. Enums (12 total, all persisted as strings)

| Enum | Values | Used By |
|------|--------|---------|
| `ContractProtocol` | OpenAPI, Swagger, WSDL, AsyncAPI, etc. | ContractVersion, ContractDraft |
| `ContractType` | REST, SOAP, AsyncAPI, etc. | ContractVersion |
| `ContractLifecycleState` | Draft, InReview, Approved, Locked, Deprecated, Sunset, Retired | ContractVersion |
| `DraftStatus` | Editing, InReview, Approved, Published, Rejected | ContractDraft |
| `ReviewDecision` | Approve, Reject, RequestChanges | ContractReview |
| `ContractArtifactType` | Types of generated artifacts | ContractArtifact |
| `ValidationSeverity` | ERROR, WARNING, INFO | ContractRuleViolation |
| `CanonicalEntityState` | States for canonical entities | CanonicalEntity |
| `KafkaSchemaCompatibility` | Compatibility modes | ContractVersion (Kafka) |
| `SpectralEnforcementBehavior` | Enforcement behavior for linting | SpectralRuleset |
| `SpectralExecutionMode` | Execution modes | SpectralRuleset |
| `SpectralRulesetOrigin` | Built-in, Custom, External | SpectralRuleset |

---

## 5. Internal Entity Relationships

```
ContractVersion (Aggregate Root)
  ├── 1:N → ContractDiff (diffs referencing this version)
  ├── 1:N → ContractArtifact (generated artifacts)
  ├── 1:N → ContractExample (examples for published version)
  ├── 1:N → ContractRuleViolation (validation violations)
  ├── 1:1 → ContractLock (optional lock state)
  ├── 1:1 → ContractScorecard (quality scorecard)
  ├── 1:1 → ContractEvidencePack (compliance evidence)
  ├── Owned → ContractSignature (embedded digital signature)
  └── Owned → ContractProvenance (embedded origin metadata)

ContractDraft (Aggregate Root)
  ├── 1:N → ContractReview (review records)
  └── 1:N → ContractExample (examples for draft)

SpectralRuleset (Aggregate Root, standalone)
  └── 1:N → ContractRuleViolation (violations referencing ruleset)

CanonicalEntity (Aggregate Root, standalone)
```

---

## 6. Cross-Module Relationships

| This Module | References | Other Module | Type |
|------------|-----------|-------------|------|
| `ContractVersion.ApiAssetId` | → `ApiAsset.Id` | Catalog | Guid FK reference (no navigation property) |
| `IContractsModule` interface | ← queried by | Governance, DeveloperPortal | Cross-module query |
| Integration events | → consumed by | Change Governance, Operational Intelligence | Outbox pattern |

---

## 7. Anemic Entity Assessment

| Entity | Assessment | Notes |
|--------|-----------|-------|
| ContractVersion | **Rich** | Has lifecycle transitions, signing, locking behavior |
| ContractDraft | **Rich** | Has status transitions, review submission, publication |
| ContractDiff | **Adequate** | Primarily data holder with change classification |
| ContractReview | **Adequate** | Decision recording with validation |
| ContractExample | **Adequate** | Content entity with format validation |
| ContractArtifact | **Adequate** | Generated content entity |
| ContractRuleViolation | **Adequate** | Validation result entity |
| SpectralRuleset | **Needs review** | May need richer behavior for enforcement logic |
| CanonicalEntity | **Needs review** | May need promotion/deprecation behavior |
| ContractLock | **Thin** | Simple lock state — acceptable |
| ContractScorecard | **Thin** | Generated result — acceptable |
| ContractEvidencePack | **Thin** | Generated result — acceptable |

---

## 8. Business Rules Location Assessment

| Rule | Current Location | Correct? |
|------|-----------------|----------|
| Lifecycle state transitions | Handler (TransitionLifecycleState) | ⚠️ Should be in entity |
| Draft status transitions | Handler (SubmitDraftForReview, etc.) | ⚠️ Should be in entity |
| Version immutability after signing | Handler (SignContractVersion) | ⚠️ Should be entity invariant |
| Breaking change requires review | Handler (ClassifyBreakingChange) | ✅ Correct (domain service) |
| Semantic version suggestion | Handler (SuggestSemanticVersion) | ✅ Correct (domain service) |
| Spectral rule evaluation | Handler (EvaluateContractRules) | ✅ Correct (domain service) |

---

## 9. Missing Fields

| Entity | Missing Field | Rationale | Priority |
|--------|-------------|-----------|----------|
| ContractVersion | `RowVersion` (uint/xmin) | Optimistic concurrency for concurrent edits | HIGH |
| ContractDraft | `RowVersion` (uint/xmin) | Optimistic concurrency for studio editing | HIGH |
| SpectralRuleset | `RowVersion` (uint/xmin) | Optimistic concurrency | MEDIUM |

---

## 10. Unnecessary Fields

No unnecessary fields identified. All current fields serve clear purposes.

---

## 11. Final Domain Model

The current domain model is **approved as final** with these required additions:

### Must Add to ContractsDbContext
1. `DbSet<SpectralRuleset> SpectralRulesets`
2. `DbSet<CanonicalEntity> CanonicalEntities`
3. `DbSet<ContractLock> ContractLocks`
4. `DbSet<ContractScorecard> ContractScorecards`
5. `DbSet<ContractEvidencePack> ContractEvidencePacks`

### Must Add to Entities
1. `RowVersion` (xmin) on ContractVersion, ContractDraft, SpectralRuleset

### Must Add EF Core Configurations
1. `SpectralRulesetConfiguration.cs`
2. `CanonicalEntityConfiguration.cs`
3. `ContractLockConfiguration.cs`
4. `ContractScorecardConfiguration.cs`
5. `ContractEvidencePackConfiguration.cs`

### Business Rule Improvement (recommended)
1. Move lifecycle state transition validation into `ContractVersion` entity
2. Move draft status transition validation into `ContractDraft` entity

**The domain model is ready for the migration baseline once DbSet gaps are filled.**
