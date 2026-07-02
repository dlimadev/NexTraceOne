# Catalog Discovery Redesign — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reformular a `ServiceCatalogPage` numa superfície de descoberta ("Browse") centrada no consumidor — pesquisa-primeiro, filtros facetados, cartões de serviço ricos — com as ferramentas de análise realojadas sob um segmento "Explorar".

**Architecture:** Um segmento de topo `Browse | Explorar`. "Browse" é uma superfície nova composta por: um hook de estado sincronizado no URL, um adapter puro (grafo → view-model + facetas + filtragem), uma barra de facetas, e cartões/linhas de resultado. "Explorar" reembrulha as abas de análise existentes (overview/graph/impact/temporal) **sem** as redesenhar. A lógica pura (adapter/filtragem) é isolada e testada por TDD; os componentes são finos por cima dela.

**Tech Stack:** React 19, TypeScript 5.9, TanStack Query, react-router-dom 7 (`useSearchParams` para estado no URL), Vitest + Testing Library, design system NexTraceOne (`components/*`), i18next.

## Global Constraints

- Design system apenas: `TextField/SearchInput/Select/Button/IconButton/Tabs/Card/Badge/StatCard` de `src/frontend/src/components/*`. **Zero controlos HTML crus, zero cores hardcoded** — só tokens semânticos (`bg-canvas/text-heading/text-muted/border-edge/accent/success/warning/critical/on-accent`).
- Texto de UI só por chaves i18n `t('...')` — nunca strings hardcoded. Locales flat em `src/frontend/src/locales/<l>.json` (`en/es/pt-BR/pt-PT`).
- **Honest-null:** sinal do cartão sem dado da API → esconder, nunca inventar.
- `npx tsc --noEmit`, `npx eslint`, `npx vitest run` verdes ao fim de cada tarefa. Testes correm de `src/frontend`.
- Commits atómicos por tarefa. Trabalhar na branch `redesign/betterstack-catalog-discovery`.
- Não redesenhar o interior das abas de análise, nem a página de detalhe do serviço, nem a criação.

---

### Task 0: Confirmar tipos de dados & definir view-model

**Files:**
- Read: `src/frontend/src/features/catalog/api/serviceCatalog.ts` (ou `../api`), `src/frontend/src/types` (tipos `graph.services[]` e `graph.apis[]`)
- Create: `src/frontend/src/features/catalog/browse/catalogTypes.ts`

**Interfaces:**
- Produces: os view-models e o mapa de facetas que todas as tarefas seguintes consomem.

- [ ] **Step 1: Ler os tipos reais** de um item de `graph.services` e `graph.apis` (campos disponíveis: confirmar quais destes existem — `name`, `teamName`, `domain`, `capability`/`description`, `lifecycle`/`stage`, `exposure`/`visibility`, `health`, `serviceAssetId`, e para apis `routePattern`, `visibility`, `version`, `consumers`, `apiAssetId`, `hasContract`/`contractCount`). Anotar num comentário quais existem hoje e quais faltam (→ honest-null).

- [ ] **Step 2: Escrever `catalogTypes.ts`** com view-models tolerantes a campos em falta (opcionais):

