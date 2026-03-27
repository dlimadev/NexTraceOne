# Relatório de Páginas e Rotas do Frontend — NexTraceOne

> **Data:** 2025-07-14  
> **Versão:** 2.0  
> **Escopo:** Inventário completo de 108 páginas e 130+ rotas  
> **Status global:** GAP_IDENTIFIED  
> **Caminho do router:** `src/frontend/src/App.tsx`

---

## 1. Resumo

| Métrica | Valor |
|---------|-------|
| Total de páginas (componentes Page) | 108 |
| Total de rotas registadas em App.tsx | 130+ |
| Rotas públicas (sem auth) | 7 |
| Rotas protegidas | 123+ |
| Rotas quebradas (sidebar sem App.tsx) | 3 |
| Páginas órfãs (sem rota) | 9 |
| Páginas vazias (0 bytes) | 1 |
| Redirects configurados | 3 |

---

## 2. Rotas Públicas (7)

| Rota | Página | Ficheiro | Status |
|------|--------|----------|--------|
| `/login` | LoginPage | `features/identity-access/pages/LoginPage.tsx` | ✅ FUNCIONAL |
| `/forgot-password` | ForgotPasswordPage | `features/identity-access/pages/ForgotPasswordPage.tsx` | ✅ FUNCIONAL |
| `/reset-password` | ResetPasswordPage | `features/identity-access/pages/ResetPasswordPage.tsx` | ✅ FUNCIONAL |
| `/activate` | ActivationPage | `features/identity-access/pages/ActivationPage.tsx` | ✅ FUNCIONAL |
| `/mfa` | MfaPage | `features/identity-access/pages/MfaPage.tsx` | ✅ FUNCIONAL |
| `/invitation` | InvitationPage | `features/identity-access/pages/InvitationPage.tsx` | ⚠️ Eager import (redirect) |
| `/select-tenant` | TenantSelectionPage | `features/identity-access/pages/TenantSelectionPage.tsx` | ✅ FUNCIONAL |

---

## 3. Rotas Protegidas por Módulo

### 3.1 Home e Busca Global

| Rota | Página | Permissão | Menu | Status |
|------|--------|-----------|------|--------|
| `/` | DashboardPage | *(nenhuma)* | ✅ home | ✅ FUNCIONAL |
| `/search` | GlobalSearchPage | `catalog:assets:read` | Via topbar | ✅ FUNCIONAL |

### 3.2 Catalog & Source of Truth

| Rota | Página | Ficheiro | Permissão | Menu | Status |
|------|--------|----------|-----------|------|--------|
| `/services` | ServiceCatalogListPage | `catalog/pages/ServiceCatalogListPage.tsx` | `catalog:assets:read` | ✅ services | ✅ FUNCIONAL |
| `/services/graph` | ServiceCatalogPage | `catalog/pages/ServiceCatalogPage.tsx` | `catalog:assets:read` | ✅ services | ✅ FUNCIONAL |
| `/services/:serviceId` | ServiceDetailPage | `catalog/pages/ServiceDetailPage.tsx` | `catalog:assets:read` | Navegação | ✅ FUNCIONAL |
| `/source-of-truth` | SourceOfTruthExplorerPage | `catalog/pages/SourceOfTruthExplorerPage.tsx` | `catalog:assets:read` | ✅ knowledge | ✅ FUNCIONAL |
| `/source-of-truth/services/:serviceId` | ServiceSourceOfTruthPage | `catalog/pages/ServiceSourceOfTruthPage.tsx` | `catalog:assets:read` | Navegação | ✅ FUNCIONAL |
| `/source-of-truth/contracts/:contractVersionId` | ContractSourceOfTruthPage | `catalog/pages/ContractSourceOfTruthPage.tsx` | `catalog:assets:read` | Navegação | ✅ FUNCIONAL |
| `/portal/*` | DeveloperPortalPage | `catalog/pages/DeveloperPortalPage.tsx` | `developer-portal:read` | ✅ knowledge | ✅ FUNCIONAL |
| `/graph` | *(redirect)* | — | — | — | → `/services/graph` |

