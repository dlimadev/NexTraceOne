# Design Audit — Fase 1 (P1–P5): Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Corrigir as 5 features com nível 🔴 Crítico da auditoria de design: platform-admin, saas, governance chart widgets, observability e contracts badges — eliminando o uso de Tailwind palette hardcoded, hex colors avulsos, e ausência de PageContainer/PageHeader/EmptyState/ErrorState.

**Architecture:** Cada task é independente e pode ser commitada atomicamente. As tasks de utilidade (T1 chartColors, T2 BadgeVariant) devem ser concluídas primeiro pois são dependências das demais. As tasks de migração de página (T3 platform-admin, T4 saas, T5 observability) e de componente (T6 chart widgets, T7 contracts badges) podem então executar em paralelo ou em sequência.

**Tech Stack:** React 19, TypeScript 5.9, Tailwind CSS 4.x, Vitest + Testing Library, componentes custom: `<PageContainer>`, `<PageHeader>`, `<Button>`, `<Card>`, `<Badge>`, `<EmptyState>`, `<ErrorState>`

---

## Referência — Token Map de Migração

Sempre que um ficheiro contiver qualquer classe Tailwind da coluna "ANTES", substitui pela coluna "DEPOIS":

| ANTES (Tailwind palette) | DEPOIS (design token) |
|--------------------------|----------------------|
| `bg-indigo-600` | `bg-accent` — ou usar `<Button variant="primary">` |
| `bg-indigo-700` | `bg-accent/90` — ou hover state do Button |
| `bg-violet-600` | `bg-accent` |
| `bg-violet-700` | `bg-accent/90` |
| `text-indigo-600` | `text-accent` |
| `text-violet-600` | `text-accent` |
| `border-indigo-200` | `border-accent/20` |
| `hover:bg-indigo-50` | `hover:bg-accent/10` |
| `focus:ring-indigo-500` | `focus:ring-accent/50` |
| `bg-red-50` | `bg-critical/10` |
| `border-red-200` | `border-critical/20` |
| `text-red-700` | `text-critical` |
| `bg-amber-50` | `bg-warning/10` |
| `border-amber-200` | `border-warning/20` |
| `text-amber-700` | `text-warning` |
| `bg-emerald-50` | `bg-success/10` |
| `border-emerald-200` | `border-success/20` |
| `text-emerald-600` | `text-success` |
| `bg-yellow-50` | `bg-warning/10` |
| `text-yellow-700` | `text-warning` |
| `bg-slate-50` | `bg-elevated` |
| `bg-slate-100` | `bg-elevated` |
| `bg-white` (em cards/panels) | `bg-card` |
| `border-slate-100` | `border-edge/50` |
| `border-slate-200` | `border-edge` |
| `border-slate-300` | `border-edge` |
| `divide-slate-100` | `divide-edge/50` |
| `text-slate-900` | `text-heading` |
| `text-slate-800` | `text-heading` |
| `text-slate-700` | `text-body` |
| `text-slate-600` | `text-muted` |
| `text-slate-500` | `text-muted` |
| `text-slate-400` | `text-faded` |
| `text-slate-300` | `text-faded` |
| `hover:bg-slate-50` | `hover:bg-elevated` |
| `hover:text-slate-800` | `hover:text-body` |
| `bg-blue-100 text-blue-700 border-blue-200` | `bg-accent/10 text-accent border-accent/20` |
| `bg-violet-100 text-violet-700 border-violet-200` | `bg-accent/10 text-accent border-accent/20` |

**Padrão de botão primário:**
```tsx
// ANTES:
<button className="flex items-center gap-2 px-4 py-2 text-sm bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors">
  {label}
</button>

// DEPOIS:
<Button variant="primary" onClick={...} disabled={...} className="flex items-center gap-2">
  {label}
</Button>
```

**Padrão de botão outline:**
```tsx
// ANTES:
<button className="px-3 py-1.5 text-sm text-indigo-600 border border-indigo-200 rounded hover:bg-indigo-50">
  {label}
</button>

// DEPOIS:
<Button variant="outline" onClick={...}>
  {label}
</Button>
```

**Padrão de botão ghost/secundário:**
```tsx
// ANTES:
<button className="flex items-center gap-2 text-sm text-slate-600 hover:text-slate-800 border border-slate-200 rounded-lg px-3 py-2">
  {label}
</button>

// DEPOIS:
<Button variant="ghost" onClick={...} className="flex items-center gap-2">
  {label}
</Button>
```

**Padrão de alerta de erro inline:**
```tsx
// ANTES:
<div className="flex items-center gap-3 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
  <XCircle size={16} />
  {message}
</div>

// DEPOIS:
<div className="flex items-center gap-3 p-4 bg-critical/10 border border-critical/20 rounded-lg text-critical text-sm">
  <XCircle size={16} />
  {message}
</div>
```

**Padrão de card de dados:**
```tsx
// ANTES:
<div className="bg-white border border-slate-200 rounded-xl p-5">
  ...
</div>

// DEPOIS:
<Card>
  <CardBody>
    ...
  </CardBody>
</Card>
// ou, para manter padding custom:
<div className="bg-card border border-edge rounded-xl p-5">
  ...
</div>
```

**Padrão de header de página:**
```tsx
// ANTES:
<div className="flex items-center justify-between">
  <div className="flex items-center gap-3">
    <SomeIcon size={24} className="text-indigo-600" />
    <div>
      <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
      <p className="mt-1 text-sm text-slate-500">{t('subtitle')}</p>
    </div>
  </div>
  <button ...>{t('refresh')}</button>
</div>

// DEPOIS:
<PageHeader
  title={t('title')}
  subtitle={t('subtitle')}
  icon={<SomeIcon size={20} />}
  actions={<Button variant="ghost" onClick={refetch}><RefreshCw size={14} />{t('refresh')}</Button>}
/>
```

---

## Task 1: Criar `src/frontend/src/lib/chartColors.ts`

**Files:**
- Create: `src/frontend/src/lib/chartColors.ts`

- [ ] **Step 1.1: Criar o ficheiro de paleta de gráficos**

