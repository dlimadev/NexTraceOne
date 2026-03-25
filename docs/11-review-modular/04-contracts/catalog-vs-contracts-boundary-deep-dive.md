# Contracts Module — Catalog vs Contracts Boundary Deep Dive

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 04 — Contracts  
> **Phase:** B1 — Module Consolidation

---

## 1. Current Responsibilities of Catalog Related to Contracts

The Catalog module (`src/modules/catalog/`) currently houses:

| Responsibility | Location | Should Stay in Catalog? |
|---------------|----------|------------------------|
| Service registry (services, APIs, events) | `NexTraceOne.Catalog.Domain/` (root entities) | ✅ YES |
| Service metadata and classifications | `NexTraceOne.Catalog.Domain/` | ✅ YES |
| Dependency graph / topology | `NexTraceOne.Catalog.Domain/Graph/` | ✅ YES |
| Developer portal (read-only view) | `NexTraceOne.Catalog.Domain/Portal/`, `API/Portal/` | ✅ YES |
| **Contract lifecycle management** | `NexTraceOne.Catalog.Domain/Contracts/` | ❌ Should be in Contracts |
| **Contract versioning & schemas** | `NexTraceOne.Catalog.Domain/Contracts/Entities/` | ❌ Should be in Contracts |
| **Contract draft studio** | `NexTraceOne.Catalog.Application/Contracts/Features/` | ❌ Should be in Contracts |
| **Contract validation & Spectral** | `NexTraceOne.Catalog.Application/Contracts/Features/` | ❌ Should be in Contracts |
| **ContractsDbContext** | `NexTraceOne.Catalog.Infrastructure/Contracts/Persistence/` | ❌ Should be in Contracts |
| **Contract API endpoints** | `NexTraceOne.Catalog.API/Contracts/Endpoints/` | ❌ Should be in Contracts |

---

## 2. Current Responsibilities of Contracts Module

The Contracts module currently exists only as:
- **Frontend:** `src/frontend/src/features/contracts/` (fully functional, 69 files)
- **Backend:** Subdirectory within Catalog (`src/modules/catalog/*/Contracts/`)
- **No dedicated `src/modules/contracts/` project yet** (pending extraction — OI-01)

The backend Contracts code within Catalog manages:

| Responsibility | CQRS Features | Status |
|---------------|--------------|--------|
| Contract import & creation | ImportContract, CreateContractVersion | ✅ Implemented |
| Contract versioning | ListContracts, GetContractHistory, GetContractVersionDetail | ✅ Implemented |
| Semantic diff & compatibility | ComputeSemanticDiff, ClassifyBreakingChange, GetCompatibilityAssessment | ✅ Implemented |
| Lifecycle management | TransitionLifecycleState, DeprecateContractVersion, LockContractVersion | ✅ Implemented |
| Draft Studio | CreateDraft, UpdateDraftContent, UpdateDraftMetadata, ListDrafts | ✅ Implemented |
| Review & approval workflow | SubmitDraftForReview, ApproveDraft, RejectDraft, PublishDraft | ✅ Implemented |
| Spectral validation | EvaluateContractRules, ListRuleViolations | ✅ Implemented |
| Digital signatures | SignContractVersion, VerifySignature | ✅ Implemented |
| Contract export | ExportContract (multiple formats) | ✅ Implemented |
| Scorecard & evidence | GenerateScorecard, GenerateEvidencePack | ✅ Implemented |
| AI-assisted generation | GenerateDraftFromAi, AddDraftExample | ✅ Implemented |
| Canonical entities | (Domain entity exists, no CQRS features) | ⚠️ Partial |
| Spectral ruleset management | (Domain entity exists, no CQRS features) | ⚠️ Partial |

---

## 3. What Is Still Improperly Inside Catalog

The entire Contracts backend is currently a subdomain within Catalog. These must be extracted:

| Layer | Path Inside Catalog | Target Path After Extraction |
|-------|-------------------|------------------------------|
| Domain Entities | `NexTraceOne.Catalog.Domain/Contracts/` | `src/modules/contracts/NexTraceOne.Contracts.Domain/` |
| Domain Enums | `NexTraceOne.Catalog.Domain/Contracts/Enums/` | `src/modules/contracts/NexTraceOne.Contracts.Domain/Enums/` |
| Domain VOs | `NexTraceOne.Catalog.Domain/Contracts/ValueObjects/` | `src/modules/contracts/NexTraceOne.Contracts.Domain/ValueObjects/` |
| Domain Errors | `NexTraceOne.Catalog.Domain/Contracts/Errors/` | `src/modules/contracts/NexTraceOne.Contracts.Domain/Errors/` |
| Application Features | `NexTraceOne.Catalog.Application/Contracts/Features/` (36 features) | `src/modules/contracts/NexTraceOne.Contracts.Application/Features/` |
| Application Abstractions | `NexTraceOne.Catalog.Application/Contracts/Abstractions/` | `src/modules/contracts/NexTraceOne.Contracts.Application/Abstractions/` |
| Infrastructure Persistence | `NexTraceOne.Catalog.Infrastructure/Contracts/Persistence/` | `src/modules/contracts/NexTraceOne.Contracts.Infrastructure/Persistence/` |
| Infrastructure Services | `NexTraceOne.Catalog.Infrastructure/Contracts/Services/` | `src/modules/contracts/NexTraceOne.Contracts.Infrastructure/Services/` |
| API Endpoints | `NexTraceOne.Catalog.API/Contracts/Endpoints/` | `src/modules/contracts/NexTraceOne.Contracts.API/Endpoints/` |
| Public Contracts | `NexTraceOne.Catalog.Contracts/Contracts/` | `src/modules/contracts/NexTraceOne.Contracts.Contracts/` |

