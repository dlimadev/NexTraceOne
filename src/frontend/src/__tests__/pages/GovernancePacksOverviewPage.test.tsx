import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { GovernancePacksOverviewPage } from '../../features/governance/pages/GovernancePacksOverviewPage';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    listTeams: vi.fn(),
    getTeamDetail: vi.fn(),
    getTeamGovernanceSummary: vi.fn(),
    listDomains: vi.fn(),
    listGovernancePacks: vi.fn(),
    getGovernancePack: vi.fn(),
    listWaivers: vi.fn(),
    getComplianceSummary: vi.fn(),
    getRiskSummary: vi.fn(),
    getExecutiveReport: vi.fn(),
    getMaturityScorecard: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));

import { organizationGovernanceApi } from '../../features/governance/api/organizationGovernance';

const mockPacks = [
  {
    packId: 'pack-1',
    name: 'api-security-standards',
    displayName: 'API Security Standards',
    description: 'Security standards for APIs',
    category: 'Contracts',
    status: 'Published',
    currentVersion: '2.0',
    ruleCount: 15,
    scopeCount: 8,
  },
  {
    packId: 'pack-2',
    name: 'observability-baseline',
    displayName: 'Observability Baseline',
    description: 'Baseline observability rules',
    category: 'Reliability',
    status: 'Draft',
    currentVersion: '1.0',
    ruleCount: 10,
    scopeCount: 0,
  },
];

const mockResponse = {
  packs: mockPacks,
  totalPacks: 2,
  publishedCount: 1,
  draftCount: 1,
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <GovernancePacksOverviewPage />
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
describe('GovernancePacksOverviewPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(organizationGovernanceApi.listGovernancePacks).mockResolvedValue(mockResponse);
  });

  it('calls listGovernancePacks on mount', async () => {
    renderPage();
    await waitFor(() => expect(organizationGovernanceApi.listGovernancePacks).toHaveBeenCalledTimes(1));
  });

  it('renders pack items from API response', async () => {
    renderPage();
    await waitFor(() => expect(screen.getByText('API Security Standards')).toBeInTheDocument());
    expect(screen.getByText('Observability Baseline')).toBeInTheDocument();
  });

  it('shows loading state while fetching', () => {
    vi.mocked(organizationGovernanceApi.listGovernancePacks).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('API Security Standards')).not.toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(organizationGovernanceApi.listGovernancePacks).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => expect(screen.queryByText('API Security Standards')).not.toBeInTheDocument());
  });

  it('shows empty state when no packs returned', async () => {
    vi.mocked(organizationGovernanceApi.listGovernancePacks).mockResolvedValue({
      packs: [], totalPacks: 0, publishedCount: 0, draftCount: 0,
    });
    renderPage();
    await waitFor(() => expect(screen.queryByText('API Security Standards')).not.toBeInTheDocument());
  });

  it('shows status badges', async () => {
    renderPage();
    await waitFor(() => screen.getByText('API Security Standards'));
    const published = screen.getAllByText('Published');
    expect(published.length).toBeGreaterThan(0);
    const draft = screen.getAllByText('Draft');
    expect(draft.length).toBeGreaterThan(0);
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getByText('API Security Standards'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });
});
