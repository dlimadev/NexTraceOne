# NexTraceOne — Inventário de Páginas e Rotas Frontend

**Data:** 2026-03-24
**Fonte:** `src/frontend/src/App.tsx`, `src/frontend/src/components/shell/AppSidebar.tsx`
**Nota:** Baseado no código real do router (App.tsx). Não presume funcionalidade — apenas documenta existência.

---

## Legenda

| Símbolo | Significado |
|---------|-------------|
| ✅ | Rota existe e página existe |
| ⚠️ | Rota existe mas há problemas (redirect, permissão, etc.) |
| ❌ | Página existe no código mas SEM rota em App.tsx |
| 🔕 | Rota existe mas SEM item no menu sidebar |
| 🔐 | Rota protegida por permissão (`ProtectedRoute`) |
| 👁️ | Página de autenticação (fora do AppShell, carregamento eager) |

---

## GRUPO 1 — Autenticação (fora do AppShell)

Estas páginas são carregadas de forma **eager** (não lazy) por serem críticas para o primeiro render.

| Página | Rota | Módulo | Permissão | Estado |
|--------|------|--------|-----------|--------|
| `LoginPage` | `/login` | identity-access | Nenhuma | 👁️ ✅ |
| `ForgotPasswordPage` | `/forgot-password` | identity-access | Nenhuma | 👁️ ✅ |
| `ResetPasswordPage` | `/reset-password` | identity-access | Nenhuma | 👁️ ✅ |
| `ActivationPage` | `/activate` | identity-access | Nenhuma | 👁️ ✅ |
| `MfaPage` | `/mfa` | identity-access | Nenhuma | 👁️ ✅ |
| `InvitationPage` | `/invitation` | identity-access | Nenhuma | 👁️ ✅ |
| `TenantSelectionPage` | `/select-tenant` | identity-access | Nenhuma | 👁️ ✅ |

---

## GRUPO 2 — Home

| Página | Rota | Módulo | Permissão | Estado |
|--------|------|--------|-----------|--------|
| `DashboardPage` | `/` | shared | Nenhuma | ✅ |

---

## GRUPO 3 — Catalog / Services

| Página | Rota | Módulo | Permissão | No Menu | Estado |
|--------|------|--------|-----------|---------|--------|
| `ServiceCatalogListPage` | `/services` | catalog | `catalog:assets:read` | ✅ | 🔐 ✅ |
| `ServiceCatalogPage` | `/services/graph` | catalog | `catalog:assets:read` | ✅ | 🔐 ✅ |
| `ServiceDetailPage` | `/services/:serviceId` | catalog | `catalog:assets:read` | — | 🔐 ✅ 🔕 |
| `SourceOfTruthExplorerPage` | `/source-of-truth` | catalog | `catalog:assets:read` | ✅ | 🔐 ✅ |
| `ServiceSourceOfTruthPage` | `/source-of-truth/services/:serviceId` | catalog | `catalog:assets:read` | — | 🔐 ✅ 🔕 |
| `ContractSourceOfTruthPage` | `/source-of-truth/contracts/:contractVersionId` | catalog | `catalog:assets:read` | — | 🔐 ✅ 🔕 |
| `GlobalSearchPage` | `/search` | catalog | `catalog:assets:read` | — | 🔐 ✅ 🔕 |
| `DeveloperPortalPage` | `/portal/*` | catalog | `developer-portal:read` | ✅ | 🔐 ✅ |
| **Redirect** | `/graph` → `/services/graph` | — | — | — | ⚠️ Redirect |

---

## GRUPO 4 — Contracts

