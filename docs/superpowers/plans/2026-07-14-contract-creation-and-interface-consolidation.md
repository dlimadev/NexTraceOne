# Criação de contrato ancorada ao serviço + consolidação APIs/Interfaces — Plano

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development para implementar tarefa-a-tarefa. Steps usam checkbox (`- [ ]`).

**Goal:** Remover as entradas soltas de criação de contrato (o contrato passa a nascer só do serviço/interface ou do onboarding wizard) e consolidar as abas "APIs" e "Interfaces" da tela de detalhe do serviço numa só superfície.

**Architecture:** Frontend React 19 / TS. Peça A é redirect de rotas + remoção de CTAs. Peça B extrai a tabela de APIs inline para um componente e empilha-a com o tab de interfaces sob uma aba unificada.

**Tech Stack:** React 19, react-router-dom 7, react-i18next, Vitest + Testing Library.

## Global Constraints

- UI só via chaves i18n — nunca strings hardcoded. 4 locales flat: `src/frontend/src/locales/{en,es,pt-BR,pt-PT}.json`.
- Testes Vitest só são descobertos em `src/frontend/src/__tests__/**/*.test.{ts,tsx}`.
- Gates por tarefa: `npm run build` (tsc incluído), `npm run lint`, testes afetados. Suíte completa no fim.
- Mudanças cirúrgicas: não apagar código morto pré-existente; não refatorar o que não faz parte do pedido.
- Comentários/XML docs em português; identificadores em inglês.
- Rota estática `/contracts/studio/new` tem precedência sobre `/contracts/studio/:draftId` no react-router 7 (ranking por especificidade), independente da ordem.

---

### Task 1: Redirecionar rotas de criação de contrato

**Files:**
- Modify: `src/frontend/src/routes/contractsRoutes.tsx`
- Test: `src/frontend/src/__tests__/contracts/contractCreationRoutes.test.tsx` (novo)

**Interfaces:**
- Consumes: `Navigate` de `react-router-dom` (já importado no ficheiro).
- Produces: rotas `/contracts/new` e `/contracts/studio/new` que renderizam `<Navigate to="/contracts" replace />`.

- [ ] **Step 1: Escrever teste que falha**

Criar `src/frontend/src/__tests__/contracts/contractCreationRoutes.test.tsx`. Renderiza a app de rotas mínima montando `ContractsRoutes` dentro de um `MemoryRouter` e um `<Routes>`, com um destino sentinela em `/contracts`. Como as páginas são `lazy`, mockar os módulos lazy pesados não é necessário se testarmos só o redirect — mas para evitar carregar `CreateContractPage`/`ContractStudioPage`, o teste deve asserir que ao navegar para `/contracts/new` a URL efetiva resolve para o elemento de `/contracts`. Abordagem concreta:

```tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { Suspense } from 'react';

// Mock ProtectedRoute para passar sempre (sem auth) e as páginas lazy alvo do catálogo
vi.mock('../../components/ProtectedRoute', () => ({
  ProtectedRoute: ({ children }: { children: React.ReactNode }) => <>{children}</>,
}));
vi.mock('../../features/contracts/catalog/ContractCatalogPage', () => ({
  ContractCatalogPage: () => <div>CONTRACT_CATALOG</div>,
}));

import { ContractsRoutes } from '../../routes/contractsRoutes';

function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Suspense fallback={<div>loading</div>}>
        <Routes>{ContractsRoutes()}</Routes>
      </Suspense>
    </MemoryRouter>,
  );
}

describe('rotas de criação de contrato redirecionam para o catálogo', () => {
  it('/contracts/new redireciona para /contracts', async () => {
    renderAt('/contracts/new');
    expect(await screen.findByText('CONTRACT_CATALOG')).toBeInTheDocument();
  });

  it('/contracts/studio/new redireciona para /contracts', async () => {
    renderAt('/contracts/studio/new');
    expect(await screen.findByText('CONTRACT_CATALOG')).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Correr o teste — deve falhar**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts/contractCreationRoutes.test.tsx`
Expected: FAIL (as rotas ainda renderizam as páginas de criação, não o catálogo).

- [ ] **Step 3: Implementar os redirects**

Em `contractsRoutes.tsx`, substituir o elemento das duas rotas por `Navigate`. Manter os `path` e a estrutura `<Route>`:

