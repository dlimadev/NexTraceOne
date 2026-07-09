# Contract Consumer Journey Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ligar a espinha do consumidor de contratos sobre a chave partilhada `contractVersionId` — playground pré-carrega por query param + volta ao portal, portal liga ao playground, e o workspace liga ao portal do consumidor.

**Architecture:** Redesign de jornada no frontend React, só wiring por `Link`/query-param entre 3 páginas que já partilham a chave `contractVersionId`. Zero backend novo, zero query nova, honest-null.

**Tech Stack:** React 19, TypeScript 5.9, react-router-dom 7 (`Link`, `useSearchParams`, `useParams`), TanStack Query 5, DS `../../../shared/ui`, lucide-react, i18next (4 locales), Vitest + Testing Library, Playwright.

## Global Constraints

- DS de `../../../shared/ui`; componentes de `components/*`; ícones `lucide-react`; `Link`/`useSearchParams`/`useParams` de `react-router-dom`.
- Honest-null: links só quando `contractVersionId` existe; nunca fabricar.
- i18n: nenhuma string de UI hardcoded; `t('key','fallback inglês')`; chaves nos 4 locales `en, es, pt-BR, pt-PT` (NÃO existe `fr`); ficheiros FLAT `src/frontend/src/locales/<l>.json`.
- Mudanças cirúrgicas: não refatorar a lógica de execução do playground nem o interior das tabs do portal; a entrada manual do playground mantém-se como fallback.
- Testes centralizados em `src/frontend/src/__tests__/**`; e2e em `src/frontend/e2e/**` (globs de URL Playwright usam `**`).
- Tooling: `npm run test` (NÃO `npx vitest`); gate final `npm run build` (`tsc -b`); `npm run validate:i18n`.
- Rotas verbatim: portal `/contracts/portal/:contractVersionId`; playground `/contracts/playground`; workspace `/contracts/:contractVersionId`.

---

### Task 1: Playground — pré-carregar por `?contractVersionId=` + voltar ao portal

**Files:**
- Modify: `src/frontend/src/features/contracts/playground/ContractPlaygroundPage.tsx`
- Test: `src/frontend/src/__tests__/contracts/ContractPlaygroundPage.journey.test.tsx`

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/contracts/ContractPlaygroundPage.journey.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ContractPlaygroundPage } from '../../features/contracts/playground/ContractPlaygroundPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
const getDetail = vi.fn(() => Promise.resolve({ protocol: 'OpenApi', semVer: '1.0.0', spec: '{}' }));
vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: { getDetail: (...a: unknown[]) => getDetail(...a) },
}));