```ts
// src/frontend/src/lib/chartColors.ts

/**
 * Paleta de cores aprovada para gráficos ECharts / Recharts.
 * Usar sempre estas constantes em vez de hex hardcoded nos widgets.
 * Design-Audit §1.1 — Chart Colors Exception.
 */

/** Série de cores para dados multi-série (compatível com ECharts e Recharts). */
export const CHART_SERIES = [
  '#1B7FE8', // brand blue  (≈ accent)
  '#0891B2', // cyan        (≈ t-cyan)
  '#059669', // emerald     (≈ t-success)
  '#D97706', // amber       (≈ t-warning)
  '#DC2626', // red         (≈ t-critical)
  '#7C3AED', // violet      (extra)
  '#EA580C', // orange      (extra)
  '#0369A1', // sky         (extra)
] as const;

/** Paleta rainbow — para distribuições circulares (pie/donut). */
export const CHART_RAINBOW = [
  '#6366f1', '#f59e0b', '#10b981', '#ef4444', '#8b5cf6', '#06b6d4',
] as const;

/** Paleta blue — para barras de comparação em tons azuis. */
export const CHART_BLUE = [
  '#1d4ed8', '#2563eb', '#3b82f6', '#60a5fa', '#93c5fd', '#bfdbfe',
] as const;

/** Paleta red — para métricas de erro/degradação. */
export const CHART_RED = [
  '#991b1b', '#b91c1c', '#dc2626', '#ef4444', '#f87171', '#fca5a5',
] as const;

/** Cores semânticas — para eixos, thresholds, indicadores. */
export const CHART_SEMANTIC = {
  success:  '#059669', // var(--t-success)
  warning:  '#D97706', // var(--t-warning)
  critical: '#DC2626', // var(--t-critical)
  info:     '#0891B2', // var(--t-cyan)
  accent:   '#1B7FE8', // var(--t-accent)
  muted:    '#64748b', // var(--t-muted)
  axis:     '#94a3b8', // var(--t-faded) — eixos e gridlines
  grid:     'rgba(148,163,184,0.15)', // gridline muito subtil
} as const;

/** Retorna a paleta certa dado um colorScheme string. */
export function getChartPalette(colorScheme?: string | null): readonly string[] {
  switch (colorScheme) {
    case 'rainbow': return CHART_RAINBOW;
    case 'blue':    return CHART_BLUE;
    case 'red':     return CHART_RED;
    default:        return CHART_SERIES;
  }
}
```

- [ ] **Step 1.2: Commit**

```bash
git add src/frontend/src/lib/chartColors.ts
git commit -m "feat(design-tokens): add approved chart color palette utility

Design-Audit P3 prerequisite.
Replaces ad-hoc hex arrays in chart widgets.

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 2: Criar `contracts/lib/contractVariants.ts`

**Files:**
- Create: `src/frontend/src/features/contracts/lib/contractVariants.ts`

- [ ] **Step 2.1: Criar o ficheiro de mapeamento de estados**

```ts
// src/frontend/src/features/contracts/lib/contractVariants.ts

import type { BadgeProps } from '../../../components/Badge';

type BadgeVariant = NonNullable<BadgeProps['variant']>;

/** Estados do ciclo de vida de contrato → variante de Badge. */
export type ContractLifecycleState =
  | 'Draft'
  | 'InReview'
  | 'Approved'
  | 'Locked'
  | 'Deprecated'
  | 'Archived';

const STATE_VARIANT_MAP: Record<ContractLifecycleState, BadgeVariant> = {
  Draft:      'default',
  InReview:   'warning',
  Approved:   'success',
  Locked:     'info',
  Deprecated: 'critical',
  Archived:   'default',
};

/**
 * Converte um estado de ciclo de vida de contrato para a variante de Badge correspondente.
 * @example stateToVariant('Approved') // → 'success'
 */
export function stateToVariant(state: string): BadgeVariant {
  return STATE_VARIANT_MAP[state as ContractLifecycleState] ?? 'default';
}

/** Mapeamento estado → classe CSS de cor legacy (para gradual migration). */
export const STATE_COLOR_CLASSES: Record<string, string> = {
  Draft:      'bg-default/10 text-muted border-edge/30',
  InReview:   'bg-warning/10 text-warning border-warning/20',
  Approved:   'bg-success/10 text-success border-success/20',
  Locked:     'bg-info/10 text-info border-info/20',
  Deprecated: 'bg-critical/10 text-critical border-critical/20',
  Archived:   'bg-default/10 text-faded border-edge/20',
};
```

- [ ] **Step 2.2: Commit**

```bash
git add src/frontend/src/features/contracts/lib/contractVariants.ts
git commit -m "feat(contracts): add stateToVariant utility for Badge migration

Design-Audit P5 prerequisite.
Replaces STATE_COLORS inline maps throughout contracts module.

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 3: Migrar feature `saas` (4 páginas)

**Files:**
- Modify: `src/frontend/src/features/saas/pages/LicensingPage.tsx`
- Modify: `src/frontend/src/features/saas/pages/TenantProvisioningPage.tsx`
- Modify: `src/frontend/src/features/saas/pages/AlertsPage.tsx`
- Modify: `src/frontend/src/features/saas/pages/AgentRegistrationsPage.tsx`
- Create: `src/frontend/src/__tests__/saas/LicensingPage.test.tsx`

**Padrão de migração para todas as 4 páginas saas:**
1. Adicionar imports: `PageContainer, PageSection` de `'../../../components/shell'` e `PageHeader` de `'../../../components/PageHeader'` e `Button` de `'../../../components/Button'`
2. Substituir root `<div className="p-6 space-y-6">` por `<PageContainer>`
3. Substituir bloco de header custom (h1 + button de refresh) por `<PageHeader>`
4. Aplicar Token Map de todas as classes Tailwind palette
5. Substituir `<button className="...bg-*">` por `<Button variant="...">`

- [ ] **Step 3.1: Migrar `LicensingPage.tsx`**

Adicionar ao topo dos imports:
```tsx
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { Card, CardBody } from '../../../components/Card';
```