**Páginas órfãs do catalog:**

| Página | Ficheiro | Referência | Status |
|--------|----------|-----------|--------|
| ContractDetailPage | `catalog/pages/ContractDetailPage.tsx` | Nenhuma | ❌ ÓRFÃ |
| ContractListPage | `catalog/pages/ContractListPage.tsx` | Nenhuma | ❌ ÓRFÃ |
| ContractsPage | `catalog/pages/ContractsPage.tsx` | Nenhuma | ❌ ÓRFÃ |
| CatalogContractsConfigurationPage | `catalog/pages/CatalogContractsConfigurationPage.tsx` | Via admin | ⚠️ ROTA ADMIN |

### 3.3 Contracts

| Rota | Página | Ficheiro | Permissão | Menu | Status |
|------|--------|----------|-----------|------|--------|
| `/contracts` | ContractCatalogPage | `contracts/catalog/ContractCatalogPage.tsx` | `contracts:read` | ✅ contracts | ✅ FUNCIONAL |
| `/contracts/new` | CreateServicePage | `contracts/create/CreateServicePage.tsx` | `contracts:write` | ✅ contracts | ✅ FUNCIONAL |
| `/contracts/studio/:draftId` | DraftStudioPage | `contracts/studio/DraftStudioPage.tsx` | `contracts:write` | ✅ contracts | ✅ FUNCIONAL |
| `/contracts/studio` | *(redirect)* | — | — | — | → `/contracts` |
| `/contracts/legacy` | *(redirect)* | — | — | — | → `/contracts` |
| `/contracts/:contractVersionId` | ContractWorkspacePage | `contracts/workspace/ContractWorkspacePage.tsx` | `contracts:read` | Navegação | ✅ FUNCIONAL |

**Rotas quebradas (sidebar → sem registo em App.tsx):**

| Rota do Sidebar | Página existente | Ficheiro | Status |
|-----------------|-----------------|----------|--------|
| `/contracts/governance` | ContractGovernancePage | `contracts/governance/ContractGovernancePage.tsx` | ❌ QUEBRADA |
| `/contracts/spectral` | SpectralRulesetManagerPage | `contracts/spectral/SpectralRulesetManagerPage.tsx` | ❌ QUEBRADA |
| `/contracts/canonical` | CanonicalEntityCatalogPage | `contracts/canonical/CanonicalEntityCatalogPage.tsx` | ❌ QUEBRADA |

**Página órfã adicional:**

| Página | Ficheiro | Status |
|--------|----------|--------|
| ContractPortalPage | `contracts/portal/ContractPortalPage.tsx` | ❌ ÓRFÃ (sem referência) |

### 3.4 Change Governance

| Rota | Página | Ficheiro | Permissão | Menu | Status |
|------|--------|----------|-----------|------|--------|
| `/changes` | ChangeCatalogPage | `change-governance/pages/ChangeCatalogPage.tsx` | `change-intelligence:read` | ✅ changes | ✅ FUNCIONAL |
| `/changes/:changeId` | ChangeDetailPage | `change-governance/pages/ChangeDetailPage.tsx` | `change-intelligence:read` | Navegação | ✅ FUNCIONAL |
| `/releases` | ReleasesPage | `change-governance/pages/ReleasesPage.tsx` | `change-intelligence:releases:read` | ✅ changes | ✅ FUNCIONAL |
| `/workflow` | WorkflowPage | `change-governance/pages/WorkflowPage.tsx` | `workflow:read` | ✅ changes | ✅ FUNCIONAL |
| `/promotion` | PromotionPage | `change-governance/pages/PromotionPage.tsx` | `promotion:read` | ✅ changes | ✅ FUNCIONAL |
| `/workflow/configuration` | WorkflowConfigurationPage | `change-governance/pages/WorkflowConfigurationPage.tsx` | Admin | Via admin | ✅ FUNCIONAL |

