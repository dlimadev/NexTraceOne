# Redesign da Navegação do Catálogo — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Substituir a secção "Catálogo" da sidebar (19 itens planos com duplo-destaque por prefix-matching) por 5 roots ancorados + uma sub-nav de área por cada área (`/services`, `/contracts`), eliminando o duplo-destaque e o "teletransporte" do item ativo.

**Architecture:** A sidebar passa a listar apenas os 5 destinos-raiz do catálogo. Cada área (`/services`, `/contracts`) ganha um layout wrapper (`ServicesAreaLayout`, `ContractsAreaLayout`) que renderiza um componente reutilizável `AreaSubNav` (config-driven, i18n, `NavLink`) seguido de `<Outlet/>`. As rotas de nível de área são aninhadas sob esses layouts; as rotas de detalhe (`/services/:id`, `/contracts/:id`) ficam fora do wrapper mas o seu root permanece destacado na sidebar por prefix-matching. Dois strays `/catalog/*` são renomeados para os roots naturais com redirects.

**Tech Stack:** React 19, TypeScript 5.9, react-router-dom 7, react-i18next, Vitest + Testing Library, Vite 7.

## Global Constraints

- Idioma de UI: **nunca** strings hardcoded — sempre chaves i18n (`t(...)`). XML/doc e comentários inline em Português; identificadores em Inglês.
- 4 locales obrigatórios e sincronizados: `src/frontend/src/locales/{en,es,pt-BR,pt-PT}.json`. Toda a chave nova existe nos 4.
- `TreatWarningsAsErrors` no frontend: `npm run lint` e `tsc` têm de passar com zero erros/warnings.
- Prefix-matching de rota: os 5 roots (`/services`, `/contracts`, `/portal`, `/source-of-truth`, `/knowledge`) são disjuntos — nenhum é prefixo de outro.
- `NavLink` do item de lista de cada sub-nav usa `end` (só ativo no path exato); os restantes usam prefix (default).
- Rotas antigas continuam a funcionar via `<Navigate replace/>` — deep links guardados não quebram.
- Mudanças cirúrgicas: tocar apenas o necessário; não refatorar código adjacente.
- Comandos de gate correm a partir de `src/frontend` (o cwd do shell reinicia — prefixar `Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend";` em cada comando PowerShell).
- Convenção de commit: cada mensagem termina com `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

---

## File Structure

**Novos:**
- `src/frontend/src/features/catalog/components/AreaSubNav.tsx` — componente de sub-nav reutilizável (config-driven).
- `src/frontend/src/features/catalog/layouts/ServicesAreaLayout.tsx` — wrapper `AreaSubNav` (services) + `<Outlet/>`.
- `src/frontend/src/features/catalog/layouts/ContractsAreaLayout.tsx` — wrapper `AreaSubNav` (contracts) + `<Outlet/>`.
- `src/frontend/src/features/catalog/components/__tests__/AreaSubNav.test.tsx` — testes de estado ativo.
- `src/frontend/src/features/catalog/layouts/__tests__/AreaLayouts.test.tsx` — testes de render dos layouts.
- `src/frontend/src/routes/__tests__/catalogNavRedirects.test.tsx` — testes de redirect das rotas renomeadas.

**Modificados:**
- `src/frontend/src/components/shell/AppSidebar.tsx` — reduzir secção `catalog` a 5 roots.
- `src/frontend/src/routes/catalogRoutes.tsx` — aninhar `/services/*` sob `ServicesAreaLayout`; renomear `/catalog/developer-experience-score` → `/services/experience` (+ redirect).
- `src/frontend/src/routes/contractsRoutes.tsx` — aninhar `/contracts/*` de área sob `ContractsAreaLayout`; adicionar `/contracts/pipeline` (+ redirect de `/catalog/contracts/pipeline` em catalogRoutes).
- `src/frontend/src/features/catalog/components/ServiceScoreTab.tsx:210` — atualizar link para `/services/experience`.
- `src/frontend/src/locales/{en,es,pt-BR,pt-PT}.json` — chaves `catalogAreaNav.*` e `contractsAreaNav.*`.

---

## Task 1: Componente AreaSubNav

**Files:**
- Create: `src/frontend/src/features/catalog/components/AreaSubNav.tsx`
- Test: `src/frontend/src/features/catalog/components/__tests__/AreaSubNav.test.tsx`

**Interfaces:**
- Consumes: `react-router-dom` `NavLink`; `react-i18next` `useTranslation`; `../../../lib/cn`.
- Produces:
  - `export interface AreaSubNavItem { labelKey: string; to: string; end?: boolean }`
  - `export function AreaSubNav({ items, ariaLabelKey }: { items: AreaSubNavItem[]; ariaLabelKey: string }): JSX.Element`
  - Cada item renderiza um `NavLink to={item.to} end={item.end}` com `role`/`aria-current` naturais do `NavLink`; a classe ativa aplica `data-active="true"` para asserção em testes.

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/features/catalog/components/__tests__/AreaSubNav.test.tsx
import { describe, it, expect } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../../../../__tests__/test-utils';
import { AreaSubNav, type AreaSubNavItem } from '../AreaSubNav';

const items: AreaSubNavItem[] = [
  { labelKey: 'catalogAreaNav.catalog', to: '/services', end: true },
  { labelKey: 'catalogAreaNav.graph', to: '/services/graph' },
  { labelKey: 'catalogAreaNav.discovery', to: '/services/discovery' },
];

describe('AreaSubNav', () => {
  it('renders all tabs with translated labels', () => {
    renderWithProviders(<AreaSubNav items={items} ariaLabelKey="catalogAreaNav.ariaLabel" />, {
      routerProps: { initialEntries: ['/services'] },
    });
    expect(screen.getByText('Catalog')).toBeInTheDocument();
    expect(screen.getByText('Graph')).toBeInTheDocument();
    expect(screen.getByText('Discovery')).toBeInTheDocument();
  });

  it('marks only the exact list root active on /services (end)', () => {
    renderWithProviders(<AreaSubNav items={items} ariaLabelKey="catalogAreaNav.ariaLabel" />, {
      routerProps: { initialEntries: ['/services'] },
    });
    expect(screen.getByText('Catalog').closest('a')).toHaveAttribute('data-active', 'true');
    expect(screen.getByText('Graph').closest('a')).toHaveAttribute('data-active', 'false');
  });

  it('does not activate the list root on a sub-path, activates the sub-tab', () => {
    renderWithProviders(<AreaSubNav items={items} ariaLabelKey="catalogAreaNav.ariaLabel" />, {
      routerProps: { initialEntries: ['/services/discovery'] },
    });
    expect(screen.getByText('Catalog').closest('a')).toHaveAttribute('data-active', 'false');
    expect(screen.getByText('Discovery').closest('a')).toHaveAttribute('data-active', 'true');
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend"; npx vitest run src/features/catalog/components/__tests__/AreaSubNav.test.tsx`
Expected: FAIL — `Cannot find module '../AreaSubNav'`.

- [ ] **Step 3: Write minimal implementation**

```tsx
// src/frontend/src/features/catalog/components/AreaSubNav.tsx
import { NavLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { cn } from '../../../lib/cn';

/** Item de uma sub-nav de área do catálogo. */
export interface AreaSubNavItem {
  labelKey: string;
  to: string;
  /** Só ativo no path exato (usar no separador de lista/raiz). */
  end?: boolean;
}