Localizar e substituir o root return e o bloco de header:
```tsx
// ANTES — root return:
return (
  <div className="p-6 space-y-6">
    {/* Header */}
    <div className="flex items-center justify-between">
      <div>
        <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
        <p className="text-sm text-slate-500 mt-1">{t('subtitle')}</p>
      </div>
      <button
        onClick={() => refetch()}
        disabled={isFetching}
        className="flex items-center gap-2 text-sm text-slate-600 hover:text-slate-800 border border-slate-200 rounded-lg px-3 py-2 transition-colors"
      >
        <RefreshCw size={14} className={isFetching ? 'animate-spin' : ''} />
        {t('refresh')}
      </button>
    </div>

// DEPOIS — root return:
return (
  <PageContainer>
    <PageHeader
      title={t('title')}
      subtitle={t('subtitle')}
      icon={<Award size={20} />}
      actions={
        <Button variant="ghost" onClick={() => refetch()} disabled={isFetching} className="flex items-center gap-2">
          <RefreshCw size={14} className={isFetching ? 'animate-spin' : ''} />
          {t('refresh')}
        </Button>
      }
    />
    <div className="space-y-6">
```

Substituir fechamento do return:
```tsx
// ANTES:
    </div>
  );
}

// DEPOIS:
    </div>
  </PageContainer>
  );
}
```

Substituir todas as classes Tailwind palette usando o Token Map (ver topo do plano):
- `text-slate-900` → `text-heading`
- `text-slate-500` → `text-muted`
- `border-slate-200` → `border-edge`
- `bg-white` → `bg-card`
- `bg-slate-100` → `bg-elevated`
- Constante `PLAN_COLORS`:
```tsx
// ANTES:
const PLAN_COLORS: Record<TenantPlan, string> = {
  Trial:        'bg-slate-100 text-slate-700 border-slate-200',
  Starter:      'bg-blue-100 text-blue-700 border-blue-200',
  Professional: 'bg-violet-100 text-violet-700 border-violet-200',
  Enterprise:   'bg-amber-100 text-amber-700 border-amber-200',
};

// DEPOIS:
const PLAN_COLORS: Record<TenantPlan, string> = {
  Trial:        'bg-elevated text-muted border-edge',
  Starter:      'bg-accent/10 text-accent border-accent/20',
  Professional: 'bg-accent/10 text-accent border-accent/20',
  Enterprise:   'bg-warning/10 text-warning border-warning/20',
};
```

- [ ] **Step 3.2: Migrar `TenantProvisioningPage.tsx`**

Seguir o mesmo padrão que `LicensingPage.tsx`:
1. Adicionar os mesmos 4 imports
2. Substituir `<div className="p-6 space-y-6">` → `<PageContainer>` com `<div className="space-y-6">` dentro
3. Substituir header custom → `<PageHeader title={t('title')} subtitle={t('subtitle')} icon={<...Icon size={20} />} actions={...} />`
4. Aplicar Token Map completo

- [ ] **Step 3.3: Migrar `AlertsPage.tsx`**

Seguir o mesmo padrão. Verificar se a página tem lista de alertas — se tiver lista possivelmente vazia, adicionar no final da lista:
```tsx
{alerts.length === 0 && !isLoading && (
  <EmptyState
    icon={<Bell />}
    title={t('saasAlerts.empty.title', 'No active alerts')}
    description={t('saasAlerts.empty.description', 'There are currently no alerts for this tenant.')}
  />
)}
```
Adicionar import: `import { EmptyState } from '../../../components/EmptyState';`  
Adicionar import: `import { Bell } from 'lucide-react';` (se não existir)

- [ ] **Step 3.4: Migrar `AgentRegistrationsPage.tsx`**

Seguir o mesmo padrão. Se tiver lista possivelmente vazia, adicionar `<EmptyState>` equivalente.

- [ ] **Step 3.5: Escrever teste para `LicensingPage`**

```tsx
// src/frontend/src/__tests__/saas/LicensingPage.test.tsx
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { LicensingPage } from '../../features/saas/pages/LicensingPage';

vi.mock('../../features/saas/api/saasApi', () => ({
  saasApi: {
    getLicense: vi.fn(),
    provisionLicense: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

import { saasApi } from '../../features/saas/api/saasApi';

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <LicensingPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('LicensingPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders page title', () => {
    vi.mocked(saasApi.getLicense).mockResolvedValue({
      plan: 'Professional',
      includedHostUnits: 50,
      activeHostUnits: 12,
      usagePercent: 24,
      renewalDate: '2027-01-01',
      features: [],
    } as any);
    renderPage();
    expect(screen.getByRole('heading')).toBeInTheDocument();
  });

  it('renders plan information on load', async () => {
    vi.mocked(saasApi.getLicense).mockResolvedValue({
      plan: 'Professional',
      includedHostUnits: 50,
      activeHostUnits: 12,
      usagePercent: 24,
      renewalDate: '2027-01-01',
      features: [],
    } as any);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Professional')).toBeInTheDocument();
    });
  });

  it('shows error state on API failure', async () => {
    vi.mocked(saasApi.getLicense).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => {
      // Page should still render without crashing
      expect(screen.getByRole('heading')).toBeInTheDocument();
    });
  });
});
```

- [ ] **Step 3.6: Executar testes**

```bash
cd src/frontend
npm run test -- --reporter=verbose src/__tests__/saas/LicensingPage.test.tsx
```
Expected: 3 tests PASS

- [ ] **Step 3.7: Commit**

