# NexTraceOne — Frontend Correction Plan

**Data:** 2026-04-10
**Referência:** [FRONTEND-AUDIT-REPORT.md](./FRONTEND-AUDIT-REPORT.md)

---

## Organização

O plano está dividido em **7 waves** por prioridade, com estimativa de complexidade e dependências.

---

## Wave 1 — Scroll e Overflow Críticos (Prioridade Máxima)

> **Objetivo:** Corrigir todas as situações onde conteúdo está cortado ou scroll não funciona.

### 1.1 Shell — AppSidebar content panel overflow

- **Ficheiro:** `src/components/shell/AppSidebar.tsx`
- **Linha:** 333-338
- **Correção:** Mudar `overflow-hidden` para `overflow-y-auto` no content panel
- **Complexidade:** Baixa

### 1.2 Shell — AppSidebar nav sem min-h-0

- **Ficheiro:** `src/components/shell/AppSidebar.tsx`
- **Linha:** 360
- **Correção:** Adicionar `min-h-0` ao `<nav className="flex-1 px-5 py-4 overflow-y-auto">`
- **Complexidade:** Baixa

### 1.3 Shell — AppSidebar icon rail sem min-h-0

- **Ficheiro:** `src/components/shell/AppSidebar.tsx`
- **Linha:** 248-249
- **Correção:** Adicionar `min-h-0` ao flex column do icon rail
- **Complexidade:** Baixa

### 1.4 Shell — MobileDrawer sem overflow-y-auto

- **Ficheiro:** `src/components/shell/MobileDrawer.tsx`
- **Linha:** 25
- **Correção:** Adicionar `overflow-y-auto` ao `div.h-full.w-[320px]`
- **Complexidade:** Baixa

### 1.5 AuthShell — overflow-hidden bloqueia conteúdo

- **Ficheiro:** `src/features/identity-access/components/AuthShell.tsx`
- **Linha:** 62
- **Correção:** Remover `overflow-hidden` ou mudar para `overflow-y-auto`
- **Complexidade:** Baixa

### 1.6 WorkspaceLayout — triple nested scroll

- **Ficheiro:** `src/features/contracts/workspace/WorkspaceLayout.tsx`
- **Linhas:** 68, 73, 114, 120
- **Correção:** Remover `overflow-y-auto` dos containers internos; deixar apenas um scroll principal. Adicionar `min-h-0` aos flex children.
- **Complexidade:** Média — requer teste em todo o workspace de contratos

### 1.7 ConsumerDrivenContractPage — min-h-screen indevido

- **Ficheiro:** `src/features/contracts/cdct/ConsumerDrivenContractPage.tsx`
- **Linha:** 83
- **Correção:** Remover `min-h-screen`; confiar no flex layout do AppShell
- **Complexidade:** Baixa

### 1.8 ContractPlaygroundPage — min-h-screen indevido

- **Ficheiro:** `src/features/contracts/playground/ContractPlaygroundPage.tsx`
- **Linha:** 95
- **Correção:** Remover `min-h-screen`; confiar no flex layout do AppShell
- **Complexidade:** Baixa

### 1.9 AiAssistantPage — scroll zones conflitantes

- **Ficheiro:** `src/features/ai-hub/pages/AiAssistantPage.tsx`
- **Linhas:** 568, 647, 824, 1158
- **Correção:** Consolidar para uma única scroll zone por secção (sidebar e main). Adicionar `min-h-0` a todos os flex-1 children.
- **Complexidade:** Alta — página complexa com sidebar + messages + modals

### 1.10 AiCopilotPage — flex sem min-h-0

- **Ficheiro:** `src/features/ai-hub/pages/AiCopilotPage.tsx`
- **Linhas:** 534, 554, 605, 743
- **Correção:** Adicionar `min-h-0` a todos os `flex-1 flex flex-col`. Remover `overflow-y-auto` duplicados.
- **Complexidade:** Média

### 1.11 DraftStudioPage — scroll interno conflitante

