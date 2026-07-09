# Self-service / Guided Setup Journey (J2 Fase 2, fatia 1) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give the service producer a guided path from a freshly-onboarded `Planning` service to a governed one — a maturity-style **setup checklist** on the service detail (honest-null, deep-link per gap) plus a **rebooted self-service hub** that fixes its broken links, leads with the onboarding golden path, and surfaces services that still need setup.

**Architecture:** A pure derivation function (`deriveSetupItems`) computes checklist items from data already loaded on `ServiceDetailPage` (service fields + contract count) — no new backend/endpoint. A presentational `ServiceSetupChecklist` renders them with deep-link CTAs. `ServiceDetailPage` wires the CTAs to its existing edit-mode / navigation. `SelfServicePortalPage` is reworked to fix dead links, add a golden-path header, and mount a `ServicesNeedingSetupSection` that queries `Planning` services.

**Tech Stack:** React 19, TypeScript 5.9, react-router-dom 7 (`useNavigate`/`Link`), TanStack Query 5, Vitest + Testing Library, Playwright (mock backend), i18next (4 locales: en, es, pt-BR, pt-PT). DS from `src/frontend/src/shared/ui` + `src/frontend/src/components`.

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-09-catalog-self-service-setup-journey-design.md`. Build fatia 1 only; the deferred items in spec §5 are OUT of scope.
- **No new backend/endpoint.** The checklist derives from data already loaded on the service detail (`ServiceDetail` fields + contract count). Runbook/Monitoring are omitted (the single-service maturity response exposes only `dimensions[]` with names/scores, not clean booleans) — honest-null, do not guess.
- **Honest-null everywhere:** never fabricate. Contract row is **N/A** (not "to-do") when `!supportsContracts(serviceType)`. The "needing setup" hub section hides entirely when empty. The checklist hides when it has no applicable items.
- **DRY on lifecycle:** do NOT add a competing "promote" transition button — lifecycle transitions belong to the existing `ServiceLifecyclePanel` (its own allowed-transition state machine). The checklist "complete" state points the user at that panel.
- No hardcoded UI strings — every user-facing string is an i18n key present in ALL 4 locales; `npm run validate:i18n` must pass. DS components + semantic tokens only (no raw hex).
- Tests live centrally under `src/frontend/src/__tests__/**` (co-located tests are NOT collected). Run unit tests with `npm run test` (never bare `npx vitest`). Final gate is `npm run build` (`tsc -b` catches what `tsc --noEmit` misses — e.g. Zod-internal types). e2e: `npx playwright test --project=chromium <spec>` with `$env:CI=""`, from `src/frontend`, URL globs use `**` (a `*` stops at `/`).
- All commands run from `src/frontend`. End every commit message with `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`. Solo branch — merge direct to `main`, no PR.

---

## File Structure

New:
- `src/frontend/src/features/catalog/components/serviceSetupChecklist.ts` — pure `deriveSetupItems(...)` + types (testable core).
- `src/frontend/src/features/catalog/components/ServiceSetupChecklist.tsx` — presentational checklist (consumes the pure fn).
- `src/frontend/src/features/catalog/components/ServicesNeedingSetupSection.tsx` — hub section querying `Planning` services (honest-null).

Modified:
- `src/frontend/src/features/catalog/pages/ServiceDetailPage.tsx` — mount the checklist in view mode; thread an `onEditField` handler (enters edit mode on the right tab).
- `src/frontend/src/features/catalog/pages/SelfServicePortalPage.tsx` — fix dead links, add golden-path header, mount `ServicesNeedingSetupSection`.
- `src/frontend/src/locales/{en,es,pt-BR,pt-PT}.json` — `serviceSetup.*` + `selfServicePortal.needingSetup.*` + `selfServicePortal.goldenPaths.*` keys.

Tests (new, under `src/frontend/src/__tests__/catalog/`):
- `serviceSetupChecklist.test.ts`, `ServiceSetupChecklist.test.tsx`, `ServicesNeedingSetupSection.test.tsx`, `SelfServicePortalPage.test.tsx` (new).
- e2e: `src/frontend/e2e/service-setup-journey.spec.ts`.

---

## Task 1: i18n keys (4 locales)

**Files:**
- Modify: `src/frontend/src/locales/en.json`, `es.json`, `pt-BR.json`, `pt-PT.json`

**Interfaces:**
- Produces the `serviceSetup.*`, `selfServicePortal.goldenPaths.*`, `selfServicePortal.needingSetup.*` key namespaces.

- [ ] **Step 1: Add the keys to each locale**

Add these keys (nest under the existing `serviceSetup` / `selfServicePortal` objects — create `serviceSetup` if absent). Values per locale:

| key | en | es | pt-BR | pt-PT |
|-----|----|----|-------|-------|
| serviceSetup.title | Setup checklist | Lista de configuración | Checklist de setup | Checklist de setup |
| serviceSetup.subtitle | Complete these to make the service production-ready. | Complétalos para dejar el servicio listo. | Complete para deixar o serviço pronto. | Complete para deixar o serviço pronto. |
| serviceSetup.progress | {{done}} of {{total}} complete | {{done}} de {{total}} completos | {{done}} de {{total}} concluídos | {{done}} de {{total}} concluídos |
| serviceSetup.complete | Setup complete — you can advance the lifecycle below. | Configuración completa — puedes avanzar el ciclo de vida abajo. | Setup completo — pode avançar o ciclo de vida abaixo. | Setup completo — pode avançar o ciclo de vida abaixo. |
| serviceSetup.na | Not applicable | No aplicable | Não aplicável | Não aplicável |
| serviceSetup.action | Complete | Completar | Completar | Completar |
| serviceSetup.items.ownership | Assign a technical owner | Asignar responsable técnico | Definir owner técnico | Definir owner técnico |
| serviceSetup.items.repository | Link the source repository | Enlazar el repositorio | Ligar o repositório | Ligar o repositório |
| serviceSetup.items.documentation | Add documentation | Añadir documentación | Adicionar documentação | Adicionar documentação |
| serviceSetup.items.interface | Register an interface | Registrar una interfaz | Registar uma interface | Registar uma interface |
| serviceSetup.items.contract | Publish a contract | Publicar un contrato | Publicar um contrato | Publicar um contrato |
| selfServicePortal.goldenPaths.title | Start here | Empieza aquí | Comece aqui | Comece aqui |
| selfServicePortal.goldenPaths.onboard | Onboard a service | Incorporar un servicio | Registrar um serviço | Registar um serviço |
| selfServicePortal.goldenPaths.onboard_desc | Register a service and its interface & contract in one guided flow. | Registra un servicio y su interfaz y contrato en un flujo guiado. | Registre um serviço e sua interface e contrato num fluxo guiado. | Registe um serviço e a sua interface e contrato num fluxo guiado. |
| selfServicePortal.goldenPaths.template | Start from a template | Empezar desde una plantilla | Começar de um template | Começar de um template |
| selfServicePortal.goldenPaths.template_desc | Scaffold from a golden-path template. | Genera desde una plantilla golden-path. | Gere a partir de um template golden-path. | Gere a partir de um template golden-path. |
| selfServicePortal.needingSetup.title | Services needing setup | Servicios por configurar | Serviços a configurar | Serviços a configurar |
| selfServicePortal.needingSetup.item | {{name}} · {{status}} | {{name}} · {{status}} | {{name}} · {{status}} | {{name}} · {{status}} |

- [ ] **Step 2: Validate**

Run: `npm run validate:i18n`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add src/frontend/src/locales/en.json src/frontend/src/locales/es.json src/frontend/src/locales/pt-BR.json src/frontend/src/locales/pt-PT.json
git commit -m "i18n(catalog): serviceSetup + self-service golden-path keys (4 locales)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 2: `deriveSetupItems` pure helper + `ServiceSetupChecklist` component

**Files:**
- Create: `src/frontend/src/features/catalog/components/serviceSetupChecklist.ts`
- Create: `src/frontend/src/features/catalog/components/ServiceSetupChecklist.tsx`
- Test: `src/frontend/src/__tests__/catalog/serviceSetupChecklist.test.ts`
- Test: `src/frontend/src/__tests__/catalog/ServiceSetupChecklist.test.tsx`

**Interfaces:**
- Produces:
  ```ts
  export type SetupItemId = 'ownership' | 'repository' | 'documentation' | 'interface' | 'contract';
  export interface SetupItem { id: SetupItemId; done: boolean; applicable: boolean; }
  export interface SetupServiceInput {
    technicalOwner?: string | null;
    repositoryUrl?: string | null;
    gitRepository?: string | null;
    documentationUrl?: string | null;
    apis?: unknown[] | null;
    serviceType?: string | null;
  }
  export function deriveSetupItems(
    service: SetupServiceInput, contractCount: number, supportsContracts: (t: string) => boolean,
  ): SetupItem[];
  export function setupProgress(items: SetupItem[]): { done: number; total: number; allDone: boolean };
  ```
  Component:
  ```ts
  interface ServiceSetupChecklistProps {
    service: SetupServiceInput;
    contractCount: number;
    lifecycleStatus: string;
    onEditOwnership: () => void;
    onEditReferences: () => void;
    onAddInterface: () => void;
    onAddContract: () => void;
  }
  export function ServiceSetupChecklist(props: ServiceSetupChecklistProps): JSX.Element | null;
  ```
- Consumes: `supportsContracts` from `../../contracts/shared/serviceContractPolicy` (existing).

- [ ] **Step 1: Write the failing test for the pure helper**

```ts
// src/frontend/src/__tests__/catalog/serviceSetupChecklist.test.ts
import { describe, it, expect } from 'vitest';
import { deriveSetupItems, setupProgress } from '../../features/catalog/components/serviceSetupChecklist';

const always = () => true;
const never = () => false;

describe('deriveSetupItems', () => {
  it('marks each dimension done based on loaded data', () => {
    const items = deriveSetupItems(
      { technicalOwner: 'a@x.com', repositoryUrl: 'https://r', documentationUrl: 'https://d', apis: [{}], serviceType: 'RestApi' },
      2, always,
    );
    const byId = Object.fromEntries(items.map((i) => [i.id, i]));
    expect(byId.ownership.done).toBe(true);
    expect(byId.repository.done).toBe(true);
    expect(byId.documentation.done).toBe(true);
    expect(byId.interface.done).toBe(true);
    expect(byId.contract.done).toBe(true);
  });

  it('flags missing dimensions as not done', () => {
    const items = deriveSetupItems({ serviceType: 'RestApi', apis: [] }, 0, always);
    const byId = Object.fromEntries(items.map((i) => [i.id, i]));
    expect(byId.ownership.done).toBe(false);
    expect(byId.interface.done).toBe(false);
    expect(byId.contract.done).toBe(false);
  });

  it('marks the contract row not-applicable when the type has no public contracts', () => {
    const items = deriveSetupItems({ serviceType: 'BackgroundService' }, 0, never);
    const contract = items.find((i) => i.id === 'contract')!;
    expect(contract.applicable).toBe(false);
  });

  it('setupProgress counts only applicable items', () => {
    const items = deriveSetupItems(
      { technicalOwner: 'a', serviceType: 'BackgroundService' }, 0, never,
    );
    const p = setupProgress(items);
    // 5 items, contract N/A -> 4 applicable; only ownership done -> 1/4
    expect(p.total).toBe(4);
    expect(p.done).toBe(1);
    expect(p.allDone).toBe(false);
  });
});
```

- [ ] **Step 2: Run to verify it fails**

Run: `npm run test -- serviceSetupChecklist`
Expected: FAIL (module not found).

- [ ] **Step 3: Write the pure helper**

```ts
// src/frontend/src/features/catalog/components/serviceSetupChecklist.ts
export type SetupItemId = 'ownership' | 'repository' | 'documentation' | 'interface' | 'contract';

export interface SetupItem {
  id: SetupItemId;
  done: boolean;
  applicable: boolean;
}

export interface SetupServiceInput {
  technicalOwner?: string | null;
  repositoryUrl?: string | null;
  gitRepository?: string | null;
  documentationUrl?: string | null;
  apis?: unknown[] | null;
  serviceType?: string | null;
}

const filled = (v?: string | null): boolean => !!v && v.trim().length > 0;

/** Deriva os itens de setup a partir de dados já carregados no detalhe (honest-null). */
export function deriveSetupItems(
  service: SetupServiceInput,
  contractCount: number,
  supportsContracts: (t: string) => boolean,
): SetupItem[] {
  const contractApplicable = service.serviceType ? supportsContracts(service.serviceType) : true;
  return [
    { id: 'ownership', done: filled(service.technicalOwner), applicable: true },
    { id: 'repository', done: filled(service.repositoryUrl) || filled(service.gitRepository), applicable: true },
    { id: 'documentation', done: filled(service.documentationUrl), applicable: true },
    { id: 'interface', done: (service.apis?.length ?? 0) > 0, applicable: true },
    { id: 'contract', done: contractCount > 0, applicable: contractApplicable },
  ];
}

