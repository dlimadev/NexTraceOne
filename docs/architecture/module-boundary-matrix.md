# NexTraceOne — Official Module Boundary Matrix

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Phase:** A0 + A1 — Consolidation  
> **Sources:** `docs/11-review-modular/modular-review-master.md`, module consolidated reviews

---

## 01 — Identity & Access

| Attribute | Value |
|-----------|-------|
| **Official name** | Identity & Access |
| **Objective** | Authentication (JWT, OIDC, API Key, Cookie), authorization (73 permissions, RBAC), multi-tenancy (3-layer RLS isolation), security events, JIT/BreakGlass/Delegation access |
| **Documentation** | `docs/11-review-modular/01-identity-access/` |
| **Frontend module** | `src/frontend/src/features/identity-access/` |
| **Backend module** | `src/modules/identityaccess/` |
| **DbContext (current)** | `IdentityDbContext` (16 DbSets) |
| **DbContext (target)** | `IdentityDbContext` |
| **Table prefix** | `iam_` |
| **Primary database** | PostgreSQL |
| **Uses ClickHouse** | NO |
| **Direct dependencies** | None (foundational) |
| **Depended on by** | All 12 other modules |
| **Functional owner** | Platform Admin / Security |
| **Technical owner** | Backend Core Team |
| **Boundary status** | **PARTIAL** — Environment Management entities still embedded here |

---

## 02 — Environment Management

| Attribute | Value |
|-----------|-------|
| **Official name** | Environment Management |
| **Objective** | Environment lifecycle (Dev, Staging, Production), environment policies, criticality levels, drift detection, promotion validation, access control per environment |
| **Documentation** | `docs/11-review-modular/02-environment-management/` |
| **Frontend module** | `src/frontend/src/features/identity-access/` (shared, pages like EnvironmentsPage) |
| **Backend module** | `src/modules/identityaccess/` (shared — no dedicated module yet) |
| **DbContext (current)** | `IdentityDbContext` (shared) |
| **DbContext (target)** | `EnvironmentDbContext` (to be created) |
| **Table prefix** | `env_` |
| **Primary database** | PostgreSQL |
| **Uses ClickHouse** | NO |
| **Direct dependencies** | Identity & Access (strong — currently integrated) |
| **Depended on by** | Configuration, Change Governance, Operational Intelligence |
| **Functional owner** | Platform Admin |
| **Technical owner** | Backend Core Team |
| **Boundary status** | **NEEDS_REFACTOR** — Not a bounded context, entities dispersed in Identity |

---

## 03 — Service Catalog

| Attribute | Value |
|-----------|-------|
| **Official name** | Service Catalog |
| **Objective** | Central registry of services (source of truth), API catalog, dependency graph, topology visualization, Developer Portal, health monitoring |
| **Documentation** | `docs/11-review-modular/03-catalog/` |
| **Frontend module** | `src/frontend/src/features/catalog/` (12 pages) |
| **Backend module** | `src/modules/catalog/` (256 files) |
| **DbContext (current)** | `CatalogGraphDbContext` (8 DbSets), `DeveloperPortalDbContext` (5 DbSets) |
| **DbContext (target)** | `CatalogDbContext` (consolidate Graph + DeveloperPortal) |
| **Table prefix** | `cat_` |
| **Primary database** | PostgreSQL |
| **Uses ClickHouse** | NO |
| **Direct dependencies** | Identity & Access |
| **Depended on by** | Contracts, Change Governance, Operational Intelligence, Audit & Compliance, Governance |
| **Functional owner** | Architect / Tech Lead |
| **Technical owner** | Backend Core Team |
| **Boundary status** | **PARTIAL** — ContractsDbContext still resides here, needs extraction |

---

## 04 — Contracts

