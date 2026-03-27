# Recomendações de Redesign — NexTraceOne Frontend

> **Data:** 2026-03-26
> **Foco:** Melhorias concretas de cores, layout, componentes, tipografia, navegação e dashboards para elevar o NexTraceOne a padrão enterprise premium

---

## 1. Cores

### 1.1 Problema Central

A paleta atual é tecnicamente correta na sua definição (`index.css @theme`) mas tem dois riscos de percepção:

1. `--color-mint: #1EF2C1` e `--color-cyan: #18CFF2` são muito saturados — em uso denso, tendem ao "neon/sci-fi"
2. Tokens duplicados (`critical`≈`danger`, `card`=`panel`, `elevated`=`hover`) geram ruído de manutenção

### 1.2 Ajustes Recomendados

**Ajustar saturação do mint (success) para ambiente enterprise:**

```css
/* Atual — muito saturado para uso extenso */
--color-success: #1EF2C1;  /* saturação alta, cyan-green neon */

/* Recomendado — mais sóbrio, ainda claramente verde */
--color-success: #2DD4A0;  /* green-teal mais suave */
--color-success-muted: rgba(45, 212, 160, 0.12);
```

**Consolidar tokens duplicados:**

```css
/* Remover um de cada par duplicado */
/* --color-panel e --color-card → manter apenas --color-card */
/* --color-hover e --color-elevated → manter apenas --color-elevated */
/* --color-critical e --color-danger → manter apenas --color-danger */

/* Renomear para clareza */
--color-danger: #FF5C6B;    /* um único vermelho enterprise */
--color-danger-muted: rgba(255, 92, 107, 0.12);
```

**Clarificar semântica de accent vs cyan:**

```css
/* Manter apenas um acento primário */
--color-accent: #2BB7E3;          /* CTA, links, foco, primário interativo */
--color-accent-subtle: #18CFF2;   /* dados, gráficos, destaque leve */
/* Remover --color-cyan (renomear para --color-accent-subtle) */
```

### 1.3 Cores de Dados para ECharts

Definir explicitamente a paleta de visualização de dados separada dos tokens semânticos:

```css
/* Data visualization palette (para ECharts) */
--color-data-blue:   #2BB7E3;
--color-data-teal:   #2DD4A0;
--color-data-violet: #7C6FFF;
--color-data-amber:  #F5C062;
--color-data-coral:  #FF7A86;
--color-data-slate:  #8EA0B7;
```

### 1.4 Gradientes — Disciplina de Uso

```css
/* Reservar brand-gradient APENAS para: */
/* - Faixa de topo da sidebar (brand indicator) */
/* - Elemento hero de marketing interno */
/* - CTA button primário */

/* NÃO usar brand-gradient em: */
/* - Cards de KPI */
/* - Backgrounds de seção */
/* - Badges */
/* - Ícones decorativos */
```

---

## 2. Layout

### 2.1 EntityDetailLayout — Template Padronizado

Criar `src/components/shell/EntityDetailLayout.tsx` como template para todas as páginas de detalhe de entidade:

```
┌─────────────────────────────────────────────────────────┐
│ EntityHeader                                            │
│  ├── [ícone] Nome da Entidade                           │
│  ├── Tipo Badge · Status Badge · Criticidade Badge      │
│  ├── Owner: Team Name                                   │
│  └── [Ações principais: Edit, Archive, View in SOT...] │
├─────────────────────────────────────────────────────────┤
│ ContextRibbon (optional)                                │
│  ├── Ambiente: production                               │
│  ├── Última modificação: 2h ago                         │
│  └── Contexto adicional relevante                       │
├─────────────────────────────────────────────────────────┤
│ [Tab: Overview] [Tab: Dependencies] [Tab: Changes]      │
│ [Tab: Incidents] [Tab: Contracts] [Tab: Timeline]       │
├─────────────────────────────────────────────────────────┤
│ Conteúdo da tab ativa                                   │
│                                                         │
├─────────────────────────────────────────────────────────┤
│ AssistantPanel (collapsível)                            │
└─────────────────────────────────────────────────────────┘
```

**Páginas que devem adotar este template:**
- `ServiceDetailPage`
- `IncidentDetailPage`
- `TeamDetailPage`
- `DomainDetailPage`
- `AgentDetailPage`
- `ContractDetailPage`
- `GovernancePackDetailPage`

### 2.2 App Shell — Breadcrumbs Integrados

Integrar `Breadcrumbs` no `AppContentFrame` para exibição automática baseada na rota ativa:

```
AppTopbar
AppContentFrame
  ├── [Breadcrumbs automáticos via route config]
  └── <Outlet />
```

### 2.3 Lista + Detalhe (Split View)

Para listas de entidades com detalhe frequente (ServiceCatalog, Incidents, Contracts):

```
┌──────────────────────┬────────────────────────────────┐
│ Filter Bar           │ Detail Panel (slide in)        │
├──────────────────────┤                                │
│ List Item 1 [active] │  EntityHeader rápido           │
│ List Item 2          │  Campos chave                  │
│ List Item 3          │  Ações principais              │
│ List Item 4          │  Link → página completa        │
└──────────────────────┴────────────────────────────────┘
```

