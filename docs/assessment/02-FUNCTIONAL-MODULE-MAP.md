# 02 — Functional Module Map

**Date:** 2026-03-22

---

## Module Overview

| Module | Frontend Feature | Pages | API Endpoints | Backend Features | Production Scope |
|--------|-----------------|-------|---------------|-----------------|-----------------|
| **IdentityAccess** | `identity-access` | 15 | 11 endpoint files | 42 | ✅ Included |
| **Catalog** | `catalog` + `contracts` | 11 + 7 = 18 | 5 endpoint modules | 83 | ✅ Mostly included |
| **ChangeGovernance** | `change-governance` | 5 | 10 endpoint files | 57 | ✅ Included |
| **AIKnowledge** | `ai-hub` | 10 | 5 endpoint modules | 68 | ⚠️ Partially excluded |
| **Governance** | `governance` | 24 | 15 endpoint modules | 73 | ⚠️ Partially excluded |
| **OperationalIntelligence** | `operations` | 10 | 7 endpoint modules | 52 | ⚠️ Partially excluded |
| **AuditCompliance** | `audit-compliance` | 1 | 1 endpoint module | 7 | ✅ Included |
| **Integrations** | `integrations` | 4 | (via Governance) | (via Governance) | ⚠️ Partially excluded |
| **ProductAnalytics** | `product-analytics` | 5 | (via Governance) | (via Governance) | ⚠️ Partially excluded |

---

## Detailed Module Maps

### 1. IdentityAccess

**Purpose:** User management, authentication, authorization, tenancy, environments, session management, advanced access controls.

| Feature | Backend | Frontend | Endpoint | Persistence | Status |
|---------|---------|----------|----------|-------------|--------|
| Login / Auth | ✅ | ✅ LoginPage, MfaPage | ✅ AuthEndpoints | ✅ IdentityDbContext | ✅ Complete |
| User Management | ✅ | ✅ UsersPage | ✅ UserEndpoints | ✅ | ✅ Complete |
| Tenant Management | ✅ | ✅ TenantSelectionPage | ✅ TenantEndpoints | ✅ | ✅ Complete |
| Environment Management | ✅ | ✅ EnvironmentsPage | ✅ EnvironmentEndpoints | ✅ | ✅ Complete |
| Role & Permissions | ✅ | (within UsersPage) | ✅ RolePermissionEndpoints | ✅ | ✅ Complete |
| Delegation | ✅ | ✅ DelegationPage | ✅ DelegationEndpoints | ✅ | ✅ Complete |
| Break Glass | ✅ | ✅ BreakGlassPage | ✅ BreakGlassEndpoints | ✅ | ✅ Complete |
| JIT Access | ✅ | ✅ JitAccessPage | ✅ JitAccessEndpoints | ✅ | ✅ Complete |
| Access Review | ✅ | ✅ AccessReviewPage | ✅ AccessReviewEndpoints | ✅ | ✅ Complete |
| Session Management | ✅ | ✅ MySessionsPage | ✅ RuntimeContextEndpoints | ✅ | ✅ Complete |
| Cookie Session | ✅ | (internal) | ✅ CookieSessionEndpoints | ✅ | ✅ Complete |
| Forgot/Reset Password | ✅ | ✅ ForgotPasswordPage, ResetPasswordPage | ✅ AuthEndpoints | ✅ | ✅ Complete |
| Account Activation | ✅ | ✅ ActivationPage | ✅ AuthEndpoints | ✅ | ✅ Complete |
| Invitation | ✅ | ✅ InvitationPage | ✅ AuthEndpoints | ✅ | ✅ Complete |

**Overall Completeness: ~92%**

---

### 2. Catalog (Services + Contracts + Source of Truth)

**Purpose:** Service catalog, contract management, contract studio, developer portal, source of truth.