interface AreaSubNavProps {
  items: AreaSubNavItem[];
  ariaLabelKey: string;
}

/** Barra de separadores persistente no topo de uma área (services/contracts). */
export function AreaSubNav({ items, ariaLabelKey }: AreaSubNavProps) {
  const { t } = useTranslation();
  return (
    <nav
      aria-label={t(ariaLabelKey)}
      className="flex items-center gap-1 overflow-x-auto border-b border-edge px-6"
    >
      {items.map(item => (
        <NavLink
          key={item.to}
          to={item.to}
          end={item.end}
          className={({ isActive }) =>
            cn(
              'shrink-0 border-b-2 px-3 py-2.5 text-sm transition-colors duration-150',
              isActive
                ? 'border-accent text-accent font-medium'
                : 'border-transparent text-body hover:text-heading',
            )
          }
          data-active={({ isActive }: { isActive: boolean }) => (isActive ? 'true' : 'false')}
        >
          {t(item.labelKey)}
        </NavLink>
      ))}
    </nav>
  );
}
```

> Nota: `NavLink` não aceita função em atributos DOM arbitrários. Substituir a linha `data-active={...}` por render explícito (ver correção no Step 3b).

- [ ] **Step 3b: Corrigir `data-active` (NavLink não passa função a atributos DOM)**

Substituir o `NavLink` do Step 3 por esta forma que usa `children`/`className` como função e injeta `data-active` via wrapper:

```tsx
        <NavLink
          key={item.to}
          to={item.to}
          end={item.end}
          className={({ isActive }) =>
            cn(
              'group shrink-0 border-b-2 px-3 py-2.5 text-sm transition-colors duration-150',
              isActive
                ? 'border-accent text-accent font-medium'
                : 'border-transparent text-body hover:text-heading',
            )
          }
        >
          {({ isActive }) => (
            <span data-active={isActive ? 'true' : 'false'}>{t(item.labelKey)}</span>
          )}
        </NavLink>
