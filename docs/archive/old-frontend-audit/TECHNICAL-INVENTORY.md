# TECHNICAL-INVENTORY.md — NexTraceOne Frontend

> **Data:** Junho 2025
> **Escopo:** Inventário técnico completo do frontend React

---

## 1. Componentes Base Existentes (`src/components/`)

### UI Primitivos (candidatos a `shared/ui/`)

| Componente | Estado | Tokens NTO | Acessibilidade | Notas |
|---|---|---|---|---|
| `Button` | ✅ Bom | ✅ Usa tokens | ✅ focus-visible, disabled | 4 variantes: primary, secondary, danger, ghost |
| `Badge` | ✅ Bom | ✅ Usa tokens | Parcial | 5 variantes semânticas. Candidato a base oficial |
| `Card` + `CardHeader` + `CardBody` | ✅ Bom | ✅ Usa tokens | N/A | Simples e reutilizável |
| `TextField` | ✅ Bom | ✅ Usa tokens | ✅ aria-invalid, describedby | h-14, rounded-lg, glow focus |
| `Select` | ✅ Bom | ✅ Usa tokens | ✅ aria-invalid | Nativo estilizado. Tamanho default deveria ser lg |
| `SearchInput` | ✅ Bom | ✅ Usa tokens | Parcial | 3 tamanhos. Falta aria-label |
| `TextArea` | Não auditado | - | - | Verificar |
| `Checkbox` | ✅ Bom | ✅ accent-color | ✅ focus-visible | Nativo com styling |
| `Toggle` | ✅ Bom | ✅ Usa tokens | ✅ role=switch | Animação suave |
| `Radio` | Não auditado | - | - | Verificar |
| `Tabs` | ✅ Bom | ✅ Usa tokens | ✅ role=tab, aria-selected | 2 variantes: underline, pill |
| `Tooltip` | ⚠️ Limitado | ✅ Usa tokens | ❌ CSS-only, sem keyboard | Precisa reescrita com portal |
| `Skeleton` | ✅ Bom | ✅ Usa tokens | ✅ aria-hidden | Shimmer animation |
| `EmptyState` | ✅ Bom | ✅ Usa tokens | N/A | 2 tamanhos: default, compact |
| `Divider` | Não auditado | - | - | Verificar |
| `FilterChip` | Não auditado | - | - | Verificar |
| `InlineMessage` | Não auditado | - | - | Verificar |
| `StatCard` | Não auditado | - | - | Verificar |

### Layout & Shell (candidatos a `shared/layout/`)

| Componente | Estado | Notas |
|---|---|---|
| `AppLayout` | ✅ Bom | Sidebar + Topbar + Content. Redirect se !authenticated |
| `AppHeader` (Topbar) | ✅ Funcional | Falta: workspace selector, notifications, profile dropdown, environment |
| `Sidebar` | ✅ Bom | Persona-aware, collapsible, sections com highlight |
| `Breadcrumbs` | Não auditado | Verificar granularidade |
| `PageHeader` | ✅ Bom | Título + subtitle + badge + actions |
| `ModuleHeader` | Não auditado | Verificar sobreposição com PageHeader |
| `SectionHeader` | Não auditado | Verificar |

### Feedback & State (candidatos a `shared/feedback/`)

| Componente | Estado | Notas |
|---|---|---|
| `ErrorBoundary` | ✅ Bom | Seguro: não expõe stack em prod |
| `StateDisplay` | Não auditado | Verificar |
| `Modal` | ✅ Bom | Usa `<dialog>`, 4 tamanhos, footer slot |
| `Drawer` | ✅ Bom | Side panel, 3 tamanhos, right/left |

### Navigation & Discovery

| Componente | Estado | Notas |
|---|---|---|
| `CommandPalette` | Não auditado profundo | Ctrl+K, busca global |
| `ProtectedRoute` | ✅ Bom | Permission-based route guard |

### Domain-specific (candidatos a permanecer em features/)

| Componente | Estado | Notas |
|---|---|---|
| `DomainBadges` | Feature-specific | Deveria estar em feature |
| `HomeWidgetCard` | Feature-specific | Dashboard only |
| `PersonaQuickstart` | Feature-specific | Dashboard only |
| `QuickActions` | Feature-specific | Dashboard only |
| `OnboardingHints` | Feature-specific | Verificar se é cross-feature |
| `ScopeIndicator` | Feature-specific | Verificar |

