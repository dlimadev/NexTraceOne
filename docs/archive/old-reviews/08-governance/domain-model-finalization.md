# Governance Module — Domain Model Finalization

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 08 — Governance  
> **Phase:** B1 — Module Consolidation

---

## 1. Aggregate Roots

| Aggregate Root | File | Responsibility |
|---------------|------|---------------|
| `Team` | `Domain/Entities/Team.cs` | Organizational team unit — owns team metadata, status, membership references |
| `GovernanceDomain` | `Domain/Entities/GovernanceDomain.cs` | Organizational governance domain — groups services/teams under a business domain |
| `GovernancePack` | `Domain/Entities/GovernancePack.cs` | Governance rule pack — contains versioned governance rules for compliance |
| `GovernanceWaiver` | `Domain/Entities/GovernanceWaiver.cs` | Compliance exception — formal waiver of a governance rule |
| `DelegatedAdministration` | `Domain/Entities/DelegatedAdministration.cs` | Administrative delegation — grants delegated admin rights |

---

## 2. Entities (after extraction — 9 remain in Governance)

| Entity | Type | Parent | DbSet | Status |
|--------|------|--------|-------|--------|
| `Team` | Aggregate Root | — | ✅ `Teams` | ✅ Mapped |
| `TeamDomainLink` | Entity | Team/Domain | ✅ `TeamDomainLinks` | ✅ Mapped |
| `GovernanceDomain` | Aggregate Root | — | ✅ `Domains` | ✅ Mapped |
| `GovernancePack` | Aggregate Root | — | ✅ `Packs` | ✅ Mapped |
| `GovernancePackVersion` | Entity | GovernancePack | ✅ `PackVersions` | ✅ Mapped |
| `GovernanceRolloutRecord` | Entity | GovernancePack | ✅ `RolloutRecords` | ✅ Mapped |
| `GovernanceRuleBinding` | Entity | GovernancePack | ❌ No DbSet | ⚠️ **NEEDS mapping** |
| `GovernanceWaiver` | Aggregate Root | — | ✅ `Waivers` | ✅ Mapped |
| `DelegatedAdministration` | Aggregate Root | — | ✅ `DelegatedAdministrations` | ✅ Mapped |

### Entities to REMOVE from Governance (extract to other modules)

| Entity | Target Module | Reason |
|--------|-------------|--------|
| `IntegrationConnector` | Integrations | Connector management is not governance |
| `IngestionSource` | Integrations | Data source tracking is not governance |
| `IngestionExecution` | Integrations | Execution history is not governance |
| `AnalyticsEvent` | Product Analytics | Usage analytics is not governance |

---

## 3. Value Objects

No explicit value object files found in the governance domain. Governance uses:
- Enum-based classification (policy categories, risk levels, maturity levels)
- JSON-stored complex data (`AllowedTeams` in rules, `ConfigurationJson` in policies)
- String-based identifiers and labels

**Recommendation:** Consider introducing value objects for:
- `ComplianceScore` (percentage + grade)
- `RiskRating` (level + dimension + score)
- `MaturityScore` (level + category + score)

---

## 4. Enums (after extraction — ~31 remain in Governance)

| Category | Enums | Count |
|----------|-------|-------|
| **Pack Lifecycle** | GovernancePackStatus (Draft, Published, Deprecated, Archived) | 1 |
| **Rule Classification** | GovernanceRuleCategory, GovernanceScopeType, GovernanceMaturity | 3 |
| **Compliance** | ComplianceStatus, ComplianceCheckStatus, PolicyStatus, PolicySeverity, PolicyEnforcementMode, PolicyCategory, EvidenceType, EvidencePackageStatus | 8 |
| **Risk** | RiskLevel, RiskDimension, ControlDimension, DeploymentReadinessLevel | 4 |
| **Waiver** | WaiverStatus | 1 |
| **Delegation** | DelegationScope, OwnershipType | 2 |
| **Rollout** | DeploymentMode, RolloutStatus | 2 |
| **Maturity** | MaturityLevel, DomainCriticality | 2 |
| **Team/Platform** | TeamStatus, PlatformSubsystemStatus, BackgroundJobStatus, EfficiencyCategory, CostEfficiency, CostDimension, TrendDirection, SimulationStatus, EnforcementMode, PlatformEventSeverity | ~8 |
| **Total in Governance** | | **~31** |

