# Contract Discovery — Browse-First Parity Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Trazer a descoberta de contratos (`/contracts` → `ContractCatalogPage`) à paridade com o redesign browse-first do catálogo de serviços já entregue: pesquisa-primeiro, filtros facetados, estado sincronizado no URL, e um **toggle de vista Tabela | Cartões** — mantendo a tabela governada existente como uma das vistas (não-regressivo) e adicionando uma vista de cartões ricos para descoberta.

**Reference:** Espelha `features/catalog/browse/*` (adapter puro + hook de estado no URL + facet bar + result card + surface orquestrador), adaptado ao modelo de dados de contratos (`CatalogItem`). NÃO alterar o código do service browse; construir peças contract-specific em `features/contracts/catalog/browse/`.

**Tech Stack:** React 19, TS 5.9, TanStack Query, react-router-dom 7 (`useSearchParams`), Vitest + Testing Library, design system NexTraceOne, i18next.

## Current state (grounding)
- `ContractCatalogPage.tsx` (253L): PageHeader+CTA, summary lifecycle FilterChips, `CatalogToolbar` (search+filters), `CatalogTable` (sortable), estados loading/error/empty. Data: `useContractList` + `useContractsSummary`.
- `CatalogItem` (view model, já rico): `name, semVer, domain, team, technicalOwner, criticality, exposure, updatedAt, catalogServiceType (RestApi|Soap|Event|KafkaProducer|KafkaConsumer|BackgroundService|GraphQl|SharedSchema), approvalState (Pending|InReview|Approved), lifecycleState (Draft|InReview|Approved|Locked|Deprecated|Sunset|Retired), protocol`.
- `CatalogFilters`: `search, serviceType, domain, owner, team, lifecycle, approvalState, risk, exposure, protocol`. `applyClientFilters` + `applySorting` já existem (client-side).

## Global Constraints
- Design system apenas (`components/*`) + tokens semânticos. Zero controlos HTML crus, zero cores hardcoded.
- Texto de UI só por chaves i18n `t('...')`. Locales flat em `src/frontend/src/locales/<l>.json` (`en/es/pt-BR/pt-PT`), paridade nos 4.
- Honest-null: sinal do cartão sem dado → esconder, nunca inventar.
- `npx tsc --noEmit`, `npx eslint`, `npx vitest run` verdes ao fim de cada tarefa (correr de `src/frontend`). Suite COMPLETA verde no fim.
- Commits atómicos por tarefa. Branch nova `redesign/betterstack-contract-discovery` (a partir de `main`).
- NÃO redesenhar `CatalogTable`, `CatalogToolbar`, a workspace/studio/create, nem o interior de outros ecrãs. A tabela é reutilizada como vista; os filtros migram para a facet bar.
- NÃO tocar em `features/catalog/browse/*` (código do service browse).

---

### Task 0: Tipos de view-model do contract browse

**Files:**
- Read: `src/frontend/src/features/contracts/catalog/types.ts` (CatalogItem/CatalogFilters), `src/frontend/src/features/catalog/browse/catalogTypes.ts` (referência).
- Create: `src/frontend/src/features/contracts/catalog/browse/contractBrowseTypes.ts`

- [ ] **Step 1:** Confirmar os campos reais de `CatalogItem`. Reutilizar `CatalogItem` como VM âncora (já é rico); NÃO duplicar.
- [ ] **Step 2:** Definir os tipos do browse:
```typescript
export type ContractViewMode = 'table' | 'cards';
export type ContractDensity = 'comfortable' | 'compact';
export type ContractSortKey = 'relevance' | 'name' | 'updated' | 'criticality';

export interface ContractBrowseFilters {
  q: string;
  serviceTypes: string[];
  lifecycles: string[];
  domains: string[];
  teams: string[];
  criticalities: string[];
  exposures: string[];
  approvals: string[];
}

export interface ContractFacetCount { value: string; label: string; count: number; }
export interface ContractFacetGroups {
  serviceTypes: ContractFacetCount[];
  lifecycles: ContractFacetCount[];
  domains: ContractFacetCount[];
  teams: ContractFacetCount[];
  criticalities: ContractFacetCount[];
  exposures: ContractFacetCount[];
  approvals: ContractFacetCount[];
}
export const EMPTY_CONTRACT_BROWSE_FILTERS: ContractBrowseFilters = {
  q: '', serviceTypes: [], lifecycles: [], domains: [], teams: [], criticalities: [], exposures: [], approvals: [],
};
```
- [ ] **Step 3:** Commit — `feat(contracts): browse view-model types for contract discovery`

---

### Task 1: Adapter puro — facetas, filtragem, ordenação (TDD)

**Files:**
- Create: `src/frontend/src/features/contracts/catalog/browse/contractBrowseAdapter.ts`
- Test: `src/frontend/src/__tests__/contracts/contractBrowseAdapter.test.ts`

