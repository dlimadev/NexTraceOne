# Environment Management Module — Module Dependency Map

> **Status:** DRAFT  
> **Date:** 2025-07-17  
> **Module:** 02 — Environment Management  
> **Phase:** B1 — Module Consolidation

---

## 1. Dependency Overview

Environment Management is a **foundational module** — it provides environment definitions consumed by nearly every other module. It has few inbound dependencies but many outbound consumers.

```
                    ┌──────────────────────┐
                    │  ENVIRONMENT         │
                    │  MANAGEMENT          │
                    │                      │
                    │  Source of Truth for  │
                    │  environment defs    │
                    └──────────┬───────────┘
                               │
         ┌─────────┬───────────┼───────────┬───────────┬─────────────┐
         │         │           │           │           │             │
         ▼         ▼           ▼           ▼           ▼             ▼
    ┌─────────┐ ┌────────┐ ┌────────┐ ┌─────────┐ ┌──────────┐ ┌──────────┐
    │Identity │ │Config  │ │Change  │ │Ops      │ │Audit &   │ │Notifica  │
    │& Access │ │        │ │Gov     │ │Intell   │ │Compliance│ │tions     │
    └─────────┘ └────────┘ └────────┘ └─────────┘ └──────────┘ └──────────┘
```

---

## 2. Dependency Type Legend

| Type | Symbol | Description |
|------|--------|-------------|
| Hard dependency | `→→` | Module cannot function without the other |
| Soft dependency | `→` | Module benefits from but can function without |
| Event-based | `⚡` | Communication via domain events (async, decoupled) |
| Data reference | `📎` | References ID only (no navigation property) |

---

## 3. Dependencies — Environment Management Depends On

### 3.1 Identity & Access (`→→` Hard Dependency)

| Aspect | Description |
|--------|-------------|
| **What** | TenantId resolution, user identity, authentication |
| **Why** | Every environment belongs to a tenant. Every operation requires authenticated user context. |
| **Interface** | `TenantId` from JWT claim, `UserId` from authentication context |
| **Coupling type** | Strongly-typed ID reference (`TenantId`), middleware context |
| **Impact if unavailable** | Environment Management cannot function — no tenant context, no auth |

**Shared components (currently in IdentityAccess, used by Environment Management):**

| Component | Direction | Notes |
|-----------|-----------|-------|
| `TenantEnvironmentContext` | Identity provides → Env Mgmt consumes | Cross-cutting value object |
| `EnvironmentResolutionMiddleware` | Identity owns | Resolves environment context from HTTP headers |
| `EnvironmentAccessValidator` | Identity owns | Checks user-to-environment access grants |

### 3.2 Integrations Module (`→` Soft Dependency)

| Aspect | Description |
|--------|-------------|
| **What** | Integration connector definitions |
| **Why** | `EnvironmentIntegrationBinding` references `IntegrationConnectorId` |
| **Interface** | `IntegrationConnectorId` (strongly-typed ID reference) |
| **Coupling type** | Data reference only (`📎`) |
| **Impact if unavailable** | Integration bindings reference unknown connectors — degraded but functional |

---

## 4. Dependencies — Other Modules Depend On Environment Management

### 4.1 Identity & Access (`→→` Hard Dependency)

| Aspect | Description |
|--------|-------------|
| **What** | Environment definitions for access control |
| **Why** | `EnvironmentAccess` entity in Identity references `EnvironmentId`. Access validation needs to know which environments exist. |
| **Interface** | `EnvironmentId` (strongly-typed ID), environment listing API or direct read |
| **Coupling type** | Data reference (`📎`) + possible direct DB read |
| **Impact if unavailable** | Cannot validate environment access grants — auth degraded |

**Specific touchpoints:**

| Component in Identity | Uses from Env Mgmt | How |
|----------------------|-------------------|-----|
| `EnvironmentAccess` entity | `EnvironmentId` | FK reference |
| `EnvironmentAccessValidator` | Environment existence check | Repository read |
| `EnvironmentResolutionMiddleware` | Environment slug resolution | Repository read |
| `EnvironmentContextAccessor` | Current environment profile | Repository or cache read |

### 4.2 Configuration Module (`→→` Hard Dependency)

| Aspect | Description |
|--------|-------------|
| **What** | Environment definitions for per-environment config resolution |
| **Why** | Configuration values are scoped per environment. Config module needs to know which environments exist and their profiles. |
| **Interface** | `EnvironmentId`, environment profile/criticality |
| **Coupling type** | Data reference (`📎`) + event subscription (`⚡`) |
| **Impact if unavailable** | Cannot resolve environment-specific config values |

**Interaction patterns:**
- Config module queries environment list to populate environment selectors
- Config module receives `EnvironmentCreated` / `EnvironmentDeactivated` events to update its environment-scoped entries
- Baseline capture in Env Mgmt may request config hash from Config module

