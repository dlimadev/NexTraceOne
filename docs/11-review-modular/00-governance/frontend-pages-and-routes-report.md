# Inventário de Páginas e Rotas — NexTraceOne Frontend

> **Data:** 2026-03-24  
> **Tipo:** Auditoria Estrutural — Parte 3  
> **Fonte de verdade:** App.tsx, AppSidebar.tsx e ficheiros de páginas do repositório

---

## Resumo

| Métrica | Valor |
|---------|-------|
| Total de páginas (ficheiros *Page.tsx) | 105 |
| Rotas definidas no App.tsx | 85+ |
| Rotas públicas | 7 |
| Rotas protegidas | 78+ |
| Itens no menu (navItems) | 49 |
| Páginas órfãs (sem rota no App.tsx) | 7 |
| Itens de menu sem rota real | 3 |
| Rotas sem item no menu (intencional) | ~30 |

---

## 1. Rotas Públicas (Autenticação)

| Rota | Página | Módulo | Menu | Funcional | i18n |
|------|--------|--------|------|-----------|------|
| `/login` | LoginPage | identity-access | Não | ✅ | ✅ |
| `/forgot-password` | ForgotPasswordPage | identity-access | Não | ✅ | ✅ |
| `/reset-password` | ResetPasswordPage | identity-access | Não | ✅ | ✅ |
| `/activate` | ActivationPage | identity-access | Não | ✅ | ✅ |
| `/mfa` | MfaPage | identity-access | Não | ✅ | ✅ |
| `/invitation` | InvitationPage | identity-access | Não | ✅ | ✅ |
| `/select-tenant` | TenantSelectionPage | identity-access | Não | ✅ | ✅ |

---

## 2. Rotas Protegidas — Services & Knowledge

| Rota | Página | Módulo | Menu | Permissão | Estado | Observações |
|------|--------|--------|------|-----------|--------|-------------|
| `/` | DashboardPage | — | ✅ home | — | ✅ Funcional | Dashboard principal |
| `/services` | ServiceCatalogListPage | catalog | ✅ services | catalog:assets:read | ✅ Funcional | Lista de serviços |
| `/services/graph` | ServiceCatalogPage | catalog | ✅ services | catalog:assets:read | ✅ Funcional | Grafo de dependências (1010 linhas) |
| `/services/:serviceId` | ServiceDetailPage | catalog | Não (detalhe) | catalog:assets:read | ✅ Funcional | Detalhe de serviço |
| `/source-of-truth` | SourceOfTruthExplorerPage | catalog | ✅ knowledge | catalog:assets:read | ✅ Funcional | Explorador Source of Truth |
| `/source-of-truth/service/:serviceId` | ServiceSourceOfTruthPage | catalog | Não (detalhe) | catalog:assets:read | ✅ Funcional | Source of Truth por serviço |
| `/source-of-truth/contract/:contractId` | ContractSourceOfTruthPage | catalog | Não (detalhe) | catalog:assets:read | ✅ Funcional | Source of Truth por contrato |
| `/portal` | DeveloperPortalPage | catalog | ✅ knowledge | developer-portal:read | ✅ Funcional | Portal do developer |
| `/search` | GlobalSearchPage | catalog | Não (header) | catalog:assets:read | ✅ Funcional | Pesquisa global |

---

## 3. Rotas Protegidas — Contracts

| Rota | Página | Módulo | Menu | Permissão | Estado | Observações |
|------|--------|--------|------|-----------|--------|-------------|
| `/contracts` | ContractCatalogPage | contracts | ✅ contracts | contracts:read | ✅ Funcional | Catálogo de contratos |
| `/contracts/new` | CreateServicePage | contracts | ✅ contracts | contracts:write | ✅ Funcional | Criação de contrato |
| `/contracts/studio` | DraftStudioPage | contracts | ✅ contracts | contracts:read | ✅ Funcional | Studio de drafts |
| `/contracts/studio/:draftId` | DraftStudioPage | contracts | Não (detalhe) | contracts:write | ✅ Funcional | Edição de draft |
| `/contracts/:contractVersionId` | ContractWorkspacePage | contracts | Não (detalhe) | contracts:read | ✅ Funcional | Workspace de contrato |
| `/contracts/governance` | — | — | ✅ contracts | — | ❌ **SEM ROTA** | Menu aponta para rota inexistente |
| `/contracts/spectral` | — | — | ✅ contracts | — | ❌ **SEM ROTA** | Menu aponta para rota inexistente |
| `/contracts/canonical` | — | — | ✅ contracts | — | ❌ **SEM ROTA** | Menu aponta para rota inexistente |

