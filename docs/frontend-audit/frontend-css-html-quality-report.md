# Relatório de Qualidade CSS e HTML — NexTraceOne Frontend

> **Data:** 2026-03-26
> **Foco:** Semântica HTML · Qualidade de JSX · CSS/Tailwind · Padrões de espaçamento · Acessibilidade estrutural · Sustentabilidade

---

## 1. Objetivo

Avaliar a qualidade técnica do CSS e HTML/JSX produzidos — com foco em semântica, legibilidade, manutenibilidade, acoplamento de estilos e sustentabilidade da base de código visual.

---

## 2. HTML Semântico

### 2.1 Bom

**Modal com `<dialog>` nativo (`src/components/Modal.tsx`)**
A utilização do elemento `<dialog>` nativo é uma escolha semântica excelente. Fornece comportamento de dialogo acessível nativamente, sem precisar de bibliotecas externas.

**`<nav>` com `aria-label` na sidebar (`AppSidebar.tsx:148-150`)**
```tsx
<div role="navigation" aria-label={t('shell.sidebarNav')}>
  <nav aria-label={t('shell.mainNavigation')}>
```
Dupla camada de semântica de navegação, sendo a segunda mais específica.

**`:focus-visible` global (`index.css:213-218`)**
```css
:focus-visible {
  outline: 2px solid var(--color-accent);
  outline-offset: 2px;
}
```
Implementação correta de foco acessível sem impactar navegação por rato.

**`@media (prefers-reduced-motion)` (`index.css:355-365`)**
Todas as animações são desabilitadas em contextos de motion reduzido. Excelente aderência a acessibilidade de movimento.

---

### 2.2 Problemas

**Tabelas sem semântica de acessibilidade (`ServiceDetailPage.tsx:201-254`)**

```tsx
// PROBLEMA: Tabela sem caption, sem scope nos headers
<table className="w-full text-sm">
  <thead className="sticky top-0 z-10 bg-panel">
    <tr className="border-b border-edge text-left">
      <th className="px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
        {t('catalog.columns.name')}
      </th>
```

Ausências:
- `<caption>` descrevendo o propósito da tabela
- `scope="col"` nos `<th>` de cabeçalho
- `id/headers` para associação em tabelas complexas

**StatCard sem semântica de KPI (`StatCard.tsx`)**

```tsx
// PROBLEMA: Valor numérico sem contexto para leitores de ecrã
<p className="text-2xl font-bold text-heading tabular-nums">{value}</p>
```

O valor `42` sem contexto é ininteligível para leitores de ecrã. Deveria incluir `aria-label="42 incidentes abertos"` ou estar dentro de um `<dl>/<dt>/<dd>` semântico.

**Back-link sem `aria-label` descritivo (`ServiceDetailPage.tsx:115-121`)**

```tsx
<Link to="/services" className="inline-flex items-center gap-1 text-sm text-muted hover:text-accent transition-colors mb-4">
  <ArrowLeft size={14} />
  {t('common.back')}
```

"Back" (ou "Voltar") não é suficientemente descritivo para leitores de ecrã. Deveria ser "Voltar ao Catálogo de Serviços".

**Ícones decorativos sem `aria-hidden` (`ServiceDetailPage.tsx`, `IncidentDetailPage.tsx`)**

Ícones Lucide usados decorativamente (não como único indicador de ação) deveriam ter `aria-hidden="true"` para não serem lidos desnecessariamente. Exemplo:
```tsx
<Server size={16} className="text-cyan" />  // decorativo — precisa aria-hidden="true"
```

---

## 3. Qualidade do JSX

### 3.1 Bom

**Componentes pequenos e bem nomeados**
A divisão em `AppSidebarHeader`, `AppSidebarFooter`, `AppSidebarGroup`, `AppSidebarItem` é um exemplo de decomposição adequada.

**Uso de `cn()` para composição de classes**
O utilitário `cn` (clsx + tailwind-merge) é usado consistentemente para composição condicional de classes, evitando conflitos de classes Tailwind.

**`data-testid` para E2E (`AppShell.tsx:57,79`)**
```tsx
data-testid="app-shell"
data-testid="app-shell-main"
```
Atributos de teste separados de lógica de estilo — prática correta.

---

### 3.2 Problemas

**Inline `<style>` no AppShell (`AppShell.tsx:81-85`)**

```tsx
<style>{`
  @media (min-width: 1024px) {
    [data-testid="app-shell-main"] { margin-left: var(--shell-sidebar-w); }
  }