- **Ficheiro:** `src/features/contracts/studio/DraftStudioPage.tsx`
- **Linhas:** 157, 237
- **Correção:** Remover `overflow-y-auto` interno; usar scroll do AppContentFrame
- **Complexidade:** Média

### 1.12 ContractSection — flex sem min-h-0

- **Ficheiro:** `src/features/contracts/workspace/sections/ContractSection.tsx`
- **Linhas:** 78, 221
- **Correção:** Adicionar `min-h-0` e verificar overflow handling
- **Complexidade:** Baixa

---

## Wave 2 — Navegação Quebrada (Prioridade Alta)

> **Objetivo:** Corrigir todas as navegações que levam a páginas inexistentes ou incorretas.

### 2.1 Criar rota /knowledge/documents/new

- **Ficheiro:** `src/routes/knowledgeRoutes.tsx`
- **Correção:** Adicionar rota para `/knowledge/documents/new` com o componente adequado (criar página ou redirecionar para KnowledgeDocumentPage em modo criação)
- **Referência:** `features/knowledge/pages/KnowledgeHubPage.tsx:105` faz `navigate('/knowledge/documents/new')`
- **Complexidade:** Média

### 2.2 Criar rota /contracts/studio/new

- **Ficheiro:** `src/routes/contractsRoutes.tsx`
- **Correção:** Adicionar rota para `/contracts/studio/new` com o componente DraftStudioPage em modo criação
- **Referência:** `features/contracts/publication/PublicationCenterPage.tsx:61` tem link para esta rota
- **Complexidade:** Média

### 2.3 Corrigir colisão de rotas /governance/scorecards

- **Ficheiro:** `src/routes/governanceRoutes.tsx`
- **Linhas:** 253, 261
- **Correção:** Reordenar rotas — `/governance/scorecards/:serviceName` antes de `/governance/scorecards`, ou usar nested routes
- **Complexidade:** Baixa

### 2.4 Adicionar ProtectedRoute às rotas sem proteção

- **Ficheiro:** `src/routes/adminRoutes.tsx`
- **Linhas:** 329-334, 343-348, 349-354
- **Correção:** Envolver `UserPreferencesPage`, `APIKeysPage` e `IntegrationMappingsPage` em `<ProtectedRoute>`
- **Complexidade:** Baixa

---

## Wave 3 — i18n — Strings Hardcoded (Prioridade Alta)

> **Objetivo:** Mover todas as 59 strings hardcoded para os ficheiros de locale.

### 3.1 Placeholders hardcoded (26 instâncias)

**Ação:** Criar novas keys nos 4 ficheiros de locale e substituir cada placeholder hardcoded por `t('chave')`.

**Ficheiros a alterar:**
1. `features/ai-hub/pages/AiAnalysisPage.tsx` — 2 placeholders
2. `features/ai-hub/pages/AiAgentsPage.tsx` — 1 placeholder
3. `features/operations/pages/TraceExplorerPage.tsx` — 1 placeholder
4. `features/contracts/governance/ApiPolicyAsCodePage.tsx` — 2 placeholders
5. `features/governance/pages/ScheduledReportsPage.tsx` — 2 placeholders
6. `features/contracts/create/VisualEventBuilder.tsx` — 7 placeholders
7. `features/contracts/create/VisualWorkserviceBuilder.tsx` — 6 placeholders
8. `features/contracts/create/VisualWebhookBuilder.tsx` — 2 placeholders
9. `features/contracts/create/VisualRestBuilder.tsx` — 1 placeholder
10. `features/contracts/create/SchemaCompositionEditor.tsx` — 2 placeholders

### 3.2 Texto de status/badge (10 instâncias)

**Ficheiros a alterar:**
1. `features/ai-hub/pages/AiIntegrationsConfigurationPage.tsx` — "Enabled"/"Disabled"
2. `features/configuration/pages/AdvancedConfigurationConsolePage.tsx` — "Masked", "Enabled", "Disabled", "Inherited", "Default", "Mandatory", "Sensitive"

