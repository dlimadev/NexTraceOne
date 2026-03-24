# Relatório de Permissões e Guards do Frontend — NexTraceOne

> **Data:** 2025-07-14  
> **Versão:** 2.0  
> **Escopo:** Mapeamento completo de permissões, guards de rota e controlo de acesso  
> **Status global:** GAP_IDENTIFIED  
> **Ficheiro de permissões:** `src/frontend/src/auth/permissions.ts`  
> **Ficheiro de guard:** `src/frontend/src/components/ProtectedRoute.tsx`

---

## 1. Resumo

| Métrica | Valor |
|---------|-------|
| Permissões distintas | 60+ |
| Domínios de permissão | 12 |
| Rotas protegidas | 123+ |
| Rotas públicas | 7 |
| Guard component | `ProtectedRoute` |
| Hook de verificação | `usePermissions()` |
| Fonte de verdade | Backend (JWT claims + /me endpoint) |

---

## 2. Arquitetura de Permissões

### 2.1 Modelo

```
<domínio>:<recurso>:<ação>
```

Exemplos: `catalog:assets:read`, `contracts:write`, `operations:incidents:read`

### 2.2 Fluxo de verificação

1. **Login** → Backend retorna JWT com claims de permissões
2. **AuthContext** → Armazena perfil do utilizador com permissões efetivas
3. **usePermissions()** → Hook que expõe `can(permission)` derivado do AuthContext
4. **ProtectedRoute** → Componente wrapper que verifica permissão antes de renderizar
5. **AppSidebar** → Cada item verifica permissão antes de ser exibido

### 2.3 Princípio de design

> O frontend **NÃO** faz mapeamento role→permission. O frontend recebe as permissões efetivas do backend e apenas controla a visibilidade da UI. A autorização real é sempre aplicada pelo backend.

---

## 3. Mapa Completo: Página → Permissão

### 3.1 Home & Global

| Página | Rota | Permissão | Guard |
|--------|------|-----------|-------|
| DashboardPage | `/` | *(nenhuma)* | Não |
| GlobalSearchPage | `/search` | `catalog:assets:read` | ProtectedRoute |
| UnauthorizedPage | `/unauthorized` | *(nenhuma)* | Não |

### 3.2 Catalog & Source of Truth

| Página | Rota | Permissão | Guard |
|--------|------|-----------|-------|
| ServiceCatalogListPage | `/services` | `catalog:assets:read` | ProtectedRoute |
| ServiceCatalogPage | `/services/graph` | `catalog:assets:read` | ProtectedRoute |
| ServiceDetailPage | `/services/:serviceId` | `catalog:assets:read` | ProtectedRoute |
| SourceOfTruthExplorerPage | `/source-of-truth` | `catalog:assets:read` | ProtectedRoute |
| ServiceSourceOfTruthPage | `/source-of-truth/services/:serviceId` | `catalog:assets:read` | ProtectedRoute |
| ContractSourceOfTruthPage | `/source-of-truth/contracts/:contractVersionId` | `catalog:assets:read` | ProtectedRoute |
| DeveloperPortalPage | `/portal/*` | `developer-portal:read` | ProtectedRoute |

### 3.3 Contracts

| Página | Rota | Permissão | Guard |
|--------|------|-----------|-------|
| ContractCatalogPage | `/contracts` | `contracts:read` | ProtectedRoute |
| CreateServicePage | `/contracts/new` | `contracts:write` | ProtectedRoute |
| DraftStudioPage | `/contracts/studio/:draftId` | `contracts:write` | ProtectedRoute |
| ContractWorkspacePage | `/contracts/:contractVersionId` | `contracts:read` | ProtectedRoute |
| ContractGovernancePage | `/contracts/governance` | `contracts:read` | ❌ SEM ROTA |
| SpectralRulesetManagerPage | `/contracts/spectral` | `contracts:write` | ❌ SEM ROTA |
| CanonicalEntityCatalogPage | `/contracts/canonical` | `contracts:read` | ❌ SEM ROTA |

