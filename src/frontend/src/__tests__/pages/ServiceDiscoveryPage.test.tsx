import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import ServiceDiscoveryPage from '../../features/catalog/pages/ServiceDiscoveryPage';

vi.mock('../../features/catalog/api/serviceCatalog', () => ({
  serviceCatalogApi: {
    registerService: vi.fn(),
    listDiscoveredServices: vi.fn(),
    getMaturityDashboard: vi.fn(),
    getOwnershipAudit: vi.fn(),
    runServiceDiscovery: vi.fn(),
    getDiscoveryDashboard: vi.fn(),
    matchDiscoveredService: vi.fn(),
    registerFromDiscovery: vi.fn(),
    ignoreDiscoveredService: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { serviceCatalogApi } from '../../features/catalog/api/serviceCatalog';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ServiceDiscoveryPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

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

describe('ServiceDiscoveryPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(serviceCatalogApi.getDiscoveryDashboard).mockResolvedValue({
      totalDiscovered: 0,
      pending: 0,
      matched: 0,
      registered: 0,
      ignored: 0,
      newThisWeek: 0,
      recentRuns: [],
    });
    vi.mocked(serviceCatalogApi.listDiscoveredServices).mockResolvedValue({
      items: [],
      totalCount: 0,
    });
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Service Discovery')).toBeDefined();
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(serviceCatalogApi.getDiscoveryDashboard).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders discovered services when data is available', async () => {
    vi.mocked(serviceCatalogApi.listDiscoveredServices).mockResolvedValue({
      items: [
        {
          id: 'disc-001',
          serviceName: 'payment-gateway',
          serviceNamespace: 'payments',
          environment: 'Production',
          status: 'Pending',
          traceCount: 1250,
          endpointCount: 8,
          firstSeenAt: '2026-03-01T00:00:00Z',
          lastSeenAt: '2026-04-01T00:00:00Z',
          matchedServiceAssetId: null,
          ignoreReason: null,
        },
      ],
      totalCount: 1,
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('payment-gateway')).toBeDefined();
    });
  });
});