### 3.3 Labels/secções (5 instâncias)

**Ficheiros a alterar:**
1. `features/contracts/workspace/sections/SummarySection.tsx` — "Producer"
2. `features/contracts/workspace/editor/LivePreviewRenderer.tsx` — "Parameters", "Response"
3. `features/configuration/pages/AdvancedConfigurationConsolePage.tsx` — "Previous:", "New:"

### 3.4 Mensagens de erro (5 instâncias)

**Ficheiros a alterar:**
1. `features/contracts/governance/ContractHealthTimelinePage.tsx` — erro + botão Retry
2. `features/contracts/governance/CanonicalEntityImpactCascadePage.tsx` — erro + botão Retry
3. `components/ComboBox.tsx` — "No results"

### 3.5 Aria-labels (13 instâncias)

**Ficheiros a alterar:**
1. `features/contracts/governance/ContractHealthTimelinePage.tsx` — "API Asset ID"
2. `components/Breadcrumbs.tsx` — "Breadcrumbs"
3. `components/StackedProgressBar.tsx` — "Progress"
4. `components/Modal.tsx` — "Close"
5. `components/NexTraceLogo.tsx` — "NexTraceOne" × 2
6. `components/StatCard.tsx` — "More options"
7. `components/DataTable.tsx` — "Select all rows"
8. `components/Toast.tsx` — "Notifications", "Dismiss"
9. `components/DatePicker.tsx` — "Clear date"
10. `components/Drawer.tsx` — "Close"
11. `components/RouteProgressBar.tsx` — "Loading"

---

## Wave 4 — Z-Index e Layering (Prioridade Média-Alta)

> **Objetivo:** Migrar todos os z-index hardcoded para o sistema de CSS variables.

### 4.1 Eliminar z-[9999]

| Ficheiro | Linha | Correção |
|---|---|---|
| `components/RouteProgressBar.tsx` | 39 | Mudar para `z-[var(--z-toast)]` ou novo `--z-progress-bar` |
| `components/shell/AppShell.tsx` | 92 | Mudar para `z-[var(--z-skip-link)]` (definir novo nível) |

### 4.2 Migrar z-50 para CSS variables

| Ficheiro | Linha | Correção |
|---|---|---|
| `components/CommandPalette.tsx` | 322 | Mudar para `z-[var(--z-modal)]` |
| `components/ColumnSelector.tsx` | 99 | Mudar para `z-[var(--z-dropdown)]` |
| `components/SavedViewSelector.tsx` | 115,179 | Mudar para `z-[var(--z-modal)]` |
| `components/DashboardTemplatePicker.tsx` | 34 | Mudar para `z-[var(--z-modal)]` |
| `config/RolePicker.tsx` | 122 | Mudar para `z-[var(--z-dropdown)]` |

### 4.3 Migrar z-20 para CSS variables

| Ficheiro | Linha | Correção |
|---|---|---|
| `components/WatchButton.tsx` | 98 | Mudar para `z-[var(--z-sticky)]` (definir novo nível) |
| `components/ModuleHeader.tsx` | 45 | Mudar para `z-[var(--z-sticky)]` |

### 4.4 Definir novos níveis de z-index

Adicionar ao CSS/theme:
```css
:root {
  --z-sticky: 10;
  --z-header: 20;
  --z-dropdown: 30;
  --z-modal: 40;
  --z-toast: 50;
  --z-skip-link: 60;
  --z-progress-bar: 70;
}
```

---

## Wave 5 — Responsividade (Prioridade Média)

> **Objetivo:** Corrigir grids não-responsivos e elementos que quebram em mobile/tablet.

### 5.1 Grids não-responsivos — top 10 ficheiros

**Padrão de correção:**
```
❌ grid grid-cols-3 gap-3
✅ grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3
```