### 3.4 Change Governance

| Página | Rota | Permissão | Guard |
|--------|------|-----------|-------|
| ChangeCatalogPage | `/changes` | `change-intelligence:read` | ProtectedRoute |
| ChangeDetailPage | `/changes/:changeId` | `change-intelligence:read` | ProtectedRoute |
| ReleasesPage | `/releases` | `change-intelligence:releases:read` | ProtectedRoute |
| WorkflowPage | `/workflow` | `workflow:read` | ProtectedRoute |
| PromotionPage | `/promotion` | `promotion:read` | ProtectedRoute |

### 3.5 Operations

| Página | Rota | Permissão | Guard |
|--------|------|-----------|-------|
| IncidentsPage | `/operations/incidents` | `operations:incidents:read` | ProtectedRoute |
| IncidentDetailPage | `/operations/incidents/:incidentId` | `operations:incidents:read` | ProtectedRoute |
| RunbooksPage | `/operations/runbooks` | `operations:runbooks:read` | ProtectedRoute |
| TeamReliabilityPage | `/operations/reliability` | `operations:reliability:read` | ProtectedRoute |
| ServiceReliabilityDetailPage | `/operations/reliability/:serviceId` | `operations:reliability:read` | ProtectedRoute |
| AutomationWorkflowsPage | `/operations/automation` | `operations:automation:read` | ProtectedRoute |
| AutomationWorkflowDetailPage | `/operations/automation/:workflowId` | `operations:automation:read` | ProtectedRoute |
| EnvironmentComparisonPage | `/operations/runtime-comparison` | `operations:runtime:read` | ProtectedRoute |
| PlatformOperationsPage | `/platform/operations` | `platform:admin:read` | ProtectedRoute |

### 3.6 AI Hub

| Página | Rota | Permissão | Guard |
|--------|------|-----------|-------|
| AiAssistantPage | `/ai/assistant` | `ai:assistant:read` | ProtectedRoute |
| AiAgentsPage | `/ai/agents` | `ai:assistant:read` | ProtectedRoute |
| AgentDetailPage | `/ai/agents/:agentId` | `ai:assistant:read` | ProtectedRoute |
| ModelRegistryPage | `/ai/models` | `ai:governance:read` | ProtectedRoute |
| AiPoliciesPage | `/ai/policies` | `ai:governance:read` | ProtectedRoute |
| AiRoutingPage | `/ai/routing` | `ai:governance:read` | ProtectedRoute |
| IdeIntegrationsPage | `/ai/ide` | `ai:governance:read` | ProtectedRoute |
| TokenBudgetPage | `/ai/budgets` | `ai:governance:read` | ProtectedRoute |
| AiAuditPage | `/ai/audit` | `ai:governance:read` | ProtectedRoute |
| AiAnalysisPage | `/ai/analysis` | `ai:runtime:write` | ProtectedRoute |

### 3.7 Governance

