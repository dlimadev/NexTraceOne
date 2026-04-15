import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    listDomains: vi.fn().mockResolvedValue({
      domains: [
        { domainId: 'd1', displayName: 'Commerce', criticality: 'High', maturityLevel: 'Managed', teamCount: 3, serviceCount: 12, contractCount: 5 },
      ],
    }),
  },
}));

import { DomainsOverviewPage } from '../../features/governance/pages/DomainsOverviewPage';

describe('DomainsOverviewPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders page heading', async () => {
    renderWithProviders(<DomainsOverviewPage />);
    await waitFor(() => {
      expect(screen.getByText('Business Domains')).toBeInTheDocument();
    });
  });

  it('renders domain data after loading', async () => {
    renderWithProviders(<DomainsOverviewPage />);
    await waitFor(() => {
      expect(screen.getByText('Commerce')).toBeInTheDocument();
    });
  });
});
