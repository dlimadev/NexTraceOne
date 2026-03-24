# Inventário de Módulos do Frontend — NexTraceOne

> **Data:** 2025-07-14  
> **Versão:** 2.0  
> **Escopo:** Inventário completo dos 14 módulos de features  
> **Status global:** GAP_IDENTIFIED  
> **Caminho base:** `src/frontend/src/features/`

---

## 1. Tabela Resumo Consolidada

| # | Módulo | Propósito | Ficheiros | Páginas | Rotas ativas | Itens menu | API | Classificação |
|---|--------|-----------|-----------|---------|-------------|------------|-----|---------------|
| 1 | contracts | Gestão de contratos de serviço (REST, SOAP, eventos) | 70 | 8 | 5 | 6 | 3 ficheiros | PARTIAL |
| 2 | governance | Governance organizacional, FinOps, risk, compliance | 30 | 25 | 25+ | 7+2 | 4 ficheiros | COMPLETE_APPARENT |
| 3 | catalog | Catálogo de serviços, Source of Truth, Developer Portal | 23 | 12 | 9 | 2 | 7 ficheiros | PARTIAL |
| 4 | identity-access | Autenticação, utilizadores, sessões, delegações | 23 | 15 | 15 | 8 | 2 ficheiros | COMPLETE_APPARENT |
| 5 | ai-hub | Assistente IA, agentes, governance, modelo, budgets | 15 | 11 | 11 | 9 | 2 ficheiros | COMPLETE_APPARENT |
| 6 | operations | Incidentes, runbooks, reliability, automação | 16 | 10 | 10 | 5 | 5 ficheiros | COMPLETE_APPARENT |
| 7 | change-governance | Change intelligence, releases, workflow, promoção | 13 | 6 | 6 | 4 | 5 ficheiros | COMPLETE_APPARENT |
| 8 | notifications | Centro de notificações e preferências | 11 | 3 | 3 | 0 | 1 ficheiro | COMPLETE_APPARENT |
| 9 | product-analytics | Métricas de produto, adoção, personas, jornadas | 8 | 6 | 5 | 1 | 1 ficheiro | PARTIAL |
| 10 | integrations | Hub de integrações e conectores | 6 | 4 | 4 | 1 | 1 ficheiro | COMPLETE_APPARENT |
| 11 | configuration | Configuração avançada da plataforma | 6 | 2 | 2 | 1 | 1 ficheiro | COMPLETE_APPARENT |
| 12 | audit-compliance | Logs de auditoria e compliance | 4 | 1 | 1 | 1 | 2 ficheiros | COMPLETE_APPARENT |
| 13 | operational-intelligence | FinOps operacional e configuração | 1 | 1 | 1 | 0 | 0 | PARTIAL |
| 14 | shared | Componentes partilhados (Dashboard) | 2 | 1 | 1 | 1 | N/A | N/A |

**Totais:** 228 ficheiros | 108 páginas | 130+ rotas | 45+ itens menu | 34 ficheiros API

---

## 2. Detalhe por Módulo

---

### 2.1 contracts (70 ficheiros)

**Propósito:** Gestão completa do ciclo de vida de contratos — catálogo, criação, edição em studio, governance, regras Spectral, entidades canónicas.

**Classificação:** PARTIAL  
**Prioridade de correção:** CRITICAL

**Páginas (8):**

| Página | Ficheiro | Rota | Status |
|--------|----------|------|--------|
| ContractCatalogPage | `contracts/catalog/ContractCatalogPage.tsx` | `/contracts` | ✅ Funcional |
| CreateServicePage | `contracts/create/CreateServicePage.tsx` | `/contracts/new` | ✅ Funcional |
| DraftStudioPage | `contracts/studio/DraftStudioPage.tsx` | `/contracts/studio/:draftId` | ✅ Funcional |
| ContractWorkspacePage | `contracts/workspace/ContractWorkspacePage.tsx` | `/contracts/:contractVersionId` | ✅ Funcional |
| ContractGovernancePage | `contracts/governance/ContractGovernancePage.tsx` | `/contracts/governance` | ❌ SEM ROTA |
| SpectralRulesetManagerPage | `contracts/spectral/SpectralRulesetManagerPage.tsx` | `/contracts/spectral` | ❌ SEM ROTA |
| CanonicalEntityCatalogPage | `contracts/canonical/CanonicalEntityCatalogPage.tsx` | `/contracts/canonical` | ❌ SEM ROTA |
| ContractPortalPage | `contracts/portal/ContractPortalPage.tsx` | — | ❌ ÓRFÃ |

