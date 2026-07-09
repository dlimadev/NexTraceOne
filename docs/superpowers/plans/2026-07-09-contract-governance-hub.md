# Contract Quality & Governance Hub Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Dar espinha à jornada de qualidade/governança de contratos — promover `ContractGovernancePage` a hub com uma secção de lançamento das 11 ferramentas agrupadas por intenção, adicionar entrada de sidebar, e ligar os dead-ends órfãos (health→timeline/portal, canonical→impact-cascade).

**Architecture:** Redesign de jornada no frontend React. Um componente apresentacional novo (`GovernanceToolsSection`) montado na página-hub existente; wiring por `Link`/query-param nas páginas-mãe; uma entrada de navegação no sidebar. Zero backend novo, honest-null onde faltam dados.

**Tech Stack:** React 19, TypeScript 5.9, react-router-dom 7 (`Link`, `useSearchParams`), TanStack Query 5 (já presente), DS `../../../shared/ui` (`Button`), `components/*` (`PageHeader`, `Card`), lucide-react, i18next (4 locales), Vitest + Testing Library, Playwright.

## Global Constraints

- DS de `../../../shared/ui`; componentes de `components/*`; ícones `lucide-react`; `Link`/`useSearchParams` de `react-router-dom`.
- Honest-null: nunca fabricar contagens/estado; ocultar o link quando falta o id (ex. `contractVersionId`).
- i18n: nenhuma string de UI hardcoded; usar `t('key','fallback inglês')`; chaves novas nos 4 locales `en, es, pt-BR, pt-PT` (NÃO existe `fr`); ficheiros FLAT `src/frontend/src/locales/<l>.json`.
- Mudanças cirúrgicas: não refatorar a lógica de insights nem o interior das ferramentas.
- Testes centralizados em `src/frontend/src/__tests__/**` (co-located NÃO são recolhidos). e2e em `src/frontend/e2e/**` (globs de URL Playwright usam `**`, não `*`).
- Gates de tooling: usar `npm run test` (NÃO `npx vitest`); gate final `npm run build` (`tsc -b`, apanha o que `tsc --noEmit` não apanha); `npm run validate:i18n`.
- Rotas verbatim: hub `/contracts/governance`; ferramentas `/contracts/health`, `/contracts/health/timeline`, `/contracts/spectral`, `/contracts/cdct`, `/contracts/canonical`, `/contracts/canonical/impact-cascade`, `/contracts/publication`, `/contracts/migration`, `/contracts/playground`; portal `/contracts/portal/:contractVersionId`.

---

### Task 1: `GovernanceToolsSection` (grelha de ferramentas)

**Files:**
- Create: `src/frontend/src/features/contracts/governance/GovernanceToolsSection.tsx`
- Test: `src/frontend/src/__tests__/contracts/GovernanceToolsSection.test.tsx`

**Interfaces:**
- Produces: `export function GovernanceToolsSection(): JSX.Element` — sem props; apresentacional; renderiza 5 grupos e 9 `Link`s de ferramenta.

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/contracts/GovernanceToolsSection.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { GovernanceToolsSection } from '../../features/contracts/governance/GovernanceToolsSection';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));

function wrap() {
  render(<MemoryRouter><GovernanceToolsSection /></MemoryRouter>);
}

