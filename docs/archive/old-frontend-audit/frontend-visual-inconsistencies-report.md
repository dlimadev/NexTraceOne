# Relatório de Inconsistências Visuais — NexTraceOne Frontend

> **Data:** 2026-03-26
> **Foco:** Desvios entre design system definido e implementação real · Tokens ignorados · Padrões divergentes entre páginas

---

## 1. Objetivo

Catalogar todas as inconsistências visuais identificadas no frontend, classificadas por severidade, com localização exata e ação corretiva recomendada.

---

## 2. Inconsistências Críticas

### IC-001 — ServiceDetailPage: Bypass completo do sistema de Badge

**Ficheiro:** `src/features/catalog/pages/ServiceDetailPage.tsx:31-66`
**Severidade:** Crítica
**Tipo:** Bypass de design system

Esta página define 4 mapas de cores locais com Tailwind hardcoded em vez de usar o componente `Badge` com variantes semânticas:

```tsx
// PROBLEMA: Cores Tailwind hardcoded em vez de Badge component
const criticalityColors: Record<Criticality, string> = {
  Critical: 'bg-red-900/40 text-red-300 border border-red-700/50',   // Deveria: Badge variant="danger"
  High:     'bg-orange-900/40 text-orange-300 border border-orange-700/50', // Deveria: Badge variant="warning"
  Medium:   'bg-yellow-900/40 text-yellow-300 border border-yellow-700/50', // Deveria: Badge variant="warning"
  Low:      'bg-slate-800/40 text-slate-300 border border-slate-700/50',    // Deveria: Badge variant="neutral"
};

const lifecycleColors: Record<LifecycleStatus, string> = {
  Planning:   'bg-blue-900/40 text-blue-300 border border-blue-700/50',
  Development:'bg-indigo-900/40 text-indigo-300 border border-indigo-700/50',
  Staging:    'bg-purple-900/40 text-purple-300 border border-purple-700/50',
  Active:     'bg-emerald-900/40 text-emerald-300 border border-emerald-700/50',
  Deprecating:'bg-amber-900/40 text-amber-300 border border-amber-700/50',
  Deprecated: 'bg-orange-900/40 text-orange-300 border border-orange-700/50',
  Retired:    'bg-slate-900/40 text-slate-400 border border-slate-700/50',
};

const protocolColors: Record<string, string> = { ... }       // 6 variantes com cores Tailwind diretas
const contractLifecycleColors: Record<string, string> = { ... } // 7 variantes com cores Tailwind diretas
```

**Impacto:**
- Badges desta página usam `bg-red-900/40` enquanto outras páginas usam `bg-critical/15` via `Badge` — aspeto visual divergente
- Cores `indigo`, `purple`, `teal`, `pink` introduzidas aqui não existem no design system — paleta se expande inadvertidamente
- Quando o design system atualizar cores semânticas, estas definições locais não serão afetadas — criando drift permanente

**Ação:** Substituir os 4 mapas de cores por chamadas ao componente `Badge` com variantes adequadas. Para lifecycle e protocolo (que têm mais estados do que as variantes do Badge suportam), criar mapeamento para as variantes semânticas mais próximas.

---

### IC-002 — ExecutiveOverviewPage: Cores fora do design system

**Ficheiro:** `src/features/governance/pages/ExecutiveOverviewPage.tsx`
**Severidade:** Alta
**Tipo:** Token bypass

```tsx
// PROBLEMA: Cores Tailwind diretas em vez de tokens do design system
color={d.operationalTrend.incidentRateChange < 0 ? 'text-emerald-500' : 'text-critical'}  // linha ~122
color="text-emerald-500"   // linha ~219 (safeChanges)
color="text-orange-500"    // linha ~225 (riskyChanges)
color="text-emerald-500"   // linha ~263 (resolvedLast30Days)
color="text-amber-500"     // linha ~269 (recurrenceRate)
color={barColor}           // linha ~184 — barColor usa 'bg-emerald-500', 'bg-amber-500', 'bg-critical'
```

**Design system correto:**
- `text-emerald-500` → `text-success` (token `--color-success: #1EF2C1`)
- `text-orange-500` → `text-warning` (token `--color-warning: #F5C062`)
- `text-amber-500` → `text-warning`
- `bg-emerald-500` → `bg-success`

