# Feature Flags no detalhe do serviço — Plano

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development para implementar tarefa-a-tarefa. Steps usam checkbox (`- [ ]`).

**Goal:** Mover as feature flags de um serviço para uma tab no detalhe do serviço (consulta + toggle) e remover o item de menu solto, mantendo o dashboard de portefólio reachable por deep-link.

**Architecture:** Extrair a api+tipos de feature flags para um módulo partilhado; construir uma tab per-serviço que filtra o dashboard por `serviceId`; ligá-la ao `ServiceDetailPage`; remover o item da sidebar.

**Tech Stack:** React 19, TS 5.9, TanStack Query 5, react-i18next, Vitest + Testing Library.

## Global Constraints

- UI só via chaves i18n — nunca strings hardcoded. 4 locales flat `src/frontend/src/locales/{en,es,pt-BR,pt-PT}.json` — chaves novas em TODOS os 4.
- Testes Vitest só descobertos em `src/frontend/src/__tests__/**`.
- Gates por tarefa: `npm run build` (tsc incluído), `npm run lint`, testes afetados. Suíte completa no fim (correr de `src/frontend`).
- Mudanças cirúrgicas: não apagar código morto pré-existente não relacionado; não refatorar o que não faz parte do pedido.
- Comentários/XML-docs em português; identificadores em inglês. TS strict, TreatWarningsAsErrors.
- `shared/ui` exporta `Button, Select, SearchInput, Toggle, Tabs`. `useEnvironment()` (de `contexts/EnvironmentContext`) expõe `activeEnvironmentId: string | null`. `client` default export em `src/frontend/src/api/client`.

---

### Task 1: Extrair api+tipos de feature flags para módulo partilhado

**Files:**
- Create: `src/frontend/src/features/catalog/api/featureFlags.ts`
- Modify: `src/frontend/src/features/catalog/pages/ServiceFeatureFlagsPage.tsx`
- Test: `src/frontend/src/__tests__/catalog/featureFlagsApi.test.ts` (novo)

**Interfaces:**
- Produces: `export interface ServiceFeatureFlag`, `export interface ServiceFeatureFlagDashboard`, `export const serviceFeatureFlagsApi = { getDashboard(): Promise<ServiceFeatureFlagDashboard>, toggle(flagId: string, enabled: boolean): Promise<void> }`.

- [ ] **Step 1: Escrever o teste que falha**

Criar `src/frontend/src/__tests__/catalog/featureFlagsApi.test.ts`:

```ts
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { serviceFeatureFlagsApi } from '../../features/catalog/api/featureFlags';
import client from '../../api/client';

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), patch: vi.fn() },
}));

describe('serviceFeatureFlagsApi', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('getDashboard faz GET /catalog/feature-flags', async () => {
    (client.get as ReturnType<typeof vi.fn>).mockResolvedValue({ data: { totalFlags: 0, enabledFlags: 0, disabledFlags: 0, affectedServices: 0, flags: [] } });
    const res = await serviceFeatureFlagsApi.getDashboard();
    expect(client.get).toHaveBeenCalledWith('/catalog/feature-flags');
    expect(res.flags).toEqual([]);
  });

  it('toggle faz PATCH /catalog/feature-flags/:id', async () => {
    (client.patch as ReturnType<typeof vi.fn>).mockResolvedValue({ data: {} });
    await serviceFeatureFlagsApi.toggle('f1', true);
    expect(client.patch).toHaveBeenCalledWith('/catalog/feature-flags/f1', { enabled: true });
  });
});
```

- [ ] **Step 2: Correr — deve falhar**

Run: `cd src/frontend && npx vitest run src/__tests__/catalog/featureFlagsApi.test.ts`
Expected: FAIL (módulo inexistente).

- [ ] **Step 3: Criar o módulo**

