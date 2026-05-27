# Design Audit Spec — NexTraceOne Frontend
**Data:** 2026-05-27  
**Autor:** Design Audit automatizado + análise manual  
**Escopo:** 19 features, 294 páginas, 870 ficheiros TSX  
**Abordagem:** Dimensão-Primeiro — Standard canónico → Rubrica de scoring → Conformidade por feature → Catálogo por página  

---

## §1 Design Standards Canónicos

O Design Standard define a **verdade** contra a qual todas as páginas são auditadas.

### 1.1 Color Token System

**Tokens aprovados** (exclusivamente via CSS custom properties):

| Token | Uso |
|-------|-----|
| `var(--t-accent)` | Cor primária de interação, CTA, links |
| `var(--t-cyan)` | Destaque técnico, ícones de catálogo |
| `var(--t-success)` | Estados de sucesso, conformidade |
| `var(--t-warning)` | Estados de aviso, atenção |
| `var(--t-critical)` | Erros, incidentes críticos |
| `var(--t-info)` | Informação contextual, badges neutros |
| `var(--t-card)` | Background de cards/panels |
| `var(--t-canvas)` | Background de página |
| `var(--t-edge)` | Bordas, separadores |
| `var(--t-body)` | Texto principal |
| `var(--t-muted)` | Texto secundário, labels |
| `var(--t-faded)` | Texto terciário, timestamps |
| `var(--t-heading)` | Headings, títulos |
| `var(--t-panel)` | Panel elevado sobre card |

**Classes Tailwind aprovadas para cor:**  
Apenas classes baseadas em tokens: `text-body`, `text-muted`, `text-faded`, `text-heading`, `text-accent`, `text-success`, `text-warning`, `text-critical`, `text-cyan`, `text-info`, `bg-accent/10`, `bg-success/10`, `bg-warning/10`, `bg-critical/10`, `border-edge`, `border-accent/20`, etc.

**Classes Tailwind PROIBIDAS:**  
`bg-blue-*`, `bg-red-*`, `bg-green-*`, `bg-yellow-*`, `bg-purple-*`, `bg-orange-*`, `bg-gray-*`, `bg-slate-*`, `bg-zinc-*`, `bg-neutral-*`, `bg-stone-*`, `bg-amber-*`, `bg-lime-*`, `bg-emerald-*`, `bg-teal-*`, `bg-cyan-*`, `bg-sky-*`, `bg-indigo-*`, `bg-violet-*`, `bg-fuchsia-*`, `bg-pink-*`, `bg-rose-*` (e equivalentes `text-*`, `border-*`)

**Hex codes PROIBIDOS** exceto nas seguintes exceções documentadas:

| Hex | Contexto | Justificação |
|-----|----------|-------------|
| `#1B7FE8` | AssistantMessageBubble — user bubble | Brand blue, não tem token equivalente |
| `#0891B2` | AssistantMessageBubble — AI header | Cyan escuro, equivale a `var(--t-cyan)` mas com opacidade diferente |
| Qualquer cor em `[0-9A-Fa-f]{6}` em ficheiros de *widget* ECharts/Recharts | Chart series colors | Charts requerem cores explícitas; devem usar uma paleta interna documentada |

**Paleta de gráficos aprovada** (para ECharts, Recharts — evitar hex avulsos):
```ts
export const CHART_COLORS = {
  primary: 'var(--t-accent)',
  secondary: 'var(--t-cyan)',
  success: 'var(--t-success)',
  warning: 'var(--t-warning)',
  critical: 'var(--t-critical)',
  // Series adicionais com opacidade:
  series: ['#1B7FE8', '#0891B2', '#059669', '#D97706', '#DC2626', '#7C3AED']
};
```

---

### 1.2 Typography Scale

| Uso | Classe aprovada | Notas |
|-----|----------------|-------|
| Título de página | `text-xl font-semibold` ou `text-2xl font-bold` | Via PageHeader |
| Subtítulo de secção | `text-sm font-semibold text-muted` | Labels acima de cards/tables |
| Corpo principal | `text-sm text-body` | Conteúdo padrão |
| Corpo secundário | `text-xs text-muted` | Metadados, descrições |
| Timestamp / terciário | `text-[10px] text-faded` | `text-[10px]` APROVADO |
| Valor de KPI/stat | `text-2xl font-bold tabular-nums` | Via StatCard |
| Label de badge | `text-[10px] font-medium` | `text-[10px]` APROVADO |
| Label de secção uppercase | `text-[10px] uppercase tracking-wider text-muted font-medium` | APROVADO |
| Código/mono | `text-xs font-mono` | IDs, hashes, código |

**PROIBIDO:** qualquer `text-[11px]`, `text-[12px]`, `text-[13px]`, `text-[14px]`, `text-[15px]` (usar as classes de escala Tailwind: `text-xs`=12px, `text-sm`=14px)  
**PROIBIDO:** mistura de `font-medium` e `font-semibold` sem intenção hierárquica clara  
**PROIBIDO:** `style={{ fontSize: 'Xpx' }}` inline (exceto em componentes de gráfico)

---

### 1.3 Spacing & Layout

**PageContainer** (obrigatório em todas as páginas autenticadas com scroll vertical):
```tsx
<PageContainer>
  {/* conteúdo da página */}
</PageContainer>
```
Classes aplicadas: `px-4 md:px-6 lg:px-8 py-6 lg:py-8 max-w-[1600px] mx-auto`

**Exceções justificadas ao PageContainer:**
- Páginas de autenticação (`LoginPage`, `MfaPage`, `ForgotPasswordPage`, `ResetPasswordPage`, `ActivationPage`, `TenantSelectionPage`, `UnauthorizedPage`) — layout full-screen centrado
- Páginas de editor full-screen (`ContractStudioPage`, `DraftStudioPage`, `MonacoEditorWrapper` wrappers) — requerem 100% da viewport
- `AiAssistantPage`, `AiCopilotPage` — chat full-screen

**PageHeader** (obrigatório em todas as páginas com título visível):
```tsx
<PageHeader
  title={t('feature.page.title')}
  subtitle={t('feature.page.subtitle')}
  icon={<SomeIcon />}
  actions={<Button>...</Button>}
/>
```

**Grid patterns aprovados:**
```tsx
// KPI grid (4 colunas)
<div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">

// Dashboard (3 colunas)
<div className="grid grid-cols-1 lg:grid-cols-3 gap-6">

// Detail + sidebar (2 colunas)
<div className="grid grid-cols-1 lg:grid-cols-[1fr_320px] gap-6">

// Cards list (2 colunas)
<div className="grid grid-cols-1 md:grid-cols-2 gap-4">
```

**Spacing vertical entre secções:** `space-y-6` (padrão) ou `space-y-8` (secções maiores)  
**Padding interno de cards:** `p-4` (compacto) ou `p-5` (standard) ou `p-6` (espaçoso)

---

### 1.4 Border & Radius

