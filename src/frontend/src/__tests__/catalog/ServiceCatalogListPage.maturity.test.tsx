/**
 * Testes da coluna "Maturidade" na ServiceCatalogListPage.
 * Verifica badge de nível, fallback "—" e deep-link ao dashboard.
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
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

/** Fixture com todos os campos obrigatórios de ServiceMaturityItemDto. */
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

const mockServiceSvc1 = {
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

const mockServiceSvc2 = {
  serviceId: 'svc-2',
  name: 'Order API',
  displayName: 'Order API',
  description: 'REST API for orders',
  serviceType: 'RestApi' as const,
  domain: 'Commerce',
  systemArea: 'Orders',
  teamName: 'Commerce Team',
  technicalOwner: 'Bob',
  criticality: 'Medium' as const,
  lifecycleStatus: 'Active' as const,
  exposureType: 'Internal' as const,
};

const mockSummary = {
  total: 2,
  active: 2,
  critical: 0,
  withIncidents: 0,
  withContractCoverage: 1,
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ServiceCatalogListPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ServiceCatalogListPage — coluna Maturidade', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(serviceCatalogApi.listServices).mockResolvedValue({
      items: [mockServiceSvc1, mockServiceSvc2],
      totalCount: 2,
    });
    vi.mocked(serviceCatalogApi.getServicesSummary).mockResolvedValue(mockSummary);
    vi.mocked(serviceCatalogApi.getMaturityDashboard).mockResolvedValue({
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
    });
  });

  it('exibe badge de maturidade para svc-1 (Managed)', async () => {
    renderPage();
    // Badge de nível "Managed" deve aparecer após carregar
    expect(await screen.findByText('Managed')).toBeInTheDocument();
  });

  it('exibe "—" para svc-2 sem dados de maturidade', async () => {
    renderPage();
    await waitFor(() => expect(screen.getByText('Order API')).toBeInTheDocument());
    // Traço para svc-2 que não tem entrada no dashboard de maturidade
    expect(screen.getByText('—')).toBeInTheDocument();
  });

  it('renderiza deep-link ao dashboard de maturidade', async () => {
    renderPage();
    await waitFor(() => expect(screen.getByText('Payment API')).toBeInTheDocument());
    const link = screen.getByRole('link', { name: /maturity dashboard|painel de maturidade|panel de madurez/i });
    expect(link).toHaveAttribute('href', '/services/maturity');
  });
});