Criar `src/frontend/src/features/catalog/api/featureFlags.ts` movendo verbatim de `ServiceFeatureFlagsPage.tsx` os dois `export interface` (`ServiceFeatureFlag`, `ServiceFeatureFlagDashboard`) e o objeto `serviceFeatureFlagsApi` (com `getDashboard` e `toggle`). Importar `client` de `../../../api/client`. XML-doc em português.

```ts
import client from '../../../api/client';

/** Feature flag escopada por serviço (tabela ctr_feature_flag_records). */
export interface ServiceFeatureFlag { /* campos verbatim */ }

/** Dashboard agregado de feature flags de todos os serviços. */
export interface ServiceFeatureFlagDashboard { /* campos verbatim */ }

/** Cliente de feature flags do catálogo. */
export const serviceFeatureFlagsApi = {
  getDashboard: async (): Promise<ServiceFeatureFlagDashboard> => {
    const res = await client.get<ServiceFeatureFlagDashboard>('/catalog/feature-flags');
    return res.data;
  },
  toggle: async (flagId: string, enabled: boolean): Promise<void> => {
    await client.patch(`/catalog/feature-flags/${flagId}`, { enabled });
  },
};
```

- [ ] **Step 4: Página passa a importar do módulo**

Em `ServiceFeatureFlagsPage.tsx`: remover os dois `export interface` e o `const serviceFeatureFlagsApi` locais e importar do novo módulo:
`import { serviceFeatureFlagsApi, type ServiceFeatureFlag } from '../api/featureFlags';`
Remover o import `client` se ficar sem uso na página. O resto da página inalterado.

- [ ] **Step 5: Correr o teste + verificar a página**

Run: `cd src/frontend && npx vitest run src/__tests__/catalog/featureFlagsApi.test.ts`
Expected: PASS.
Run: `cd src/frontend && npm run build && npm run lint`
Expected: 0 erros (sem `client` órfão na página).

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/features/catalog/api/featureFlags.ts src/frontend/src/features/catalog/pages/ServiceFeatureFlagsPage.tsx src/frontend/src/__tests__/catalog/featureFlagsApi.test.ts
git commit -m "refactor(catalog): extrai api+tipos de feature flags para módulo partilhado"
```

---

### Task 2: Componente `ServiceFeatureFlagsTab`

**Files:**
- Create: `src/frontend/src/features/catalog/components/ServiceFeatureFlagsTab.tsx`
- Modify: `src/frontend/src/locales/{en,es,pt-BR,pt-PT}.json`
- Test: `src/frontend/src/__tests__/catalog/ServiceFeatureFlagsTab.test.tsx` (novo)

**Interfaces:**
- Consumes: `serviceFeatureFlagsApi`, `ServiceFeatureFlag` de `../api/featureFlags`; `useEnvironment` de `../../../contexts/EnvironmentContext`; `Toggle` de `../../../shared/ui` (ou `../../../components/Toggle`); `Card, CardHeader, CardBody` de `../../../components/Card`; `TableWrapper` de `../../../components/shell`; `Link` de `react-router-dom`.
- Produces: `export function ServiceFeatureFlagsTab({ serviceId }: { serviceId: string })`.

- [ ] **Step 1: Escrever o teste que falha**

Criar `src/frontend/src/__tests__/catalog/ServiceFeatureFlagsTab.test.tsx`:

```tsx
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ServiceFeatureFlagsTab } from '../../features/catalog/components/ServiceFeatureFlagsTab';
import { serviceFeatureFlagsApi } from '../../features/catalog/api/featureFlags';

vi.mock('react-i18next', async (orig) => ({
  ...(await orig<typeof import('react-i18next')>()),
  useTranslation: () => ({ t: (_k: string, d?: string) => d ?? _k, i18n: { language: 'en' } }),
}));
vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: () => ({ activeEnvironmentId: 'env-prod' }),
}));
vi.mock('../../features/catalog/api/featureFlags', () => ({
  serviceFeatureFlagsApi: { getDashboard: vi.fn(), toggle: vi.fn() },
}));

