import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
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
    const link = await waitFor(() => screen.getByRole('link', { name: 'View in catalog' }));
    expect(link).toHaveAttribute('href', '/services/svc-42');
  });

  it('item Pending (sem match) não mostra o link para o catálogo', async () => {
    renderPage();
    await waitFor(() => screen.getByText('temp-worker'));
    const links = screen.queryAllByRole('link', { name: 'View in catalog' });
    expect(links).toHaveLength(1); // só o Matched
  });

  it('abrir "Match" mostra o modal DS com o campo de Service Asset ID', async () => {
    renderPage();
    fireEvent.click(await waitFor(() => screen.getByRole('button', { name: 'Match' })));
    expect(await screen.findByText('Service Asset ID')).toBeInTheDocument();
  });
});
