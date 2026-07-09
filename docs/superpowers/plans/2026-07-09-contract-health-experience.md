# Contract Health Experience Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Tornar a experiência de saúde de contratos utilizável e visual — timeline pré-carrega por `apiAssetId` + tendência SVG, health dashboard filtrável por domínio/tipo, e drill honesto workspace→timeline.

**Architecture:** Redesign de UX no frontend React. Um componente puro de sparkline; alterações cirúrgicas na timeline (query param + montar sparkline), na dashboard (filtros ligados a params já suportados), e no header do workspace (link para a timeline com o apiAssetId). Zero backend novo, honest-null.

**Tech Stack:** React 19, TypeScript 5.9, react-router-dom 7 (`Link`, `useSearchParams`), TanStack Query 5, DS `../../../shared/ui` (`TextField`, `Select`), lucide-react, i18next (4 locales), Vitest + Testing Library, Playwright.

## Global Constraints

- DS de `../../../shared/ui`; componentes de `components/*`; ícones `lucide-react`; `Link`/`useSearchParams` de `react-router-dom`.
- Honest-null: sparkline oculto com <2 pontos; link do workspace só com `apiAssetId`; params de filtro vazios omitidos (não enviar string vazia); nunca fabricar.
- i18n: nenhuma string de UI hardcoded; `t('key','fallback inglês')`; chaves nos 4 locales `en, es, pt-BR, pt-PT` (NÃO existe `fr`); ficheiros FLAT `src/frontend/src/locales/<l>.json`.
- Mudanças cirúrgicas: não refatorar lógica de query nem o interior das tabelas; a entrada manual da timeline mantém-se como fallback.
- Testes centralizados em `src/frontend/src/__tests__/**`; e2e em `src/frontend/e2e/**` (globs de URL Playwright usam `**`).
- Tooling: `npm run test` (NÃO `npx vitest`); gate final `npm run build` (`tsc -b`); `npm run validate:i18n`.
- Rotas verbatim: timeline `/contracts/health/timeline`; workspace `/contracts/:contractVersionId`.
- `getHealthDashboard` aceita `{ domain?, contractType?, page?, pageSize? }`; `getContractHealthTimeline(apiAssetId)`.
- `CONTRACT_TYPES` de `../shared/constants` = `{ value: string; labelKey: string; icon: string }[]`.

---

### Task 1: `HealthTrendSparkline` (tendência SVG pura)

**Files:**
- Create: `src/frontend/src/features/contracts/governance/HealthTrendSparkline.tsx`
- Test: `src/frontend/src/__tests__/contracts/HealthTrendSparkline.test.tsx`

**Interfaces:**
- Produces: `export function HealthTrendSparkline(props: { points: { semVer: string; healthScore: number }[] }): JSX.Element | null`. Devolve `null` quando `points.length < 2`.

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/contracts/HealthTrendSparkline.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render } from '@testing-library/react';
import { HealthTrendSparkline } from '../../features/contracts/governance/HealthTrendSparkline';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));

