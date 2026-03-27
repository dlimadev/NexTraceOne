# Part 12 — Documentation & Onboarding Upgrade

> **Module:** Operational Intelligence
> **Date:** 2025-07-14
> **Status:** Review Complete
> **Scope:** Existing documentation review, gap identification, minimum documentation set, onboarding notes, end-to-end flow documentation outline

---

## 1. Existing Documentation Review

### 1.1 module-review.md

**Path:** `docs/11-review-modular/06-operational-intelligence/module-review.md`
**Size:** ~161 lines, 7.3 KB
**Date:** 2026-03-24

| Section | Coverage | Assessment |
|---|---|---|
| Module overview | ✅ Present | Brief but adequate |
| Subdomain list (5) | ✅ Present | Lists Incidents, Automation, Reliability, Runtime, Cost |
| Permission inventory (16) | ✅ Present | Complete enumeration |
| Role mapping | ✅ Present | Admin, Eng Lead, SRE, Ops, Viewer |
| Frontend pages (10) | ✅ Present | Route + permission mapping |
| Backend endpoints (7 modules) | ✅ Present | Endpoint module listing |
| Domain entities | ⚠️ Partial | Names only, no property detail |
| Scoring formula | ⚠️ Partial | Mentioned but not fully documented |
| Gaps and actions | ✅ Present | 5 P1 actions identified |
| Maturity assessment | ✅ Present | 74% overall |

**Verdict:** Good summary document but lacks technical depth for onboarding.

### 1.2 module-consolidated-review.md

**Path:** `docs/11-review-modular/06-operational-intelligence/module-consolidated-review.md`
**Size:** ~276 lines, 19.7 KB
**Date:** 2026-03-24

| Section | Coverage | Assessment |
|---|---|---|
| Executive summary | ✅ Present | Maturity 74%, key metrics |
| Quick wins (5) | ✅ Present | Actionable items |
| Structural refactors (6) | ✅ Present | Prioritized list |
| Backend maturity (90%) | ✅ Present | Detailed assessment |
| Frontend maturity (85%) | ✅ Present | Page-by-page review |
| Documentation maturity (50%) | ✅ Present | Gap identification |
| Test maturity (70%) | ✅ Present | Coverage analysis |
| Cross-module dependencies | ⚠️ Partial | Listed but not mapped |
| Migration readiness | ✅ Present | Pre-conditions listed |

**Verdict:** Comprehensive review document. Primary reference for remediation planning.

### 1.3 Other Existing Documents (8 Files)

| Document | Lines | Focus | Quality |
|---|---|---|---|
| `domain-model-finalization.md` | ~500+ | Entity definitions, enums, value objects | ✅ Thorough |
| `backend-functional-corrections.md` | ~550+ | Backend bugs, missing features | ✅ Thorough |
| `frontend-functional-corrections.md` | ~500+ | Frontend bugs, missing pages | ✅ Thorough |
| `persistence-model-finalization.md` | ~480+ | DB schema, table prefixes, indexes | ✅ Thorough |
| `module-role-finalization.md` | ~250+ | Role/permission matrix | ✅ Complete |
| `end-to-end-operational-flow-validation.md` | ~400+ | E2E flow scenarios | ✅ Detailed |
| `module-scope-finalization.md` | ~350+ | Scope boundaries, subdomain map | ✅ Clear |
| `clickhouse-data-placement-review.md` | ~330+ | ClickHouse migration planning | ✅ Forward-looking |

**Total existing documentation:** 10 files, ~228 KB

---

## 2. Missing Documentation

### 2.1 Critical Missing Documents

| Document | Priority | Purpose | Audience |
|---|---|---|---|
| **Module README** | P0 | Entry point for developers — what the module does, how to run it, key concepts | All developers |
| **Scoring Formula Reference** | P1 | Formal specification of `OverallScore` computation, sub-score derivation, weight rationale | SRE, Eng Lead |
| **Automation Workflow Guide** | P1 | State machine diagram, transition rules, approval flow, audit points | Operators, SRE |
| **API Reference** | P1 | All 44+ endpoints with request/response schemas, permissions, error codes | Frontend devs, integrators |
| **Architecture Decision Records** | P2 | Why 5 subdomains, why string-based ServiceId, why RLS over application filter | Architects, new team members |

### 2.2 Missing Inline Documentation

| Area | What's Missing | Priority |
|---|---|---|
| Domain entities | XML docs on all public properties of `IncidentRecord`, `AutomationWorkflowRecord`, `ReliabilitySnapshot`, `RuntimeSnapshot`, `CostRecord` | P1 |
| Service interfaces | XML docs on `IIncidentCorrelationService`, `IReliabilityComputationService`, `IAutomationOrchestrationService` | P1 |
| Enums | XML docs on each value of `AutomationWorkflowStatus`, `AutomationApprovalStatus`, `AutomationActionType`, `IncidentSeverity`, `IncidentStatus`, `HealthStatus` | P2 |
| Value objects | XML docs on `TelemetrySummary`, `BusinessImpact`, `NarrativeGuidance`, `TemporalContext` | P2 |
| DbContext classes | XML docs on all 5 DbContexts explaining table ownership and outbox table names | P2 |