**Note:** This extraction is documented as OI-01 in `docs/architecture/phase-a-open-items.md` but is NOT in scope for this phase. This phase prepares the module to be extraction-ready.

---

## 4. What Should Remain as Catalog Reference Only

| Catalog Concept | Relationship to Contracts | How Contracts References It |
|----------------|--------------------------|---------------------------|
| `ApiAsset` (service/API) | A contract belongs to an API asset | Via `ApiAssetId` (Guid FK reference, not navigation property) |
| `Service` | Services group API assets | Indirect — through ApiAsset |
| `ServiceType` | Classification of services | Read-only reference for UI display |
| `DependencyGraph` | Service topology | Not directly referenced by Contracts |
| `DeveloperPortal` | Portal reads contract data | Via `IContractsModule` interface (cross-module query) |

---

## 5. Clear Ownership Rules

### What Belongs to the **Asset** (Catalog)
- Service metadata (name, description, owner, team, tags)
- Service classification (type, protocol, technology)
- Service dependencies and topology
- Service health records
- Consumer/provider relationships at the service level
- Service discovery and registration

### What Belongs to the **Contract** (Contracts)
- Contract specification content (OpenAPI, WSDL, AsyncAPI, etc.)
- Contract versions and lifecycle states
- Semantic versioning and diff analysis
- Breaking change classification
- Spectral validation rules and violations
- Contract review and approval workflow
- Draft management (Contract Studio)
- Digital signatures and integrity verification
- Contract export and format conversion
- Compliance scoring and evidence packs
- Canonical entity management
- Contract provenance tracking

---

## 6. Relationship: Catalog Asset → Contract

```
┌──────────────────────────┐        ┌────────────────────────────┐
│       CATALOG             │        │       CONTRACTS             │
│                          │        │                            │
│  Service                 │  1:N   │  ContractVersion           │
│    └─ ApiAsset ─────────────────►│    ├─ SpecContent          │
│       (ServiceId,        │        │    ├─ Protocol             │
│        Name, Type,       │        │    ├─ SemVer              │
│        Protocol)         │        │    ├─ LifecycleState      │
│                          │        │    ├─ Signature           │
│                          │        │    └─ Provenance          │
│                          │        │                            │
│  DependencyGraph         │        │  ContractDraft             │
│  DeveloperPortal ◄──────query────│  ContractDiff              │
│                          │        │  ContractReview            │
│                          │        │  ContractArtifact          │
│                          │        │  ContractRuleViolation     │
│                          │        │  SpectralRuleset           │
│                          │        │  CanonicalEntity           │
└──────────────────────────┘        └────────────────────────────┘
```

**Cross-module reference:** `ContractVersion.ApiAssetId → Catalog.ApiAsset.Id`  
**Communication:** Via `IContractsModule` interface and integration events (outbox pattern)

---

## 7. What Must NEVER Be in Catalog

- Contract specification content or schemas
- Contract lifecycle state management
- Contract review/approval workflow logic
- Spectral validation rules or results
- Contract diff/compatibility analysis
- Contract digital signatures
- Contract Studio draft management
- Canonical entity definitions

---

## 8. What Must NEVER Be in Contracts

- Service registration or metadata management
- Service dependency graph or topology
- Service health monitoring
- Team/ownership assignment at service level
- Service discovery or consumer registry (these are Catalog responsibilities)
- Configuration definitions (Configuration module)
- Change governance workflows (Change Governance module)

---

## 9. Concrete Examples

### Example 1: "Register a new REST API"
- **Catalog:** Creates `ApiAsset(name="Orders API", protocol=REST, serviceId=...)`
- **Contracts:** NOT involved at this stage

### Example 2: "Import an OpenAPI spec for Orders API"
- **Catalog:** Provides `ApiAssetId` reference
- **Contracts:** Creates `ContractVersion(apiAssetId, specContent=openapi_yaml, protocol=OpenAPI, semVer=1.0.0)`

### Example 3: "Check if a new version has breaking changes"
- **Catalog:** NOT involved
- **Contracts:** `ComputeSemanticDiff(previousVersionId, newVersionId)` → returns `ContractDiff` with `ChangeLevel`

### Example 4: "Approve a contract draft for publication"
- **Catalog:** NOT involved
- **Contracts:** `SubmitDraftForReview → ApproveDraft → PublishDraft` workflow

### Example 5: "View all contracts for a service in the developer portal"
- **Catalog:** DeveloperPortal queries `IContractsModule.HasContractVersionAsync(apiAssetId)`
- **Contracts:** Returns contract data via the interface

---

## 10. Boundary Verdict

| Aspect | Status |
|--------|--------|
| Module scope | **CLEAR** — well-defined bounded context |
| Entity ownership | **CLEAN** — all 13 entities are contract-domain entities |
| Cross-module reference | **CORRECT** — only `ApiAssetId` as a Guid reference, no navigation properties |
| Communication pattern | **CORRECT** — `IContractsModule` interface + integration events |
| Physical extraction readiness | **READY** — code is already organized in `/Contracts/` subdirectories within each Catalog layer |

**Conclusion:** The boundary between Catalog and Contracts is conceptually clear. The main action item is the physical extraction (OI-01) which is out of scope for this phase but fully prepared for.
