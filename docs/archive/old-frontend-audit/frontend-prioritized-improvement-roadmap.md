# Roadmap Priorizado de Melhorias do Frontend — NexTraceOne

> **Data:** 2026-03-26
> **Base:** Auditoria enterprise completa do frontend
> **Critérios de priorização:** Impacto na percepção enterprise · Risco de dívida visual · Frequência de uso · Persona afetada · Esforço estimado

---

## Visão Geral do Roadmap

```
FASE 1 — Corrigir (1-2 semanas)
  Inconsistências críticas e altas que comprometem design system e UX

FASE 2 — Consolidar (2-4 semanas)
  Design system hardening e padrões de componente

FASE 3 — Elevar (4-8 semanas)
  Páginas-chave redesenhadas e componentes estratégicos criados

FASE 4 — Premium (8-12 semanas)
  Refinamento visual, interações avançadas e elevação enterprise final
```

---

## FASE 1 — Correções Críticas e Altas

> **Objetivo:** Eliminar inconsistências que comprometem a credibilidade visual e a saúde do design system.
> **Impacto:** Imediato na percepção de consistência e profissionalismo.

---

### F1-01 — Corrigir bypass de design system em ServiceDetailPage

**Prioridade:** P0 — Crítica
**Ficheiro:** `src/features/catalog/pages/ServiceDetailPage.tsx:31-66`
**Esforço estimado:** 2-3 horas
**Persona afetada:** Engineer, Tech Lead, Architect

**O que fazer:**
Remover os 4 mapas de cores inline (`criticalityColors`, `lifecycleColors`, `protocolColors`, `contractLifecycleColors`) e substituir por chamadas ao componente `Badge` com as variantes semânticas mais próximas.

**Mapeamento recomendado:**
```tsx
// criticality
Critical → Badge variant="danger"
High     → Badge variant="warning"
Medium   → Badge variant="warning"
Low      → Badge variant="neutral"

// lifecycle
Active      → Badge variant="success"
Planning    → Badge variant="info"
Development → Badge variant="info"
Staging     → Badge variant="info"
Deprecating → Badge variant="warning"
Deprecated  → Badge variant="warning"
Retired     → Badge variant="neutral"
```

---

### F1-02 — Corrigir cores hardcoded em ExecutiveOverviewPage

**Prioridade:** P0 — Crítica
**Ficheiro:** `src/features/governance/pages/ExecutiveOverviewPage.tsx:122-269`
**Esforço estimado:** 1 hora
**Persona afetada:** Executive, Platform Admin

**O que fazer:**
```tsx
text-emerald-500 → text-success
text-orange-500  → text-warning
text-amber-500   → text-warning
bg-emerald-500   → bg-success
bg-amber-500     → bg-warning
bg-critical      → bg-danger (após consolidação de tokens)
```

---

### F1-03 — Corrigir cores hardcoded em IncidentDetailPage

**Prioridade:** P1 — Alta
**Ficheiro:** `src/features/operations/pages/IncidentDetailPage.tsx:388-389`
**Esforço estimado:** 30 minutos
**Persona afetada:** Engineer, SRE, Tech Lead

**O que fazer:**
```tsx
text-emerald-400 → text-success
text-amber-400   → text-warning
```

---

### F1-04 — Envolver ServiceDetailPage em PageContainer

**Prioridade:** P1 — Alta
**Ficheiro:** `src/features/catalog/pages/ServiceDetailPage.tsx:113`
**Esforço estimado:** 30 minutos
**Persona afetada:** Todos

**O que fazer:**
Substituir `<div className="p-6 lg:p-8 animate-fade-in">` por `<PageContainer>`.
Verificar que o conteúdo interno não fica com padding duplo.

---

### F1-05 — Migrar ExecutiveOverviewPage para TanStack Query

**Prioridade:** P1 — Alta
**Ficheiro:** `src/features/governance/pages/ExecutiveOverviewPage.tsx:50-62`
**Esforço estimado:** 1-2 horas
**Persona afetada:** Executive

**O que fazer:**
```tsx
// Remover:
const [data, setData] = useState(null);
const [loading, setLoading] = useState(true);
useEffect(() => { ... fetchData ... }, []);

// Substituir por:
const { data, isLoading, isError } = useQuery({
  queryKey: ['executive-overview'],
  queryFn: () => organizationGovernanceApi.getExecutiveOverview(),
  staleTime: 60_000,
});
```
Substituir loading state inline por `<PageLoadingState>`.