| Página | Rota | Permissão | Guard |
|--------|------|-----------|-------|
| ExecutiveOverviewPage | `/governance/executive` | `governance:read` | ProtectedRoute |
| ExecutiveDrillDownPage | `/governance/executive/:area` | `governance:read` | ProtectedRoute |
| ExecutiveFinOpsPage | `/governance/executive/finops` | `governance:read` | ProtectedRoute |
| ReportsPage | `/governance/reports` | `governance:read` | ProtectedRoute |
| CompliancePage | `/governance/compliance` | `governance:read` | ProtectedRoute |
| RiskCenterPage | `/governance/risk` | `governance:read` | ProtectedRoute |
| RiskHeatmapPage | `/governance/risk/heatmap` | `governance:read` | ProtectedRoute |
| FinOpsPage | `/governance/finops` | `governance:read` | ProtectedRoute |
| DomainFinOpsPage | `/governance/finops/domain/:id` | `governance:read` | ProtectedRoute |
| ServiceFinOpsPage | `/governance/finops/service/:id` | `governance:read` | ProtectedRoute |
| TeamFinOpsPage | `/governance/finops/team/:id` | `governance:read` | ProtectedRoute |
| PolicyCatalogPage | `/governance/policies` | `governance:read` | ProtectedRoute |
| GovernancePacksOverviewPage | `/governance/packs` | `governance:read` | ProtectedRoute |
| GovernancePackDetailPage | `/governance/packs/:packId` | `governance:read` | ProtectedRoute |
| TeamsOverviewPage | `/governance/teams` | `governance:read` | ProtectedRoute |
| TeamDetailPage | `/governance/teams/:teamId` | `governance:read` | ProtectedRoute |
| DomainsOverviewPage | `/governance/domains` | `governance:read` | ProtectedRoute |
| DomainDetailPage | `/governance/domains/:domainId` | `governance:read` | ProtectedRoute |
| BenchmarkingPage | `/governance/benchmarking` | `governance:read` | ProtectedRoute |
| MaturityScorecardsPage | `/governance/maturity` | `governance:read` | ProtectedRoute |
| EnterpriseControlsPage | `/governance/controls` | `governance:read` | ProtectedRoute |
| EvidencePackagesPage | `/governance/evidence` | `governance:read` | ProtectedRoute |
| WaiversPage | `/governance/waivers` | `governance:read` | ProtectedRoute |
| DelegatedAdminPage | `/governance/delegated-admin` | `governance:read` | ProtectedRoute |
| GovernanceConfigurationPage | `/governance/configuration` | `governance:read` | ProtectedRoute |

### 3.8 Identity & Admin

| Página | Rota | Permissão | Guard |
|--------|------|-----------|-------|
| UsersPage | `/users` | `identity:users:read` | ProtectedRoute |
| BreakGlassPage | `/break-glass` | `identity:sessions:read` | ProtectedRoute |
| JitAccessPage | `/jit-access` | `identity:users:read` | ProtectedRoute |
| DelegationPage | `/delegations` | `identity:users:read` | ProtectedRoute |
| AccessReviewPage | `/access-reviews` | `identity:users:read` | ProtectedRoute |
| MySessionsPage | `/my-sessions` | `identity:sessions:read` | ProtectedRoute |
| EnvironmentsPage | `/environments` | Admin-level | ProtectedRoute |
| AuditPage | `/audit` | `audit:read` | ProtectedRoute |
| ConfigurationAdminPage | `/platform/configuration` | `platform:admin:read` | ProtectedRoute |

### 3.9 Outros

| Página | Rota | Permissão | Guard |
|--------|------|-----------|-------|
| IntegrationHubPage | `/integrations` | `integrations:read` | ProtectedRoute |
| ConnectorDetailPage | `/integrations/:connectorId` | `integrations:read` | ProtectedRoute |
| IngestionExecutionsPage | `/integrations/executions` | `integrations:read` | ProtectedRoute |
| IngestionFreshnessPage | `/integrations/freshness` | `integrations:read` | ProtectedRoute |
| ProductAnalyticsOverviewPage | `/analytics` | `analytics:read` | ProtectedRoute |
| ModuleAdoptionPage | `/analytics/adoption` | `analytics:read` | ProtectedRoute |
| PersonaUsagePage | `/analytics/personas` | `analytics:read` | ProtectedRoute |
| JourneyFunnelPage | `/analytics/journeys` | `analytics:read` | ProtectedRoute |
| ValueTrackingPage | `/analytics/value` | `analytics:read` | ProtectedRoute |
| NotificationCenterPage | `/notifications` | *(nenhuma)* | Não |
| NotificationConfigurationPage | `/notifications/configuration` | Admin | ProtectedRoute |
| NotificationPreferencesPage | `/notifications/preferences` | *(nenhuma)* | Não |

---

## 4. Mapa: Item de Menu → Permissão

