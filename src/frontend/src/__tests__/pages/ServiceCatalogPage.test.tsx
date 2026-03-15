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

  it('exibe formulário de serviço ao clicar em Register Service', async () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue(mockGraph);
    renderGraph();
    const serviceBtn = screen.getAllByRole('button').find(
      (btn) => btn.textContent?.includes('Register Service')
    )!;
    await userEvent.click(serviceBtn);
    await waitFor(() => {
      // O formulário renderiza campos de input para nome e time
      expect(screen.getAllByRole('textbox').length).toBeGreaterThanOrEqual(2);
    });
  });

  it('exibe formulário de API ao clicar em Register API', async () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue(mockGraph);
    renderGraph();
    const apiBtn = screen.getAllByRole('button').find(
      (btn) => btn.textContent?.includes('Register API')
    )!;
    await userEvent.click(apiBtn);
    await waitFor(() => {
      expect(screen.getByText('Register API Asset')).toBeInTheDocument();
    });
  });

  it('chama registerService ao submeter o formulário de serviço', async () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue({ services: [], apis: [] });
    vi.mocked(serviceCatalogApi.registerService).mockResolvedValue({ id: 's-new' });
    renderGraph();
    const serviceBtn = screen.getAllByRole('button').find(
      (btn) => btn.textContent?.includes('Register Service')
    )!;
    await userEvent.click(serviceBtn);
    await userEvent.type(screen.getAllByRole('textbox')[0], 'new-service');
    await userEvent.type(screen.getAllByRole('textbox')[1], 'Platform');
    await userEvent.click(screen.getByRole('button', { name: /register$/i }));
    await waitFor(() => {
      expect(serviceCatalogApi.registerService).toHaveBeenCalled();
      const [firstArg] = vi.mocked(serviceCatalogApi.registerService).mock.calls[0];
      expect(firstArg).toMatchObject({ name: 'new-service', team: 'Platform' });
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