### Enums to REMOVE (extract)

| Enum | Target Module |
|------|-------------|
| ConnectorStatus, ConnectorHealth | Integrations |
| SourceStatus, SourceTrustLevel, FreshnessStatus, ExecutionResult | Integrations |
| AnalyticsEventType, ProductModule, WasteSignalType, FrictionSignalType, ValueMilestoneType, JourneyStatus | Product Analytics |

---

## 5. Internal Entity Relationships

```
Team (Aggregate Root)
  └── N:M → GovernanceDomain (via TeamDomainLink)

GovernanceDomain (Aggregate Root)
  └── N:M → Team (via TeamDomainLink)

GovernancePack (Aggregate Root)
  ├── 1:N → GovernancePackVersion (versioned rule definitions)
  ├── 1:N → GovernanceRolloutRecord (rollout tracking per version)
  ├── 1:N → GovernanceRuleBinding (rule-to-scope bindings)
  └── 1:N → GovernanceWaiver (waivers against this pack)

GovernanceWaiver (Aggregate Root)
  └── N:1 → GovernancePack (which pack is waived)

DelegatedAdministration (Aggregate Root, standalone)

TeamDomainLink (Association Entity)
  ├── N:1 → Team
  └── N:1 → GovernanceDomain
```

---

## 6. Anemic Entity Assessment

| Entity | Assessment | Notes |
|--------|-----------|-------|
| Team | **Adequate** | CRUD + status management |
| GovernanceDomain | **Adequate** | CRUD + criticality classification |
| GovernancePack | **Rich** | Lifecycle (Draft→Published→Deprecated), versioning, applicability, rollout |
| GovernancePackVersion | **Adequate** | Version tracking with content |
| GovernanceRolloutRecord | **Thin** | Simple tracking record — acceptable |
| GovernanceRuleBinding | **Thin** | Binding association — acceptable |
| GovernanceWaiver | **Rich** | Status transitions (Pending→Approved/Rejected), justification, expiration |
| DelegatedAdministration | **Adequate** | Delegation with scope and expiration |
| TeamDomainLink | **Thin** | Association entity — acceptable |

---

## 7. Misplaced Business Rules

| Rule | Current Location | Correct? |
|------|-----------------|----------|
| Pack status transitions | Handler (UpdateGovernancePack) | ⚠️ Should be in entity |
| Waiver approval/rejection | Handler (ApproveGovernanceWaiver, RejectGovernanceWaiver) | ⚠️ Should be in entity |
| Compliance evaluation | Handler (RunComplianceChecks) | ✅ Correct (domain service) |
| Risk calculation | Handler (GetRiskSummary) | ✅ Correct (query/read-model) |

---

## 8. Missing Fields

| Entity | Missing Field | Rationale | Priority |
|--------|-------------|-----------|----------|
| Team | `RowVersion` (xmin) | Optimistic concurrency | HIGH |
| GovernanceDomain | `RowVersion` (xmin) | Optimistic concurrency | HIGH |
| GovernancePack | `RowVersion` (xmin) | Optimistic concurrency | HIGH |
| GovernanceWaiver | `RowVersion` (xmin) | Optimistic concurrency | MEDIUM |

---

## 9. Unnecessary Fields / Entities

The following entities should NOT be in Governance after extraction:
- `IntegrationConnector` → Integrations
- `IngestionSource` → Integrations
- `IngestionExecution` → Integrations
- `AnalyticsEvent` → Product Analytics

No unnecessary fields identified on remaining entities.

---

## 10. Final Domain Model

The domain model for Governance after extraction is **approved** with these conditions:

### Must Complete
1. Add `GovernanceRuleBinding` to DbSet + EF configuration
2. Add `RowVersion` (xmin) on Team, GovernanceDomain, GovernancePack, GovernanceWaiver
3. Move pack status transition validation into `GovernancePack` entity
4. Move waiver approval/rejection validation into `GovernanceWaiver` entity

### Must Extract (not in this phase, but documented)
5. Remove IntegrationConnector, IngestionSource, IngestionExecution from GovernanceDbContext
6. Remove AnalyticsEvent from GovernanceDbContext
7. Remove associated enums, configurations, repositories

**The domain model is ready for the future baseline migration once extraction is documented and DbSet gaps are filled.**