> **⚠️ Alerta:** 3 itens de menu de contratos apontam para rotas que **não existem** no App.tsx. As páginas existem como ficheiros (`ContractGovernancePage.tsx`, `SpectralRulesetManagerPage.tsx`, `CanonicalEntityCatalogPage.tsx`) mas não estão importadas nem roteadas.

---

## 4. Rotas Protegidas — Changes

| Rota | Página | Módulo | Menu | Permissão | Estado | Observações |
|------|--------|--------|------|-----------|--------|-------------|
| `/changes` | ChangeCatalogPage | change-governance | ✅ changes | change-intelligence:read | ✅ Funcional | Catálogo de mudanças |
| `/changes/:changeId` | ChangeDetailPage | change-governance | Não (detalhe) | change-intelligence:read | ✅ Funcional | Detalhe de mudança |
| `/releases` | ReleasesPage | change-governance | ✅ changes | change-intelligence:releases:read | ✅ Funcional | Releases |
| `/workflow` | WorkflowPage | change-governance | ✅ changes | workflow:read | ✅ Funcional | Workflows |
| `/promotion` | PromotionPage | change-governance | ✅ changes | promotion:read | ✅ Funcional | Promoções |

---

## 5. Rotas Protegidas — Operations

| Rota | Página | Módulo | Menu | Permissão | Estado | Observações |
|------|--------|--------|------|-----------|--------|-------------|
| `/operations/incidents` | IncidentsPage | operations | ✅ operations | operations:incidents:read | ✅ Funcional | Incidentes |
| `/operations/incidents/:incidentId` | IncidentDetailPage | operations | Não (detalhe) | operations:incidents:read | ✅ Funcional | Detalhe de incidente |
| `/operations/runbooks` | RunbooksPage | operations | ✅ operations | operations:runbooks:read | ✅ Funcional | Runbooks |
| `/operations/reliability` | TeamReliabilityPage | operations | ✅ operations | operations:reliability:read | ✅ Funcional | Fiabilidade por equipa |
| `/operations/reliability/:serviceId` | ServiceReliabilityDetailPage | operations | Não (detalhe) | operations:reliability:read | ✅ Funcional | Fiabilidade por serviço |
| `/operations/automation` | AutomationWorkflowsPage | operations | ✅ operations | operations:automation:read | ✅ Funcional | Automação |
| `/operations/automation/admin` | AutomationAdminPage | operations | Não (admin) | operations:automation:read | ✅ Funcional | Admin de automação |
| `/operations/automation/:workflowId` | AutomationWorkflowDetailPage | operations | Não (detalhe) | operations:automation:read | ✅ Funcional | Detalhe de workflow |
| `/operations/runtime-comparison` | EnvironmentComparisonPage | operations | ✅ operations | operations:runtime:read | ✅ Funcional | Comparação de ambientes |

---

## 6. Rotas Protegidas — AI Hub

| Rota | Página | Módulo | Menu | Permissão | Estado | Observações |
|------|--------|--------|------|-----------|--------|-------------|
| `/ai/assistant` | AiAssistantPage | ai-hub | ✅ aiHub | ai:assistant:read | ✅ Parcial | Assistente IA (483 linhas) — UI completa, backend com stubs |
| `/ai/agents` | AiAgentsPage | ai-hub | ✅ aiHub | ai:assistant:read | ✅ Parcial | Agentes IA |
| `/ai/agents/:agentId` | AgentDetailPage | ai-hub | Não (detalhe) | ai:assistant:read | ✅ Parcial | Detalhe de agente |
| `/ai/models` | ModelRegistryPage | ai-hub | ✅ aiHub | ai:governance:read | ✅ Parcial | Registo de modelos |
| `/ai/policies` | AiPoliciesPage | ai-hub | ✅ aiHub | ai:governance:read | ✅ Parcial | Políticas IA |
| `/ai/routing` | AiRoutingPage | ai-hub | ✅ aiHub | ai:governance:read | ✅ Parcial | Routing IA |
| `/ai/ide` | IdeIntegrationsPage | ai-hub | ✅ aiHub | ai:governance:read | ✅ Parcial | Integrações IDE |
| `/ai/budgets` | TokenBudgetPage | ai-hub | ✅ aiHub | ai:governance:read | ✅ Parcial | Orçamento de tokens |
| `/ai/audit` | AiAuditPage | ai-hub | ✅ aiHub | ai:governance:read | ✅ Parcial | Auditoria IA |
| `/ai/analysis` | AiAnalysisPage | ai-hub | ✅ aiHub | ai:runtime:write | ✅ Parcial | Análise IA |

---

## 7. Rotas Protegidas — Governance

