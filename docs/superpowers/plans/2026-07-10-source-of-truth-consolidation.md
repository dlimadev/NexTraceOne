# Source-of-Truth Consolidation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Inline execution (não subagent).

**Goal:** Tornar a vista Source-of-Truth consolidada de cada entidade alcançável de onde o utilizador já está (detalhe do serviço, workspace do contrato, resultado da busca global).

**Architecture:** Três fatias sobre páginas React existentes: (F1) `ServiceDetailPage` liga à sua vista SoT de serviço; (F2) `ContractWorkspacePage` liga à sua vista SoT de contrato; (F3) `GlobalSearchPage`/`SearchResultCard` oferece uma ponte "Fonte da verdade" só para entidades de serviço. Os motores de busca (CommandPalette, GlobalSearchPage query) não mudam. Filosofia da P3: a vista consolidada acessível de onde já estás.

**Tech Stack:** React 19, TypeScript 5.9, react-router-dom 7 (`Link`, `useParams`), TanStack Query 5, Vitest + Testing Library, i18next (4 locales).

## Global Constraints

- Idioma de UI: **chaves i18n**, nunca strings hardcoded. Usar `t('key', 'English fallback')`.
- Novas chaves nos 4 locales (`en`, `es`, `pt-BR`, `pt-PT`) via script deep-merge; `npm run validate:i18n` **tem de passar**.
- Honest-null: nunca ligar sem identificador. F3 ponte SoT **apenas** para `entityType` de serviço (`entityId===serviceId` inequívoco); **não** fabricar ponte para contratos na busca global.
- Comandos npm a partir de `src/frontend`: `npm run test`, `npm run build`, `npm run validate:i18n`. Bash tool: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend` antes (o cwd volta à raiz entre chamadas).
- Testes centralizados em `src/frontend/src/__tests__/**`.
- Rotas SoT: `/source-of-truth/services/:serviceId`, `/source-of-truth/contracts/:contractVersionId`. Reverso (SoT→primário) **já existe** em ambas as páginas SoT.
- **Armadilha do mock `t`:** dois padrões coexistem — (a) `t:(k,f)=>typeof f==='string'?f:k` devolve o **fallback**; (b) `t:(k)=>k` (usado em `GlobalSearchPage.test.tsx`) devolve a **chave**. Assertir por `href` via `querySelector` é robusto a ambos e preferível para links.
- **Armadilha ServiceDetail view:** serviço `Planning` mostra o checklist; o bloco secundário com os cross-links só aparece no detalhe completo → testar com `lifecycleStatus:'Active'`.
- Cada commit termina com `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

---

### Task 1: F1 — ServiceDetail → vista SoT de serviço

**Files:**
- Modify: `src/frontend/src/features/catalog/pages/ServiceDetailPage.tsx` (bloco de cross-links do separador overview, junto de "Ver mudanças"/"Ver scorecard")
- Modify: `src/frontend/src/locales/{en,es,pt-BR,pt-PT}.json` (chave `serviceDetail.viewSourceOfTruth`)
- Test: `src/frontend/src/__tests__/catalog/ServiceDetailPage.sourceOfTruthLink.test.tsx` (novo)

**Interfaces:**
- Consumes: `serviceId` (prop de `ViewContent`, em scope no separador overview onde já estão os links `/changes` e `/services/scorecards`).
- Produces: URL de drill `/source-of-truth/services/${serviceId}`.

- [ ] **Step 1: Escrever o teste (falha)**

Criar `src/frontend/src/__tests__/catalog/ServiceDetailPage.sourceOfTruthLink.test.tsx` (reaproveita o scaffold de `ServiceDetailPage.scorecardLink.test.tsx`):

```tsx
import { describe, it, expect, vi } from 'vitest';
import { render, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { ServiceDetailPage } from '../../features/catalog/pages/ServiceDetailPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, d?: string) => (typeof d === 'string' ? d : k) }) }));
vi.mock('../../contexts/EnvironmentContext', () => ({ useEnvironment: () => ({ activeEnvironment: null }) }));
vi.mock('../../features/catalog/components/ServiceLifecyclePanel', () => ({ ServiceLifecyclePanel: () => null }));
vi.mock('../../features/catalog/components/ServiceLinksSection', () => ({ ServiceLinksSection: () => null }));
vi.mock('../../features/ai-hub/components/AssistantPanel', () => ({ AssistantPanel: () => null }));

const service = {
  id: 'svc-1', name: 'orders-api', displayName: 'Orders API', domain: 'Commerce',
  serviceType: 'RestApi', criticality: 'Medium', exposureType: 'Internal',
  lifecycleStatus: 'Active', teamName: 'Orders', technicalOwner: '', apis: [], apiCount: 0,
};
vi.mock('../../features/catalog/api', () => ({
  serviceCatalogApi: {
    getServiceDetail: vi.fn(() => Promise.resolve(service)),
    getServiceMaturity: vi.fn(() => Promise.resolve({ level: 'Bronze', dimensions: [] })),
  },
  contractsApi: { listContractsByService: vi.fn(() => Promise.resolve({ contracts: [], totalCount: 0 })) },
}));

function renderAt(id: string) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={[`/services/${id}`]}>
        <Routes><Route path="/services/:serviceId" element={<ServiceDetailPage />} /></Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ServiceDetailPage source-of-truth link', () => {
  it('liga à vista SoT consolidada do serviço', async () => {
    renderAt('svc-1');
    const link = await waitFor(() => {
      const a = document.querySelector('a[href="/source-of-truth/services/svc-1"]');
      if (!a) throw new Error('SoT link ainda não renderizado');
      return a;
    });
    expect(link).toHaveTextContent('View source of truth');
  });
});
```

- [ ] **Step 2: Correr — falha**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run test -- ServiceDetailPage.sourceOfTruthLink --run`
Expected: FAIL (link não existe).

- [ ] **Step 3: Adicionar o link no bloco de cross-links do overview**

Em `ServiceDetailPage.tsx`, o bloco atual (separador overview do `ViewContent`) é:
```tsx
              <div className="flex flex-wrap items-center gap-4">
                <Link
                  to={`/changes?serviceName=${encodeURIComponent(service.name)}`}
                  className="inline-flex items-center gap-1.5 text-xs text-accent hover:underline"
                >
                  <ExternalLink size={12} />
                  {t('catalog.detail.viewChange')}
                </Link>
                <Link
                  to={`/services/scorecards?serviceName=${encodeURIComponent(service.name)}`}
                  className="inline-flex items-center gap-1.5 text-xs text-accent hover:underline"
                >
                  <ExternalLink size={12} />
                  {t('serviceDetail.viewScorecard', 'View scorecard')}
                </Link>
              </div>
```
Adicionar um terceiro `Link` antes do `</div>`:
```tsx
                <Link
                  to={`/services/scorecards?serviceName=${encodeURIComponent(service.name)}`}
                  className="inline-flex items-center gap-1.5 text-xs text-accent hover:underline"
                >
                  <ExternalLink size={12} />
                  {t('serviceDetail.viewScorecard', 'View scorecard')}
                </Link>
                <Link
                  to={`/source-of-truth/services/${serviceId}`}
                  className="inline-flex items-center gap-1.5 text-xs text-accent hover:underline"
                >
                  <ExternalLink size={12} />
                  {t('serviceDetail.viewSourceOfTruth', 'View source of truth')}
                </Link>
              </div>
```

- [ ] **Step 4: Correr — passa**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run test -- ServiceDetailPage.sourceOfTruthLink --run`
Expected: PASS.

- [ ] **Step 5: i18n `serviceDetail.viewSourceOfTruth` (4 locales)**

Script Node (scratchpad):
```js
import { readFileSync, writeFileSync } from 'node:fs';
const dir = 'C:/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend/src/locales';
const byLoc = { en: 'View source of truth', es: 'Ver fuente de verdad', 'pt-BR': 'Ver fonte da verdade', 'pt-PT': 'Ver fonte da verdade' };
function deepMerge(t, p) { for (const [k, v] of Object.entries(p)) { t[k] = (v && typeof v === 'object' && !Array.isArray(v)) ? deepMerge(t[k] && typeof t[k] === 'object' ? t[k] : {}, v) : v; } return t; }
for (const [loc, v] of Object.entries(byLoc)) {
  const path = `${dir}/${loc}.json`;
  const json = JSON.parse(readFileSync(path, 'utf8'));
  deepMerge(json, { serviceDetail: { viewSourceOfTruth: v } });
  writeFileSync(path, JSON.stringify(json, null, 2) + '\n', 'utf8');
}
```
Run: `node <script>.mjs` depois `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run validate:i18n`
Expected: PASS.

- [ ] **Step 6: Build + commit**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run build`
Expected: sem erros.
```bash
git add src/frontend/src/features/catalog/pages/ServiceDetailPage.tsx src/frontend/src/locales/*.json "src/frontend/src/__tests__/catalog/ServiceDetailPage.sourceOfTruthLink.test.tsx"
git commit -m "feat(catalog): detalhe do serviço liga à sua vista source-of-truth (P4 F1)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: F2 — ContractWorkspace → vista SoT de contrato

**Files:**
- Modify: `src/frontend/src/features/contracts/workspace/ContractWorkspacePage.tsx` (import `Globe`; link no wrapper de ações do header, no bloco gated em `contractVersionId`)
- Modify: `src/frontend/src/locales/{en,es,pt-BR,pt-PT}.json` (chave `contracts.workspace.viewSourceOfTruth`)
- Test: `src/frontend/src/__tests__/contracts/ContractWorkspacePage.sourceOfTruthLink.test.tsx` (novo)

**Interfaces:**
- Consumes: `contractVersionId` (de `useParams`, já usado para o link "Consumer portal"); `detail`/`studioContract` do `useContractDetail`.
- Produces: URL de drill `/source-of-truth/contracts/${contractVersionId}`.

- [ ] **Step 1: Escrever o teste (falha)**

Criar `src/frontend/src/__tests__/contracts/ContractWorkspacePage.sourceOfTruthLink.test.tsx`. O workspace importa secções que puxam `MonacoEditorWrapper` (`?worker`, falha no vitest) → mockar. Mockar `WorkspaceLayout` para renderizar só o `header`. O link é gated em `useParams().contractVersionId` → precisa de `<Route>`.

```tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { ContractWorkspacePage } from '../../features/contracts/workspace/ContractWorkspacePage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, d?: string) => (typeof d === 'string' ? d : k) }) }));
vi.mock('../../contexts/EnvironmentContext', () => ({ useEnvironment: () => ({ activeEnvironmentId: 'env-1' }) }));
vi.mock('../../features/contracts/editor/MonacoEditorWrapper', () => ({ MonacoEditorWrapper: () => null }));
vi.mock('../../features/contracts/workspace/WorkspaceLayout', () => ({ WorkspaceLayout: ({ header }: { header: React.ReactNode }) => <div>{header}</div> }));
vi.mock('../../features/contracts/workspace/StudioRail', () => ({ StudioRail: () => null }));
vi.mock('../../features/contracts/workspace/WorkspaceTabs', () => ({ WorkspaceTabs: () => null }));

const detail = {
  contractVersionId: 'cv-1', apiAssetId: 'a-1', semVer: '1.0.0', protocol: 'OpenApi',
  format: 'json', lifecycleState: 'Draft', isLocked: false, technicalName: 'orders-api', domain: 'Commerce',
};
vi.mock('../../features/contracts/hooks', () => ({
  useContractDetail: () => ({ data: detail, isLoading: false, isError: false }),
  useContractViolations: () => ({ data: { items: [] }, isLoading: false }),
  useContractTransition: () => ({ mutate: vi.fn(), isPending: false }),
}));

function renderAt(id: string) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={[`/contracts/${id}`]}>
        <Routes><Route path="/contracts/:contractVersionId" element={<ContractWorkspacePage />} /></Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ContractWorkspacePage source-of-truth link', () => {
  it('liga à vista SoT consolidada do contrato', async () => {
    renderAt('cv-1');
    const link = await waitFor(() => {
      const a = document.querySelector('a[href="/source-of-truth/contracts/cv-1"]');
      if (!a) throw new Error('SoT link ainda não renderizado');
      return a;
    });
    expect(link).toHaveTextContent('View source of truth');
  });
});
```

> Nota: os nomes exatos dos hooks (`useContractDetail`/`useContractViolations`/`useContractTransition`) e o shape mínimo de `detail`/`studioContract` devem ser confirmados ao abrir o ficheiro; ajustar o mock ao que o componente realmente consome (ex.: se usa `toStudioContract(detail)`, enriquecer `detail` com `technicalName`/`domain`/`protocol`/`semVer`/`format`/`lifecycleState`/`isLocked` — já incluídos acima). Se o componente derivar `studioContract` e este crashar por campo em falta, acrescentar o campo ao mock `detail`.

- [ ] **Step 2: Correr — falha**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run test -- ContractWorkspacePage.sourceOfTruthLink --run`
Expected: FAIL (link não existe). Se falhar por crash de mock (campo em falta), corrigir o mock até o header renderizar, então confirmar que falha por ausência do link.

- [ ] **Step 3: Adicionar `Globe` ao import de ícones e o link no header**

Adicionar `Globe` à lista de imports de `lucide-react` no topo de `ContractWorkspacePage.tsx` (junto de `TrendingUp`, `BookOpen`).

No wrapper de ações do header, o bloco atual é:
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
```
Adicionar, logo a seguir, um segundo bloco gated:
```tsx
              {contractVersionId && (
                <Link
                  to={`/source-of-truth/contracts/${contractVersionId}`}
                  className="inline-flex items-center gap-1.5 text-sm text-accent hover:underline"
                >
                  <Globe size={14} />
                  {t('contracts.workspace.viewSourceOfTruth', 'View source of truth')}
                </Link>
              )}
```

- [ ] **Step 4: Correr — passa**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run test -- ContractWorkspacePage.sourceOfTruthLink --run`
Expected: PASS.

- [ ] **Step 5: i18n `contracts.workspace.viewSourceOfTruth` (4 locales)**

Script Node análogo, com:
```js
const byLoc = { en: 'View source of truth', es: 'Ver fuente de verdad', 'pt-BR': 'Ver fonte da verdade', 'pt-PT': 'Ver fonte da verdade' };
// deepMerge(json, { contracts: { workspace: { viewSourceOfTruth: v } } })
```
Run: `node <script>.mjs` depois `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run validate:i18n`
Expected: PASS.

- [ ] **Step 6: Build + commit**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run build`
Expected: sem erros.
```bash
git add src/frontend/src/features/contracts/workspace/ContractWorkspacePage.tsx src/frontend/src/locales/*.json "src/frontend/src/__tests__/contracts/ContractWorkspacePage.sourceOfTruthLink.test.tsx"
git commit -m "feat(contracts): workspace liga à sua vista source-of-truth (P4 F2)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: F3 — Busca global → ponte SoT (só serviços)

**Files:**
- Modify: `src/frontend/src/features/catalog/pages/GlobalSearchPage.tsx` (reestruturar `SearchResultCard`: card vira `<div>` com Link primário + Link SoT irmão para serviços)
- Modify: `src/frontend/src/locales/{en,es,pt-BR,pt-PT}.json` (chave `commandPalette.globalSearch.sourceOfTruthLink`)
- Test: `src/frontend/src/__tests__/catalog/GlobalSearchPage.sotBridge.test.tsx` (novo)

**Interfaces:**
- Consumes: `SearchResultItem` (`entityId`, `entityType`, `title`, `subtitle`, `owner`, `status`, `route`); `globalSearchApi.search`.
- Produces: URL de ponte `/source-of-truth/services/${item.entityId}` (apenas `entityType.toLowerCase()==='service'`).

- [ ] **Step 1: Escrever o teste (falha)**

Criar `src/frontend/src/__tests__/catalog/GlobalSearchPage.sotBridge.test.tsx`:

```tsx
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/catalog/api/globalSearch', () => ({ globalSearchApi: { search: vi.fn() } }));
vi.mock('../../api/client', () => ({ default: { get: vi.fn(), post: vi.fn() } }));
vi.mock('../../releaseScope', () => ({ isRouteAvailableInFinalProductionScope: () => true }));
vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, d?: string) => (typeof d === 'string' ? d : k) }) }));
vi.mock('../../contexts/EnvironmentContext', () => ({ useEnvironment: () => ({ activeEnvironmentId: 'env-1' }) }));

import { globalSearchApi } from '../../features/catalog/api/globalSearch';
import { GlobalSearchPage } from '../../features/catalog/pages/GlobalSearchPage';

const results = {
  items: [
    { entityId: 'svc-1', route: '/services/svc-1', entityType: 'Service', title: 'Order Service', subtitle: null, owner: null, status: 'active', relevanceScore: 1 },
    { entityId: 'cv-9', route: '/contracts/cv-9', entityType: 'Contract', title: 'orders-api', subtitle: null, owner: null, status: 'published', relevanceScore: 1 },
  ],
  facetCounts: { services: 1, contracts: 1 },
  totalResults: 2,
};

function renderPage(search: string) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={[`/search${search}`]}>
        <GlobalSearchPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('GlobalSearchPage SoT bridge', () => {
  beforeEach(() => { vi.clearAllMocks(); vi.mocked(globalSearchApi.search).mockResolvedValue(results); });

  it('resultado de serviço mostra a ponte para a vista SoT', async () => {
    renderPage('?q=order');
    await waitFor(() => {
      const a = document.querySelector('a[href="/source-of-truth/services/svc-1"]');
      if (!a) throw new Error('ponte SoT ainda não renderizada');
      return a;
    });
  });

  it('resultado de contrato NÃO mostra ponte SoT de serviço', async () => {
    renderPage('?q=order');
    await waitFor(() => expect(globalSearchApi.search).toHaveBeenCalled());
    // só o serviço tem ponte; nenhum href SoT aponta para o id do contrato
    expect(document.querySelector('a[href="/source-of-truth/services/cv-9"]')).toBeNull();
    expect(document.querySelectorAll('a[href^="/source-of-truth/services/"]').length).toBe(1);
  });
});
```

- [ ] **Step 2: Correr — falha**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run test -- GlobalSearchPage.sotBridge --run`
Expected: FAIL (nenhuma ponte).

- [ ] **Step 3: Reestruturar `SearchResultCard`**

Substituir a função `SearchResultCard` (o `return` que hoje é um único `<Link to={item.route}>` a envolver ícone/texto/status/go-hint) por um `<div>` contentor com o Link primário e, para serviços, um Link SoT irmão (evita `<a>` aninhado):

```tsx
function SearchResultCard({
  item,
}: {
  item: SearchResultItem;
}) {
  const { t } = useTranslation();
  const isService = item.entityType.toLowerCase() === 'service';

  return (
    <div className="bg-panel border border-edge rounded-lg hover:bg-hover transition-colors flex items-center gap-2 group">
      <Link
        to={item.route}
        className="flex-1 min-w-0 flex items-center gap-4 p-4"
      >
        {/* Ícone por tipo */}
        <div className="flex items-center justify-center w-9 h-9 rounded-lg bg-elevated text-muted shrink-0">
          {entityTypeIcons[item.entityType.toLowerCase()] ?? <Search size={16} />}
        </div>

        {/* Texto principal */}
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium text-heading truncate">{item.title}</p>
          <p className="text-xs text-muted truncate mt-0.5">
            {item.subtitle ?? t(`commandPalette.entity${capitalize(item.entityType)}`, item.entityType)}
            {item.owner && (
              <span className="text-faded"> · {item.owner}</span>
            )}
          </p>
        </div>

        {/* Status badge */}
        {item.status && (
          <span
            className={`shrink-0 rounded-full px-2 py-0.5 text-[10px] font-medium ${
              statusColors[item.status] ?? STATUS_COLOR_DEFAULT
            }`}
          >
            {item.status}
          </span>
        )}
      </Link>

      {/* Ponte para a vista Source of Truth — apenas serviços (entityId===serviceId) */}
      {isService && (
        <Link
          to={`/source-of-truth/services/${item.entityId}`}
          className="shrink-0 pr-4 text-xs text-accent hover:underline whitespace-nowrap"
        >
          {t('commandPalette.globalSearch.sourceOfTruthLink', 'Source of truth')}
        </Link>
      )}
    </div>
  );
}
```

> Nota: o `entityTypeIcons` lookup passa a usar `.toLowerCase()` (as chaves do mapa são minúsculas; os dados vêm capitalizados, ex. `'Service'`), corrigindo de passagem o fallback de ícone. O `capitalize(item.entityType)` do subtítulo mantém-se. O `ChevronRight`/"go" hint pré-existente pode ser removido nesta reestruturação (deixou de caber no layout de dois links) — remover o import `ChevronRight` se ficar órfão.

- [ ] **Step 4: Correr — passa**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run test -- GlobalSearchPage.sotBridge --run`
Expected: PASS (2 testes).

