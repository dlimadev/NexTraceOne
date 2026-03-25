# Change Governance — Module Scope Finalization

> **Module:** 05 — Change Governance  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1

---

## 1. Functional Scope — Final Definition

### 1.1 ChangeIntelligence Subdomain

| Capability | Status | Description |
|------------|--------|-------------|
| Release lifecycle tracking | ✅ Implemented | Full lifecycle: Pending → Running → Succeeded/Failed → RolledBack |
| Change level classification | ✅ Implemented | 5 levels: Operational, NonBreaking, Additive, Breaking, Publication. `ClassifyChangeLevel` command |
| Risk score computation | ✅ Implemented | Composite 0.0–1.0 score from BreakingChangeWeight, BlastRadiusWeight, EnvironmentWeight. `ComputeChangeScore` command |
| Blast radius calculation | ⚠️ Partial | Direct consumers calculated via `CalculateBlastRadius`. Transitive resolution via Catalog Graph not fully operational |
| Freeze window management | ✅ Implemented | 3 scopes (All, Environment, Service). `CreateFreezeWindow`, `CheckFreezeConflict` |
| Release baseline capture | ✅ Implemented | Pre-release metrics: RPM, ErrorRate, AvgLatency, P95, P99, Throughput |
| Observation windows | ✅ Implemented | 3 phases: PreRelease, PostRelease, Validation |
| Post-release review | ✅ Implemented | Review lifecycle with confidence scoring and outcome (Success, Regression, Inconclusive) |
| Rollback assessment | ✅ Implemented | Viability check, risk level, impact analysis |
| External marker ingestion | ✅ Implemented | CI/CD, APM, Incident markers with source system tracking |
| Deployment state tracking | ✅ Implemented | State machine: Pending → Running → Succeeded/Failed → RolledBack |
| Change advisory | ✅ Implemented | AI-assisted recommendations via `GetChangeAdvisory` |
| Change decision history | ✅ Implemented | Approval/rejection decisions with actor, comment, timestamp |
| Jira work item integration | ✅ Implemented | Bidirectional sync: `AttachWorkItemContext`, `SyncJiraWorkItems` |

### 1.2 Workflow Subdomain

| Capability | Status | Description |
|------------|--------|-------------|
| Workflow template management | ✅ Implemented | Configurable templates with change type, API criticality, target environment, minimum approvers |
| Workflow instance lifecycle | ✅ Implemented | Draft → Pending → InReview → Approved/Rejected/Cancelled |
| Multi-stage approval | ✅ Implemented | Stages with configurable approver count, sequential progression |
| Approval decisions | ✅ Implemented | Approved, Rejected, RequestedChanges with comments |
| Evidence pack generation | ✅ Implemented | JSON content, attachments, PDF export capability |
| SLA policy enforcement | ✅ Implemented | Max duration per stage, escalation policy, notification thresholds |

### 1.3 Promotion Subdomain

| Capability | Status | Description |
|------------|--------|-------------|
| Cross-environment promotion | ✅ Implemented | Source → Target environment promotion with justification |
| Gate evaluation | ✅ Implemented | 4 gate types: Quality, Security, Performance, Compliance |
| Gate override with justification | ✅ Implemented | Auth-sensitive override requiring `promotion:admin:write` permission |
| Deployment environment management | ✅ Implemented | Dev/Staging/Production with criticality levels |
| Promotion lifecycle | ✅ Implemented | Pending → InEvaluation → Approved/Rejected/Blocked/Cancelled |

### 1.4 RulesetGovernance Subdomain

| Capability | Status | Description |
|------------|--------|-------------|
| Ruleset upload (JSON/YAML) | ✅ Implemented | Custom and default rulesets with content storage |
| Ruleset binding to asset types | ✅ Implemented | Bind to API, Service, or Event types |
| Lint execution | ✅ Implemented | Execute against releases with findings (Error, Warning, Info) |
| Ruleset scoring | ✅ Implemented | Aggregated score from total findings, error/warning/info counts |
| Default ruleset installation | ✅ Implemented | `InstallDefaultRulesets` command |
| Ruleset archival | ✅ Implemented | Soft-delete via `ArchiveRuleset` |