---

## 3. Classes and Flows Needing Explanation

### 3.1 Complex Classes

| Class | Location | Why It Needs Documentation |
|---|---|---|
| `IncidentCorrelationService` | `Incidents/Services/` | Core business logic — correlates incidents with changes. Algorithm, confidence levels, and data sources must be documented. |
| `ReliabilitySnapshot` | `Reliability/Entities/` | Scoring formula is embedded in domain entity. Weight rationale, score ranges, and trend calculation need explanation. |
| `AutomationWorkflowRecord` | `Automation/Entities/` | Complex state machine with 11 states and 2 approval states. Valid transitions must be documented. |
| `RuntimeSnapshot` | `Runtime/Entities/` | Health classification logic with hardcoded thresholds. Classification algorithm and threshold rationale need explanation. |
| `TenantRlsInterceptor` | `BuildingBlocks/Infrastructure/` | Critical security component. How it works, when it fires, and failure modes must be documented. |

### 3.2 Complex Flows

| Flow | Components Involved | Documentation Need |
|---|---|---|
| Incident creation → correlation → mitigation | `IncidentEndpointModule` → `IncidentCorrelationService` → `MitigationWorkflow` | End-to-end sequence diagram |
| Automation: Draft → Approval → Execution | `AutomationEndpointModule` → `AutomationWorkflowRecord` → `AutomationAuditRecord` | State machine diagram + permission gates |
| Reliability scoring pipeline | `RuntimeSnapshot` → sub-scores → `ReliabilitySnapshot.OverallScore` | Data flow diagram |
| Cost import → attribution → trends | `CostIntelligenceEndpointModule` → `CostRecord` → `CostSnapshot` → `CostTrend` | Pipeline diagram |

---

## 4. Onboarding Notes Needed

### 4.1 Module Overview (for new developers)

```markdown
# Operational Intelligence — Quick Start

## What is this module?
Operational Intelligence provides incident management, automation workflows,
service reliability scoring, runtime health monitoring, and cost intelligence.

## 5 Subdomains
1. Incidents — incident lifecycle, correlation, mitigation
2. Automation — workflow orchestration, approval, execution, audit
3. Reliability — service reliability scoring, trends
4. Runtime — health snapshots, drift detection, baselines
5. Cost — cost records, snapshots, trends, attribution

## Key Concepts
- ReliabilitySnapshot: weighted score (50% runtime + 30% incidents + 20% observability)
- RuntimeSnapshot: health classification (Healthy/Degraded/Unhealthy)
- AutomationWorkflow: 11-state machine with approval gates
- TenantRlsInterceptor: PostgreSQL RLS for tenant isolation

## How to Run
[Instructions for running the module locally]

## How to Test
[Instructions for running module tests]
```

### 4.2 Subdomain Map

| Subdomain | Aggregate Roots | DbContext | API Prefix | Table Prefix |
|---|---|---|---|---|
| Incidents | `IncidentRecord`, `RunbookRecord` | `IncidentDbContext` | `/api/operations/incidents` | `oi_inc_` |
| Automation | `AutomationWorkflowRecord` | `AutomationDbContext` | `/api/operations/automation` | `oi_aut_` |
| Reliability | `ReliabilitySnapshot` | `ReliabilityDbContext` | `/api/operations/reliability` | `oi_rel_` |
| Runtime | `RuntimeSnapshot` | `RuntimeIntelligenceDbContext` | `/api/operations/runtime` | `oi_rt_` |
| Cost | `CostRecord` | `CostIntelligenceDbContext` | `/api/operations/costs` | `oi_cost_` |

---

## 5. Minimum Documentation Set

The following documents are the **minimum** required for the module to be considered adequately documented:

| # | Document | Type | Priority | Status |
|---|---|---|---|---|
| 1 | Module README.md | Onboarding | P0 | ❌ Missing |
| 2 | Subdomain map (table above) | Architecture | P0 | ⚠️ In review docs only |
| 3 | Scoring formula reference | Specification | P1 | ❌ Missing |
| 4 | Automation workflow state machine | Specification | P1 | ❌ Missing (diagram) |
| 5 | API reference (all endpoints) | Reference | P1 | ❌ Missing |
| 6 | Permission matrix | Reference | P1 | ✅ In `module-role-finalization.md` |
| 7 | XML docs on public APIs | Inline | P1 | ❌ Missing |
| 8 | XML docs on domain entities | Inline | P1 | ❌ Missing |
| 9 | Cross-module dependency map | Architecture | P2 | ✅ Created (Part 11) |
| 10 | Getting started guide | Onboarding | P2 | ❌ Missing |