/** Progresso considerando apenas itens aplicáveis (N/A não conta). */
export function setupProgress(items: SetupItem[]): { done: number; total: number; allDone: boolean } {
  const applicable = items.filter((i) => i.applicable);
  const done = applicable.filter((i) => i.done).length;
  return { done, total: applicable.length, allDone: applicable.length > 0 && done === applicable.length };
}
```

- [ ] **Step 4: Run to verify the helper passes**

Run: `npm run test -- serviceSetupChecklist`
Expected: PASS (4 tests).

- [ ] **Step 5: Write the failing component test**

```tsx
// src/frontend/src/__tests__/catalog/ServiceSetupChecklist.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ServiceSetupChecklist } from '../../features/catalog/components/ServiceSetupChecklist';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (k: string, o?: Record<string, unknown>) =>
      (o && typeof o === 'object' && 'done' in o) ? `${o.done}/${o.total}` : k,
  }),
}));

const base = {
  service: { serviceType: 'RestApi', apis: [] as unknown[] },
  contractCount: 0,
  lifecycleStatus: 'Planning',
  onEditOwnership: vi.fn(),
  onEditReferences: vi.fn(),
  onAddInterface: vi.fn(),
  onAddContract: vi.fn(),
};

describe('ServiceSetupChecklist', () => {
  it('fires the contract CTA for an incomplete applicable item', () => {
    const onAddContract = vi.fn();
    render(<ServiceSetupChecklist {...base} onAddContract={onAddContract} />);
    // O item de contrato está por fazer e aplicável -> tem CTA acionável.
    fireEvent.click(screen.getByTestId('setup-cta-contract'));
    expect(onAddContract).toHaveBeenCalled();
  });

  it('shows a completion note when all applicable items are done and not Active', () => {
    render(
      <ServiceSetupChecklist
        {...base}
        service={{ serviceType: 'RestApi', apis: [{}], technicalOwner: 'a', repositoryUrl: 'r', documentationUrl: 'd' }}
        contractCount={1}
      />,
    );
    expect(screen.getByText('serviceSetup.complete')).toBeInTheDocument();
  });
});
```

- [ ] **Step 6: Run to verify it fails**

Run: `npm run test -- ServiceSetupChecklist`
Expected: FAIL (module not found).

- [ ] **Step 7: Write the component**

```tsx
// src/frontend/src/features/catalog/components/ServiceSetupChecklist.tsx
import { useTranslation } from 'react-i18next';
import { Check, Circle, ArrowRight } from 'lucide-react';
import { Button } from '../../../shared/ui';
import { cn } from '../../../lib/cn';
import { supportsContracts } from '../../contracts/shared/serviceContractPolicy';
import type { ServiceType } from '../../../types';
import { deriveSetupItems, setupProgress, type SetupItemId, type SetupServiceInput } from './serviceSetupChecklist';

