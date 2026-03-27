# Environment Management Module — Boundary Finalization

> **Status:** DRAFT  
> **Date:** 2025-07-17  
> **Module:** 02 — Environment Management  
> **Phase:** B1 — Module Consolidation

---

## 1. What Is Environment Management in NexTraceOne

Environment Management in NexTraceOne means **the definition, classification, lifecycle, and governance of runtime environments across the platform.** It answers: "What environments exist? What type are they? Which is production? What is the promotion path? Is an environment ready for promotion? What drifted?"

Environment Management is the **Source of Truth** for environment definitions, profiles, criticality, promotion paths, baselines, and readiness.

Environment Management is NOT:
- User authentication or identity management (that's Identity & Access)
- Per-environment configuration values (that's Configuration)
- Change approval workflows referencing environments (that's Change Governance)
- Runtime metric comparison between environments (that's Operational Intelligence)
- Audit logging of environment changes (that's Audit & Compliance)

---

## 2. Key Concepts

| Concept | Definition in NexTraceOne |
|---------|--------------------------|
| **Environment** | A named, classified runtime context (Dev, Staging, Production, DR) with profile, criticality, and region |
| **Environment Profile** | The type classification: Development, Validation, Staging, Production, DisasterRecovery |
| **Criticality** | The business impact level: Low, Medium, High, Critical |
| **Primary Production** | The single designated main production environment for a tenant |
| **Production-Like** | Environments that mirror production characteristics (staging, DR) |
| **Promotion Path** | The ordered sequence of environments through which changes must flow (e.g., Dev → Staging → Prod) |
| **Baseline** | A known-good snapshot of an environment's configuration and state for drift comparison |
| **Drift** | The deviation of an environment from its baseline or from another reference environment |
| **Readiness** | An assessment of whether an environment is ready to receive a promotion |
| **Environment Group** | A logical grouping of related environments (e.g., "EU Region", "DR Set") |

---

## 3. What Stays Inside Environment Management (Final)

| # | Capability | Justification |
|---|-----------|--------------|
| 1 | Environment CRUD (create, read, update, soft-delete) | Core aggregate root lifecycle |
| 2 | Environment Profile classification | Defines type: Dev, Staging, Prod, etc. |
| 3 | Environment Criticality assignment | Business impact classification |
| 4 | Primary Production designation | Source of Truth for which env is primary prod |
| 5 | Production-Like flag management | Classification for staging/DR mirroring |
| 6 | Promotion Path definition | Ordered sequence of environments for change promotion |
| 7 | Environment Baseline management | Set and track known-good state |
| 8 | Drift detection (configuration drift) | Compare environment configs against baseline |
| 9 | Readiness scoring | Assess if an environment is ready for promotion |
| 10 | Environment Grouping | Logical grouping of related environments |
| 11 | Environment relationships (parent/child, DR pairs) | Structural relationships between environments |
| 12 | Environment Policy management | Policies governing what is allowed per environment |
| 13 | Environment Telemetry Policy | Telemetry collection settings per environment |
| 14 | Environment Integration Bindings | Which integrations are bound per environment |
| 15 | Environment lifecycle events | Domain events: Created, Updated, Deactivated, PromotionPathChanged, BaselineSet, DriftDetected |

---

## 4. What Stays in Identity & Access

| # | Capability | Justification |
|---|-----------|--------------|
| 1 | User-to-Environment access grants (EnvironmentAccess) | User access control is an Identity concern |
| 2 | Environment context resolution for authentication | Middleware that resolves which env a request targets — cross-cutting auth concern |
| 3 | EnvironmentAccessAuthorizationHandler | Authorization policy evaluation — Identity responsibility |
| 4 | EnvironmentAccessValidator | Validates user has access to specific environment — auth concern |
| 5 | EnvironmentResolutionMiddleware | Resolves tenant-environment context from request — cross-cutting |

**Note:** Identity will consume Environment definitions from Environment Management via:
- Direct repository read (if same database) or
- Integration event / internal API (if separate bounded context)

---

## 5. What Stays in Configuration Module

| # | Capability | Justification |
|---|-----------|--------------|
| 1 | Per-environment configuration key-value pairs | Configuration values are owned by Configuration |
| 2 | Configuration overrides per environment | Layered config resolution |
| 3 | Configuration comparison between environments | Config module compares its own data, referencing env definitions |

---

## 6. What Stays in Change Governance

| # | Capability | Justification |
|---|-----------|--------------|
| 1 | Promotion request workflows | Change Governance owns the approval process |
| 2 | Environment gates in change pipelines | Change Governance defines gates, references env definitions |
| 3 | Change-to-environment correlation | Change Governance tracks which changes went to which env |

**Interaction:** Change Governance consumes Environment definitions and Promotion Paths from Environment Management to enforce promotion rules.

---

## 7. What Stays in Operational Intelligence

| # | Capability | Justification |
|---|-----------|--------------|
| 1 | Runtime metric comparison between environments | Operational Intelligence owns runtime metrics |
| 2 | EnvironmentComparisonPage | UI for comparing runtime behavior — operations concern |
| 3 | Release health timeline per environment | Runtime health data — operations domain |
| 4 | Observability score per environment | Derived from runtime telemetry — operations concern |

**Interaction:** Operational Intelligence consumes Environment definitions from Environment Management to contextualize runtime data.

---

## 8. What Stays in Audit & Compliance

| # | Capability | Justification |
|---|-----------|--------------|
| 1 | Audit log entries for environment changes | Audit & Compliance owns the audit trail |
| 2 | Compliance evidence for environment governance | Compliance evidence collection |

**Interaction:** Environment Management emits domain events; Audit & Compliance captures them.

---

## 9. Boundary Interaction Summary

```
┌─────────────────────────────────────────────────────────────────┐
│                    ENVIRONMENT MANAGEMENT                        │
│                                                                  │
│  Environment CRUD · Profiles · Criticality · Primary Prod        │
│  Promotion Paths · Baselines · Drift · Readiness · Grouping     │
│  Policies · Telemetry Policies · Integration Bindings            │
│                                                                  │
│  Emits: EnvironmentCreated, EnvironmentUpdated,                  │
│         PromotionPathChanged, BaselineSet, DriftDetected,        │
│         ReadinessAssessed, PrimaryProductionChanged              │
└──────┬──────────┬──────────┬──────────┬──────────┬──────────────┘
       │          │          │          │          │
       ▼          ▼          ▼          ▼          ▼
  ┌─────────┐ ┌────────┐ ┌────────┐ ┌─────────┐ ┌──────────┐
  │Identity │ │Config  │ │Change  │ │Ops      │ │Audit &   │
  │& Access │ │        │ │Gov     │ │Intell   │ │Compliance│
  ├─────────┤ ├────────┤ ├────────┤ ├─────────┤ ├──────────┤
  │User-Env │ │Per-env │ │Promo   │ │Runtime  │ │Audit     │
  │access   │ │config  │ │requests│ │metrics  │ │trail     │
  │grants   │ │values  │ │gates   │ │compariso│ │evidence  │
  │auth     │ │override│ │change  │ │health   │ │          │
  │resoluti │ │comparis│ │correlat│ │scores   │ │          │
  └─────────┘ └────────┘ └────────┘ └─────────┘ └──────────┘
```

---

## 10. Entities That Must Move (IdentityAccess → EnvironmentManagement)

| # | Entity | Current Location | Target Location | Notes |
|---|--------|-----------------|----------------|-------|
| 1 | `Environment` | `IdentityAccess.Domain/Entities/` | `EnvironmentManagement.Domain/Entities/` | Aggregate root — must move |
| 2 | `EnvironmentPolicy` | `IdentityAccess.Domain/Entities/` | `EnvironmentManagement.Domain/Entities/` | Direct child of Environment |
| 3 | `EnvironmentTelemetryPolicy` | `IdentityAccess.Domain/Entities/` | `EnvironmentManagement.Domain/Entities/` | Direct child of Environment |
| 4 | `EnvironmentIntegrationBinding` | `IdentityAccess.Domain/Entities/` | `EnvironmentManagement.Domain/Entities/` | Direct child of Environment |
| 5 | `EnvironmentCriticality` | `IdentityAccess.Domain/Enums/` | `EnvironmentManagement.Domain/Enums/` | Enum owned by Environment |
| 6 | `EnvironmentProfile` | `IdentityAccess.Domain/Enums/` | `EnvironmentManagement.Domain/Enums/` | Enum owned by Environment |
| 7 | `TenantEnvironmentContext` | `IdentityAccess.Domain/ValueObjects/` | Shared / cross-cutting | Used by Identity middleware — may stay shared |
| 8 | `EnvironmentUiProfile` | `IdentityAccess.Domain/ValueObjects/` | `EnvironmentManagement.Domain/ValueObjects/` | UI presentation owned by Environment |

---

## 11. Entities That Stay in Identity & Access

| # | Entity | Reason |
|---|--------|--------|
| 1 | `EnvironmentAccess` | User-to-environment access grant — Identity domain concept |

**Note:** `EnvironmentAccess` references `EnvironmentId` as a foreign key. After extraction, it will reference the Environment entity cross-module via ID only (no navigation property across bounded contexts).

---

## 12. Infrastructure That Must Move

| # | Service | Current | Target | Notes |
|---|---------|---------|--------|-------|
| 1 | `EnvironmentRepository` | IdentityAccess.Infrastructure | EnvironmentManagement.Infrastructure | Data access for Environment aggregate |
| 2 | `EnvironmentProfileResolver` | IdentityAccess.Infrastructure | EnvironmentManagement.Infrastructure | Profile resolution logic |

---

## 13. Infrastructure That Stays in Identity & Access

| # | Service | Reason |
|---|---------|--------|
| 1 | `EnvironmentContextAccessor` | Cross-cutting concern for request context |
| 2 | `EnvironmentAccessValidator` | Authorization validation — Identity concern |
| 3 | `TenantEnvironmentContextResolver` | Cross-cutting tenant-env resolution |
| 4 | `EnvironmentResolutionMiddleware` | Cross-cutting HTTP middleware |
| 5 | `EnvironmentAccessAuthorizationHandler` | Authorization policy handler |

---

## 14. Open Decisions

| # | Decision | Options | Recommendation |
|---|----------|---------|---------------|
| 1 | Shared database or separate? | Same DB with schema prefix / Separate DB | Same DB with `env_` table prefix (Phase 1) |
| 2 | `EnvironmentAccess` ownership | Move to Env Management / Keep in Identity | Keep in Identity — it's an access control concept |
| 3 | `TenantEnvironmentContext` location | Move to shared kernel / Keep in Identity | Move to shared kernel — used by multiple modules |
| 4 | Cross-module reference pattern | Direct DB FK / Integration events / Internal API | Direct DB FK in Phase 1, events in Phase 2 |