```tsx
<Route path="/contracts/new" element={<Navigate to="/contracts" replace />} />
```
```tsx
{/* Wave V3.12 — hub "do zero" desativado: contrato nasce do serviço/onboarding */}
<Route path="/contracts/studio/new" element={<Navigate to="/contracts" replace />} />
```

Remover os imports lazy `CreateContractPage` e `ContractStudioPage` se ficarem sem uso (verificar: `CreateContractPage` só é usado na rota `/contracts/new`; `ContractStudioPage` só na `/contracts/studio/new`). Remover ambos os `const ... = lazy(...)` órfãos. `DraftStudioPage` (rota `:draftId`) mantém-se.

- [ ] **Step 4: Correr o teste — deve passar**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts/contractCreationRoutes.test.tsx`
Expected: PASS.

- [ ] **Step 5: Verificar build e lint**

Run: `cd src/frontend && npm run build && npm run lint`
Expected: tsc 0 erros; eslint sem novos erros (imports lazy órfãos removidos).

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/routes/contractsRoutes.tsx src/frontend/src/__tests__/contracts/contractCreationRoutes.test.tsx
git commit -m "feat(contracts): rotas de criação de contrato redirecionam para o catálogo"
```

---

### Task 2: Remover o botão "New contract" do Contract Catalog

**Files:**
- Modify: `src/frontend/src/features/contracts/catalog/ContractCatalogPage.tsx`
- Test: `src/frontend/src/__tests__/contracts/ContractCatalogPage.browse.test.tsx` (existente — ajustar)

**Interfaces:**
- Consumes: nada novo.
- Produces: `ContractCatalogPage` sem CTA de criação.

- [ ] **Step 1: Ler o teste existente**

Abrir `src/frontend/src/__tests__/contracts/ContractCatalogPage.browse.test.tsx`. Se algum caso asserir a presença do botão "New contract"/`newContract`, invertê-lo para asserir a **ausência**. Adicionar um caso explícito:

```tsx
it('não mostra o botão de criação de contrato (contrato nasce do serviço)', async () => {
  // ...render existente da página...
  expect(screen.queryByRole('button', { name: /new contract/i })).not.toBeInTheDocument();
});
```

(Reutilizar o setup de render/mocks já existente no ficheiro; não duplicar mocks.)

- [ ] **Step 2: Correr o teste — deve falhar**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts/ContractCatalogPage.browse.test.tsx`
Expected: FAIL (botão ainda presente).

- [ ] **Step 3: Remover o botão**

Em `ContractCatalogPage.tsx`, remover o bloco do `<Button ... onClick={() => navigate('/contracts/new')}>` (à volta da linha 77) e o `{t('contracts.catalog.actions.newContract', ...)}`. Se `navigate` ficar sem uso, remover o `useNavigate`/`navigate`. Se o único `action` do `PageHeader` era esse botão, remover a prop `actions` (verificar a assinatura do header usado). Não tocar no resto da página.

- [ ] **Step 4: Correr o teste — deve passar**

Run: `cd src/frontend && npx vitest run src/__tests__/contracts/ContractCatalogPage.browse.test.tsx`
Expected: PASS.

- [ ] **Step 5: Build + lint**

Run: `cd src/frontend && npm run build && npm run lint`
Expected: 0 erros; sem `navigate` órfão.

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/features/contracts/catalog/ContractCatalogPage.tsx src/frontend/src/__tests__/contracts/ContractCatalogPage.browse.test.tsx
git commit -m "feat(contracts): Contract Catalog deixa de oferecer criação de contrato"
```

---

### Task 3: Redirecionar CTAs soltos para o onboarding wizard

**Files:**
- Modify: `src/frontend/src/features/catalog/pages/SelfServicePortalPage.tsx`
- Modify: `src/frontend/src/features/contracts/publication/PublicationCenterPage.tsx`
- Test: nenhum novo (mudança de destino de navegação de baixo risco; coberto por build/lint e verificação de stub)

**Interfaces:**
- Consumes: `/services/onboard` (rota existente do onboarding wizard).
- Produces: os CTAs de criação apontam para `/services/onboard`.

- [ ] **Step 1: Ajustar SelfServicePortalPage**

Nas linhas ~78 e ~84, os tiles com `href: '/contracts/new?type=RestApi'` e `href: '/contracts/new?type=Event'` passam a `href: '/services/onboard'`. Manter labels/descrições (não inventar copy). Se os dois tiles ficarem idênticos e redundantes, manter ambos (cirúrgico) — a consolidação de tiles não faz parte deste pedido.