| Página | Rota | Módulo | Permissão | No Menu | Estado |
|--------|------|--------|-----------|---------|--------|
| `ContractCatalogPage` | `/contracts` | contracts | `contracts:read` | ✅ | 🔐 ✅ |
| `CreateServicePage` | `/contracts/new` | contracts | `contracts:write` | ✅ | 🔐 ✅ |
| `DraftStudioPage` | `/contracts/studio/:draftId` | contracts | `contracts:write` | — | 🔐 ✅ 🔕 |
| `ContractWorkspacePage` | `/contracts/:contractVersionId` | contracts | `contracts:read` | — | 🔐 ✅ 🔕 |
| **Redirect** | `/contracts/studio` → `/contracts` | — | — | ⚠️ | ⚠️ Menu aponta aqui mas é redirect |
| **Redirect** | `/contracts/legacy` → `/contracts` | — | — | — | ⚠️ Redirect de compatibilidade |
| `ContractGovernancePage` | `/contracts/governance` | contracts | — | ✅ | ❌ **SEM ROTA** |
| `SpectralRulesetManagerPage` | `/contracts/spectral` | contracts | — | ✅ | ❌ **SEM ROTA** |
| `CanonicalEntityCatalogPage` | `/contracts/canonical` | contracts | — | ✅ | ❌ **SEM ROTA** |
| `ContractPortalPage` | `/contracts/portal` | contracts | — | — | ❌ **SEM ROTA** |

**⚠️ Atenção:** 4 páginas do módulo contracts existem no código mas não têm rota registada em App.tsx. 3 delas têm itens no menu sidebar que não funcionam.

---

## GRUPO 5 — Change Governance

| Página | Rota | Módulo | Permissão | No Menu | Estado |
|--------|------|--------|-----------|---------|--------|
| `ChangeCatalogPage` | `/changes` | change-governance | `change-intelligence:read` | ✅ | 🔐 ✅ |
| `ChangeDetailPage` | `/changes/:changeId` | change-governance | `change-intelligence:read` | — | 🔐 ✅ 🔕 |
| `ReleasesPage` | `/releases` | change-governance | `change-intelligence:releases:read` | ✅ | 🔐 ✅ |
| `WorkflowPage` | `/workflow` | change-governance | `workflow:read` | ✅ | 🔐 ✅ |
| `PromotionPage` | `/promotion` | change-governance | `promotion:read` | ✅ | 🔐 ✅ |

---

## GRUPO 6 — Operations

| Página | Rota | Módulo | Permissão | No Menu | Estado |
|--------|------|--------|-----------|---------|--------|
| `IncidentsPage` | `/operations/incidents` | operations | `operations:incidents:read` | ✅ | 🔐 ✅ |
| `IncidentDetailPage` | `/operations/incidents/:incidentId` | operations | `operations:incidents:read` | — | 🔐 ✅ 🔕 |
| `RunbooksPage` | `/operations/runbooks` | operations | `operations:incidents:read` | ✅ | 🔐 ✅ |
| `TeamReliabilityPage` | `/operations/reliability` | operations | `operations:reliability:read` | ✅ | 🔐 ✅ |
| `ServiceReliabilityDetailPage` | `/operations/reliability/:serviceId` | operations | `operations:reliability:read` | — | 🔐 ✅ 🔕 |
| `AutomationWorkflowsPage` | `/operations/automation` | operations | `operations:automation:read` | ✅ | 🔐 ✅ |
| `AutomationAdminPage` | `/operations/automation/admin` | operations | `operations:automation:read` | — | 🔐 ✅ 🔕 |
| `AutomationWorkflowDetailPage` | `/operations/automation/:workflowId` | operations | `operations:automation:read` | — | 🔐 ✅ 🔕 |
| `EnvironmentComparisonPage` | `/operations/runtime-comparison` | operations | `operations:runtime:read` | ✅ | 🔐 ✅ |

**Nota:** A permissão de Runbooks é `operations:incidents:read` — pode ser incorreta (deveria ser `operations:runbooks:read` conforme o sidebar).

---

## GRUPO 7 — AI Hub