`}</style>
```

Injeção de CSS via string dentro de JSX é tecnicamente funcional mas:
- Cria side-effects difíceis de rastrear
- Faz o CSS depender de `data-testid` (atributo de teste) — acoplamento incorreto
- Não é idiomático com Tailwind 4

**Solução alternativa:** Usar CSS Grid no shell (`grid-template-columns: var(--shell-sidebar-w) 1fr`) ou Tailwind variant `lg:ml-[var(--shell-sidebar-w)]`.

**4 mapas de cor locais em ServiceDetailPage (`ServiceDetailPage.tsx:31-66`)**

Já documentado em IC-001. Do ponto de vista de qualidade de código, criar 4 objetos Record de mapeamento de estilos com 4-7 entradas cada é um antipadrão quando existe um sistema de Badge com variantes semânticas. Aumenta a superfície de manutenção e torna o diff de mudanças maior.

**Condições de feature flag inline repetidas (`DashboardPage.tsx`)**

```tsx
if (isRouteAvailableInFinalProductionScope('/contracts')) { ... }
if (isRouteAvailableInFinalProductionScope('/operations/incidents')) { ... }
```

Estas guards estão espalhadas pelo JSX do componente em múltiplos locais. O `<ReleaseScopeGate>` component existe para encapsular este padrão — deveria ser usado em vez de condicionais inline.

**`key={idx}` em listas dinâmicas (`DashboardPage.tsx:225`)**

```tsx
{attentionAlerts.map((alert, idx) => (
  <Link key={idx} ...>
```

Usar o índice do array como `key` é um antipadrão React quando os items podem ser reordenados ou filtrados. Cada alert deveria ter um ID estável.

---

## 4. Qualidade do CSS e Tailwind

### 4.1 Bom

**Sistema `@theme` no Tailwind 4**

A integração de `@theme` em `index.css` com variáveis CSS permite que os tokens sejam utilizáveis tanto em Tailwind (via `text-accent`, `bg-canvas`) quanto em CSS inline (via `var(--color-accent)`). Esta é a abordagem correta com Tailwind 4.

**Classes compostas em `foundations.ts`**

```ts
export const focusRingClass = 'focus-visible:outline-none focus-visible:ring-2 ...';
export const surfaceClass = { panel: 'bg-panel border border-edge rounded-lg shadow-surface', ... };
```

Centralizar strings de classes repetidas previne drift e facilita refactoring. Boa prática.

**`tailwind-merge` via `cn()`**

Previne classes conflitantes ao aplicar composição (ex: `p-4` + `p-6` resulta em `p-6` e não em ambas). Essencial para componentes extensíveis.

---

### 4.2 Problemas

**Bypass de tokens com cores Tailwind hardcoded**

Já extensamente documentado nos relatórios de inconsistências. A causa raiz: não existe enforcement (ESLint rule) que impeça `text-emerald-500` quando `text-success` existe.

**Tailwind `!important` override (`DashboardPage.tsx:215`)**

```tsx
<ContentGrid className="!grid-cols-2 lg:!grid-cols-5">
```

O prefixo `!` do Tailwind força `!important` no CSS gerado. Indica que o componente `ContentGrid` não suporta a configuração necessária. A solução é expandir `ContentGrid` para suportar `columns={5}`.

**Classes de tipografia Tailwind vs `.type-*` do design system**

O design system define classes `.type-heading-01` a `.type-overline` em CSS. Na prática, o código usa `text-2xl font-bold`, `text-sm font-semibold`, etc. diretamente.

Consequência: a escala tipográfica formal nunca é usada nos componentes — existe apenas no papel.

Exemplo de inconsistência de título:
```tsx
// DashboardPage.tsx:188
<h1 className="text-2xl font-bold text-heading">  // → 24px/700

// ServiceDetailPage.tsx:127
<h1 className="text-2xl font-bold text-heading">  // → 24px/700

// ExecutiveOverviewPage.tsx: título via PageHeader component
// (PageHeader não auditado — provavelmente usa text-xl ou text-2xl)
```

Embora os valores sejam coincidentemente iguais aqui, a falta de uso da classe `.type-heading-01` significa que uma mudança na escala precisaria ser aplicada manualmente em todos os locais.

**Falta de breakpoints padronizados para grids de detalhe**