- [ ] **Step 5: Rodar o teste pré-existente da página (não regredir)**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run test -- GlobalSearchPage --run`
Expected: PASS (o `GlobalSearchPage.test.tsx` existente continua verde; se o "go" hint removido era assertado, ajustar — não é).

- [ ] **Step 6: i18n `commandPalette.globalSearch.sourceOfTruthLink` (4 locales)**

Script Node análogo, com:
```js
const byLoc = { en: 'Source of truth', es: 'Fuente de verdad', 'pt-BR': 'Fonte da verdade', 'pt-PT': 'Fonte da verdade' };
// deepMerge(json, { commandPalette: { globalSearch: { sourceOfTruthLink: v } } })
```
Run: `node <script>.mjs` depois `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run validate:i18n`
Expected: PASS.

- [ ] **Step 7: Build + commit**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run build`
Expected: sem erros (confirma que nenhum import ficou órfão).
```bash
git add src/frontend/src/features/catalog/pages/GlobalSearchPage.tsx src/frontend/src/locales/*.json "src/frontend/src/__tests__/catalog/GlobalSearchPage.sotBridge.test.tsx"
git commit -m "feat(catalog): busca global oferece ponte source-of-truth p/ serviços (P4 F3)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: Revisão final e merge

- [ ] **Step 1: Suite completa + build + i18n**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run test -- --run && npm run build && npm run validate:i18n`
Expected: tudo PASS.