| Attribute | Value |
|-----------|-------|
| **Official name** | Contracts |
| **Objective** | API contract governance as first-class citizens: drafts, versioning, Spectral validation, lifecycle (Draft→InReview→Published→Deprecated), compliance scoring, canonical entities, export |
| **Documentation** | `docs/11-review-modular/04-contracts/` |
| **Frontend module** | `src/frontend/src/features/contracts/` (8 pages, 3 broken routes) |
| **Backend module** | `src/modules/catalog/` (subdomain — **architectural coupling**) |
| **DbContext (current)** | `ContractsDbContext` (7 DbSets, in nextraceone_catalog) |
| **DbContext (target)** | `ContractsDbContext` (to be extracted from Catalog project) |
| **Table prefix** | `ctr_` |
| **Primary database** | PostgreSQL |
| **Uses ClickHouse** | NO |
| **Direct dependencies** | Identity & Access, Service Catalog (service references), AI & Knowledge (AI-assisted generation) |
| **Depended on by** | Change Governance (compatibility checks), Developer Portal, Source of Truth views |
| **Functional owner** | Architect / Tech Lead |
| **Technical owner** | Backend Core Team |
| **Boundary status** | **NEEDS_REFACTOR** — Backend physically in Catalog, 3 broken frontend routes |

---

## 05 — Change Governance

| Attribute | Value |
|-----------|-------|
| **Official name** | Change Governance |
| **Objective** | Change Confidence pillar: release tracking, risk scoring, blast radius analysis, post-release review, workflow templates, promotion gates, ruleset governance, freeze windows |
| **Documentation** | `docs/11-review-modular/05-change-governance/` |
| **Frontend module** | `src/frontend/src/features/change-governance/` (6 pages) |
| **Backend module** | `src/modules/changegovernance/` (232 files) |
| **DbContext (current)** | `ChangeIntelligenceDbContext` (10), `WorkflowDbContext` (6), `PromotionDbContext` (4), `RulesetGovernanceDbContext` (3) |
| **DbContext (target)** | `ChangeGovernanceDbContext` (consider consolidation of 4 contexts) |
| **Table prefix** | `chg_` |
| **Primary database** | PostgreSQL |
| **Uses ClickHouse** | NO |
| **Direct dependencies** | Identity & Access, Service Catalog (blast radius), Configuration (workflow templates, freeze windows) |
| **Depended on by** | Operational Intelligence (incident correlation) |
| **Functional owner** | Tech Lead / Engineer |
| **Technical owner** | Backend Core Team |
| **Boundary status** | **CLEAR** — Well-defined bounded context with dedicated backend project |

---

## 06 — Operational Intelligence

| Attribute | Value |
|-----------|-------|
| **Official name** | Operational Intelligence |
| **Objective** | Incidents & mitigation, automation workflows, reliability scoring, runtime health, cost tracking (FinOps operational), runbooks |
| **Documentation** | `docs/11-review-modular/06-operational-intelligence/` |
| **Frontend module** | `src/frontend/src/features/operations/` (10 pages) + embryonic `operational-intelligence/` (1 file) |
| **Backend module** | `src/modules/operationalintelligence/` (5 subdomains) |
| **DbContext (current)** | `IncidentDbContext` (5), `AutomationDbContext` (3), `ReliabilityDbContext` (1), `RuntimeIntelligenceDbContext` (4), `CostIntelligenceDbContext` (6) |
| **DbContext (target)** | `OperationalIntelligenceDbContext` (consider consolidation of 5 contexts) |
| **Table prefix** | `ops_` |
| **Primary database** | PostgreSQL |
| **Uses ClickHouse** | YES — runtime metrics, cost analytics, time-series telemetry |
| **Direct dependencies** | Identity & Access, Service Catalog (ServiceId references), Change Governance (change correlation) |
| **Depended on by** | Governance (operational reports) |
| **Functional owner** | Engineer / Tech Lead |
| **Technical owner** | Backend Core Team |
| **Boundary status** | **CLEAR** — Well-defined bounded context, 5 subdomains properly isolated |

---

## 07 — AI & Knowledge