**API (3 ficheiros):**
- `contracts/api/contracts.ts` — CRUD de contratos
- `contracts/api/contractStudio.ts` — operações do studio

**Problemas identificados:**
1. ❌ 3 páginas têm link no sidebar mas a rota NÃO está em `App.tsx`
2. ❌ `ContractPortalPage.tsx` não é referenciada em nenhum router ou menu
3. ⚠️ Módulo é o maior do frontend (70 ficheiros) — candidato a reorganização interna

---

### 2.2 governance (30 ficheiros)

**Propósito:** Governance organizacional incluindo executivo, relatórios, compliance, risk center, FinOps, políticas e packs.

**Classificação:** COMPLETE_APPARENT  
**Prioridade de correção:** LOW

**Páginas (25):**

| Página | Ficheiro | Rota | Menu |
|--------|----------|------|------|
| ExecutiveOverviewPage | `governance/pages/ExecutiveOverviewPage.tsx` | `/governance/executive` | ✅ |
| ExecutiveDrillDownPage | `governance/pages/ExecutiveDrillDownPage.tsx` | `/governance/executive/:area` | Navegação interna |
| ExecutiveFinOpsPage | `governance/pages/ExecutiveFinOpsPage.tsx` | `/governance/executive/finops` | Navegação interna |
| ReportsPage | `governance/pages/ReportsPage.tsx` | `/governance/reports` | ✅ |
| CompliancePage | `governance/pages/CompliancePage.tsx` | `/governance/compliance` | ✅ |
| RiskCenterPage | `governance/pages/RiskCenterPage.tsx` | `/governance/risk` | ✅ |
| RiskHeatmapPage | `governance/pages/RiskHeatmapPage.tsx` | `/governance/risk/heatmap` | Navegação interna |
| FinOpsPage | `governance/pages/FinOpsPage.tsx` | `/governance/finops` | ✅ |
| DomainFinOpsPage | `governance/pages/DomainFinOpsPage.tsx` | `/governance/finops/domain/:id` | Navegação interna |
| ServiceFinOpsPage | `governance/pages/ServiceFinOpsPage.tsx` | `/governance/finops/service/:id` | Navegação interna |
| TeamFinOpsPage | `governance/pages/TeamFinOpsPage.tsx` | `/governance/finops/team/:id` | Navegação interna |
| PolicyCatalogPage | `governance/pages/PolicyCatalogPage.tsx` | `/governance/policies` | ✅ |
| GovernancePacksOverviewPage | `governance/pages/GovernancePacksOverviewPage.tsx` | `/governance/packs` | ✅ |
| GovernancePackDetailPage | `governance/pages/GovernancePackDetailPage.tsx` | `/governance/packs/:packId` | Navegação interna |
| TeamsOverviewPage | `governance/pages/TeamsOverviewPage.tsx` | `/governance/teams` | ✅ |
| TeamDetailPage | `governance/pages/TeamDetailPage.tsx` | `/governance/teams/:teamId` | Navegação interna |
| DomainsOverviewPage | `governance/pages/DomainsOverviewPage.tsx` | `/governance/domains` | ✅ |
| DomainDetailPage | `governance/pages/DomainDetailPage.tsx` | `/governance/domains/:domainId` | Navegação interna |
| BenchmarkingPage | `governance/pages/BenchmarkingPage.tsx` | `/governance/benchmarking` | Via relatórios |
| MaturityScorecardsPage | `governance/pages/MaturityScorecardsPage.tsx` | `/governance/maturity` | Via relatórios |
| EnterpriseControlsPage | `governance/pages/EnterpriseControlsPage.tsx` | `/governance/controls` | Via compliance |
| EvidencePackagesPage | `governance/pages/EvidencePackagesPage.tsx` | `/governance/evidence` | Via compliance |
| WaiversPage | `governance/pages/WaiversPage.tsx` | `/governance/waivers` | Via policies |
| DelegatedAdminPage | `governance/pages/DelegatedAdminPage.tsx` | `/governance/delegated-admin` | Via admin |
| GovernanceConfigurationPage | `governance/pages/GovernanceConfigurationPage.tsx` | `/governance/configuration` | Via admin |

