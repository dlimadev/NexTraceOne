import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ServiceDependencyTab } from '../../features/catalog/components/ServiceDependencyTab';

vi.mock('../../api/client', () => ({
  default: {
    get: vi.fn().mockResolvedValue({
      data: {
        serviceId: 'service-001',
        healthScore: 85,
        lastScanAt: '2026-01-15T10:00:00Z',
        totalDeps: 42,
        directDeps: 18,
        transitiveDeps: 24,
        criticalVulnCount: 0,
        highVulnCount: 1,
        mediumVulnCount: 2,
        lowVulnCount: 3,
        outdatedCount: 4,
        deprecatedCount: 0,
        licenseRiskCounts: {},
      },
    }),
    post: vi.fn().mockResolvedValue({
      data: {
        profileId: 'profile-001',
        healthScore: 85,
        totalDependencies: 42,
        directDependencies: 18,
        vulnerabilityCount: 3,
      },
    }),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
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

const renderWithProviders = (ui: React.ReactElement) => {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>{ui}</MemoryRouter>
    </QueryClientProvider>
  );
};

describe('ServiceDependencyTab', () => {
  it('renders the service health section', () => {
    renderWithProviders(<ServiceDependencyTab serviceId="service-001" />);
    expect(screen.getByText('dependencyDashboard.serviceHealth')).toBeDefined();
  });

  it('renders the scanner section', () => {
    renderWithProviders(<ServiceDependencyTab serviceId="service-001" />);
    expect(screen.getByText('dependencyDashboard.scanner')).toBeDefined();
  });

  it('does not render the portfolio vulnerable-services list', () => {
    renderWithProviders(<ServiceDependencyTab serviceId="service-001" />);
    expect(screen.queryByText('dependencyDashboard.vulnerableServices')).toBeNull();
  });
});