---

### F1-06 — Corrigir ícones duplicados na sidebar

**Prioridade:** P1 — Alta
**Ficheiro:** `src/components/shell/AppSidebar.tsx:57,64`
**Esforço estimado:** 30 minutos
**Persona afetada:** Todos (navegação diária)

**O que fazer:**
- `aiAgents`: substituir `<Bot>` por `<Network>` ou `<Cpu>` (Lucide)
- `automation`: substituir `<Zap>` por `<GitMerge>` ou `<Workflow>` (Lucide)

---

### F1-07 — Adicionar ESLint rule para prevenir cores Tailwind hardcoded

**Prioridade:** P1 — Alta (prevenção de regressão)
**Ficheiro:** `eslint.config.js`
**Esforço estimado:** 2-3 horas

**O que fazer:**
Adicionar regra usando `eslint-plugin-tailwindcss` ou regra custom que alerte quando classes como `text-red-*`, `text-emerald-*`, `text-orange-*`, `text-amber-*`, `bg-red-*`, `bg-emerald-*` são usadas, sugerindo os tokens equivalentes do design system.

---

## FASE 2 — Consolidação do Design System

> **Objetivo:** Hardening do design system — eliminar ambiguidades de tokens, enforçar tipografia, padronizar componentes base.

---

### F2-01 — Consolidar tokens duplicados em index.css

**Prioridade:** P2 — Média
**Ficheiro:** `src/index.css`
**Esforço estimado:** 3-4 horas (incluindo busca e substituição)

**Tokens a consolidar:**
- Remover `--color-panel` (manter `--color-card`)
- Renomear `--color-elevated` para consolidar com `--color-hover` (ou diferenciá-los claramente)
- Consolidar `--color-critical` e `--color-danger` em apenas `--color-danger`
- Consolidar sombras: remover aliases `shadow-surface`=`shadow-md`, `shadow-elevated`=`shadow-lg`, `shadow-floating`=`shadow-xl`

---

### F2-02 — Expandir Badge com prop `icon` e tamanho `sm`

**Prioridade:** P2 — Média
**Ficheiro:** `src/components/Badge.tsx`
**Esforço estimado:** 2 horas

**O que fazer:**
```tsx
interface BadgeProps {
  icon?: ReactNode;     // NOVO
  size?: 'sm' | 'md';  // NOVO
  // remover variante 'neutral' (manter apenas 'default')
}
```

---

### F2-03 — Expandir StatCard para InsightCard (clicável + aria)

**Prioridade:** P2 — Média
**Ficheiro:** `src/components/StatCard.tsx`
**Esforço estimado:** 3 horas

**O que fazer:**
- Substituir `↑`/`↓` por ícones Lucide `TrendingUp`/`TrendingDown`
- Adicionar prop `href?: string` para tornar o card clicável (usando `Link` quando presente)
- Adicionar prop `ariaLabel?: string` para acessibilidade do valor
- Adicionar prop `context?: string` para subtítulo contextual

---

### F2-04 — Expandir ContentGrid para suportar 5+ colunas

**Prioridade:** P2 — Média
**Ficheiro:** `src/components/shell/ContentGrid.tsx`
**Esforço estimado:** 1 hora

**O que fazer:**
Adicionar suporte a `columns={5}` para eliminar o uso de `!grid-cols-5` com Tailwind important.

---

### F2-05 — Enforçar uso de PageContainer em todas as páginas

**Prioridade:** P2 — Média
**Ficheiro:** Múltiplas páginas

**O que fazer:**
Auditar todas as páginas que não usam `PageContainer` como wrapper raiz. Além de `ServiceDetailPage`, verificar:
- `ServiceCatalogPage` (graph view pode ser full-width — usar `PageContainer fluid`)
- `DraftStudioPage`
- `ContractWorkspacePage`

---

### F2-06 — Integrar Breadcrumbs no AppContentFrame

**Prioridade:** P2 — Média
**Ficheiro:** `src/components/shell/AppContentFrame.tsx`
**Esforço estimado:** 3-4 horas