| Item de menu (i18n) | Permissão necessária | Visível para |
|---------------------|---------------------|-------------|
| `sidebar.dashboard` | *(nenhuma)* | Todos |
| `sidebar.serviceCatalog` | `catalog:assets:read` | Engineer, TechLead, Architect |
| `sidebar.dependencyGraph` | `catalog:assets:read` | Engineer, TechLead, Architect |
| `sidebar.sourceOfTruth` | `catalog:assets:read` | Engineer, TechLead, Architect |
| `sidebar.developerPortal` | `developer-portal:read` | Engineer, TechLead |
| `sidebar.contractCatalog` | `contracts:read` | Engineer, TechLead, Architect |
| `sidebar.createContract` | `contracts:write` | Engineer, TechLead |
| `sidebar.contractStudio` | `contracts:read` | Engineer, TechLead, Architect |
| `sidebar.contractGovernance` | `contracts:read` | Engineer, TechLead, Architect |
| `sidebar.spectralRulesets` | `contracts:write` | Engineer, TechLead |
| `sidebar.canonicalEntities` | `contracts:read` | Engineer, TechLead, Architect |
| `sidebar.changeConfidence` | `change-intelligence:read` | Engineer, TechLead |
| `sidebar.changeIntelligence` | `change-intelligence:releases:read` | Engineer, TechLead |
| `sidebar.workflow` | `workflow:read` | Engineer, TechLead |
| `sidebar.promotion` | `promotion:read` | TechLead, Architect |
| `sidebar.incidents` | `operations:incidents:read` | Engineer, TechLead, PlatformAdmin |
| `sidebar.runbooks` | `operations:runbooks:read` | Engineer, TechLead, PlatformAdmin |
| `sidebar.reliability` | `operations:reliability:read` | TechLead, PlatformAdmin |
| `sidebar.automation` | `operations:automation:read` | PlatformAdmin |
| `sidebar.environmentComparison` | `operations:runtime:read` | PlatformAdmin |
| `sidebar.aiAssistant` | `ai:assistant:read` | Todos com permissão |
| `sidebar.aiAgents` | `ai:assistant:read` | Todos com permissão |
| `sidebar.modelRegistry` | `ai:governance:read` | PlatformAdmin, Architect |
| `sidebar.aiPolicies` | `ai:governance:read` | PlatformAdmin, Architect |
| `sidebar.aiRouting` | `ai:governance:read` | PlatformAdmin |
| `sidebar.aiIde` | `ai:governance:read` | PlatformAdmin |
| `sidebar.aiBudgets` | `ai:governance:read` | PlatformAdmin, Executive |
| `sidebar.aiAudit` | `ai:governance:read` | Auditor, PlatformAdmin |
| `sidebar.aiAnalysis` | `ai:runtime:write` | Engineer, TechLead |
| `sidebar.executiveOverview` | `governance:read` | Executive, Architect |
| `sidebar.reports` | `governance:read` | Executive, Architect, Product |
| `sidebar.compliance` | `governance:read` | Auditor, Architect |
| `sidebar.riskCenter` | `governance:read` | Architect, Executive |
| `sidebar.finops` | `governance:read` | Executive, PlatformAdmin |
| `sidebar.policies` | `governance:read` | Architect, PlatformAdmin |
| `sidebar.packs` | `governance:read` | Architect, PlatformAdmin |
| `sidebar.teams` | `governance:read` | TechLead, Executive |
| `sidebar.domains` | `governance:read` | Architect, Executive |
| `sidebar.integrationHub` | `integrations:read` | PlatformAdmin |
| `sidebar.productAnalytics` | `analytics:read` | Product, Executive |
| `sidebar.users` | `identity:users:read` | PlatformAdmin |
| `sidebar.breakGlass` | `identity:sessions:read` | PlatformAdmin |
| `sidebar.jitAccess` | `identity:users:read` | PlatformAdmin |
| `sidebar.delegations` | `identity:users:read` | PlatformAdmin |
| `sidebar.accessReview` | `identity:users:read` | PlatformAdmin, Auditor |
| `sidebar.mySessions` | `identity:sessions:read` | PlatformAdmin |
| `sidebar.audit` | `audit:read` | Auditor |
| `sidebar.platformOperations` | `platform:admin:read` | PlatformAdmin |
| `sidebar.platformConfiguration` | `platform:admin:read` | PlatformAdmin |

