# Catalog — Current State

**Maturity:** READY (91.7% real)
**Last verified:** March 2026 — Forensic Audit
**Source:** `docs/audit-forensic-2026-03/backend-state-report.md §Catalog`, `docs/audit-forensic-2026-03/frontend-state-report.md §Catalog`

---

## DbContexts

| DbContext | Migrations | Status |
|---|---|---|
| ContractsDbContext | Confirmed (with snapshot) | READY |
| CatalogGraphDbContext | Confirmed (with snapshot) | READY |
| DeveloperPortalDbContext | Confirmed (with snapshot) | READY |

Table prefix: `cat_` / `ctr_`

---

## Features

| Area | Count | Real | Stubs | Notes |
|---|---|---|---|---|
| Graph / Service Catalog | 27 | 27 | 0 | RegisterServiceAsset, ImportFromBackstage, ListServices, GetAssetGraph, CreateGraphSnapshot |
| Contracts (REST/SOAP/Event/Background) | 35 | 35 | 0 | CreateContractVersion, PublishDraft, SignContractVersion, GenerateScorecard, EvaluateCompatibility, ComputeSemanticDiff |
| Developer Portal | 22 | 15 | 7 | RecordAnalyticsEvent, CreateSubscription, ExecutePlayground real; SearchCatalog, RenderOpenApiContract, GetApiHealth, GetMyApis, GetApisIConsume, GetApiDetail, GetAssetTimeline are intentional stubs |
| **Total** | **84** | **77** | **7** | **91.7% real** |

---

## Frontend Pages

| Page | Status |
|---|---|
| ServiceCatalogListPage, ServiceCatalogPage, ServiceDetailPage | READY — API real |
| ContractsPage, ContractListPage, ContractDetailPage | READY — API real |
| SourceOfTruthExplorerPage, ServiceSourceOfTruthPage, ContractSourceOfTruthPage | READY — API real |
| CatalogContractsConfigurationPage | READY — API real |
| DeveloperPortalPage | PARTIAL — 7 stubs backend |
| GlobalSearchPage | PARTIAL — GlobalSearch real; SearchCatalog stub (cross-module) |

---

## Key Gaps

- `SearchCatalog`, `RenderOpenApiContract`, `GetApiHealth`, `GetMyApis`, `GetApisIConsume`, `GetApiDetail`, `GetAssetTimeline` — stubs awaiting `IContractsModule` cross-module interface implementation
- `IContractsModule` cross-module interface: defined, 0 implementations
- Developer Portal 7 stubs: awaiting cross-module integration
- Contract Studio UX needs polish

---

## Cross-Module Interface Status

| Interface | Status |
|---|---|
| `IContractsModule` | PLAN — defined, not implemented |

---

*Source: `docs/audit-forensic-2026-03/backend-state-report.md`, `docs/audit-forensic-2026-03/capability-gap-matrix.md`*