describe('HealthTrendSparkline', () => {
  it('renders a polyline for two or more points', () => {
    const { container } = render(
      <HealthTrendSparkline points={[
        { semVer: '1.0.0', healthScore: 40 },
        { semVer: '1.1.0', healthScore: 80 },
      ]} />,
    );
    expect(container.querySelector('polyline')).not.toBeNull();
  });

  it('renders nothing (honest-null) with fewer than two points', () => {
    const { container } = render(<HealthTrendSparkline points={[{ semVer: '1.0.0', healthScore: 40 }]} />);
    expect(container.firstChild).toBeNull();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/HealthTrendSparkline.test.tsx --run`
Expected: FAIL — módulo não existe.

- [ ] **Step 3: Write minimal implementation**

```tsx
// src/frontend/src/features/contracts/governance/HealthTrendSparkline.tsx
import { useTranslation } from 'react-i18next';

interface HealthTrendSparklineProps {
  points: { semVer: string; healthScore: number }[];
}

/**
 * Tendência do health score ao longo das versões — polyline SVG pura (sem libs de gráfico).
 * Honest-null: com menos de 2 pontos não há tendência a mostrar.
 */
export function HealthTrendSparkline({ points }: HealthTrendSparklineProps) {
  const { t } = useTranslation();
  if (points.length < 2) return null;

  const W = 100;
  const H = 32;
  const scores = points.map((p) => p.healthScore);
  const min = Math.min(...scores);
  const max = Math.max(...scores);
  const span = max - min || 1;
  const stepX = W / (points.length - 1);

  const coords = points.map((p, i) => {
    const x = i * stepX;
    const y = H - ((p.healthScore - min) / span) * H;
    return { x, y };
  });
  const polyPoints = coords.map((c) => `${c.x.toFixed(2)},${c.y.toFixed(2)}`).join(' ');

  return (
    <div className="rounded-lg border border-edge bg-panel p-4">
      <p className="text-xs font-medium uppercase tracking-wide text-muted mb-2">
        {t('contracts.healthTimeline.trend', 'Health score trend')}
      </p>
      <svg
        viewBox={`0 0 ${W} ${H}`}
        preserveAspectRatio="none"
        className="w-full h-16"
        role="img"
        aria-label={t('contracts.healthTimeline.trend', 'Health score trend')}
      >
        <polyline
          points={polyPoints}
          fill="none"
          stroke="var(--color-accent)"
          strokeWidth={1.5}
          vectorEffect="non-scaling-stroke"
        />
        {coords.map((c, i) => (
          <circle key={points[i].semVer} cx={c.x} cy={c.y} r={1.5} fill="var(--color-accent)" vectorEffect="non-scaling-stroke" />
        ))}
      </svg>
    </div>
  );
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/HealthTrendSparkline.test.tsx --run`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/governance/HealthTrendSparkline.tsx src/frontend/src/__tests__/contracts/HealthTrendSparkline.test.tsx
git commit -m "feat(contracts): HealthTrendSparkline — pure SVG health score trend

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: Timeline — pré-carregar por `?apiAssetId=` + montar sparkline

**Files:**
- Modify: `src/frontend/src/features/contracts/governance/ContractHealthTimelinePage.tsx`
- Test: `src/frontend/src/__tests__/contracts/ContractHealthTimelinePage.preload.test.tsx`

**Interfaces:**
- Consumes: `HealthTrendSparkline` de Task 1.

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/contracts/ContractHealthTimelinePage.preload.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ContractHealthTimelinePage } from '../../features/contracts/governance/ContractHealthTimelinePage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
const getContractHealthTimeline = vi.fn(() => Promise.resolve({ apiAssetId: 'asset-1', points: [] }));
vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: { getContractHealthTimeline: (...a: unknown[]) => getContractHealthTimeline(...a) },
}));

function wrap(entry: string) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={[entry]}><ContractHealthTimelinePage /></MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ContractHealthTimelinePage preload', () => {
  it('pre-fills and auto-loads from ?apiAssetId=', async () => {
    wrap('/contracts/health/timeline?apiAssetId=asset-1');
    const input = screen.getByLabelText(/API Asset ID/i) as HTMLInputElement;
    expect(input.value).toBe('asset-1');
    await waitFor(() => expect(getContractHealthTimeline).toHaveBeenCalledWith('asset-1'));
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/ContractHealthTimelinePage.preload.test.tsx --run`
Expected: FAIL — input vazio e query não dispara.

- [ ] **Step 3: Write minimal implementation**

Adicionar `useSearchParams` ao import de `react-router-dom` (criar o import — o ficheiro ainda não importa de react-router-dom) e importar o sparkline:

```tsx
import { useSearchParams } from 'react-router-dom';
import { HealthTrendSparkline } from './HealthTrendSparkline';
```

Substituir a inicialização de estado (linhas ~50-52) para semear do query param:

```tsx
  const [searchParams] = useSearchParams();
  const initialAssetId = searchParams.get('apiAssetId') ?? '';
  const [apiAssetId, setApiAssetId] = useState(initialAssetId);
  const [submittedId, setSubmittedId] = useState(initialAssetId);
  const [validationError, setValidationError] = useState('');
```

(A query já tem `enabled: !!submittedId` → semear `submittedId` dispara o load. `getContractHealthTimeline(submittedId)` é chamado com `'asset-1'`.)

Montar o sparkline dentro do bloco `{data && (...)}`, imediatamente antes do `<div className="bg-elevated rounded-lg border border-edge overflow-hidden">` da tabela:

```tsx
      {data && (
        <div className="space-y-4">
          {points.length >= 2 && <HealthTrendSparkline points={points} />}
          <div className="bg-elevated rounded-lg border border-edge overflow-hidden">
```

E fechar o `<div className="space-y-4">` adicional: no fim do bloco `{data && (...)}`, o `</div>` de fecho da tabela passa a ser seguido por mais um `</div>`. Ou seja, envolver a tabela existente com `<div className="space-y-4">…</div>`. Verificar o balanceamento de tags após a edição (a tabela e o sparkline ficam ambos dentro do novo wrapper).

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/ContractHealthTimelinePage.preload.test.tsx --run`
Expected: PASS (1 test).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/governance/ContractHealthTimelinePage.tsx src/frontend/src/__tests__/contracts/ContractHealthTimelinePage.preload.test.tsx
git commit -m "feat(contracts): timeline preloads by apiAssetId and shows a trend sparkline

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: Health dashboard — filtros `domain` + `contractType`

**Files:**
- Modify: `src/frontend/src/features/contracts/governance/ContractHealthDashboardPage.tsx`
- Test: `src/frontend/src/__tests__/contracts/ContractHealthDashboardPage.filters.test.tsx`

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/contracts/ContractHealthDashboardPage.filters.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ContractHealthDashboardPage } from '../../features/contracts/governance/ContractHealthDashboardPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
const getHealthDashboard = vi.fn(() => Promise.resolve({
  totalContractVersions: 0, distinctContracts: 0, deprecatedVersions: 0, filteredCount: 0,
  percentWithExamples: 0, percentWithCanonicalEntities: 0, healthScore: 0, topViolations: [],
}));
vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: { getHealthDashboard: (...a: unknown[]) => getHealthDashboard(...a) },
}));

function wrap() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(<QueryClientProvider client={qc}><MemoryRouter><ContractHealthDashboardPage /></MemoryRouter></QueryClientProvider>);
}

describe('ContractHealthDashboardPage filters', () => {
  it('refetches with contractType when the type filter changes', async () => {
    wrap();
    await waitFor(() => expect(getHealthDashboard).toHaveBeenCalled());
    const select = screen.getByLabelText(/Type/i) as HTMLSelectElement;
    fireEvent.change(select, { target: { value: 'RestApi' } });
    await waitFor(() => expect(getHealthDashboard).toHaveBeenCalledWith(expect.objectContaining({ contractType: 'RestApi' })));
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/ContractHealthDashboardPage.filters.test.tsx --run`
Expected: FAIL — não há Select de tipo.

- [ ] **Step 3: Write minimal implementation**

Adicionar imports (o ficheiro já usa `useTranslation`; adicionar `useState` ao import de `react` e os controlos DS + constantes):

```tsx
import { useState } from 'react';
```
```tsx
import { TextField, Select } from '../../../shared/ui';
import { CONTRACT_TYPES } from '../shared/constants';
```

Adicionar estado e ligar à query (substituir o bloco `useQuery` existente):

```tsx
  const [domain, setDomain] = useState('');
  const [contractType, setContractType] = useState('');

  const { data, isLoading, error } = useQuery<HealthDashboardData>({
    queryKey: ['contract-health-dashboard', domain.trim(), contractType],
    queryFn: () => contractsApi.getHealthDashboard({
      page: 1,
      pageSize: 50,
      ...(domain.trim() ? { domain: domain.trim() } : {}),
      ...(contractType ? { contractType } : {}),
    }) as Promise<HealthDashboardData>,
    staleTime: 30_000,
  });
```

Adicionar a barra de filtros logo após o `<PageHeader ... />`:

```tsx
      <div className="flex flex-wrap gap-3 items-end">
        <div className="min-w-[220px]">
          <TextField
            label={t('contracts.healthDashboard.filterDomain', 'Domain')}
            size="sm"
            value={domain}
            onChange={(e) => setDomain(e.target.value)}
            placeholder={t('contracts.healthDashboard.filterDomain', 'Domain')}
          />
        </div>
        <div className="w-52">
          <Select
            label={t('contracts.healthDashboard.filterType', 'Type')}
            size="sm"
            value={contractType}
            onChange={(e) => setContractType(e.target.value)}
            options={[
              { value: '', label: t('contracts.healthDashboard.allTypes', 'All types') },
              ...CONTRACT_TYPES.map((ct) => ({ value: ct.value, label: t(ct.labelKey, ct.value) })),
            ]}
          />
        </div>
      </div>
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/ContractHealthDashboardPage.filters.test.tsx --run`
Expected: PASS (1 test).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/governance/ContractHealthDashboardPage.tsx src/frontend/src/__tests__/contracts/ContractHealthDashboardPage.filters.test.tsx
git commit -m "feat(contracts): expose domain and contract-type filters on the health dashboard

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: Workspace → link "Health timeline"

**Files:**
- Modify: `src/frontend/src/features/contracts/workspace/ContractWorkspacePage.tsx`
- Test: `src/frontend/src/__tests__/contracts/ContractWorkspacePage.healthLink.test.tsx`

**Interfaces:**
- Consumes: `detail.apiAssetId` (já carregado pelo `useContractDetail`).

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/contracts/ContractWorkspacePage.healthLink.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { ContractWorkspacePage } from '../../features/contracts/workspace/ContractWorkspacePage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
// Renderiza só o header do WorkspaceLayout para isolar o teste do resto do workspace.
vi.mock('../../features/contracts/workspace/WorkspaceLayout', () => ({
  WorkspaceLayout: ({ header }: { header: React.ReactNode }) => <div>{header}</div>,
}));
vi.mock('../../features/contracts/workspace/components/ContractLifecycleActions', () => ({
  ContractLifecycleActions: () => null,
}));
vi.mock('../../features/contracts/hooks', () => ({
  useContractDetail: () => ({
    data: { apiAssetId: 'asset-1', protocol: 'OpenApi', semVer: '1.0.0', lifecycleState: 'Approved', isLocked: false, format: 'json' },
    isLoading: false, isError: false, refetch: vi.fn(),
  }),
  useContractViolations: () => ({ data: [] }),
  useContractTransition: () => ({ mutate: vi.fn() }),
  useContractExport: () => ({ exportVersion: vi.fn() }),
}));
vi.mock('../../features/contracts/workspace/toStudioContract', () => ({
  toStudioContract: (d: Record<string, unknown>) => ({ technicalName: 'orders-api', domain: 'Commerce', ...d }),
}));

describe('ContractWorkspacePage health link', () => {
  it('links the header to the contract health timeline', () => {
    render(<MemoryRouter initialEntries={['/contracts/cv-1']}><ContractWorkspacePage /></MemoryRouter>);
    const link = screen.getByRole('link', { name: /health timeline/i });
    expect(link.getAttribute('href')).toBe('/contracts/health/timeline?apiAssetId=asset-1');
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/ContractWorkspacePage.healthLink.test.tsx --run`
Expected: FAIL — não há link "Health timeline".

- [ ] **Step 3: Write minimal implementation**

Adicionar imports: `Link` ao import de `react-router-dom` (já importa `useParams`), `useTranslation`, e `TrendingUp` (novo import de lucide-react):

```tsx
import { useParams, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { TrendingUp } from 'lucide-react';
```

Dentro do componente, obter `t`:

```tsx
  const { t } = useTranslation();
```

Substituir o `actions={<ContractLifecycleActions ... />}` do `PageHeader` por um wrapper que inclui o link (honest-null no `apiAssetId`):

```tsx
          actions={
            <div className="flex items-center gap-3">
              {detail.apiAssetId && (
                <Link
                  to={`/contracts/health/timeline?apiAssetId=${detail.apiAssetId}`}
                  className="inline-flex items-center gap-1.5 text-sm text-accent hover:underline"
                >
                  <TrendingUp size={14} />
                  {t('contracts.workspace.healthTimeline', 'Health timeline')}
                </Link>
              )}
              <ContractLifecycleActions
                lifecycleState={detail.lifecycleState as ContractLifecycleState}
                isLocked={detail.isLocked}
                onTransition={handleTransition}
                onExport={handleExport}
              />
            </div>
          }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/ContractWorkspacePage.healthLink.test.tsx --run`
Expected: PASS (1 test).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/workspace/ContractWorkspacePage.tsx src/frontend/src/__tests__/contracts/ContractWorkspacePage.healthLink.test.tsx
git commit -m "feat(contracts): link contract workspace header to its health timeline

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 5: Chaves i18n (4 locales)

**Files:**
- Modify: `src/frontend/src/locales/en.json`
- Modify: `src/frontend/src/locales/es.json`
- Modify: `src/frontend/src/locales/pt-BR.json`
- Modify: `src/frontend/src/locales/pt-PT.json`

Chaves a adicionar (valores por locale):

- `contracts.healthTimeline.trend` — en `Health score trend` · es `Tendencia del puntaje de salud` · pt-BR `Tendência do índice de saúde` · pt-PT `Tendência do índice de saúde`
- `contracts.healthDashboard.filterDomain` — en `Domain` · es `Dominio` · pt-BR `Domínio` · pt-PT `Domínio`
- `contracts.healthDashboard.filterType` — en `Type` · es `Tipo` · pt-BR `Tipo` · pt-PT `Tipo`
- `contracts.healthDashboard.allTypes` — en `All types` · es `Todos los tipos` · pt-BR `Todos os tipos` · pt-PT `Todos os tipos`
- `contracts.workspace.healthTimeline` — en `Health timeline` · es `Cronología de salud` · pt-BR `Linha do tempo de saúde` · pt-PT `Cronologia de saúde`

- [ ] **Step 1: Adicionar as chaves aos 4 locales** (deep-merge sob `contracts.healthTimeline`, `contracts.healthDashboard`, `contracts.workspace`, preservando chaves existentes; reescrever com `JSON.stringify(obj, null, 2)`).

- [ ] **Step 2: Validar i18n**

Run: `cd src/frontend && npm run validate:i18n`
Expected: PASS — 4 locales completos e em paridade.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/locales/en.json src/frontend/src/locales/es.json src/frontend/src/locales/pt-BR.json src/frontend/src/locales/pt-PT.json
git commit -m "i18n(contracts): health experience keys (4 locales)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 6: e2e + gates finais

**Files:**
- Create: `src/frontend/e2e/contract-health-experience.spec.ts`

- [ ] **Step 1: Escrever o e2e**

```ts
// src/frontend/e2e/contract-health-experience.spec.ts
import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/** E2E — a timeline pré-carrega pelo apiAssetId do URL, sem digitação manual. */
test.describe('Contract health experience', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/contracts/asset-1/health/timeline**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          apiAssetId: 'asset-1',
          points: [
            { semVer: '1.0.0', healthScore: 55, createdAt: '2026-01-01T00:00:00Z', lifecycleState: 'Approved', isBreakingChange: false },
            { semVer: '1.1.0', healthScore: 82, createdAt: '2026-02-01T00:00:00Z', lifecycleState: 'Approved', isBreakingChange: true },
          ],
        }),
      }));
  });

  test('timeline auto-loads from the apiAssetId query param', async ({ page }) => {
    await page.goto('/contracts/health/timeline?apiAssetId=asset-1');
    await expect(page.getByText('1.1.0')).toBeVisible({ timeout: 5_000 });
    await expect(page.locator('polyline')).toBeVisible({ timeout: 5_000 });
  });
});
```

- [ ] **Step 2: Correr o e2e**

Run (PowerShell): `Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend"; $env:CI=""; npx playwright test --project=chromium e2e/contract-health-experience.spec.ts`
Expected: 1 passed.

- [ ] **Step 3: Gates finais**

Run: `cd src/frontend && npm run test -- --run 2>&1 | tail -5` → suite completa verde.
Run: `cd src/frontend && npm run validate:i18n` → PASS.
Run: `cd src/frontend && npm run build 2>&1 | tail -3` → exit 0.
Run: `cd src/frontend && npx eslint src/features/contracts/governance/HealthTrendSparkline.tsx src/features/contracts/governance/ContractHealthTimelinePage.tsx src/features/contracts/governance/ContractHealthDashboardPage.tsx src/features/contracts/workspace/ContractWorkspacePage.tsx` → 0 erros.

- [ ] **Step 4: Commit**

```bash
git add src/frontend/e2e/contract-health-experience.spec.ts
git commit -m "test(contracts): e2e — health timeline auto-loads from query param

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Self-Review

**1. Spec coverage:**
- §4.1 timeline preload → Task 2. ✓
- §4.2 sparkline → Task 1 (componente) + Task 2 (montagem). ✓
- §4.3 dashboard filters → Task 3. ✓
- §4.4 workspace→timeline link → Task 4. ✓
- §7 i18n → Task 5. ✓
- §8 testes (sparkline, preload, filters, workspace link, e2e) → Tasks 1-4, 6. ✓

**2. Placeholder scan:** Sem TBD/TODO. Código completo em cada step. Task 2 Step 3 nota o balanceamento de tags do wrapper `space-y-4` (instrução concreta, não placeholder).

**3. Type consistency:** `HealthTrendSparkline({ points: { semVer; healthScore }[] })` idêntico entre Task 1 (produz) e Task 2 (consome). `getHealthDashboard` recebe `{ domain?, contractType?, page, pageSize }` — consistente com a assinatura real. `detail.apiAssetId` usado em Task 4 confere com o `useContractDetail` real. Rotas verbatim iguais entre spec, plano e testes.