### 3.5 Operations

| Rota | Página | Ficheiro | Permissão | Menu | Status |
|------|--------|----------|-----------|------|--------|
| `/operations/incidents` | IncidentsPage | `operations/pages/IncidentsPage.tsx` | `operations:incidents:read` | ✅ operations | ✅ FUNCIONAL |
| `/operations/incidents/:incidentId` | IncidentDetailPage | `operations/pages/IncidentDetailPage.tsx` | `operations:incidents:read` | Navegação | ✅ FUNCIONAL |
| `/operations/runbooks` | RunbooksPage | `operations/pages/RunbooksPage.tsx` | `operations:runbooks:read` | ✅ operations | ✅ FUNCIONAL |
| `/operations/reliability` | TeamReliabilityPage | `operations/pages/TeamReliabilityPage.tsx` | `operations:reliability:read` | ✅ operations | ✅ FUNCIONAL |
| `/operations/reliability/:serviceId` | ServiceReliabilityDetailPage | `operations/pages/ServiceReliabilityDetailPage.tsx` | `operations:reliability:read` | Navegação | ✅ FUNCIONAL |
| `/operations/automation` | AutomationWorkflowsPage | `operations/pages/AutomationWorkflowsPage.tsx` | `operations:automation:read` | ✅ operations | ✅ FUNCIONAL |
| `/operations/automation/:workflowId` | AutomationWorkflowDetailPage | `operations/pages/AutomationWorkflowDetailPage.tsx` | `operations:automation:read` | Navegação | ✅ FUNCIONAL |
| `/operations/runtime-comparison` | EnvironmentComparisonPage | `operations/pages/EnvironmentComparisonPage.tsx` | `operations:runtime:read` | ✅ operations | ✅ FUNCIONAL |
| `/platform/operations` | PlatformOperationsPage | `operations/pages/PlatformOperationsPage.tsx` | `platform:admin:read` | ✅ admin | ✅ FUNCIONAL |

### 3.6 AI Hub

| Rota | Página | Ficheiro | Permissão | Menu | Status |
|------|--------|----------|-----------|------|--------|
| `/ai/assistant` | AiAssistantPage | `ai-hub/pages/AiAssistantPage.tsx` | `ai:assistant:read` | ✅ aiHub | ✅ FUNCIONAL |
| `/ai/agents` | AiAgentsPage | `ai-hub/pages/AiAgentsPage.tsx` | `ai:assistant:read` | ✅ aiHub | ✅ FUNCIONAL |
| `/ai/agents/:agentId` | AgentDetailPage | `ai-hub/pages/AgentDetailPage.tsx` | `ai:assistant:read` | Navegação | ✅ FUNCIONAL |
| `/ai/models` | ModelRegistryPage | `ai-hub/pages/ModelRegistryPage.tsx` | `ai:governance:read` | ✅ aiHub | ✅ FUNCIONAL |
| `/ai/policies` | AiPoliciesPage | `ai-hub/pages/AiPoliciesPage.tsx` | `ai:governance:read` | ✅ aiHub | ✅ FUNCIONAL |
| `/ai/routing` | AiRoutingPage | `ai-hub/pages/AiRoutingPage.tsx` | `ai:governance:read` | ✅ aiHub | ✅ FUNCIONAL |
| `/ai/ide` | IdeIntegrationsPage | `ai-hub/pages/IdeIntegrationsPage.tsx` | `ai:governance:read` | ✅ aiHub | ✅ FUNCIONAL |
| `/ai/budgets` | TokenBudgetPage | `ai-hub/pages/TokenBudgetPage.tsx` | `ai:governance:read` | ✅ aiHub | ✅ FUNCIONAL |
| `/ai/audit` | AiAuditPage | `ai-hub/pages/AiAuditPage.tsx` | `ai:governance:read` | ✅ aiHub | ✅ FUNCIONAL |
| `/ai/analysis` | AiAnalysisPage | `ai-hub/pages/AiAnalysisPage.tsx` | `ai:runtime:write` | ✅ aiHub | ✅ FUNCIONAL |
| `/ai/integrations/config` | AiIntegrationsConfigurationPage | `ai-hub/pages/AiIntegrationsConfigurationPage.tsx` | Admin | Via admin | ✅ FUNCIONAL |