```

E ajustar o teste para `screen.getByText('Catalog').closest('span')` ou manter `.closest('a')`? O `data-active` fica no `<span>` interno. Atualizar as asserções do Step 1 para: `screen.getByText('Catalog').getAttribute('data-active')` (o `getByText` devolve o próprio `<span>` que carrega o atributo). Reescrever as três asserções de estado activo como:

```tsx
expect(screen.getByText('Catalog')).toHaveAttribute('data-active', 'true');
expect(screen.getByText('Graph')).toHaveAttribute('data-active', 'false');
```

- [ ] **Step 4: Run test to verify it passes**

Run: `Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend"; npx vitest run src/features/catalog/components/__tests__/AreaSubNav.test.tsx`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/catalog/components/AreaSubNav.tsx src/frontend/src/features/catalog/components/__tests__/AreaSubNav.test.tsx
git commit -m "feat(catalog): AreaSubNav — sub-nav de área config-driven

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 2: i18n — chaves da sub-nav nos 4 locales

**Files:**
- Modify: `src/frontend/src/locales/en.json`
- Modify: `src/frontend/src/locales/es.json`
- Modify: `src/frontend/src/locales/pt-BR.json`
- Modify: `src/frontend/src/locales/pt-PT.json`

**Interfaces:**
- Produces: dois objetos de topo `catalogAreaNav` e `contractsAreaNav` em cada locale, com as chaves consumidas pelas configs das Tasks 3/4.

- [ ] **Step 1: Adicionar `catalogAreaNav` e `contractsAreaNav` ao `en.json`**

Inserir como objetos de topo (a seguir a qualquer chave existente, respeitando vírgulas JSON):

```json
  "catalogAreaNav": {
    "ariaLabel": "Service catalog sub-navigation",
    "catalog": "Catalog",
    "graph": "Graph",
    "discovery": "Discovery",
    "maturity": "Maturity",
    "experience": "Experience",
    "featureFlags": "Feature Flags",
    "legacy": "Legacy"
  },
  "contractsAreaNav": {
    "ariaLabel": "Contract catalog sub-navigation",
    "catalog": "Catalog",
    "governance": "Governance",
    "health": "Health",
    "rulesets": "Rulesets",
    "canonical": "Canonical",
    "publication": "Publication",
    "pipeline": "Pipeline",
    "cdct": "CDCT"
  },
```

- [ ] **Step 2: Adicionar as mesmas chaves ao `es.json`**

```json
  "catalogAreaNav": {
    "ariaLabel": "Subnavegación del catálogo de servicios",
    "catalog": "Catálogo",
    "graph": "Grafo",
    "discovery": "Descubrimiento",
    "maturity": "Madurez",
    "experience": "Experiencia",
    "featureFlags": "Feature Flags",
    "legacy": "Heredados"
  },
  "contractsAreaNav": {
    "ariaLabel": "Subnavegación del catálogo de contratos",
    "catalog": "Catálogo",
    "governance": "Gobernanza",
    "health": "Salud",
    "rulesets": "Reglas",
    "canonical": "Canónicas",
    "publication": "Publicación",
    "pipeline": "Pipeline",
    "cdct": "CDCT"
  },
```

- [ ] **Step 3: Adicionar as mesmas chaves ao `pt-BR.json`**

```json
  "catalogAreaNav": {
    "ariaLabel": "Subnavegação do catálogo de serviços",
    "catalog": "Catálogo",
    "graph": "Grafo",
    "discovery": "Descoberta",
    "maturity": "Maturidade",
    "experience": "Experiência",
    "featureFlags": "Feature Flags",
    "legacy": "Legados"
  },
  "contractsAreaNav": {
    "ariaLabel": "Subnavegação do catálogo de contratos",
    "catalog": "Catálogo",
    "governance": "Governança",
    "health": "Saúde",
    "rulesets": "Regras",
    "canonical": "Canônicas",
    "publication": "Publicação",
    "pipeline": "Pipeline",
    "cdct": "CDCT"
  },
```

- [ ] **Step 4: Adicionar as mesmas chaves ao `pt-PT.json`**

```json
  "catalogAreaNav": {
    "ariaLabel": "Subnavegação do catálogo de serviços",
    "catalog": "Catálogo",
    "graph": "Grafo",
    "discovery": "Descoberta",
    "maturity": "Maturidade",
    "experience": "Experiência",
    "featureFlags": "Feature Flags",
    "legacy": "Legados"
  },
  "contractsAreaNav": {
    "ariaLabel": "Subnavegação do catálogo de contratos",
    "catalog": "Catálogo",
    "governance": "Governança",
    "health": "Saúde",
    "rulesets": "Regras",
    "canonical": "Canónicas",
    "publication": "Publicação",
    "pipeline": "Pipeline",
    "cdct": "CDCT"
  },
```

- [ ] **Step 5: Validar JSON**