### 4.3 Change Governance (`→` Soft Dependency)

| Aspect | Description |
|--------|-------------|
| **What** | Environment definitions, promotion paths for change validation |
| **Why** | Change Governance enforces promotion gates and validates that changes flow through the correct environment sequence. |
| **Interface** | `EnvironmentId`, promotion path definitions, environment profile |
| **Coupling type** | Data reference (`📎`) + API call + event subscription (`⚡`) |
| **Impact if unavailable** | Cannot enforce promotion path rules — change validation degraded |

**Interaction patterns:**
- Change Governance reads promotion paths to determine valid promotion sequences
- Change Governance checks environment readiness before allowing promotion
- Change Governance receives `PromotionPathChanged` events to update its gate configuration
- Promotion requests reference source and target `EnvironmentId`

### 4.4 Operational Intelligence (`→` Soft Dependency)

| Aspect | Description |
|--------|-------------|
| **What** | Environment definitions for runtime metric contextualization |
| **Why** | Runtime metrics, health scores, and comparisons are scoped per environment. |
| **Interface** | `EnvironmentId`, environment name/profile for display |
| **Coupling type** | Data reference (`📎`) |
| **Impact if unavailable** | Runtime data lacks environment context — display degraded |

**Interaction patterns:**
- `EnvironmentComparisonPage` reads environment list for selection dropdowns
- Runtime metrics are tagged with `EnvironmentId` for filtering
- Observability scores are computed per environment
- Release health timeline is environment-scoped

### 4.5 Catalog (Service Catalog) (`→` Soft Dependency)

| Aspect | Description |
|--------|-------------|
| **What** | Environment definitions for service-environment mapping |
| **Why** | Services are deployed to specific environments. Catalog needs to know which environments exist. |
| **Interface** | `EnvironmentId`, environment profile |
| **Coupling type** | Data reference (`📎`) |
| **Impact if unavailable** | Cannot map services to environments — service detail degraded |

**Interaction patterns:**
- Service detail page shows which environments a service is deployed to
- Service topology view includes environment as a dimension
- Service reliability is measured per environment

### 4.6 Audit & Compliance (`⚡` Event-Based)

| Aspect | Description |
|--------|-------------|
| **What** | Environment change events for audit trail |
| **Why** | All environment lifecycle events must be captured in the audit trail. |
| **Interface** | Domain events (`EnvironmentCreated`, `EnvironmentUpdated`, etc.) |
| **Coupling type** | Event-based (`⚡`) — fully decoupled |
| **Impact if unavailable** | Audit trail missing environment events — compliance gap |

**Events consumed by Audit:**

| Event | Audit Action |
|-------|-------------|
| `EnvironmentCreated` | Log creation with full details |
| `EnvironmentUpdated` | Log change with before/after diff |
| `EnvironmentDeactivated` | Log deactivation with reason |
| `PrimaryProductionChanged` | Log designation change (high importance) |
| `PromotionPathCreated` | Log path creation |
| `PromotionPathUpdated` | Log path modification |
| `BaselineSet` | Log baseline capture |
| `DriftDetected` | Log drift finding (may trigger alert) |

### 4.7 Notifications (`⚡` Event-Based)

| Aspect | Description |
|--------|-------------|
| **What** | Environment events for notification triggers |
| **Why** | Critical environment changes should trigger notifications to relevant personas. |
| **Interface** | Domain events |
| **Coupling type** | Event-based (`⚡`) — fully decoupled |
| **Impact if unavailable** | No notifications on environment changes — awareness gap |

**Notification triggers:**

| Event | Notification | Target Personas |
|-------|-------------|-----------------|
| `PrimaryProductionChanged` | "Primary production environment changed" | Tech Lead, Architect, Platform Admin |
| `EnvironmentDeactivated` | "Environment deactivated" | Tech Lead, Platform Admin |
| `DriftDetected` (Critical) | "Critical drift detected in {envName}" | Tech Lead, Architect, Platform Admin |
| `ReadinessAssessed` (NotReady) | "Environment {envName} not ready for promotion" | Tech Lead |

### 4.8 AI & Knowledge (`→` Soft Dependency)

| Aspect | Description |
|--------|-------------|
| **What** | Environment context for AI-assisted analysis |
| **Why** | AI assistant needs environment context to provide relevant recommendations. |
| **Interface** | Environment profile, criticality, promotion path position |
| **Coupling type** | Data reference (`📎`) |
| **Impact if unavailable** | AI lacks environment context — recommendations less relevant |

---

## 5. Dependency Matrix