| Elemento | Classe aprovada |
|----------|----------------|
| Cards, panels, modals | `rounded-xl` (12px) |
| Botões, inputs, dropdowns | `rounded-lg` (8px) |
| Botões pequenos, chips | `rounded-md` (6px) |
| Badges inline | `rounded` (4px) ou `rounded-full` |
| Avatar/icon container | `rounded-lg` ou `rounded-xl` |

**Bordas aprovadas:**
- `border border-edge` — borda standard  
- `border border-edge/50` — borda subtil  
- `border-t-3` com token de cor — accent border (StatCard pattern)

**PROIBIDO:** `border-gray-*`, `border-slate-*`, `border-zinc-*`, etc.

---

### 1.5 Elevation & Shadows

| Nível | Classe | Uso |
|-------|--------|-----|
| 0 (base) | sem sombra | Cards no canvas |
| 1 (hover) | `shadow-sm` | Cards com estado hover |
| 2 (floating) | `shadow-md` | Dropdowns, tooltips |
| 3 (modal) | `shadow-xl` | Modais, drawers, sidepanels |

**Focus ring:** `focus:outline-none focus:ring-2 focus:ring-accent/50`  
**Glow accent:** `box-shadow: 0 0 0 1px var(--t-accent)` para focus de inputs importantes

---

### 1.6 Empty & Error States

**Obrigatório:** usar os componentes `<EmptyState>` e `<ErrorState>` — nunca criar inline.

```tsx
// EmptyState — quando não há dados
<EmptyState
  icon={<SomeIcon />}
  title={t('feature.entity.empty.title')}
  description={t('feature.entity.empty.description')}
  action={<Button onClick={onCreate}>{t('common.create')}</Button>}
/>

// ErrorState — quando há erro de carregamento
<ErrorState
  variant="critical" // ou "warning" | "info"
  title={t('common.error.loadTitle')}
  description={error?.message}
  onRetry={refetch}
/>
```

**Loading states:** skeleton via `animate-pulse` ou componentes Skeleton — nunca spinner de texto `Loading...` sem visual.

**PROIBIDO:** 
- `<p>No data</p>` ou `<p>Nenhum item</p>` ou strings hardcoded de empty state
- `<div className="text-center text-muted">No items found</div>` inline

---

### 1.7 Componentes do Design System (obrigatórios)

| Necessidade | Componente aprovado | PROIBIDO |
|-------------|--------------------|-|
| Badge de estado | `<Badge variant="success\|warning\|critical\|info\|default">` | `<span className="rounded-full bg-green-...">` |
| Card container | `<Card>` + `<CardHeader>` + `<CardFooter>` | `<div className="rounded-xl border p-4">` ad-hoc |
| Stat KPI | `<StatCard icon={} title={} value={} color={}>` | cards KPI custom |
| Tabela de dados | `<DataTable>` | `<table>` nativa sem wrapper |
| Botão | `<Button variant="primary\|secondary\|ghost\|destructive">` | `<button className="bg-indigo-600...">` |
| Estado vazio | `<EmptyState>` | texto inline |
| Estado de erro | `<ErrorState>` | texto inline |
| Header de página | `<PageHeader>` | `<h1>` avulso |
| Container de página | `<PageContainer>` | `<div className="px-8 py-6">` |

---

## §2 Rubrica de Conformidade

### Dimensões de Scoring (0–4 cada, total 0–20)

| Dim. | Nome | D0 — Crítico (0) | D1 — Alerta (1) | D2 — Parcial (2) | D3 — Conforme (3) | D4 — Exemplar (4) |
|------|------|-----------------|-----------------|-----------------|-------------------|-------------------|
| **C1** | Color Tokens | Hex avulsos em >5 lugares OU Tailwind palette em >30% ficheiros | Hex em 1-5 lugares OU Tailwind em 15-30% | Tailwind palette em 5-15% ficheiros | ≤1 exceção doc. + sem Tailwind palette | 0 violações |
| **C2** | Typography | `text-[11-15px]` em >20% ficheiros OU inline `fontSize` | Tamanhos arbitrários em 10-20% | Tamanhos em 3-10% ficheiros | ≤2 ficheiros com tamanhos fora da escala | 0 violações (text-[10px] = APROVADO) |
| **C3** | Layout & Structure | <40% páginas com PageContainer OU PageHeader | 40-60% compliance | 60-80% compliance | 80-95% compliance | >95% compliance (excl. exceções justificadas) |
| **C4** | Component Usage | Reinventa cards, badges, botões sem usar design system | Usa design system parcialmente, mix com custom | Maioria usa design system, alguns custom | >90% uso do design system | 100% design system + exceções documentadas |
| **C5** | State Handling | 0 EmptyState e 0 ErrorState | ≤20% cobertura | 20-60% cobertura | 60-90% cobertura | >90% cobertura + i18n |

### Níveis de Conformidade

| Score | % | Nível | Cor |
|-------|---|-------|-----|
| 17–20 | 85–100% | ✅ Conforme | 🟢 |
| 13–16 | 65–80% | ⚠️ Atenção | 🟡 |
| 9–12 | 45–60% | 🔶 Desvio | 🟠 |
| 0–8 | 0–40% | 🚨 Crítico | 🔴 |

---

## §3 Metodologia de Auditoria

### Scans Automáticos Executados

```bash
# C1 — Hex colors
grep -rn '#[0-9A-Fa-f]{6}' --include="*.tsx" features/

# C1 — Tailwind palette violations  
grep -rln 'bg-(blue|red|green|yellow|purple|orange|gray|slate|zinc|neutral|stone|amber|lime|emerald|teal|cyan|sky|indigo|violet|fuchsia|pink|rose)-[0-9]'

# C2 — Arbitrary font sizes (excluding approved text-[10px])
grep -rln 'text-\[1[1-9]px\]|text-\[2[0-9]px\]' --include="*.tsx"

# C3 — PageContainer coverage
grep -rL "PageContainer" --include="*Page.tsx" features/<module>/

# C3 — PageHeader coverage
grep -rL "PageHeader" --include="*Page.tsx" features/<module>/

# C5 — EmptyState coverage
grep -rln "EmptyState" --include="*.tsx" features/<module>/

# C5 — ErrorState coverage
grep -rln "ErrorState" --include="*.tsx" features/<module>/
```

### Dados Capturados (snapshot 2026-05-27)

| Métrica | Valor | Universo |
|---------|-------|---------|
| Ficheiros TSX totais | 870 | — |
| Páginas (*Page.tsx) | 294 | — |
| Ficheiros com hex colors | 21 | 870 tsx |
| Ficheiros com Tailwind palette | 127 | 870 tsx |
| Páginas sem PageContainer | 54* | 294 |
| Páginas sem PageHeader | 103 | 294 |
| Ficheiros usando EmptyState | 91 | 870 tsx |
| Ficheiros usando ErrorState | 193 | 870 tsx |
| Ficheiros com fontes arbitrárias† | 120+ | 870 tsx |

*Excluindo exceções justificadas (auth, editors, full-screen): ~30 páginas têm exceção válida  
†Inclui `text-[10px]` (APROVADO); as verdadeiras violações são `text-[11-15px]`