| Attribute | Value |
|-----------|-------|
| **Official name** | AI & Knowledge |
| **Objective** | AI intelligence pillar with 3 subdomains: AI Core (model registry, providers, policies, routing, budgets), Agents (agent definitions, orchestration, executions), Knowledge (capture, retrieval, operational context) |
| **Documentation** | `docs/11-review-modular/07-ai-knowledge/` |
| **Frontend module** | `src/frontend/src/features/ai-hub/` (11 pages) |
| **Backend module** | `src/modules/aiknowledge/` (278 files) |
| **DbContext (current)** | `AiGovernanceDbContext` (19+ DbSets), `AiOrchestrationDbContext` (4), `ExternalAiDbContext` (4) |
| **DbContext (target)** | `AiKnowledgeDbContext` (consider consolidation of 3 contexts) |
| **Table prefix** | `aik_` |
| **Primary database** | PostgreSQL |
| **Uses ClickHouse** | YES — AI usage analytics, token consumption metrics |
| **Direct dependencies** | Identity & Access, Contracts (knowledge context), Service Catalog (topology context), Change Governance (change context), Operational Intelligence (incident context) |
| **Depended on by** | Contracts (AI-assisted generation), Change Governance (assistant panel) |
| **Functional owner** | Architect / Platform Admin |
| **Technical owner** | AI Team |
| **Boundary status** | **PARTIAL** — Backend 25% maturity, frontend 70% creates perception gap |

---

## 08 — Governance

| Attribute | Value |
|-----------|-------|
| **Official name** | Governance |
| **Objective** | Executive views, compliance, risk assessment, FinOps governance, policies, governance packs, evidence, waivers, reports, teams, domains |
| **Documentation** | `docs/11-review-modular/08-governance/` |
| **Frontend module** | `src/frontend/src/features/governance/` (25 pages) |
| **Backend module** | `src/modules/governance/` (18 endpoint modules) |
| **DbContext (current)** | `GovernanceDbContext` (12 DbSets — includes Integrations and Product Analytics entities) |
| **DbContext (target)** | `GovernanceDbContext` (after extracting Integrations and Product Analytics entities) |
| **Table prefix** | `gov_` |
| **Primary database** | PostgreSQL |
| **Uses ClickHouse** | YES — compliance analytics, risk trend analysis, FinOps aggregated reporting |
| **Direct dependencies** | Identity & Access, Service Catalog (service data), Change Governance (correlation) |
| **Depended on by** | Executive views, cross-module reports |
| **Functional owner** | Executive / Auditor |
| **Technical owner** | Backend Core Team |
| **Boundary status** | **NEEDS_REFACTOR** — Catch-all with 15 subdomains; Integrations and Product Analytics must be extracted |

---

## 09 — Configuration

| Attribute | Value |
|-----------|-------|
| **Official name** | Configuration |
| **Objective** | Centralized configuration store (~345 definitions across 8 phases): instance settings, policies, notifications, workflows, governance, catalog/contracts/change, operations/incidents/FinOps, AI/integrations |
| **Documentation** | `docs/11-review-modular/09-configuration/` |
| **Frontend module** | `src/frontend/src/features/configuration/` (2 pages + 6 distributed) |
| **Backend module** | `src/modules/configuration/` |
| **DbContext (current)** | `ConfigurationDbContext` (3 DbSets — uses EnsureCreated) |
| **DbContext (target)** | `ConfigurationDbContext` (must migrate to proper migrations) |
| **Table prefix** | `cfg_` |
| **Primary database** | PostgreSQL |
| **Uses ClickHouse** | NO |
| **Direct dependencies** | Identity & Access |
| **Depended on by** | All 12 other modules (transversal) |
| **Functional owner** | Platform Admin |
| **Technical owner** | Backend Core Team |
| **Boundary status** | **CLEAR** — Well-defined bounded context; critical issue is EnsureCreated usage |

---

## 10 — Audit & Compliance

| Attribute | Value |
|-----------|-------|
| **Official name** | Audit & Compliance |
| **Objective** | Immutable audit trail, cryptographic hash chain, compliance policies, audit campaigns, data retention, compliance reporting |
| **Documentation** | `docs/11-review-modular/10-audit-compliance/` |
| **Frontend module** | `src/frontend/src/features/audit-compliance/` (1 page — AuditPage) |
| **Backend module** | `src/modules/auditcompliance/` |
| **DbContext (current)** | `AuditDbContext` (6 DbSets) |
| **DbContext (target)** | `AuditDbContext` |
| **Table prefix** | `aud_` |
| **Primary database** | PostgreSQL |
| **Uses ClickHouse** | NO |
| **Direct dependencies** | Identity & Access (SecurityAuditBridge) |
| **Depended on by** | All modules (via Outbox event publishing), Governance (compliance reports) |
| **Functional owner** | Auditor / Platform Admin |
| **Technical owner** | Backend Core Team |
| **Boundary status** | **CLEAR** — Well-defined bounded context; frontend coverage is minimal |

