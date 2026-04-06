import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { DependencyDashboardPage } from '../../features/catalog/pages/DependencyDashboardPage';

vi.mock('../../api/client', () => ({
  default: {
    get: vi.fn().mockResolvedValue({ data: [] }),
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

const renderWithProviders = (ui: React.ReactElement) => {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>{ui}</MemoryRouter>
    </QueryClientProvider>
  );
};

describe('DependencyDashboardPage', () => {
  it('renders title from i18n', () => {
    renderWithProviders(<DependencyDashboardPage />);
    expect(screen.getByText('dependencyDashboard.title')).toBeDefined();
  });

  it('shows scanner form section', () => {
    renderWithProviders(<DependencyDashboardPage />);
    expect(screen.getByText('dependencyDashboard.scanner')).toBeDefined();
    expect(screen.getByText('dependencyDashboard.projectFileContent')).toBeDefined();
    expect(screen.getByText('dependencyDashboard.scanButton')).toBeDefined();
  });

  it('shows vulnerable services section', () => {
    renderWithProviders(<DependencyDashboardPage />);
    expect(screen.getByText('dependencyDashboard.vulnerableServices')).toBeDefined();
    expect(screen.getByText('dependencyDashboard.loadVulnerable')).toBeDefined();
  });

  it('shows health score section', () => {
    renderWithProviders(<DependencyDashboardPage />);
    expect(screen.getByText('dependencyDashboard.serviceHealth')).toBeDefined();
    expect(screen.getAllByText('dependencyDashboard.serviceId').length).toBeGreaterThan(0);
  });
});