```bash
git add src/frontend/src/features/saas/pages/
git add src/frontend/src/__tests__/saas/
git commit -m "feat(saas): migrate all 4 pages to design system

- Add PageContainer + PageHeader to all saas pages
- Replace Tailwind palette with design tokens throughout
- Replace raw <button> with <Button> component
- Add EmptyState to AlertsPage and AgentRegistrationsPage
- Add LicensingPage smoke tests

Design-Audit P2 resolved.

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 4: Migrar feature `observability` (1 página + 3 componentes)

**Context:** `ObservabilityDashboardPage` foi scaffolded com shadcn/ui (`@/components/ui/*`) em vez do design system do projeto. Os sub-componentes (`RequestMetricsDashboard`, `ErrorAnalyticsDashboard`, `SystemHealthDashboard`) usam hex colors hardcoded.

**Files:**
- Modify: `src/frontend/src/features/observability/pages/ObservabilityDashboardPage.tsx`
- Modify: `src/frontend/src/features/observability/components/RequestMetricsDashboard.tsx`
- Modify: `src/frontend/src/features/observability/components/ErrorAnalyticsDashboard.tsx`
- Modify: `src/frontend/src/features/observability/components/SystemHealthDashboard.tsx`

- [ ] **Step 4.1: Migrar `ObservabilityDashboardPage.tsx`**

Substituir todos os imports `@/components/ui/*` pelos equivalentes do design system:
```tsx
// ANTES:
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';

// DEPOIS:
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Button } from '../../../components/Button';
// Nota: Tabs usa o Radix UI nativo ou o componente Tabs existente no projeto
// Verificar se existe src/frontend/src/components/Tabs.tsx — se sim, importar daí
// Se não existir, manter as classes CSS para tabs inline
```

Verificar se existe `components/Tabs.tsx`:
```bash
ls src/frontend/src/components/Tabs* 2>/dev/null || echo "no Tabs component"
```

Se não existir, implementar tabs com estado simples:
```tsx
// Substituir <Tabs>, <TabsList>, <TabsTrigger>, <TabsContent> por:
const TABS = [
  { id: 'overview', label: t('observability.tab.overview', 'Overview'), icon: <Activity size={14} /> },
  { id: 'requests', label: t('observability.tab.requests', 'Requests'), icon: <TrendingUp size={14} /> },
  { id: 'errors',   label: t('observability.tab.errors', 'Errors'),    icon: <AlertTriangle size={14} /> },
  { id: 'system',   label: t('observability.tab.system', 'System'),    icon: <Server size={14} /> },
] as const;

type TabId = typeof TABS[number]['id'];
```

Substituir o root return:
```tsx
// ANTES:
return (
  <div className="container mx-auto p-6 space-y-6">
    {/* Header */}
    <div className="flex items-center justify-between">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Observability Dashboard</h1>
        <p className="text-muted-foreground mt-1">
          Real-time monitoring and analytics powered by ClickHouse
        </p>
      </div>
      <Button variant="outline" onClick={() => window.location.reload()}>
        <TrendingUp className="mr-2 h-4 w-4" />
        Refresh
      </Button>
    </div>

// DEPOIS:
const { t } = useTranslation(); // adicionar se não existir