---

## §4 Findings Globais (Cross-Feature)

### G1 — Ausência de EmptyState em páginas de listagem 🔴 CRÍTICO

**Escopo:** 91/870 ficheiros usam EmptyState, mas existem centenas de páginas de listagem sem estado vazio definido.

**Impacto:** Páginas de listagem mostram espaço em branco ou tabela vazia sem mensagem — experiência degradada e não profissional.

**Padrão correto:**
```tsx
{data.length === 0 && !isLoading && (
  <EmptyState
    icon={<PackageIcon />}
    title={t('feature.entity.empty.title')}
    description={t('feature.entity.empty.description')}
    action={canCreate && <Button onClick={onOpen}>{t('common.create')}</Button>}
  />
)}
```

**Features mais afetadas:** platform-admin (0 EmptyState em 35 páginas), saas (0), product-analytics (0 em páginas de listagem), operations (6/37 páginas).

---

### G2 — Platform-Admin: Ausência total do Design System 🔴 CRÍTICO

**Escopo:** 31/35 páginas do platform-admin não usam PageContainer nem PageHeader. 27/35 ficheiros usam Tailwind palette em vez de tokens.

**Padrão proibido encontrado:**
```tsx
// platform-admin/pages/AiGovernancePage.tsx
<div className="flex items-center gap-2 px-4 py-2 text-sm bg-indigo-600 text-white rounded-lg hover:bg-indigo-700">
<div className="flex items-center gap-3 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700">
<div className="flex items-start gap-3 p-4 bg-amber-50 border border-amber-200 rounded-lg">
```

**Padrão correto:**
```tsx
<Button variant="primary">...</Button>
<ErrorState variant="critical">...</ErrorState>
// ou com tokens:
<div className="flex items-center gap-3 p-4 bg-critical/10 border border-critical/20 rounded-lg text-critical">
```

---

### G3 — Governance: Chart Widgets com hex colors hardcoded 🔴 CRÍTICO

**Escopo:** 11 ficheiros na feature governance usam hex colors — maioritariamente em widgets de gráfico (`HistogramWidget`, `PieChartWidget`, `OtelMetricsWidget`, `TreemapWidget`, `OtelServiceMapWidget`, `HeatmapCalendarWidget`, `BarGaugeWidget`).

**Problema:** Cores hardcoded ignoram o tema escuro/claro e o design system.

**Solução:** Usar a `CHART_COLORS` palette aprovada (ver §1.1) e CSS custom properties onde possível.

---

### G4 — Catalog: Tailwind palette em componentes de dados 🟠 DESVIO

**Escopo:** 15 ficheiros no catalog usam Tailwind palette. Principal ofensor: `DependencyGraph.tsx` (20 hex codes), `ServiceScoreTab.tsx` (8).

**Padrão proibido:**
```tsx
// catalog/components/DependencyGraph.tsx
const nodeColors = {
  service: '#22c55e',
  api: '#3b82f6',
  warning: '#f59e0b',
  error: '#ef4444',
};
```

**Padrão correto:**
```tsx
const nodeColors = {
  service: 'var(--t-success)',
  api: 'var(--t-accent)',
  warning: 'var(--t-warning)',
  error: 'var(--t-critical)',
};
```

---

### G5 — Contracts: Badge patterns custom em vez de `<Badge>` 🟠 DESVIO

**Escopo:** Módulo contracts usa `<span className="px-2 py-0.5 text-[10px] rounded-full ...">` em dezenas de locais em vez do componente `<Badge>`.

**Padrão proibido:**
```tsx
// contracts/canonical/CanonicalEntityCatalogPage.tsx
<span className={`px-2 py-0.5 text-[10px] rounded-full ${STATE_COLORS[entity.state]}`}>
  {entity.state}
</span>
```

**Padrão correto:**
```tsx
<Badge variant={stateToVariant(entity.state)}>
  {entity.state}
</Badge>
```

---

### G6 — Saas: Ausência de PageContainer e design tokens 🔴 CRÍTICO

**Escopo:** Todas as 4 páginas do saas não usam PageContainer nem PageHeader. 4/5 ficheiros usam Tailwind palette.

**Impacto:** Páginas de administração SaaS visualmente inconsistentes com o resto da plataforma.

---

### G7 — Legacy-Assets: Ausência de PageHeader 🟡 ATENÇÃO

**Escopo:** Ambas as páginas de legacy-assets usam PageContainer mas não PageHeader.

**Impacto:** Páginas sem título/breadcrumb padronizado.

---

### G8 — Catalog: Páginas sem PageContainer 🟠 DESVIO

**Escopo:** 8 páginas do catalog (`AiScaffoldWizardPage`, `ContractPipelinePage`, `SecurityGateDashboardPage`, `ServiceDiscoveryPage`, `ServiceFeatureFlagsPage`, `TemplateDetailPage`, `TemplateEditorPage`, `TemplateLibraryPage`) não usam PageContainer.

---

### G9 — Observability: Dashboard sem layout standard 🔴 CRÍTICO

**Escopo:** `ObservabilityDashboardPage` não usa PageContainer nem PageHeader, e usa hex colors e Tailwind palette nos componentes de dashboard.

---

### G10 — AI Hub: 5 páginas sem PageHeader 🟠 DESVIO

**Escopo:** `AgentDetailPage`, `AiAgentsPage`, `AiAnalysisPage`, `AiAssistantPage`, `AiCopilotPage` não usam PageHeader. Os últimos dois têm exceção válida (full-screen). Os primeiros três precisam de PageHeader.

---

## §5 Conformidade por Feature

| Feature | Páginas | C1 Cor | C2 Tipo | C3 Layout | C4 Componentes | C5 States | Total | % | Nível |
|---------|---------|--------|---------|-----------|----------------|-----------|-------|---|-------|
| **ai-hub** | 15 | 3 | 2 | 2 | 2 | 2 | 11/20 | 55% | 🟠 |
| **audit-compliance** | 1 | 4 | 4 | 4 | 3 | 2 | 17/20 | 85% | 🟢 |
| **catalog** | 27 | 1 | 2 | 1 | 2 | 3 | 9/20 | 45% | 🟠 |
| **change-governance** | 24 | 4 | 3 | 3 | 3 | 2 | 15/20 | 75% | 🟡 |
| **configuration** | 13 | 1 | 3 | 4 | 3 | 3 | 14/20 | 70% | 🟡 |
| **contracts** | 22 | 2 | 2 | 1 | 1 | 1 | 7/20 | 35% | 🔴 |
| **governance** | 69 | 0 | 1 | 3 | 2 | 1 | 7/20 | 35% | 🔴 |
| **identity-access** | 16 | 3 | 3 | 3 | 3 | 2 | 14/20 | 70% | 🟡 |
| **integrations** | 5 | 4 | 4 | 1 | 3 | 2 | 14/20 | 70% | 🟡 |
| **knowledge** | 6 | 3 | 3 | 3 | 3 | 2 | 14/20 | 70% | 🟡 |
| **legacy-assets** | 2 | 4 | 4 | 0 | 3 | 3 | 14/20 | 70% | 🟡 |
| **notifications** | 5 | 3 | 2 | 4 | 3 | 2 | 14/20 | 70% | 🟡 |
| **observability** | 1 | 1 | 4 | 0 | 2 | 0 | 7/20 | 35% | 🔴 |
| **operational-intelligence** | 1 | 4 | 4 | 4 | 3 | 2 | 17/20 | 85% | 🟢 |
| **operations** | 37 | 1 | 2 | 3 | 2 | 2 | 10/20 | 50% | 🟠 |
| **platform-admin** | 35 | 0 | 3 | 0 | 0 | 0 | 3/20 | 15% | 🔴 |
| **product-analytics** | 10 | 4 | 3 | 4 | 3 | 2 | 16/20 | 80% | 🟡 |
| **saas** | 4 | 0 | 4 | 0 | 2 | 0 | 6/20 | 30% | 🔴 |
| **shared** | 1 | 4 | 3 | 2 | 4 | 3 | 16/20 | 80% | 🟡 |
| **PLATAFORMA** | **294** | — | — | — | — | — | **11.3/20** | **56%** | **🟠** |