Run: `Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend"; node -e "['en','es','pt-BR','pt-PT'].forEach(l=>{const j=require('./src/locales/'+l+'.json'); if(!j.catalogAreaNav||!j.contractsAreaNav) throw new Error('missing '+l); console.log(l,'ok')})"`
Expected: `en ok`, `es ok`, `pt-BR ok`, `pt-PT ok`.

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/locales/en.json src/frontend/src/locales/es.json src/frontend/src/locales/pt-BR.json src/frontend/src/locales/pt-PT.json
git commit -m "feat(catalog): i18n keys catalogAreaNav/contractsAreaNav (4 locales)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 3: Layouts de área (ServicesAreaLayout, ContractsAreaLayout)

**Files:**
- Create: `src/frontend/src/features/catalog/layouts/ServicesAreaLayout.tsx`
- Create: `src/frontend/src/features/catalog/layouts/ContractsAreaLayout.tsx`
- Test: `src/frontend/src/features/catalog/layouts/__tests__/AreaLayouts.test.tsx`

**Interfaces:**
- Consumes: `AreaSubNav`, `AreaSubNavItem` (Task 1); `react-router-dom` `Outlet`; chaves i18n (Task 2).
- Produces:
  - `export function ServicesAreaLayout(): JSX.Element` — `AreaSubNav` (services) + `<Outlet/>`.
  - `export function ContractsAreaLayout(): JSX.Element` — `AreaSubNav` (contracts) + `<Outlet/>`.

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/features/catalog/layouts/__tests__/AreaLayouts.test.tsx
import { describe, it, expect } from 'vitest';
import { screen } from '@testing-library/react';
import { Routes, Route } from 'react-router-dom';
import { renderWithProviders } from '../../../../__tests__/test-utils';
import { ServicesAreaLayout } from '../ServicesAreaLayout';
import { ContractsAreaLayout } from '../ContractsAreaLayout';

describe('ServicesAreaLayout', () => {
  it('renders the services sub-nav and the outlet content', () => {
    renderWithProviders(
      <Routes>
        <Route element={<ServicesAreaLayout />}>
          <Route path="/services" element={<div data-testid="list">List</div>} />
        </Route>
      </Routes>,
      { routerProps: { initialEntries: ['/services'] } },
    );
    expect(screen.getByTestId('list')).toBeInTheDocument();
    expect(screen.getByText('Discovery')).toBeInTheDocument();
    expect(screen.getByText('Legacy')).toBeInTheDocument();
  });
});

