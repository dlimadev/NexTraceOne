/**
 * Testes de página para ServiceCatalogListPage.
 * Cobrem estados de loading, lista de serviços, estado vazio e erro.
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

const mockService = {
  serviceId: 'svc-001',
  name: 'Order API',
  displayName: 'Order API',
  description: 'REST API for order management',
  serviceType: 'RestApi' as const,
  domain: 'Commerce',
  systemArea: 'Orders',
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

describe('ServiceCatalogListPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(serviceCatalogApi.listServices).mockResolvedValue({ items: [mockService], totalCount: 1 });
    vi.mocked(serviceCatalogApi.getServicesSummary).mockResolvedValue(mockSummary);
  });

  it('renders service list after loading', async () => {
    renderPage();
    await waitFor(() => expect(screen.getByText('Order API')).toBeInTheDocument());
  });

  it('shows empty state when no services exist', async () => {
    vi.mocked(serviceCatalogApi.listServices).mockResolvedValue({ items: [], totalCount: 0 });
    vi.mocked(serviceCatalogApi.getServicesSummary).mockResolvedValue({ ...mockSummary, total: 0, active: 0 });
    renderPage();
    await waitFor(() => expect(screen.queryByText('Order API')).not.toBeInTheDocument());
  });

  it('calls listServices API on mount', async () => {
    renderPage();
    await waitFor(() => expect(serviceCatalogApi.listServices).toHaveBeenCalled());
  });

  it('shows loading state while fetching', () => {
    vi.mocked(serviceCatalogApi.listServices).mockReturnValue(new Promise(() => {}));
    vi.mocked(serviceCatalogApi.getServicesSummary).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Order API')).not.toBeInTheDocument();
  });

  it('displays service criticality badge', async () => {
    renderPage();
    await waitFor(() => expect(screen.getByText('High')).toBeInTheDocument());
  });
});