interface ServiceSetupChecklistProps {
  service: SetupServiceInput;
  contractCount: number;
  lifecycleStatus: string;
  onEditOwnership: () => void;
  onEditReferences: () => void;
  onAddInterface: () => void;
  onAddContract: () => void;
}

/** Checklist de setup guiado do serviço (detalhe). Honest-null, deep-link por lacuna. */
export function ServiceSetupChecklist({
  service, contractCount, lifecycleStatus,
  onEditOwnership, onEditReferences, onAddInterface, onAddContract,
}: ServiceSetupChecklistProps) {
  const { t } = useTranslation();
  const items = deriveSetupItems(service, contractCount, (ty) => supportsContracts(ty as ServiceType));
  if (items.length === 0) return null;

  const { done, total, allDone } = setupProgress(items);
  const isActive = lifecycleStatus === 'Active';
  const labelKey: Record<SetupItemId, string> = {
    ownership: 'serviceSetup.items.ownership',
    repository: 'serviceSetup.items.repository',
    documentation: 'serviceSetup.items.documentation',
    interface: 'serviceSetup.items.interface',
    contract: 'serviceSetup.items.contract',
  };
  const cta: Record<SetupItemId, () => void> = {
    ownership: onEditOwnership,
    repository: onEditReferences,
    documentation: onEditReferences,
    interface: onAddInterface,
    contract: onAddContract,
  };

  return (
    <div className="rounded-xl border border-edge bg-card p-4 mb-5">
      <div className="flex items-center justify-between mb-3">
        <div>
          <h3 className="text-sm font-semibold text-heading">{t('serviceSetup.title')}</h3>
          <p className="text-xs text-muted mt-0.5">{t('serviceSetup.subtitle')}</p>
        </div>
        <span className="text-xs font-medium text-muted shrink-0">{t('serviceSetup.progress', { done, total })}</span>
      </div>

      <div className="h-1.5 rounded-full bg-elevated overflow-hidden mb-3">
        <div className="h-full bg-accent transition-all" style={{ width: total ? `${(done / total) * 100}%` : '0%' }} />
      </div>

      <ul className="divide-y divide-edge/60">
        {items.map((item) => (
          <li key={item.id} className="flex items-center gap-3 py-2.5 text-sm">
            <span className={cn('flex items-center justify-center w-5 h-5 rounded-full shrink-0',
              item.done ? 'bg-success text-white' : 'bg-elevated text-muted')}>
              {item.done ? <Check size={12} /> : <Circle size={10} />}
            </span>
            <span className={cn('min-w-0 truncate', item.done ? 'text-muted line-through' : 'text-heading')}>
              {t(labelKey[item.id])}
            </span>
            <span className="ml-auto shrink-0">
              {!item.applicable ? (
                <span className="text-xs text-muted">{t('serviceSetup.na')}</span>
              ) : !item.done ? (
                <Button variant="ghost" size="xs" data-testid={`setup-cta-${item.id}`}
                  onClick={cta[item.id]} icon={<ArrowRight size={12} />}>
                  {t('serviceSetup.action')}
                </Button>
              ) : null}
            </span>
          </li>
        ))}
      </ul>

      {allDone && !isActive && (
        <p className="mt-3 text-xs text-success">{t('serviceSetup.complete')}</p>
      )}
    </div>
  );
}
```

- [ ] **Step 8: Run to verify component test passes**

Run: `npm run test -- ServiceSetupChecklist`
Expected: PASS (2 tests).
Run: `npx tsc --noEmit`
Expected: no errors in the two new files.

- [ ] **Step 9: Commit**

```bash
git add src/frontend/src/features/catalog/components/serviceSetupChecklist.ts src/frontend/src/features/catalog/components/ServiceSetupChecklist.tsx src/frontend/src/__tests__/catalog/serviceSetupChecklist.test.ts src/frontend/src/__tests__/catalog/ServiceSetupChecklist.test.tsx
git commit -m "feat(catalog): ServiceSetupChecklist + pure deriveSetupItems

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 3: Mount the checklist in `ServiceDetailPage`