- [ ] **Step 2: Revisão opus de todo o branch**

Dispatch do code-reviewer sobre o diff da P4 (desde o último commit pré-P4 até HEAD). Corrigir Critical/Important com um único fix.

- [ ] **Step 3: Merge/push em `main`** (sem PR, conforme instrução do owner)

O `main` local acompanha o branch de trabalho; após a revisão retornar 0 Critical/0 Important:
```bash
git push origin main
```

---

## Self-Review

**1. Spec coverage:**
- F1 (ServiceDetail → `/source-of-truth/services/:id`) → Task 1 ✓
- F2 (ContractWorkspace → `/source-of-truth/contracts/:contractVersionId`; reverso já existe) → Task 2 ✓
- F3 (GlobalSearch SearchResultCard ponte SoT só serviços; honest-null p/ contratos) → Task 3 ✓
- i18n 4 locales + validate → Steps de cada task ✓
- Não-objetivos (não rebuildar busca, não fundir endpoints, não ponte SoT-contrato na busca) → nenhuma task os viola ✓

**2. Placeholder scan:** sem TBD/TODO; código completo. As duas notas de incerteza (nomes de hooks do workspace no F2; remoção do "go" hint no F3) têm instrução explícita de resolução (confirmar ao abrir o ficheiro / remover import órfão). ✓

**3. Type consistency:** `serviceId`/`contractVersionId`/`entityId` usados conforme os tipos reais; rotas SoT verbatim de `catalogRoutes.tsx`; chaves i18n idênticas entre implementação e teste (fallbacks 'View source of truth' / 'Source of truth'). ✓