`ServiceDetailPage.tsx:148`: `grid-cols-1 lg:grid-cols-3`
`IncidentDetailPage.tsx:202`: `grid-cols-1 lg:grid-cols-2`
`ExecutiveOverviewPage.tsx:144`: `grid-cols-2 md:grid-cols-4`
`ExecutiveOverviewPage.tsx:213`: `grid-cols-2 md:grid-cols-4`

Cada página define os seus próprios breakpoints de grid. Sem um padrão de `DetailLayout` ou `SplitLayout`, estes valores divergem conforme o produto cresce.

---

## 5. Padrões de Espaçamento

### Estado Geral: Bom mas com desvios pontuais

O espaçamento é majoritariamente consistente via `PageContainer` e classes Tailwind `gap-*`, `space-y-*`, `mb-*`. O problema é que `ServiceDetailPage.tsx` não usa `PageContainer`, gerando padding divergente.

**Vertical rhythm no ExecutiveOverviewPage:**

```tsx
<Card className="mb-6">...</Card>  // repetido 5 vezes
```

`mb-6` (24px) uniforme entre todos os cards não cria ritmo visual — todos os blocos ficam equidistantes sem hierarquia. Blocos mais importantes deveriam ter mais breathing space.

**Inconsistência de `gap` em grids do dashboard:**

```tsx
// DashboardPage.tsx
<div className="grid grid-cols-1 lg:grid-cols-2 gap-5 mb-6">  // gap-5 (20px)

// ExecutiveOverviewPage.tsx
<div className="grid grid-cols-1 md:grid-cols-3 gap-4">         // gap-4 (16px)
<div className="grid grid-cols-2 md:grid-cols-4 gap-4">         // gap-4 (16px)
```

`gap-4` vs `gap-5` em componentes equivalentes — pequena inconsistência mas evitável com `ContentGrid`.

---

## 6. Estrutura de Ficheiros e Organização

### Bom

- Feature modules bem isolados: `src/features/<module>/`
- Separação clara: `api/`, `components/`, `pages/`, `types/`, `hooks/`
- Componentes de shell separados: `src/components/shell/`
- Design system em pasta própria: `src/shared/design-system/`

### Atenção

**`App.tsx` com 46KB de conteúdo**

O ficheiro de routing principal tem 46KB. Com 101 rotas, este ficheiro crescerá indefinidamente se não for dividido em lazy-loaded route groups por módulo.

**`src/features/contracts/` com múltiplas sub-pastas complexas**

```
contracts/
├── api/
├── canonical/
├── catalog/
├── create/
├── governance/
├── hooks/
├── portal/
├── shared/
├── spectral/
├── studio/
├── types/
└── workspace/
    └── sections/ (12+ sections)
    └── builders/ (4 builders)
```

Esta estrutura é a mais complexa do produto. A profundidade e quantidade de sub-módulos pode dificultar onboarding de novos contribuidores.

---

## 7. Acessibilidade Estrutural — Sumário

| Item | Estado |
|------|--------|
| `:focus-visible` global | Implementado |
| `@media (prefers-reduced-motion)` | Implementado |
| `<dialog>` em Modal | Implementado |
| `role="navigation"` + `aria-label` na sidebar | Implementado |
| `scope="col"` em `<th>` de tabelas | Ausente |
| `aria-hidden="true"` em ícones decorativos | Inconsistente |
| `aria-label` descritivo em links de back/navegação | Fraco |
| `aria-live` em loading states | Não verificado |
| `aria-label` com contagem em badges de notificação | Não verificado |
| KPI values com contexto acessível | Ausente (StatCard) |

---

## 8. Recomendações Técnicas

| Ação | Prioridade | Impacto |
|------|------------|---------|
| Adicionar `scope="col"` em todos os `<th>` de tabelas | Alta | Acessibilidade |
| Adicionar `aria-hidden="true"` em ícones decorativos | Média | Acessibilidade |
| Substituir `key={idx}` por IDs estáveis em listas dinâmicas | Média | Estabilidade React |
| Remover inline `<style>` do AppShell; usar CSS Grid ou variante Tailwind | Baixa | Maintainability |
| Adicionar ESLint rule para prevenir cores Tailwind hardcoded | Alta | Design system |
| Dividir App.tsx em route groups lazy-loaded por módulo | Média | Performance e maintainability |
| Expandir `ContentGrid` para suportar 5 colunas (eliminar `!grid-cols-5`) | Baixa | CSS quality |
| Criar `aria-label` contextual no StatCard | Alta | Acessibilidade |
| Promover uso de `.type-*` classes ou garantir que `Typography.tsx` seja o único ponto de definição de texto | Média | Design system |