| Feature | Backend | Frontend | Endpoint | Persistence | Status |
|---------|---------|----------|----------|-------------|--------|
| Service Catalog | ✅ | ✅ ServiceCatalogPage, ServiceCatalogListPage, ServiceDetailPage | ✅ ServiceCatalogEndpointModule | ✅ CatalogGraphDbContext | ✅ Complete |
| Contract Management | ✅ | ✅ ContractsPage, ContractListPage, ContractDetailPage | ✅ ContractsEndpointModule | ✅ ContractsDbContext | ✅ Complete |
| Contract Studio | ✅ | ✅ DraftStudioPage, ContractWorkspacePage | ✅ ContractStudioEndpointModule | ✅ | ✅ Complete |
| Contract Creation | ✅ | ✅ CreateServicePage | ✅ | ✅ | ✅ Complete |
| Contract Catalog | ✅ | ✅ ContractCatalogPage | ✅ | ✅ | ✅ Complete |
| Contract Governance | ✅ | ✅ ContractGovernancePage | ✅ | ✅ | ✅ Complete |
| Canonical Entities | ✅ | ✅ CanonicalEntityCatalogPage | ✅ | ✅ | ✅ Complete |
| Source of Truth | ✅ | ✅ SourceOfTruthExplorerPage, ServiceSourceOfTruthPage, ContractSourceOfTruthPage | ✅ SourceOfTruthEndpointModule | ✅ | ✅ Complete |
| Developer Portal | ✅ | ✅ DeveloperPortalPage | ✅ DeveloperPortalEndpointModule | ✅ DeveloperPortalDbContext | ⚠️ Excluded (`/portal`) |
| Spectral Ruleset | ✅ | ✅ SpectralRulesetManagerPage | ✅ | ✅ | ✅ |
| Contract Portal | ✅ | ✅ ContractPortalPage | ✅ | ✅ | ⚠️ Excluded (`/portal`) |
| Global Search | ✅ | ✅ GlobalSearchPage | ✅ | ✅ | ✅ |

**Overall Completeness: ~85%** (Portal excluded)

---

### 3. ChangeGovernance

**Purpose:** Release management, change intelligence, blast radius, deployment tracking, promotion, workflow approvals.

| Feature | Backend | Frontend | Endpoint | Persistence | Status |
|---------|---------|----------|----------|-------------|--------|
| Change Catalog | ✅ | ✅ ChangeCatalogPage | ✅ ChangeIntelligenceEndpointModule | ✅ ChangeIntelligenceDbContext | ✅ Complete |
| Change Detail | ✅ | ✅ ChangeDetailPage | ✅ IntelligenceEndpoints | ✅ | ✅ Complete |
| Releases | ✅ | ✅ ReleasesPage | ✅ ReleaseQueryEndpoints | ✅ | ✅ Complete |
| Promotion | ✅ | ✅ PromotionPage | ✅ PromotionEndpointModule | ✅ PromotionDbContext | ✅ Complete |
| Workflow Approvals | ✅ | ✅ WorkflowPage | ✅ ApprovalEndpoints, StatusEndpoints, TemplateEndpoints, EvidencePackEndpoints | ✅ WorkflowDbContext | ✅ Complete |
| Analysis / Blast Radius | ✅ | (within ChangeDetailPage) | ✅ AnalysisEndpoints | ✅ | ✅ Complete |
| Change Confidence | ✅ | (within pages) | ✅ ChangeConfidenceEndpoints | ✅ | ✅ Complete |
| Deployments | ✅ | (within pages) | ✅ DeploymentEndpoints | ✅ | ✅ Complete |
| Freeze Periods | ✅ | (within pages) | ✅ FreezeEndpoints | ✅ | ✅ Complete |
| Ruleset Governance | ✅ | (via SpectralRulesetManagerPage) | ✅ RulesetGovernanceEndpointModule | ✅ RulesetGovernanceDbContext | ✅ Complete |

**Overall Completeness: ~88%**

---

### 4. AIKnowledge

**Purpose:** AI governance, model registry, agent management, policies, token budgets, external AI integration, routing, IDE extensions, audit.

| Feature | Backend | Frontend | Endpoint | Persistence | Status |
|---------|---------|----------|----------|-------------|--------|
| AI Assistant | ✅ | ✅ AiAssistantPage | ✅ AiRuntimeEndpointModule | ✅ | ✅ In scope |
| AI Agents | ✅ | ✅ AiAgentsPage, AgentDetailPage | ✅ AiGovernanceEndpointModule | ✅ AiGovernanceDbContext | ✅ In scope |
| AI Analysis | ✅ | ✅ AiAnalysisPage | ✅ AiOrchestrationEndpointModule | ✅ AiOrchestrationDbContext | ✅ In scope |
| Model Registry | ✅ | ✅ ModelRegistryPage | ✅ | ✅ | ⚠️ Excluded |
| AI Policies | ✅ | ✅ AiPoliciesPage | ✅ | ✅ | ⚠️ Excluded |
| AI Routing | ✅ | ✅ AiRoutingPage | ✅ | ✅ | ⚠️ Excluded |
| IDE Integrations | ✅ | ✅ IdeIntegrationsPage | ✅ AiIdeEndpointModule | ✅ | ⚠️ Excluded |
| Token Budget | ✅ | ✅ TokenBudgetPage | ✅ | ✅ | ⚠️ Excluded |
| AI Audit | ✅ | ✅ AiAuditPage | ✅ | ✅ | ⚠️ Excluded |
| External AI | ✅ | (internal) | ✅ ExternalAiEndpointModule | ✅ ExternalAiDbContext | ✅ |