| Página | Rota | Módulo | Permissão | No Menu | Estado |
|--------|------|--------|-----------|---------|--------|
| `AiAssistantPage` | `/ai/assistant` | ai-hub | `ai:assistant:read` | ✅ | 🔐 ✅ |
| `AiAgentsPage` | `/ai/agents` | ai-hub | `ai:assistant:read` | ✅ | 🔐 ✅ |
| `AgentDetailPage` | `/ai/agents/:agentId` | ai-hub | `ai:assistant:read` | — | 🔐 ✅ 🔕 |
| `ModelRegistryPage` | `/ai/models` | ai-hub | `ai:governance:read` | ✅ | 🔐 ✅ |
| `AiPoliciesPage` | `/ai/policies` | ai-hub | `ai:governance:read` | ✅ | 🔐 ✅ |
| `AiRoutingPage` | `/ai/routing` | ai-hub | `ai:governance:read` | ✅ | 🔐 ✅ |
| `IdeIntegrationsPage` | `/ai/ide` | ai-hub | `ai:governance:read` | ✅ | 🔐 ✅ |
| `TokenBudgetPage` | `/ai/budgets` | ai-hub | `ai:governance:read` | ✅ | 🔐 ✅ |
| `AiAuditPage` | `/ai/audit` | ai-hub | `ai:governance:read` | ✅ | 🔐 ✅ |
| `AiAnalysisPage` | `/ai/analysis` | ai-hub | `ai:runtime:write` | ✅ | 🔐 ✅ |

---

## GRUPO 8 — Governance

| Página | Rota | Módulo | Permissão | No Menu | Estado |
|--------|------|--------|-----------|---------|--------|
| `ExecutiveOverviewPage` | `/governance/executive` | governance | `governance:read` | ✅ | 🔐 ✅ |
| `ExecutiveDrillDownPage` | `/governance/executive/drilldown` | governance | `governance:read` | — | 🔐 ✅ 🔕 |
| `ExecutiveFinOpsPage` | `/governance/executive/finops` | governance | `governance:read` | — | 🔐 ✅ 🔕 |
| `ReportsPage` | `/governance/reports` | governance | `governance:read` | ✅ | 🔐 ✅ |
| `CompliancePage` | `/governance/compliance` | governance | `governance:read` | ✅ | 🔐 ✅ |
| `RiskCenterPage` | `/governance/risk` | governance | `governance:read` | ✅ | 🔐 ✅ |
| `RiskHeatmapPage` | `/governance/risk/heatmap` | governance | `governance:read` | — | 🔐 ✅ 🔕 |
| `FinOpsPage` | `/governance/finops` | governance | `governance:read` | ✅ | 🔐 ✅ |
| `ServiceFinOpsPage` | `/governance/finops/service/:serviceId` | governance | `governance:read` | — | 🔐 ✅ 🔕 |
| `TeamFinOpsPage` | `/governance/finops/team/:teamId` | governance | `governance:read` | — | 🔐 ✅ 🔕 |
| `DomainFinOpsPage` | `/governance/finops/domain/:domainId` | governance | `governance:read` | — | 🔐 ✅ 🔕 |
| `PolicyCatalogPage` | `/governance/policies` | governance | `governance:read` | ✅ | 🔐 ✅ |
| `EnterpriseControlsPage` | `/governance/controls` | governance | `governance:read` | — | 🔐 ✅ 🔕 |
| `EvidencePackagesPage` | `/governance/evidence` | governance | `governance:read` | — | 🔐 ✅ 🔕 |
| `MaturityScorecardsPage` | `/governance/maturity` | governance | `governance:read` | — | 🔐 ✅ 🔕 |
| `BenchmarkingPage` | `/governance/benchmarking` | governance | `governance:read` | — | 🔐 ✅ 🔕 |
| `TeamsOverviewPage` | `/governance/teams` | governance | `governance:read` | ✅ | 🔐 ✅ |
| `TeamDetailPage` | `/governance/teams/:teamId` | governance | `governance:read` | — | 🔐 ✅ 🔕 |
| `DomainsOverviewPage` | `/governance/domains` | governance | `governance:read` | ✅ | 🔐 ✅ |
| `DomainDetailPage` | `/governance/domains/:domainId` | governance | `governance:read` | — | 🔐 ✅ 🔕 |
| `GovernancePacksOverviewPage` | `/governance/packs` | governance | `governance:read` | ✅ | 🔐 ✅ |
| `GovernancePackDetailPage` | `/governance/packs/:packId` | governance | `governance:read` | — | 🔐 ✅ 🔕 |
| `WaiversPage` | `/governance/waivers` | governance | `governance:read` | — | 🔐 ✅ 🔕 |
| `DelegatedAdminPage` | `/governance/delegated-admin` | governance | `governance:read` | — | 🔐 ✅ 🔕 |