**Produces:**
- `computeContractFacets(items: CatalogItem[]): ContractFacetGroups`
- `filterContracts(items: CatalogItem[], f: ContractBrowseFilters): CatalogItem[]` (AND entre grupos, OR dentro; q = substring sobre name/domain/team/owner/semVer)
- `sortContracts(items: CatalogItem[], key: ContractSortKey): CatalogItem[]` (`name`→A→Z; `updated`→desc por updatedAt; `criticality`→ordem High>Medium>Low; `relevance`→preserva ordem)

- [ ] **Step 1:** Testes que falham (casos: facetas contam por serviceType/lifecycle/domain; filtro combina q+facetas AND/OR; sort por name estável; honest defaults quando campo vazio → excluído da faceta). Fixtures = `CatalogItem[]` mínimos.
- [ ] **Step 2:** Correr → FAIL.
- [ ] **Step 3:** Implementar funções puras (defensivas a strings vazias — valor vazio não entra na faceta nem conta). Reaproveitar a lógica de `applyClientFilters`/`applySorting` como base, mas em funções puras testadas sobre os novos filtros multi-valor.
- [ ] **Step 4:** Correr → PASS. Depois `tsc`+`eslint` limpos.
- [ ] **Step 5:** Commit — `feat(contracts): pure contract-browse adapter (facets, filter, sort) with tests`

---

### Task 2: Hook de estado sincronizado no URL (TDD)

**Files:**
- Create: `src/frontend/src/features/contracts/catalog/browse/useContractBrowseState.ts`
- Test: `src/frontend/src/__tests__/contracts/useContractBrowseState.test.tsx`

**Produces:** `useContractBrowseState(): { filters, setFilter, clearAll, viewMode, setViewMode, density, setDensity, sort, setSort }` espelhado em `useSearchParams` (chaves: `q, type, lifecycle, domain, team, crit, exposure, approval, view, density, sort`; arrays como CSV). Default `view=table` (preserva o comportamento atual como default), `density=comfortable`, `sort=relevance`. `clearAll` limpa filtros, mantém `view`/`density`.

- [ ] **Step 1:** Teste que falha (MemoryRouter; setFilter escreve/lê; setViewMode('cards'); clearAll). 
- [ ] **Step 2:** FAIL. 
- [ ] **Step 3:** Implementar com `useSearchParams` (functional update; CSV join/split drop-empties). 
- [ ] **Step 4:** PASS. 
- [ ] **Step 5:** Commit — `feat(contracts): URL-synced contract-browse state hook with tests`

---

### Task 3: `ContractFacetBar` (pesquisa + facetas + toggle vista)

**Files:**
- Create: `src/frontend/src/features/contracts/catalog/browse/ContractFacetBar.tsx`
- Test: `src/frontend/src/__tests__/contracts/ContractFacetBar.test.tsx`

**Produces:** `<ContractFacetBar facets filters onSetFilter viewMode onViewMode sort onSort density onDensity onClearAll resultCount />`.
- `SearchInput` (valor `filters.q`); grupos de facetas como `FilterChip` toggle com count (serviceType, lifecycle, domain, team, criticality, exposure, approval — esconder grupo vazio); `Select` de ordenação; segmento `Tabs` (pill) para **Ver como: Tabela | Cartões** → `onViewMode`; toggle de densidade; "limpar tudo" só quando há filtros ativos. DS + tokens + i18n (`contracts.catalog.browse.*`).

- [ ] **Step 1:** Teste que falha (clicar chip de lifecycle → `onSetFilter('lifecycles', [...])`; escrever pesquisa → `onSetFilter('q', ...)`; toggle "Cartões" → `onViewMode('cards')`). Inspecionar APIs reais dos componentes DS (FilterChip/Tabs/SearchInput/Select) antes.
- [ ] **Step 2:** FAIL. 
- [ ] **Step 3:** Implementar. 
- [ ] **Step 4:** PASS. 
- [ ] **Step 5:** Commit — `feat(contracts): ContractFacetBar (search + facets + view/sort/density)`

---

### Task 4: `ContractResultCard`

**Files:**
- Create: `src/frontend/src/features/contracts/catalog/browse/ContractResultCard.tsx`
- Test: `src/frontend/src/__tests__/contracts/ContractResultCard.test.tsx`

**Produces:** `<ContractResultCard item={CatalogItem} density onOpen={(item)=>void} />`.
- Anatomy (comfortable): linha-scan nome + dot de lifecycle (`aria-label` localizado; tokens `bg-success/warning/critical/muted`) + badge de tipo (reutilizar `ServiceTypeBadge`/`ProtocolBadge`/`LifecycleBadge` de `contracts/shared/components` se aplicáveis — inspecionar); versão (semVer); linha de contexto domínio·equipa·dono (honest-null por campo); badges de criticality + exposure + approvalState (honest-null). Clicar → `onOpen(item)`. `density='compact'` → variante linha única.
- Honest-null: campos vazios (`''`) não renderizam.