---

## 2. Componentes Duplicados

| Conceito | Instâncias | Resolução recomendada |
|---|---|---|
| Badge de criticidade | `Badge.tsx` (base) + inline styles em `ServiceCatalogListPage` | Usar `Badge` base + variantes |
| Badge de lifecycle | `Badge.tsx` + inline em `ServiceCatalogListPage` | Criar variantes no Badge ou `LifecycleBadge` com tokens |
| Status icon | Inline em `IncidentsPage` + potencialmente outras | Criar `StatusIndicator` componente |
| API files de contracts | `features/contracts/api/contracts.ts` + `features/catalog/api/contracts.ts` | Unificar ou separar claramente |
| Card styles | `Card.tsx` + inline cards em várias páginas | Usar `Card` base consistentemente |

---

## 3. Componentes em Falta (DESIGN-SYSTEM.md §4 / §8)

| Componente | Prioridade | Descrição |
|---|---|---|
| `DataTable` | **P0** | Tabela enterprise genérica com header, rows, hover, badges, ações |
| `Pagination` | **P0** | Navegação de páginas para tabelas |
| `StatusBadge` | **P1** | Badge semântico com ícone + cor + texto (semantic state) |
| `KpiCard` | **P1** | Card de métrica com título, valor grande, contexto, trend |
| `FormField` | **P1** | Wrapper para react-hook-form: label + input + error + helper |
| `PasswordInput` | **P1** | TextField + toggle visibility (extrair de LoginPage) |
| `Toast` | **P2** | Notificação efêmera (canto superior direito) |
| `InlineAlert` | **P2** | Alerta contextual dentro do layout |
| `DropdownMenu` | **P2** | Menu de ações com keyboard support |
| `TopologyGraph` | **P3** | Visualização de nós e edges |
| `AuthHero` | **P3** | Componente reutilizável para hero do auth shell |

---

## 4. Telas Existentes (84 páginas)

### Identity & Access

| Página | Rota | Estado |
|---|---|---|
| `LoginPage` | `/login` | ✅ Enterprise, alinhada ao DESIGN.md |
| `TenantSelectionPage` | `/select-tenant` | Funcional |
| `UsersPage` | `/users` | Protected |
| `BreakGlassPage` | `/break-glass` | Protected |
| `JitAccessPage` | `/jit-access` | Protected |
| `DelegationPage` | `/delegations` | Protected |
| `AccessReviewPage` | `/access-reviews` | Protected |
| `MySessionsPage` | `/my-sessions` | Protected |
| `UnauthorizedPage` | (fallback) | Funcional |

### Catalog (11 páginas)

`ServiceCatalogPage`, `ServiceCatalogListPage`, `ServiceDetailPage`, `DeveloperPortalPage`,
`SourceOfTruthExplorerPage`, `ServiceSourceOfTruthPage`, `ContractSourceOfTruthPage`, `GlobalSearchPage`, etc.

### Contracts (7 páginas)

`ContractCatalogPage`, `CreateServicePage`, `ContractWorkspacePage`, `ContractPortalPage`,
`ContractGovernancePage`, `SpectralRulesetManagerPage`, `CanonicalEntityCatalogPage`

### Change Governance (5 páginas)

`ReleasesPage`, `WorkflowPage`, `PromotionPage`, `ChangeCatalogPage`, `ChangeDetailPage`

### Operations (9 páginas)

`IncidentsPage`, `IncidentDetailPage`, `RunbooksPage`, `TeamReliabilityPage`,
`ServiceReliabilityDetailPage`, `AutomationWorkflowsPage`, `AutomationWorkflowDetailPage`,
`AutomationAdminPage`, `PlatformOperationsPage`

### AI Hub (5 páginas)

`AiAssistantPage`, `ModelRegistryPage`, `AiPoliciesPage`, `IdeIntegrationsPage`, `AiRoutingPage`

### Governance (20+ páginas)