**Files:**
- Modify: `src/frontend/src/features/catalog/pages/ServiceDetailPage.tsx`
- Test: `src/frontend/src/__tests__/catalog/ServiceDetailPage.setup.test.tsx`

**Interfaces:**
- Consumes: `ServiceSetupChecklist` (Task 2). Uses existing `ServiceDetailPage` internals: `enterEditMode`, `setActiveFormTab`, `navigate`, `serviceId`, `service`, `contracts`.
- Produces: the checklist rendered in view mode with wired CTAs.

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/catalog/ServiceDetailPage.setup.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { ServiceDetailPage } from '../../features/catalog/pages/ServiceDetailPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, d?: string) => (typeof d === 'string' ? d : k) }) }));
vi.mock('../../contexts/EnvironmentContext', () => ({ useEnvironment: () => ({ activeEnvironment: null }) }));

const service = {
  id: 'svc-1', name: 'orders-api', displayName: 'orders-api', domain: 'Commerce',
  serviceType: 'RestApi', criticality: 'Medium', exposureType: 'Internal',
  lifecycleStatus: 'Planning', teamName: 'Orders', technicalOwner: '', apis: [], apiCount: 0,
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

describe('ServiceDetailPage setup checklist', () => {
  it('renders the setup checklist for a Planning service', async () => {
    renderAt('svc-1');
    await waitFor(() => expect(screen.getByText('Setup checklist')).toBeInTheDocument());
  });
});
```

> Note: the i18n mock returns the default string, so assert on the default `'Setup checklist'` passed as `t('serviceSetup.title', 'Setup checklist')`. Ensure the checklist call in `ServiceSetupChecklist` uses a default arg OR adjust this assertion to the key. Since Task 2's component calls `t('serviceSetup.title')` WITHOUT a default, change the checklist's title/subtitle calls to include defaults `t('serviceSetup.title', 'Setup checklist')` / `t('serviceSetup.subtitle', '...')` in Task 2, OR assert on the key here. Use the key assertion to avoid touching Task 2: replace `'Setup checklist'` with `'serviceSetup.title'`.

Adjust the test's final assertion to: `await waitFor(() => expect(screen.getByText('serviceSetup.title')).toBeInTheDocument());`

- [ ] **Step 2: Run to verify it fails**

Run: `npm run test -- ServiceDetailPage.setup`
Expected: FAIL (checklist not rendered).

- [ ] **Step 3: Add the `onEditField` handler in the parent and pass to `ViewContent`**

In `ServiceDetailPage.tsx`, add a handler in the main component (near `enterEditMode`):

```tsx
  /** Entra em modo edição posicionado na tab que preenche a lacuna do checklist. */
  const handleEditField = useCallback((tab: FormTab) => {
    enterEditMode();
    setActiveFormTab(tab);
  }, [enterEditMode]);
```

Pass it into `<ViewContent … onEditField={handleEditField} />` (add the prop to the JSX where `ViewContent` is rendered).

- [ ] **Step 4: Thread the prop through `ViewContentProps` and render the checklist**

Add to `interface ViewContentProps` (after `navigate`):

```tsx
  onEditField: (tab: 'ownership' | 'references') => void;
```

Destructure `onEditField` in the `ViewContent` function params. Add the import at the top of the file:

```tsx
import { ServiceSetupChecklist } from '../components/ServiceSetupChecklist';
```

Inside `ViewContent`'s returned fragment, immediately AFTER the mini stat strip (`<div className="grid grid-cols-3 gap-3 mb-5"> … </div>`) and BEFORE the "Identidade & Classificação" `SectionBlock`, insert:

```tsx
      <ServiceSetupChecklist
        service={service}
        contractCount={serviceContracts?.totalCount ?? contracts.length}
        lifecycleStatus={service.lifecycleStatus}
        onEditOwnership={() => onEditField('ownership')}
        onEditReferences={() => onEditField('references')}
        onAddInterface={() => navigate(`/services/${serviceId}/interfaces/new`)}
        onAddContract={() => navigate(`/contracts/new?serviceId=${serviceId}`)}
      />
```

- [ ] **Step 5: Run to verify it passes**

Run: `npm run test -- ServiceDetailPage.setup ServiceDetailPage`
Expected: PASS (new test + existing ServiceDetailPage tests still green).
Run: `npx tsc --noEmit`
Expected: no errors in `ServiceDetailPage.tsx`.

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/features/catalog/pages/ServiceDetailPage.tsx src/frontend/src/__tests__/catalog/ServiceDetailPage.setup.test.tsx
git commit -m "feat(catalog): mount setup checklist on service detail with deep-link CTAs

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 4: `ServicesNeedingSetupSection` (hub query section)

**Files:**
- Create: `src/frontend/src/features/catalog/components/ServicesNeedingSetupSection.tsx`
- Test: `src/frontend/src/__tests__/catalog/ServicesNeedingSetupSection.test.tsx`

**Interfaces:**
- Consumes: `serviceCatalogApi.listServices({ lifecycleStatus })` → `{ items: { serviceId: string; displayName: string; lifecycleStatus: string; domain: string }[] }`.
- Produces: `export function ServicesNeedingSetupSection(): JSX.Element | null;` — renders nothing (null) when there are no Planning/Development services (honest-null).

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/catalog/ServicesNeedingSetupSection.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ServicesNeedingSetupSection } from '../../features/catalog/components/ServicesNeedingSetupSection';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, o?: Record<string, unknown>) => (o?.name ? `${o.name} · ${o.status}` : k) }) }));

const listServices = vi.fn();
vi.mock('../../features/catalog/api', () => ({ serviceCatalogApi: { listServices: (...a: unknown[]) => listServices(...a) } }));

function wrap(ui: React.ReactNode) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={qc}><MemoryRouter>{ui}</MemoryRouter></QueryClientProvider>);
}

describe('ServicesNeedingSetupSection', () => {
  it('lists Planning services when present', async () => {
    listServices.mockResolvedValue({ items: [{ serviceId: 's1', displayName: 'orders-api', lifecycleStatus: 'Planning', domain: 'Commerce' }] });
    wrap(<ServicesNeedingSetupSection />);
    await waitFor(() => expect(screen.getByText(/orders-api/)).toBeInTheDocument());
  });

  it('renders nothing (honest-null) when there are none', async () => {
    listServices.mockResolvedValue({ items: [] });
    const { container } = wrap(<ServicesNeedingSetupSection />);
    await waitFor(() => expect(listServices).toHaveBeenCalled());
    expect(container.textContent).toBe('');
  });
});
```

- [ ] **Step 2: Run to verify it fails**

Run: `npm run test -- ServicesNeedingSetupSection`
Expected: FAIL (module not found).

- [ ] **Step 3: Write the component**

```tsx
// src/frontend/src/features/catalog/components/ServicesNeedingSetupSection.tsx
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { ClipboardList, ArrowRight } from 'lucide-react';
import { serviceCatalogApi } from '../api';

/** Serviços ainda por configurar (Planning). Honest-null: oculta-se quando não há nenhum. */
export function ServicesNeedingSetupSection() {
  const { t } = useTranslation();
  const { data } = useQuery({
    queryKey: ['catalog-services-needing-setup'],
    queryFn: () => serviceCatalogApi.listServices({ lifecycleStatus: 'Planning', pageSize: 5 }),
    staleTime: 30_000,
  });

  const items = data?.items ?? [];
  if (items.length === 0) return null;

  return (
    <section className="mb-6">
      <h2 className="flex items-center gap-2 text-sm font-semibold text-heading mb-3">
        <ClipboardList size={16} />
        {t('selfServicePortal.needingSetup.title')}
      </h2>
      <ul className="grid grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-3">
        {items.slice(0, 5).map((s) => (
          <li key={s.serviceId}>
            <Link
              to={`/services/${s.serviceId}`}
              className="group flex items-center gap-2 rounded-lg border border-edge bg-card px-3 py-2.5 text-sm shadow-sm transition-all hover:border-accent/40"
            >
              <span className="min-w-0 truncate text-heading group-hover:text-accent">
                {t('selfServicePortal.needingSetup.item', { name: s.displayName, status: s.lifecycleStatus })}
              </span>
              <ArrowRight size={13} className="ml-auto shrink-0 text-muted group-hover:text-accent" />
            </Link>
          </li>
        ))}
      </ul>
    </section>
  );
}
```

- [ ] **Step 4: Run to verify it passes**

Run: `npm run test -- ServicesNeedingSetupSection`
Expected: PASS (2 tests).

- [ ] **Step 5: Commit**

```bash
git add src/frontend/src/features/catalog/components/ServicesNeedingSetupSection.tsx src/frontend/src/__tests__/catalog/ServicesNeedingSetupSection.test.tsx
git commit -m "feat(catalog): ServicesNeedingSetupSection (honest-null Planning list)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 5: Reboot `SelfServicePortalPage` (fix links + golden paths + needing-setup)

**Files:**
- Modify: `src/frontend/src/features/catalog/pages/SelfServicePortalPage.tsx`
- Test: `src/frontend/src/__tests__/catalog/SelfServicePortalPage.test.tsx`

**Interfaces:**
- Consumes: `ServicesNeedingSetupSection` (Task 4).
- Produces: fixed hrefs + golden-path header + mounted section.

- [ ] **Step 1: Write the failing test**

```tsx
// src/frontend/src/__tests__/catalog/SelfServicePortalPage.test.tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { SelfServicePortalPage } from '../../features/catalog/pages/SelfServicePortalPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string) => k }) }));
vi.mock('../../features/catalog/api', () => ({ serviceCatalogApi: { listServices: vi.fn(() => Promise.resolve({ items: [] })) } }));

function wrap() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(<QueryClientProvider client={qc}><MemoryRouter><SelfServicePortalPage /></MemoryRouter></QueryClientProvider>);
}