### Distribuição por Nível

| Nível | Features | % Features |
|-------|---------|-----------|
| 🟢 Conforme (85%+) | audit-compliance, operational-intelligence | 10.5% |
| 🟡 Atenção (65-80%) | change-governance, configuration, identity-access, integrations, knowledge, legacy-assets, notifications, product-analytics, shared | 47.4% |
| 🟠 Desvio (45-60%) | ai-hub, catalog, operations | 15.8% |
| 🔴 Crítico (<45%) | contracts, governance, observability, platform-admin, saas | 26.3% |

---

## §6 Catálogo de Conformidade por Página (294 páginas)

**Legenda:** ✅ presente | ❌ ausente | ⚠️ parcial | — não aplicável

### 6.1 ai-hub (15 páginas)

| Página | PageContainer | PageHeader | Hex Colors | Tailwind Palette | EmptyState | ErrorState | Score Est. | Nível |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| AgentDetailPage | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | 11 | 🟠 |
| AgentMarketplacePage | ✅ | ✅ | ✅ | ⚠️ | ✅ | ✅ | 14 | 🟡 |
| AiAgentsPage | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ | 13 | 🟡 |
| AiAnalysisPage | ✅ | ❌ | ✅ | ✅ | ❌ | ❌ | 9 | 🟠 |
| AiAssistantPage | ✅ | —* | ⚠️ | ✅ | ❌ | ❌ | 10 | 🟠 |
| AiAuditPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 16 | 🟡 |
| AiCopilotPage | ✅ | —* | ✅ | ✅ | ❌ | ❌ | 10 | 🟠 |
| AiIntegrationsConfigurationPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 13 | 🟡 |
| AiMemoryIntelligencePage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 13 | 🟡 |
| AiPoliciesPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 13 | 🟡 |
| AiRoutingPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 16 | 🟡 |
| IdeIntegrationsPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 16 | 🟡 |
| McpServerPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 16 | 🟡 |
| ModelRegistryPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 16 | 🟡 |
| TokenBudgetPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 16 | 🟡 |

*Exceção justificada — full-screen app

### 6.2 audit-compliance (1 página)

| Página | PageContainer | PageHeader | Hex Colors | Tailwind Palette | EmptyState | ErrorState | Score Est. | Nível |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| AuditPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 17 | 🟢 |

### 6.3 catalog (27 páginas)

| Página | PageContainer | PageHeader | Hex Colors | Tailwind Palette | EmptyState | ErrorState | Score Est. | Nível |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| AiScaffoldWizardPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| CatalogContractsConfigurationPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 13 | 🟡 |
| ContractDetailPage | ✅ | ❌ | ✅ | ⚠️ | ✅ | ❌ | 10 | 🟠 |
| ContractListPage | ✅ | ❌ | ✅ | ⚠️ | ✅ | ❌ | 10 | 🟠 |
| ContractPipelinePage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| ContractSourceOfTruthPage | ✅ | ❌ | ✅ | ⚠️ | ✅ | ✅ | 12 | 🟠 |
| ContractsPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 13 | 🟡 |
| CreateServiceInterfacePage | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | 11 | 🟠 |
| DependencyDashboardPage | ✅ | ✅ | ✅ | ⚠️ | ❌ | ✅ | 12 | 🟠 |
| DeveloperExperienceScorePage | ✅ | ✅ | ✅ | ⚠️ | ✅ | ✅ | 14 | 🟡 |
| DeveloperPortalPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 11 | 🟠 |
| GlobalSearchPage | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ | 13 | 🟡 |
| LicenseCompliancePage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 13 | 🟡 |
| SecurityGateDashboardPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| SelfServicePortalPage | ✅ | ✅ | ✅ | ⚠️ | ❌ | ❌ | 10 | 🟠 |
| ServiceCatalogListPage | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ | 14 | 🟡 |
| ServiceCatalogPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 13 | 🟡 |
| ServiceDetailPage | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | 11 | 🟠 |
| ServiceDiscoveryPage | ❌ | ❌ | ✅ | ✅ | ❌ | ✅ | 8 | 🔴 |
| ServiceFeatureFlagsPage | ❌ | ❌ | ⚠️ | ⚠️ | ❌ | ❌ | 3 | 🔴 |
| ServiceMaturityPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 13 | 🟡 |
| ServiceScorecardPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 16 | 🟡 |
| ServiceSourceOfTruthPage | ✅ | ❌ | ✅ | ⚠️ | ✅ | ✅ | 12 | 🟠 |
| SourceOfTruthExplorerPage | ✅ | ❌ | ✅ | ⚠️ | ✅ | ✅ | 12 | 🟠 |
| TemplateDetailPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ✅ | 8 | 🔴 |
| TemplateEditorPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ✅ | 8 | 🔴 |
| TemplateLibraryPage | ❌ | ❌ | ✅ | ⚠️ | ✅ | ✅ | 10 | 🟠 |

### 6.4 change-governance (24 páginas)

| Página | PageContainer | PageHeader | Hex Colors | Tailwind Palette | EmptyState | ErrorState | Score Est. | Nível |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| ChangeAdvisoryPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 |
| ChangeCatalogPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 |
| ChangeDetailPage | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | 12 | 🟠 |
| DoraMetricsPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 |
| EvidencePackViewerPage | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | 15 | 🟡 |
| ExternalReleaseIngestPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| PostReleaseReviewPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 17 | 🟢 |
| PromotionPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 |
| ReleaseApprovalGatewayPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 |
| ReleaseApprovalPoliciesPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 |
| ReleaseCalendarPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 |
| ReleaseChecklistExecutionPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| ReleaseCommitPoolPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 |
| ReleaseControlParametersPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| ReleaseGatesDashboardPage | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | 15 | 🟡 |
| ReleaseImpactReportPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 |
| ReleaseNotesPage | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | 15 | 🟡 |
| ReleaseParameterAuditPage | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | 15 | 🟡 |
| ReleaseParameterEnvironmentOverridePage | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | 15 | 🟡 |
| ReleaseRollbackPage | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | 15 | 🟡 |
| ReleaseTrainPage | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | 15 | 🟡 |
| ReleasesPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 17 | 🟢 |
| WorkflowConfigurationPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 |
| WorkflowPage | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | 12 | 🟠 |