| Rota | Página | Módulo | Menu | Permissão | Estado |
|------|--------|--------|------|-----------|--------|
| `/governance/executive` | ExecutiveOverviewPage | governance | ✅ governance | governance:read | ✅ Funcional |
| `/governance/executive/drilldown` | ExecutiveDrillDownPage | governance | Não (sub-rota) | governance:read | ✅ Funcional |
| `/governance/executive/finops` | ExecutiveFinOpsPage | governance | Não (sub-rota) | governance:read | ✅ Funcional |
| `/governance/reports` | ReportsPage | governance | ✅ governance | governance:read | ✅ Funcional |
| `/governance/compliance` | CompliancePage | governance | ✅ governance | governance:read | ✅ Funcional |
| `/governance/risk` | RiskCenterPage | governance | ✅ governance | governance:read | ✅ Funcional |
| `/governance/risk/heatmap` | RiskHeatmapPage | governance | Não (sub-rota) | governance:read | ✅ Funcional |
| `/governance/finops` | FinOpsPage | governance | ✅ governance | governance:read | ✅ Funcional |
| `/governance/finops/service/:serviceId` | ServiceFinOpsPage | governance | Não (detalhe) | governance:read | ✅ Funcional |
| `/governance/finops/team/:teamId` | TeamFinOpsPage | governance | Não (detalhe) | governance:read | ✅ Funcional |
| `/governance/finops/domain/:domainId` | DomainFinOpsPage | governance | Não (detalhe) | governance:read | ✅ Funcional |
| `/governance/policies` | PolicyCatalogPage | governance | ✅ governance | governance:read | ✅ Funcional |
| `/governance/controls` | EnterpriseControlsPage | governance | Não (sub-rota) | governance:read | ✅ Funcional |
| `/governance/evidence` | EvidencePackagesPage | governance | Não (sub-rota) | governance:read | ✅ Funcional |
| `/governance/maturity` | MaturityScorecardsPage | governance | Não (sub-rota) | governance:read | ✅ Funcional |
| `/governance/benchmarking` | BenchmarkingPage | governance | Não (sub-rota) | governance:read | ✅ Funcional |
| `/governance/teams` | TeamsOverviewPage | governance | ✅ organization | governance:read | ✅ Funcional |
| `/governance/teams/:teamId` | TeamDetailPage | governance | Não (detalhe) | governance:read | ✅ Funcional |
| `/governance/domains` | DomainsOverviewPage | governance | ✅ organization | governance:read | ✅ Funcional |
| `/governance/domains/:domainId` | DomainDetailPage | governance | Não (detalhe) | governance:read | ✅ Funcional |
| `/governance/packs` | GovernancePacksOverviewPage | governance | ✅ governance | governance:read | ✅ Funcional |
| `/governance/packs/:packId` | GovernancePackDetailPage | governance | Não (detalhe) | governance:read | ✅ Funcional |
| `/governance/waivers` | WaiversPage | governance | Não (sub-rota) | governance:read | ✅ Funcional |
| `/governance/delegated-admin` | DelegatedAdminPage | governance | Não (sub-rota) | governance:read | ✅ Funcional |

---

## 8. Rotas Protegidas — Integrations

| Rota | Página | Módulo | Menu | Permissão | Estado |
|------|--------|--------|------|-----------|--------|
| `/integrations` | IntegrationHubPage | integrations | ✅ integrations | integrations:read | ✅ Funcional |
| `/integrations/:connectorId` | ConnectorDetailPage | integrations | Não (detalhe) | integrations:read | ✅ Funcional |
| `/integrations/executions` | IngestionExecutionsPage | integrations | Não (sub-rota) | integrations:read | ✅ Funcional |
| `/integrations/freshness` | IngestionFreshnessPage | integrations | Não (sub-rota) | integrations:read | ✅ Funcional |

---

## 9. Rotas Protegidas — Analytics

| Rota | Página | Módulo | Menu | Permissão | Estado |
|------|--------|--------|------|-----------|--------|
| `/analytics` | ProductAnalyticsOverviewPage | product-analytics | ✅ analytics | analytics:read | ✅ Parcial |
| `/analytics/adoption` | ModuleAdoptionPage | product-analytics | Não (sub-rota) | analytics:read | ✅ Parcial |
| `/analytics/personas` | PersonaUsagePage | product-analytics | Não (sub-rota) | analytics:read | ✅ Parcial |
| `/analytics/journeys` | JourneyFunnelPage | product-analytics | Não (sub-rota) | analytics:read | ✅ Parcial |
| `/analytics/value` | ValueTrackingPage | product-analytics | Não (sub-rota) | analytics:read | ✅ Parcial |

---

## 10. Rotas Protegidas — Administration