**Impacto:** O utilizador vê um verde diferente em `text-emerald-500` (#10b981) versus o verde do design system `--color-success` (#1EF2C1). A paleta da página ExecutiveOverview diverge da paleta do Dashboard.

---

### IC-003 — IncidentDetailPage: Cores fora do design system

**Ficheiro:** `src/features/operations/pages/IncidentDetailPage.tsx:388-389`
**Severidade:** Alta
**Tipo:** Token bypass

```tsx
// PROBLEMA: Cores fora do design system
<CheckCircle size={14} className="text-emerald-400 shrink-0" />  // linha 388
<Clock size={14} className="text-amber-400 shrink-0" />          // linha 389
```

**Correção:** `text-success` e `text-warning` respetivamente.

---

### IC-004 — ServiceDetailPage: Não usa PageContainer

**Ficheiro:** `src/features/catalog/pages/ServiceDetailPage.tsx:113`
**Severidade:** Alta
**Tipo:** Layout inconsistente

```tsx
// PROBLEMA: Container manual em vez de PageContainer
return (
  <div className="p-6 lg:p-8 animate-fade-in">
```

**Padrão correto (usado em DashboardPage, IncidentDetailPage, ExecutiveOverviewPage):**
```tsx
return (
  <PageContainer>
```

**Impacto visual:** Padding de `ServiceDetailPage` é `p-6 lg:p-8` (24px/32px). `PageContainer` usa `px-4 sm:px-5 lg:px-6 xl:px-8` com max-width 1600px. O resultado é que `ServiceDetailPage` tem padding ligeiramente diferente das outras páginas em alguns breakpoints, e não respeita o `max-w-[1600px]`.

---

## 3. Inconsistências Altas

### IC-005 — Sidebar: Ícones duplicados entre itens diferentes

**Ficheiro:** `src/components/shell/AppSidebar.tsx:56-57,49,54`
**Severidade:** Alta
**Tipo:** Identidade visual

| Item | Ícone | Conflito com |
|------|-------|-------------|
| `aiAssistant` (/ai/assistant) | `<Bot size={18} />` | `aiAgents` (/ai/agents) |
| `aiAgents` (/ai/agents) | `<Bot size={18} />` | `aiAssistant` (/ai/assistant) |
| `changeIntelligence` (/releases) | `<Zap size={18} />` | `automation` (/operations/automation) |
| `automation` (/operations/automation) | `<Zap size={18} />` | `changeIntelligence` (/releases) |

**Impacto:** Quando a sidebar está colapsada (modo ícone), o utilizador não consegue distinguir "AI Assistant" de "AI Agents" ou "Change Intelligence" de "Automation". Compromete a navegação por ícone.

---

### IC-006 — StatCard: Indicadores de tendência sem semântica visual adequada

**Ficheiro:** `src/components/StatCard.tsx:29`
**Severidade:** Média
**Tipo:** Qualidade de componente

```tsx
// PROBLEMA: Caracteres ASCII em vez de ícones do sistema
{trend.direction === 'up' ? '↑' : '↓'} {trend.label}
```

**Correção recomendada:** Usar `TrendingUp` e `TrendingDown` de `lucide-react` — consistente com o restante do produto que usa Lucide em todo lado.

---

### IC-007 — Badge: Variantes `default` e `neutral` com CSS idêntico

**Ficheiro:** `src/components/Badge.tsx:16-17`
**Severidade:** Média
**Tipo:** Código duplicado / ambiguidade

```tsx
const variantClasses = {
  default: 'bg-elevated text-body',  // ← idêntico
  neutral: 'bg-elevated text-body',  // ← idêntico
  ...
};
```

**Impacto:** Nenhum visual direto — mas causa confusão em quem escreve código sobre quando usar `default` vs `neutral`. Dois nomes para a mesma coisa divergem ao longo do tempo se alguém alterar um sem o outro.

---

### IC-008 — ExecutiveOverviewPage: Modo de fetch inconsistente com o padrão do produto

**Ficheiro:** `src/features/governance/pages/ExecutiveOverviewPage.tsx:50-62`
**Severidade:** Média
**Tipo:** Inconsistência arquitetural com impacto visual (loading states)

```tsx
// PROBLEMA: useEffect + useState em vez de TanStack Query
const [data, setData] = useState<ExecutiveOverviewResponse | null>(null);
const [loading, setLoading] = useState(true);
const [error, setError] = useState<string | null>(null);

useEffect(() => {
  organizationGovernanceApi.getExecutiveOverview()
    .then(d => { setData(d); setLoading(false); })
    .catch(err => { setError(err.message); setLoading(false); });
}, [t]);
```

**Padrão correto (usado em DashboardPage, ServiceDetailPage, IncidentDetailPage):**
```tsx
const { data, isLoading, isError } = useQuery({
  queryKey: ['executive-overview'],
  queryFn: () => organizationGovernanceApi.getExecutiveOverview(),
  staleTime: 30_000,
});
```

**Impacto:** Loading state divergente (spinner centrado com `Loader2` em vez de `PageLoadingState`), sem cache, sem refetch automático, sem retry built-in. Também incompatível com devtools do TanStack Query.

---

### IC-009 — IncidentDetailPage vs ServiceDetailPage: Padrão de back-link divergente

**Ficheiro:** `IncidentDetailPage.tsx:167` vs `ServiceDetailPage.tsx:115-121`
**Severidade:** Baixa
**Tipo:** Padrão UX inconsistente

```tsx
// IncidentDetailPage usa NavLink
<NavLink to="/operations/incidents" className="flex items-center gap-1 text-sm text-accent hover:underline mb-4">

// ServiceDetailPage usa Link com estilo diferente
<Link to="/services" className="inline-flex items-center gap-1 text-sm text-muted hover:text-accent transition-colors mb-4">
```

Um usa `NavLink` com `text-accent` e `hover:underline`. O outro usa `Link` com `text-muted` e `hover:text-accent`. Pequena diferença, mas em uma plataforma enterprise, padrões idênticos devem ter implementação idêntica.

---

## 4. Inconsistências Médias

### IC-010 — ServiceDetailPage: UUID do serviço exposto ao utilizador

**Ficheiro:** `src/features/operations/pages/IncidentDetailPage.tsx:354`
**Severidade:** Média
**Tipo:** UX / Qualidade de dados

```tsx
<p className="text-xs text-muted">{svc.serviceId} · {svc.serviceType}</p>
```

`serviceId` é provavelmente um UUID (ex: `3fa85f64-5717-4562-b3fc-2c963f66afa6`). Exibir UUIDs ao utilizador é anti-padrão em UX enterprise. Deve ser substituído por um identificador humano (nome do serviço, código de referência, etc.).

---

### IC-011 — Ausência de contadores de alerta na sidebar

**Ficheiro:** `src/components/shell/AppSidebar.tsx`
**Severidade:** Média
**Tipo:** Feature missing / UX

Itens de navegação como `incidents`, `changes`, `governance/risk`, `governance/compliance` não mostram badges com contagem de itens ativos que exigem atenção. Em plataformas enterprise de referência (Jira, Dynatrace, ServiceNow), esta é uma feature fundamental de orientação.

---

### IC-012 — Dashboard: KPI StatCards não são clicáveis

**Ficheiro:** `src/features/shared/pages/DashboardPage.tsx:78-119`
**Severidade:** Média
**Tipo:** UX / Navegação

Os StatCards do dashboard mostram métricas mas não permitem navegar para o contexto. Um utilizador que vê "4 Incidentes Abertos" não tem ação direta a partir do card — precisa navegar manualmente pela sidebar.

---

### IC-013 — Tabelas inline sem uso de TableWrapper

**Ficheiro:** `src/features/catalog/pages/ServiceDetailPage.tsx:201-254`, `src/features/catalog/pages/ServiceDetailPage.tsx:283-345`
**Severidade:** Média
**Tipo:** Componente ignorado

A página `ServiceDetailPage` tem 2 tabelas (`<table>`) construídas manualmente inline. O componente `TableWrapper` existe em `src/components/shell/TableWrapper.tsx` mas não é usado aqui. A consistência de overflow, responsividade e estilos de tabela fica comprometida.

---

### IC-014 — Card: border-radius via Tailwind (8px) vs token do design system (18px)

**Ficheiro:** `src/components/Card.tsx` (não auditado diretamente — inferido dos componentes que o usam)
**Severidade:** Média
**Tipo:** Token não respeitado

O componente `Card` provavelmente usa `rounded-lg` que em Tailwind 4 padrão é 8px. O design system define `--radius-lg: 18px`. Se o `@theme` do Tailwind não remapear `rounded-lg` para `--radius-lg`, existe divergência entre o border-radius declarado e o aplicado.

---

## 5. Inconsistências Baixas

### IC-015 — Dashboard: Tailwind !important override

**Ficheiro:** `src/features/shared/pages/DashboardPage.tsx:215`
**Severidade:** Baixa
**Tipo:** CSS quality

```tsx
<ContentGrid className="!grid-cols-2 lg:!grid-cols-5">
```

O uso de `!` (Tailwind important modifier) indica que o componente `ContentGrid` não suporta nativamente 5 colunas. A solução correta seria expandir `ContentGrid` com suporte a `columns={5}`, em vez de forçar override com `!important`.

---

### IC-016 — Múltiplos ícones usados para "contrato" na sidebar

**Ficheiro:** `src/components/shell/AppSidebar.tsx`
**Severidade:** Baixa
**Tipo:** Semântica de ícone

```tsx
{ icon: <FileText size={18} />, ... contractCatalog }    // FileText = documento
{ icon: <Shield size={18} />, ... contractGovernance }   // Shield = proteção
{ icon: <ShieldCheck size={18} />, ... spectralRulesets } // ShieldCheck = validação
```

`FileText`, `Shield`, `ShieldCheck`, `Database`, `Layers` são usados na seção de contratos e não estabelecem uma semântica visual coerente de "contrato". A semântica de ícone na sidebar deveria ser mais consistente por domínio.

---

## 6. Mapa de Severidade

| ID | Descrição | Severidade | Ficheiro Principal |
|----|-----------|------------|-------------------|
| IC-001 | Bypass completo do Badge em ServiceDetailPage | Crítica | ServiceDetailPage.tsx:31-66 |
| IC-002 | Cores fora do design system em ExecutiveOverviewPage | Alta | ExecutiveOverviewPage.tsx:122-269 |
| IC-003 | Cores fora do design system em IncidentDetailPage | Alta | IncidentDetailPage.tsx:388-389 |
| IC-004 | ServiceDetailPage não usa PageContainer | Alta | ServiceDetailPage.tsx:113 |
| IC-005 | Ícones duplicados na sidebar | Alta | AppSidebar.tsx:56-57,49,54 |
| IC-006 | StatCard com ASCII trend em vez de ícones | Média | StatCard.tsx:29 |
| IC-007 | Badge variantes default=neutral | Média | Badge.tsx:16-17 |
| IC-008 | ExecutiveOverviewPage usa useEffect vs useQuery | Média | ExecutiveOverviewPage.tsx:50-62 |
| IC-009 | Back-link divergente entre páginas de detalhe | Baixa | ServiceDetailPage.tsx:115, IncidentDetailPage.tsx:167 |
| IC-010 | UUID exposto ao utilizador | Média | IncidentDetailPage.tsx:354 |
| IC-011 | Ausência de contadores na sidebar | Média | AppSidebar.tsx |
| IC-012 | KPI StatCards não clicáveis | Média | DashboardPage.tsx:78-119 |
| IC-013 | Tabelas sem TableWrapper | Média | ServiceDetailPage.tsx:201-254 |
| IC-014 | Card border-radius divergente do design system | Média | Card.tsx |
| IC-015 | Tailwind !important no dashboard | Baixa | DashboardPage.tsx:215 |
| IC-016 | Semântica inconsistente de ícones na seção contratos | Baixa | AppSidebar.tsx |

---

## 7. Recomendações Imediatas

**Sprint 1 — Corrigir inconsistências críticas e altas:**
1. `ServiceDetailPage.tsx`: remover 4 mapas de cor inline → usar `Badge` com mapeamento semântico
2. `ExecutiveOverviewPage.tsx`: substituir `text-emerald-500`, `text-orange-500`, `text-amber-500` por tokens do design system
3. `IncidentDetailPage.tsx`: substituir `text-emerald-400`, `text-amber-400`
4. `ServiceDetailPage.tsx`: envolver conteúdo em `<PageContainer>`
5. `ExecutiveOverviewPage.tsx`: migrar para `useQuery`
6. `AppSidebar.tsx`: corrigir ícones duplicados com ícones únicos por item

**Sprint 2 — Prevenir regressão:**
- Adicionar regra ESLint para alertar sobre cores Tailwind hardcoded (`bg-red-*`, `text-emerald-*`, etc.)
- Documentar guia de contribuição com exemplos do padrão correto