describe('SelfServicePortalPage', () => {
  it('leads with the onboarding golden path linking to /services/onboard', () => {
    wrap();
    const links = screen.getAllByRole('link').map((a) => a.getAttribute('href'));
    expect(links).toContain('/services/onboard');
  });

  it('has no dead legacy links', () => {
    wrap();
    const links = screen.getAllByRole('link').map((a) => a.getAttribute('href'));
    expect(links).not.toContain('/catalog/services/create');
    expect(links).not.toContain('/catalog/scaffold');
    expect(links).not.toContain('/contracts/governance/health');
  });
});
```

- [ ] **Step 2: Run to verify it fails**

Run: `npm run test -- SelfServicePortalPage`
Expected: FAIL (dead links still present / no onboarding link).

- [ ] **Step 3: Fix the dead links in `ACTION_GROUPS`**

In `SelfServicePortalPage.tsx`, change these exact `href` values:
- `href: '/catalog/services/create'` → `href: '/services/onboard'`
- `href: '/catalog/scaffold'` (appears twice — createAiScaffold and generateAdr) → `href: '/catalog/templates'`
- `href: '/contracts/governance/health'` → `href: '/contracts/health'`
- `href: '/contracts/new?type=rest'` → `href: '/contracts/new?type=RestApi'`
- `href: '/contracts/new?type=event'` → `href: '/contracts/new?type=Event'`
- `href: '/catalog/services'` → keep (valid).

- [ ] **Step 4: Add the golden-path header + mount the needing-setup section**

Add imports at the top:

```tsx
import { ArrowRight } from 'lucide-react';
import { ServicesNeedingSetupSection } from '../components/ServicesNeedingSetupSection';
```

Immediately after the `<PageHeader … />` in the returned JSX, insert the golden-path block and the section (before the `{ACTION_GROUPS.map(...)}`):

```tsx
      <section className="mb-6">
        <h2 className="text-sm font-semibold text-heading mb-3">{t('selfServicePortal.goldenPaths.title')}</h2>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          <Link
            to="/services/onboard"
            className="group flex items-start gap-3 rounded-lg border border-accent/30 bg-accent/5 p-4 shadow-sm transition-all hover:border-accent/60"
          >
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-accent/15 text-accent">
              <Plus size={20} />
            </div>
            <div className="min-w-0">
              <p className="text-sm font-semibold text-heading group-hover:text-accent">{t('selfServicePortal.goldenPaths.onboard')}</p>
              <p className="mt-0.5 text-xs text-muted leading-snug">{t('selfServicePortal.goldenPaths.onboard_desc')}</p>
            </div>
            <ArrowRight size={16} className="ml-auto shrink-0 text-accent" />
          </Link>
          <Link
            to="/catalog/templates"
            className="group flex items-start gap-3 rounded-lg border border-edge bg-card p-4 shadow-sm transition-all hover:border-accent/40"
          >
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-accent/10 text-accent">
              <Wand2 size={20} />
            </div>
            <div className="min-w-0">
              <p className="text-sm font-semibold text-heading group-hover:text-accent">{t('selfServicePortal.goldenPaths.template')}</p>
              <p className="mt-0.5 text-xs text-muted leading-snug">{t('selfServicePortal.goldenPaths.template_desc')}</p>
            </div>
          </Link>
        </div>
      </section>

      <ServicesNeedingSetupSection />