**Overall Completeness: ~55%** (6 of 10 features excluded from production)

---

### 5. Governance

**Purpose:** Enterprise governance, teams, domains, compliance, FinOps, risk, waivers, evidence packages, executive views, policies, controls, benchmarking.

| Feature | Backend | Frontend | Endpoint | Persistence | Status |
|---------|---------|----------|----------|-------------|--------|
| Executive Overview | ✅ | ✅ ExecutiveOverviewPage | ✅ ExecutiveOverviewEndpointModule | ✅ GovernanceDbContext | ✅ In scope |
| Executive Drill-Down | ✅ | ✅ ExecutiveDrillDownPage | ✅ | ✅ | ⚠️ DemoBanner |
| Executive FinOps | ✅ | ✅ ExecutiveFinOpsPage | ✅ GovernanceFinOpsEndpointModule | ✅ | ✅ In scope |
| Teams Overview | ✅ | ✅ TeamsOverviewPage, TeamDetailPage | ✅ TeamEndpointModule | ✅ | ⚠️ Excluded |
| Domains Overview | ✅ | ✅ DomainsOverviewPage, DomainDetailPage | ✅ DomainEndpointModule | ✅ | ✅ In scope |
| Compliance | ✅ | ✅ CompliancePage | ✅ GovernanceComplianceEndpointModule, ComplianceChecksEndpointModule | ✅ | ✅ In scope |
| FinOps | ✅ | ✅ FinOpsPage, ServiceFinOpsPage, TeamFinOpsPage, DomainFinOpsPage | ✅ GovernanceFinOpsEndpointModule | ✅ | ⚠️ DemoBanner (all 4 pages) |
| Risk Center | ✅ | ✅ RiskCenterPage, RiskHeatmapPage | ✅ GovernanceRiskEndpointModule | ✅ | ✅ In scope |
| Reports | ✅ | ✅ ReportsPage | ✅ GovernanceReportsEndpointModule | ✅ | ✅ In scope |
| Governance Packs | ✅ | ✅ GovernancePacksOverviewPage, GovernancePackDetailPage | ✅ GovernancePacksEndpointModule | ✅ | ⚠️ Excluded |
| Waivers | ✅ | ✅ WaiversPage | ✅ GovernanceWaiversEndpointModule | ✅ | ✅ In scope |
| Evidence Packages | ✅ | ✅ EvidencePackagesPage | ✅ EvidencePackagesEndpointModule | ✅ | ✅ In scope |
| Enterprise Controls | ✅ | ✅ EnterpriseControlsPage | ✅ EnterpriseControlsEndpointModule | ✅ | ✅ In scope |
| Maturity Scorecards | ✅ | ✅ MaturityScorecardsPage | ✅ | ✅ | ✅ In scope |
| Policy Catalog | ✅ | ✅ PolicyCatalogPage | ✅ PolicyCatalogEndpointModule | ✅ | ✅ In scope |
| Benchmarking | ✅ | ✅ BenchmarkingPage | ✅ | ✅ | ⚠️ DemoBanner |
| Delegated Admin | ✅ | ✅ DelegatedAdminPage | ✅ DelegatedAdminEndpointModule | ✅ | ✅ In scope |
| Integration Hub | ✅ | ✅ IntegrationHubPage | ✅ IntegrationHubEndpointModule | ✅ | ✅ In scope |

**Overall Completeness: ~60%** (Teams/Packs excluded, FinOps/Benchmarking demo-only)

---

### 6. OperationalIntelligence

**Purpose:** Incidents, runbooks, reliability, automation, cost intelligence, runtime intelligence, environment comparison.

