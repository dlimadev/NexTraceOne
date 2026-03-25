# Change Governance — Module Dependency Map

> **Module:** 05 — Change Governance  
> **Date:** 2026-03-25  
> **Status:** Consolidation Phase — B1

---

## 1. Dependencies Overview

```
                    ┌──────────────────┐
                    │ Identity & Access │
                    │  (Authentication, │
                    │   Permissions,    │
                    │   Tenant Context) │
                    └────────┬─────────┘
                             │ provides auth context
                             ▼
┌──────────────┐   ┌────────────────────┐   ┌──────────────────────┐
│   Service    │──▶│  CHANGE GOVERNANCE │──▶│  Audit & Compliance  │
│   Catalog    │   │                    │   │  (event consumption) │
│ (API assets, │   │  4 subdomains:     │   └──────────────────────┘
│  dependency  │   │  - ChangeIntel     │
│  graph)      │   │  - Workflow        │   ┌──────────────────────┐
└──────────────┘   │  - Promotion       │──▶│   Notifications      │
                   │  - Rulesets        │   │  (workflow events)   │
┌──────────────┐   └────────┬───────────┘   └──────────────────────┘
│ Environment  │            │
│ Management   │◀───────────┘
│ (env data)   │ references env context
└──────────────┘

┌──────────────┐   ┌────────────────────┐
│  Contracts   │──▶│  RulesetGovernance │ (linting contracts against rulesets)
│  (schemas)   │   │  (subdomain)       │
└──────────────┘   └────────────────────┘

┌──────────────┐   ┌────────────────────┐
│ Operational  │◀──│  ChangeIntelligence │ (incident-change correlation)
│ Intelligence │   │  (subdomain)       │
└──────────────┘   └────────────────────┘
```

---

## 2. Inbound Dependencies (What Change Governance Consumes)

### 2.1 Service Catalog → Change Governance

| Dependency | Type | Usage | Criticality |
|-----------|------|-------|-------------|
| `ApiAssetId` | Reference | Release entity references Catalog API assets for blast radius | 🔴 High |
| Dependency Graph | Query | Blast radius uses Catalog's service dependency graph to find direct + transitive consumers | 🔴 High |
| Service metadata | Query | Service name, criticality, and ownership from Catalog | 🟡 Medium |

**Interface:** Read-only queries against Catalog data. No write operations.

### 2.2 Identity & Access → Change Governance

| Dependency | Type | Usage | Criticality |
|-----------|------|-------|-------------|
| Authentication | Context | All endpoints require authenticated user context | 🔴 High |
| Permission enforcement | Middleware | `RequireAuthorization` on all endpoints with permission scopes | 🔴 High |
| Tenant context | Security | `TenantId` for multi-tenant isolation via RLS | 🔴 High |
| User identity | Audit | `CreatedBy`, `ApprovedBy`, `OverriddenBy` fields | 🔴 High |

**Interface:** Implicit via middleware pipeline and interceptors.

### 2.3 Environment Management → Change Governance

| Dependency | Type | Usage | Criticality |
|-----------|------|-------|-------------|
| `EnvironmentId` | Reference | Release, FreezeWindow, PromotionRequest reference environment data | 🟠 Medium |
| Environment criticality | Query | Used in `EnvironmentWeight` for change score computation | 🟠 Medium |
| Environment profile | Query | Dev/Staging/Production classification for promotion validation | 🟠 Medium |

**Interface:** Read-only references. Change Governance has a local `DeploymentEnvironment` entity that projects environment data.

### 2.4 Contracts → Change Governance (RulesetGovernance)

| Dependency | Type | Usage | Criticality |
|-----------|------|-------|-------------|
| Contract schemas | Query | Rulesets validate contract schemas (OpenAPI, AsyncAPI) via linting | 🟡 Medium |
| Asset type classification | Reference | `RulesetBinding.AssetType` references Contracts asset types (API, Service, Event) | 🟡 Medium |

**Interface:** RulesetGovernance executes lint rules against contract content fetched from Contracts module.

### 2.5 Configuration → Change Governance

| Dependency | Type | Usage | Criticality |
|-----------|------|-------|-------------|
| Configuration values | Query | Workflow defaults, thresholds, feature flags | 🟢 Low |