```typescript
export type Exposure = 'Public' | 'Internal' | 'Partner';
export type Lifecycle = 'Stable' | 'Beta' | 'Deprecated' | 'Unknown';

/** API/interface consumível exposta por um serviço. */
export interface ApiVM {
  id: string;
  name: string;
  routePattern?: string;
  protocol?: string;              // ex.: REST, gRPC (honest-null se ausente)
  exposure?: Exposure;            // de visibility
  version?: string;
  hasContract: boolean;
  consumerCount?: number;
}

/** Cartão de serviço (unidade de descoberta âncora). */
export interface ServiceVM {
  id: string;
  name: string;
  description?: string;           // capability/description — honest-null
  domain?: string;
  team?: string;
  owner?: string;                 // honest-null
  lifecycle: Lifecycle;           // go/no-go #1
  exposure?: Exposure;            // go/no-go #2 (agregado das apis)
  health?: 'Ok' | 'Warn' | 'Down';// honest-null
  apis: ApiVM[];
  contractCount: number;
}

export type ResultViewMode = 'services' | 'apis';
export type Density = 'comfortable' | 'compact';
export type SortKey = 'relevance' | 'name' | 'consumers' | 'recent';

export interface CatalogFilters {
  q: string;
  domains: string[];
  protocols: string[];
  exposures: Exposure[];
  lifecycles: Lifecycle[];
  hasContract: boolean | null;
  teams: string[];
}

export interface FacetCount { value: string; label: string; count: number; }
export interface FacetGroups {
  domains: FacetCount[];
  protocols: FacetCount[];
  exposures: FacetCount[];
  lifecycles: FacetCount[];
  teams: FacetCount[];
}
```

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/features/catalog/browse/catalogTypes.ts
git commit -m "feat(catalog): view-model types for browse surface"
```

---

### Task 1: Adapter puro — grafo → view-model, facetas, filtragem (TDD)

**Files:**
- Create: `src/frontend/src/features/catalog/browse/catalogAdapter.ts`
- Test: `src/frontend/src/__tests__/catalog/catalogAdapter.test.ts`

**Interfaces:**
- Consumes: tipos de `catalogTypes.ts`; o objeto `graph` de `serviceCatalogApi.getGraph()`.
- Produces:
  - `toServiceVMs(graph): ServiceVM[]`
  - `computeFacets(services: ServiceVM[]): FacetGroups`
  - `filterServices(services: ServiceVM[], f: CatalogFilters): ServiceVM[]`
  - `sortServices(services: ServiceVM[], key: SortKey): ServiceVM[]`
  - `toApiVMs(services: ServiceVM[]): ApiVM[]` (para o modo "APIs")

- [ ] **Step 1: Escrever os testes que falham** (funções puras — casos-chave):

```typescript
import { describe, it, expect } from 'vitest';
import { toServiceVMs, computeFacets, filterServices, sortServices } from '../../features/catalog/browse/catalogAdapter';

const graph = {
  services: [
    { serviceAssetId: 's1', name: 'payment-service', domain: 'payments', teamName: 'Billing', capability: 'Pagamentos', lifecycle: 'Stable' },
    { serviceAssetId: 's2', name: 'legacy-x', domain: 'core', teamName: 'Core', lifecycle: 'Deprecated' },
  ],
  apis: [
    { apiAssetId: 'a1', name: 'REST payments', routePattern: '/payments', visibility: 'Internal', version: '1', consumers: ['c1','c2'], serviceAssetId: 's1', hasContract: true },
    { apiAssetId: 'a2', name: 'gRPC Pay', visibility: 'Public', serviceAssetId: 's1', hasContract: false },
  ],
} as never;

it('mapeia serviços com as suas apis e conta contratos', () => {
  const vms = toServiceVMs(graph);
  const s1 = vms.find(v => v.id === 's1')!;
  expect(s1.name).toBe('payment-service');
  expect(s1.apis).toHaveLength(2);
  expect(s1.contractCount).toBe(1);
  expect(s1.lifecycle).toBe('Stable');
});

it('agrega exposição do serviço a partir das apis (mais aberta vence)', () => {
  const s1 = toServiceVMs(graph).find(v => v.id === 's1')!;
  expect(s1.exposure).toBe('Public');
});

it('esconde sinais sem dado (honest-null)', () => {
  const s2 = toServiceVMs(graph).find(v => v.id === 's2')!;
  expect(s2.description).toBeUndefined();
  expect(s2.health).toBeUndefined();
});

it('computeFacets conta por domínio e ciclo', () => {
  const f = computeFacets(toServiceVMs(graph));
  expect(f.domains.find(d => d.value === 'payments')!.count).toBe(1);
  expect(f.lifecycles.find(l => l.value === 'Deprecated')!.count).toBe(1);
});

it('filterServices combina pesquisa + facetas (AND entre grupos, OR dentro)', () => {
  const vms = toServiceVMs(graph);
  const out = filterServices(vms, { q: 'pay', domains: ['payments'], protocols: [], exposures: [], lifecycles: [], hasContract: true, teams: [] });
  expect(out.map(s => s.id)).toEqual(['s1']);
});