| Rota | Página | Módulo | Menu | Permissão | Estado |
|------|--------|--------|------|-----------|--------|
| `/users` | UsersPage | identity-access | ✅ admin | identity:users:read | ✅ Funcional |
| `/environments` | EnvironmentsPage | identity-access | Não (sub-rota) | identity:users:read | ✅ Funcional |
| `/break-glass` | BreakGlassPage | identity-access | ✅ admin | identity:sessions:read | ✅ Funcional |
| `/jit-access` | JitAccessPage | identity-access | ✅ admin | identity:users:read | ✅ Funcional |
| `/delegations` | DelegationPage | identity-access | ✅ admin | identity:users:read | ✅ Funcional |
| `/access-reviews` | AccessReviewPage | identity-access | ✅ admin | identity:users:read | ✅ Funcional |
| `/my-sessions` | MySessionsPage | identity-access | ✅ admin | identity:sessions:read | ✅ Funcional |
| `/audit` | AuditPage | audit-compliance | ✅ admin | audit:read | ✅ Funcional |
| `/platform/operations` | PlatformOperationsPage | operations | ✅ admin | platform:admin:read | ✅ Funcional |
| `/platform/configuration` | ConfigurationAdminPage | configuration | ✅ admin | platform:admin:read | ✅ Funcional |
| `/platform/configuration/notifications` | NotificationConfigurationPage | notifications | Não (sub) | platform:admin:read | ✅ Funcional |
| `/platform/configuration/workflows` | WorkflowConfigurationPage | change-governance | Não (sub) | platform:admin:read | ✅ Funcional |
| `/platform/configuration/governance` | GovernanceConfigurationPage | governance | Não (sub) | platform:admin:read | ✅ Funcional |
| `/platform/configuration/catalog-contracts` | CatalogContractsConfigurationPage | catalog | Não (sub) | platform:admin:read | ✅ Funcional |
| `/platform/configuration/operations-finops` | OperationsFinOpsConfigurationPage | operational-intelligence | Não (sub) | platform:admin:read | ✅ Funcional |
| `/platform/configuration/ai-integrations` | AiIntegrationsConfigurationPage | ai-hub | Não (sub) | platform:admin:read | ✅ Funcional |
| `/platform/configuration/advanced` | AdvancedConfigurationConsolePage | configuration | Não (sub) | platform:admin:read | ✅ Funcional |

---

## 11. Páginas Órfãs (Sem Rota no App.tsx)

| Ficheiro | Módulo | Observação |
|----------|--------|------------|
| `ContractGovernancePage.tsx` | contracts/governance | No menu mas sem rota — **candidato a integração** |
| `SpectralRulesetManagerPage.tsx` | contracts/spectral | No menu mas sem rota — **candidato a integração** |
| `CanonicalEntityCatalogPage.tsx` | contracts/canonical | No menu mas sem rota — **candidato a integração** |
| `ContractPortalPage.tsx` | contracts/portal | Sem menu e sem rota — **órfã** |
| `ContractDetailPage.tsx` | catalog/pages | Sem rota — pode ser versão antiga da ContractWorkspacePage |
| `ContractListPage.tsx` | catalog/pages | Sem rota — pode ser versão antiga da ContractCatalogPage |
| `ContractsPage.tsx` | catalog/pages | Sem rota — pode ser versão antiga da ContractCatalogPage |

---

## 12. Rotas Especiais

| Rota | Comportamento | Observação |
|------|--------------|------------|
| `/graph` | Redirect → `/services/graph` | Rota legacy de compatibilidade |
| `/unauthorized` | UnauthorizedPage | Página de acesso negado |
| `*` (catch-all) | Redirect → `/` | Qualquer rota desconhecida vai para a home |

---

## Problemas Identificados

### Críticos

1. **3 itens de menu sem rota real** — `/contracts/governance`, `/contracts/spectral`, `/contracts/canonical`
2. **7 páginas órfãs** — existem como ficheiros mas não estão acessíveis

### Moderados

3. **Páginas possivelmente duplicadas** — ContractDetailPage vs ContractWorkspacePage; ContractListPage vs ContractCatalogPage; ContractsPage
4. **AI Hub inteiro marcado como Parcial** — 11 páginas com UI mas backend com stubs
5. **Product Analytics sem documentação** — 5 páginas sem documentação dedicada

### Menores

6. **Muitas sub-rotas não aparecem no menu** — ~30 rotas são acessíveis apenas por navegação interna (intencional mas pode dificultar descoberta)
7. **EnterpriseControls, EvidencePackages, MaturityScorecards, Benchmarking, Waivers, DelegatedAdmin** — rotas existem mas não têm item direto no menu (acessíveis via sub-navegação)
