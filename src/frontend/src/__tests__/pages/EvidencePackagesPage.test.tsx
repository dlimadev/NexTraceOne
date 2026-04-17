import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/governance/api/evidence', () => ({
  evidenceApi: {
    listPackages: vi.fn(),
    getPackage: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { evidenceApi } from '../../features/governance/api/evidence';
import { EvidencePackagesPage } from '../../features/governance/pages/EvidencePackagesPage';

const mockData = {
  packages: [
    {
      packageId: 'pkg-1',
      name: 'Q1 2025 Compliance Pack',
      status: 'Sealed' as const,
      scope: 'Organization',
      createdBy: 'admin@example.com',
      createdAt: '2025-03-01T00:00:00Z',
      sealedAt: '2025-03-15T00:00:00Z',
      itemCount: 12,
    },
  ],
  totalCount: 5,
  sealedCount: 3,
  exportedCount: 1,
  draftCount: 1,
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter><EvidencePackagesPage /></MemoryRouter>
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
describe('EvidencePackagesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(evidenceApi.listPackages).mockResolvedValue(mockData);
  });

  it('renders page heading', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('governance.evidence.title')).toBeInTheDocument();
    });
  });

  it('renders data after loading', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Q1 2025 Compliance Pack')).toBeInTheDocument();
    });
  });

  it('shows loading state while fetching', () => {
    vi.mocked(evidenceApi.listPackages).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Q1 2025 Compliance Pack')).not.toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Q1 2025 Compliance Pack'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });

  it('calls evidenceApi.listPackages on mount', async () => {
    renderPage();
    await waitFor(() => expect(evidenceApi.listPackages).toHaveBeenCalled());
  });
});