**API (4 ficheiros):**
- `governance/api/organizationGovernance.ts`
- `governance/api/evidence.ts`
- `governance/api/executive.ts`
- `governance/api/finOps.ts`

**Observação:** Todas as páginas usam a mesma permissão `governance:read` — falta granularidade.

---

### 2.3 catalog (23 ficheiros)

**Propósito:** Catálogo de serviços, Source of Truth, Developer Portal, busca global.

**Classificação:** PARTIAL  
**Prioridade de correção:** HIGH

**Páginas (12):**

| Página | Ficheiro | Rota | Status |
|--------|----------|------|--------|
| ServiceCatalogListPage | `catalog/pages/ServiceCatalogListPage.tsx` | `/services` | ✅ |
| ServiceCatalogPage | `catalog/pages/ServiceCatalogPage.tsx` | `/services/graph` | ✅ |
| ServiceDetailPage | `catalog/pages/ServiceDetailPage.tsx` | `/services/:serviceId` | ✅ |
| SourceOfTruthExplorerPage | `catalog/pages/SourceOfTruthExplorerPage.tsx` | `/source-of-truth` | ✅ |
| ServiceSourceOfTruthPage | `catalog/pages/ServiceSourceOfTruthPage.tsx` | `/source-of-truth/services/:serviceId` | ✅ |
| ContractSourceOfTruthPage | `catalog/pages/ContractSourceOfTruthPage.tsx` | `/source-of-truth/contracts/:contractVersionId` | ✅ |
| DeveloperPortalPage | `catalog/pages/DeveloperPortalPage.tsx` | `/portal/*` | ✅ |
| GlobalSearchPage | `catalog/pages/GlobalSearchPage.tsx` | `/search` | ✅ |
| CatalogContractsConfigurationPage | `catalog/pages/CatalogContractsConfigurationPage.tsx` | Via admin | ✅ |
| ContractDetailPage | `catalog/pages/ContractDetailPage.tsx` | — | ❌ ÓRFÃ |
| ContractListPage | `catalog/pages/ContractListPage.tsx` | — | ❌ ÓRFÃ |
| ContractsPage | `catalog/pages/ContractsPage.tsx` | — | ❌ ÓRFÃ |

**API (7 ficheiros):**
- `catalog/api/serviceCatalog.ts`
- `catalog/api/contracts.ts`
- `catalog/api/contractStudio.ts`
- `catalog/api/developerPortal.ts`
- `catalog/api/globalSearch.ts`
- `catalog/api/sourceOfTruth.ts`
- (+ 1 ficheiro adicional)

**Problemas:** 3 páginas de contratos (`ContractDetailPage`, `ContractListPage`, `ContractsPage`) parecem ser resíduos de uma versão anterior, substituídas pelo módulo `contracts`.

---

### 2.4 identity-access (23 ficheiros)

**Propósito:** Autenticação, gestão de utilizadores, sessões, delegações, break-glass, JIT, MFA.

**Classificação:** COMPLETE_APPARENT  
**Prioridade:** LOW

**Páginas (15):**

| Página | Tipo | Rota |
|--------|------|------|
| LoginPage | Pública | `/login` |
| ForgotPasswordPage | Pública | `/forgot-password` |
| ResetPasswordPage | Pública | `/reset-password` |
| ActivationPage | Pública | `/activate` |
| MfaPage | Pública | `/mfa` |
| InvitationPage | Pública | `/invitation` |
| TenantSelectionPage | Pública | `/select-tenant` |
| UsersPage | Protegida | `/users` |
| BreakGlassPage | Protegida | `/break-glass` |
| JitAccessPage | Protegida | `/jit-access` |
| DelegationPage | Protegida | `/delegations` |
| AccessReviewPage | Protegida | `/access-reviews` |
| MySessionsPage | Protegida | `/my-sessions` |
| EnvironmentsPage | Protegida | `/environments` |
| UnauthorizedPage | Protegida | `/unauthorized` |

**API (2 ficheiros):** `identity-access/api/identity.ts`

---

### 2.5 ai-hub (15 ficheiros)