```

(`Plus` and `Wand2` are already imported in this file.)

- [ ] **Step 5: Run to verify it passes**

Run: `npm run test -- SelfServicePortalPage`
Expected: PASS (2 tests).
Run: `npx tsc --noEmit`
Expected: no errors.

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/features/catalog/pages/SelfServicePortalPage.tsx src/frontend/src/__tests__/catalog/SelfServicePortalPage.test.tsx
git commit -m "feat(catalog): reboot self-service hub — fix dead links, golden paths, needing-setup

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Task 6: e2e journey + final gates

**Files:**
- Create: `src/frontend/e2e/service-setup-journey.spec.ts`

- [ ] **Step 1: Write the e2e spec**

```ts
// src/frontend/e2e/service-setup-journey.spec.ts
import { test, expect } from '@playwright/test';
import { mockAuthSession } from './helpers/auth';

/** E2E — checklist de setup no detalhe de um serviço Planning + hub self-service. */
test.describe('Service setup journey', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthSession(page);
    await page.route('**/api/v1/catalog/services/svc-1/maturity**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ level: 'Bronze', dimensions: [] }) }));
    await page.route('**/api/v1/catalog/services/svc-1**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({
        id: 'svc-1', name: 'orders-api', displayName: 'orders-api', domain: 'Commerce', serviceType: 'RestApi',
        criticality: 'Medium', exposureType: 'Internal', lifecycleStatus: 'Planning', teamName: 'Orders',
        technicalOwner: '', apis: [], apiCount: 0,
      }) }));
    await page.route('**/api/v1/contracts/by-service/**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ contracts: [], totalCount: 0 }) }));
  });

  test('service detail shows the setup checklist and the contract CTA navigates', async ({ page }) => {
    await page.goto('/services/svc-1');
    await expect(page.getByText(/setup checklist/i)).toBeVisible({ timeout: 5_000 });
    await page.getByTestId('setup-cta-contract').click();
    await expect(page).toHaveURL(/\/contracts\/new\?serviceId=svc-1/, { timeout: 5_000 });
  });

  test('self-service hub leads with onboarding golden path', async ({ page }) => {
    await page.route('**/api/v1/catalog/services**', (route) =>
      route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [] }) }));
    await page.goto('/catalog/self-service');
    await expect(page.getByRole('link', { name: /onboard a service/i })).toBeVisible({ timeout: 5_000 });
  });
});
```

> If the contracts-by-service mock URL differs, align it to whatever `contractsApi.listContractsByService` calls (check `features/catalog/api/contracts.ts`); the checklist only needs the service + maturity mocks to render, so a missing contracts mock still shows the checklist (contract row simply "to-do").

- [ ] **Step 2: Run the e2e spec**

Run (PowerShell, from `src/frontend`):
```
Set-Location "C:\Users\dlima\Documents\GitHub\NexTraceOne\src\frontend"; $env:CI=""; npx playwright test --project=chromium e2e/service-setup-journey.spec.ts
```
Expected: 2 passed.

- [ ] **Step 3: Final gates**

Run (from `src/frontend`):
```bash
npm run test
npm run lint
npm run validate:i18n
npm run build
```
Expected: all green. Fix any failure before committing.

- [ ] **Step 4: Commit**

```bash
git add src/frontend/e2e/service-setup-journey.spec.ts
git commit -m "test(catalog): e2e service setup journey + hub golden path

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Self-Review

