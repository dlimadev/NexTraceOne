import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { GovernancePackDetailPage } from '../../features/governance/pages/GovernancePackDetailPage';

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

const mockPackDetail = {
  pack: {
    packId: 'pack-1',
    name: 'api-security-standards',
    displayName: 'API Security Standards',
    description: 'Comprehensive security standards for APIs',
    category: 'Contracts' as const,
    status: 'Published' as const,
    currentVersion: '2.0',
    ruleCount: 15,
    scopeCount: 8,
    createdAt: '2026-01-15T00:00:00Z',
    updatedAt: '2026-03-01T00:00:00Z',
    rules: [
      { ruleId: 'r1', ruleName: 'HTTPS Required', description: 'All endpoints must use HTTPS', defaultEnforcementMode: 'HardEnforce' as const, isRequired: true },
    ],
    scopes: [
      { scopeId: 's1', scopeType: 'Service', scopeName: 'Order Service', scopeValue: 'svc-order-api' },
    ],
    recentVersions: [],
  },
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/governance/packs/pack-1']}>
        <Routes>
          <Route path="/governance/packs/:packId" element={<GovernancePackDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('GovernancePackDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(organizationGovernanceApi.getGovernancePack).mockResolvedValue(mockPackDetail);
  });

  it('calls getGovernancePack with correct packId', async () => {
    renderPage();
    await waitFor(() => expect(organizationGovernanceApi.getGovernancePack).toHaveBeenCalledWith('pack-1'));
  });

  it('renders pack name from API response', async () => {
    renderPage();
    await waitFor(() => expect(screen.getByText('API Security Standards')).toBeInTheDocument());
  });

  it('shows loading state while fetching', () => {
    vi.mocked(organizationGovernanceApi.getGovernancePack).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('API Security Standards')).not.toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(organizationGovernanceApi.getGovernancePack).mockRejectedValue(new Error('Not found'));
    renderPage();
    await waitFor(() => expect(screen.queryByText('API Security Standards')).not.toBeInTheDocument());
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getByText('API Security Standards'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });
});