### 6.5 configuration (13 páginas)

| Página | PageContainer | PageHeader | Hex Colors | Tailwind Palette | EmptyState | ErrorState | Score Est. | Nível |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| APIKeysPage | ✅ | ✅ | ✅ | ⚠️ | ✅ | ✅ | 14 | 🟡 |
| AdvancedConfigurationConsolePage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| AutomationRulesPage | ✅ | ✅ | ✅ | ⚠️ | ✅ | ✅ | 14 | 🟡 |
| BrandingAdminPage | ✅ | ✅ | ⚠️ | ⚠️ | ❌ | ✅ | 11 | 🟠 |
| ChangeChecklistsPage | ✅ | ✅ | ✅ | ⚠️ | ✅ | ✅ | 14 | 🟡 |
| ConfigurationAdminPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| ContractTemplatesPage | ✅ | ✅ | ✅ | ⚠️ | ✅ | ✅ | 14 | 🟡 |
| IntegrationMappingsPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 12 | 🟠 |
| ParameterComplianceDashboardPage | ✅ | ✅ | ✅ | ⚠️ | ❌ | ✅ | 12 | 🟠 |
| ParameterUsageReportPage | ✅ | ✅ | ✅ | ⚠️ | ❌ | ✅ | 12 | 🟠 |
| PersonalAlertRulesPage | ✅ | ✅ | ✅ | ⚠️ | ✅ | ✅ | 14 | 🟡 |
| UserPreferencesPage | ✅ | ✅ | ✅ | ⚠️ | ❌ | ✅ | 12 | 🟠 |
| WebhookTemplatesPage | ✅ | ✅ | ✅ | ⚠️ | ✅ | ✅ | 14 | 🟡 |

### 6.6 contracts (22 páginas)

| Página | PageContainer | PageHeader | Hex Colors | Tailwind Palette | EmptyState | ErrorState | Score Est. | Nível |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| CanonicalEntityCatalogPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 16 | 🟡 |
| CanonicalEntityImpactCascadePage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| ContractCatalogPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 16 | 🟡 |
| ConsumerDrivenContractPage | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | 12 | 🟠 |
| CreateContractPage | ✅ | ❌ | ✅ | ✅ | ❌ | ❌ | 10 | 🟠 |
| CreateServicePage | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | 7 | 🔴 |
| ContractGovernancePage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| ContractHealthDashboardPage | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | 7 | 🔴 |
| ContractHealthTimelinePage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| ContractMigrationPage | ✅ | ✅ | ✅ | ⚠️ | ❌ | ❌ | 11 | 🟠 |
| AsyncApiBuilderPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| ContractStudioPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| GraphQLBuilderPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| ProtobufBuilderPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| RestOpenApiBuilderPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| SoapWsdlBuilderPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| ContractPlaygroundPage | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | 7 | 🔴 |
| ContractPortalPage | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ | 14 | 🟡 |
| PublicationCenterPage | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ | 14 | 🟡 |
| SpectralRulesetManagerPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 16 | 🟡 |
| DraftStudioPage | ✅ | ❌ | ✅ | ✅ | ❌ | ❌ | 10 | 🟠 |
| ContractWorkspacePage | ❌ | ❌ | ✅ | ✅ | ❌ | ✅ | 8 | 🔴 |

### 6.7 governance (69 páginas)

| Página | PageContainer | PageHeader | Hex Colors | Tailwind Palette | EmptyState | ErrorState | Score Est. | Nível |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| AiAgentMarketplacePage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| ApiPolicyAsCodePage | ✅ | ✅ | ✅ | ⚠️ | ❌ | ❌ | 12 | 🟠 |
| BenchmarkingPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 17 | 🟢 |
| BreakGlassAccessPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| CompliancePage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 17 | 🟢 |
| CustomDashboardsPage | ✅ | ✅ | ✅ | ⚠️ | ✅ | ✅ | 14 | 🟡 |
| DashboardBuilderPage | ❌ | ❌ | ⚠️ | ⚠️ | ❌ | ✅ | 6 | 🔴 |
| DashboardReportsPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 17 | 🟢 |
| DashboardTemplatesPage | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | 15 | 🟡 |
| DashboardUsageAnalyticsPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 17 | 🟢 |
| DashboardViewPage | ✅ | ❌ | ✅ | ⚠️ | ❌ | ✅ | 11 | 🟠 |
| DashboardsAsCodePage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| DelegatedAdminPage | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ | 14 | 🟡 |
| DomainDetailPage | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | 11 | 🟠 |
| DomainFinOpsPage | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | 11 | 🟠 |
| DomainsOverviewPage | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ | 14 | 🟡 |
| DoraMetricsPage | ✅ | ✅ | ✅ | ⚠️ | ✅ | ✅ | 14 | 🟡 |
| EnterpriseControlsPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| EvidencePackagesPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 17 | 🟢 |
| ExecutiveDrillDownPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| ExecutiveFinOpsPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| ExecutiveIntelligenceDashboardPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| ExecutiveOverviewPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 17 | 🟢 |
| FinOpsBudgetApprovalsPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| FinOpsConfigurationPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| FinOpsPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| GovernanceConfigurationPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| GovernanceGatesPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| GovernancePackDetailPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| GovernancePacksOverviewPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 17 | 🟢 |
| IdeExtensionsConsolePage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| LicensingAdminPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| MaturityScorecardsPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| MobileOnCallPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| NotebookEditorPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ✅ | 8 | 🔴 |
| NotebooksPage | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ | 10 | 🟠 |
| PersonaHomePage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| PluginMarketplacePage | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | 15 | 🟡 |
| PolicyCatalogPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 17 | 🟢 |
| ReportsPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| RiskCenterPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 17 | 🟢 |
| RiskHeatmapPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 17 | 🟢 |
| ScheduledReportsPage | ✅ | ✅ | ✅ | ⚠️ | ✅ | ✅ | 14 | 🟡 |
| ServiceFinOpsPage | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | 11 | 🟠 |
| ServiceScorecardPage | ✅ | ✅ | ✅ | ⚠️ | ✅ | ✅ | 14 | 🟡 |
| TeamDetailPage | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | 11 | 🟠 |
| TeamFinOpsPage | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | 11 | 🟠 |
| TeamsOverviewPage | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ | 14 | 🟡 |
| TechnicalDebtPage | ✅ | ✅ | ✅ | ⚠️ | ✅ | ✅ | 14 | 🟡 |
| WaiversPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 17 | 🟢 |
| WarRoomPage | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | 15 | 🟡 |
| WasteDetectionPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| centers/BlastRadiusExplorerPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| centers/ChangeConfidenceHubPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| centers/ComplianceScorecardCenterPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| centers/DriftCenterPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| centers/EvidencePackViewerPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| centers/FinOpsContextViewsPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| centers/OperationalReadinessBoardPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| centers/ReleaseCalendarGatePage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| centers/RollbackCockpitPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| centers/SLOServiceCenterPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| persona-suites/ArchitectLandscapePage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| persona-suites/AuditorConsolePage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| persona-suites/EngineerCockpitPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| persona-suites/ExecutiveBriefCenterPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| persona-suites/PlatformAdminCockpitPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| persona-suites/ProductPortfolioHomePage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |
| persona-suites/TechLeadCommandCenterPage | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | 13 | 🟡 |

