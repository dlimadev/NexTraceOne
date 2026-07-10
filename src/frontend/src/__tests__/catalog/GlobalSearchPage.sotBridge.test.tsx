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
    // espera os resultados renderizarem (a ponte do serviço aparece)
    await waitFor(() => {
      if (!document.querySelector('a[href="/source-of-truth/services/svc-1"]')) throw new Error('ainda não renderizado');
    });
    // só o serviço tem ponte; nenhum href SoT aponta para o id do contrato
    expect(document.querySelector('a[href="/source-of-truth/services/cv-9"]')).toBeNull();
    expect(document.querySelectorAll('a[href^="/source-of-truth/services/"]').length).toBe(1);
  });
});