| Module | Env Mgmt Depends On | Depends On Env Mgmt | Coupling | Priority |
|--------|---------------------|---------------------|----------|----------|
| Identity & Access | `→→` Hard | `→→` Hard | Bidirectional, high | **P0** |
| Configuration | — | `→→` Hard | Unidirectional | **P0** |
| Change Governance | — | `→` Soft | Unidirectional + events | **P1** |
| Operational Intelligence | — | `→` Soft | Unidirectional | **P1** |
| Catalog | — | `→` Soft | Unidirectional | **P1** |
| Audit & Compliance | — | `⚡` Event | Decoupled | **P2** |
| Notifications | — | `⚡` Event | Decoupled | **P2** |
| AI & Knowledge | — | `→` Soft | Unidirectional | **P3** |
| Integrations | `→` Soft | — | Unidirectional | **P3** |
| Governance | — | `→` Soft (FinOps) | Unidirectional | **P3** |

---

## 6. Cross-Module Communication Patterns

### 6.1 Synchronous (Direct Read)

Used when consumer needs immediate, consistent data:

| Consumer | Data Needed | Pattern | Notes |
|----------|------------|---------|-------|
| Identity (access validation) | Environment exists + is active | Direct DB read via repository | Same database, different context |
| Configuration (env-scoped config) | Environment list + profiles | API call or shared read model | Consider caching |
| Change Governance (gate validation) | Promotion path + readiness | API call | Can cache promotion paths |

### 6.2 Asynchronous (Domain Events)

Used when consumer can tolerate eventual consistency:

| Producer Event | Consumers | Delivery | Notes |
|---------------|-----------|----------|-------|
| `EnvironmentCreated` | Audit, Config, Notifications | Message bus | Low latency requirement |
| `EnvironmentUpdated` | Audit, Config | Message bus | Include changed fields |
| `EnvironmentDeactivated` | Audit, Config, Change Gov, Notifications | Message bus | High importance |
| `PrimaryProductionChanged` | Audit, Notifications, Operations | Message bus | Critical event |
| `PromotionPathChanged` | Change Gov, Audit | Message bus | Important for gate config |
| `DriftDetected` | Notifications, Audit, Operations | Message bus | May trigger alerts |

### 6.3 Shared Kernel

Components used across module boundaries:

| Component | Owner | Consumers | Location |
|-----------|-------|-----------|----------|
| `EnvironmentId` | Env Mgmt | All modules | Shared kernel / contracts package |
| `TenantEnvironmentContext` | Shared kernel | Identity, all modules | Shared kernel package |
| `EnvironmentProfile` enum | Env Mgmt | Configuration, Operations, Change Gov | Shared kernel / contracts package |
| `EnvironmentCriticality` enum | Env Mgmt | Operations, Governance | Shared kernel / contracts package |

---

## 7. Risks from Dependencies

| # | Risk | Severity | Modules Involved | Mitigation |
|---|------|----------|-----------------|-----------|
| 1 | Bidirectional dependency with Identity creates tight coupling | **HIGH** | Identity ↔ Env Mgmt | Use shared kernel for common types; Identity reads env data via ID only |
| 2 | Environment deletion cascades to all dependent modules | **HIGH** | All consumers | Soft-delete only; emit event for consumers to handle |
| 3 | Promotion path changes break active change workflows | **MEDIUM** | Change Governance | Validate no active promotions before path modification |
| 4 | Stale environment cache in consumers | **MEDIUM** | Config, Operations | Use events for cache invalidation; short TTL |
| 5 | Circular event chain (drift → notification → action → drift) | **LOW** | Notifications, Operations | Idempotent event handlers; circuit breaker |

---

## 8. Integration Sequence Diagrams

### 8.1 Create Environment Flow

```
User → API → CreateEnvironment Handler
  │
  ├── Validate (name, slug, profile, criticality)
  ├── Check slug uniqueness (per tenant)
  ├── Persist to env_environments
  ├── Emit EnvironmentCreated event
  │     │
  │     ├── → Audit: log creation
  │     ├── → Configuration: register env for config scoping
  │     └── → Notifications: notify Platform Admin
  │
  └── Return created environment
```

### 8.2 Promotion Path Validation Flow

```
Change Governance → Env Mgmt API
  │
  ├── GET /environments/promotion-paths
  │     └── Returns ordered paths with environment details
  │
  ├── GET /environments/{targetEnvId}/readiness
  │     └── Returns readiness score and findings
  │
  └── If ready → Change Governance proceeds with promotion
      If not ready → Change Governance blocks promotion
```

### 8.3 Drift Detection Flow

```
Scheduled Job or User → DetectDrift Handler
  │
  ├── Get current baseline for environment
  ├── Get current state (policies, bindings, config hash)
  ├── Compare current vs baseline
  ├── Generate drift findings
  ├── Persist findings
  ├── Emit DriftDetected event (if drift found)
  │     │
  │     ├── → Notifications: alert if critical drift
  │     ├── → Audit: log drift detection
  │     └── → Operations: update drift dashboard
  │
  └── Return drift report
```