| # | Ficheiro | Correção |
|---|---|---|
| 1 | `features/change-governance/components/ReleasesIntelligenceTab.tsx` | Adicionar breakpoints nas 4 grids |
| 2 | `features/change-governance/pages/PromotionPage.tsx` | Adicionar breakpoints |
| 3 | `features/contracts/workspace/sections/ValidationSection.tsx` | grid-cols-5 → grid-cols-1 md:grid-cols-3 lg:grid-cols-5 |
| 4 | `features/governance/pages/TeamsOverviewPage.tsx` | Adicionar breakpoints |
| 5 | `features/governance/pages/DomainsOverviewPage.tsx` | Adicionar breakpoints |
| 6 | `features/shared/pages/DashboardPage.tsx` | Adicionar breakpoints (6 instâncias) |
| 7 | `features/contracts/workspace/sections/DefinitionSection.tsx` | Adicionar breakpoints (6 instâncias) |
| 8 | `features/governance/pages/ExecutiveOverviewPage.tsx` | Adicionar breakpoints (3 instâncias) |
| 9 | `features/ai-hub/components/AssistantPanel.tsx` | Adicionar breakpoints |
| 10 | `features/ai-hub/pages/AgentDetailPage.tsx` | Adicionar breakpoints |

### 5.2 Componentes com largura fixa problemática

| Componente | Correção |
|---|---|
| `shell/MobileDrawer.tsx` | `w-[320px]` → `w-[min(320px,90vw)]` ou `max-w-[320px] w-[90vw]` |
| `shell/DetailPanel.tsx` | Adicionar `md:w-[360px]` entre sm e lg |
| `components/SplitView.tsx` | Adicionar `md:w-[360px]` entre sm e lg |
| `shell/PageContainer.tsx` | Adicionar variantes responsivas de max-width |

### 5.3 Imagens/SVGs sem dimensões

| Ficheiro | Correção |
|---|---|
| `features/identity-access/components/AuthShell.tsx:99` | Adicionar `w-*` e `h-*` ao `<img>` |
| `features/catalog/pages/ServiceSourceOfTruthPage.tsx:350` | Adicionar `w-*` e `h-*` ao `<svg>` |

---

## Wave 6 — Duplicatas e Consolidação (Prioridade Média)

> **Objetivo:** Eliminar redundância entre módulos, consolidando páginas duplicadas.

### 6.1 Consolidar DoraMetricsPage

**Estado atual:** Duas versões em `change-governance/` e `governance/`

**Opções:**
1. **Manter ambas** mas com nomes distintos que reflitam o propósito (e.g., `ChangeDoraMetricsPage` vs `ExecutiveDoraMetricsPage`) e documentar a distinção
2. **Criar componente base** `DoraMetricsView` reutilizado por ambas as páginas com diferentes data sources

**Recomendação:** Opção 2 — extrair lógica visual para componente shared, manter páginas como wrappers que injectam dados.

### 6.2 Consolidar ServiceScorecardPage

**Estado atual:** Duas versões em `catalog/` e `governance/`

**Opções:** Mesmo padrão da 6.1

**Recomendação:** Extrair `ServiceScorecardView` para `features/shared/components/`

---

## Wave 7 — Polish e Consistência (Prioridade Baixa)

> **Objetivo:** Padronizar espaçamento, touch targets e detalhes visuais.

### 7.1 Padronizar espaçamento Card vs Modal/Drawer

- Card usa `px-5`, Modal/Drawer usam `px-6`
- **Decisão necessária:** Escolher um padrão único (recomendado: `px-6` para todos)

### 7.2 Touch targets mínimos

- Auditar todos os elementos interativos com `p-1`, `p-1.5`, `px-3 py-1`
- Garantir mínimo 44px de área clicável
- Ficheiros prioritários: KnowledgeHubPage, OperationalNotesPage, AppSidebar badges

### 7.3 Lazy imports inconsistentes

- **Ficheiro:** `src/routes/catalogRoutes.tsx` linhas 17-20
- **Correção:** Padronizar uso de `.then()` handler em lazy imports para named exports
- **Complexidade:** Baixa

### 7.4 Consistência de Drawer sizing