function wrap(entry: string) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={[entry]}><ContractPlaygroundPage /></MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ContractPlaygroundPage journey', () => {
  it('preloads from ?contractVersionId= and links back to the portal', async () => {
    wrap('/contracts/playground?contractVersionId=cv-1');
    const input = screen.getByLabelText(/Contract Version ID/i) as HTMLInputElement;
    expect(input.value).toBe('cv-1');
    await waitFor(() => expect(getDetail).toHaveBeenCalledWith('cv-1'));
    const back = screen.getByRole('link', { name: /back to portal/i });
    expect(back.getAttribute('href')).toBe('/contracts/portal/cv-1');
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/ContractPlaygroundPage.journey.test.tsx --run`
Expected: FAIL — input vazio, sem link "Back to portal".

- [ ] **Step 3: Write minimal implementation**

Adicionar imports de react-router-dom (o ficheiro ainda não importa de lá) e `ArrowLeft` à lista de lucide-react:

```tsx
import { useSearchParams, Link } from 'react-router-dom';
```
```tsx
// juntar ArrowLeft à lista existente de ícones lucide-react
  ArrowLeft,
```

Semear o estado do `contractVersionId` a partir do query param (substituir a linha `const [contractVersionId, setContractVersionId] = useState('');`):

```tsx
  const [searchParams] = useSearchParams();
  const [contractVersionId, setContractVersionId] = useState(searchParams.get('contractVersionId') ?? '');
```

Adicionar o link de retorno ao portal imediatamente antes do bloco `{/* ─── Contract Selector ─── */}` (dentro do `PageContainer`, após o `PageHeader`):

```tsx
      {contractVersionId && (
        <Link
          to={`/contracts/portal/${contractVersionId}`}
          className="inline-flex items-center gap-1.5 text-xs text-muted hover:text-accent transition-colors mb-3"
        >
          <ArrowLeft size={12} />
          {t('contracts.playground.backToPortal', 'Back to portal')}
        </Link>
      )}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/ContractPlaygroundPage.journey.test.tsx --run`
Expected: PASS (1 test).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/playground/ContractPlaygroundPage.tsx src/frontend/src/__tests__/contracts/ContractPlaygroundPage.journey.test.tsx
git commit -m "feat(contracts): playground preloads by contractVersionId and links back to portal

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: Portal → "Try in playground"

**Files:**
- Modify: `src/frontend/src/features/contracts/portal/ContractPortalPage.tsx`
- Test: `src/frontend/src/__tests__/contracts/ContractPortalPage.playgroundLink.test.tsx`

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/contracts/ContractPortalPage.playgroundLink.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { ContractPortalPage } from '../../features/contracts/portal/ContractPortalPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: {
    getDetail: vi.fn(() => Promise.resolve({
      apiAssetId: 'a-1', apiName: 'orders-api', protocol: 'OpenApi', semVer: '1.0.0',
      lifecycleState: 'Approved', routePattern: '/orders', createdAt: '2026-01-01T00:00:00Z',
    })),
    listRuleViolations: vi.fn(() => Promise.resolve([])),
    getHistory: vi.fn(() => Promise.resolve([])),
  },
}));
vi.mock('../../features/contracts/workspace/toStudioContract', () => ({
  toStudioContract: () => ({ friendlyName: 'Orders API', functionalDescription: 'desc', owner: 'team', domain: 'Commerce', criticality: 'High', technicalName: 'orders-api' }),
}));

function wrap() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={['/contracts/portal/cv-1']}>
        <Routes><Route path="/contracts/portal/:contractVersionId" element={<ContractPortalPage />} /></Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ContractPortalPage playground link', () => {
  it('links the header to the playground preloaded with the contract', async () => {
    wrap();
    const link = await screen.findByRole('link', { name: /try in playground/i });
    expect(link.getAttribute('href')).toBe('/contracts/playground?contractVersionId=cv-1');
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/ContractPortalPage.playgroundLink.test.tsx --run`
Expected: FAIL — sem link "Try in playground".

- [ ] **Step 3: Write minimal implementation**

Adicionar `Play` à lista de ícones importados de `lucide-react` no topo do ficheiro:

```tsx
// juntar Play à lista existente de ícones lucide-react
  Play,
```

Envolver a ação atual do `PageHeader` (o `<Button>` "Download") num wrapper que inclui o link, dentro do `actions={...}`. Substituir:

```tsx
        actions={
          <Button
            variant="outline"
            size="sm"
            icon={<ExternalLink size={14} />}
```
por:
```tsx
        actions={
          <div className="flex items-center gap-2">
            <Link
              to={`/contracts/playground?contractVersionId=${contractVersionId}`}
              className="inline-flex items-center gap-1.5 text-sm text-accent hover:underline"
            >
              <Play size={14} />
              {t('contracts.portal.tryInPlayground', 'Try in playground')}
            </Link>
            <Button
              variant="outline"
              size="sm"
              icon={<ExternalLink size={14} />}
```
E fechar o novo `<div>` logo após o `</Button>` de fecho da ação Download (antes do `}` que fecha `actions`):

```tsx
            {t('contracts.portal.download', 'Download')}
          </Button>
          </div>
        }
```

(`Link` já está importado no ficheiro — linha 2.)

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/ContractPortalPage.playgroundLink.test.tsx --run`
Expected: PASS (1 test).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/portal/ContractPortalPage.tsx src/frontend/src/__tests__/contracts/ContractPortalPage.playgroundLink.test.tsx
git commit -m "feat(contracts): portal links to the playground preloaded with the contract

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: Workspace → "Consumer portal"

**Files:**
- Modify: `src/frontend/src/features/contracts/workspace/ContractWorkspacePage.tsx`
- Test: `src/frontend/src/__tests__/contracts/ContractWorkspacePage.portalLink.test.tsx`

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/contracts/ContractWorkspacePage.portalLink.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { ContractWorkspacePage } from '../../features/contracts/workspace/ContractWorkspacePage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
vi.mock('../../features/contracts/workspace/editor/MonacoEditorWrapper', () => ({ MonacoEditorWrapper: () => null }));
vi.mock('../../features/contracts/workspace/WorkspaceLayout', () => ({
  WorkspaceLayout: ({ header }: { header: React.ReactNode }) => <div>{header}</div>,
}));
vi.mock('../../features/contracts/workspace/components/ContractLifecycleActions', () => ({
  ContractLifecycleActions: () => null,
}));
vi.mock('../../features/contracts/hooks', () => ({
  useContractDetail: () => ({
    data: { apiAssetId: 'a-1', protocol: 'OpenApi', semVer: '1.0.0', lifecycleState: 'Approved', isLocked: false, format: 'json' },
    isLoading: false, isError: false, refetch: vi.fn(),
  }),
  useContractViolations: () => ({ data: [] }),
  useContractTransition: () => ({ mutate: vi.fn() }),
  useContractExport: () => ({ exportVersion: vi.fn() }),
}));
vi.mock('../../features/contracts/workspace/toStudioContract', () => ({
  toStudioContract: (d: Record<string, unknown>) => ({ technicalName: 'orders-api', domain: 'Commerce', ...d }),
}));

describe('ContractWorkspacePage consumer portal link', () => {
  it('links the header to the consumer portal', () => {
    render(<MemoryRouter initialEntries={['/contracts/cv-1']}><ContractWorkspacePage /></MemoryRouter>);
    const link = screen.getByRole('link', { name: /consumer portal/i });
    expect(link.getAttribute('href')).toBe('/contracts/portal/cv-1');
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/ContractWorkspacePage.portalLink.test.tsx --run`
Expected: FAIL — sem link "Consumer portal".

- [ ] **Step 3: Write minimal implementation**

Adicionar `BookOpen` ao import de `lucide-react` (o ficheiro já importa `TrendingUp`):

```tsx
import { TrendingUp, BookOpen } from 'lucide-react';
```

Adicionar o link "Consumer portal" dentro do wrapper `flex items-center gap-3` das ações, entre o bloco do "Health timeline" e o `ContractLifecycleActions`:

```tsx
              {contractVersionId && (
                <Link
                  to={`/contracts/portal/${contractVersionId}`}
                  className="inline-flex items-center gap-1.5 text-sm text-accent hover:underline"
                >
                  <BookOpen size={14} />
                  {t('contracts.workspace.consumerPortal', 'Consumer portal')}
                </Link>
              )}
              <ContractLifecycleActions
```

(`contractVersionId` vem de `useParams` já no topo do componente; `Link` já importado na fatia 2.)

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/ContractWorkspacePage.portalLink.test.tsx --run`
Expected: PASS (1 test).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/workspace/ContractWorkspacePage.tsx src/frontend/src/__tests__/contracts/ContractWorkspacePage.portalLink.test.tsx
git commit -m "feat(contracts): link contract workspace header to the consumer portal

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: Chaves i18n (4 locales)

**Files:**
- Modify: `src/frontend/src/locales/en.json`
- Modify: `src/frontend/src/locales/es.json`
- Modify: `src/frontend/src/locales/pt-BR.json`
- Modify: `src/frontend/src/locales/pt-PT.json`

Chaves a adicionar (valores por locale):

- `contracts.portal.tryInPlayground` — en `Try in playground` · es `Probar en el playground` · pt-BR `Testar no playground` · pt-PT `Testar no playground`
- `contracts.playground.backToPortal` — en `Back to portal` · es `Volver al portal` · pt-BR `Voltar ao portal` · pt-PT `Voltar ao portal`
- `contracts.workspace.consumerPortal` — en `Consumer portal` · es `Portal del consumidor` · pt-BR `Portal do consumidor` · pt-PT `Portal do consumidor`

- [ ] **Step 1: Adicionar as chaves aos 4 locales** (deep-merge sob `contracts.portal`, `contracts.playground`, `contracts.workspace`, preservando chaves existentes; reescrever com `JSON.stringify(obj, null, 2)`).

- [ ] **Step 2: Validar i18n**

Run: `cd src/frontend && npm run validate:i18n`
Expected: PASS — 4 locales completos e em paridade.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/locales/en.json src/frontend/src/locales/es.json src/frontend/src/locales/pt-BR.json src/frontend/src/locales/pt-PT.json
git commit -m "i18n(contracts): consumer journey keys (4 locales)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 5: e2e + gates finais

**Files:**
- Create: `src/frontend/e2e/contract-consumer-journey.spec.ts`

- [ ] **Step 1: Escrever o e2e**

```ts
// src/frontend/e2e/contract-consumer-journey.spec.ts
import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/** E2E — o playground pré-carrega pelo contractVersionId do URL e volta ao portal. */
test.describe('Contract consumer journey', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/contracts/cv-1/detail**', (route) =>
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ contractVersionId: 'cv-1', apiAssetId: 'a-1', apiName: 'orders-api', protocol: 'OpenApi', semVer: '1.0.0', lifecycleState: 'Approved', spec: '{"paths":{}}' }),
      }));
  });

  test('playground auto-loads from the contractVersionId query param', async ({ page }) => {
    await page.goto('/contracts/playground?contractVersionId=cv-1');
    await expect(page.getByRole('link', { name: /back to portal/i })).toBeVisible({ timeout: 5_000 });
    await expect(page.getByText(/v1\.0\.0/)).toBeVisible({ timeout: 5_000 });
  });
});
```

- [ ] **Step 2: Correr o e2e**

Run (PowerShell): `Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend"; $env:CI=""; npx playwright test --project=chromium e2e/contract-consumer-journey.spec.ts`
Expected: 1 passed.