`ReportsPage`, `RiskCenterPage`, `CompliancePage`, `FinOpsPage`, `ServiceFinOpsPage`,
`TeamFinOpsPage`, `DomainFinOpsPage`, `ExecutiveFinOpsPage`, `ExecutiveOverviewPage`,
`RiskHeatmapPage`, `MaturityScorecardsPage`, `BenchmarkingPage`, `ExecutiveDrillDownPage`,
`PolicyCatalogPage`, `EvidencePackagesPage`, `EnterpriseControlsPage`,
`GovernancePacksOverviewPage`, `GovernancePackDetailPage`, `PackSimulationPage`, `WaiversPage`,
`TeamsOverviewPage`, `TeamDetailPage`, `DomainsOverviewPage`, `DomainDetailPage`, `DelegatedAdminPage`

### Other

`DashboardPage`, `AuditPage`, `IntegrationHubPage`, `ConnectorDetailPage`,
`IngestionExecutionsPage`, `IngestionFreshnessPage`, `ProductAnalyticsOverviewPage`,
`ModuleAdoptionPage`, `PersonaUsagePage`, `JourneyFunnelPage`, `ValueTrackingPage`

---

## 5. Shells Existentes

| Shell | Implementação | Alinhamento |
|---|---|---|
| Auth Shell | Inline em `LoginPage` (split layout) | ✅ Alinhado ao DESIGN-SYSTEM.md §4.2 |
| App Shell | `AppLayout` + `Sidebar` + `AppHeader` | ✅ Estrutura correta, falta contexto no topbar |

---

## 6. Padrões Reutilizáveis Existentes

| Padrão | Implementação | Estado |
|---|---|---|
| `cn()` class merge | `src/lib/cn.ts` (clsx + twMerge) | ✅ Excelente |
| API client centralizado | `src/api/client.ts` | ✅ Excelente |
| Token storage seguro | `src/utils/tokenStorage.ts` | ✅ Seguro |
| API error resolver | `src/utils/apiErrors.ts` | ✅ Funcional |
| Permission system | `auth/permissions.ts` + `hooks/usePermissions.ts` | ✅ Funcional |
| Persona system | `auth/persona.ts` + `contexts/PersonaContext.tsx` | ✅ Funcional |
| i18n detection | `src/i18n.ts` com navigator.language | ✅ Funcional |
| Protected route | `components/ProtectedRoute.tsx` | ✅ Funcional |

---

## 7. Tokens/Estilos Existentes

| Categoria | Localização | Estado |
|---|---|---|
| Color tokens | `index.css` `@theme` block | ✅ Completo e alinhado |
| Border radius | `index.css` `@theme` block | ✅ 7 níveis |
| Shadows | `index.css` `@theme` block | ✅ Inclui glows |
| Spacing | `index.css` `@theme` block | ✅ 8pt base |
| Typography | `index.css` `@theme` block | ✅ Font stack |
| Motion | `index.css` `:root` block | ✅ 4 durações + 2 easing |
| Gradients | `index.css` `:root` block | ✅ 4 gradientes |
| Z-index | `index.css` `:root` block | ✅ 5 layers |
| Keyframes | `index.css` global | ✅ fade-in, slide-up, shimmer, etc. |

---

## 8. Dívidas Técnicas Identificadas

| ID | Dívida | Severidade | Esforço |
|---|---|---|---|
| DT-01 | 54 ficheiros com cores Tailwind hardcoded | Alto | Médio (mecânico) |
| DT-02 | Sem DataTable genérico (84 páginas) | Alto | Alto |
| DT-03 | react-hook-form + zod instalados mas não usados | Médio | Alto |
| DT-04 | App.tsx monolítico (400+ linhas de rotas) | Médio | Médio |
| DT-05 | Sem aliases TypeScript (@/) | Médio | Baixo |
| DT-06 | Sem query key factory | Médio | Baixo |
| DT-07 | react-query-devtools não activado | Baixo | Mínimo |
| DT-08 | Tooltip inacessível por teclado | Médio | Médio |
| DT-09 | Sem telas auth (forgot, reset, activate, MFA) | Médio | Alto |
| DT-10 | types/index.ts monolítico | Baixo | Médio |
| DT-11 | Sem skip-nav link | Médio | Mínimo |
| DT-12 | Sem testes de acessibilidade automatizados | Médio | Baixo |
| DT-13 | Sem type scale utility classes | Baixo | Baixo |
| DT-14 | Topbar incompleto (falta workspace, env, notifications) | Médio | Médio |