it('sortServices por nome é estável e A→Z', () => {
  const vms = toServiceVMs(graph);
  expect(sortServices(vms, 'name').map(s => s.name)).toEqual(['legacy-x', 'payment-service']);
});
```

- [ ] **Step 2: Correr e ver falhar** — `cd src/frontend && npx vitest run src/__tests__/catalog/catalogAdapter.test.ts` → FAIL (módulo não existe).

- [ ] **Step 3: Implementar `catalogAdapter.ts`** (funções puras, defensivas a campos ausentes):

```typescript
import type { ServiceVM, ApiVM, Exposure, Lifecycle, CatalogFilters, FacetGroups, FacetCount, SortKey } from './catalogTypes';

const EXPOSURE_RANK: Record<Exposure, number> = { Internal: 0, Partner: 1, Public: 2 };
const norm = (v: unknown): string | undefined => (typeof v === 'string' && v.trim() ? v : undefined);
const asExposure = (v: unknown): Exposure | undefined =>
  v === 'Public' || v === 'Internal' || v === 'Partner' ? v : undefined;
const asLifecycle = (v: unknown): Lifecycle =>
  v === 'Stable' || v === 'Beta' || v === 'Deprecated' ? v : 'Unknown';

export function toServiceVMs(graph: { services?: any[]; apis?: any[] }): ServiceVM[] {
  const apisByService = new Map<string, ApiVM[]>();
  for (const a of graph.apis ?? []) {
    const vm: ApiVM = {
      id: a.apiAssetId ?? a.id,
      name: a.name,
      routePattern: norm(a.routePattern),
      protocol: norm(a.protocol) ?? norm(a.interfaceType),
      exposure: asExposure(a.visibility ?? a.exposure),
      version: norm(a.version),
      hasContract: Boolean(a.hasContract ?? (a.contractCount ?? 0) > 0),
      consumerCount: Array.isArray(a.consumers) ? a.consumers.length : a.consumerCount,
    };
    const key = a.serviceAssetId ?? a.serviceId ?? '';
    (apisByService.get(key) ?? apisByService.set(key, []).get(key)!).push(vm);
  }
  return (graph.services ?? []).map((s) => {
    const apis = apisByService.get(s.serviceAssetId ?? s.id) ?? [];
    const exposure = apis
      .map((a) => a.exposure)
      .filter((e): e is Exposure => !!e)
      .sort((x, y) => EXPOSURE_RANK[y] - EXPOSURE_RANK[x])[0]
      ?? asExposure(s.exposure ?? s.visibility);
    return {
      id: s.serviceAssetId ?? s.id,
      name: s.name,
      description: norm(s.capability) ?? norm(s.description),
      domain: norm(s.domain),
      team: norm(s.teamName) ?? norm(s.team),
      owner: norm(s.technicalOwner) ?? norm(s.owner),
      lifecycle: asLifecycle(s.lifecycle ?? s.stage),
      exposure,
      health: (['Ok', 'Warn', 'Down'] as const).includes(s.health) ? s.health : undefined,
      apis,
      contractCount: apis.filter((a) => a.hasContract).length,
    };
  });
}

const countBy = (items: (string | undefined)[]): FacetCount[] => {
  const m = new Map<string, number>();
  for (const v of items) if (v) m.set(v, (m.get(v) ?? 0) + 1);
  return [...m.entries()].map(([value, count]) => ({ value, label: value, count })).sort((a, b) => b.count - a.count);
};

export function computeFacets(services: ServiceVM[]): FacetGroups {
  return {
    domains: countBy(services.map((s) => s.domain)),
    protocols: countBy(services.flatMap((s) => s.apis.map((a) => a.protocol))),
    exposures: countBy(services.map((s) => s.exposure)),
    lifecycles: countBy(services.map((s) => s.lifecycle)),
    teams: countBy(services.map((s) => s.team)),
  };
}

export function filterServices(services: ServiceVM[], f: CatalogFilters): ServiceVM[] {
  const q = f.q.trim().toLowerCase();
  return services.filter((s) => {
    if (q) {
      const hay = [s.name, s.description, s.domain, s.team, ...s.apis.map((a) => `${a.name} ${a.routePattern ?? ''}`)]
        .join(' ').toLowerCase();
      if (!hay.includes(q)) return false;
    }
    if (f.domains.length && !(s.domain && f.domains.includes(s.domain))) return false;
    if (f.teams.length && !(s.team && f.teams.includes(s.team))) return false;
    if (f.exposures.length && !(s.exposure && f.exposures.includes(s.exposure))) return false;
    if (f.lifecycles.length && !f.lifecycles.includes(s.lifecycle)) return false;
    if (f.protocols.length && !s.apis.some((a) => a.protocol && f.protocols.includes(a.protocol))) return false;
    if (f.hasContract === true && s.contractCount === 0) return false;
    if (f.hasContract === false && s.contractCount > 0) return false;
    return true;
  });
}