**1. Spec coverage:**
- §4.1 checklist (dimensions, contract N/A, progress, complete→lifecycle panel, honest-null) → Tasks 2 (derivation + component) + 3 (mount). Runbook/Monitoring omitted per Global Constraints (endpoint lacks clean booleans) — consistent with spec §4.1's honest-null clause. ✓
- §4.2 hub reboot (fix links, golden path, needing-setup honest-null) → Tasks 4 + 5. ✓
- §4.3 reuse (loaded data, `supportsContracts`, `listServices`, no backend) → Tasks 2/4. ✓
- §4.4 states (loading via parent, honest-null hide, Active compact via completion note, pending is N/A since no new mutation) → Tasks 2/3/4. ✓
- §4.5 testing (unit per unit, e2e, i18n×4, gates) → all tasks + Task 6. ✓
- §5 deferrals not built. ✓

**2. Placeholder scan:** No TBD/TODO; every code step has complete code; every command has expected output. The Task 3 i18n-mock note resolves the assertion explicitly (assert on the key `serviceSetup.title`). ✓

**3. Type consistency:** `SetupItem`/`SetupItemId`/`SetupServiceInput` defined in Task 2, consumed unchanged in Task 2's component and Task 3. `deriveSetupItems`/`setupProgress` signatures match between helper and test. `ServicesNeedingSetupSection` (Task 4) named import matches Task 5 usage. `onEditField(tab)` in Task 3 matches the `FormTab` subset `'ownership'|'references'`. `listServices({ lifecycleStatus, pageSize })` matches the real API signature. ✓

**One documented deviation from the spec:** the spec's §4.1 mentioned a possible "Promover a Active" CTA; per Global Constraints (DRY on lifecycle) the plan instead renders a completion note pointing at the existing `ServiceLifecyclePanel` rather than duplicating a transition button. Sound — avoids duplicating the allowed-transition state machine.