**O que fazer:**
Configurar `Breadcrumbs.tsx` para leitura automática de rota via React Router `useMatches()` com handles de breadcrumb definidos por rota no `App.tsx`.

---

### F2-07 — Adicionar scope="col" nas tabelas e aria-hidden em ícones decorativos

**Prioridade:** P2 — Média (acessibilidade)
**Ficheiros:** `ServiceDetailPage.tsx`, `IncidentDetailPage.tsx`, tabelas de catálogo
**Esforço estimado:** 2 horas

---

## FASE 3 — Reestruturação de Páginas-Chave

> **Objetivo:** Redesenhar as páginas mais críticas para cada persona com padrões enterprise consolidados.

---

### F3-01 — Criar componente EntityHeader

**Prioridade:** P2 — Alta
**Ficheiro:** `src/components/EntityHeader.tsx` (novo)
**Esforço estimado:** 4-6 horas

**O que fazer:**
Criar componente com props: `name`, `entityType`, `status`, `criticality`, `owner`, `badges[]`, `actions`, `icon`.
Aplicar em: `ServiceDetailPage`, `IncidentDetailPage`, `TeamDetailPage`.

---

### F3-02 — Criar componente TimelinePanel

**Prioridade:** P2 — Alta
**Ficheiro:** `src/components/TimelinePanel.tsx` (novo)
**Esforço estimado:** 4-5 horas

**O que fazer:**
Extrair a implementação inline de `IncidentDetailPage.tsx:212-228` para componente reutilizável.
Suporte a tipos de entrada (event, change, incident, approval) com ícones e cores semânticas.

---

### F3-03 — Redesenhar ServiceDetailPage com EntityDetailLayout

**Prioridade:** P3 — Alta
**Ficheiro:** `src/features/catalog/pages/ServiceDetailPage.tsx`
**Esforço estimado:** 8-12 horas
**Depende de:** F3-01 (EntityHeader)

**O que fazer:**
1. Adicionar EntityHeader no topo
2. Converter conteúdo para Tabs: Overview · APIs · Contracts · Changes · Incidents
3. Mover Ownership + Classification para sidebar direita compacta
4. Usar TableWrapper nas tabelas de APIs e contratos
5. Adicionar tab "Changes" com TimelinePanel real

---

### F3-04 — Redesenhar ExecutiveOverviewPage com charts e hierarquia

**Prioridade:** P3 — Alta
**Ficheiro:** `src/features/governance/pages/ExecutiveOverviewPage.tsx`
**Esforço estimado:** 12-16 horas
**Depende de:** F1-05 (useQuery), F1-02 (cores)

**O que fazer:**
1. Adicionar secção hero com 3 KPIs críticos em destaque (maior tipografia)
2. Adicionar painel "Ação Imediata" com alertas ativos e links diretos
3. Adicionar gráficos ECharts: tendência de incidentes, tendência de risco
4. Reorganizar maturidade com gráfico de radar ou barras
5. Hierarquizar visualmente por urgência (não empilhamento linear)

---

### F3-05 — Criar DiffViewer para contratos e mudanças

**Prioridade:** P3 — Média
**Ficheiro:** `src/components/DiffViewer.tsx` (novo)
**Esforço estimado:** 8-10 horas

**O que fazer:**
Componente de diff lado a lado (before/after) para JSON, YAML e texto.
Aplicar em: `ContractWorkspacePage` (comparação de versões), `PromotionPage` (non-prod vs prod), `ChangeDetailPage`.

---

### F3-06 — Implementar SplitView nas páginas de lista

**Prioridade:** P3 — Média
**Ficheiro:** `src/components/shell/SplitView.tsx` (novo)
**Esforço estimado:** 6-8 horas

**O que fazer:**
Criar componente `SplitView` e aplicar em:
- `ServiceCatalogListPage`: lista + `ServiceDetailPanel` lateral
- `IncidentsPage`: lista + `IncidentDetailPanel` lateral

---

## FASE 4 — Elevação Premium Enterprise

> **Objetivo:** Refinamento final para posicionar o NexTraceOne como plataforma enterprise premium.

---

### F4-01 — Contadores de alerta na sidebar

**Prioridade:** P4 — Média
**Esforço estimado:** 4-6 horas

