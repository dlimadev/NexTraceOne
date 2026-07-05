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
    getNodeHealth: vi.fn(),
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

// Reshell "browse-first": o topo é um segmento Browse | Explorar. Browse é a
// superfície de descoberta (pesquisa + cartões de serviço); Explorar reagrupa as
// abas de análise (overview/graph/impact/temporal) sem as redesenhar.
describe('ServiceCatalogPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(serviceCatalogApi.getNodeHealth).mockResolvedValue({ nodes: [] } as never);
    vi.mocked(serviceCatalogApi.listSnapshots).mockResolvedValue({ items: [] } as never);
  });

  it('exibe o título da página', () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue(mockGraph);
    renderGraph();
    expect(screen.getByRole('heading', { name: /service catalog/i })).toBeInTheDocument();
  });

  it('exibe o segmento de topo Browse | Explorar', () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue(mockGraph);
    renderGraph();
    // O componente Tabs do DS usa role="tab" (WCAG).
    expect(screen.getByRole('tab', { name: /browse/i })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /explore/i })).toBeInTheDocument();
  });

  it('Browse (por defeito) exibe os serviços carregados como cartões', async () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue(mockGraph);
    renderGraph();
    await waitFor(() => {
      expect(screen.getByText('payments-service')).toBeInTheDocument();
      expect(screen.getByText('auth-service')).toBeInTheDocument();
    });
  });

  it('a vista "APIs" do Browse exibe as APIs', async () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue(mockGraph);
    renderGraph();
    // Alterna a facet bar do Browse para a vista de APIs.
    await userEvent.click(await screen.findByRole('tab', { name: /apis/i }));
    await waitFor(() => {
      expect(screen.getByText('Payments API')).toBeInTheDocument();
      expect(screen.getByText('/api/payments')).toBeInTheDocument();
    });
  });

  it('Explorar revela as sub-abas de análise (graph/impact/temporal)', async () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue(mockGraph);
    renderGraph();
    await userEvent.click(screen.getByRole('tab', { name: /explore/i }));
    await waitFor(() => {
      expect(screen.getByRole('tab', { name: /graph/i })).toBeInTheDocument();
      expect(screen.getByRole('tab', { name: /impact/i })).toBeInTheDocument();
      expect(screen.getByRole('tab', { name: /temporal/i })).toBeInTheDocument();
    });
  });

  it('navega para a aba Impact dentro de Explorar sem erro', async () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue(mockGraph);
    vi.mocked(serviceCatalogApi.getImpactPropagation).mockResolvedValue({ nodes: [], edges: [] } as never);
    renderGraph();
    await userEvent.click(screen.getByRole('tab', { name: /explore/i }));
    const impactBtn = await screen.findByRole('tab', { name: /impact/i });
    await userEvent.click(impactBtn);
    await waitFor(() => {
      expect(impactBtn).toBeInTheDocument();
    });
  });

  it('navega para a aba Temporal dentro de Explorar sem erro', async () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue(mockGraph);
    renderGraph();
    await userEvent.click(screen.getByRole('tab', { name: /explore/i }));
    const temporalBtn = await screen.findByRole('tab', { name: /temporal/i });
    await userEvent.click(temporalBtn);
    await waitFor(() => {
      expect(temporalBtn).toBeInTheDocument();
    });
  });

  it('exibe mensagem quando não há serviços registados', async () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue({ services: [], apis: [] });
    renderGraph();
    // Browse é o segmento por defeito; um grafo vazio mostra o EmptyState.
    await waitFor(() => {
      expect(screen.getByText(/no services registered/i)).toBeInTheDocument();
    });
  });
});