- [ ] **Step 2: Ajustar PublicationCenterPage**

Na linha ~64, `onClick={() => navigate('/contracts/studio/new')}` passa a `onClick={() => navigate('/services/onboard')}`.

- [ ] **Step 3: Build + lint**

Run: `cd src/frontend && npm run build && npm run lint`
Expected: 0 erros.

- [ ] **Step 4: Commit**

```bash
git add src/frontend/src/features/catalog/pages/SelfServicePortalPage.tsx src/frontend/src/features/contracts/publication/PublicationCenterPage.tsx
git commit -m "feat(catalog): CTAs de criação de contrato apontam para o onboarding wizard"
```

---

### Task 4: Extrair `ServiceApisSection` da tabela de APIs inline

**Files:**
- Create: `src/frontend/src/features/catalog/components/ServiceApisSection.tsx`
- Test: `src/frontend/src/__tests__/catalog/ServiceApisSection.test.tsx` (novo)

**Interfaces:**
- Consumes: `ServiceApiSummary` de `../../../types`; componentes `Card`, `CardHeader`, `CardBody` de `../../../components/Card`; `TableWrapper` de `../../../components/shell`; `Badge` de `../../../components/Badge`; ícones `Globe`, `Eye` de `lucide-react`; `useTranslation`.
- Produces: `export function ServiceApisSection({ apis }: { apis: ServiceApiSummary[] })`.

- [ ] **Step 1: Escrever o teste que falha**

Criar `src/frontend/src/__tests__/catalog/ServiceApisSection.test.tsx`:

```tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ServiceApisSection } from '../../features/catalog/components/ServiceApisSection';
import type { ServiceApiSummary } from '../../types';

vi.mock('react-i18next', async (orig) => ({
  ...(await orig<typeof import('react-i18next')>()),
  useTranslation: () => ({ t: (_k: string, d?: string) => d ?? _k, i18n: { language: 'en' } }),
}));

const api: ServiceApiSummary = {
  apiId: 'a1', name: 'payments-api', routePattern: '/v1/payments', version: '1.2.0',
  visibility: 'Public', isDecommissioned: false, consumerCount: 3,
};

describe('ServiceApisSection', () => {
  it('renderiza uma linha por API', () => {
    render(<ServiceApisSection apis={[api]} />);
    expect(screen.getByText('payments-api')).toBeInTheDocument();
    expect(screen.getByText('/v1/payments')).toBeInTheDocument();
  });

  it('mostra estado vazio quando não há APIs', () => {
    render(<ServiceApisSection apis={[]} />);
    expect(screen.getByText(/no.*api/i)).toBeInTheDocument();
  });
});
```

- [ ] **Step 2: Correr — deve falhar**

Run: `cd src/frontend && npx vitest run src/__tests__/catalog/ServiceApisSection.test.tsx`
Expected: FAIL (componente inexistente).

- [ ] **Step 3: Implementar `ServiceApisSection`**

Criar o componente movendo **verbatim** o markup do bloco `activeViewTab === 'apis'` de `ServiceDetailPage.tsx` (linhas 1110–1162): o `<Card>` com header (`Globe` + título `catalog.detail.apis`) e a tabela / estado vazio (`catalog.detail.noApis`). Colunas: name, routePattern, version, visibility (`Eye`), consumerCount, status (`Badge` decommissioned/active). Assinatura:

```tsx
export function ServiceApisSection({ apis }: { apis: ServiceApiSummary[] }) {
  const { t } = useTranslation();
  // ...markup movido...
}
```

Não alterar colunas, chaves i18n, nem lógica. XML doc em português a descrever o componente.

- [ ] **Step 4: Correr — deve passar**

Run: `cd src/frontend && npx vitest run src/__tests__/catalog/ServiceApisSection.test.tsx`
Expected: PASS.

- [ ] **Step 5: Build + lint**

Run: `cd src/frontend && npm run build && npm run lint`
Expected: 0 erros.

- [ ] **Step 6: Commit**

```bash
git add src/frontend/src/features/catalog/components/ServiceApisSection.tsx src/frontend/src/__tests__/catalog/ServiceApisSection.test.tsx
git commit -m "refactor(catalog): extrai ServiceApisSection da tabela de APIs inline"
```

---

### Task 5: Badge "API" nos tipos de interface consumíveis