### 3.7 Governance

| Rota | Página | Ficheiro | Permissão | Menu | Status |
|------|--------|----------|-----------|------|--------|
| `/governance/executive` | ExecutiveOverviewPage | `governance/pages/ExecutiveOverviewPage.tsx` | `governance:read` | ✅ governance | ✅ FUNCIONAL |
| `/governance/executive/:area` | ExecutiveDrillDownPage | `governance/pages/ExecutiveDrillDownPage.tsx` | `governance:read` | Navegação | ✅ FUNCIONAL |
| `/governance/executive/finops` | ExecutiveFinOpsPage | `governance/pages/ExecutiveFinOpsPage.tsx` | `governance:read` | Navegação | ✅ FUNCIONAL |
| `/governance/reports` | ReportsPage | `governance/pages/ReportsPage.tsx` | `governance:read` | ✅ governance | ✅ FUNCIONAL |
| `/governance/compliance` | CompliancePage | `governance/pages/CompliancePage.tsx` | `governance:read` | ✅ governance | ✅ FUNCIONAL |
| `/governance/risk` | RiskCenterPage | `governance/pages/RiskCenterPage.tsx` | `governance:read` | ✅ governance | ✅ FUNCIONAL |
| `/governance/risk/heatmap` | RiskHeatmapPage | `governance/pages/RiskHeatmapPage.tsx` | `governance:read` | Navegação | ✅ FUNCIONAL |
| `/governance/finops` | FinOpsPage | `governance/pages/FinOpsPage.tsx` | `governance:read` | ✅ governance | ✅ FUNCIONAL |
| `/governance/finops/domain/:id` | DomainFinOpsPage | `governance/pages/DomainFinOpsPage.tsx` | `governance:read` | Navegação | ✅ FUNCIONAL |
| `/governance/finops/service/:id` | ServiceFinOpsPage | `governance/pages/ServiceFinOpsPage.tsx` | `governance:read` | Navegação | ✅ FUNCIONAL |
| `/governance/finops/team/:id` | TeamFinOpsPage | `governance/pages/TeamFinOpsPage.tsx` | `governance:read` | Navegação | ✅ FUNCIONAL |
| `/governance/policies` | PolicyCatalogPage | `governance/pages/PolicyCatalogPage.tsx` | `governance:read` | ✅ governance | ✅ FUNCIONAL |
| `/governance/packs` | GovernancePacksOverviewPage | `governance/pages/GovernancePacksOverviewPage.tsx` | `governance:read` | ✅ governance | ✅ FUNCIONAL |
| `/governance/packs/:packId` | GovernancePackDetailPage | `governance/pages/GovernancePackDetailPage.tsx` | `governance:read` | Navegação | ✅ FUNCIONAL |
| `/governance/teams` | TeamsOverviewPage | `governance/pages/TeamsOverviewPage.tsx` | `governance:read` | ✅ organization | ✅ FUNCIONAL |
| `/governance/teams/:teamId` | TeamDetailPage | `governance/pages/TeamDetailPage.tsx` | `governance:read` | Navegação | ✅ FUNCIONAL |
| `/governance/domains` | DomainsOverviewPage | `governance/pages/DomainsOverviewPage.tsx` | `governance:read` | ✅ organization | ✅ FUNCIONAL |
| `/governance/domains/:domainId` | DomainDetailPage | `governance/pages/DomainDetailPage.tsx` | `governance:read` | Navegação | ✅ FUNCIONAL |
| `/governance/benchmarking` | BenchmarkingPage | `governance/pages/BenchmarkingPage.tsx` | `governance:read` | Via reports | ✅ FUNCIONAL |
| `/governance/maturity` | MaturityScorecardsPage | `governance/pages/MaturityScorecardsPage.tsx` | `governance:read` | Via reports | ✅ FUNCIONAL |
| `/governance/controls` | EnterpriseControlsPage | `governance/pages/EnterpriseControlsPage.tsx` | `governance:read` | Via compliance | ✅ FUNCIONAL |
| `/governance/evidence` | EvidencePackagesPage | `governance/pages/EvidencePackagesPage.tsx` | `governance:read` | Via compliance | ✅ FUNCIONAL |
| `/governance/waivers` | WaiversPage | `governance/pages/WaiversPage.tsx` | `governance:read` | Via policies | ✅ FUNCIONAL |
| `/governance/delegated-admin` | DelegatedAdminPage | `governance/pages/DelegatedAdminPage.tsx` | `governance:read` | Via admin | ✅ FUNCIONAL |
| `/governance/configuration` | GovernanceConfigurationPage | `governance/pages/GovernanceConfigurationPage.tsx` | `governance:read` | Via admin | ✅ FUNCIONAL |