**Propósito:** Assistente IA governado, agentes, modelo registry, políticas, routing, IDE, budgets, auditoria.

**Classificação:** COMPLETE_APPARENT  
**Prioridade:** LOW

**Páginas (11):**

| Página | Rota | Permissão |
|--------|------|-----------|
| AiAssistantPage | `/ai/assistant` | `ai:assistant:read` |
| AiAgentsPage | `/ai/agents` | `ai:assistant:read` |
| AgentDetailPage | `/ai/agents/:agentId` | `ai:assistant:read` |
| ModelRegistryPage | `/ai/models` | `ai:governance:read` |
| AiPoliciesPage | `/ai/policies` | `ai:governance:read` |
| AiRoutingPage | `/ai/routing` | `ai:governance:read` |
| IdeIntegrationsPage | `/ai/ide` | `ai:governance:read` |
| TokenBudgetPage | `/ai/budgets` | `ai:governance:read` |
| AiAuditPage | `/ai/audit` | `ai:governance:read` |
| AiAnalysisPage | `/ai/analysis` | `ai:runtime:write` |
| AiIntegrationsConfigurationPage | `/ai/integrations/config` | Via admin |

**API (2 ficheiros):** `ai-hub/api/aiGovernance.ts` + 1

---

### 2.6 operations (16 ficheiros)

**Propósito:** Gestão de incidentes, runbooks, reliability por equipa/serviço, automação, comparação de ambientes.

**Classificação:** COMPLETE_APPARENT  
**Prioridade:** LOW

**Páginas (10):**

| Página | Rota | Permissão |
|--------|------|-----------|
| IncidentsPage | `/operations/incidents` | `operations:incidents:read` |
| IncidentDetailPage | `/operations/incidents/:incidentId` | `operations:incidents:read` |
| RunbooksPage | `/operations/runbooks` | `operations:runbooks:read` |
| TeamReliabilityPage | `/operations/reliability` | `operations:reliability:read` |
| ServiceReliabilityDetailPage | `/operations/reliability/:serviceId` | `operations:reliability:read` |
| AutomationWorkflowsPage | `/operations/automation` | `operations:automation:read` |
| AutomationWorkflowDetailPage | `/operations/automation/:workflowId` | `operations:automation:read` |
| EnvironmentComparisonPage | `/operations/runtime-comparison` | `operations:runtime:read` |
| PlatformOperationsPage | `/platform/operations` | `platform:admin:read` |
| AutomationAdminPage | Via admin | `platform:admin:read` |

**API (5 ficheiros):** incidents, reliability, automation, runtimeIntelligence, platformOps

---

### 2.7 change-governance (13 ficheiros)

**Propósito:** Change intelligence, confiança em mudanças, releases, workflow de promoção.

**Classificação:** COMPLETE_APPARENT  
**Prioridade:** LOW

**Páginas (6):**

| Página | Rota | Permissão |
|--------|------|-----------|
| ChangeCatalogPage | `/changes` | `change-intelligence:read` |
| ChangeDetailPage | `/changes/:changeId` | `change-intelligence:read` |
| ReleasesPage | `/releases` | `change-intelligence:releases:read` |
| WorkflowPage | `/workflow` | `workflow:read` |
| PromotionPage | `/promotion` | `promotion:read` |
| WorkflowConfigurationPage | Via admin | Admin |

**API (5 ficheiros):** changeIntelligence, workflow, promotion, changeConfidence + 1

---

### 2.8 notifications (11 ficheiros)

**Propósito:** Centro de notificações, configuração e preferências de utilizador.

**Classificação:** COMPLETE_APPARENT  
**Prioridade:** LOW

**Páginas (3):**

| Página | Rota | Menu |
|--------|------|------|
| NotificationCenterPage | `/notifications` | Via topbar |
| NotificationConfigurationPage | `/notifications/configuration` | Via admin |
| NotificationPreferencesPage | `/notifications/preferences` | Via perfil |

**Nota:** Sem entrada direta no sidebar — acesso via topbar (ícone de sino). Padrão intencional.

**API (1 ficheiro):** `notifications/api/notifications.ts`

---

### 2.9 product-analytics (8 ficheiros)

**Propósito:** Métricas de adoção do produto, análise por persona, jornadas, valor.

**Classificação:** PARTIAL  
**Prioridade de correção:** MEDIUM

