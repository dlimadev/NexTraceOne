import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { DashboardPage } from '../../features/shared/pages/DashboardPage';

vi.mock('../../features/catalog/api/serviceCatalog', () => ({
  serviceCatalogApi: {
    getGraph: vi.fn(),
  },
}));

import { serviceCatalogApi } from '../../features/catalog/api/serviceCatalog';

function renderDashboard() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <DashboardPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('DashboardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('exibe o título do dashboard', () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue({
      services: [],
      apis: [],
    });
    renderDashboard();
    expect(screen.getByRole('heading', { name: /dashboard/i })).toBeInTheDocument();
  });

  it('exibe os stat cards com labels corretos', () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue({
      services: [],
      apis: [],
    });
    renderDashboard();
    expect(screen.getByText('Active Services')).toBeInTheDocument();
    expect(screen.getAllByText('Registered APIs').length).toBeGreaterThan(0);
    expect(screen.getByText('Consumer Relations')).toBeInTheDocument();
    expect(screen.getByText('Total Contracts')).toBeInTheDocument();
    expect(screen.getByText('Pending Approvals')).toBeInTheDocument();
  });

  it('exibe serviços e APIs carregados do grafo', async () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue({
      services: [
        { serviceAssetId: 's1', name: 'payments-service', teamName: 'Payments', domain: 'Payments', serviceType: 'RestApi', criticality: 'High', lifecycleStatus: 'Active' },
        { serviceAssetId: 's2', name: 'auth-service', teamName: 'Identity', domain: 'Identity', serviceType: 'RestApi', criticality: 'Critical', lifecycleStatus: 'Active' },
      ],
      apis: [
        { apiAssetId: 'a1', name: 'Payments API', routePattern: '/api/payments', version: '1.0.0', visibility: 'Public', ownerServiceAssetId: 's1', consumers: [] },
      ],
    });
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText('payments-service')).toBeInTheDocument();
      expect(screen.getByText('auth-service')).toBeInTheDocument();
      expect(screen.getByText('Payments API')).toBeInTheDocument();
    });
  });

  it('exibe mensagem quando não há serviços registados', async () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue({
      services: [],
      apis: [],
    });
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText(/no services registered yet/i)).toBeInTheDocument();
    });
  });

  it('exibe mensagem quando não há APIs registadas', async () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue({
      services: [],
      apis: [],
    });
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText(/no apis registered yet/i)).toBeInTheDocument();
    });
  });
});