describe('ContractsAreaLayout', () => {
  it('renders the contracts sub-nav and the outlet content', () => {
    renderWithProviders(
      <Routes>
        <Route element={<ContractsAreaLayout />}>
          <Route path="/contracts" element={<div data-testid="clist">CList</div>} />
        </Route>
      </Routes>,
      { routerProps: { initialEntries: ['/contracts'] } },
    );
    expect(screen.getByTestId('clist')).toBeInTheDocument();
    expect(screen.getByText('Governance')).toBeInTheDocument();
    expect(screen.getByText('CDCT')).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend"; npx vitest run src/features/catalog/layouts/__tests__/AreaLayouts.test.tsx`
Expected: FAIL — módulos não encontrados.

- [ ] **Step 3: Implement ServicesAreaLayout**

```tsx
// src/frontend/src/features/catalog/layouts/ServicesAreaLayout.tsx
import { Outlet } from 'react-router-dom';
import { AreaSubNav, type AreaSubNavItem } from '../components/AreaSubNav';

const SERVICES_TABS: AreaSubNavItem[] = [
  { labelKey: 'catalogAreaNav.catalog', to: '/services', end: true },
  { labelKey: 'catalogAreaNav.graph', to: '/services/graph' },
  { labelKey: 'catalogAreaNav.discovery', to: '/services/discovery' },
  { labelKey: 'catalogAreaNav.maturity', to: '/services/maturity' },
  { labelKey: 'catalogAreaNav.experience', to: '/services/experience' },
  { labelKey: 'catalogAreaNav.featureFlags', to: '/services/feature-flags' },
  { labelKey: 'catalogAreaNav.legacy', to: '/services/legacy' },
];

/** Layout de área do catálogo de serviços: sub-nav + conteúdo (Outlet). */
export function ServicesAreaLayout() {
  return (
    <div className="flex flex-col h-full">
      <AreaSubNav items={SERVICES_TABS} ariaLabelKey="catalogAreaNav.ariaLabel" />
      <div className="flex-1 min-h-0 overflow-y-auto">
        <Outlet />
      </div>
    </div>
  );
}
```

- [ ] **Step 4: Implement ContractsAreaLayout**

```tsx
// src/frontend/src/features/catalog/layouts/ContractsAreaLayout.tsx
import { Outlet } from 'react-router-dom';
import { AreaSubNav, type AreaSubNavItem } from '../components/AreaSubNav';

const CONTRACTS_TABS: AreaSubNavItem[] = [
  { labelKey: 'contractsAreaNav.catalog', to: '/contracts', end: true },
  { labelKey: 'contractsAreaNav.governance', to: '/contracts/governance' },
  { labelKey: 'contractsAreaNav.health', to: '/contracts/health' },
  { labelKey: 'contractsAreaNav.rulesets', to: '/contracts/spectral' },
  { labelKey: 'contractsAreaNav.canonical', to: '/contracts/canonical' },
  { labelKey: 'contractsAreaNav.publication', to: '/contracts/publication' },
  { labelKey: 'contractsAreaNav.pipeline', to: '/contracts/pipeline' },
  { labelKey: 'contractsAreaNav.cdct', to: '/contracts/cdct' },
];

/** Layout de área do catálogo de contratos: sub-nav + conteúdo (Outlet). */
export function ContractsAreaLayout() {
  return (
    <div className="flex flex-col h-full">
      <AreaSubNav items={CONTRACTS_TABS} ariaLabelKey="contractsAreaNav.ariaLabel" />
      <div className="flex-1 min-h-0 overflow-y-auto">
        <Outlet />
      </div>
    </div>
  );
}
```

- [ ] **Step 5: Run test to verify it passes**

Run: `Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend"; npx vitest run src/features/catalog/layouts/__tests__/AreaLayouts.test.tsx`
Expected: PASS (2 tests).

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/features/catalog/layouts/
git commit -m "feat(catalog): ServicesAreaLayout + ContractsAreaLayout (sub-nav + Outlet)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 4: Aninhar rotas de área + renomear strays + redirects

**Files:**
- Modify: `src/frontend/src/routes/catalogRoutes.tsx`
- Modify: `src/frontend/src/routes/contractsRoutes.tsx`
- Modify: `src/frontend/src/features/catalog/components/ServiceScoreTab.tsx:210`
- Test: `src/frontend/src/routes/__tests__/catalogNavRedirects.test.tsx`

**Interfaces:**
- Consumes: `ServicesAreaLayout`, `ContractsAreaLayout` (Task 3).
- Produces: rotas `/services/experience` e `/contracts/pipeline` novas; redirects de `/catalog/developer-experience-score` e `/catalog/contracts/pipeline`; sub-nav visível nas páginas de área.

**Notas de aninhamento (react-router 7):**
- Os grupos de rota são fragmentos `<>...</>` spread dentro do `<Route element={<AppShell/>}>` em `App.tsx`. Para aninhar, envolver as rotas de área num `<Route element={<ServicesAreaLayout/>}>` pai (que renderiza `<Outlet/>`).
- Rotas de **área** (recebem sub-nav) em services: `/services`, `/services/graph`, `/services/discovery`, `/services/maturity`, `/services/experience`, `/services/feature-flags`, `/services/legacy`.
- Rotas que ficam **fora** do wrapper (sem sub-nav, mas mantêm o root `/services` destacado na sidebar): `/services/onboard`, `/services/new` (redirect), `/services/:serviceId`, `/services/:serviceId/interfaces/new`, `/services/legacy/:assetType/:assetId`, `/services/scorecards`.
- Rotas de **área** em contracts: `/contracts`, `/contracts/governance`, `/contracts/health`, `/contracts/spectral`, `/contracts/canonical`, `/contracts/publication`, `/contracts/pipeline`, `/contracts/cdct`.
- Rotas que ficam **fora** do wrapper em contracts: `/contracts/new`, `/contracts/studio/:draftId`, `/contracts/studio/new`, `/contracts/legacy` (redirect), `/contracts/health/timeline`, `/contracts/canonical/impact-cascade`, `/contracts/playground`, `/contracts/migration`, `/contracts/portal/:contractVersionId`, `/contracts/:contractVersionId`.
- Ranking do react-router resolve estático > dinâmico, portanto `/contracts/pipeline` (dentro do wrapper) não colide com `/contracts/:contractVersionId` (fora).

- [ ] **Step 1: Write the failing redirect test**

```tsx
// src/frontend/src/routes/__tests__/catalogNavRedirects.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { Routes, Route, useLocation } from 'react-router-dom';
import { renderWithProviders } from '../../__tests__/test-utils';
import { CatalogRoutes } from '../catalogRoutes';
import { ContractsRoutes } from '../contractsRoutes';

// ProtectedRoute passa-o à frente com permissões concedidas no test-utils; caso
// contrário, mockar para render directo.
vi.mock('../../components/ProtectedRoute', () => ({
  ProtectedRoute: ({ children }: { children: React.ReactNode }) => <>{children}</>,
}));

function LocationProbe() {
  const loc = useLocation();
  return <div data-testid="pathname">{loc.pathname}</div>;
}

function renderAt(path: string) {
  return renderWithProviders(
    <Routes>
      {CatalogRoutes()}
      {ContractsRoutes()}
      <Route path="*" element={<LocationProbe />} />
    </Routes>,
    { routerProps: { initialEntries: [path] } },
  );
}

describe('catalog navigation redirects', () => {
  it('redirects /catalog/developer-experience-score to /services/experience', async () => {
    renderAt('/catalog/developer-experience-score');
    expect(await screen.findByTestId('pathname')).toHaveTextContent('/services/experience');
  });

  it('redirects /catalog/contracts/pipeline to /contracts/pipeline', async () => {
    renderAt('/catalog/contracts/pipeline');
    expect(await screen.findByTestId('pathname')).toHaveTextContent('/contracts/pipeline');
  });
});
```

> Se o `LocationProbe` não capturar a rota final (porque as páginas reais montam), simplificar: asserir que a página-destino (`/services/experience`) renderiza um marcador conhecido. Ajustar durante execução se necessário — o critério é: navegar para o path antigo termina no path novo.

- [ ] **Step 2: Run test to verify it fails**

Run: `Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend"; npx vitest run src/routes/__tests__/catalogNavRedirects.test.tsx`
Expected: FAIL — sem redirect, path permanece o antigo.

- [ ] **Step 3: Editar `catalogRoutes.tsx` — importar layout e aninhar services**

Adicionar import no topo (a seguir aos `lazy(...)`):

```tsx
import { ServicesAreaLayout } from '../features/catalog/layouts/ServicesAreaLayout';
```

Substituir as rotas de área de services (`/services`, `/services/graph`, `/services/discovery`, `/services/maturity`, `/services/feature-flags`, `/services/legacy`) por um wrapper. Manter as rotas de detalhe fora. Estrutura resultante (dentro do fragmento):

```tsx
      {/* Área de serviços — sub-nav persistente */}
      <Route element={<ServicesAreaLayout />}>
        <Route
          path="/services"
          element={
            <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
              <ServiceCatalogListPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/services/graph"
          element={
            <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
              <ServiceCatalogPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/services/discovery"
          element={
            <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
              <ServiceDiscoveryPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/services/maturity"
          element={
            <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
              <ServiceMaturityPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/services/experience"
          element={
            <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
              <DeveloperExperienceScorePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/services/feature-flags"
          element={
            <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
              <ServiceFeatureFlagsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/services/legacy"
          element={
            <ProtectedRoute permission="catalog:assets:read" redirectTo="/unauthorized">
              <LegacyAssetCatalogPage />
            </ProtectedRoute>
          }
        />
      </Route>
```

Manter FORA do wrapper (não mover): `/services/onboard`, `/services/new` (redirect), `/services/:serviceId`, `/services/:serviceId/interfaces/new`, `/services/legacy/:assetType/:assetId`, `/services/scorecards`, `/graph` (redirect), `/search`, `/source-of-truth*`, `/portal/*`, e todas as `/catalog/*`.

- [ ] **Step 4: Editar `catalogRoutes.tsx` — renomear stray + redirects**

Remover a rota `/catalog/developer-experience-score` (o componente `DeveloperExperienceScorePage` passa a ser servido por `/services/experience`, ver Step 3) e substituir por um redirect. Substituir também `/catalog/contracts/pipeline` (que serve `ContractPipelinePage`) por um redirect — o `ContractPipelinePage` passa a ser servido em `/contracts/pipeline` (Task 4, Step 5, contractsRoutes). Adicionar:

```tsx
      {/* Rotas renomeadas — redirects de compatibilidade */}
      <Route path="/catalog/developer-experience-score" element={<Navigate to="/services/experience" replace />} />
      <Route path="/catalog/contracts/pipeline" element={<Navigate to="/contracts/pipeline" replace />} />
```

> `ContractPipelinePage` está importado em `catalogRoutes.tsx`. Como passa a ser usado em `contractsRoutes.tsx`, mover a linha `const ContractPipelinePage = lazy(...)` para `contractsRoutes.tsx` e remover de `catalogRoutes.tsx` (evita import órfão → erro de lint `no-unused-vars`).

- [ ] **Step 5: Editar `contractsRoutes.tsx` — importar layout, mover ContractPipelinePage, aninhar contracts**

Adicionar imports no topo:

```tsx
import { ContractsAreaLayout } from '../features/catalog/layouts/ContractsAreaLayout';
const ContractPipelinePage = lazy(() => import('../features/catalog/pages/ContractPipelinePage').then(m => ({ default: m.ContractPipelinePage })));
```

Envolver as rotas de área de contracts num wrapper e adicionar `/contracts/pipeline`:

```tsx
      {/* Área de contratos — sub-nav persistente */}
      <Route element={<ContractsAreaLayout />}>
        <Route
          path="/contracts"
          element={
            <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
              <ContractCatalogPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/contracts/governance"
          element={
            <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
              <ContractGovernancePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/contracts/health"
          element={
            <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
              <ContractHealthDashboardPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/contracts/spectral"
          element={
            <ProtectedRoute permission="rulesets:read" redirectTo="/unauthorized">
              <SpectralRulesetManagerPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/contracts/canonical"
          element={
            <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
              <CanonicalEntityCatalogPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/contracts/publication"
          element={
            <ProtectedRoute permission="contracts:write" redirectTo="/unauthorized">
              <PublicationCenterPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/contracts/pipeline"
          element={
            <ProtectedRoute permission="catalog:contracts:pipeline:read" redirectTo="/unauthorized">
              <ContractPipelinePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/contracts/cdct"
          element={
            <ProtectedRoute permission="contracts:read" redirectTo="/unauthorized">
              <ConsumerDrivenContractPage />
            </ProtectedRoute>
          }
        />
      </Route>
```

Remover as definições standalone antigas de `/contracts`, `/contracts/governance`, `/contracts/health`, `/contracts/spectral`, `/contracts/canonical`, `/contracts/publication`, `/contracts/cdct` (agora dentro do wrapper). Manter FORA do wrapper todas as outras rotas de `/contracts/*` listadas nas notas de aninhamento. **Importante:** o `/contracts/:contractVersionId` tem de continuar a ser a última rota do fragmento.

- [ ] **Step 6: Atualizar link interno em ServiceScoreTab.tsx**

Editar `src/frontend/src/features/catalog/components/ServiceScoreTab.tsx:210`:

```tsx
              to={`/services/experience?serviceId=${serviceId}`}
```

(era `/catalog/developer-experience-score?serviceId=${serviceId}`)

- [ ] **Step 7: Run redirect test to verify it passes**

Run: `Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend"; npx vitest run src/routes/__tests__/catalogNavRedirects.test.tsx`
Expected: PASS (2 tests).

- [ ] **Step 8: Commit**

```bash
git add src/frontend/src/routes/catalogRoutes.tsx src/frontend/src/routes/contractsRoutes.tsx src/frontend/src/features/catalog/components/ServiceScoreTab.tsx src/frontend/src/routes/__tests__/catalogNavRedirects.test.tsx
git commit -m "feat(catalog): aninhar rotas de area sob layouts + renomear strays com redirects

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 5: Reduzir a sidebar da secção catalog a 5 roots

**Files:**
- Modify: `src/frontend/src/components/shell/AppSidebar.tsx:53-75`

**Interfaces:**
- Consumes: nada novo.
- Produces: `navItems` da secção `catalog` com exatamente 5 entradas (roots disjuntos), sem `subGroup` do catálogo.

- [ ] **Step 1: Substituir o bloco CATÁLOGO do `navItems`**

Substituir as linhas 53–75 (do comentário `// ── CATÁLOGO ──` até à última entrada `sidebar.operationalNotes`) por:

```tsx
  // ── CATÁLOGO ──────────────────────────────────────────────────────────────
  { labelKey: 'sidebar.serviceCatalog', to: '/services', icon: <Server size={18} />, permission: 'catalog:assets:read', section: 'catalog' },
  { labelKey: 'sidebar.contractCatalog', to: '/contracts', icon: <FileText size={18} />, permission: 'contracts:read', section: 'catalog' },
  { labelKey: 'sidebar.developerPortal', to: '/portal', icon: <BookMarked size={18} />, permission: 'developer-portal:read', section: 'catalog' },
  { labelKey: 'sidebar.sourceOfTruth', to: '/source-of-truth', icon: <BookOpenCheck size={18} />, permission: 'catalog:assets:read', section: 'catalog' },
  { labelKey: 'sidebar.knowledgeHub', to: '/knowledge', icon: <BookOpen size={18} />, permission: 'catalog:assets:read', section: 'catalog' },
```

- [ ] **Step 2: Remover imports de ícones que ficaram órfãos**

Após o Step 1, verificar quais ícones deixaram de ser usados **em todo o ficheiro** (só remover os que já não aparecem em mais lado nenhum). Candidatos removidos do bloco catalog: `Share2`, `Radar`, `Award`, `Star`, `Sliders`, `Archive`, `ShieldCheck`, `GitBranch`, `Stethoscope`, `FlaskConical`, `ListChecks`, `Boxes`, `Send`, `StickyNote`. **Verificar cada um com busca no ficheiro** — muitos (`Award`, `Archive`, `Stethoscope`, `FlaskConical`, `ListChecks`, `ShieldCheck`, `Send`, `Sliders`, `Share2`) ainda são usados noutras secções e **não** devem ser removidos. Remover do import apenas os que a busca confirmar sem outras ocorrências.

Run (verificação por ícone, exemplo para `Radar`): `Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend"; Select-String -Path src/components/shell/AppSidebar.tsx -Pattern "Radar" | Measure-Object | Select-Object -ExpandProperty Count`
Expected: `1` (só o import) → remover; `>1` → manter.

- [ ] **Step 3: tsc + lint no ficheiro**

Run: `Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend"; npx tsc --noEmit; npx eslint src/components/shell/AppSidebar.tsx`
Expected: zero erros (sem imports órfãos, sem unused).

- [ ] **Step 4: Run existing sidebar/shell tests**

Run: `Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend"; npx vitest run src/__tests__/components/shell/SidebarComponents.test.tsx src/__tests__/components/shell/AppShell.test.tsx`
Expected: PASS (não asseram os itens de drill-down; testam sub-componentes e estrutura).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/components/shell/AppSidebar.tsx
git commit -m "feat(catalog): sidebar do catalogo reduzida a 5 roots ancorados

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 6: Gates completos + varredura em browser (stub)

**Files:** nenhum (verificação).

- [ ] **Step 1: tsc**

Run: `Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend"; npx tsc --noEmit`
Expected: zero erros.

- [ ] **Step 2: lint**

Run: `Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend"; npm run lint`
Expected: zero erros/warnings.

- [ ] **Step 3: build**

Run: `Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend"; npm run build`
Expected: build sem erros.

- [ ] **Step 4: suite de testes completa**

Run: `Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend"; npm run test`
Expected: todos os testes passam (baseline ~2467 + novos). Zero falhas.

- [ ] **Step 5: Varredura em browser no modo stub**

Arrancar `npm run stub` e, com as ferramentas de browser, percorrer (verificar destaque único na sidebar + sub-nav visível + ausência de erros de consola via `read_console_messages` com `onlyErrors:true`):
- `/services` → sidebar "Catálogo de Serviços" ativo; sub-nav "Catálogo" ativo.
- `/services/graph`, `/services/discovery`, `/services/maturity`, `/services/experience`, `/services/feature-flags`, `/services/legacy` → sidebar continua em "Catálogo de Serviços"; a sub-tab correspondente ativa; "Catálogo" (root) NÃO ativo.
- `/services/:id` (abrir um serviço) → sidebar continua em "Catálogo de Serviços".
- `/contracts` → sidebar "Catálogo de Contratos" ativo; sub-nav "Catálogo" ativo.
- `/contracts/governance`, `/contracts/health`, `/contracts/spectral`, `/contracts/canonical`, `/contracts/publication`, `/contracts/pipeline`, `/contracts/cdct` → sidebar continua em "Catálogo de Contratos"; sub-tab correspondente ativa.
- `/contracts/:id` (workspace) → sidebar continua em "Catálogo de Contratos".
- `/catalog/developer-experience-score` → redireciona para `/services/experience`.
- `/catalog/contracts/pipeline` → redireciona para `/contracts/pipeline`.

- [ ] **Step 6: Merge para main**

Após todos os gates verdes e varredura limpa:

```bash
git checkout main
git merge --no-ff redesign/betterstack-catalog-discovery -m "Merge: redesign da navegacao do catalogo (5 roots + sub-nav de area)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
git push origin main
```

---

## Self-Review (checklist executado ao escrever o plano)

**Cobertura da spec:**
- Spec §1 (sidebar 5 roots) → Task 5. ✓
- Spec §2 (sub-nav de área + AreaSubNav + 2 layouts) → Tasks 1, 3. ✓
- Spec §3 (estado ativo, sem teletransporte) → resulta de Tasks 3+5 (só roots disjuntos na sidebar); verificado em Task 6 Step 5. ✓
- Spec §4 (unificação de rotas + 2 redirects) → Task 4 Steps 4–6. ✓
- Spec §5 (ficheiros afetados) → cobertos em File Structure. ✓
- Spec §6 (faseamento) → mapeado: Fase 1 = Tasks 1,3,5; Fase 2 = Task 4; Fase 3 = Tasks 2,6. ✓
- Spec §7 (testes) → AreaSubNav (Task 1), layouts (Task 3), redirects (Task 4), sidebar/shell (Task 5), browser (Task 6). ✓

**Consistência de tipos:** `AreaSubNavItem { labelKey; to; end? }` e `AreaSubNav({ items, ariaLabelKey })` idênticos entre Task 1 (definição), Task 3 (consumo) e testes. ✓

**Ambiguidades resolvidas:**
- `data-active` não pode ser função num atributo DOM → resolvido via `<span>` interno (Task 1 Step 3b), com asserções ajustadas.
- `ContractPipelinePage` importado em catalogRoutes mas usado em contracts → mover o `lazy(...)` para contractsRoutes (Task 4 Steps 4–5) evita import órfão.
- Remoção de imports de ícones exige verificação por-ícone (muitos são partilhados por outras secções) → Task 5 Step 2 com comando de contagem.