### 6.8 identity-access (16 páginas)

| Página | PageContainer | PageHeader | Hex Colors | Tailwind Palette | EmptyState | ErrorState | Score Est. | Nível | Notas |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|-------|
| AccessReviewPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 | |
| ActivationPage | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | 10 | 🟠 | Auth full-screen* |
| BreakGlassPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 | |
| DelegationPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 | |
| EnvironmentsPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 17 | 🟢 | |
| ForgotPasswordPage | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | 10 | 🟠 | Auth full-screen* |
| JitAccessPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 | |
| LoginPage | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | 10 | 🟠 | Auth full-screen* |
| MfaPage | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | 10 | 🟠 | Auth full-screen* |
| MySessionsPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 17 | 🟢 | |
| OnboardingWizardPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 8 | 🔴 | |
| ResetPasswordPage | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | 10 | 🟠 | Auth full-screen* |
| TenantSelectionPage | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | 10 | 🟠 | Auth full-screen* |
| TenantsPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 17 | 🟢 | |
| UnauthorizedPage | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | 10 | 🟠 | Auth full-screen* |
| UsersPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 17 | 🟢 | |

*Exceção justificada — layout auth full-screen via AuthShell

### 6.9 integrations (5 páginas)

| Página | PageContainer | PageHeader | Hex Colors | Tailwind Palette | EmptyState | ErrorState | Score Est. | Nível |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| ConnectorDetailPage | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | 12 | 🟠 |
| IngestionExecutionsPage | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | 12 | 🟠 |
| IngestionFreshnessPage | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | 12 | 🟠 |
| IntegrationHubPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| WebhookSubscriptionsPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 17 | 🟢 |

### 6.10 knowledge (6 páginas)

| Página | PageContainer | PageHeader | Hex Colors | Tailwind Palette | EmptyState | ErrorState | Score Est. | Nível |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| AutoDocumentationPage | ✅ | ✅ | ✅ | ⚠️ | ❌ | ✅ | 13 | 🟡 |
| KnowledgeDocumentPage | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | 12 | 🟠 |
| KnowledgeGraphPage | ✅ | ✅ | ✅ | ⚠️ | ✅ | ✅ | 15 | 🟡 |
| KnowledgeHubPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| OperationalNotesPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| ServiceTimelinePage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |

### 6.11 legacy-assets (2 páginas)

| Página | PageContainer | PageHeader | Hex Colors | Tailwind Palette | EmptyState | ErrorState | Score Est. | Nível |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| LegacyAssetCatalogPage | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ | 14 | 🟡 |
| MainframeSystemDetailPage | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | 12 | 🟠 |

### 6.12 notifications (5 páginas)

| Página | PageContainer | PageHeader | Hex Colors | Tailwind Palette | EmptyState | ErrorState | Score Est. | Nível |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| NotificationAnalyticsPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| NotificationCenterPage | ✅ | ✅ | ✅ | ⚠️ | ✅ | ✅ | 14 | 🟡 |
| NotificationConfigurationPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| NotificationDetailPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| NotificationPreferencesPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |

### 6.13 observability (1 página)

| Página | PageContainer | PageHeader | Hex Colors | Tailwind Palette | EmptyState | ErrorState | Score Est. | Nível |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| ObservabilityDashboardPage | ❌ | ❌ | ⚠️ | ⚠️ | ❌ | ❌ | 4 | 🔴 |

### 6.14 operational-intelligence (1 página)

| Página | PageContainer | PageHeader | Hex Colors | Tailwind Palette | EmptyState | ErrorState | Score Est. | Nível |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| OperationsFinOpsConfigurationPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 |

### 6.15 operations (37 páginas)

| Página | PageContainer | PageHeader | Hex Colors | Tailwind Palette | EmptyState | ErrorState | Score Est. | Nível |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| AiAnomalyPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| AiIncidentSummarizerPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| AiRunbookSuggesterPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| ApiRegressionPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| AutomationAdminPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| AutomationWorkflowDetailPage | ✅ | ❌ | ✅ | ✅ | ✅ | ✅ | 14 | 🟡 |
| AutomationWorkflowsPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 17 | 🟢 |
| ChaosEngineeringPage | ✅ | ✅ | ✅ | ⚠️ | ✅ | ❌ | 13 | 🟡 |
| CustomChartBuilderPage | ✅ | ✅ | ✅ | ⚠️ | ✅ | ✅ | 14 | 🟡 |
| DbExplorerPage | ✅ | ✅ | ✅ | ⚠️ | ❌ | ✅ | 12 | 🟠 |
| DependencyRiskPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| EnvironmentComparisonPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| ErrorTrackingPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| IncidentDetailPage | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | 12 | 🟠 |
| IncidentTimelinePage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| IncidentsPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 17 | 🟢 |
| LoadTestingPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| LogExplorerPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| OnCallIntelligencePage | ✅ | ✅ | ✅ | ⚠️ | ❌ | ✅ | 12 | 🟠 |
| OnCallSchedulePage | ✅ | ✅ | ✅ | ⚠️ | ❌ | ✅ | 12 | 🟠 |
| PlatformOperationsPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| PostIncidentPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| PredictiveIntelligencePage | ✅ | ✅ | ✅ | ⚠️ | ❌ | ❌ | 11 | 🟠 |
| ProfilingExplorerPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| ReliabilitySloManagementPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| RequestExplorerPage | ✅ | ✅ | ✅ | ⚠️ | ❌ | ✅ | 12 | 🟠 |
| RunbookBuilderPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| RunbooksPage | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | 17 | 🟢 |
| RuntimeIntelligenceDashboardPage | ❌ | ❌ | ⚠️ | ✅ | ❌ | ❌ | 5 | 🔴 |
| ServiceMaturitySrePage | ✅ | ✅ | ✅ | ⚠️ | ❌ | ✅ | 12 | 🟠 |
| ServiceReliabilityDetailPage | ✅ | ❌ | ✅ | ✅ | ❌ | ✅ | 12 | 🟠 |
| SloBurnRatePage | ✅ | ✅ | ✅ | ⚠️ | ❌ | ✅ | 12 | 🟠 |
| SloMarketplacePage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| SreDashboardPage | ✅ | ✅ | ✅ | ⚠️ | ❌ | ✅ | 12 | 🟠 |
| SyntheticMonitoringPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| TeamReliabilityPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| TraceExplorerPage | ✅ | ✅ | ✅ | ⚠️ | ❌ | ✅ | 12 | 🟠 |