**Nota:** EnterpriseControls, EvidencePackages, Maturity, Benchmarking, Waivers, DelegatedAdmin existem como rotas reais mas não têm itens no sidebar. Provavelmente são acessíveis via navegação interna a partir das páginas pai mas não estão expostos diretamente no menu.

---

## GRUPO 9 — Integrations

| Página | Rota | Módulo | Permissão | No Menu | Estado |
|--------|------|--------|-----------|---------|--------|
| `IntegrationHubPage` | `/integrations` | integrations | `integrations:read` | ✅ | 🔐 ✅ |
| `ConnectorDetailPage` | `/integrations/:connectorId` | integrations | `integrations:read` | — | 🔐 ✅ 🔕 |
| `IngestionExecutionsPage` | `/integrations/executions` | integrations | `integrations:read` | — | 🔐 ✅ 🔕 |
| `IngestionFreshnessPage` | `/integrations/freshness` | integrations | `integrations:read` | — | 🔐 ✅ 🔕 |

---

## GRUPO 10 — Product Analytics

| Página | Rota | Módulo | Permissão | No Menu | Estado |
|--------|------|--------|-----------|---------|--------|
| `ProductAnalyticsOverviewPage` | `/analytics` | product-analytics | `analytics:read` | ✅ | 🔐 ✅ |
| `ModuleAdoptionPage` | `/analytics/adoption` | product-analytics | `analytics:read` | — | 🔐 ✅ 🔕 |
| `PersonaUsagePage` | `/analytics/personas` | product-analytics | `analytics:read` | — | 🔐 ✅ 🔕 |
| `JourneyFunnelPage` | `/analytics/journeys` | product-analytics | `analytics:read` | — | 🔐 ✅ 🔕 |
| `ValueTrackingPage` | `/analytics/value` | product-analytics | `analytics:read` | — | 🔐 ✅ 🔕 |

---

## GRUPO 11 — Admin / Identity

| Página | Rota | Módulo | Permissão | No Menu | Estado |
|--------|------|--------|-----------|---------|--------|
| `UsersPage` | `/users` | identity-access | `identity:users:read` | ✅ | 🔐 ✅ |
| `EnvironmentsPage` | `/environments` | identity-access | `identity:users:read` | — | 🔐 ✅ 🔕 |
| `BreakGlassPage` | `/break-glass` | identity-access | `identity:sessions:read` | ✅ | 🔐 ✅ |
| `JitAccessPage` | `/jit-access` | identity-access | `identity:users:read` | ✅ | 🔐 ✅ |
| `DelegationPage` | `/delegations` | identity-access | `identity:users:read` | ✅ | 🔐 ✅ |
| `AccessReviewPage` | `/access-reviews` | identity-access | `identity:users:read` | ✅ | 🔐 ✅ |
| `MySessionsPage` | `/my-sessions` | identity-access | `identity:sessions:read` | ✅ | 🔐 ✅ |
| `AuditPage` | `/audit` | audit-compliance | `audit:read` | ✅ | 🔐 ✅ |
| `UnauthorizedPage` | `/unauthorized` | identity-access | Nenhuma | — | ✅ 🔕 |

**⚠️ Atenção:** `EnvironmentsPage` existe com rota mas **não tem item no sidebar**. A gestão de ambientes está invisível na navegação.

---

## GRUPO 12 — Notifications

| Página | Rota | Módulo | Permissão | No Menu | Estado |
|--------|------|--------|-----------|---------|--------|
| `NotificationCenterPage` | `/notifications` | notifications | `notifications:inbox:read` | — | 🔐 ✅ 🔕 |
| `NotificationPreferencesPage` | `/notifications/preferences` | notifications | `notifications:inbox:read` | — | 🔐 ✅ 🔕 |

**⚠️ Atenção:** Notificações provavelmente acessíveis via topbar (ícone de sino) mas não têm item no sidebar.

---