### 3.8 Identity & Access (Admin)

| Rota | Página | Ficheiro | Permissão | Menu | Status |
|------|--------|----------|-----------|------|--------|
| `/users` | UsersPage | `identity-access/pages/UsersPage.tsx` | `identity:users:read` | ✅ admin | ✅ FUNCIONAL |
| `/break-glass` | BreakGlassPage | `identity-access/pages/BreakGlassPage.tsx` | `identity:sessions:read` | ✅ admin | ✅ FUNCIONAL |
| `/jit-access` | JitAccessPage | `identity-access/pages/JitAccessPage.tsx` | `identity:users:read` | ✅ admin | ✅ FUNCIONAL |
| `/delegations` | DelegationPage | `identity-access/pages/DelegationPage.tsx` | `identity:users:read` | ✅ admin | ✅ FUNCIONAL |
| `/access-reviews` | AccessReviewPage | `identity-access/pages/AccessReviewPage.tsx` | `identity:users:read` | ✅ admin | ✅ FUNCIONAL |
| `/my-sessions` | MySessionsPage | `identity-access/pages/MySessionsPage.tsx` | `identity:sessions:read` | ✅ admin | ✅ FUNCIONAL |
| `/environments` | EnvironmentsPage | `identity-access/pages/EnvironmentsPage.tsx` | Admin | Via admin | ✅ FUNCIONAL |
| `/unauthorized` | UnauthorizedPage | `identity-access/pages/UnauthorizedPage.tsx` | *(nenhuma)* | — | ✅ FUNCIONAL |

### 3.9 Outros Módulos

| Rota | Página | Módulo | Permissão | Menu | Status |
|------|--------|--------|-----------|------|--------|
| `/integrations` | IntegrationHubPage | integrations | `integrations:read` | ✅ integrations | ✅ FUNCIONAL |
| `/integrations/:connectorId` | ConnectorDetailPage | integrations | `integrations:read` | Navegação | ✅ FUNCIONAL |
| `/integrations/executions` | IngestionExecutionsPage | integrations | `integrations:read` | Navegação | ✅ FUNCIONAL |
| `/integrations/freshness` | IngestionFreshnessPage | integrations | `integrations:read` | Navegação | ✅ FUNCIONAL |
| `/analytics` | ProductAnalyticsOverviewPage | product-analytics | `analytics:read` | ✅ analytics | ✅ FUNCIONAL |
| `/analytics/adoption` | ModuleAdoptionPage | product-analytics | `analytics:read` | Navegação | ✅ FUNCIONAL |
| `/analytics/personas` | PersonaUsagePage | product-analytics | `analytics:read` | Navegação | ✅ FUNCIONAL |
| `/analytics/journeys` | JourneyFunnelPage | product-analytics | `analytics:read` | Navegação | ✅ FUNCIONAL |
| `/analytics/value` | ValueTrackingPage | product-analytics | `analytics:read` | Navegação | ✅ FUNCIONAL |
| `/audit` | AuditPage | audit-compliance | `audit:read` | ✅ admin | ✅ FUNCIONAL |
| `/platform/configuration` | ConfigurationAdminPage | configuration | `platform:admin:read` | ✅ admin | ✅ FUNCIONAL |
| `/notifications` | NotificationCenterPage | notifications | *(nenhuma)* | Via topbar | ✅ FUNCIONAL |
| `/notifications/configuration` | NotificationConfigurationPage | notifications | Admin | Via admin | ✅ FUNCIONAL |
| `/notifications/preferences` | NotificationPreferencesPage | notifications | *(nenhuma)* | Via perfil | ✅ FUNCIONAL |