---

## 5. Ações Críticas e Permissões de Escrita

| Ação | Rota / Contexto | Permissão | Módulo |
|------|-----------------|-----------|--------|
| Criar contrato | `/contracts/new` | `contracts:write` | contracts |
| Editar draft no studio | `/contracts/studio/:draftId` | `contracts:write` | contracts |
| Importar contrato | Ação inline | `contracts:import` | contracts |
| Gerir regras Spectral | `/contracts/spectral` | `contracts:write` | contracts |
| Análise IA em runtime | `/ai/analysis` | `ai:runtime:write` | ai-hub |
| Gestão de políticas IA | `/ai/policies` | `ai:governance:write` | ai-hub |
| Gestão de utilizadores | `/users` | `identity:users:write` | identity-access |
| Atribuição de roles | Ação inline | `identity:roles:assign` | identity-access |
| Revogação de sessões | `/break-glass` | `identity:sessions:revoke` | identity-access |
| Configuração da plataforma | `/platform/configuration` | `platform:admin:read` | configuration |

---

## 6. Inconsistências e Lacunas

### 6.1 Gaps identificados

| # | Gap | Detalhe | Prioridade | Status |
|---|-----|---------|------------|--------|
| 1 | Governance monolítica | Todas as 25 páginas de governance usam `governance:read` — sem distinção entre FinOps, Risk, Compliance, Policies | MEDIUM | GAP_IDENTIFIED |
| 2 | Sem governance:write | Não existe permissão de escrita para governance — edição de políticas e packs sem guard | MEDIUM | GAP_IDENTIFIED |
| 3 | Notificações sem guard | NotificationCenterPage e NotificationPreferencesPage não requerem permissão | LOW | GAP_IDENTIFIED |
| 4 | 3 rotas sem guard | ContractGovernance, Spectral, Canonical não têm rota → não têm guard | CRITICAL | GAP_IDENTIFIED |
| 5 | Dashboard sem permissão | DashboardPage acessível sem permissão (intencional mas notável) | LOW | IN_ANALYSIS |

### 6.2 Recomendações de evolução

| # | Recomendação | Prioridade | Esforço |
|---|-------------|------------|---------|
| 1 | Registar as 3 rotas em falta com permissão `contracts:read`/`contracts:write` | CRITICAL | Baixo |
| 2 | Granularizar `governance:read` em sub-permissões: `governance:finops:read`, `governance:risk:read`, `governance:compliance:read`, `governance:policies:read` | MEDIUM | Médio |
| 3 | Adicionar `governance:write` para ações de edição em policies e packs | MEDIUM | Médio |
| 4 | Adicionar permissão a NotificationConfigurationPage (admin) | LOW | Baixo |
| 5 | Documentar intenção de DashboardPage sem permissão | LOW | Baixo |

---

## 7. Segurança

### 7.1 Tokens e armazenamento

| Token | Armazenamento | Risco |
|-------|--------------|-------|
| Access token | `sessionStorage` | Médio — limpo ao fechar tab |
| Refresh token | Memória apenas | Baixo — não persistido |
| CSRF token | Enviado em mutações | Baixo — proteção adequada |

### 7.2 Proteção contra redirect aberto

O utilitário `isSafeRedirectPath()` em `utils/navigation.ts` valida que redirects são apenas para caminhos internos. Proteção adequada contra ataques de open redirect.

### 7.3 Observações de segurança

- Frontend não armazena roles nem faz mapeamento local → adequado
- Todas as mutações passam CSRF token → adequado
- Refresh token não persistido → adequado
- Permissões derivadas do backend → adequado

---

*Documento gerado como parte da auditoria modular do NexTraceOne.*
