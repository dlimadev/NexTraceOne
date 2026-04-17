import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { LicenseCompliancePage } from '../../features/catalog/pages/LicenseCompliancePage';

vi.mock('../../api/client', () => ({
  default: {
    get: vi.fn().mockResolvedValue({
      data: {
        serviceId: 'service-001',
        conflicts: [],
        suggestions: [],
        checkedAt: '2026-01-15T10:00:00Z',
      },
    }),
    post: vi.fn().mockResolvedValue({
      data: {
        profileId: 'sbom-profile-001',
        serviceId: 'service-001',
        generatedAt: '2026-01-15T10:00:00Z',
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

describe('LicenseCompliancePage', () => {
  it('renders title from i18n', () => {
    renderWithProviders(<LicenseCompliancePage />);
    expect(screen.getByText('licenseCompliance.title')).toBeDefined();
  });

  it('shows serviceId input', () => {
    renderWithProviders(<LicenseCompliancePage />);
    expect(screen.getByText('licenseCompliance.serviceId')).toBeDefined();
  });

  it('shows check licenses button', () => {
    renderWithProviders(<LicenseCompliancePage />);
    expect(screen.getByText('licenseCompliance.checkLicenses')).toBeDefined();
  });

  it('shows generate SBOM button', () => {
    renderWithProviders(<LicenseCompliancePage />);
    expect(screen.getByText('licenseCompliance.generateSbom')).toBeDefined();
  });
});
