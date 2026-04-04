import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { Routes, Route } from 'react-router-dom';
import { renderWithProviders } from '../test-utils';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    getDomainDetail: vi.fn(),
    getDomainGovernanceSummary: vi.fn(),
  },
}));

import { organizationGovernanceApi } from '../../features/governance/api/organizationGovernance';
import { DomainDetailPage } from '../../features/governance/pages/DomainDetailPage';

const mockDomain = {
  domainId: 'domain-1',
  displayName: 'Commerce Domain',
  description: 'Handles all commerce operations',
  criticality: 'High',
  maturityLevel: 'Managed',
  teamCount: 4,
  serviceCount: 12,
  activeIncidentCount: 1,
  reliabilityScore: 95,
  createdAt: '2024-01-01T00:00:00Z',
  capabilityClassification: 'Core',
  teams: [],
  services: [],
  crossDomainDependencies: [],
};

const mockGov = {
  overallMaturity: 'Managed',
  openRiskCount: 2,
  policyViolationCount: 1,
  ownershipCoverage: 0.9,
  contractCoverage: 0.85,
  documentationCoverage: 0.7,
  dimensions: [],
};

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route path="/governance/domains/:domainId" element={<DomainDetailPage />} />
    </Routes>,
    { routerProps: { initialEntries: ['/governance/domains/domain-1'] } },
  );
}

describe('DomainDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(organizationGovernanceApi.getDomainDetail).mockResolvedValue(mockDomain);
    vi.mocked(organizationGovernanceApi.getDomainGovernanceSummary).mockResolvedValue(mockGov);
  });

  it('renders domain name from API response', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Commerce Domain')).toBeInTheDocument();
    });
  });

  it('calls getDomainDetail with correct id', async () => {
    renderPage();
    await waitFor(() => expect(organizationGovernanceApi.getDomainDetail).toHaveBeenCalledWith('domain-1'));
  });

  it('shows loading state while fetching', () => {
    vi.mocked(organizationGovernanceApi.getDomainDetail).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Commerce Domain')).not.toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(organizationGovernanceApi.getDomainDetail).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => expect(screen.queryByText('Commerce Domain')).not.toBeInTheDocument());
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Commerce Domain'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });
});
