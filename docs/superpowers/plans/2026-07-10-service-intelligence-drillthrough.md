# Service Intelligence Drill-through Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Inline execution (não subagent).

**Goal:** Ligar os dashboards agregados da inteligência de serviço ao detalhe de cada serviço (loop bidireccional), tendo `/services/:id` como hub.

**Architecture:** Três fatias sobre páginas React existentes: (F1) o scorecard passa a aceitar `?serviceName=` e o detalhe do serviço liga-lhe; (F2) as linhas de maturity/auditoria ligam a `/services/:serviceId`; (F3) itens de discovery já associados ligam ao catálogo e o modal cru migra para o DS `Modal`. Padrão de pré-carregamento por query-param replica a P2 F2 (`ContractHealthTimelinePage` com `?apiAssetId=`).

**Tech Stack:** React 19, TypeScript 5.9, react-router-dom 7 (`useSearchParams`, `Link`), TanStack Query 5, Vitest + Testing Library, i18next (4 locales).

## Global Constraints

- Idioma de UI: **chaves i18n**, nunca strings hardcoded. Usar `t('key', 'English fallback')`.
- Novas chaves adicionadas aos 4 locales (`en`, `es`, `pt-BR`, `pt-PT`) via script de deep-merge; `npm run validate:i18n` **tem de passar**.
- Honest-null: nunca ligar quando o identificador está ausente. `ServiceScorecardResponse` **não tem `serviceId`** → não fabricar back-link do scorecard para `/services/:id`.
- Comandos npm a partir de `src/frontend`: `npm run test`, `npm run build`, `npm run validate:i18n`. Bash tool: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend` antes.
- Testes centralizados em `src/frontend/src/__tests__/**`.
- Rota do scorecard: `/services/scorecards`. Rota do detalhe: `/services/:serviceId`. Rota do detalhe usa `useParams<{ serviceId?: string }>()`.
- Tipos-chave: `ServiceScorecardResponse.serviceName`; `ServiceMaturityItemDto.serviceId`; `AuditFindingDto.serviceId`; `DiscoveredServiceItem.matchedServiceAssetId: string | null`.
- Cada commit termina com `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

---

### Task 1: F1 — Scorecard on-ramp (`?serviceName=` preload + link do detalhe)

**Files:**
- Modify: `src/frontend/src/features/catalog/pages/ServiceScorecardPage.tsx` (imports + estado inicial a partir de `useSearchParams`)
- Modify: `src/frontend/src/features/catalog/pages/ServiceDetailPage.tsx:1059-1067` (link "Ver scorecard" no separador overview)
- Modify: `src/frontend/src/locales/{en,es,pt-BR,pt-PT}.json` (chave `serviceDetail.viewScorecard`)
- Test: `src/frontend/src/__tests__/catalog/ServiceScorecardPage.preload.test.tsx` (novo)
- Test: `src/frontend/src/__tests__/catalog/ServiceDetailPage.scorecardLink.test.tsx` (novo)

**Interfaces:**
- Consumes: `sourceOfTruthApi.getServiceScorecard(serviceName: string, environment?: string)`; `service.name` no scope top-level de `ServiceDetailPage` (linha 1062 já o usa).
- Produces: URL de drill `/services/scorecards?serviceName=<encoded>`.

- [ ] **Step 1: Escrever o teste de preload (falha)**

Criar `src/frontend/src/__tests__/catalog/ServiceScorecardPage.preload.test.tsx`:

```tsx
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ServiceScorecardPage } from '../../features/catalog/pages/ServiceScorecardPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: unknown) => (typeof f === 'string' ? f : k) }) }));
vi.mock('../../contexts/EnvironmentContext', () => ({ useEnvironment: () => ({ activeEnvironmentId: 'env-1' }) }));

const dim = { score: 0.8, weight: 0.125, justification: 'ok' };
const dimensions = {
  ownership: dim, documentation: dim, contracts: dim, slos: dim,
  observability: dim, changeGovernance: dim, runbooks: dim, security: dim,
};
const getServiceScorecard = vi.fn().mockResolvedValue({
  serviceName: 'orders-api', teamName: null, domain: null, overallScore: 0.8,
  maturityLevel: 'Managed', dimensions, computedAt: '2026-01-01T00:00:00Z',
});
vi.mock('../../features/catalog/api/sourceOfTruth', () => ({
  sourceOfTruthApi: { getServiceScorecard: (...a: unknown[]) => getServiceScorecard(...a) },
}));

function renderAt(path: string) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={[path]}>
        <ServiceScorecardPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ServiceScorecardPage preload', () => {
  beforeEach(() => getServiceScorecard.mockClear());

  it('auto-computa o scorecard a partir de ?serviceName= sem digitação manual', async () => {
    renderAt('/services/scorecards?serviceName=orders-api');
    await waitFor(() => expect(getServiceScorecard).toHaveBeenCalledWith('orders-api', 'Production'));
  });

  it('não consulta quando não há ?serviceName=', () => {
    renderAt('/services/scorecards');
    expect(getServiceScorecard).not.toHaveBeenCalled();
  });
});
```

- [ ] **Step 2: Correr o teste — falha**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run test -- ServiceScorecardPage.preload`
Expected: FAIL (query não chamada — estado inicial vazio).

- [ ] **Step 3: Implementar o preload em `ServiceScorecardPage.tsx`**

Alterar o import da linha 1:
```tsx
import { useState } from 'react';
```
para:
```tsx
import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
```

Dentro de `export function ServiceScorecardPage()` (linhas 153-158), substituir:
```tsx
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [serviceName, setServiceName] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [environment, setEnvironment] = useState('Production');
```
por:
```tsx
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [searchParams] = useSearchParams();
  const preloadName = searchParams.get('serviceName')?.trim() ?? '';
  const [serviceName, setServiceName] = useState(preloadName);
  const [searchInput, setSearchInput] = useState(preloadName);
  const [environment, setEnvironment] = useState('Production');
```

- [ ] **Step 4: Correr o teste — passa**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run test -- ServiceScorecardPage.preload`
Expected: PASS (2 testes).

- [ ] **Step 5: Escrever o teste do link no detalhe (falha)**

Criar `src/frontend/src/__tests__/catalog/ServiceDetailPage.scorecardLink.test.tsx` (reaproveita o scaffold de `ServiceDetailPage.setup.test.tsx`):

```tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
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

describe('ServiceDetailPage scorecard link', () => {
  it('liga ao scorecard pré-carregado pelo nome do serviço (raw name)', async () => {
    renderAt('svc-1');
    const link = await waitFor(() => screen.getByRole('link', { name: 'serviceDetail.viewScorecard' }));
    expect(link).toHaveAttribute('href', '/services/scorecards?serviceName=orders-api');
  });
});
```

- [ ] **Step 6: Correr o teste — falha**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run test -- ServiceDetailPage.scorecardLink`
Expected: FAIL (link não existe).

- [ ] **Step 7: Adicionar o link "Ver scorecard" em `ServiceDetailPage.tsx`**

No separador overview (linhas 1059-1068), substituir o bloco:
```tsx
            <CardBody>
              <p className="text-xs text-muted mb-3">{t('catalog.detail.recentChangesDescription')}</p>
              <Link
                to={`/changes?serviceName=${encodeURIComponent(service.name)}`}
                className="inline-flex items-center gap-1.5 text-xs text-accent hover:underline"
              >
                <ExternalLink size={12} />
                {t('catalog.detail.viewChange')}
              </Link>
            </CardBody>
```
por:
```tsx
            <CardBody>
              <p className="text-xs text-muted mb-3">{t('catalog.detail.recentChangesDescription')}</p>
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
            </CardBody>
```

- [ ] **Step 8: Correr o teste — passa**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run test -- ServiceDetailPage.scorecardLink`
Expected: PASS.

- [ ] **Step 9: Adicionar chave i18n `serviceDetail.viewScorecard` aos 4 locales**

Criar e correr um script Node (scratchpad) que faz deep-merge da chave em cada locale:
```js
import { readFileSync, writeFileSync } from 'node:fs';
const dir = 'C:/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend/src/locales';
const byLoc = {
  en: 'View scorecard', es: 'Ver scorecard', 'pt-BR': 'Ver scorecard', 'pt-PT': 'Ver scorecard',
};
function deepMerge(t, p) { for (const [k, v] of Object.entries(p)) { t[k] = (v && typeof v === 'object' && !Array.isArray(v)) ? deepMerge(t[k] && typeof t[k] === 'object' ? t[k] : {}, v) : v; } return t; }
for (const [loc, v] of Object.entries(byLoc)) {
  const path = `${dir}/${loc}.json`;
  const json = JSON.parse(readFileSync(path, 'utf8'));
  deepMerge(json, { serviceDetail: { viewScorecard: v } });
  writeFileSync(path, JSON.stringify(json, null, 2) + '\n', 'utf8');
}
```
Run: `node <script>.mjs` depois `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run validate:i18n`
Expected: validate:i18n PASS.

- [ ] **Step 10: Build + commit**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run build`
Expected: build sem erros.
```bash
git add src/frontend/src/features/catalog/pages/ServiceScorecardPage.tsx src/frontend/src/features/catalog/pages/ServiceDetailPage.tsx src/frontend/src/locales/*.json "src/frontend/src/__tests__/catalog/ServiceScorecardPage.preload.test.tsx" "src/frontend/src/__tests__/catalog/ServiceDetailPage.scorecardLink.test.tsx"
git commit -m "feat(catalog): scorecard on-ramp por serviço (P3 F1)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: F2 — Maturity & auditoria → detalhe do serviço

**Files:**
- Modify: `src/frontend/src/features/catalog/pages/ServiceMaturityPage.tsx` (import `Link`; link "Abrir serviço" na linha de maturity ~202-243 e no finding de auditoria ~293-311)
- Modify: `src/frontend/src/locales/{en,es,pt-BR,pt-PT}.json` (chave `serviceMaturity.openService`)
- Test: `src/frontend/src/__tests__/catalog/ServiceMaturityPage.drill.test.tsx` (novo)

**Interfaces:**
- Consumes: `serviceCatalogApi.getMaturityDashboard({ teamName, domain })` → `{ summary, services: ServiceMaturityItemDto[] }` (item tem `serviceId`, `serviceName`, `displayName`, `level`, `overallScore`, flags `has*`); `serviceCatalogApi.getOwnershipAudit(...)` → `{ summary, findings: AuditFindingDto[] }` (finding tem `serviceId`, `serviceName`, `displayName`, `severity`, `findings: string[]`).
- Produces: URL de drill `/services/${serviceId}`.

- [ ] **Step 1: Escrever o teste de drill (falha)**

Criar `src/frontend/src/__tests__/catalog/ServiceMaturityPage.drill.test.tsx`:

```tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import ServiceMaturityPage from '../../features/catalog/pages/ServiceMaturityPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: unknown) => (typeof f === 'string' ? f : k) }) }));
vi.mock('../../contexts/EnvironmentContext', () => ({ useEnvironment: () => ({ activeEnvironmentId: 'env-1' }) }));

const maturity = {
  summary: { totalServices: 1, averageScore: 0.8, withoutOwnership: 0, withoutContracts: 0, withoutDocumentation: 0, withoutRunbooks: 0, initial: 0, developing: 0, defined: 0, managed: 1, optimizing: 0 },
  services: [{ serviceId: 'svc-9', serviceName: 'orders-api', displayName: 'Orders API', teamName: 'Orders', domain: 'Commerce', level: 'Managed', overallScore: 0.8, hasOwnership: true, hasContracts: true, hasDocumentation: true, hasRepository: true, hasMonitoring: true, hasRunbook: true }],
};
const audit = {
  summary: { totalServicesAudited: 1, healthyServices: 0, servicesWithIssues: 1, criticalFindings: 1, withoutTeam: 0, apisWithoutContracts: 1 },
  findings: [{ serviceId: 'svc-9', serviceName: 'orders-api', displayName: 'Orders API', teamName: 'Orders', domain: 'Commerce', severity: 'high', findings: ['noContracts:1'] }],
};
const getMaturityDashboard = vi.fn().mockResolvedValue(maturity);
const getOwnershipAudit = vi.fn().mockResolvedValue(audit);
vi.mock('../../features/catalog/api/serviceCatalog', () => ({
  serviceCatalogApi: {
    getMaturityDashboard: (...a: unknown[]) => getMaturityDashboard(...a),
    getOwnershipAudit: (...a: unknown[]) => getOwnershipAudit(...a),
  },
}));

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={qc}>
      <MemoryRouter><ServiceMaturityPage /></MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ServiceMaturityPage drill-through', () => {
  it('linha de maturity liga ao detalhe do serviço', async () => {
    renderPage();
    const link = await waitFor(() => screen.getByRole('link', { name: 'serviceMaturity.openService' }));
    expect(link).toHaveAttribute('href', '/services/svc-9');
  });

  it('abrir-serviço não alterna o expand/collapse da linha', async () => {
    renderPage();
    const link = await waitFor(() => screen.getByRole('link', { name: 'serviceMaturity.openService' }));
    // dimensão só aparece após expand; clicar no link não a deve mostrar
    fireEvent.click(link);
    expect(screen.queryByText('serviceMaturity.dim.ownership')).not.toBeInTheDocument();
  });

  it('finding de auditoria liga ao detalhe do serviço', async () => {
    renderPage();
    fireEvent.click(await waitFor(() => screen.getByText('serviceMaturity.tabs.audit')));
    const links = await waitFor(() => screen.getAllByRole('link', { name: 'serviceMaturity.openService' }));
    expect(links[0]).toHaveAttribute('href', '/services/svc-9');
  });
});
```

> Nota: o label da tab audit vem de `tabs` (`labelKey`). Confirmar a chave real ao implementar; se diferir de `serviceMaturity.tabs.audit`, ajustar o `getByText` do 3º teste para a chave correcta (o `t` mockado devolve a própria chave).

- [ ] **Step 2: Correr o teste — falha**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run test -- ServiceMaturityPage.drill`
Expected: FAIL (nenhum link `serviceMaturity.openService`).

- [ ] **Step 3: Adicionar `Link` e o afford na linha de maturity**

No topo, alterar a linha 1:
```tsx
import { useState } from 'react';
```
para:
```tsx
import { useState } from 'react';
import { Link } from 'react-router-dom';
```

Na `MaturityTab`, o item de serviço (linhas 202-242) tem esta forma:
```tsx
              {services.map((svc: ServiceMaturityItemDto) => (
                <div key={svc.serviceId} className="py-3">
                  <Button
                    variant="ghost"
                    onClick={() => setExpandedId(expandedId === svc.serviceId ? null : svc.serviceId)}
                    className="w-full justify-start h-auto p-0 gap-3 font-normal"
                  >
                    ...
                  </Button>
                  {expandedId === svc.serviceId && ( ... )}
                </div>
              ))}
```
Envolver o `Button` e um novo `Link` num contentor flex, deixando o `Button` com `flex-1`. Substituir a abertura `<Button ... className="w-full justify-start h-auto p-0 gap-3 font-normal">` por um wrapper:
```tsx
                <div key={svc.serviceId} className="py-3">
                  <div className="flex items-center gap-2">
                    <Button
                      variant="ghost"
                      onClick={() => setExpandedId(expandedId === svc.serviceId ? null : svc.serviceId)}
                      className="flex-1 justify-start h-auto p-0 gap-3 font-normal"
                    >
```
e, imediatamente **após** o fecho `</Button>` desse item (antes do bloco `{expandedId === svc.serviceId && (`), adicionar o link e fechar o wrapper flex:
```tsx
                    </Button>
                    <Link
                      to={`/services/${svc.serviceId}`}
                      className="text-[11px] text-accent hover:underline whitespace-nowrap"
                    >
                      {t('serviceMaturity.openService', 'Open service')}
                    </Link>
                  </div>
                  {expandedId === svc.serviceId && (
```
(o `</div>` de fecho do `key={svc.serviceId}` mantém-se no fim do item.)

- [ ] **Step 4: Adicionar o link no finding de auditoria**

Na `AuditTab`, o cabeçalho do finding (linhas 295-302) é:
```tsx
                  <div className="flex items-center gap-2 mb-1">
                    <Badge variant={severityBadgeVariant(f.severity)}>
                      {f.severity.toUpperCase()}
                    </Badge>
                    <span className="text-sm font-medium text-heading">{f.displayName || f.serviceName}</span>
                    <span className="text-[11px] text-muted">{f.teamName || '—'}</span>
                    <span className="text-[11px] text-muted">{f.domain}</span>
                  </div>
```
Adicionar o link no fim dessa row:
```tsx
                  <div className="flex items-center gap-2 mb-1">
                    <Badge variant={severityBadgeVariant(f.severity)}>
                      {f.severity.toUpperCase()}
                    </Badge>
                    <span className="text-sm font-medium text-heading">{f.displayName || f.serviceName}</span>
                    <span className="text-[11px] text-muted">{f.teamName || '—'}</span>
                    <span className="text-[11px] text-muted">{f.domain}</span>
                    <Link
                      to={`/services/${f.serviceId}`}
                      className="ml-auto text-[11px] text-accent hover:underline whitespace-nowrap"
                    >
                      {t('serviceMaturity.openService', 'Open service')}
                    </Link>
                  </div>
```

- [ ] **Step 5: Correr o teste — passa**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run test -- ServiceMaturityPage.drill`
Expected: PASS (3 testes). Se o 3º falhar por causa do label da tab, corrigir o `getByText` para a `labelKey` real e correr de novo.

- [ ] **Step 6: Adicionar chave i18n `serviceMaturity.openService` aos 4 locales**

Script Node análogo ao da Task 1, com:
```js
const byLoc = { en: 'Open service', es: 'Abrir servicio', 'pt-BR': 'Abrir serviço', 'pt-PT': 'Abrir serviço' };
// deepMerge(json, { serviceMaturity: { openService: v } })
```
Run: `node <script>.mjs` depois `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run validate:i18n`
Expected: PASS.

- [ ] **Step 7: Build + commit**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run build`
Expected: sem erros.
```bash
git add src/frontend/src/features/catalog/pages/ServiceMaturityPage.tsx src/frontend/src/locales/*.json "src/frontend/src/__tests__/catalog/ServiceMaturityPage.drill.test.tsx"
git commit -m "feat(catalog): drill maturity/auditoria para o detalhe do serviço (P3 F2)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: F3 — Discovery → catálogo + migração do ActionModal para DS Modal

**Files:**
- Modify: `src/frontend/src/features/catalog/pages/ServiceDiscoveryPage.tsx` (import `Link` + `Modal`; link "Ver no catálogo" na célula de acções; migrar `ActionModal` cru → DS `Modal`)
- Modify: `src/frontend/src/locales/{en,es,pt-BR,pt-PT}.json` (chave `catalog.discovery.actions.viewInCatalog`)
- Test: `src/frontend/src/__tests__/catalog/ServiceDiscoveryPage.drill.test.tsx` (novo)

**Interfaces:**
- Consumes: `serviceCatalogApi.getDiscoveryDashboard()`; `serviceCatalogApi.listDiscoveredServices({ status?, environment?, search? })` → `{ items: DiscoveredServiceItem[], totalCount }` (item tem `id`, `serviceName`, `status`, `matchedServiceAssetId: string | null`, `traceCount`, `endpointCount`, `lastSeenAt`, `environment`).
- Produces: URL de drill `/services/${matchedServiceAssetId}`.
- DS `Modal` API: `<Modal open onClose title description? size? footer?>{children}</Modal>`. Import: `import { Modal } from '../../../shared/ui'`.

- [ ] **Step 1: Escrever o teste de drill (falha)**

Criar `src/frontend/src/__tests__/catalog/ServiceDiscoveryPage.drill.test.tsx`:

```tsx
import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import ServiceDiscoveryPage from '../../features/catalog/pages/ServiceDiscoveryPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: unknown) => (typeof f === 'string' ? f : k) }) }));
vi.mock('../../contexts/EnvironmentContext', () => ({ useEnvironment: () => ({ activeEnvironmentId: 'env-1' }) }));

const dashboard = { totalDiscovered: 2, pending: 1, matched: 1, registered: 0, ignored: 0, newThisWeek: 1, recentRuns: [] };
const items = [
  { id: 'd-1', serviceName: 'orders-api', serviceNamespace: '', environment: 'production', firstSeenAt: '2026-01-01T00:00:00Z', lastSeenAt: '2026-01-02T00:00:00Z', traceCount: 10, endpointCount: 2, status: 'Matched', matchedServiceAssetId: 'svc-42', ignoreReason: null },
  { id: 'd-2', serviceName: 'temp-worker', serviceNamespace: '', environment: 'production', firstSeenAt: '2026-01-01T00:00:00Z', lastSeenAt: '2026-01-02T00:00:00Z', traceCount: 5, endpointCount: 1, status: 'Pending', matchedServiceAssetId: null, ignoreReason: null },
];
vi.mock('../../features/catalog/api/serviceCatalog', () => ({
  serviceCatalogApi: {
    getDiscoveryDashboard: () => Promise.resolve(dashboard),
    listDiscoveredServices: () => Promise.resolve({ items, totalCount: 2 }),
    matchDiscoveredService: vi.fn(), registerFromDiscovery: vi.fn(),
    ignoreDiscoveredService: vi.fn(), runServiceDiscovery: vi.fn(),
  },
}));

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={qc}>
      <MemoryRouter><ServiceDiscoveryPage /></MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ServiceDiscoveryPage drill-through', () => {
  it('item Matched liga ao serviço no catálogo', async () => {
    renderPage();
    const link = await waitFor(() => screen.getByRole('link', { name: 'catalog.discovery.actions.viewInCatalog' }));
    expect(link).toHaveAttribute('href', '/services/svc-42');
  });

  it('item Pending (sem match) não mostra o link para o catálogo', async () => {
    renderPage();
    await waitFor(() => screen.getByText('temp-worker'));
    const links = screen.queryAllByRole('link', { name: 'catalog.discovery.actions.viewInCatalog' });
    expect(links).toHaveLength(1); // só o Matched
  });
});
```

- [ ] **Step 2: Correr o teste — falha**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run test -- ServiceDiscoveryPage.drill`
Expected: FAIL (nenhum link para o catálogo).

- [ ] **Step 3: Adicionar imports `Link` e `Modal`**

Alterar a linha 1 (`import { useState } from 'react';`) para adicionar o Link:
```tsx
import { useState } from 'react';
import { Link } from 'react-router-dom';
```
e a linha de imports do DS (linha 20) de:
```tsx
import { Button, IconButton, TextField, TextArea, Select, SearchInput } from '../../../shared/ui';
```
para:
```tsx
import { Button, IconButton, TextField, TextArea, Select, SearchInput, Modal } from '../../../shared/ui';
```

- [ ] **Step 4: Adicionar o link "Ver no catálogo" na célula de acções**

Na célula de acções (linhas 276-315), dentro de `<td className="px-4 py-3 text-right">`, adicionar — antes do bloco `{svc.status === 'Pending' && (` — o link condicional:
```tsx
                  <td className="px-4 py-3 text-right">
                    {svc.matchedServiceAssetId && (
                      <Link
                        to={`/services/${svc.matchedServiceAssetId}`}
                        className="text-xs text-accent hover:underline mr-2 whitespace-nowrap"
                      >
                        {t('catalog.discovery.actions.viewInCatalog', 'View in catalog')}
                      </Link>
                    )}
                    {svc.status === 'Pending' && (
```
(o resto da célula mantém-se.)

- [ ] **Step 5: Correr o teste de drill — passa**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run test -- ServiceDiscoveryPage.drill`
Expected: PASS (2 testes).

- [ ] **Step 6: Migrar o `ActionModal` cru para DS `Modal`**

Substituir o corpo do `ActionModal` (o `return (...)` das linhas 408-481) que hoje é:
```tsx
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50" onClick={onClose}>
      <div className="bg-panel border border-edge rounded-md shadow-lg w-full max-w-md p-6" onClick={(e) => e.stopPropagation()}>
        <h3 ...>{ titles }</h3>
        <p ...>{ service line }</p>
        { match / register / ignore fields }
        <div className="flex justify-end gap-2 mt-5">
          <Button variant="outline" onClick={onClose}>Cancel</Button>
          <Button variant="primary" loading={isLoading} onClick={confirm}>Confirm</Button>
        </div>
      </div>
    </div>
  );
```
por uma versão com DS `Modal` (título consoante `actionType`, footer com Cancel/Confirm, corpo com os campos):
```tsx
  const title =
    actionType === 'match' ? t('catalog.discovery.modal.matchTitle', 'Match to Existing Service')
    : actionType === 'register' ? t('catalog.discovery.modal.registerTitle', 'Register as New Service')
    : t('catalog.discovery.modal.ignoreTitle', 'Ignore Discovered Service');

  return (
    <Modal
      open
      onClose={onClose}
      title={title}
      size="md"
      footer={
        <>
          <Button variant="outline" onClick={onClose}>
            {t('common.cancel', 'Cancel')}
          </Button>
          <Button
            variant="primary"
            loading={isLoading}
            onClick={() => {
              if (actionType === 'match') onMatch(serviceAssetId);
              if (actionType === 'register') onRegister(domain, teamName);
              if (actionType === 'ignore') onIgnore(reason);
            }}
          >
            {isLoading ? t('common.loading', 'Loading...') : t('common.confirm', 'Confirm')}
          </Button>
        </>
      }
    >
      <p className="text-xs text-muted mb-4">
        {t('catalog.discovery.modal.service', 'Service')}: <strong>{service.serviceName}</strong> ({service.environment})
      </p>

      {actionType === 'match' && (
        <TextField
          size="sm"
          label={t('catalog.discovery.modal.serviceAssetId', 'Service Asset ID')}
          value={serviceAssetId}
          onChange={(e) => setServiceAssetId(e.target.value)}
          placeholder={t('catalog.discovery.modal.serviceAssetIdPlaceholder', 'Select or enter service ID...')}
        />
      )}

      {actionType === 'register' && (
        <div className="space-y-3">
          <TextField
            size="sm"
            label={t('catalog.discovery.modal.domain', 'Domain')}
            value={domain}
            onChange={(e) => setDomain(e.target.value)}
            placeholder={t('catalog.discovery.modal.domainPlaceholder', 'e.g. Payments')}
          />
          <TextField
            size="sm"
            label={t('catalog.discovery.modal.teamName', 'Team')}
            value={teamName}
            onChange={(e) => setTeamName(e.target.value)}
            placeholder={t('catalog.discovery.modal.teamNamePlaceholder', 'e.g. Platform Engineering')}
          />
        </div>
      )}

      {actionType === 'ignore' && (
        <TextArea
          label={t('catalog.discovery.modal.reason', 'Reason')}
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          placeholder={t('catalog.discovery.modal.reasonPlaceholder', 'e.g. Internal tooling, not a business service')}
          textareaClassName="resize-none h-20 min-h-0"
        />
      )}
    </Modal>
  );
```
O `ActionModal` continua a ser montado condicionalmente pelo pai (`{selectedService && actionType && (<ActionModal .../>)}`, linhas 350-366) — como o DS `Modal` devolve `null` quando fechado e aqui está sempre `open`, o gating do pai mantém o comportamento.

- [ ] **Step 7: Escrever/rodar teste de comportamento do modal migrado**

Acrescentar ao `ServiceDiscoveryPage.drill.test.tsx` um teste que abre a acção e confirma o conteúdo do modal:
```tsx
  it('abrir "Match" mostra o modal DS com o campo de Service Asset ID', async () => {
    const { fireEvent } = await import('@testing-library/react');
    renderPage();
    fireEvent.click(await waitFor(() => screen.getByRole('button', { name: 'catalog.discovery.actions.match' })));
    expect(await screen.findByText('catalog.discovery.modal.serviceAssetId')).toBeInTheDocument();
  });
```
> Nota jsdom/DS Modal: o polyfill `showModal` está em `setup.ts`. Se `getByRole('dialog')` for instável, assertar pelo **conteúdo** (label do campo), como acima e como feito na P2 F4.

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run test -- ServiceDiscoveryPage.drill`
Expected: PASS (3 testes).

- [ ] **Step 8: Adicionar chave i18n `catalog.discovery.actions.viewInCatalog` aos 4 locales**

Script Node análogo, com:
```js
const byLoc = { en: 'View in catalog', es: 'Ver en el catálogo', 'pt-BR': 'Ver no catálogo', 'pt-PT': 'Ver no catálogo' };
// deepMerge(json, { catalog: { discovery: { actions: { viewInCatalog: v } } } })
```
Run: `node <script>.mjs` depois `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run validate:i18n`
Expected: PASS.

- [ ] **Step 9: Build + commit**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run build`
Expected: sem erros (verifica que já não há JSX órfão do `<div>` cru substituído).
```bash
git add src/frontend/src/features/catalog/pages/ServiceDiscoveryPage.tsx src/frontend/src/locales/*.json "src/frontend/src/__tests__/catalog/ServiceDiscoveryPage.drill.test.tsx"
git commit -m "feat(catalog): discovery liga ao catálogo + modal de triagem no DS (P3 F3)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: Revisão final e merge

- [ ] **Step 1: Suite completa + build + i18n**

Run: `cd /c/Users/dlima/Documents/GitHub/NexTraceOne/src/frontend && npm run test -- --run && npm run build && npm run validate:i18n`
Expected: tudo PASS.

- [ ] **Step 2: Revisão opus de todo o branch**

Dispatch do code-reviewer (superpowers:requesting-code-review) sobre o diff `merge-base(main, HEAD)..HEAD`. Corrigir Critical/Important com um único fix.

- [ ] **Step 3: Merge direto em `main` + push** (sem PR, conforme instrução do owner)

```bash
git checkout main && git merge --no-ff redesign/betterstack-catalog-discovery && git push origin main
```
(ou fast-forward, conforme o estado do branch). Push só depois da revisão retornar 0 Critical/0 Important.

---

## Self-Review

**1. Spec coverage:**
- F1 (scorecard `?serviceName=` + link do detalhe + honest-null sem back-link) → Task 1 ✓
- F2 (maturity row + audit finding → `/services/:serviceId`, sem colidir com expand) → Task 2 ✓
- F3 (discovery matched → catálogo + ActionModal → DS Modal) → Task 3 ✓
- i18n nos 4 locales + validate:i18n → Steps 9/6/8 de cada task ✓
- Fora de escopo (DX/SecurityGate/Dependency/License/Pipeline) → nenhuma task lhes toca ✓

**2. Placeholder scan:** sem TBD/TODO; todo o código está presente. A única incerteza sinalizada (label da tab audit no teste F2) tem instrução explícita de fallback (usar a `labelKey` real). ✓

**3. Type consistency:** `serviceId` (maturity/audit), `matchedServiceAssetId` (discovery), `serviceName` (scorecard) usados de forma consistente com os tipos em `serviceCatalog.ts`/`sourceOfTruth.ts`. Rotas `/services/:serviceId` e `/services/scorecards` coerentes com `catalogRoutes.tsx`. ✓