Adicionar badge numérico nos itens de navegação relevantes (incidents, changes, risk, compliance) com dados em tempo real via React Query.

---

### F4-02 — Ajuste de saturação da paleta (mint/cyan)

**Prioridade:** P4 — Baixa
**Esforço estimado:** 4 horas + QA visual

Reduzir levemente a saturação de `--color-success` e rever uso de glow shadows para ambientes onde a plataforma será usada prolongadamente.

---

### F4-03 — Widgets de dashboard específicos por persona

**Prioridade:** P4 — Média
**Esforço estimado:** 12-16 horas

Substituir `HomeWidgetCard` genérico por widgets específicos:
- `ServiceHealthWidget`
- `RecentChangesWidget`
- `ActiveIncidentsWidget`
- `GovernanceScoreWidget`
- `RiskSummaryWidget`

---

### F4-04 — Command Palette enriquecida

**Prioridade:** P4 — Baixa
**Esforço estimado:** 4-6 horas

Adicionar categorias de ação rápida e histórico de navegação recente na CommandPalette.

---

### F4-05 — Dividir App.tsx em route groups lazy-loaded

**Prioridade:** P4 — Baixa (performance + maintainability)
**Esforço estimado:** 4-6 horas

Dividir os 101 routes do `App.tsx` (46KB) em chunks lazy-loaded por módulo para melhorar o bundle split e manutenibilidade.

---

### F4-06 — Sidebar com agrupamento agressivo por persona

**Prioridade:** P4 — Média
**Esforço estimado:** 6-8 horas

Implementar sidebar filtrada mais agressivamente por persona:
- Executive: máximo 6 itens
- Auditor: máximo 8 itens
- Engineer: máximo 15 itens (os mais relevantes)

---

## Sumário do Roadmap

| Fase | Itens | Esforço total estimado | Impacto |
|------|-------|----------------------|---------|
| Fase 1 — Corrigir | 7 itens | 8-12 horas | Eliminação de inconsistências críticas |
| Fase 2 — Consolidar | 7 itens | 20-25 horas | Design system sólido e enforçado |
| Fase 3 — Elevar | 6 itens | 42-57 horas | Páginas-chave redesenhadas |
| Fase 4 — Premium | 6 itens | 34-46 horas | Elevação visual final |
| **Total** | **26 itens** | **104-140 horas** | |

---

## Dependências Entre Fases

```
F1-01 (ServiceDetail cores)
  └── F3-03 (ServiceDetail redesign) ← depende de F3-01 (EntityHeader)

F1-05 (ExecutiveOverview useQuery)
  └── F3-04 (ExecutiveOverview redesign)

F1-02 (ExecutiveOverview cores)
  └── F3-04 (ExecutiveOverview redesign)

F3-01 (EntityHeader)
  └── F3-03, F3-06 (ServiceDetail, SplitView)

F2-04 (ContentGrid 5 cols)
  └── elimina F1-07 dependency

ESLint rule (F1-07)
  ← deve ser implementada ANTES de F3 para prevenir regressão durante redesign
```

---

## Critérios de Done para Cada Fase

### Fase 1 ✓ quando:
- Nenhuma cor Tailwind hardcoded (`text-red-*`, `text-emerald-*`, `text-orange-*`, etc.) fora do design system
- Todas as páginas de detalhe usam `PageContainer`
- ExecutiveOverviewPage usa `useQuery`
- Ícones da sidebar são únicos por item

### Fase 2 ✓ quando:
- Design system tokens sem duplicatas
- `Badge` suporta `icon` e `size`
- `StatCard` clicável com ícones corretos
- Breadcrumbs visíveis em todas as rotas de detalhe
- ESLint rule ativa e sem exceções

### Fase 3 ✓ quando:
- `EntityHeader` usado em todas as páginas de detalhe
- `TimelinePanel` usado em `IncidentDetailPage` e `ChangeDetailPage`
- `ServiceDetailPage` com tabs e `PageContainer`
- `ExecutiveOverviewPage` com charts ECharts e hierarquia visual
- `DiffViewer` disponível para contratos

### Fase 4 ✓ quando:
- Sidebar mostra contadores de alerta ativos
- Paleta revisada aprovada por design review
- Widgets de dashboard específicos por persona
- App.tsx dividido em lazy-loaded chunks