---

## 6. End-to-End Flow Documentation Outline

### Flow 1: Incident Lifecycle

```
1. Incident Detection
   - External trigger (API call) or internal event
   - IncidentEndpointModule.CreateIncident()
   - Permission: operations:incidents:write

2. Incident Correlation
   - IncidentCorrelationService analyzes recent changes
   - Sets HasCorrelation, CorrelationConfidence, CorrelatedChanges
   - Depends on: Change Governance data

3. Incident Triage
   - Severity assignment, team assignment
   - UI: IncidentDetailPage
   - Permission: operations:incidents:write

4. Mitigation
   - MitigationWorkflow created
   - Linked runbooks suggested
   - Permission: operations:mitigation:write

5. Resolution
   - Status → Resolved
   - Impact on ReliabilitySnapshot recalculation
   - (Missing) Notification to stakeholders

6. Post-Incident
   - Timeline review
   - Correlation validation
   - (Missing) Central audit record
```

### Flow 2: Automation Workflow Lifecycle

```
1. Creation (Draft)
   - Manual or AI-suggested
   - Permission: operations:automation:write

2. Precondition Check (PendingPreconditions)
   - Validate target service state
   - Verify dependency health

3. Approval Request (AwaitingApproval)
   - (Missing) Notification to approvers
   - Permission: operations:automation:approve

4. Approval Decision
   - Approved → ReadyToExecute
   - Rejected → terminal state
   - (Missing) Four-eyes enforcement

5. Execution (Executing)
   - Permission: operations:automation:execute
   - AutomationAuditRecord created
   - (Missing) Step-up authentication

6. Validation (AwaitingValidation)
   - Post-execution health check
   - Compare pre/post metrics

7. Completion
   - Completed / Failed terminal state
   - (Missing) Notification of outcome
   - (Missing) Central audit event
```

### Flow 3: Reliability Scoring Pipeline

```
1. Runtime Data Collection
   - RuntimeSnapshot ingested (currently from seed data)
   - Health classification: Healthy/Degraded/Unhealthy

2. Sub-Score Computation
   - RuntimeHealthScore from RuntimeSnapshot health
   - IncidentImpactScore from open incident count/severity
   - ObservabilityScore from monitoring coverage

3. Overall Score Calculation
   - OverallScore = Runtime*0.50 + Incident*0.30 + Observability*0.20
   - Clamped to 0-100

4. Trend Determination
   - Compare with previous snapshot
   - Set TrendDirection: Improving/Stable/Declining

5. Persistence
   - ReliabilitySnapshot saved to ReliabilityDbContext
   - (Missing) ReliabilityScoreChangedEvent published

6. Consumption
   - ReliabilityEndpointModule serves scores
   - Frontend: ReliabilityPage, ReliabilityDetailPage
```

---

## 7. Documentation Corrections Backlog

| ID | Item | Priority | Effort | Area |
|---|---|---|---|---|
| DOC-01 | Create module README.md | P0 | Small | Docs |
| DOC-02 | Document scoring formula (formal spec) | P1 | Small | Docs |
| DOC-03 | Create automation state machine diagram | P1 | Small | Docs |
| DOC-04 | Generate API reference (all 44+ endpoints) | P1 | Medium | Docs |
| DOC-05 | Add XML docs to all public domain entities | P1 | Medium | Backend |
| DOC-06 | Add XML docs to all service interfaces | P1 | Medium | Backend |
| DOC-07 | Add XML docs to all enum values | P2 | Small | Backend |
| DOC-08 | Create getting started guide | P2 | Small | Docs |
| DOC-09 | Create ADRs for key decisions | P2 | Medium | Docs |
| DOC-10 | Create end-to-end flow diagrams (3 flows) | P2 | Medium | Docs |

---

## 8. References

| Artifact | Path |
|---|---|
| module-review.md | `docs/11-review-modular/06-operational-intelligence/module-review.md` |
| module-consolidated-review.md | `docs/11-review-modular/06-operational-intelligence/module-consolidated-review.md` |
| domain-model-finalization.md | `docs/11-review-modular/06-operational-intelligence/domain-model-finalization.md` |
| module-role-finalization.md | `docs/11-review-modular/06-operational-intelligence/module-role-finalization.md` |
| end-to-end-operational-flow-validation.md | `docs/11-review-modular/06-operational-intelligence/end-to-end-operational-flow-validation.md` |
| Backend module root | `src/modules/operationalintelligence/` |
| Frontend feature root | `src/frontend/src/features/operations/` |