### 6.16 platform-admin (35 páginas)

| Página | PageContainer | PageHeader | Hex Colors | Tailwind Palette | EmptyState | ErrorState | Score Est. | Nível |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| AiGovernancePage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| AiModelManagerPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| AiResourceGovernorPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| BackupCoordinatorPage | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | 7 | 🔴 |
| CanaryDashboardPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| CapacityForecastPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| CompliancePacksPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| DatabaseHealthPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| DemoSeedPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| DoraAdminDashboardPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| ElasticsearchManagerPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| EnvironmentPoliciesPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| ExternalHttpAuditPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| FeatureFlagsRuntimePage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| GracefulShutdownPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| GreenOpsPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| MigrationPreviewPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| MtlsManagerPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| MultiTenantSchemaPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| NetworkPolicyPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| NonProdSchedulerPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| ObservabilityModePage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| PlatformAlertRulesPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| PlatformHealthDashboardPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| PreflightPage | ❌ | ❌ | ✅ | ✅ | ❌ | ✅ | 9 | 🟠 |
| ProxyConfigPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| RecoveryWizardPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| ResourceBudgetPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| RightsizingPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| SamlSsoPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| SessionSecurityPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| SetupWizardPage | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | 7 | 🔴 |
| StartupReportPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 14 | 🟡 |
| SupportBundlePage | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | 7 | 🔴 |
| SystemHealthPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |

### 6.17 product-analytics (10 páginas)

| Página | PageContainer | PageHeader | Hex Colors | Tailwind Palette | EmptyState | ErrorState | Score Est. | Nível |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| AdoptionFunnelPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 |
| CohortAnalysisPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 |
| FeatureHeatmapPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 |
| JourneyConfigPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 |
| JourneyFunnelPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 |
| ModuleAdoptionPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 |
| PersonaUsagePage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 |
| ProductAnalyticsOverviewPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 |
| TimeToValuePage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 |
| ValueTrackingPage | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | 15 | 🟡 |

### 6.18 saas (4 páginas)

| Página | PageContainer | PageHeader | Hex Colors | Tailwind Palette | EmptyState | ErrorState | Score Est. | Nível |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| AgentRegistrationsPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| AlertsPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| LicensingPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |
| TenantProvisioningPage | ❌ | ❌ | ✅ | ⚠️ | ❌ | ❌ | 5 | 🔴 |

### 6.19 shared (1 página)

| Página | PageContainer | PageHeader | Hex Colors | Tailwind Palette | EmptyState | ErrorState | Score Est. | Nível |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| DashboardPage | ✅ | —* | ✅ | ✅ | ✅ | ❌ | 16 | 🟡 |

*Exceção justificada — dashboard usa layout custom sem PageHeader standard

---

## §7 Lista de Priorização de Fixes

Ordenada por **impacto × frequência** — fix mais urgente primeiro.

### P1 — CRÍTICO: Migrate platform-admin para design system
**Impacto:** 35 páginas | **Tipo:** C1 + C3 + C4 + C5 combinados  
**Acção:** Para cada página em `features/platform-admin/pages/`:
1. Envolver conteúdo em `<PageContainer>` e adicionar `<PageHeader>`
2. Substituir `bg-indigo-*`, `bg-red-*`, `bg-amber-*`, `bg-emerald-*`, `bg-slate-*` por tokens
3. Substituir `<table>` nativas por `<DataTable>`
4. Adicionar `<EmptyState>` em listas
5. Substituir `<button className="bg-indigo-600...">` por `<Button variant="primary">`

**Ficheiros prioritários:** `AiGovernancePage.tsx`, `AiModelManagerPage.tsx`, `AiResourceGovernorPage.tsx`, `CanaryDashboardPage.tsx` (todos semelhantes, ~10-20 linhas de alteração cada)

---

### P2 — CRÍTICO: Migrate saas para design system
**Impacto:** 4 páginas | **Tipo:** C1 + C3 + C5  
**Acção:** Igual ao P1 mas para `features/saas/pages/` — adicionar `PageContainer`, `PageHeader`, substituir Tailwind palette, adicionar `EmptyState`/`ErrorState`.

---

### P3 — CRÍTICO: Corrigir DependencyGraph e chart widgets com CHART_COLORS
**Impacto:** 20 ficheiros | **Tipo:** C1 Hex  
**Ficheiros:** `catalog/components/DependencyGraph.tsx`, `governance/widgets/HistogramWidget.tsx`, `governance/widgets/PieChartWidget.tsx`, `governance/widgets/OtelMetricsWidget.tsx`, `governance/widgets/TreemapWidget.tsx`, `governance/widgets/OtelServiceMapWidget.tsx`, `governance/widgets/HeatmapCalendarWidget.tsx`  
**Acção:** Criar `src/frontend/src/lib/chartColors.ts` com paleta aprovada. Substituir todos os hex hardcoded por referências à paleta.

---

### P4 — CRÍTICO: Adicionar PageContainer + PageHeader a observability
**Impacto:** 1 página | **Tipo:** C3 + C5  
**Acção:** `ObservabilityDashboardPage.tsx` — envolver em `PageContainer`, adicionar `PageHeader`, adicionar `EmptyState`/`ErrorState` nos sub-componentes de dashboard.

---

### P5 — ALTO: Substituir custom badges por `<Badge>` em contracts
**Impacto:** ~30 ficheiros | **Tipo:** C4  
**Acção:** Substituir todos os `<span className="px-2 py-0.5 text-[10px] rounded-full ...">` por `<Badge variant="...">`. Criar mapeamento `stateToVariant()` em `contracts/lib/contractUtils.ts`.

---

### P6 — ALTO: Adicionar PageHeader às páginas de catalog sem título
**Impacto:** 17 páginas | **Tipo:** C3  
**Páginas:** `ContractDetailPage`, `ContractListPage`, `ContractSourceOfTruthPage`, `CreateServiceInterfacePage`, `GlobalSearchPage`, `ServiceCatalogListPage`, `ServiceDetailPage`, `ServiceSourceOfTruthPage`, `SourceOfTruthExplorerPage`, + 8 mais  
**Acção:** Adicionar `<PageHeader title={t('...')} />` após `<PageContainer>`.

---

### P7 — ALTO: Adicionar PageContainer + PageHeader às páginas de catalog sem container
**Impacto:** 8 páginas | **Tipo:** C3  
**Páginas:** `AiScaffoldWizardPage`, `ContractPipelinePage`, `SecurityGateDashboardPage`, `ServiceDiscoveryPage`, `ServiceFeatureFlagsPage`, `TemplateDetailPage`, `TemplateEditorPage`, `TemplateLibraryPage`  
**Acção:** Envolver conteúdo em `<PageContainer>` e adicionar `<PageHeader>`.