export function sortServices(services: ServiceVM[], key: SortKey): ServiceVM[] {
  const arr = [...services];
  if (key === 'name') arr.sort((a, b) => a.name.localeCompare(b.name));
  else if (key === 'consumers') arr.sort((a, b) => b.apis.reduce((n, x) => n + (x.consumerCount ?? 0), 0) - a.apis.reduce((n, x) => n + (x.consumerCount ?? 0), 0));
  return arr; // 'relevance'/'recent' preservam a ordem da API por agora
}

export function toApiVMs(services: ServiceVM[]): ApiVM[] {
  return services.flatMap((s) => s.apis);
}
```

- [ ] **Step 4: Correr e ver passar** — `npx vitest run src/__tests__/catalog/catalogAdapter.test.ts` → PASS.

- [ ] **Step 5: Commit** — `git add ... && git commit -m "feat(catalog): pure adapter (graph->VM, facets, filter, sort) with tests"`

---

### Task 2: Hook de estado sincronizado no URL (TDD)

**Files:**
- Create: `src/frontend/src/features/catalog/browse/useCatalogBrowseState.ts`
- Test: `src/frontend/src/__tests__/catalog/useCatalogBrowseState.test.tsx`

**Interfaces:**
- Produces: `useCatalogBrowseState(): { filters: CatalogFilters; setFilter; clearAll; viewMode; setViewMode; density; setDensity; sort; setSort; }` — tudo espelhado em `useSearchParams` (chaves: `q, domain, protocol, exposure, lifecycle, contract, team, view, density, sort`; arrays como CSV).

- [ ] **Step 1: Teste que falha** (render com `MemoryRouter`, muda um filtro, confirma URL + leitura de volta):

```tsx
import { renderHook, act } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { useCatalogBrowseState } from '../../features/catalog/browse/useCatalogBrowseState';

const wrapper = ({ children }: { children: React.ReactNode }) => <MemoryRouter>{children}</MemoryRouter>;