**Files:**
- Modify: `src/frontend/src/features/catalog/components/ServiceInterfacesTab.tsx`
- Modify: `src/frontend/src/locales/{en,es,pt-BR,pt-PT}.json`
- Test: `src/frontend/src/__tests__/catalog/ServiceInterfacesTab.apiBadge.test.tsx` (novo)

**Interfaces:**
- Consumes: `InterfaceType`, `ServiceInterface` (já importados no tab).
- Produces: badge "API" visível para `RestApi`/`GraphqlApi`/`GrpcService`/`SoapService`.

- [ ] **Step 1: Escrever o teste que falha**

Criar `src/frontend/src/__tests__/catalog/ServiceInterfacesTab.apiBadge.test.tsx`. Mockar `serviceCatalogApi.listServiceInterfaces` para devolver duas interfaces (uma `RestApi`, uma `KafkaProducer`) e `react-i18next` (t = fallback). Renderizar com `QueryClientProvider`. Asserir:

```tsx
// a interface RestApi mostra o badge "API"; a KafkaProducer não
const apiBadges = screen.getAllByText('API');
expect(apiBadges.length).toBe(1);
```

(Definir os objetos `ServiceInterface` completos no teste; incluir os campos obrigatórios do tipo.)

- [ ] **Step 2: Correr — deve falhar**

Run: `cd src/frontend && npx vitest run src/__tests__/catalog/ServiceInterfacesTab.apiBadge.test.tsx`
Expected: FAIL.

- [ ] **Step 3: Implementar o badge**

Em `ServiceInterfacesTab.tsx`, definir uma constante de tipos consumíveis e renderizar o badge na coluna de tipo (junto ao badge de tipo existente):

```tsx
const API_INTERFACE_TYPES: ReadonlySet<InterfaceType> = new Set([
  'RestApi', 'GraphqlApi', 'GrpcService', 'SoapService',
]);
```

Na célula de tipo, após o `<Badge>` do tipo:

```tsx
{API_INTERFACE_TYPES.has(iface.interfaceType) && (
  <Badge variant="success" size="sm" className="ml-1">{t('serviceInterfaces.apiBadge', 'API')}</Badge>
)}
```

- [ ] **Step 4: Adicionar a chave i18n nos 4 locales**

Em cada `locales/{en,es,pt-BR,pt-PT}.json`, dentro do objeto `serviceInterfaces`, adicionar `"apiBadge": "API"`.

- [ ] **Step 5: Correr — deve passar**

Run: `cd src/frontend && npx vitest run src/__tests__/catalog/ServiceInterfacesTab.apiBadge.test.tsx`
Expected: PASS.

- [ ] **Step 6: Build + lint**

Run: `cd src/frontend && npm run build && npm run lint`
Expected: 0 erros.

- [ ] **Step 7: Commit**

```bash
git add src/frontend/src/features/catalog/components/ServiceInterfacesTab.tsx src/frontend/src/locales
git add src/frontend/src/__tests__/catalog/ServiceInterfacesTab.apiBadge.test.tsx
git commit -m "feat(catalog): badge API nos tipos de interface consumíveis"
```

---

### Task 6: Consolidar abas "APIs" + "Interfaces" na tela do serviço

**Files:**
- Modify: `src/frontend/src/features/catalog/pages/ServiceDetailPage.tsx`
- Modify: `src/frontend/src/locales/{en,es,pt-BR,pt-PT}.json`
- Test: `src/frontend/src/__tests__/catalog/ServiceDetailPage.interfacesTab.test.tsx` (novo)

**Interfaces:**
- Consumes: `ServiceApisSection` da Task 4; `ServiceInterfacesTab` (existente).
- Produces: uma aba unificada `interfaces` com label "Interfaces & APIs"; sem aba `apis`.

- [ ] **Step 1: Escrever o teste que falha**

Criar `src/frontend/src/__tests__/catalog/ServiceDetailPage.interfacesTab.test.tsx`. Reutilizar o padrão de mocks dos testes existentes de `ServiceDetailPage` (mockar `serviceCatalogApi.getServiceDetail`/`getServiceMaturity`, `contractsApi.listContractsByService`, `EnvironmentContext`, `ServiceLifecyclePanel`, `ServiceLinksSection`, `AssistantPanel`, `ServiceContractDrawer`, `react-router-dom` com `useParams`/`useSearchParams`). Mockar também `ServiceApisSection` e `ServiceInterfacesTab` como stubs (`() => <div data-testid="apis-section"/>` e `() => <div data-testid="interfaces-tab"/>`) para evitar queries pesadas. Asserir:

```tsx
// existe a aba unificada e não existem abas separadas
expect(screen.getByRole('tab', { name: /interfaces & apis/i })).toBeInTheDocument();
expect(screen.queryByRole('tab', { name: /^apis \(/i })).not.toBeInTheDocument();
// ao ativar a aba, ambas as secções são renderizadas
fireEvent.click(screen.getByRole('tab', { name: /interfaces & apis/i }));
expect(await screen.findByTestId('apis-section')).toBeInTheDocument();
expect(screen.getByTestId('interfaces-tab')).toBeInTheDocument();
```

- [ ] **Step 2: Correr — deve falhar**

Run: `cd src/frontend && npx vitest run src/__tests__/catalog/ServiceDetailPage.interfacesTab.test.tsx`
Expected: FAIL.

- [ ] **Step 3: Implementar a consolidação**

Em `ServiceDetailPage.tsx`:
1. Remover `'apis'` do tipo `ServiceTab` (linha 236).
2. No `viewTabItems` (linhas 477–494): remover o item `{ id: 'apis', ... }`; alterar o item `interfaces` para `label: t('serviceDetail.tabInterfacesApis', 'Interfaces & APIs')` (manter `icon: <Server .../>`).
3. Importar `ServiceApisSection`.
4. Remover o bloco inteiro `activeViewTab === 'apis'` (linhas 1110–1162).
5. Substituir o render `activeViewTab === 'interfaces'` (linha 1250) por:

```tsx
{activeViewTab === 'interfaces' && (
  <div className="space-y-4">
    <div>
      <h3 className="text-xs font-medium uppercase tracking-wider text-muted mb-2">
        {t('serviceDetail.publishedApis', 'Published APIs')}
      </h3>
      <ServiceApisSection apis={service.apis} />
    </div>
    <ServiceInterfacesTab serviceId={serviceId} />
  </div>
)}
```

Verificar que `Globe`/`Eye`/`ServiceApiSummary` ainda são usados noutros pontos; se algum import ficar órfão pela remoção do bloco inline, removê-lo. (`ServiceApiSummary` continua importado como tipo — confirmar uso; `service.apis` passa-se ao componente.)

- [ ] **Step 4: Adicionar chaves i18n nos 4 locales**

Em cada `locales/{en,es,pt-BR,pt-PT}.json`, dentro de `serviceDetail`, adicionar:
- `"tabInterfacesApis"`: EN "Interfaces & APIs" · ES "Interfaces y APIs" · pt-BR "Interfaces e APIs" · pt-PT "Interfaces e APIs"
- `"publishedApis"`: EN "Published APIs" · ES "APIs publicadas" · pt-BR "APIs publicadas" · pt-PT "APIs publicadas"

- [ ] **Step 5: Correr o novo teste + os testes existentes de ServiceDetailPage**

Run: `cd src/frontend && npx vitest run src/__tests__/catalog/ServiceDetailPage`
Expected: PASS em todos (o novo + os existentes; nenhum existente dependia da aba `apis` — confirmar).

- [ ] **Step 6: Build + lint**

Run: `cd src/frontend && npm run build && npm run lint`
Expected: 0 erros; sem imports órfãos.

- [ ] **Step 7: Commit**

```bash
git add src/frontend/src/features/catalog/pages/ServiceDetailPage.tsx src/frontend/src/locales
git add src/frontend/src/__tests__/catalog/ServiceDetailPage.interfacesTab.test.tsx
git commit -m "feat(catalog): consolida abas APIs e Interfaces numa só superfície"
```

---

### Task 7: Gates finais + verificação de stub

**Files:** nenhum (verificação).

- [ ] **Step 1: Suíte completa**

Run: `cd src/frontend && npm run test -- --run`
Expected: toda a suíte passa.

- [ ] **Step 2: Build de produção**

Run: `cd src/frontend && npm run build`
Expected: sucesso.

- [ ] **Step 3: Verificação manual no stub (controlador, não subagente)**

`npm run stub` e verificar: (a) Contract Catalog sem botão de criação; (b) `/contracts/new` e `/contracts/studio/new` redirecionam para `/contracts`; (c) tela de serviço mostra uma só aba "Interfaces & APIs" com as duas secções e badge "API"; (d) 0 erros de consola. Esta verificação é feita pelo controlador após o review final.