---

## 11 — Notifications

| Attribute | Value |
|-----------|-------|
| **Official name** | Notifications |
| **Objective** | Multi-channel notification center (Email, Teams): preferences, templates, routing, deduplication, grouping, digest, escalation, quiet hours |
| **Documentation** | `docs/11-review-modular/11-notifications/` |
| **Frontend module** | `src/frontend/src/features/notifications/` (3 pages) |
| **Backend module** | `src/modules/notifications/` |
| **DbContext (current)** | `NotificationsDbContext` (3 DbSets — uses EnsureCreated) |
| **DbContext (target)** | `NotificationsDbContext` (must migrate to proper migrations) |
| **Table prefix** | `ntf_` |
| **Primary database** | PostgreSQL |
| **Uses ClickHouse** | NO |
| **Direct dependencies** | All modules (event handlers consume events from 8+ sources) |
| **Depended on by** | Identity & Access (user preferences) |
| **Functional owner** | Engineer / Platform Admin |
| **Technical owner** | Backend Core Team |
| **Boundary status** | **CLEAR** — Well-defined bounded context; critical issue is EnsureCreated usage |

---

## 12 — Integrations

| Attribute | Value |
|-----------|-------|
| **Official name** | Integrations |
| **Objective** | Integration hub: connector management, data ingestion, execution monitoring, freshness tracking |
| **Documentation** | `docs/11-review-modular/12-integrations/` |
| **Frontend module** | `src/frontend/src/features/integrations/` (4 pages) |
| **Backend module** | `src/modules/governance/` (**architectural coupling** — IntegrationHubEndpointModule) |
| **DbContext (current)** | `GovernanceDbContext` (shared — entities embedded in Governance) |
| **DbContext (target)** | `IntegrationsDbContext` (to be created) |
| **Table prefix** | `int_` |
| **Primary database** | PostgreSQL |
| **Uses ClickHouse** | YES — ingestion execution analytics, connector performance metrics |
| **Direct dependencies** | Governance (currently hosted here), Configuration (connector settings), Notifications (failure alerts) |
| **Depended on by** | None explicitly |
| **Functional owner** | Platform Admin / Tech Lead |
| **Technical owner** | Backend Core Team |
| **Boundary status** | **NEEDS_REFACTOR** — Backend physically in Governance, no dedicated DbContext |

---

## 13 — Product Analytics

| Attribute | Value |
|-----------|-------|
| **Official name** | Product Analytics |
| **Objective** | Product usage analytics: adoption by module, usage by persona, journey funnels, value tracking, engagement metrics |
| **Documentation** | `docs/11-review-modular/13-product-analytics/` |
| **Frontend module** | `src/frontend/src/features/product-analytics/` (5 pages) |
| **Backend module** | `src/modules/governance/` (**architectural coupling** — ProductAnalyticsEndpointModule) |
| **DbContext (current)** | `GovernanceDbContext` (shared — entities embedded in Governance) |
| **DbContext (target)** | `ProductAnalyticsDbContext` (to be created) |
| **Table prefix** | `pan_` |
| **Primary database** | PostgreSQL (transactional definitions), ClickHouse (analytical data) |
| **Uses ClickHouse** | YES — REQUIRED for event streams, aggregated metrics, time-series analytics |
| **Direct dependencies** | Governance (currently hosted here), Identity & Access (persona/user data), All modules (event tracking requires cross-module instrumentation) |
| **Depended on by** | None explicitly |
| **Functional owner** | Product / Executive |
| **Technical owner** | Backend Core Team |
| **Boundary status** | **NEEDS_REFACTOR** — Backend physically in Governance, no dedicated DbContext, data possibly simulated |

---

## Summary — Boundary Status

| Status | Modules |
|--------|---------|
| **CLEAR** | Change Governance, Operational Intelligence, Configuration, Audit & Compliance, Notifications |
| **PARTIAL** | Identity & Access, Service Catalog, AI & Knowledge |
| **NEEDS_REFACTOR** | Environment Management, Contracts, Governance, Integrations, Product Analytics |