| Feature | Backend | Frontend | Endpoint | Persistence | Status |
|---------|---------|----------|----------|-------------|--------|
| Incidents | ✅ | ✅ IncidentsPage, IncidentDetailPage | ✅ IncidentEndpointModule, MitigationEndpointModule | ✅ IncidentDbContext | ✅ In scope |
| Runbooks | ✅ | ✅ RunbooksPage | ✅ RunbookEndpointModule | ✅ | ⚠️ Excluded |
| Team Reliability | ✅ | ✅ TeamReliabilityPage, ServiceReliabilityDetailPage | ✅ ReliabilityEndpointModule | ✅ ReliabilityDbContext | ⚠️ Excluded |
| Automation | ✅ | ✅ AutomationWorkflowsPage, AutomationWorkflowDetailPage, AutomationAdminPage | ✅ AutomationEndpointModule | ✅ AutomationDbContext | ⚠️ Excluded |
| Platform Operations | ✅ | ✅ PlatformOperationsPage | ✅ PlatformStatusEndpointModule | ✅ | ✅ In scope |
| Environment Comparison | ✅ | ✅ EnvironmentComparisonPage | ✅ RuntimeIntelligenceEndpointModule | ✅ RuntimeIntelligenceDbContext | ✅ In scope |
| Cost Intelligence | ✅ | (via FinOps pages) | ✅ CostIntelligenceEndpointModule | ✅ CostIntelligenceDbContext | ⚠️ Demo |

**Overall Completeness: ~55%** (Runbooks, Reliability, Automation excluded)

---

### 7. AuditCompliance

**Purpose:** Audit trail, compliance reporting.

| Feature | Backend | Frontend | Endpoint | Persistence | Status |
|---------|---------|----------|----------|-------------|--------|
| Audit Trail | ✅ | ✅ AuditPage | ✅ AuditEndpointModule (6 endpoints) | ✅ AuditDbContext | ✅ In scope |

**Overall Completeness: ~65%** (minimal entity model, 0 tests)

---

### 8. ProductAnalytics (frontend feature, backend via Governance)

| Feature | Backend | Frontend | Status |
|---------|---------|----------|--------|
| Product Analytics Overview | ✅ via ProductAnalyticsEndpointModule | ✅ ProductAnalyticsOverviewPage | ✅ In scope |
| Module Adoption | ✅ | ✅ ModuleAdoptionPage | ✅ |
| Persona Usage | ✅ | ✅ PersonaUsagePage | ✅ |
| Journey Funnel | ✅ | ✅ JourneyFunnelPage | ✅ |
| Value Tracking | ✅ | ✅ ValueTrackingPage | ⚠️ Excluded |

**Overall Completeness: ~75%** (Value Tracking excluded)

---

### 9. Integrations (frontend feature, backend via Governance)

| Feature | Backend | Frontend | Status |
|---------|---------|----------|--------|
| Integration Hub | ✅ IntegrationHubEndpointModule | ✅ IntegrationHubPage | ✅ In scope |
| Connector Detail | ✅ | ✅ ConnectorDetailPage | ✅ |
| Ingestion Freshness | ✅ | ✅ IngestionFreshnessPage | ✅ |
| Ingestion Executions | ✅ | ✅ IngestionExecutionsPage | ⚠️ Excluded |

**Overall Completeness: ~75%** (Executions excluded)

---

## Production Scope Summary (releaseScope.ts)

### Routes INCLUDED in production:
`/`, `/login`, `/forgot-password`, `/reset-password`, `/activate`, `/mfa`, `/invitation`, `/select-tenant`, `/search`, `/source-of-truth`, `/services`, `/graph`, `/contracts`, `/changes`, `/releases`, `/workflow`, `/promotion`, `/operations`, `/ai`, `/users`, `/audit`, `/break-glass`, `/jit-access`, `/delegations`, `/access-reviews`, `/my-sessions`, `/unauthorized`, `/portal`, `/governance`, `/integrations`, `/analytics`, `/platform`

### Routes EXCLUDED from production (14 prefixes):
`/portal`, `/governance/teams`, `/governance/packs`, `/integrations/executions`, `/analytics/value`, `/operations/runbooks`, `/operations/reliability`, `/operations/automation`, `/ai/models`, `/ai/policies`, `/ai/routing`, `/ai/ide`, `/ai/budgets`, `/ai/audit`
