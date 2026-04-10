# NexTraceOne — Frontend Audit Report

**Data:** 2026-04-10
**Escopo:** Análise completa de layout, UX, HTML, CSS, i18n, navegação, duplicatas e responsividade
**Ficheiros analisados:** ~301 páginas, ~74 componentes globais, ~24 componentes shell, 8 ficheiros de rotas, 4 ficheiros de localização

---

## Índice

1. [Resumo Executivo](#1-resumo-executivo)
2. [Problemas de Scroll e Overflow](#2-problemas-de-scroll-e-overflow)
3. [Problemas de Layout e Dimensionamento](#3-problemas-de-layout-e-dimensionamento)
4. [Problemas de Espaçamento](#4-problemas-de-espaçamento)
5. [Problemas de i18n](#5-problemas-de-i18n)
6. [Conteúdo e Páginas Duplicadas](#6-conteúdo-e-páginas-duplicadas)
7. [Navegação Quebrada e Rotas Incorretas](#7-navegação-quebrada-e-rotas-incorretas)
8. [Problemas de Responsividade](#8-problemas-de-responsividade)
9. [Problemas de Z-Index e Layering](#9-problemas-de-z-index-e-layering)
10. [Problemas de Segurança de Rotas](#10-problemas-de-segurança-de-rotas)
11. [Plano de Correção Consolidado](#11-plano-de-correção-consolidado)

---

## 1. Resumo Executivo

### Totais por Severidade

| Severidade | Qtd | Descrição |
|---|---|---|
| 🔴 Crítico | 18 | Scroll quebrado, navegação impossível, conteúdo cortado |
| 🟠 Alto | 34 | Layout quebrado em tablet/mobile, i18n em falta, z-index conflitante |
| 🟡 Médio | 47 | Inconsistências de espaçamento, responsividade parcial, touch targets pequenos |
| 🔵 Baixo | 15 | Padronização de estilo, otimizações |

### Áreas mais afetadas

1. **Módulo Contracts** — workspace com triple nested scroll, min-h-screen indevido
2. **Módulo AI Hub** — scroll zones conflitantes, flex sem min-h-0
3. **Shell/Sidebar** — overflow-hidden cortando conteúdo, min-h-0 em falta
4. **Grids não-responsivos** — 64 grids com colunas fixas sem breakpoints
5. **i18n hardcoded** — 59 strings hardcoded em ficheiros de produção

---

## 2. Problemas de Scroll e Overflow

### 2.1 Arquitetura de Scroll

A arquitetura base está correta:
- `AppShell.tsx` → `flex h-screen overflow-hidden`
- `AppContentFrame.tsx` → `flex-1 overflow-y-auto` (scroll principal)

**Porém, várias páginas criam containers de scroll internos que conflituam com o scroll principal.**

### 2.2 Problemas Críticos de Scroll

| # | Ficheiro | Linha | Problema | Severidade |
|---|---|---|---|---|
| S-01 | `components/shell/AppSidebar.tsx` | 333-338 | Content panel usa `overflow-hidden` — conteúdo cortado, sem scroll | 🔴 Crítico |
| S-02 | `components/shell/AppSidebar.tsx` | 360 | `<nav>` flex-1 sem `min-h-0` — scroll não funciona | 🔴 Crítico |
| S-03 | `components/shell/AppSidebar.tsx` | 248-249 | Icon rail flex column sem `min-h-0` | 🟠 Alto |
| S-04 | `components/shell/MobileDrawer.tsx` | 25 | `h-full` sem `overflow-y-auto` — drawer pode estourar verticalmente | 🟠 Alto |
| S-05 | `features/identity-access/components/AuthShell.tsx` | 62 | `min-h-screen overflow-hidden` — conteúdo preso sem scroll | 🔴 Crítico |
| S-06 | `features/contracts/workspace/WorkspaceLayout.tsx` | 68,73,114,120 | Triple nested scroll — nav + main + rail com `overflow-y-auto` separados | 🔴 Crítico |
| S-07 | `features/contracts/cdct/ConsumerDrivenContractPage.tsx` | 83 | `min-h-screen` dentro do shell — força viewport height, quebra flex layout | 🔴 Crítico |
| S-08 | `features/contracts/playground/ContractPlaygroundPage.tsx` | 95 | `min-h-screen` dentro do shell — mesmo problema que S-07 | 🔴 Crítico |
| S-09 | `features/ai-hub/pages/AiAssistantPage.tsx` | 568,647,824,1158 | 4 containers `overflow-y-auto` aninhados — scroll zones conflitantes | 🔴 Crítico |
| S-10 | `features/ai-hub/pages/AiCopilotPage.tsx` | 534,554,605,743 | Múltiplos flex scrolls + flex-1 sem `min-h-0` | 🔴 Crítico |
| S-11 | `features/contracts/studio/DraftStudioPage.tsx` | 157,237 | `overflow-y-auto` interno conflitua com AppContentFrame | 🟠 Alto |
| S-12 | `features/contracts/workspace/sections/ContractSection.tsx` | 78,221 | flex-col h-full sem scroll + flex-1 sem `min-h-0` | 🟠 Alto |
| S-13 | `features/contracts/workspace/editor/ContractEditorSplitPane.tsx` | 38 | flex flex-col h-full sem overflow handling | 🟠 Alto |
| S-14 | `features/catalog/pages/TraceExplorerPage.tsx` (via operations) | 223 | `overflow-hidden relative` — conteúdo preso | 🟠 Alto |

### 2.3 Páginas com `overflow-hidden` que pode cortar conteúdo

| Ficheiro | Linha |
|---|---|
| `features/catalog/pages/ContractPipelinePage.tsx` | 120 |
| `features/catalog/pages/ServiceDiscoveryPage.tsx` | 193 |
| `features/catalog/pages/SecurityGateDashboardPage.tsx` | 150, 453 |
| `features/catalog/pages/AiScaffoldWizardPage.tsx` | 492 |
| `features/contracts/governance/ContractHealthTimelinePage.tsx` | 112 |

---

## 3. Problemas de Layout e Dimensionamento

### 3.1 Componentes com larguras fixas problemáticas

| # | Componente | Linha | Valor | Problema | Severidade |
|---|---|---|---|---|---|
| L-01 | `shell/MobileDrawer.tsx` | 25 | `w-[320px]` | Sem variante responsiva — pode estourar em phones < 375px | 🔴 Crítico |
| L-02 | `shell/DetailPanel.tsx` | 22 | `lg:w-[400px] xl:w-[480px]` | Falta breakpoint `md:` — width errada em tablets (768-1024px) | 🟠 Alto |
| L-03 | `components/SplitView.tsx` | 69 | `lg:w-[400px] xl:w-[480px]` | Mesmo problema: falta `md:` breakpoint | 🟠 Alto |
| L-04 | `shell/PageContainer.tsx` | 31 | `max-w-[1600px]` | Sem redução responsiva para tablets | 🟡 Médio |
| L-05 | `components/Drawer.tsx` | 26-28 | `w-80 / w-[480px] / w-[640px]` | Mix de Tailwind scale e pixel values; sem max-width constraint | 🟡 Médio |
| L-06 | `shell/WorkspaceSwitcher.tsx` | 68,95 | `max-w-[100px] / min-w-[280px]` | Dropdown com min-width fixo; nome truncado | 🟡 Médio |
| L-07 | `shell/AppUserMenu.tsx` | 40,43,53 | `max-w-[120px] × 2 / min-w-[200px]` | Username/email truncados em 120px | 🟡 Médio |

### 3.2 Elementos com alturas fixas

| Componente | Linha | Valor | Risco |
|---|---|---|---|
| `components/Stepper.tsx` | 128 | `text-[10px] max-w-[120px]` | Texto ilegível; step label cortado |
| `shell/AppSidebar.tsx` | 285 | `h-[70px]` | Header fixo sem responsive |
| `shell/AppSidebar.tsx` | 253 | `w-[48px]` | Icon rail fixo |
| `components/FilterChip.tsx` | 57 | `h-[18px]` | Badge muito pequeno |
| `components/NotesWidget.tsx` | 94 | `min-h-[160px]` | Sem encolhimento responsivo |

---

## 4. Problemas de Espaçamento

### 4.1 Inconsistências de padding entre componentes

| Componente | Horizontal | Vertical | Observação |
|---|---|---|---|
| Card (header) | `px-5` | `py-4` | Escala "5" |
| Card (body) | `px-5` | `py-5` | Escala "5" |
| Modal (header) | `px-6` | `py-4` | Escala "6" |
| Modal (body) | `px-6` | `py-5` | Escala "6" |
| Drawer | `px-6` | `py-4/5` | Escala "6" — coerente com Modal |
| DetailPanel | `px-5` | `py-4/5` | Escala "5" — deveria ser "6" como Modal/Drawer |

**Problema:** Card e DetailPanel usam `px-5`, enquanto Modal e Drawer usam `px-6`. Deveria haver um padrão único.

### 4.2 Grids não-responsivos (64 instâncias)

| Ficheiro | Linha | Classes | Severidade |
|---|---|---|---|
| `features/change-governance/components/ReleasesIntelligenceTab.tsx` | 132,274 | `grid grid-cols-3 gap-3` | 🔴 Crítico |
| `features/change-governance/pages/PromotionPage.tsx` | 163 | `grid grid-cols-3 gap-4` | 🔴 Crítico |
| `features/contracts/workspace/sections/ValidationSection.tsx` | 138 | `grid grid-cols-5 gap-3` | 🔴 Crítico |
| `features/governance/pages/TeamsOverviewPage.tsx` | 156 | `grid grid-cols-3 gap-3` | 🔴 Crítico |
| `features/governance/pages/DomainsOverviewPage.tsx` | 158 | `grid grid-cols-3 gap-3` | 🟠 Alto |
| `features/shared/pages/DashboardPage.tsx` | (6 inst.) | `grid grid-cols-2/3` sem breakpoints | 🟠 Alto |
| `features/contracts/workspace/sections/DefinitionSection.tsx` | (6 inst.) | `grid grid-cols-2/3` sem breakpoints | 🟠 Alto |
| `features/governance/pages/ExecutiveOverviewPage.tsx` | (3 inst.) | `grid grid-cols-2/3` sem breakpoints | 🟠 Alto |
| `features/ai-hub/components/AssistantPanel.tsx` | 864 | `grid grid-cols-2` sem breakpoints | 🟡 Médio |
| `features/ai-hub/pages/AgentDetailPage.tsx` | 404 | `grid grid-cols-2` sem breakpoints | 🟡 Médio |

**Exemplo correto existente:** `features/operations/pages/ReliabilitySloManagementPage.tsx:135` — `grid grid-cols-1 xl:grid-cols-3` ✅

### 4.3 Touch targets demasiado pequenos

| Componente | Linha | Tamanho | Problema |
|---|---|---|---|
| `features/knowledge/pages/KnowledgeHubPage.tsx` | 215 | `p-1.5` | Abaixo do mínimo 44px para mobile |
| `features/knowledge/pages/OperationalNotesPage.tsx` | 67,85,119 | `px-3 py-1` | Botões muito pequenos |
| `shell/AppSidebar.tsx` | badges | `text-[9px] min-w-[16px]` | Texto ilegível em mobile |
| `components/Stepper.tsx` | 128 | `text-[10px]` | Texto ilegível |
| `components/FilterChip.tsx` | 57 | `h-[18px]` | Badge pequeno demais |

---

## 5. Problemas de i18n

### 5.1 Estado geral dos locales

| Ficheiro | Keys | Vazios | Estado |
|---|---|---|---|
| `en.json` | 7.453 | 0 | ✅ Completo |
| `es.json` | 7.453 | 0 | ✅ Sincronizado |
| `pt-BR.json` | 7.453 | 0 | ✅ Sincronizado |
| `pt-PT.json` | 7.453 | 0 | ✅ Sincronizado |

**Os ficheiros de locale estão completos e sincronizados.** O problema é que há 59 strings hardcoded nos ficheiros TSX que deviam usar `t()`.

### 5.2 Strings hardcoded em ficheiros de produção (59 instâncias)

#### Placeholders hardcoded (26 instâncias)

| Ficheiro | Linha | Valor |
|---|---|---|
| `features/ai-hub/pages/AiAnalysisPage.tsx` | 396 | `placeholder="e.g. payment-service"` |
| `features/ai-hub/pages/AiAnalysisPage.tsx` | 405 | `placeholder="e.g. 2.1.0"` |
| `features/ai-hub/pages/AiAgentsPage.tsx` | 167 | `placeholder="my-custom-agent"` |
| `features/operations/pages/TraceExplorerPage.tsx` | 378 | `placeholder="0"` |
| `features/contracts/governance/ApiPolicyAsCodePage.tsx` | 174 | `placeholder="my-api-policy"` |
| `features/contracts/governance/ApiPolicyAsCodePage.tsx` | 195 | `placeholder="1.0.0"` |
| `features/governance/pages/ScheduledReportsPage.tsx` | 278 | `placeholder="compliance"` |
| `features/governance/pages/ScheduledReportsPage.tsx` | 326 | `placeholder="user@example.com, team@example.com"` |
| `features/contracts/create/VisualEventBuilder.tsx` | 136,192,201,205,213,215,221 | Múltiplos placeholders de schema/config |
| `features/contracts/create/VisualWorkserviceBuilder.tsx` | 217,224,229,253,255,484 | Múltiplos placeholders de config |
| `features/contracts/create/VisualWebhookBuilder.tsx` | 302,309 | Placeholders de retry config |
| `features/contracts/create/VisualRestBuilder.tsx` | 1049 | `placeholder="^[a-z]+$"` |
| `features/contracts/create/SchemaCompositionEditor.tsx` | 177,221 | Placeholders de schema ref |

#### Texto de status/badge hardcoded (10 instâncias)

| Ficheiro | Linha | Valor |
|---|---|---|
| `features/ai-hub/pages/AiIntegrationsConfigurationPage.tsx` | 122-123 | `>Enabled<` / `>Disabled<` |
| `features/configuration/pages/AdvancedConfigurationConsolePage.tsx` | 92,94-95 | `>Masked<` / `>Enabled<` / `>Disabled<` |
| `features/configuration/pages/AdvancedConfigurationConsolePage.tsx` | 358-361 | `>Inherited<` / `>Default<` / `>Mandatory<` / `>Sensitive<` |
| `features/configuration/pages/AdvancedConfigurationConsolePage.tsx` | 411 | `>Inherited<` |

#### Labels/texto de secção hardcoded (5 instâncias)

| Ficheiro | Linha | Valor |
|---|---|---|
| `features/contracts/workspace/sections/SummarySection.tsx` | 184 | `>Producer<` |
| `features/contracts/workspace/editor/LivePreviewRenderer.tsx` | 212,218 | `>Parameters<` / `>Response<` |
| `features/configuration/pages/AdvancedConfigurationConsolePage.tsx` | 661,666 | `>Previous:<` / `>New:<` |

#### Mensagens de erro hardcoded (5 instâncias)

| Ficheiro | Linha | Valor |
|---|---|---|
| `features/contracts/governance/ContractHealthTimelinePage.tsx` | 105-106 | `"Failed to load..."` / `>Retry<` |
| `features/contracts/governance/CanonicalEntityImpactCascadePage.tsx` | 168-169 | `"Failed to load..."` / `>Retry<` |
| `components/ComboBox.tsx` | 267 | `>No results<` |

#### Aria-labels hardcoded (13 instâncias)

| Ficheiro | Linha | Valor |
|---|---|---|
| `features/contracts/governance/ContractHealthTimelinePage.tsx` | 86 | `aria-label="API Asset ID"` |
| `components/Breadcrumbs.tsx` | 91 | `aria-label="Breadcrumbs"` |
| `components/StackedProgressBar.tsx` | 48 | `aria-label="Progress"` |
| `components/Modal.tsx` | 170 | `aria-label="Close"` |
| `components/NexTraceLogo.tsx` | 63,150 | `aria-label="NexTraceOne"` |
| `components/StatCard.tsx` | 91 | `aria-label="More options"` |
| `components/DataTable.tsx` | 185 | `aria-label="Select all rows"` |
| `components/Toast.tsx` | 122,142 | `aria-label="Notifications"` / `aria-label="Dismiss"` |
| `components/DatePicker.tsx` | 62 | `aria-label="Clear date"` |
| `components/Drawer.tsx` | 165 | `aria-label="Close"` |
| `components/RouteProgressBar.tsx` | 41 | `aria-label="Loading"` |

---

## 6. Conteúdo e Páginas Duplicadas

### 6.1 Páginas duplicadas entre módulos

| Página | Localização 1 | Localização 2 | Severidade |
|---|---|---|---|
| `DoraMetricsPage.tsx` | `features/change-governance/pages/` (257 linhas) | `features/governance/pages/` (289 linhas) | 🟠 Alto |
| `ServiceScorecardPage.tsx` | `features/catalog/pages/` (334 linhas) | `features/governance/pages/` (348 linhas) | 🟠 Alto |

**Detalhes:**
- **DoraMetricsPage** — versão change-governance usa `changeConfidenceApi.getDoraMetrics()`, versão governance usa executive API (`/executive/dora-metrics`). Rotas: `/dora-metrics` vs `/governance/dora-metrics`
- **ServiceScorecardPage** — versão catalog usa `sourceOfTruthApi.getServiceScorecard()`, versão governance usa executive API. Rotas: `/services/scorecards` vs `/governance/scorecards`

**Problema:** Dois componentes com o mesmo nome servindo funcionalidade semelhante mas com dados de APIs diferentes, criando confusão de manutenção e experiência inconsistente para o utilizador.

---

## 7. Navegação Quebrada e Rotas Incorretas

### 7.1 Rotas que não existem (navegação impossível)

| # | Ficheiro | Linha | Path de navegação | Problema | Severidade |
|---|---|---|---|---|---|
| N-01 | `features/knowledge/pages/KnowledgeHubPage.tsx` | 105 | `/knowledge/documents/new` | Rota não definida — resulta em 404 | 🔴 Crítico |
| N-02 | `features/contracts/publication/PublicationCenterPage.tsx` | 61 | `/contracts/studio/new` | Rota não existe — `/contracts/studio` faz redirect para `/contracts` | 🔴 Crítico |

### 7.2 Colisão de rotas

| # | Ficheiro | Linhas | Problema | Severidade |
|---|---|---|---|---|
| N-03 | `routes/governanceRoutes.tsx` | 253,261 | `/governance/scorecards` e `/governance/scorecards/:serviceName` — rota genérica captura antes da específica | 🟠 Alto |

### 7.3 Lazy imports inconsistentes

| # | Ficheiro | Linha | Componente | Problema |
|---|---|---|---|---|
| N-04 | `routes/catalogRoutes.tsx` | 17 | `LegacyAssetCatalogPage` | Lazy import sem `.then()` handler para named exports |
| N-05 | `routes/catalogRoutes.tsx` | 18 | `MainframeSystemDetailPage` | Mesmo problema |
| N-06 | `routes/catalogRoutes.tsx` | 19 | `ServiceDiscoveryPage` | Padrão inconsistente |
| N-07 | `routes/catalogRoutes.tsx` | 20 | `ServiceMaturityPage` | Mesmo problema |

---

## 8. Problemas de Responsividade

### 8.1 Grids sem breakpoints responsivos

**64 instâncias** de `grid-cols-N` (N>=2) sem prefixos `sm:`, `md:`, `lg:`, `xl:`.

Ficheiros mais afetados:
- `features/shared/pages/DashboardPage.tsx` — 6 instâncias
- `features/contracts/workspace/sections/DefinitionSection.tsx` — 6 instâncias
- `features/change-governance/components/ReleasesIntelligenceTab.tsx` — 4 instâncias
- `features/governance/pages/ExecutiveOverviewPage.tsx` — 3 instâncias
- `features/contracts/workspace/sections/SecuritySection.tsx` — 3 instâncias

**Caso mais grave:** `ValidationSection.tsx:138` — `grid grid-cols-5 gap-3` sem qualquer breakpoint (ilegível em mobile).

### 8.2 Flex layouts sem wrapping

**~1.859 instâncias** de `flex` sem `flex-wrap`, `flex-col`, ou constraints de largura. Em ecrãs pequenos, o conteúdo pode criar overflow horizontal.

### 8.3 Elementos sem max-width

**~446 instâncias** de `w-full` sem `max-w-*` constraint, causando esticamento indevido em monitores ultra-wide.

### 8.4 Imagens/SVGs sem dimensões

| Ficheiro | Linha | Problema |
|---|---|---|
| `features/identity-access/components/AuthShell.tsx` | 99 | `<img>` sem w-/h- definidos |
| `features/catalog/pages/ServiceSourceOfTruthPage.tsx` | 350 | `<svg>` sem w-/h- definidos |

---

## 9. Problemas de Z-Index e Layering

### 9.1 Z-index hardcoded (fora do sistema CSS variables)

| Componente | Linha | Valor | Conflito |
|---|---|---|---|
| `components/RouteProgressBar.tsx` | 39 | `z-[9999]` | Excessivo — bypassa todo o sistema |
| `components/shell/AppShell.tsx` | 92 | `z-[9999]` | Conflitua com RouteProgressBar |
| `components/Toast.tsx` | 120 | `z-[var(--z-toast,9999)]` | Fallback excessivo |
| `components/CommandPalette.tsx` | 322 | `z-50` | Deveria usar `var(--z-modal)` |
| `components/ColumnSelector.tsx` | 99 | `z-50` | Conflitua com CommandPalette |
| `components/SavedViewSelector.tsx` | 115,179 | `z-50` × 2 | Conflitua com outros `z-50` |
| `components/DashboardTemplatePicker.tsx` | 34 | `z-50` | Conflitua com CommandPalette |
| `config/RolePicker.tsx` | 122 | `z-50` | Conflitua com modais |
| `components/WatchButton.tsx` | 98 | `z-20` | Pode ser coberto por dropdown |
| `components/ModuleHeader.tsx` | 45 | `z-20` | Hardcoded |
| `components/DataTable.tsx` | 177 | `z-10` | Sticky header; aceitável |

### 9.2 Componentes usando CSS variables corretamente ✅

- `Modal.tsx` → `z-[var(--z-modal)]`
- `Drawer.tsx` → `z-[var(--z-modal)]`
- `Tooltip.tsx` → `z-[var(--z-dropdown)]`
- `DropdownMenu.tsx` → `z-[var(--z-dropdown)]`
- `AppSidebar.tsx` → `z-[var(--z-header)]`
- `AppTopbar.tsx` → `z-[var(--z-header)]`

---

## 10. Problemas de Segurança de Rotas

### 10.1 Rotas sem ProtectedRoute

| Ficheiro | Linha | Página | Problema |
|---|---|---|---|
| `routes/adminRoutes.tsx` | 329-334 | `UserPreferencesPage` | Sem wrapper `ProtectedRoute` |
| `routes/adminRoutes.tsx` | 343-348 | `APIKeysPage` | Sem wrapper `ProtectedRoute` |
| `routes/adminRoutes.tsx` | 349-354 | `IntegrationMappingsPage` | Sem wrapper `ProtectedRoute` |

**Nota:** O backend é a autoridade final de autorização, mas a UX deve refletir proteção adequada no frontend.

---

## 11. Plano de Correção Consolidado

Ver ficheiro separado: [FRONTEND-CORRECTION-PLAN.md](./FRONTEND-CORRECTION-PLAN.md)