## GRUPO 13 — Platform / Configuration Admin

Estas páginas são para administradores de plataforma (`platform:admin:read`). Accessible via `/platform/operations` e `/platform/configuration` no sidebar.

| Página | Rota | Módulo | Permissão | No Menu | Estado |
|--------|------|--------|-----------|---------|--------|
| `PlatformOperationsPage` | `/platform/operations` | operations | `platform:admin:read` | ✅ | 🔐 ✅ |
| `ConfigurationAdminPage` | `/platform/configuration` | configuration | `platform:admin:read` | ✅ | 🔐 ✅ |
| `NotificationConfigurationPage` | `/platform/configuration/notifications` | notifications | `platform:admin:read` | — | 🔐 ✅ 🔕 |
| `WorkflowConfigurationPage` | `/platform/configuration/workflows` | change-governance | `platform:admin:read` | — | 🔐 ✅ 🔕 |
| `GovernanceConfigurationPage` | `/platform/configuration/governance` | governance | `platform:admin:read` | — | 🔐 ✅ 🔕 |
| `CatalogContractsConfigurationPage` | `/platform/configuration/catalog-contracts` | catalog | `platform:admin:read` | — | 🔐 ✅ 🔕 |
| `OperationsFinOpsConfigurationPage` | `/platform/configuration/operations-finops` | operational-intelligence | `platform:admin:read` | — | 🔐 ✅ 🔕 |
| `AiIntegrationsConfigurationPage` | `/platform/configuration/ai-integrations` | ai-hub | `platform:admin:read` | — | 🔐 ✅ 🔕 |
| `AdvancedConfigurationConsolePage` | `/platform/configuration/advanced` | configuration | `platform:admin:read` | — | 🔐 ✅ 🔕 |

---

## GRUPO 14 — Redirects e Fallback

| De | Para | Tipo |
|----|------|------|
| `/graph` | `/services/graph` | Redirect permanente (legado) |
| `/contracts/studio` | `/contracts` | Redirect — menu item "Contract Studio" não funciona como esperado |
| `/contracts/legacy` | `/contracts` | Redirect de compatibilidade |
| `*` (qualquer rota não encontrada) | `/` | Navigate fallback |

---

## Resumo — Análise de Problemas

### Problemas Críticos

| Problema | Impacto | Afeta |
|---------|---------|-------|
| `ContractGovernancePage` sem rota | Item do menu não funciona | Menu item "Contract Governance" |
| `SpectralRulesetManagerPage` sem rota | Item do menu não funciona | Menu item "Spectral Rulesets" |
| `CanonicalEntityCatalogPage` sem rota | Item do menu não funciona | Menu item "Canonical Entities" |
| `ContractPortalPage` sem rota | Página orphã inacessível | Sem menu entry |
| `/contracts/studio` é redirect | Menu item "Contract Studio" redireciona para o catálogo | UX confusa |

### Problemas Médios

| Problema | Impacto |
|---------|---------|
| `EnvironmentsPage` sem item no menu | Gestão de ambientes invisível |
| Notificações sem item no menu | Acesso apenas via topbar |
| 6 páginas de governance sem menu entry | Navegação interna não clara |
| `RunbooksPage` usa permissão `operations:incidents:read` em vez de `operations:runbooks:read` | Permissão potencialmente incorreta |

### Páginas Potencialmente Inacessíveis Para Utilizadores Comuns

- `EnvironmentsPage` — sem menu, sem link óbvio
- `ContractGovernancePage` — sem rota
- `SpectralRulesetManagerPage` — sem rota
- `CanonicalEntityCatalogPage` — sem rota
- `ContractPortalPage` — sem rota

---

## Totais

| Categoria | Qtd |
|-----------|-----|
| Rotas registadas em App.tsx | ~85 |
| Páginas únicas (excluindo detail/sub) | ~60 |
| Rotas de auth (fora do AppShell) | 7 |
| Rotas protegidas por permissão | ~75 |
| Rotas com item no sidebar | ~35 |
| Rotas SEM item no sidebar | ~50 |
| Páginas com código mas SEM rota | 4 |
| Redirects | 4 |