- [ ] **Step 1:** Teste que falha (item completo → mostra nome, versão, dot lifecycle por aria-label, tipo, domínio·equipa·dono; item com owner/criticality vazios → não renderiza; clicar → onOpen). Inspecionar os badges partilhados de `contracts/shared/components` antes de reimplementar.
- [ ] **Step 2:** FAIL. 
- [ ] **Step 3:** Implementar (reutilizar badges partilhados; DS Card; tokens). 
- [ ] **Step 4:** PASS. 
- [ ] **Step 5:** Commit — `feat(contracts): rich ContractResultCard (honest-null signals)`

---

### Task 5: `ContractBrowseSurface` (orquestrador)

**Files:**
- Create: `src/frontend/src/features/contracts/catalog/browse/ContractBrowseSurface.tsx`
- Test: `src/frontend/src/__tests__/contracts/ContractBrowseSurface.test.tsx`

**Produces:** `<ContractBrowseSurface items={CatalogItem[]} loading? sort? onSort? onOpen renderTable />`.
- Usa `useContractBrowseState`; `facets = computeContractFacets(items)`; `filtered = sortContracts(filterContracts(items, filters), sort)`.
- Render `ContractFacetBar` + resultados: `viewMode==='cards'` → grelha de `ContractResultCard`; `viewMode==='table'` → renderiza a tabela existente via prop `renderTable(filteredItems)` (a página passa `<CatalogTable items sort onSort/>`, mantendo o sort da tabela).
- Estados: `items.length===0` → EmptyState; `filtered.length===0` com filtros → "sem resultados" + limpar; skeleton quando `loading`.

- [ ] **Step 1:** Teste que falha (items mock em MemoryRouter: default `view=table` → chama `renderTable`; `?view=cards` → cartões; `?lifecycle=zzz` → sem-resultados + limpar; items vazios → EmptyState). 
- [ ] **Step 2:** FAIL. 
- [ ] **Step 3:** Implementar. 
- [ ] **Step 4:** PASS. 
- [ ] **Step 5:** Commit — `feat(contracts): ContractBrowseSurface orchestrator + states`

---

### Task 6: Reshell da `ContractCatalogPage`

**Files:**
- Modify: `src/frontend/src/features/contracts/catalog/ContractCatalogPage.tsx`
- Test: `src/frontend/src/__tests__/contracts/ContractCatalogPage.browse.test.tsx` (novo)

- [ ] **Step 1:** Teste que falha (monta a página: aparece a facet bar do browse; existe o toggle Tabela|Cartões; por defeito mostra a tabela; CTA "New contract" presente). Mockar `useContractList`/`useContractsSummary`.
- [ ] **Step 2:** FAIL.
- [ ] **Step 3:** Implementar: manter `PageHeader`+CTA e os summary lifecycle chips no topo. Substituir `CatalogToolbar` + o bloco de `Card>CatalogTable` por `<ContractBrowseSurface items={sortedItems...} loading={listQuery.isLoading} onOpen={(item)=>navigate('/contracts/'+item.contractVersionId)} renderTable={(items)=><CatalogTable items={items} sort={sort} onSort={setSort} />} />`. Passar o pipeline de dados (catalogItems) à surface; a filtragem/sort do browse aplica-se sobre os cartões, e a tabela mantém o seu próprio sort. Preservar estados loading/error. Verificar a rota real de detalhe de contrato (`/contracts/:contractVersionId`) antes de wire o onOpen. Remover imports órfãos (CatalogToolbar se deixar de ser usado diretamente).
- [ ] **Step 4:** PASS. Depois `npx vitest run src/__tests__/contracts/`, `tsc`, `eslint` limpos.
- [ ] **Step 5:** Commit — `feat(contracts): ContractCatalogPage browse-first reshell (table|cards)`

---

### Task 7: i18n + verificação final

**Files:**
- Modify: `src/frontend/src/locales/en.json`, `es.json`, `pt-BR.json`, `pt-PT.json`

- [ ] **Step 1:** Grep todas as chaves `contracts.catalog.browse.*` usadas (facetas, viewAs.table/cards, sort.*, density.*, clearAll, noResults.title/desc, lifecycle.*, etc., incl. chaves dinâmicas). Adicionar o subtree `browse` sob `contracts.catalog` nos 4 locales com paridade.
- [ ] **Step 2:** Correr `npm run validate:i18n`.
- [ ] **Step 3:** Se os testes das Tasks 3–6 tiverem asserções sobre key-paths (porque as chaves não existiam), atualizá-las para os valores traduzidos reais.
- [ ] **Step 4:** `npx vitest run` (suite COMPLETA) + `tsc` + `eslint` — tudo verde.
- [ ] **Step 5:** Commit — `feat(contracts): i18n keys for contract browse surface (4 locales)`