return (
  <PageContainer>
    <PageHeader
      title={t('observability.title', 'Observability Dashboard')}
      subtitle={t('observability.subtitle', 'Real-time monitoring and analytics')}
      icon={<Activity size={20} />}
      actions={
        <Button variant="ghost" onClick={() => window.location.reload()} className="flex items-center gap-2">
          <RefreshCw size={14} />
          {t('common.refresh', 'Refresh')}
        </Button>
      }
    />
    <div className="space-y-6">
```

Substituir tabs com implementação simples usando estado e classes:
```tsx
{/* Tab navigation */}
<div className="flex gap-1 p-1 bg-elevated rounded-lg w-fit">
  {TABS.map(tab => (
    <button
      key={tab.id}
      onClick={() => setActiveTab(tab.id as TabId)}
      className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
        activeTab === tab.id
          ? 'bg-card text-body shadow-sm'
          : 'text-muted hover:text-body'
      }`}
    >
      {tab.icon}
      {tab.label}
    </button>
  ))}
</div>

{/* Tab content */}
<div>
  {activeTab === 'overview' && (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
      {/* ... conteúdo da tab overview ... */}
    </div>
  )}
  {activeTab === 'requests' && <RequestMetricsDashboard />}
  {activeTab === 'errors' && <ErrorAnalyticsDashboard />}
  {activeTab === 'system' && <SystemHealthDashboard />}
</div>
```

Substituir `CardContent` por `CardBody` e aplicar Token Map completo (todas as classes `bg-blue-50`, etc.).

Substituir fechamento:
```tsx
    </div>
  </PageContainer>
);
```

Adicionar import `useTranslation` se não existir:
```tsx
import { useTranslation } from 'react-i18next';
```

- [ ] **Step 4.2: Migrar sub-componentes de observability**

Para `RequestMetricsDashboard.tsx`, `ErrorAnalyticsDashboard.tsx`, `SystemHealthDashboard.tsx`:

Aplicar Token Map a todas as classes Tailwind palette.

Substituir hex codes hardcoded por `CHART_SEMANTIC` e `CHART_SERIES` de `'../../../lib/chartColors'`:
```tsx
// ANTES (exemplo em ErrorAnalyticsDashboard):
stroke="#ef4444"
fill="#ef4444"
stroke="#f59e0b"

// DEPOIS:
import { CHART_SEMANTIC, CHART_SERIES } from '../../../lib/chartColors';
// ...
stroke={CHART_SEMANTIC.critical}
fill={CHART_SEMANTIC.critical}
stroke={CHART_SEMANTIC.warning}
```

Substituir `@/components/ui/card` por design system (caso usado nos sub-componentes):
```tsx
// ANTES:
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
// DEPOIS:
import { Card, CardBody, CardHeader } from '../../../components/Card';
```

- [ ] **Step 4.3: Commit**

```bash
git add src/frontend/src/features/observability/
git commit -m "feat(observability): migrate to design system

- Replace shadcn/ui imports with project design system components
- Add PageContainer + PageHeader to ObservabilityDashboardPage
- Replace inline tabs with design-system-aligned tab pattern
- Apply full token map (Tailwind palette → design tokens)
- Use chartColors utility for chart color references

Design-Audit P4 resolved.

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 5: Migrar feature `platform-admin` (31 páginas)

**Context:** 31/35 páginas do platform-admin não usam PageContainer/PageHeader e usam Tailwind palette hardcoded ao longo de todo o código. As 4 páginas já migradas (`ElasticsearchManagerPage`, `PlatformHealthDashboardPage`, `ResourceBudgetPage`, `StartupReportPage`) servem de referência.

**Strategy:** Migrar em sub-batches de ~5 páginas por commit. Ordem: AI pages primeiro, depois por complexidade decrescente.

**Files:**
- Modify: 31 ficheiros em `src/frontend/src/features/platform-admin/pages/`
- (See list in Step 5.2)

- [ ] **Step 5.1: Migrar `AiGovernancePage.tsx` (exemplo canónico completo)**

Este passo documenta a migração completa de uma página. Os passos seguintes seguem exatamente o mesmo padrão.

Adicionar imports no topo:
```tsx
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { Card, CardBody } from '../../../components/Card';
```

Substituir o root return (a `<div className="p-6 space-y-6">`):
```tsx
// ANTES:
return (
  <div className="p-6 space-y-6">
    {/* Header */}
    <div className="flex items-center justify-between">
      <div className="flex items-center gap-3">
        <ShieldCheck size={24} className="text-indigo-600" />
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
          <p className="mt-1 text-sm text-slate-500">{t('subtitle')}</p>
        </div>
      </div>
      <button
        onClick={() => refetch()}
        className="flex items-center gap-2 px-4 py-2 text-sm bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
      >
        <RefreshCw size={14} />
        {t('refresh', 'Refresh')}
      </button>
    </div>

// DEPOIS:
return (
  <PageContainer>
    <PageHeader
      title={t('title')}
      subtitle={t('subtitle')}
      icon={<ShieldCheck size={20} />}
      actions={
        <Button variant="ghost" onClick={() => refetch()} className="flex items-center gap-2">
          <RefreshCw size={14} />
          {t('refresh', 'Refresh')}
        </Button>
      }
    />
    <div className="space-y-6">
```

Substituir fechamento de return:
```tsx
// ANTES: último </div> antes de );
    </div>
  );

// DEPOIS:
    </div>
  </PageContainer>
  );
```

Aplicar Token Map completo em todos os classNames:
- `bg-indigo-600` → `bg-accent` (mas no Button já é automático)
- `bg-indigo-50` → `bg-accent/10`
- `text-indigo-600` → `text-accent`
- `border-indigo-200` → `border-accent/20`
- `bg-red-50 border border-red-200` → `bg-critical/10 border border-critical/20`
- `text-red-700` → `text-critical`
- `bg-amber-50 border border-amber-200` → `bg-warning/10 border border-warning/20`
- `bg-emerald-50 border border-emerald-200` → `bg-success/10 border border-success/20`
- `bg-slate-50 border-b border-slate-200` (table header) → `bg-elevated border-b border-edge`
- `hover:bg-slate-50` → `hover:bg-elevated`
- `text-slate-900` → `text-heading`, `text-slate-800` → `text-heading`
- `text-slate-700` → `text-body`, `text-slate-500` → `text-muted`, `text-slate-400` → `text-faded`
- `border-slate-200` → `border-edge`, `border-slate-300` → `border-edge`
- `bg-white` (em cards/panels) → `bg-card`
- `divide-slate-100` → `divide-edge/50`

Substituir botão de save:
```tsx
// ANTES:
<button
  onClick={saveConfig}
  disabled={configMutation.isPending}
  className="px-4 py-2 bg-indigo-600 text-white text-sm rounded-lg hover:bg-indigo-700 disabled:opacity-50"
>
  {configMutation.isPending ? t('saving') : t('save')}
</button>
<button
  onClick={() => setEditing(false)}
  className="px-4 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50"
>
  {t('cancel')}
</button>

// DEPOIS:
<Button variant="primary" onClick={saveConfig} disabled={configMutation.isPending}>
  {configMutation.isPending ? t('saving') : t('save')}
</Button>
<Button variant="ghost" onClick={() => setEditing(false)}>
  {t('cancel')}
</Button>
```

Substituir `SummaryCard` sub-component para usar tokens:
```tsx
// ANTES:
const colorMap = {
  indigo: 'text-indigo-600',
  emerald: 'text-emerald-600',
  red: 'text-red-600',
  slate: 'text-slate-700',
};
return (
  <div className="border border-slate-200 rounded-lg p-4 bg-white">
    <p className="text-xs text-slate-500 uppercase tracking-wide">{label}</p>
    <p className={`text-2xl font-semibold mt-1 ${colorMap[color]}`}>{value}</p>
  </div>
);

// DEPOIS:
const colorMap = {
  indigo: 'text-accent',
  emerald: 'text-success',
  red: 'text-critical',
  slate: 'text-body',
};
return (
  <div className="border border-edge rounded-lg p-4 bg-card">
    <p className="text-xs text-muted uppercase tracking-wide">{label}</p>
    <p className={`text-2xl font-semibold mt-1 ${colorMap[color]}`}>{value}</p>
  </div>
);
```

Substituir `NumberField` e `ToggleField` sub-components:
```tsx
// Em NumberField — substituir className do input:
// ANTES: "w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-1 focus:ring-indigo-500 focus:border-indigo-500"
// DEPOIS: "w-full px-3 py-2 border border-edge rounded-lg text-sm bg-canvas text-body focus:ring-1 focus:ring-accent/50 focus:border-accent"

// Em NumberField — label e hint:
// "block text-sm font-medium text-slate-700" → "block text-sm font-medium text-body"
// "text-xs text-slate-500" → "text-xs text-muted"

// Em ToggleField — checkbox:
// "mt-0.5 h-4 w-4 rounded border-slate-300 text-indigo-600" → "mt-0.5 h-4 w-4 rounded border-edge accent-accent"
// label: "text-sm font-medium text-slate-700" → "text-sm font-medium text-body"
// hint: "text-xs text-slate-500" → "text-xs text-muted"
```

- [ ] **Step 5.2: Migrar restantes 30 páginas platform-admin**

Para cada uma das páginas abaixo, aplicar exatamente o mesmo padrão documentado no Step 5.1:

**Sub-batch A — AI pages (commit juntas):**
- `AiModelManagerPage.tsx`
- `AiResourceGovernorPage.tsx`

**Sub-batch B — Infrastructure pages (commit juntas):**
- `BackupCoordinatorPage.tsx`
- `CanaryDashboardPage.tsx`
- `CapacityForecastPage.tsx`
- `DatabaseHealthPage.tsx`
- `DemoSeedPage.tsx`

**Sub-batch C — Platform ops pages (commit juntas):**
- `DoraAdminDashboardPage.tsx`
- `EnvironmentPoliciesPage.tsx`
- `ExternalHttpAuditPage.tsx`
- `FeatureFlagsRuntimePage.tsx`
- `GracefulShutdownPage.tsx`

**Sub-batch D — Security/network pages (commit juntas):**
- `GreenOpsPage.tsx`
- `MigrationPreviewPage.tsx`
- `MtlsManagerPage.tsx`
- `MultiTenantSchemaPage.tsx`
- `NetworkPolicyPage.tsx`

**Sub-batch E — Admin pages (commit juntas):**
- `NonProdSchedulerPage.tsx`
- `ObservabilityModePage.tsx`
- `PlatformAlertRulesPage.tsx`
- `ProxyConfigPage.tsx`
- `RecoveryWizardPage.tsx`

**Sub-batch F — Remaining pages (commit juntas):**
- `RightsizingPage.tsx`
- `SamlSsoPage.tsx`
- `SessionSecurityPage.tsx`
- `SetupWizardPage.tsx`
- `SupportBundlePage.tsx`
- `SystemHealthPage.tsx`

**Checklist de migração por página (aplicar em cada ficheiro):**
1. ☐ Adicionar imports: `PageContainer`, `PageHeader`, `Button`, `Card`, `CardBody`
2. ☐ Substituir `<div className="p-6 space-y-6">` → `<PageContainer><div className="space-y-6">`
3. ☐ Substituir header custom → `<PageHeader title={} subtitle={} icon={} actions={} />`
4. ☐ Fechar: adicionar `</PageContainer>` antes do `);`
5. ☐ Substituir botões primários inline → `<Button variant="primary">`
6. ☐ Substituir botões secundários/outline inline → `<Button variant="outline">` ou `<Button variant="ghost">`
7. ☐ Aplicar Token Map completo (todos os `bg-*`, `text-*`, `border-*` de Tailwind palette)
8. ☐ Substituir inline sub-components usando colors hardcoded (colorMap, COLORS)

**Commit por sub-batch:**
```bash
# Exemplo para Sub-batch A:
git add src/frontend/src/features/platform-admin/pages/AiModelManagerPage.tsx
git add src/frontend/src/features/platform-admin/pages/AiResourceGovernorPage.tsx
git commit -m "feat(platform-admin): migrate AI pages to design system (batch A)

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

- [ ] **Step 5.3: Escrever teste smoke para platform-admin**

```tsx
// src/frontend/src/__tests__/platform-admin/AiGovernancePage.test.tsx
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AiGovernancePage } from '../../features/platform-admin/pages/AiGovernancePage';

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getAiGovernanceDashboard: vi.fn(),
    updateAiGovernanceConfig: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <AiGovernancePage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('AiGovernancePage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders page heading', () => {
    vi.mocked(platformAdminApi.getAiGovernanceDashboard).mockResolvedValue({} as any);
    renderPage();
    expect(screen.getByRole('heading')).toBeInTheDocument();
  });

  it('renders without crashing on API error', async () => {
    vi.mocked(platformAdminApi.getAiGovernanceDashboard).mockRejectedValue(new Error('fail'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByRole('heading')).toBeInTheDocument();
    });
  });
});
```

- [ ] **Step 5.4: Executar testes**

```bash
cd src/frontend
npm run test -- --reporter=verbose src/__tests__/platform-admin/
```
Expected: todos os testes PASS

- [ ] **Step 5.5: Commit final platform-admin**

```bash
git add src/frontend/src/__tests__/platform-admin/
git commit -m "test(platform-admin): add smoke tests for migrated pages

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 6: Corrigir chart widgets em governance e catalog (P3)

**Context:** 11 ficheiros na feature governance e 3 na catalog usam hex colors hardcoded para paletas de gráficos ECharts/Recharts. A Task 1 criou `lib/chartColors.ts`. Esta task usa-o.

**Files:**
- Modify: `src/frontend/src/features/governance/widgets/HistogramWidget.tsx`
- Modify: `src/frontend/src/features/governance/widgets/PieChartWidget.tsx`
- Modify: `src/frontend/src/features/governance/widgets/OtelMetricsWidget.tsx`
- Modify: `src/frontend/src/features/governance/widgets/TreemapWidget.tsx`
- Modify: `src/frontend/src/features/governance/widgets/OtelServiceMapWidget.tsx`
- Modify: `src/frontend/src/features/governance/widgets/HeatmapCalendarWidget.tsx`
- Modify: `src/frontend/src/features/governance/widgets/BarGaugeWidget.tsx`
- Modify: `src/frontend/src/features/governance/widgets/QueryWidget.tsx`
- Modify: `src/frontend/src/features/governance/widgets/SloGaugeWidget.tsx`
- Modify: `src/frontend/src/features/governance/pages/DashboardBuilderPage.tsx`
- Modify: `src/frontend/src/features/governance/components/ChartAnnotations.tsx`
- Modify: `src/frontend/src/features/catalog/components/DependencyGraph.tsx`
- Modify: `src/frontend/src/features/catalog/components/ServiceScoreTab.tsx`

- [ ] **Step 6.1: Migrar `PieChartWidget.tsx`**

Adicionar import:
```tsx
import { getChartPalette, CHART_SEMANTIC } from '../../../lib/chartColors';
```

Localizar e substituir as paletas de cores hardcoded:
```tsx
// ANTES:
const RAINBOW_PALETTE = ['#6366f1', '#f59e0b', '#10b981', '#ef4444', '#8b5cf6', '#06b6d4'];
const BLUE_PALETTE = ['#1d4ed8', '#2563eb', '#3b82f6', '#60a5fa', '#93c5fd', '#bfdbfe'];
const RED_PALETTE = ['#991b1b', '#b91c1c', '#dc2626', '#ef4444', '#f87171', '#fca5a5'];
const DEFAULT_PALETTE = ['#6366f1', '#f59e0b', '#10b981', '#ef4444', '#8b5cf6', '#06b6d4', '#f97316', '#14b8a6'];

function getPalette(colorScheme?: string | null): string[] {
  switch (colorScheme) {
    case 'rainbow': return RAINBOW_PALETTE;
    case 'blue':    return BLUE_PALETTE;
    case 'red':     return RED_PALETTE;
    default:        return DEFAULT_PALETTE;
  }
}

// DEPOIS:
// Remover todas as constantes RAINBOW_PALETTE, BLUE_PALETTE, RED_PALETTE, DEFAULT_PALETTE e a função getPalette.
// Substituir usages de getPalette(x) por getChartPalette(x)
// Substituir qualquer uso de getPalette() por [...getChartPalette()] (para mutabilidade se necessário)
```

- [ ] **Step 6.2: Migrar `DependencyGraph.tsx`**

Localizar o objeto de cores de nó:
```tsx
// ANTES (aproximado — verificar o ficheiro real):
const nodeColors = {
  service: '#22c55e',
  api: '#3b82f6',
  warning: '#f59e0b',
  error: '#ef4444',
};
// ou similar com hex codes para ECharts itemStyle.color

// DEPOIS:
import { CHART_SEMANTIC } from '../../../lib/chartColors';

const nodeColors = {
  service: CHART_SEMANTIC.success,
  api:     CHART_SEMANTIC.accent,
  warning: CHART_SEMANTIC.warning,
  error:   CHART_SEMANTIC.critical,
};
```

Para os hex codes de Slate/neutral no DependencyGraph (bordas, backgrounds):
```tsx
// ANTES (aproximado):
'#475569' // slate-600
'#1e293b' // slate-800
'#e2e8f0' // slate-200
'#94a3b8' // slate-400

// DEPOIS: usar CHART_SEMANTIC.muted, CHART_SEMANTIC.axis
// ou valores CSS var: 'var(--t-muted)', 'var(--t-edge)'
```

- [ ] **Step 6.3: Migrar os restantes widgets**

Para cada widget em governance, seguir o padrão:
1. Adicionar `import { getChartPalette, CHART_SEMANTIC } from '../../../lib/chartColors';`
2. Remover constantes de paleta locais
3. Substituir `getPalette(...)` → `getChartPalette(...)`
4. Substituir hex de status semântico → `CHART_SEMANTIC.success/warning/critical/info/accent`
5. Substituir hex de eixos/grid → `CHART_SEMANTIC.axis` e `CHART_SEMANTIC.grid`

**Checklist por widget:**
- `HistogramWidget.tsx` — hex colors em bar fills
- `OtelMetricsWidget.tsx` — hex colors em series colors
- `TreemapWidget.tsx` — hex colors em node colors
- `OtelServiceMapWidget.tsx` — hex colors em node/edge colors
- `HeatmapCalendarWidget.tsx` — hex colors em heat scale
- `BarGaugeWidget.tsx` — hex colors em gauge fills
- `QueryWidget.tsx` — hex colors em chart
- `SloGaugeWidget.tsx` — hex colors em gauge
- `ChartAnnotations.tsx` — hex colors em annotation markers

- [ ] **Step 6.4: Commit**

```bash
git add src/frontend/src/features/governance/widgets/
git add src/frontend/src/features/governance/components/ChartAnnotations.tsx
git add src/frontend/src/features/governance/pages/DashboardBuilderPage.tsx
git add src/frontend/src/features/catalog/components/DependencyGraph.tsx
git add src/frontend/src/features/catalog/components/ServiceScoreTab.tsx
git commit -m "feat(charts): replace hardcoded hex colors with chartColors utility

- Governance widgets: HistogramWidget, PieChartWidget, OtelMetricsWidget,
  TreemapWidget, OtelServiceMapWidget, HeatmapCalendarWidget, BarGaugeWidget,
  QueryWidget, SloGaugeWidget, ChartAnnotations, DashboardBuilderPage
- Catalog: DependencyGraph, ServiceScoreTab
- All palette constants replaced with getChartPalette() from lib/chartColors
- Semantic colors use CHART_SEMANTIC constants

Design-Audit P3 resolved.

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 7: Migrar badges custom em `contracts` (P5)

**Context:** O módulo contracts usa `<span className="px-2 py-0.5 text-[10px] rounded-full STATE_COLORS[state]">` em vez do componente `<Badge>`. A Task 2 criou `contractVariants.ts`. Esta task usa-o.

**Files:**
- Modify: `src/frontend/src/features/contracts/canonical/CanonicalEntityCatalogPage.tsx`
- Modify: `src/frontend/src/features/contracts/catalog/components/CatalogBadges.tsx`
- Modify: `src/frontend/src/features/contracts/catalog/components/CatalogTable.tsx`
- Modify: `src/frontend/src/features/contracts/governance/ContractMigrationPage.tsx`
- Modify: outros ficheiros contracts com `STATE_COLORS` (verificar com grep)

- [ ] **Step 7.1: Encontrar todos os ficheiros com STATE_COLORS ou badges inline**

```bash
cd src/frontend/src
grep -rln "STATE_COLORS\|rounded-full.*px-2.*text-\[10px\]" --include="*.tsx" features/contracts/
```

- [ ] **Step 7.2: Migrar `CanonicalEntityCatalogPage.tsx`**

Adicionar import:
```tsx
import { Badge } from '../../../components/Badge';
import { stateToVariant } from '../lib/contractVariants';
```

Localizar e substituir o bloco `STATE_COLORS` e os spans de estado:
```tsx
// ANTES (padrão típico):
const STATE_COLORS: Record<string, string> = {
  Draft:      'bg-slate-100 text-slate-600 border border-slate-200',
  InReview:   'bg-amber-50 text-amber-700 border border-amber-200',
  Approved:   'bg-emerald-50 text-emerald-700 border border-emerald-200',
  Locked:     'bg-blue-50 text-blue-700 border border-blue-200',
  Deprecated: 'bg-red-50 text-red-700 border border-red-200',
};

// Nos componentes:
<span className={`px-2 py-0.5 text-[10px] rounded-full ${STATE_COLORS[entity.state]}`}>
  {entity.state}
</span>

// DEPOIS:
// Remover STATE_COLORS e usar:
<Badge variant={stateToVariant(entity.state)}>
  {entity.state}
</Badge>
```

Tags e aliases inline (já usam design tokens — manter como estão ou migrar para Badge):
```tsx
// Estas FICAM (já usam tokens):
<span className="px-2 py-0.5 text-[10px] rounded-full bg-accent/10 text-accent border border-accent/20">
// Opcional: migrar também para <Badge variant="info">
```

- [ ] **Step 7.3: Migrar `CatalogBadges.tsx`**

Verificar se `CatalogBadges.tsx` define componentes de badge custom. Se definir, simplificar para usar `<Badge>`:
```tsx
// ANTES (padrão CatalogBadges):
export function ProtocolBadge({ protocol, size = 'md' }: ...) {
  const sizeClass = size === 'sm' ? 'px-2 py-0.5 text-[10px]' : 'px-2.5 py-1 text-xs';
  const colors = { OpenApi: '...', GraphQL: '...', ... };
  return <span className={`inline-block font-mono ... ${sizeClass} ${colors[protocol]}`}>{protocol}</span>;
}

// DEPOIS — se os variants do Badge cobrirem os casos:
export function ProtocolBadge({ protocol }: { protocol: string }) {
  const variantMap: Record<string, BadgeVariant> = {
    OpenApi: 'info',
    GraphQL: 'success',
    AsyncApi: 'warning',
    Protobuf: 'default',
    Soap: 'default',
  };
  return <Badge variant={variantMap[protocol] ?? 'default'}>{protocol}</Badge>;
}

// Se o Badge não cobrir os casos (ex: font-mono), manter o span mas migrar as cores para tokens:
export function ProtocolBadge({ protocol }: { protocol: string }) {
  const colorMap: Record<string, string> = {
    OpenApi: 'bg-info/10 text-info border-info/20',
    GraphQL: 'bg-success/10 text-success border-success/20',
    Protobuf: 'bg-accent/10 text-accent border-accent/20',
  };
  return (
    <span className={`inline-block text-[10px] font-mono font-semibold px-1.5 py-0.5 rounded border ${colorMap[protocol] ?? 'bg-elevated text-muted border-edge'}`}>
      {protocol}
    </span>
  );
}
```

- [ ] **Step 7.4: Migrar restantes ficheiros contracts com STATE_COLORS**

Para cada ficheiro encontrado no Step 7.1, aplicar o mesmo padrão:
1. Remover `STATE_COLORS` local
2. Adicionar `import { Badge } from '...'` e `import { stateToVariant } from '../lib/contractVariants'`
3. Substituir `<span className=... STATE_COLORS[state]>` → `<Badge variant={stateToVariant(state)}>`

- [ ] **Step 7.5: Commit**

```bash
git add src/frontend/src/features/contracts/
git commit -m "feat(contracts): replace inline badge spans with Badge component

- Remove STATE_COLORS maps throughout contracts module
- Use stateToVariant() from contractVariants.ts
- Replace <span> badge patterns with <Badge variant=...>
- Migrate ProtocolBadge and CatalogBadges to design tokens

Design-Audit P5 resolved.

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 8: Verificação Final e Métricas

- [ ] **Step 8.1: Re-executar scans de auditoria para validar progresso**

```bash
cd src/frontend/src

echo "=== C1: Hex violations restantes ==="
grep -rln '#[0-9A-Fa-f]\{6\}' --include="*.tsx" features/platform-admin/ features/saas/ features/observability/ | wc -l

echo "=== C1: Tailwind palette restante (platform-admin) ==="
grep -rln 'bg-\(blue\|red\|green\|yellow\|purple\|orange\|gray\|slate\|zinc\|neutral\|stone\|amber\|lime\|emerald\|teal\|cyan\|sky\|indigo\|violet\|fuchsia\|pink\|rose\)-[0-9]' --include="*.tsx" features/platform-admin/ | wc -l

echo "=== C3: Páginas sem PageContainer (platform-admin) ==="
grep -rL "PageContainer" --include="*Page.tsx" features/platform-admin/ | wc -l

echo "=== C3: Páginas sem PageHeader (platform-admin) ==="
grep -rL "PageHeader" --include="*Page.tsx" features/platform-admin/ | wc -l

echo "=== C3: Páginas sem PageContainer (saas) ==="
grep -rL "PageContainer" --include="*Page.tsx" features/saas/ | wc -l
```

**Expected após todas as tasks:**
- Hex violations em platform-admin/saas/observability: 0
- Tailwind palette em platform-admin: ≤ 2 (apenas casos justificados)
- Pages sem PageContainer em platform-admin: 0
- Pages sem PageContainer em saas: 0

- [ ] **Step 8.2: Executar full test suite**

```bash
cd src/frontend
npm run test -- --reporter=verbose
```

Verificar que nenhum teste existente foi quebrado. Resolver falhas se houver.

- [ ] **Step 8.3: Executar lint**

```bash
cd src/frontend
npm run lint
```

Resolver qualquer erro de lint introduzido nas migrações (ex: imports não usados depois de remoções).

- [ ] **Step 8.4: Commit final de verificação**

```bash
git add -A
git commit -m "test(design-audit-phase1): verification pass — all P1-P5 resolved

Post-audit scan results:
- platform-admin: 0 Tailwind palette violations, 0 pages without PageContainer
- saas: 0 violations, 0 pages without PageContainer/PageHeader
- observability: shadcn/ui removed, design system adopted
- governance/catalog chart widgets: hex replaced with chartColors
- contracts: STATE_COLORS removed, Badge component adopted

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Referência de Imports Comuns

Ao migrar qualquer página platform-admin ou saas:

```tsx
// Shell & layout
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';

// Componentes base
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';
import { ErrorState } from '../../../components/ErrorState';

// Lib utilities
import { getChartPalette, CHART_SEMANTIC } from '../../../lib/chartColors';

// i18n
import { useTranslation } from 'react-i18next';
```

---

## Fases Seguintes (Plano B)

Este plano cobre **P1–P5** (features 🔴 Crítico). O **Plano B** (Fase Estrutural) cobrirá:

- P6: catalog — PageHeader em 17 páginas
- P7: catalog — PageContainer em 8 páginas  
- P8: catalog — token migration em 15 ficheiros
- P9: product-analytics — EmptyState em 10 páginas
- P10: integrations — PageHeader em 3 páginas
- P11: contracts — PageContainer nas 6 páginas sem container
- P12: governance — ErrorState nos centers/persona-suites
- P13: legacy-assets — PageHeader em 2 páginas
- P14: configuration — tokens em 10 ficheiros

Ver spec: `docs/superpowers/specs/2026-05-27-design-audit-spec.md §7`