**Interface:** Indirect — Configuration module provides global settings.

---

## 3. Outbound Dependencies (What Change Governance Exposes)

### 3.1 Change Governance → Operational Intelligence

| Exposed Data | Type | Usage by OI | Criticality |
|-------------|------|-------------|-------------|
| Release data | Event/Query | OI correlates incidents with releases (change-incident correlation) | 🔴 High |
| Change score | Event | OI uses change score for incident risk assessment | 🟡 Medium |
| Deployment states | Event | OI tracks deployment timeline for incident timeline | 🟡 Medium |

**Interface:** Outbox events from `ci_outbox_messages`. OI consumes `ReleaseCreated`, `DeploymentStateChanged`, `ChangeScoreComputed` events.

### 3.2 Change Governance → Audit & Compliance

| Exposed Data | Type | Usage by Audit | Criticality |
|-------------|------|----------------|-------------|
| Approval decisions | Event | Audit trail of who approved/rejected changes | 🔴 High |
| Gate overrides | Event | Audit trail of gate override justifications | 🔴 High |
| Freeze window changes | Event | Audit of freeze window creation/modification | 🟡 Medium |
| Promotion approvals | Event | Audit of cross-environment promotion decisions | 🔴 High |

**Interface:** Outbox events from all 4 DbContexts. Audit & Compliance consumes decision events.

### 3.3 Change Governance → Notifications

| Exposed Data | Type | Usage | Criticality |
|-------------|------|-------|-------------|
| Workflow stage transitions | Event | Notify approvers when approval is needed | 🟠 Medium |
| SLA violations | Event | Escalation notifications | 🟠 Medium |
| Promotion gate results | Event | Notify of gate pass/fail | 🟡 Medium |

**Interface:** Outbox events. Currently structural — Notifications module integration not yet operational.

---

## 4. What the Module Exposes (Public API Surface)

### Endpoints (46+)
All endpoints under `/api/v1/` with sub-routes for releases, changes, analysis, deployments, freeze-windows, workflow, promotion, rulesets.

### Integration Events (via Outbox)
| Event | Source DbContext | Consumers |
|-------|-----------------|-----------|
| `ReleaseCreated` | ChangeIntelligence | OI, Audit |
| `DeploymentStateChanged` | ChangeIntelligence | OI, Audit |
| `ChangeScoreComputed` | ChangeIntelligence | OI |
| `BlastRadiusCalculated` | ChangeIntelligence | OI |
| `WorkflowApproved` | Workflow | Audit, Notifications |
| `WorkflowRejected` | Workflow | Audit, Notifications |
| `EvidencePackGenerated` | Workflow | Audit |
| `PromotionApproved` | Promotion | Audit, Notifications |
| `GateOverridden` | Promotion | Audit |
| `RulesetExecuted` | RulesetGovernance | Audit |

---

## 5. What Must Never Be Duplicated Outside Change Governance

| Capability | Reason |
|-----------|--------|
| Change score computation | Single source of truth for risk assessment |
| Blast radius calculation | Must use consistent algorithm and data source |
| Approval workflow lifecycle | Single workflow engine for change governance |
| Promotion gate enforcement | Single gate system to prevent bypass |
| Evidence pack generation | Audit-grade evidence must come from authoritative source |
| Freeze window management | Single freeze enforcement point |

---

## 6. Circular Dependency Analysis

| Pair | Direction | Risk | Mitigation |
|------|-----------|------|------------|
| Change Gov ↔ Catalog | Change Gov reads; Catalog doesn't depend back | ✅ No circular dependency | — |
| Change Gov ↔ OI | Bidirectional: Change Gov sends events, OI sends incident data | ⚠️ Loose coupling via events | Events only, no direct code dependency |
| Change Gov ↔ Contracts | Change Gov reads contracts for linting; Contracts doesn't depend back | ✅ No circular dependency | — |
| Change Gov ↔ Env Mgmt | Change Gov reads; Env Mgmt doesn't depend back | ✅ No circular dependency | — |

**Conclusion:** No circular dependencies exist. The OI ↔ Change Gov relationship is event-based and loosely coupled.