---

## 2. What Is In Scope (Final)

1. **Release tracking** — full lifecycle from notification to post-release review
2. **Risk scoring** — composite score combining change level, blast radius, environment criticality
3. **Blast radius** — direct and transitive consumer impact analysis
4. **Approval workflows** — configurable multi-stage templates with SLA enforcement
5. **Evidence packs** — audit-ready documentation of approval decisions
6. **Promotion governance** — cross-environment gate enforcement with override audit trail
7. **Ruleset linting** — contract/API quality validation with scoring
8. **Freeze windows** — deployment freeze management with conflict detection
9. **External integrations** — CI/CD markers, APM baselines, Jira work items
10. **Change advisory** — AI-assisted recommendations for change decisions

---

## 3. What Is Out of Scope (Final)

| Out of Scope | Reason | Owning Module |
|-------------|--------|---------------|
| API/service registration and metadata | Catalog owns service definitions | Service Catalog |
| Contract schemas and versioning | Contracts owns schema lifecycle | Contracts |
| Environment CRUD operations | Environment Management owns env lifecycle | Environment Management |
| Incident creation and management | OI owns incident lifecycle | Operational Intelligence |
| Compliance policy definitions | Governance owns compliance policies | Governance |
| Audit trail persistence | Audit & Compliance owns the audit log | Audit & Compliance |
| User/role/permission definitions | Identity owns access control | Identity & Access |
| ClickHouse analytics ingestion | Analytics pipeline is cross-cutting | Platform / OI |

---

## 4. Scope Containment Rules

1. **Change Governance MUST NOT** duplicate service catalog data — it references `ApiAssetId` only
2. **Change Governance MUST NOT** manage environments — it references `EnvironmentId` only
3. **Change Governance MUST NOT** store contracts — it validates them via rulesets
4. **Change Governance MUST NOT** create incidents — it correlates with them via `PostReleaseReview`
5. **Change Governance MAY** define `DeploymentEnvironment` as a local projection of environment data for promotion purposes (current implementation)
6. **RulesetGovernance** stays within Change Governance as it is tightly coupled to the change validation lifecycle

---

## 5. Minimum Complete Set for Production

The following capabilities form the minimum viable scope for production readiness:

| # | Capability | Priority | Status |
|---|-----------|----------|--------|
| 1 | Release creation and lifecycle | P0 | ✅ Done |
| 2 | Change score computation | P0 | ✅ Done |
| 3 | Blast radius (at least direct) | P0 | ⚠️ Direct done, transitive partial |
| 4 | Workflow template + approval | P0 | ✅ Done |
| 5 | Freeze window management | P0 | ✅ Done |
| 6 | Promotion with gates | P1 | ✅ Done |
| 7 | Evidence pack generation | P1 | ✅ Done |
| 8 | Ruleset linting | P2 | ✅ Done |
| 9 | Post-release review | P1 | ✅ Done |
| 10 | External marker ingestion | P2 | ✅ Done |

---

## 6. Frontend Scope Coverage

| Page | Covers | Missing |
|------|--------|---------|
| ChangeCatalogPage | Change listing, filtering, summary | — |
| ChangeDetailPage | Score, blast radius, advisory, decisions, AI panel | Incident correlation panel |
| ReleasesPage | Releases, intelligence, timeline, freeze | — |
| WorkflowPage | Instances, approvals, pending, SLA | — |
| PromotionPage | Requests, gates, approval, override | — |
| WorkflowConfigurationPage | Templates, stages, approvers, SLA, gates | Missing detailed gate configuration UI |

---

## 7. API Scope Coverage

- **46+ endpoints** across 4 subdomains
- **All CRUD operations** covered for core entities
- **No dead endpoints** identified in routing (all mapped to handlers)
- **Permission-protected** — every endpoint requires specific permission scope
- **Gap:** No bulk operations (e.g., batch score computation, bulk release import)