---

### P8 — ALTO: Substituir Tailwind palette em catalog por tokens
**Impacto:** 15 ficheiros | **Tipo:** C1  
**Acção:** Substituir cores Tailwind (ex: `bg-blue-50`, `text-slate-600`) por equivalentes de token:
- `bg-blue-50` → `bg-accent/10`
- `text-slate-600` → `text-muted`
- `border-slate-200` → `border-edge`

---

### P9 — MÉDIO: Adicionar EmptyState em páginas de product-analytics
**Impacto:** 10 páginas | **Tipo:** C5  
**Acção:** Cada página de analytics tem queries com `isLoading` mas sem `EmptyState` quando dados estão vazios. Adicionar `EmptyState` com mensagem analítica contextual.

---

### P10 — MÉDIO: Adicionar PageHeader em integrations (3 páginas)
**Impacto:** 3 páginas | **Tipo:** C3  
**Páginas:** `ConnectorDetailPage`, `IngestionExecutionsPage`, `IngestionFreshnessPage`  
**Acção:** Adicionar `<PageHeader>` com título e ação de refresh.

---

### P11 — MÉDIO: Corrigir contracts sem PageContainer/PageHeader
**Impacto:** 6+11 páginas | **Tipo:** C3  
**Páginas sem container:** `CanonicalEntityImpactCascadePage`, `CreateServicePage`, `ContractHealthDashboardPage`, `ContractHealthTimelinePage`, `ContractPlaygroundPage`, `ContractWorkspacePage`  
**Acção:** Algumas são editores full-screen (exceção válida), outras precisam de container.

---

### P12 — MÉDIO: Adicionar ErrorState em governance centers e persona-suites
**Impacto:** 17 páginas | **Tipo:** C5  
**Páginas:** Todos os `centers/*` e `persona-suites/*` que têm `has_error=0`  
**Acção:** Adicionar `<ErrorState>` como fallback quando queries falham.

---

### P13 — BAIXO: Adicionar PageHeader em legacy-assets (2 páginas)
**Impacto:** 2 páginas | **Tipo:** C3  
**Páginas:** `LegacyAssetCatalogPage`, `MainframeSystemDetailPage`  
**Acção:** Adicionar `<PageHeader>` com título descritivo.

---

### P14 — BAIXO: Substituir Tailwind palette em configuration por tokens
**Impacto:** 10 ficheiros | **Tipo:** C1  
**Nota:** `BrandingAdminPage` tem exceção justificada (configurador de marca). Restantes devem migrar para tokens.

---

## §8 Componentes a Criar ou Standardizar

### C-1: `src/frontend/src/lib/chartColors.ts` (NOVO — P3)
```ts
// Paleta de cores aprovada para gráficos ECharts/Recharts
export const CHART_SERIES_COLORS = [
  '#1B7FE8', // accent blue
  '#0891B2', // cyan
  '#059669', // success green
  '#D97706', // warning amber
  '#DC2626', // critical red
  '#7C3AED', // purple
  '#EA580C', // orange
  '#0369A1', // sky blue
] as const;

export const CHART_SEMANTIC_COLORS = {
  success: 'var(--t-success)',
  warning: 'var(--t-warning)',
  critical: 'var(--t-critical)',
  info: 'var(--t-info)',
  accent: 'var(--t-accent)',
  muted: 'var(--t-muted)',
} as const;
```

### C-2: Extender `<Badge>` com mapeamento de estado de contrato (contracts)
```ts
// Em contracts/lib/contractUtils.ts
export function stateToVariant(state: ContractState): BadgeVariant {
  const map: Record<ContractState, BadgeVariant> = {
    Draft: 'default',
    InReview: 'warning',
    Approved: 'success',
    Locked: 'info',
    Deprecated: 'critical',
  };
  return map[state] ?? 'default';
}
```

### C-3: `<AdminPageShell>` para platform-admin (NOVO)
Template padrão para todas as páginas de platform-admin:
```tsx
// components/AdminPageShell.tsx
export function AdminPageShell({ title, subtitle, icon, actions, children }: Props) {
  return (
    <PageContainer>
      <PageHeader title={title} subtitle={subtitle} icon={icon} actions={actions} />
      <div className="space-y-6">{children}</div>
    </PageContainer>
  );
}
```

### C-4: `<AnalyticsPageShell>` para product-analytics (NOVO)
Template com EmptyState padronizado para quando não há dados históricos:
```tsx
export function AnalyticsPageShell({ title, isLoading, isEmpty, emptyTitle, children }: Props) {
  return (
    <PageContainer>
      <PageHeader title={title} />
      {isEmpty && !isLoading ? (
        <EmptyState icon={<BarChart3 />} title={emptyTitle} />
      ) : children}
    </PageContainer>
  );
}
```

---

## Resumo Executivo

**Estado da plataforma:** 🟠 Desvio (56% conformidade média)

**Descobertas críticas:**
1. **platform-admin** (3/20 = 15%) — Feature mais problemática: 31/35 páginas sem design system, 0 EmptyState/ErrorState
2. **saas** (6/20 = 30%) — Todas as 4 páginas sem layout padrão e sem states
3. **contracts** (7/20 = 35%) — Badges custom em vez de `<Badge>`, 6 páginas sem container
4. **governance** (7/20 = 35%) — 11 ficheiros de widgets com hex hardcoded; estados de empty/error muito baixos
5. **observability** (7/20 = 35%) — Única página sem layout standard

**Destaques positivos:**
- **audit-compliance** (17/20 = 85%) — Exemplar
- **operational-intelligence** (17/20 = 85%) — Exemplar  
- **change-governance** (15/20 = 75%) — Muito bom, color tokens perfeitos
- **product-analytics** (16/20 = 80%) — Bem estruturado, apenas falta EmptyState

**Principais padrões de desvio (por frequência):**
1. Ausência de EmptyState em páginas de listagem (>150 páginas afetadas)
2. Ausência de PageHeader em detalhe/sub-páginas (~100 páginas)
3. Tailwind palette em vez de tokens (127 ficheiros)
4. Badges inline em vez de `<Badge>` (principalmente contracts)
5. Hex colors em chart widgets (principalmente governance)

**Plano de implementação sugerido:**
- Sprint 1: P1 (platform-admin) + P2 (saas) → elimina as features 🔴 mais críticas
- Sprint 2: P3 (chart colors) + P4 (observability) + P5 (contracts badges)
- Sprint 3: P6-P8 (catalog cleanup: PageHeader, PageContainer, tokens)
- Sprint 4: P9-P12 (EmptyState sweep em todas as features)
- Sprint 5: P13-P14 (refinamentos finais)

---

*Spec gerado em 2026-05-27. Auditoria baseada em scan automático de 294 ficheiros *Page.tsx e análise manual de componentes representativos. Score estimado por página — auditoria manual completa pode revelar desvios adicionais de C2 (tipografia) e C4 (componentes).*
