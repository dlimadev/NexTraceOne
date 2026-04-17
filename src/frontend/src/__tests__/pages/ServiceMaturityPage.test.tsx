import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ServiceMaturityPage } from '../../features/catalog/pages/ServiceMaturityPage';

vi.mock('../../features/catalog/api/serviceCatalog', () => ({
  serviceCatalogApi: {
    getMaturityDashboard: vi.fn(),
    getOwnershipAudit: vi.fn(),
    listServices: vi.fn(),
    listCategories: vi.fn(),
    listDiscoveredServices: vi.fn(),
    matchDiscoveredService: vi.fn(),
    ignoreDiscoveredService: vi.fn(),
    getServiceScorecard: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { serviceCatalogApi } from '../../features/catalog/api/serviceCatalog';

const mockMaturityData = {
  summary: {
    totalServices: 45,
    averageScore: 0.72,
    optimizing: 5,
    managed: 12,
    defined: 15,
    developing: 10,
    initial: 3,
    withoutOwnership: 2,
    withoutContracts: 8,
    withoutDocumentation: 12,
    withoutRunbooks: 18,
    withoutMonitoring: 6,
  },
  services: [
    {
      serviceId: 'svc-001',
      serviceName: 'order-api',
      displayName: 'Order API',
      teamName: 'Commerce Team',
      domain: 'Commerce',
      criticality: 'High',
      lifecycleStatus: 'Active',
      level: 'Defined',
      overallScore: 0.68,
      hasOwnership: true,
      hasContracts: true,
      hasDocumentation: false,
      hasRunbook: false,
      hasMonitoring: true,
      hasRepository: true,
      apiCount: 3,
      contractCount: 2,
      linkCount: 5,
    },
  ],
  computedAt: '2026-04-01T00:00:00Z',
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ServiceMaturityPage />
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

describe('ServiceMaturityPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(serviceCatalogApi.getMaturityDashboard).mockResolvedValue(mockMaturityData);
    vi.mocked(serviceCatalogApi.getOwnershipAudit).mockResolvedValue({
      summary: {
        totalServicesAudited: 45,
        servicesWithIssues: 12,
        healthyServices: 33,
        criticalFindings: 2,
        highFindings: 5,
        mediumFindings: 8,
        withoutTeam: 2,
        withoutTechnicalOwner: 4,
        withoutDocumentation: 12,
        withoutRunbook: 18,
        apisWithoutContracts: 8,
      },
      findings: [],
    });
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Service Maturity & Ownership Audit')).toBeDefined();
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(serviceCatalogApi.getMaturityDashboard).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders maturity data when available', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('45');
    });
  });
});