### 2.4 Dashboard Executivo — Estrutura Proposta

```
┌─────────────────────────────────────────────────────────┐
│ HERO: 3 KPIs Críticos (grande, com tendência)           │
│  Risco Geral: HIGH ↑  |  Incidentes: 4 ↑  |  Conf: ↓  │
├─────────────────────────────────────────────────────────┤
│ AÇÃO IMEDIATA: "O que exige atenção agora"              │
│  [!] 2 domínios críticos    [!] 4 incidentes abertos   │
│  [!] 3 mudanças de alto risco                           │
├────────────────────┬────────────────────────────────────┤
│ Tendência de Risco │ Tendência de Incidentes            │
│ [Gráfico de linha] │ [Gráfico de barras]                │
├────────────────────┴────────────────────────────────────┤
│ Maturidade por Domínio (progress bars + sparklines)     │
├─────────────────────────────────────────────────────────┤
│ Top Domínios com Risco · Focos de Atenção               │
└─────────────────────────────────────────────────────────┘
```

---

## 3. Componentes

### 3.1 Componentes a Criar (por prioridade)

#### EntityHeader (Alta)

```tsx
interface EntityHeaderProps {
  name: string;
  entityType: string;
  status: SemanticStatus;
  criticality?: 'Critical' | 'High' | 'Medium' | 'Low';
  owner?: string;
  badges?: Array<{ label: string; variant: BadgeVariant }>;
  actions?: ReactNode;
  icon?: ReactNode;
}
```

Substituir os cabeçalhos manuais de `ServiceDetailPage`, `IncidentDetailPage`, `TeamDetailPage`.

#### TimelinePanel (Alta)

```tsx
interface TimelinePanelProps {
  entries: Array<{
    timestamp: string;
    description: string;
    type?: 'event' | 'change' | 'incident' | 'approval';
    actor?: string;
  }>;
  maxVisible?: number;
  showExpandButton?: boolean;
}
```

Substituir a timeline inline de `IncidentDetailPage.tsx:212-228`.

#### OwnershipPanel (Média)

```tsx
interface OwnershipPanelProps {
  team: string;
  technicalOwner?: string;
  businessOwner?: string;
  criticality?: string;
  dependencies?: string[];
}
```

#### RiskBadge / SeverityBadge (Média)

Especialização do `Badge` com semântica visual mais forte para risco e severidade — inclui ícone semanticamente correto por nível:

```tsx
// Critical → ShieldAlert icon + danger variant
// High → AlertTriangle icon + warning variant
// Medium → AlertCircle icon + warning variant
// Low → CheckCircle icon + success variant
```

#### DiffViewer (Média)

Para comparação de contratos, versões, configurações:
```tsx
interface DiffViewerProps {
  before: string;
  after: string;
  language?: 'json' | 'yaml' | 'text';
  title?: { before: string; after: string };
}
```

#### SplitView (Média)

```tsx
interface SplitViewProps {
  list: ReactNode;
  detail: ReactNode | null;
  detailWidth?: number;  // px, default 480
  onClose?: () => void;
}
```

### 3.2 Componentes a Refatorar

**StatCard → InsightCard**

```tsx
interface InsightCardProps {
  title: string;
  value: string | number;
  icon: ReactNode;
  color?: string;
  trend?: { direction: 'up' | 'down' | 'stable'; label: string };
  href?: string;        // NOVO: torna o card clicável
  context?: string;     // NOVO: subtítulo contextual
  ariaLabel?: string;   // NOVO: para acessibilidade
}
```

**Badge → adicionar prop `icon`**

```tsx
interface BadgeProps {
  icon?: ReactNode;   // NOVO: ícone integrado no badge
  size?: 'sm' | 'md'; // NOVO: tamanho
}
```

**ContentGrid → suportar 5 colunas**

```tsx
// Adicionar columns={5} ao ContentGrid para eliminar !grid-cols-5
```

**PageHeader → EntityHeader (expandido)**

```tsx
// PageHeader é o candidato mais próximo para se tornar EntityHeader
// Expandir com: actions prop, badges, owner, criticality
```

### 3.3 Componentes a Remover / Consolidar

| Componente | Ação | Razão |
|-----------|------|-------|
| `criticalityColors` map em ServiceDetailPage | Remover | Substituído por Badge |
| `lifecycleColors` map em ServiceDetailPage | Remover | Substituído por Badge |
| `protocolColors` map em ServiceDetailPage | Remover | Substituído por Badge |
| `contractLifecycleColors` map em ServiceDetailPage | Remover | Substituído por Badge |
| Badge variante `neutral` | Consolidar com `default` | Idênticos |
| `--color-panel` token | Consolidar com `--color-card` | Idênticos |

---

## 4. Tipografia

### 4.1 Escala Recomendada de Aplicação

Criar mapeamento explícito entre contextos de uso e classes:

| Contexto | Classe | Resultado |
|----------|--------|-----------|
| Título principal de página (`<h1>`) | `.type-heading-01` | 2rem / 700 |
| Título de seção / card | `.type-title-01` | 1.25rem / 600 |
| Subtítulo de seção | `.type-title-02` | 1.125rem / 600 |
| Label de campo / coluna de tabela | `.type-overline` | 0.6875rem / 600 / UPPERCASE |
| Corpo de texto principal | `.type-body-md` | 1rem / 400 |
| Corpo de texto secundário / descrição | `.type-body-sm` | 0.875rem / 400 |
| Metadados / timestamps / IDs | `.type-caption` ou `.type-mono-sm` | 0.75rem |
| KPI value | `text-3xl font-bold tabular-nums` | (sem classe type — é display) |

### 4.2 Densidade Tipográfica

Para interfaces operacionais densas (tabelas, listas de incidentes):
- Usar `type-body-sm` (0.875rem) como padrão de linha de tabela
- Usar `type-caption` (0.75rem) para metadados de linha
- Não usar tamanhos abaixo de 0.6875rem (11px) — compromete legibilidade em monitores de 1080p

### 4.3 Enforcement

Adicionar `Typography.tsx` como wrapper obrigatório para elementos de texto semântico (`<h1>` a `<h6>`), garantindo que a escala seja aplicada consistentemente.

---

## 5. Navegação

### 5.1 Agrupamento da Sidebar por Persona

**Engineer view:**
```
📊 Dashboard
🔧 Services
    Service Catalog
    Dependency Graph
📋 Contracts
    My Contracts
🚀 Changes
    Change Intelligence
    My Releases
🚨 Operations
    Incidents
    Runbooks
```

**Executive view:**
```
📊 Overview
    Command Center
    Executive Dashboard
📈 Governance
    Executive Overview
    FinOps
    Risk Center
    Reports
```

**Auditor view:**
```
📊 Dashboard
🔍 Compliance
    Compliance
    Evidence Packages
    Audit Log
    Policies
    Waivers
📋 Controls
    Enterprise Controls
    Governance Packs
```

### 5.2 Nomes de Módulos — Clareza

| Atual | Proposto | Razão |
|-------|----------|-------|
| "Change Intelligence" | "Change Confidence" | Alinha com a terminologia do produto |
| "Source of Truth Explorer" | "Knowledge Explorer" | Mais claro para personas não-técnicas |
| "Spectral Rulesets" | "Validation Rules" | Semântica business-first |
| "Canonical Entities" | "Canonical Models" | Mais intuitivo |
| "Break Glass" | "Emergency Access" | Linguagem enterprise-friendly |
| "JIT Access" | "Temporary Access" | Sem sigla técnica |

### 5.3 Contadores de Alerta na Sidebar

Adicionar suporte a badge numérico em itens da sidebar para:
- Incidents: contagem de incidentes abertos
- Changes: contagem de mudanças pendentes de revisão
- Governance Risk: contagem de riscos críticos
- Governance Compliance: contagem de controlos não conformes

### 5.4 Command Palette — Enriquecer

Adicionar categorias de ação rápida:
- "Criar novo incidente"
- "Criar novo serviço"
- "Ver mudanças pendentes"
- "Abrir executive overview"
- Histórico de navegação recente
- Pesquisa de serviço por nome

---

## 6. Dashboards

### 6.1 Princípio Central

Substituir a abordagem "grid de contadores" por "narrativa contextual de operação":

**De:** "Mudanças: Pendentes 3 / Aprovadas 12 / Rejeitadas 1"

**Para:**
> "3 mudanças aguardam revisão — 1 com risco alto. [Ver mudanças pendentes →]"
> "Confiança de release: 89% nas últimas 48h [↓ 4% vs ontem]"

### 6.2 Dashboard do Engineer — Melhorias

1. StatCards clicáveis com roteamento direto
2. "My Services" — serviços do utilizador com estado de saúde inline
3. "Recent Activity" — timeline de mudanças e incidentes relevantes para os serviços do utilizador
4. AlertBar mais prominente quando há incidentes ativos nos seus serviços

### 6.3 Dashboard Executivo — Melhorias

Ver proposta de estrutura na seção 2.4 deste relatório.

**Charts recomendados (com ECharts):**
- Tendência de incidentes: gráfico de área (últimos 30 dias)
- Tendência de risco: gráfico de linha por domínio
- Change confidence: gráfico de barras (últimas 10 releases)
- Maturidade: gráfico de radar por domínio
- FinOps: gráfico de donut (custo por domínio/serviço)

### 6.4 Eliminar Widgets Genéricos

`HomeWidgetCard.tsx` atual é um widget genérico sem dados reais inline. Deve ser substituído por widgets específicos por tipo:

- `ServiceHealthWidget` — serviços com status de saúde
- `RecentChangesWidget` — últimas mudanças com confidence score
- `ActiveIncidentsWidget` — incidentes ativos com severidade
- `GovernanceScoreWidget` — score de maturidade/compliance com tendência
- `RiskSummaryWidget` — top 3 riscos com drill-down