const flags = [
  { id: 'f1', serviceId: 'svc-1', serviceName: 'A', flagKey: 'new_ui', displayName: 'New UI', enabled: true, environment: 'prod', updatedAt: '2026-01-01T00:00:00Z' },
  { id: 'f2', serviceId: 'svc-2', serviceName: 'B', flagKey: 'beta', displayName: 'Beta', enabled: false, environment: 'prod', updatedAt: '2026-01-01T00:00:00Z' },
];

function renderTab(serviceId = 'svc-1') {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter><ServiceFeatureFlagsTab serviceId={serviceId} /></MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ServiceFeatureFlagsTab', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (serviceFeatureFlagsApi.getDashboard as ReturnType<typeof vi.fn>).mockResolvedValue({
      totalFlags: 2, enabledFlags: 1, disabledFlags: 1, affectedServices: 2, flags,
    });
  });

  it('mostra só as flags do serviço dado', async () => {
    renderTab('svc-1');
    expect(await screen.findByText('New UI')).toBeInTheDocument();
    expect(screen.queryByText('Beta')).not.toBeInTheDocument();
  });

  it('tem deep-link ao dashboard de portefólio', async () => {
    renderTab('svc-1');
    await screen.findByText('New UI');
    const link = screen.getByRole('link', { name: /portfolio|portefólio|portfólio/i });
    expect(link).toHaveAttribute('href', '/services/feature-flags');
  });

  it('o toggle chama a mutation', async () => {
    (serviceFeatureFlagsApi.toggle as ReturnType<typeof vi.fn>).mockResolvedValue(undefined);
    renderTab('svc-1');
    await screen.findByText('New UI');
    const toggle = screen.getByRole('switch');
    fireEvent.click(toggle);
    await waitFor(() => expect(serviceFeatureFlagsApi.toggle).toHaveBeenCalledWith('f1', false));
  });

  it('estado vazio quando o serviço não tem flags', async () => {
    renderTab('svc-999');
    await waitFor(() => expect(serviceFeatureFlagsApi.getDashboard).toHaveBeenCalled());
    expect(await screen.findByText(/no registered feature flags|não tem feature flags|no tiene feature flags/i)).toBeInTheDocument();
  });
});
```

Nota: se o componente `Toggle` não expõe `role="switch"`, ajustar o seletor do teste do toggle para o elemento clicável real do `Toggle` (ler `src/frontend/src/components/Toggle.tsx` primeiro).

- [ ] **Step 2: Correr — deve falhar**

Run: `cd src/frontend && npx vitest run src/__tests__/catalog/ServiceFeatureFlagsTab.test.tsx`
Expected: FAIL.

- [ ] **Step 3: Implementar o componente**

Criar `ServiceFeatureFlagsTab.tsx`. Ler primeiro `src/frontend/src/components/Toggle.tsx` para a assinatura de props (`checked`, `onChange(checked)`, `disabled`, `label`) e o `role` que expõe. Estrutura:
- `const { activeEnvironmentId } = useEnvironment();`
- `useQuery({ queryKey: ['service-feature-flags', activeEnvironmentId], queryFn: () => serviceFeatureFlagsApi.getDashboard() })`.
- `const flags = (data?.flags ?? []).filter(f => f.serviceId === serviceId);`
- `useMutation` toggle → `invalidateQueries({ queryKey: ['service-feature-flags'] })`.
- Card com header: título `t('serviceDetail.tabFeatureFlags', 'Feature Flags')` + `<Link to="/services/feature-flags">{t('featureFlags.viewPortfolio', 'View all portfolio flags')} →</Link>`.
- Body: loading (spinner igual ao da página), erro (bloco crítico com `featureFlags.errorTitle`/`errorDesc` existentes), vazio (`featureFlags.serviceEmpty`), ou tabela com colunas Flag (displayName+flagKey+description), Ambiente (`featureFlags.colEnvironment`), Atualizado (`featureFlags.colUpdated`, data + updatedBy), Status (`Toggle`). Reutilizar as chaves i18n `featureFlags.*` existentes para as colunas. SEM coluna "Serviço".

- [ ] **Step 4: Adicionar chaves i18n aos 4 locales**

Dentro do objeto `featureFlags` de cada `locales/{en,es,pt-BR,pt-PT}.json`:
- `viewPortfolio`: EN "View all portfolio flags" · ES "Ver todas las flags del portafolio" · pt-BR "Ver todas as flags do portfólio" · pt-PT "Ver todas as flags do portefólio"
- `serviceEmpty`: EN "This service has no registered feature flags." · ES "Este servicio no tiene feature flags registradas." · pt-BR "Este serviço não tem feature flags registradas." · pt-PT "Este serviço não tem feature flags registadas."

- [ ] **Step 5: Correr — deve passar**

Run: `cd src/frontend && npx vitest run src/__tests__/catalog/ServiceFeatureFlagsTab.test.tsx`
Expected: PASS.

- [ ] **Step 6: Build + lint**

Run: `cd src/frontend && npm run build && npm run lint`
Expected: 0 erros.

- [ ] **Step 7: Commit**

```bash
git add src/frontend/src/features/catalog/components/ServiceFeatureFlagsTab.tsx src/frontend/src/locales src/frontend/src/__tests__/catalog/ServiceFeatureFlagsTab.test.tsx
git commit -m "feat(catalog): ServiceFeatureFlagsTab — feature flags do serviço no detalhe"
```

---

### Task 3: Wiring da tab no `ServiceDetailPage`

**Files:**
- Modify: `src/frontend/src/features/catalog/pages/ServiceDetailPage.tsx`
- Modify: `src/frontend/src/locales/{en,es,pt-BR,pt-PT}.json`
- Test: `src/frontend/src/__tests__/catalog/ServiceDetailPage.featureFlagsTab.test.tsx` (novo)

**Interfaces:**
- Consumes: `ServiceFeatureFlagsTab` da Task 2.
- Produces: tab `featureFlags` "Feature Flags" no detalhe do serviço.

- [ ] **Step 1: Escrever o teste que falha**

Criar `src/frontend/src/__tests__/catalog/ServiceDetailPage.featureFlagsTab.test.tsx` reutilizando o padrão de mocks dos testes existentes de `ServiceDetailPage` (ver `ServiceDetailPage.contractDrawer.test.tsx`: mocks de `serviceCatalogApi`, `contractsApi`, `EnvironmentContext`, `ServiceLifecyclePanel`, `ServiceLinksSection`, `AssistantPanel`, `ServiceContractDrawer`, `react-router-dom` com `useParams`/`useSearchParams`). Mockar também `ServiceFeatureFlagsTab` como stub (`() => <div data-testid="ff-tab" />`). Asserir:

```tsx
expect(await screen.findByRole('tab', { name: /feature flags/i })).toBeInTheDocument();
fireEvent.click(screen.getByRole('tab', { name: /feature flags/i }));
expect(await screen.findByTestId('ff-tab')).toBeInTheDocument();
```

- [ ] **Step 2: Correr — deve falhar**

Run: `cd src/frontend && npx vitest run src/__tests__/catalog/ServiceDetailPage.featureFlagsTab.test.tsx`
Expected: FAIL.

- [ ] **Step 3: Implementar o wiring**

Em `ServiceDetailPage.tsx`:
1. Adicionar `'featureFlags'` ao tipo `ServiceTab`.
2. Importar `ServiceFeatureFlagsTab` de `../components/ServiceFeatureFlagsTab` e o ícone `Sliders` de `lucide-react` (verificar se já está importado; se não, adicionar).
3. No `viewTabItems`, após o item `score`: `{ id: 'featureFlags', label: t('serviceDetail.tabFeatureFlags', 'Feature Flags'), icon: <Sliders size={14} /> }`.
4. No render, junto às outras: `{activeViewTab === 'featureFlags' && <ServiceFeatureFlagsTab serviceId={serviceId} />}`.

- [ ] **Step 4: Adicionar chave i18n aos 4 locales**

Dentro de `serviceDetail` em cada `locales/*.json`: `"tabFeatureFlags": "Feature Flags"` (igual nos 4).

- [ ] **Step 5: Correr o novo teste + os existentes de ServiceDetailPage**

Run: `cd src/frontend && npx vitest run src/__tests__/catalog/ServiceDetailPage`
Expected: PASS em todos.

- [ ] **Step 6: Build + lint**

Run: `cd src/frontend && npm run build && npm run lint`
Expected: 0 erros.

- [ ] **Step 7: Commit**

```bash
git add src/frontend/src/features/catalog/pages/ServiceDetailPage.tsx src/frontend/src/locales src/frontend/src/__tests__/catalog/ServiceDetailPage.featureFlagsTab.test.tsx
git commit -m "feat(catalog): detalhe do serviço ganha tab Feature Flags"
```

---

### Task 4: Remover o item Feature Flags da sidebar

**Files:**
- Modify: `src/frontend/src/components/shell/AppSidebar.tsx`
- Test: `src/frontend/src/__tests__/` — teste de sidebar novo OU asserção sobre navItems (ver Step 1)

**Interfaces:**
- Produces: sidebar sem o item `/services/feature-flags`.

- [ ] **Step 1: Escrever/ajustar o teste que falha**

Verificar se existe um teste de `AppSidebar` em `src/frontend/src/__tests__/`. Se existir e asserir a presença/contagem de itens de catálogo, ajustar. Caso contrário, criar `src/frontend/src/__tests__/components/AppSidebar.featureFlags.test.tsx` que renderiza a `AppSidebar` (com os providers/mocks mínimos que ela exige — ler o componente e um teste existente que a renderize) e asseri que **não** há link para `/services/feature-flags`:

```tsx
expect(screen.queryByRole('link', { name: /feature flags/i })).not.toBeInTheDocument();
```

Se renderizar a `AppSidebar` exigir demasiado scaffolding (auth/permissions/router), preferir um teste unitário que importe o array `navItems` — mas `navItems` não é exportado; nesse caso exportar `navItems` (named export, mudança mínima) e asserir `expect(navItems.find(i => i.to === '/services/feature-flags')).toBeUndefined()`.

- [ ] **Step 2: Correr — deve falhar**

Run: `cd src/frontend && npx vitest run <ficheiro do teste>`
Expected: FAIL (item ainda presente).

- [ ] **Step 3: Remover o item**

Em `AppSidebar.tsx`, remover a linha do item `sidebar.featureFlags` (`to: '/services/feature-flags'`, ~linha 63). **NÃO** remover o import `Sliders` (ainda usado por `releaseControlParameters`, ~linha 94). Se o Step 1 exigiu exportar `navItems`, manter esse `export`.

- [ ] **Step 4: Correr — deve passar**

Run: `cd src/frontend && npx vitest run <ficheiro do teste>`
Expected: PASS.

- [ ] **Step 5: Build + lint**

Run: `cd src/frontend && npm run build && npm run lint`
Expected: 0 erros; sem import órfão.

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/components/shell/AppSidebar.tsx src/frontend/src/__tests__
git commit -m "feat(catalog): remove Feature Flags do menu (agora vive no detalhe do serviço)"
```

---

### Task 5: Gates finais + verificação de stub

**Files:** nenhum (verificação, feita pelo controlador).

- [ ] **Step 1: Suíte completa** — `cd src/frontend && npm run test -- --run` → toda passa.
- [ ] **Step 2: Build** — `cd src/frontend && npm run build` → sucesso.
- [ ] **Step 3: Stub** — `npm run stub`; verificar: (a) sidebar sem "Feature Flags" na secção Catálogo; (b) detalhe de um serviço mostra a tab "Feature Flags" com as flags só desse serviço + deep-link ao portefólio; (c) `/services/feature-flags` continua acessível por URL; (d) 0 erros de consola.