---

## 4. Redirects Configurados

| De | Para | Tipo |
|----|------|------|
| `/graph` | `/services/graph` | Navigate (redirect) |
| `/contracts/studio` (sem parâmetro) | `/contracts` | Navigate (redirect) |
| `/contracts/legacy` | `/contracts` | Navigate (redirect) |

---

## 5. Consolidação de Problemas

### 5.1 Rotas Quebradas (3) — Prioridade CRITICAL

| # | Rota | Problema | Ação necessária |
|---|------|---------|----------------|
| 1 | `/contracts/governance` | Sidebar link existe, rota NÃO registada em App.tsx | Adicionar Route em App.tsx |
| 2 | `/contracts/spectral` | Sidebar link existe, rota NÃO registada em App.tsx | Adicionar Route em App.tsx |
| 3 | `/contracts/canonical` | Sidebar link existe, rota NÃO registada em App.tsx | Adicionar Route em App.tsx |

### 5.2 Páginas Órfãs (9) — Prioridade HIGH/MEDIUM

| # | Página | Ficheiro | Tipo | Prioridade | Ação sugerida |
|---|--------|----------|------|------------|--------------|
| 1 | ContractGovernancePage | `contracts/governance/ContractGovernancePage.tsx` | Sidebar sem rota | CRITICAL | Registar rota |
| 2 | SpectralRulesetManagerPage | `contracts/spectral/SpectralRulesetManagerPage.tsx` | Sidebar sem rota | CRITICAL | Registar rota |
| 3 | CanonicalEntityCatalogPage | `contracts/canonical/CanonicalEntityCatalogPage.tsx` | Sidebar sem rota | CRITICAL | Registar rota |
| 4 | ContractPortalPage | `contracts/portal/ContractPortalPage.tsx` | Sem referência | HIGH | Integrar ou remover |
| 5 | ContractDetailPage | `catalog/pages/ContractDetailPage.tsx` | Sem referência | HIGH | Legacy — remover |
| 6 | ContractListPage | `catalog/pages/ContractListPage.tsx` | Sem referência | HIGH | Legacy — remover |
| 7 | ContractsPage | `catalog/pages/ContractsPage.tsx` | Sem referência | HIGH | Legacy — remover |
| 8 | ProductAnalyticsOverviewPage | `product-analytics/ProductAnalyticsOverviewPage.tsx` | Ficheiro vazio | MEDIUM | Eliminar ficheiro |
| 9 | InvitationPage | `identity-access/pages/InvitationPage.tsx` | Eager import only | LOW | Documentar padrão |

---

## 6. Estatísticas por Status

| Status | Quantidade | Percentagem |
|--------|-----------|-------------|
| ✅ FUNCIONAL | 96 | ~89% |
| ❌ QUEBRADA (sidebar sem rota) | 3 | ~3% |
| ❌ ÓRFÃ (sem referência) | 6 | ~5% |
| ⚠️ PARCIAL (vazia ou especial) | 3 | ~3% |

---

*Documento gerado como parte da auditoria modular do NexTraceOne.*
