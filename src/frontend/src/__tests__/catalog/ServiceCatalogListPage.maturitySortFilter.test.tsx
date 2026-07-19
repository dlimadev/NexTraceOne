/**
 * Testes do filtro por maturidade e da coluna ordenável por maturidade
 * na ServiceCatalogListPage.
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ServiceCatalogListPage } from '../../features/catalog/pages/ServiceCatalogListPage';

vi.mock('../../features/catalog/api', () => ({
  serviceCatalogApi: {
    listServices: vi.fn(),
    getServicesSummary: vi.fn(),
    getMaturityDashboard: vi.fn(),
    registerService: vi.fn(),
    archiveService: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn().mockReturnValue({
    activeEnvironmentId: 'env-prod-001',
    activeEnvironment: { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [{ id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true }],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  }),
}));

import { serviceCatalogApi } from '../../features/catalog/api';
import type { ServiceMaturityItemDto } from '../../features/catalog/api/serviceCatalog';

/** Fixture de maturidade com todos os campos obrigatórios. */
const mockMaturityItem: ServiceMaturityItemDto = {
  serviceId: 'svc-1',
  serviceName: 'payment-api',
  displayName: 'Payment API',
  teamName: 'Commerce Team',
  domain: 'Commerce',
  criticality: 'High',
  lifecycleStatus: 'Active',
  level: 'Managed',
  overallScore: 0.82,
  hasOwnership: true,
  hasContracts: true,
  hasDocumentation: true,
  hasRunbook: true,
  hasMonitoring: true,
  hasRepository: true,
  apiCount: 2,
  contractCount: 3,
  linkCount: 4,
};

const mockService = {
  serviceId: 'svc-1',
  name: 'Payment API',
  displayName: 'Payment API',
  description: 'REST API for payments',
  serviceType: 'RestApi' as const,
  domain: 'Commerce',
  systemArea: 'Payments',
  teamName: 'Commerce Team',
  technicalOwner: 'Alice',
  criticality: 'High' as const,
  lifecycleStatus: 'Active' as const,
  exposureType: 'External' as const,
};

const mockSummary = {
  total: 1,
  active: 1,
  critical: 0,
  withIncidents: 0,
  withContractCoverage: 1,
};

const mockMaturityDash = {
  summary: {
    totalServices: 1,
    averageScore: 0.82,
    optimizing: 0,
    managed: 1,
    defined: 0,
    developing: 0,
    initial: 0,
    withoutOwnership: 0,
    withoutContracts: 0,
    withoutDocumentation: 0,
    withoutRunbooks: 0,
    withoutMonitoring: 0,
  },
  services: [mockMaturityItem],
  computedAt: '2026-07-17T00:00:00Z',
};

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        // Garante que TanStack Query executa queries mesmo em ambiente de teste (jsdom)
        networkMode: 'always',
        // Remove cache imediatamente ao mudar de key — evita stale hits entre cliques
        gcTime: 0,
      },
    },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ServiceCatalogListPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ServiceCatalogListPage — filtro e ordenação por maturidade', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(serviceCatalogApi.listServices).mockResolvedValue({
      items: [mockService],
      totalCount: 1,
    });
    vi.mocked(serviceCatalogApi.getServicesSummary).mockResolvedValue(mockSummary);
    vi.mocked(serviceCatalogApi.getMaturityDashboard).mockResolvedValue(mockMaturityDash);
  });

  it('selecionar nível no dropdown de maturidade chama listServices com maturityLevel', async () => {
    renderPage();
    // Aguarda a chamada inicial da query
    await waitFor(() => expect(serviceCatalogApi.listServices).toHaveBeenCalled());

    const maturitySelect = screen.getByLabelText('Maturity');
    fireEvent.change(maturitySelect, { target: { value: 'Developing' } });

    await waitFor(() => {
      expect(serviceCatalogApi.listServices).toHaveBeenCalledWith(
        expect.objectContaining({ maturityLevel: 'Developing' }),
      );
    });
  });

  it('clicar no cabeçalho Maturidade chama listServices com sortBy=maturity', async () => {
    renderPage();
    // Aguarda a tabela renderizar (dados carregados)
    await waitFor(() => expect(screen.getByText('Payment API')).toBeInTheDocument());

    const sortButton = screen.getByRole('button', { name: /sort by maturity/i });
    fireEvent.click(sortButton);

    await waitFor(() => {
      expect(serviceCatalogApi.listServices).toHaveBeenCalledWith(
        expect.objectContaining({ sortBy: 'maturity' }),
      );
    });
  });

  it('segundo clique no cabeçalho Maturidade inverte sortDescending para true', async () => {
    const user = userEvent.setup();
    renderPage();
    // Aguarda a tabela renderizar (dados carregados)
    await waitFor(() => expect(screen.getByText('Payment API')).toBeInTheDocument());

    // Primeiro clique: activa sort com desc=false
    await user.click(screen.getByRole('button', { name: /sort by maturity/i }));

    // Aguarda o re-render provocado pela mudança de queryKey (isLoading→false):
    // a tabela desmonta brevemente quando isLoading=true, depois remonta.
    // Re-obter o botão garante que clicamos no elemento DOM recém-montado.
    await waitFor(() => expect(screen.getByText('Payment API')).toBeInTheDocument());

    // Segundo clique: inverte desc para true
    await user.click(screen.getByRole('button', { name: /sort by maturity/i }));

    await waitFor(() => {
      expect(serviceCatalogApi.listServices).toHaveBeenCalledWith(
        expect.objectContaining({ sortBy: 'maturity', sortDescending: true }),
      );
    });
  });
});
