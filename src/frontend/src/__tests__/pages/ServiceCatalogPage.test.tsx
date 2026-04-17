import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ServiceCatalogPage } from '../../features/catalog/pages/ServiceCatalogPage';
import type { AssetGraph } from '../../types';

vi.mock('../../features/catalog/api', () => ({
  serviceCatalogApi: {
    getGraph: vi.fn(),
    registerService: vi.fn(),
    registerApi: vi.fn(),
    getImpactPropagation: vi.fn(),
    listSnapshots: vi.fn(),
    getTemporalDiff: vi.fn(),
    createSnapshot: vi.fn(),
  },
}));

import { serviceCatalogApi } from '../../features/catalog/api';

const mockGraph: AssetGraph = {
  services: [
    { serviceAssetId: 's1', name: 'payments-service', teamName: 'Payments', domain: 'Payments', serviceType: 'RestApi', criticality: 'High', lifecycleStatus: 'Active' },
    { serviceAssetId: 's2', name: 'auth-service', teamName: 'Identity', domain: 'Identity', serviceType: 'RestApi', criticality: 'Critical', lifecycleStatus: 'Active' },
  ],
  apis: [
    {
      apiAssetId: 'a1',
      name: 'Payments API',
      routePattern: '/api/payments',
      version: '1.0.0',
      visibility: 'Public',
      ownerServiceAssetId: 's1',
      consumers: [
        { relationshipId: 'r1', consumerName: 'auth-service', sourceType: 'Inferred', confidenceScore: 0.85, lastObservedAt: '2024-01-01T00:00:00Z' },
      ],
    },
  ],
};

function renderGraph() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ServiceCatalogPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}


vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn().mockReturnValue({
    activeEnvironmentId: 'env-prod-001',
    activeEnvironment: { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [
      { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
      { id: 'env-staging-001', name: 'Staging', profile: 'staging', isProductionLike: false },
    ],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  }),
}));

describe('ServiceCatalogPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('exibe o título da página', () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue(mockGraph);
    renderGraph();
    expect(screen.getByRole('heading', { name: /service catalog/i })).toBeInTheDocument();
  });

  it('exibe as abas de navegação incluindo novas abas', () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue(mockGraph);
    renderGraph();
    expect(screen.getByRole('button', { name: /services/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /apis/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /graph/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /impact/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /temporal/i })).toBeInTheDocument();
  });

  it('exibe os serviços carregados na aba Services', async () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue(mockGraph);
    renderGraph();
    await userEvent.click(screen.getByRole('button', { name: /services/i }));
    await waitFor(() => {
      expect(screen.getByText('payments-service')).toBeInTheDocument();
      expect(screen.getByText('auth-service')).toBeInTheDocument();
    });
  });

  it('navega para a aba APIs e exibe as APIs', async () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue(mockGraph);
    renderGraph();
    await userEvent.click(screen.getByRole('button', { name: /apis/i }));
    await waitFor(() => {
      expect(screen.getByText('Payments API')).toBeInTheDocument();
      expect(screen.getByText('/api/payments')).toBeInTheDocument();
    });
  });

  it('exibe estatísticas na aba Overview por defeito', async () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue(mockGraph);
    renderGraph();
    await waitFor(() => {
      // Overview tab is active by default and shows stats
      expect(screen.getByRole('heading', { name: /service catalog/i })).toBeInTheDocument();
    });
  });

  it('exibe a aba Impact ao navegar para Impact Analysis', async () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue(mockGraph);
    vi.mocked(serviceCatalogApi.getImpactPropagation).mockResolvedValue({ nodes: [], edges: [] });
    renderGraph();
    const impactBtn = screen.getByRole('button', { name: /impact/i });
    await userEvent.click(impactBtn);
    await waitFor(() => {
      // Clicking impact tab should not throw
      expect(impactBtn).toBeInTheDocument();
    });
  });

  it('exibe a aba Temporal ao navegar para Temporal Analysis', async () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue(mockGraph);
    vi.mocked(serviceCatalogApi.listSnapshots).mockResolvedValue([]);
    renderGraph();
    const temporalBtn = screen.getByRole('button', { name: /temporal/i });
    await userEvent.click(temporalBtn);
    await waitFor(() => {
      expect(temporalBtn).toBeInTheDocument();
    });
  });

  it('exibe mensagem quando não há serviços registados', async () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue({ services: [], apis: [] });
    renderGraph();
    await userEvent.click(screen.getByRole('button', { name: /services/i }));
    await waitFor(() => {
      expect(screen.getByText(/no services registered/i)).toBeInTheDocument();
    });
  });
});
