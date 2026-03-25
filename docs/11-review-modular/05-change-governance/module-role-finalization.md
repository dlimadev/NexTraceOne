# Change Governance — Module Role Finalization

> **Module:** 05 — Change Governance  
> **Prefix:** `chg_`  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1  
> **Maturity:** 81% (third most mature module)

---

## 1. Official Role Definition

**Change Governance is the central module responsible for production change confidence, change intelligence, approval workflows, promotion governance, and ruleset-based quality gates across the entire NexTraceOne platform.**

It materialises:

- **Change Confidence** — every release has a quantified risk score before reaching production
- **Change Intelligence** — blast radius analysis, baseline comparison, post-release review, rollback assessment
- **Approval Workflows** — multi-stage review/approval with SLA enforcement and evidence packs
- **Promotion Governance** — cross-environment promotions controlled by quality/security/performance/compliance gates
- **Ruleset Governance** — contract and API linting rulesets bound to asset types with computed scores

---

## 2. Why Change Governance Is a Core Differentiator

| Differentiator | Description |
|----------------|-------------|
| **Production Change Confidence** | The unique ability to score every change before it reaches production, correlating risk, blast radius, environment criticality, and contract-breaking impact |
| **Blast Radius Analysis** | Computes which consumers are affected (direct + transitive) by a release, referencing the Service Catalog graph |
| **Post-Release Intelligence** | Monitors baseline metrics after deployment, runs post-release reviews, assesses rollback viability |
| **Approval as First-Class Citizen** | Workflow templates with configurable stages, approvers, SLA policies, and machine-readable evidence packs |
| **Promotion Gates** | Environment-to-environment promotion requires gate evaluation (quality, security, performance, compliance) with override audit trail |
| **Ruleset Enforcement** | Contracts and APIs are linted against configurable rulesets; scores feed into change confidence |

No other module in NexTraceOne covers this domain. Without Change Governance, the product loses its core value proposition of **production change confidence**.

---

## 3. What the Module Owns

### 3.1 Owned Bounded Contexts (4 Subdomains)

| Subdomain | Aggregate Roots | Entities | DbContext | Table Prefix |
|-----------|----------------|----------|-----------|-------------|
| **ChangeIntelligence** | Release | 11 (Release, ChangeEvent, BlastRadiusReport, ChangeIntelligenceScore, FreezeWindow, ReleaseBaseline, ObservationWindow, PostReleaseReview, RollbackAssessment, ExternalMarker, DeploymentState) | `ChangeIntelligenceDbContext` | `ci_` |
| **Workflow** | WorkflowTemplate, WorkflowInstance | 6 (WorkflowTemplate, WorkflowInstance, WorkflowStage, ApprovalDecision, EvidencePack, SlaPolicy) | `WorkflowDbContext` | `wf_` |
| **Promotion** | PromotionRequest | 4 (PromotionRequest, PromotionGate, GateEvaluation, DeploymentEnvironment) | `PromotionDbContext` | `prm_` |
| **RulesetGovernance** | Ruleset | 6 (Ruleset, RulesetBinding, LintExecution, LintFinding, RulesetScore, LintResult) | `RulesetGovernanceDbContext` | `rg_` |

### 3.2 Owned Capabilities

- Release lifecycle tracking (creation → deployment → post-release review → rollback)
- Change level classification (Operational → NonBreaking → Additive → Breaking → Publication)
- Risk score computation (0.0–1.0 composite from breaking-change weight, blast-radius weight, environment weight)
- Blast radius calculation (direct + transitive consumers)
- Freeze window management (service-scoped, environment-scoped, or global)
- Approval workflow lifecycle (Draft → Pending → InReview → Approved/Rejected/Cancelled)
- Evidence pack generation and PDF export
- SLA policy enforcement and escalation
- Cross-environment promotion governance with gates
- Ruleset management, binding, linting execution, and scoring
- External marker ingestion (CI/CD, APM, incident)
- Jira work item synchronisation

### 3.3 Owned Frontend Pages (6)

| Page | Route | Purpose |
|------|-------|---------|
| ChangeCatalogPage | `/changes` | Filter/browse all changes with confidence status |
| ChangeDetailPage | `/changes/:changeId` | Full change context: score, blast radius, advisory, decisions, AI panel |
| ReleasesPage | `/releases` | Release management with tabs: Overview, Intelligence, Timeline, Freeze |
| WorkflowPage | `/workflow` | Workflow instances, approve/reject, pending approvals |
| PromotionPage | `/promotion` | Cross-environment promotions, gate evaluation |
| WorkflowConfigurationPage | `/platform/configuration/workflows` | Admin: templates, stages, approvers, SLA, gates |

### 3.4 Owned API Surface (46+ endpoints)

- 20+ ChangeIntelligence endpoints (releases, intelligence, deployments, freeze, analysis)
- 10+ Workflow endpoints (templates, instances, approvals, evidence)
- 9+ Promotion endpoints (requests, gates, approvals, overrides)
- 7+ RulesetGovernance endpoints (upload, bind, lint, score, archive)

---

## 4. What the Module Does NOT Own

| Responsibility | Belongs To | Reason |
|---------------|------------|--------|
| API/service registration | **Service Catalog** | Change Governance only references `ApiAssetId` from Catalog for blast radius |
| Contract definitions (OpenAPI, AsyncAPI) | **Contracts** | Change Governance validates contracts via rulesets but does not own them |
| Environment lifecycle (create, edit, delete) | **Environment Management** | Change Governance only references `EnvironmentId` for freeze windows and promotions |
| Incident management | **Operational Intelligence** | Change Governance correlates incidents to releases but does not create/manage incidents |
| Audit trail storage | **Audit & Compliance** | Change Governance emits auditable events; Audit & Compliance stores them |
| User identity and permissions | **Identity & Access** | Change Governance enforces permissions but does not define them |
| General configuration | **Configuration** | Templates and defaults may be stored in Configuration; Change Governance owns its own workflow templates |

---

## 5. Why Change Governance Must Not Be Absorbed

### 5.1 Not by Operational Intelligence

Operational Intelligence deals with incidents, health scores, reliability, and runtime monitoring. Change Governance deals with **pre-production** risk assessment, approval, and promotion. They are complementary but have different lifecycles, different aggregate roots, and different data flows.

Change Governance **consumes** incident data from OI for correlation (e.g., `PostReleaseReview` correlates with incidents post-deployment). OI **consumes** release data from Change Governance for change-incident linkage. This is a bidirectional data dependency, not an ownership overlap.

### 5.2 Not by Governance (Module 08)

The Governance module manages dashboards, executive reports, compliance policies, and product analytics. It is a **read/aggregate** module. Change Governance is a **transactional/workflow** module that owns the approval lifecycle and gate enforcement. Merging them would violate the Single Responsibility Principle and create a monolithic governance module.

### 5.3 Not by Contracts

The Contracts module owns contract definitions, schemas, versioning, and compatibility checks. Change Governance's RulesetGovernance subdomain **uses** contracts for linting but does not own the contract definitions. The boundary is clear: Contracts defines; Change Governance validates.

---

## 6. Summary

Change Governance is a well-defined, mature (81%), and essential module with 227 C# files, 27 entities, 4 DbContexts, 40+ CQRS handlers, 46+ endpoints, 6 frontend pages, and 179+ tests. It is the **operational core of NexTraceOne's value proposition** around production change confidence and must remain an independent, first-class module.