describe('GovernanceToolsSection', () => {
  it('renders one link per tool with the correct route', () => {
    wrap();
    const hrefs = screen.getAllByRole('link').map((a) => a.getAttribute('href'));
    expect(hrefs).toEqual(expect.arrayContaining([
      '/contracts/health', '/contracts/health/timeline',
      '/contracts/spectral', '/contracts/cdct',
      '/contracts/canonical', '/contracts/canonical/impact-cascade',
      '/contracts/publication', '/contracts/migration',
      '/contracts/playground',
    ]));
    expect(hrefs).toHaveLength(9);
  });

  it('renders the five intent groups', () => {
    wrap();
    for (const g of ['Assess', 'Enforce', 'Model', 'Publish', 'Test']) {
      expect(screen.getByText(g)).toBeInTheDocument();
    }
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/GovernanceToolsSection.test.tsx --run`
Expected: FAIL — módulo `GovernanceToolsSection` não existe.

- [ ] **Step 3: Write minimal implementation**

```tsx
// src/frontend/src/features/contracts/governance/GovernanceToolsSection.tsx
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  Stethoscope, LineChart, ListChecks, FlaskConical, Boxes, GitBranch,
  Send, ArrowRightLeft, PlayCircle, ArrowRight,
} from 'lucide-react';

interface Tool { to: string; icon: React.ReactNode; titleKey: string; titleFallback: string; subKey: string; subFallback: string; }
interface ToolGroup { groupKey: string; groupFallback: string; tools: Tool[]; }

const GROUPS: ToolGroup[] = [
  {
    groupKey: 'contracts.governance.tools.groups.assess', groupFallback: 'Assess',
    tools: [
      { to: '/contracts/health', icon: <Stethoscope size={18} />, titleKey: 'contracts.governance.tools.items.healthDashboard.title', titleFallback: 'Health dashboard', subKey: 'contracts.governance.tools.items.healthDashboard.subtitle', subFallback: 'Aggregated quality score and violations' },
      { to: '/contracts/health/timeline', icon: <LineChart size={18} />, titleKey: 'contracts.governance.tools.items.healthTimeline.title', titleFallback: 'Health timeline', subKey: 'contracts.governance.tools.items.healthTimeline.subtitle', subFallback: 'Quality trend across versions' },
    ],
  },
  {
    groupKey: 'contracts.governance.tools.groups.enforce', groupFallback: 'Enforce',
    tools: [
      { to: '/contracts/spectral', icon: <ListChecks size={18} />, titleKey: 'contracts.governance.tools.items.spectral.title', titleFallback: 'Spectral rulesets', subKey: 'contracts.governance.tools.items.spectral.subtitle', subFallback: 'Lint rules applied to contracts' },
      { to: '/contracts/cdct', icon: <FlaskConical size={18} />, titleKey: 'contracts.governance.tools.items.cdct.title', titleFallback: 'Consumer-driven contracts', subKey: 'contracts.governance.tools.items.cdct.subtitle', subFallback: 'Consumer expectations and verification' },
    ],
  },
  {
    groupKey: 'contracts.governance.tools.groups.model', groupFallback: 'Model',
    tools: [
      { to: '/contracts/canonical', icon: <Boxes size={18} />, titleKey: 'contracts.governance.tools.items.canonical.title', titleFallback: 'Canonical entities', subKey: 'contracts.governance.tools.items.canonical.subtitle', subFallback: 'Reusable standardized schemas' },
      { to: '/contracts/canonical/impact-cascade', icon: <GitBranch size={18} />, titleKey: 'contracts.governance.tools.items.impactCascade.title', titleFallback: 'Impact cascade', subKey: 'contracts.governance.tools.items.impactCascade.subtitle', subFallback: 'Blast radius of an entity change' },
    ],
  },
  {
    groupKey: 'contracts.governance.tools.groups.publish', groupFallback: 'Publish',
    tools: [
      { to: '/contracts/publication', icon: <Send size={18} />, titleKey: 'contracts.governance.tools.items.publication.title', titleFallback: 'Publication center', subKey: 'contracts.governance.tools.items.publication.subtitle', subFallback: 'Publish and promote contract versions' },
      { to: '/contracts/migration', icon: <ArrowRightLeft size={18} />, titleKey: 'contracts.governance.tools.items.migration.title', titleFallback: 'Migration', subKey: 'contracts.governance.tools.items.migration.subtitle', subFallback: 'Generate migration patches between versions' },
    ],
  },
  {
    groupKey: 'contracts.governance.tools.groups.test', groupFallback: 'Test',
    tools: [
      { to: '/contracts/playground', icon: <PlayCircle size={18} />, titleKey: 'contracts.governance.tools.items.playground.title', titleFallback: 'Playground', subKey: 'contracts.governance.tools.items.playground.subtitle', subFallback: 'Try requests against a contract' },
    ],
  },
];

/** Grelha de lançamento das ferramentas de qualidade/governança, agrupadas por intenção. Estático (honest-null, sem contagens). */
export function GovernanceToolsSection() {
  const { t } = useTranslation();
  return (
    <section className="mt-8">
      <h2 className="text-sm font-semibold text-heading mb-3">
        {t('contracts.governance.tools.title', 'Governance tools')}
      </h2>
      <div className="space-y-5">
        {GROUPS.map((group) => (
          <div key={group.groupKey}>
            <p className="text-xs font-medium uppercase tracking-wide text-muted mb-2">
              {t(group.groupKey, group.groupFallback)}
            </p>
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
              {group.tools.map((tool) => (
                <Link
                  key={tool.to}
                  to={tool.to}
                  className="group flex items-start gap-3 rounded-lg border border-edge bg-card p-4 shadow-sm transition-all hover:border-accent/40 hover:shadow-md focus:outline-none focus:ring-2 focus:ring-accent"
                >
                  <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-accent/10 text-accent group-hover:bg-accent/20 transition-colors">
                    {tool.icon}
                  </div>
                  <div className="min-w-0">
                    <p className="text-sm font-semibold text-heading group-hover:text-accent transition-colors">
                      {t(tool.titleKey, tool.titleFallback)}
                    </p>
                    <p className="mt-0.5 text-xs text-muted leading-snug">
                      {t(tool.subKey, tool.subFallback)}
                    </p>
                  </div>
                  <ArrowRight size={16} className="ml-auto shrink-0 text-muted group-hover:text-accent" />
                </Link>
              ))}
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/GovernanceToolsSection.test.tsx --run`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/governance/GovernanceToolsSection.tsx src/frontend/src/__tests__/contracts/GovernanceToolsSection.test.tsx
git commit -m "feat(contracts): GovernanceToolsSection — launch grid for the governance journey

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: Montar a secção de ferramentas no hub

**Files:**
- Modify: `src/frontend/src/features/contracts/governance/ContractGovernancePage.tsx`
- Test: `src/frontend/src/__tests__/contracts/ContractGovernancePage.hub.test.tsx`

**Interfaces:**
- Consumes: `GovernanceToolsSection` de Task 1.

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/contracts/ContractGovernancePage.hub.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ContractGovernancePage } from '../../features/contracts/governance/ContractGovernancePage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: {
    getContractsSummary: vi.fn(() => Promise.resolve({})),
    listContracts: vi.fn(() => Promise.resolve({ items: [] })),
  },
}));

function wrap() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(<QueryClientProvider client={qc}><MemoryRouter><ContractGovernancePage /></MemoryRouter></QueryClientProvider>);
}

describe('ContractGovernancePage hub', () => {
  it('renders the governance tools launch grid', async () => {
    wrap();
    expect(await screen.findByText('Governance tools')).toBeInTheDocument();
    const hrefs = screen.getAllByRole('link').map((a) => a.getAttribute('href'));
    expect(hrefs).toEqual(expect.arrayContaining(['/contracts/playground', '/contracts/migration']));
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/ContractGovernancePage.hub.test.tsx --run`
Expected: FAIL — "Governance tools" não é renderizado.

- [ ] **Step 3: Write minimal implementation**

Em `ContractGovernancePage.tsx`, adicionar o import no topo (junto aos outros imports de `./`):

```tsx
import { GovernanceToolsSection } from './GovernanceToolsSection';
```

E renderizar a secção imediatamente antes de fechar o `</PageContainer>` (após o bloco `{view === 'audit' && (...)}`):

```tsx
      {view === 'audit' && (
        <AuditView contracts={contracts} />
      )}

      <GovernanceToolsSection />
    </PageContainer>
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/ContractGovernancePage.hub.test.tsx --run`
Expected: PASS (1 test).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/governance/ContractGovernancePage.tsx src/frontend/src/__tests__/contracts/ContractGovernancePage.hub.test.tsx
git commit -m "feat(contracts): mount governance tools launch grid on the hub

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: Wiring da Health dashboard (timeline + portal)

**Files:**
- Modify: `src/frontend/src/features/contracts/governance/ContractHealthDashboardPage.tsx`
- Test: `src/frontend/src/__tests__/contracts/ContractHealthDashboardPage.wiring.test.tsx`

**Interfaces:**
- Consumes: dados já carregados (`data.topViolations[].contractVersionId`, `.semVer`).
- Produces: linhas de violação como `Link` para `/contracts/portal/:id`; ação de header `Link` para `/contracts/health/timeline`.

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/contracts/ContractHealthDashboardPage.wiring.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ContractHealthDashboardPage } from '../../features/contracts/governance/ContractHealthDashboardPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: {
    getHealthDashboard: vi.fn(() => Promise.resolve({
      totalContractVersions: 3, distinctContracts: 3, deprecatedVersions: 0,
      filteredCount: 3, percentWithExamples: 80, percentWithCanonicalEntities: 60,
      healthScore: 72,
      topViolations: [{ contractVersionId: 'cv-1', semVer: '1.2.0', violationCount: 4, topRuleIds: ['no-empty'] }],
    })),
  },
}));

function wrap() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(<QueryClientProvider client={qc}><MemoryRouter><ContractHealthDashboardPage /></MemoryRouter></QueryClientProvider>);
}

describe('ContractHealthDashboardPage wiring', () => {
  it('links a violation row to the contract portal', async () => {
    wrap();
    const row = await screen.findByRole('link', { name: /1\.2\.0/ });
    expect(row.getAttribute('href')).toBe('/contracts/portal/cv-1');
  });

  it('exposes a View timeline action', async () => {
    wrap();
    const link = await screen.findByRole('link', { name: /timeline/i });
    expect(link.getAttribute('href')).toBe('/contracts/health/timeline');
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/ContractHealthDashboardPage.wiring.test.tsx --run`
Expected: FAIL — não há links.

- [ ] **Step 3: Write minimal implementation**

Adicionar o import de `Link` no topo:

```tsx
import { Link } from 'react-router-dom';
```

No `PageHeader`, adicionar a prop `actions`:

```tsx
      <PageHeader
        title={t('contracts.healthDashboard.title', 'Contract Health Dashboard')}
        subtitle={t('contracts.healthDashboard.subtitle', 'Aggregated quality and governance metrics across all contracts')}
        icon={<ShieldCheck />}
        actions={
          <Link
            to="/contracts/health/timeline"
            className="inline-flex items-center gap-1.5 text-sm text-accent hover:underline"
          >
            <TrendingUp size={14} />
            {t('contracts.healthDashboard.viewTimeline', 'View timeline')}
          </Link>
        }
      />
```

Substituir a `<div key={v.contractVersionId} ...>` da linha de violação por um `Link` que envolve o mesmo conteúdo (preservar layout), condicional a `contractVersionId` (honest-null):

```tsx
                {data.topViolations.map((v) => (
                  <Link
                    key={v.contractVersionId}
                    to={`/contracts/portal/${v.contractVersionId}`}
                    className="flex items-center justify-between py-2 px-3 rounded bg-elevated text-sm hover:bg-hover transition-colors"
                  >
                    <div className="flex items-center gap-2">
                      <span className="text-body font-mono text-xs">{v.semVer}</span>
                      <span className="text-faded text-xs hidden sm:block">
                        {v.topRuleIds.join(', ')}
                      </span>
                    </div>
                    <span className="text-critical font-semibold tabular-nums">
                      {v.violationCount} {t('contracts.healthDashboard.violations', 'violations')}
                    </span>
                  </Link>
                ))}
```

(Se `TrendingUp` já está importado no ficheiro — está, na linha de imports de `lucide-react` — reutilizar; não duplicar o import.)

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/ContractHealthDashboardPage.wiring.test.tsx --run`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/governance/ContractHealthDashboardPage.tsx src/frontend/src/__tests__/contracts/ContractHealthDashboardPage.wiring.test.tsx
git commit -m "feat(contracts): wire health dashboard to timeline and contract portal

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: Wiring canónico (impact-cascade query param + link do catálogo)

**Files:**
- Modify: `src/frontend/src/features/contracts/canonical/CanonicalEntityImpactCascadePage.tsx`
- Modify: `src/frontend/src/features/contracts/canonical/CanonicalEntityCatalogPage.tsx`
- Test: `src/frontend/src/__tests__/contracts/CanonicalWiring.test.tsx`

**Interfaces:**
- Produces: `CanonicalEntityImpactCascadePage` pré-seleciona e auto-corre quando `?entityId=` presente; `CanonicalEntityCatalogPage` liga cada entidade expandida a `/contracts/canonical/impact-cascade?entityId=<id>`.

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/contracts/CanonicalWiring.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { CanonicalEntityImpactCascadePage } from '../../features/contracts/canonical/CanonicalEntityImpactCascadePage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: { getCanonicalEntityImpactCascade: vi.fn(() => new Promise(() => {})) },
}));

function wrapCascade(entry: string) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={[entry]}>
        <CanonicalEntityImpactCascadePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('CanonicalEntityImpactCascadePage query param', () => {
  it('pre-fills the entity id from ?entityId=', () => {
    wrapCascade('/contracts/canonical/impact-cascade?entityId=ent-42');
    const input = screen.getByLabelText(/Canonical Entity ID/i) as HTMLInputElement;
    expect(input.value).toBe('ent-42');
  });
});
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/CanonicalWiring.test.tsx --run`
Expected: FAIL — o input começa vazio.

- [ ] **Step 3: Write minimal implementation**

Em `CanonicalEntityImpactCascadePage.tsx`, adicionar `useSearchParams` ao import de `react-router-dom` (criar o import se não existir):

```tsx
import { useSearchParams } from 'react-router-dom';
```

Substituir a inicialização dos estados (linhas ~92-95) para semear a partir do query param e auto-correr:

```tsx
  const [searchParams] = useSearchParams();
  const initialEntityId = searchParams.get('entityId') ?? '';
  const [entityId, setEntityId] = useState(initialEntityId);
  const [maxDepth, setMaxDepth] = useState(2);
  const [submittedId, setSubmittedId] = useState(initialEntityId);
  const [submittedDepth, setSubmittedDepth] = useState(2);
```

(O `TextField` do entity id já usa `label={t('phase4.impactCascade.entityName', 'Canonical Entity ID')}` — o `getByLabelText(/Canonical Entity ID/i)` do teste resolve-o. `submittedId` semeado dispara a query `enabled: !!submittedId` automaticamente.)

Em `CanonicalEntityCatalogPage.tsx`, adicionar `Link` ao import de `react-router-dom` (criar o import) e `GitBranch` ao import de `lucide-react`:

```tsx
import { Link } from 'react-router-dom';
```
```tsx
// juntar GitBranch à lista já existente de ícones lucide-react
  GitBranch,
```

Dentro de `CanonicalEntityCard`, no bloco de detalhe expandido, adicionar uma acção de rodapé logo após o grid de metadados (após o `</div>` que fecha `grid grid-cols-3 gap-4 text-xs`, dentro do `{isExpanded && (...)}`):

```tsx
            <div>
              <Link
                to={`/contracts/canonical/impact-cascade?entityId=${entity.id}`}
                className="inline-flex items-center gap-1.5 text-xs text-accent hover:underline"
              >
                <GitBranch size={12} />
                {t('contracts.canonical.catalog.impactCascade', 'Impact cascade')}
              </Link>
            </div>
```

- [ ] **Step 4: Run test to verify it passes**

Run: `cd src/frontend && npm run test -- src/__tests__/contracts/CanonicalWiring.test.tsx --run`
Expected: PASS (1 test).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/contracts/canonical/CanonicalEntityImpactCascadePage.tsx src/frontend/src/features/contracts/canonical/CanonicalEntityCatalogPage.tsx src/frontend/src/__tests__/contracts/CanonicalWiring.test.tsx
git commit -m "feat(contracts): wire canonical catalog to impact cascade via entityId param

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 5: Entrada de sidebar para o hub

**Files:**
- Modify: `src/frontend/src/components/shell/AppSidebar.tsx:65-66`

**Interfaces:**
- Produces: item de navegação `sidebar.contractGovernanceHub` → `/contracts/governance` como 1º do sub-grupo `sidebar.subGroupContractGovernance`.

- [ ] **Step 1: Adicionar a entrada**

Imediatamente antes da linha `{ labelKey: 'sidebar.contractPipeline', ... }` (linha 66), inserir:

```tsx
  { labelKey: 'sidebar.contractGovernanceHub', to: '/contracts/governance', icon: <ShieldCheck size={18} />, permission: 'contracts:read', section: 'catalog', subGroup: 'sidebar.subGroupContractGovernance' },
```

Confirmar que `ShieldCheck` está no import de `lucide-react` no topo de `AppSidebar.tsx`; se não estiver, adicioná-lo à lista de imports (ordem alfabética não obrigatória).

- [ ] **Step 2: Verificar que compila e o lint passa**

Run: `cd src/frontend && npx tsc --noEmit -p tsconfig.app.json 2>&1 | head -5 && npx eslint src/components/shell/AppSidebar.tsx`
Expected: 0 erros (warnings pré-existentes de `MapPin`/`MessageSquare`/`Eye` no ficheiro são aceitáveis — não tocar).

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/components/shell/AppSidebar.tsx
git commit -m "feat(contracts): add contract governance hub landing entry to the sidebar

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 6: Chaves i18n (4 locales)

**Files:**
- Modify: `src/frontend/src/locales/en.json`
- Modify: `src/frontend/src/locales/es.json`
- Modify: `src/frontend/src/locales/pt-BR.json`
- Modify: `src/frontend/src/locales/pt-PT.json`

**Interfaces:**
- Produces: todas as chaves novas usadas nas Tasks 1-5, em paridade nos 4 locales.

Chaves a adicionar (com os valores por locale):

- `sidebar.contractGovernanceHub` — en `Contract Governance` · es `Gobernanza de contratos` · pt-BR `Governança de contratos` · pt-PT `Governança de contratos`
- `contracts.governance.tools.title` — en `Governance tools` · es `Herramientas de gobernanza` · pt-BR `Ferramentas de governança` · pt-PT `Ferramentas de governança`
- `contracts.governance.tools.groups.assess` — en `Assess` · es `Evaluar` · pt-BR `Avaliar` · pt-PT `Avaliar`
- `contracts.governance.tools.groups.enforce` — en `Enforce` · es `Aplicar` · pt-BR `Aplicar` · pt-PT `Aplicar`
- `contracts.governance.tools.groups.model` — en `Model` · es `Modelar` · pt-BR `Modelar` · pt-PT `Modelar`
- `contracts.governance.tools.groups.publish` — en `Publish` · es `Publicar` · pt-BR `Publicar` · pt-PT `Publicar`
- `contracts.governance.tools.groups.test` — en `Test` · es `Probar` · pt-BR `Testar` · pt-PT `Testar`
- `contracts.governance.tools.items.healthDashboard.title` / `.subtitle`
- `contracts.governance.tools.items.healthTimeline.title` / `.subtitle`
- `contracts.governance.tools.items.spectral.title` / `.subtitle`
- `contracts.governance.tools.items.cdct.title` / `.subtitle`
- `contracts.governance.tools.items.canonical.title` / `.subtitle`
- `contracts.governance.tools.items.impactCascade.title` / `.subtitle`
- `contracts.governance.tools.items.publication.title` / `.subtitle`
- `contracts.governance.tools.items.migration.title` / `.subtitle`
- `contracts.governance.tools.items.playground.title` / `.subtitle`
- `contracts.healthDashboard.viewTimeline` — en `View timeline` · es `Ver cronología` · pt-BR `Ver linha do tempo` · pt-PT `Ver cronologia`
- `contracts.canonical.catalog.impactCascade` — en `Impact cascade` · es `Cascada de impacto` · pt-BR `Cascata de impacto` · pt-PT `Cascata de impacto`

Para os pares `.title`/`.subtitle` das ferramentas, usar como valor `title`/`subtitle` os mesmos textos dos fallbacks em inglês na Task 1 para `en`, e traduções equivalentes para es/pt-BR/pt-PT (ex. healthDashboard.title: en `Health dashboard`, es `Panel de salud`, pt-BR `Painel de saúde`, pt-PT `Painel de saúde`; subtitle segue o fallback correspondente traduzido).

- [ ] **Step 1: Adicionar as chaves aos 4 locales**

Preservar a estrutura aninhada existente (`contracts`, `sidebar` já existem — inserir sob elas sem quebrar chaves existentes). Se ferramenta mais segura, usar um script Node que faz deep-merge e reescreve com `JSON.stringify(obj, null, 2)`.

- [ ] **Step 2: Validar i18n**

Run: `cd src/frontend && npm run validate:i18n`
Expected: PASS — 4 locales completos e em paridade, sem chaves em falta.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/locales/en.json src/frontend/src/locales/es.json src/frontend/src/locales/pt-BR.json src/frontend/src/locales/pt-PT.json
git commit -m "i18n(contracts): governance hub tools + wiring keys (4 locales)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 7: e2e do hub + gates finais

**Files:**
- Create: `src/frontend/e2e/contract-governance-hub.spec.ts`

- [ ] **Step 1: Escrever o e2e**

```ts
// src/frontend/e2e/contract-governance-hub.spec.ts
import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/** E2E — hub de governança lança as ferramentas da jornada. */
test.describe('Contract governance hub', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/contracts/summary**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({}) }));
    await page.route('**/api/v1/contracts/list**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [], totalCount: 0 }) }));
  });

  test('hub launches the playground tool', async ({ page }) => {
    await page.goto('/contracts/governance');
    await expect(page.getByText(/governance tools/i)).toBeVisible({ timeout: 5_000 });
    await page.getByRole('link', { name: /playground/i }).click();
    await expect(page).toHaveURL(/\/contracts\/playground/, { timeout: 5_000 });
  });
});
```

- [ ] **Step 2: Correr o e2e**

Run (PowerShell): `Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend"; $env:CI=""; npx playwright test --project=chromium e2e/contract-governance-hub.spec.ts`
Expected: 1 passed (auto-build + serve, reuseExistingServer).