- [ ] **Step 3: Gates finais**

Run: `cd src/frontend && npm run test -- --run 2>&1 | tail -5` → suite completa verde.
Run: `cd src/frontend && npm run validate:i18n` → PASS.
Run: `cd src/frontend && npm run build 2>&1 | tail -3` → exit 0.
Run: `cd src/frontend && npx eslint src/features/contracts/playground/ContractPlaygroundPage.tsx src/features/contracts/portal/ContractPortalPage.tsx src/features/contracts/workspace/ContractWorkspacePage.tsx` → 0 erros.

- [ ] **Step 4: Commit**

```bash
git add src/frontend/e2e/contract-consumer-journey.spec.ts
git commit -m "test(contracts): e2e — playground auto-loads from query param, links to portal

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Self-Review

**1. Spec coverage:**
- §4.1 playground preload → Task 1. ✓
- §4.2 portal → playground → Task 2. ✓
- §4.3 playground → portal → Task 1 (mesmo ficheiro). ✓
- §4.4 workspace → portal → Task 3. ✓
- §7 i18n → Task 4. ✓
- §8 testes (playground journey, portal link, workspace link, e2e) → Tasks 1-3, 5. ✓

**2. Placeholder scan:** Sem TBD/TODO. Código completo em cada step. Task 2 Step 3 mostra o wrapping exato do `actions` com a linha de fecho do `</div>`.

**3. Type consistency:** Rotas verbatim iguais entre spec, plano e testes: portal `/contracts/portal/:contractVersionId`, playground `/contracts/playground?contractVersionId=`, workspace `/contracts/:contractVersionId`. Todos os links usam `contractVersionId` (mesma chave em playground `useState`/param, portal/workspace `useParams`). `getDetail(contractVersionId)` confere com a API real.