**Páginas (6):**

| Página | Ficheiro | Status |
|--------|----------|--------|
| ProductAnalyticsOverviewPage | `product-analytics/pages/ProductAnalyticsOverviewPage.tsx` | ✅ |
| ModuleAdoptionPage | `product-analytics/pages/ModuleAdoptionPage.tsx` | ✅ |
| PersonaUsagePage | `product-analytics/pages/PersonaUsagePage.tsx` | ✅ |
| JourneyFunnelPage | `product-analytics/pages/JourneyFunnelPage.tsx` | ✅ |
| ValueTrackingPage | `product-analytics/pages/ValueTrackingPage.tsx` | ✅ |
| ProductAnalyticsOverviewPage (raiz) | `product-analytics/ProductAnalyticsOverviewPage.tsx` | ❌ VAZIO (0 bytes) |

**Problema:** Ficheiro duplicado na raiz do módulo com 0 bytes — provável resíduo de refatoração.

**API (1 ficheiro):** `product-analytics/api/productAnalyticsApi.ts`

---

### 2.10 integrations (6 ficheiros)

**Propósito:** Hub de integrações, conectores, execuções de ingestão, freshness.

**Classificação:** COMPLETE_APPARENT  
**Prioridade:** LOW

**Páginas (4):**

| Página | Rota |
|--------|------|
| IntegrationHubPage | `/integrations` |
| ConnectorDetailPage | `/integrations/:connectorId` |
| IngestionExecutionsPage | `/integrations/executions` |
| IngestionFreshnessPage | `/integrations/freshness` |

**API (1 ficheiro):** `integrations/api/integrations.ts`

---

### 2.11 configuration (6 ficheiros)

**Propósito:** Configuração administrativa avançada da plataforma.

**Classificação:** COMPLETE_APPARENT  
**Prioridade:** LOW

**Páginas (2):**

| Página | Rota |
|--------|------|
| ConfigurationAdminPage | `/platform/configuration` |
| AdvancedConfigurationConsolePage | `/platform/configuration/advanced` |

**API (1 ficheiro):** `configuration/api/configurationApi.ts`

---

### 2.12 audit-compliance (4 ficheiros)

**Propósito:** Logs de auditoria centralizados.

**Classificação:** COMPLETE_APPARENT  
**Prioridade:** LOW

**Páginas (1):** AuditPage → `/audit`

**API (2 ficheiros):** `audit-compliance/api/audit.ts` + tipos

---

### 2.13 operational-intelligence (1 ficheiro)

**Propósito:** Configuração de FinOps operacional.

**Classificação:** PARTIAL  
**Prioridade de correção:** MEDIUM

**Páginas (1):** OperationsFinOpsConfigurationPage → Rota admin

**Problemas:**
- Apenas 1 ficheiro no módulo inteiro
- Sem integração API
- Módulo embrionário — necessita expansão ou integração noutro módulo

---

### 2.14 shared (2 ficheiros)

**Propósito:** Componentes partilhados entre módulos.

**Classificação:** N/A (infraestrutura)

**Páginas (1):** DashboardPage → `/` (Home)

---

## 3. Matriz de Classificação

| Classificação | Significado | Módulos |
|--------------|------------|---------|
| COMPLETE_APPARENT | Todas as páginas têm rotas, menu e API | governance, identity-access, ai-hub, operations, change-governance, notifications, integrations, configuration, audit-compliance |
| PARTIAL | Problemas identificados (rotas, páginas ou API em falta) | contracts, catalog, product-analytics, operational-intelligence |
| N/A | Módulo de infraestrutura | shared |

---

## 4. Recomendações por Módulo

| Módulo | Ação | Prioridade |
|--------|------|------------|
| contracts | Registar 3 rotas em falta; decidir destino de ContractPortalPage | CRITICAL |
| catalog | Remover ou redirecionar 3 páginas órfãs (resíduos legacy) | HIGH |
| product-analytics | Eliminar ficheiro vazio na raiz; consolidar overview page | MEDIUM |
| operational-intelligence | Expandir módulo ou fundir com operations/governance | MEDIUM |
| governance | Granularizar permissões (governance:finops:read, governance:risk:read) | MEDIUM |

---

*Documento gerado como parte da auditoria modular do NexTraceOne.*