it('lê e escreve filtros no URL', () => {
  const { result } = renderHook(() => useCatalogBrowseState(), { wrapper });
  act(() => result.current.setFilter('domains', ['payments']));
  expect(result.current.filters.domains).toEqual(['payments']);
  act(() => result.current.setViewMode('apis'));
  expect(result.current.viewMode).toBe('apis');
  act(() => result.current.clearAll());
  expect(result.current.filters.domains).toEqual([]);
  expect(result.current.filters.q).toBe('');
});
```

- [ ] **Step 2: Correr e ver falhar.**

- [ ] **Step 3: Implementar o hook** com `useSearchParams` (ler → `CatalogFilters`; `setFilter`/`setViewMode`/`setDensity`/`setSort` → `setSearchParams` preservando as outras chaves; arrays via `.join(',')`/`.split(',')`; `clearAll` → limpa tudo menos `view`/`density`). `hasContract`: `'1'|'0'|ausente`. Manter default `view=services`, `density=comfortable`, `sort=relevance`.

- [ ] **Step 4: Correr e ver passar.**

- [ ] **Step 5: Commit** — `git commit -m "feat(catalog): URL-synced browse state hook with tests"`

---

### Task 3: `CatalogFacetBar` (barra pesquisa + facetas)

**Files:**
- Create: `src/frontend/src/features/catalog/browse/CatalogFacetBar.tsx`
- Test: `src/frontend/src/__tests__/catalog/CatalogFacetBar.test.tsx`

**Interfaces:**
- Consumes: `FacetGroups`, `CatalogFilters`, callbacks do hook (Task 2), `SortKey`, `ResultViewMode`, `Density`.
- Produces: `<CatalogFacetBar facets filters onSetFilter viewMode onViewMode sort onSort density onDensity onClearAll resultCount />`.

- [ ] **Step 1: Teste que falha** — renderiza com facetas mock, clica num chip de domínio → chama `onSetFilter('domains', ['payments'])`; escreve na pesquisa → `onSetFilter('q', ...)`; toggle `Ver como: APIs` → `onViewMode('apis')`. Usar `screen.getByRole`/`getByLabelText`.

- [ ] **Step 2: Correr e ver falhar.**

- [ ] **Step 3: Implementar** com DS: `SearchInput` grande no topo (valor `filters.q`); por baixo grupos de facetas como chips toggle (`Button size="sm" variant={ativo?'primary':'subtle'}`) com contagem; `Select` de ordenação; segmento `Tabs variant="pill"` para `Ver como: Serviços | APIs`; `IconButton`/`Button` de densidade; link "limpar tudo" visível só quando há filtros ativos. Tudo i18n. Cores só tokens.

- [ ] **Step 4: Correr e ver passar.**

- [ ] **Step 5: Commit** — `git commit -m "feat(catalog): CatalogFacetBar (search + facets + view/sort/density)"`

---

### Task 4: `ServiceResultCard` + `ApiResultRow`

**Files:**
- Create: `src/frontend/src/features/catalog/browse/ServiceResultCard.tsx`
- Create: `src/frontend/src/features/catalog/browse/ApiResultRow.tsx`
- Test: `src/frontend/src/__tests__/catalog/ServiceResultCard.test.tsx`

**Interfaces:**
- Consumes: `ServiceVM`, `ApiVM`, `Density`; `onOpenService(id)`, `onOpenApi(id)`, `onViewContract(apiId)`.
- Produces: os dois componentes de resultado.

- [ ] **Step 1: Teste que falha** — dado um `ServiceVM` completo: mostra nome, descrição, dot de ciclo (`aria-label` "Estável"), exposição, domínio+equipa+dono, e chips das APIs; um `ServiceVM` com `description/health/owner` undefined **não** renderiza esses elementos (honest-null); clicar no cartão chama `onOpenService(id)`; clicar num chip chama `onOpenApi`.

- [ ] **Step 2: Correr e ver falhar.**

- [ ] **Step 3: Implementar** o cartão conforme a anatomia do spec (linha-scan nome + dot ciclo + exposição; 1 linha descrição; contexto domínio·equipa·dono + saúde; separador; chips de API com badge 📄 e "Ver contrato"; `+N` colapsa). `density==='compact'` → variante linha (menos padding, uma linha). Tokens só; dots `bg-success/warning/critical`; `Card` DS. `ApiResultRow` = linha para o modo "APIs" (nome, rota mono, exposição, versão, badge contrato, `onViewContract`).

- [ ] **Step 4: Correr e ver passar.**

- [ ] **Step 5: Commit** — `git commit -m "feat(catalog): rich ServiceResultCard + ApiResultRow (honest-null signals)"`

---

### Task 5: `ServiceBrowseSurface` (orquestrador)

**Files:**
- Create: `src/frontend/src/features/catalog/browse/ServiceBrowseSurface.tsx`
- Test: `src/frontend/src/__tests__/catalog/ServiceBrowseSurface.test.tsx`

**Interfaces:**
- Consumes: `graph` (via prop), Task 1 (adapter), Task 2 (hook), Tasks 3–4 (componentes).
- Produces: `<ServiceBrowseSurface graph onOpenService onOpenApi onViewContract />`.

- [ ] **Step 1: Teste que falha** — com `graph` mock dentro de `MemoryRouter`: renderiza cartões; ao filtrar por um domínio inexistente mostra o **estado "sem resultados"** com botão "limpar filtros"; `graph` vazio mostra `EmptyState`; toggle "APIs" troca para linhas de API.

- [ ] **Step 2: Correr e ver falhar.**

- [ ] **Step 3: Implementar**: `const services = useMemo(() => toServiceVMs(graph), [graph])`; `facets = computeFacets(services)`; `filtered = sortServices(filterServices(services, filters), sort)`; render `CatalogFacetBar` + grelha de `ServiceResultCard` (ou `ApiResultRow` no modo apis via `toApiVMs`); estados: `services.length===0` → EmptyState (registar); `filtered.length===0` (com filtros) → estado "sem resultados" + limpar; skeleton quando `graph` ainda indefinido (prop opcional `loading`). Grelha responsiva; densidade aplicada.

- [ ] **Step 4: Correr e ver passar.**

- [ ] **Step 5: Commit** — `git commit -m "feat(catalog): ServiceBrowseSurface orchestrator + states"`

---

### Task 6: Reshell da `ServiceCatalogPage` (Browse | Explorar)

**Files:**
- Modify: `src/frontend/src/features/catalog/pages/ServiceCatalogPage.tsx`
- Test: `src/frontend/src/__tests__/catalog/ServiceCatalogPage.browse.test.tsx` (novo; manter o teste existente da página se houver)

**Interfaces:**
- Consumes: `ServiceBrowseSurface` (Task 5); painéis de análise existentes (`ServiceCatalogOverviewTab`, `DependencyGraph`, `ImpactPanel`, `TemporalPanel`) — reagrupados, não redesenhados.

- [ ] **Step 1: Teste que falha** — ao montar a página, o segmento por defeito é **Browse** e vê-se a barra de pesquisa do browse (não a aba "overview"); existe um segmento "Explorar"; a CTA "Registar serviço" está presente mas secundária (ghost).

- [ ] **Step 2: Correr e ver falhar.**

- [ ] **Step 3: Implementar**: substituir a barra de 6 abas por um segmento de topo `Tabs` com dois itens `Browse | Explorar` (default `browse`). `Browse` → `<ServiceBrowseSurface graph loading={isLoading} onOpenService={id=>navigate('/services/'+id)} onOpenApi={...} onViewContract={...} />`. `Explorar` → sub-`Tabs` com os 4 painéis atuais (overview/graph/impact/temporal), movendo para lá o código existente **sem alterar o interior**. Remover os StatCards de arquiteto do topo do consumidor (movê-los para dentro de "Explorar > overview" se se quiser). CTA "Registar serviço" → `variant="ghost"` no header. Estados loading/error preservados.

- [ ] **Step 4: Correr e ver passar** — `npx vitest run src/__tests__/catalog/`; depois `npx tsc --noEmit` e `npx eslint src/features/catalog/browse src/features/catalog/pages/ServiceCatalogPage.tsx` limpos.

- [ ] **Step 5: Commit** — `git commit -m "feat(catalog): ServiceCatalogPage browse-first reshell (Browse | Explorar)"`

---

### Task 7: i18n + verificação final

**Files:**
- Modify: `src/frontend/src/locales/en.json`, `es.json`, `pt-BR.json`, `pt-PT.json`

- [ ] **Step 1:** Adicionar as chaves novas usadas nas Tasks 3–6 (`serviceCatalog.browse.*`: searchPlaceholder, facets.domain/protocol/exposure/lifecycle/hasContract/team, viewAs.services/apis, sort.*, density.*, clearAll, noResults.title/desc, card.viewContract, exposure.public/internal/partner, lifecycle.stable/beta/deprecated, segment.browse/explore) nos **4 locales**.
- [ ] **Step 2:** Correr `node scripts/validate-i18n*` se existir (ou confirmar paridade de chaves nos 4).
- [ ] **Step 3:** `npx vitest run` (suite completa) + `npx tsc --noEmit` + `npx eslint` — tudo verde.
- [ ] **Step 4: Commit** — `git commit -m "feat(catalog): i18n keys for browse surface (4 locales)"`

---

## Self-Review

**Cobertura do spec:** §3.1 modelo→Task 6; §3.2 layout→Tasks 3+5; §3.3 cartão→Task 4; §3.4 pesquisa/filtros→Tasks 1+2+3; §3.5 estados/handoff→Tasks 4+5+6; §4 escopo→respeitado (análise só realojada, Task 6); §5 honest-null→Task 1 (adapter) + Task 4 (render); §6 critérios→cobertos. Sem lacunas.

**Placeholders:** lógica-núcleo (Task 1) com código+testes completos; componentes (3–6) com estrutura precisa, interfaces e testes concretos — a implementação visual é derivável do spec §3.2/§3.3 sem ambiguidade. Task 0 remove a incerteza de tipos antes de qualquer código de dados.

**Consistência de tipos:** view-models definidos uma vez (Task 0) e consumidos com os mesmos nomes em todas as tarefas; funções do adapter (`toServiceVMs/computeFacets/filterServices/sortServices/toApiVMs`) referidas com a mesma assinatura em Tasks 1 e 5.