- [ ] **Step 3: Gates finais**

Run: `cd src/frontend && npm run test -- --run 2>&1 | tail -5` → suite completa verde.
Run: `cd src/frontend && npm run validate:i18n` → PASS.
Run: `cd src/frontend && npm run build 2>&1 | tail -3` → exit 0.
Run: `cd src/frontend && npx eslint src/features/contracts/governance/GovernanceToolsSection.tsx src/features/contracts/governance/ContractGovernancePage.tsx src/features/contracts/governance/ContractHealthDashboardPage.tsx src/features/contracts/canonical/CanonicalEntityImpactCascadePage.tsx src/features/contracts/canonical/CanonicalEntityCatalogPage.tsx src/components/shell/AppSidebar.tsx` → 0 erros (warnings pré-existentes do AppSidebar ok).

- [ ] **Step 4: Commit**

```bash
git add src/frontend/e2e/contract-governance-hub.spec.ts
git commit -m "test(contracts): e2e — governance hub launches journey tools

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Self-Review

**1. Spec coverage:**
- §4.1 hub + tools launcher → Task 1 + Task 2. ✓
- §4.2 sidebar entry → Task 5. ✓
- §4.3 health→timeline/portal → Task 3; canonical→impact-cascade + query param → Task 4. ✓
- §7 i18n (4 locales) → Task 6. ✓
- §8 testes (section, hub, health wiring, cascade param, e2e) → Tasks 1-4, 7. ✓

**2. Placeholder scan:** Sem TBD/TODO. Todos os steps de código têm o código completo. Task 6 lista as chaves e valores (os pares de ferramenta seguem os fallbacks da Task 1 — explícito).

**3. Type consistency:** `GovernanceToolsSection` sem props em todas as referências (Task 1 produz, Task 2 consome). Rotas verbatim idênticas entre spec, Task 1, Task 3/4/7. `contractVersionId`/`entity.id` conferem com as leituras reais dos ficheiros.