- `Drawer.tsx` mistura `w-80` (Tailwind scale) com `w-[480px]` (pixel) e `w-[640px]` (pixel)
- **Correção:** Converter todos para a mesma escala

### 7.5 Remover texto com tamanho excessivamente pequeno

| Componente | Valor | Correção |
|---|---|---|
| `AppSidebar.tsx` badges | `text-[9px]` | Mínimo `text-[10px]` ou `text-2xs` |
| `Stepper.tsx` | `text-[10px]` | Mínimo `text-xs` |

---

## Resumo por Wave

| Wave | Foco | Items | Complexidade | Dependências |
|---|---|---|---|---|
| **1** | Scroll/Overflow | 12 | Média | Nenhuma |
| **2** | Navegação | 4 | Média | Nenhuma |
| **3** | i18n | 5 blocos (59 strings) | Baixa-Média | Nenhuma |
| **4** | Z-Index | 4 blocos | Baixa | Definição CSS variables |
| **5** | Responsividade | 3 blocos | Alta | Wave 1 concluída |
| **6** | Duplicatas | 2 consolidações | Alta | Decisão de produto |
| **7** | Polish | 5 items | Baixa | Waves 1-5 concluídas |

---

## Checklist de Execução

### Wave 1 — Scroll/Overflow
- [ ] 1.1 AppSidebar content panel: `overflow-hidden` → `overflow-y-auto`
- [ ] 1.2 AppSidebar nav: adicionar `min-h-0`
- [ ] 1.3 AppSidebar icon rail: adicionar `min-h-0`
- [ ] 1.4 MobileDrawer: adicionar `overflow-y-auto`
- [ ] 1.5 AuthShell: remover `overflow-hidden`
- [ ] 1.6 WorkspaceLayout: eliminar nested scrolls
- [ ] 1.7 ConsumerDrivenContractPage: remover `min-h-screen`
- [ ] 1.8 ContractPlaygroundPage: remover `min-h-screen`
- [ ] 1.9 AiAssistantPage: consolidar scroll zones
- [ ] 1.10 AiCopilotPage: adicionar `min-h-0` a flex children
- [ ] 1.11 DraftStudioPage: remover scroll interno
- [ ] 1.12 ContractSection: adicionar `min-h-0`

### Wave 2 — Navegação
- [ ] 2.1 Criar rota `/knowledge/documents/new`
- [ ] 2.2 Criar rota `/contracts/studio/new`
- [ ] 2.3 Reordenar rotas `/governance/scorecards`
- [ ] 2.4 Adicionar ProtectedRoute a 3 rotas admin

### Wave 3 — i18n
- [ ] 3.1 Migrar 26 placeholders para i18n
- [ ] 3.2 Migrar 10 textos de status/badge
- [ ] 3.3 Migrar 5 labels de secção
- [ ] 3.4 Migrar 5 mensagens de erro
- [ ] 3.5 Migrar 13 aria-labels

### Wave 4 — Z-Index
- [ ] 4.1 Eliminar z-[9999] (2 ficheiros)
- [ ] 4.2 Migrar z-50 (5 componentes)
- [ ] 4.3 Migrar z-20 (2 componentes)
- [ ] 4.4 Definir novos níveis CSS variables

### Wave 5 — Responsividade
- [ ] 5.1 Adicionar breakpoints a 64 grids (top 10 ficheiros)
- [ ] 5.2 Corrigir larguras fixas (4 componentes)
- [ ] 5.3 Adicionar dimensões a img/svg (2 ficheiros)

### Wave 6 — Duplicatas
- [ ] 6.1 Consolidar DoraMetricsPage
- [ ] 6.2 Consolidar ServiceScorecardPage

### Wave 7 — Polish
- [ ] 7.1 Padronizar espaçamento Card/Modal/Drawer
- [ ] 7.2 Auditar touch targets mínimos
- [ ] 7.3 Padronizar lazy imports
- [ ] 7.4 Consistência Drawer sizing
- [ ] 7.5 Remover texto excessivamente pequeno
