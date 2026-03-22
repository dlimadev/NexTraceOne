import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    listDomains: vi.fn().mockResolvedValue({
      domains: [
        { id: 'd1', name: 'Commerce', criticality: 'High', maturityLevel: 'Managed', teamCount: 3, serviceCount: 12 },
      ],
    }),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { DomainsOverviewPage } from '../../features/governance/pages/DomainsOverviewPage';

describe('DomainsOverviewPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders page heading', async () => {
    render(<MemoryRouter><DomainsOverviewPage /></MemoryRouter>);
    await waitFor(() => {
      expect(screen.getByText('governance.domains.title')).toBeInTheDocument();
    });
  });

  it('renders domain data after loading', async () => {
    render(<MemoryRouter><DomainsOverviewPage /></MemoryRouter>);
    await waitFor(() => {
      expect(screen.getByText('Commerce')).toBeInTheDocument();
    });
  });
});
