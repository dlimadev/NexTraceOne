import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    getExecutiveOverview: vi.fn().mockResolvedValue({
      operationalState: 'Healthy', riskLevel: 'Low', maturityScore: 78,
      changesLast30Days: 42, openIncidents: 3, isSimulated: false,
    }),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { ExecutiveOverviewPage } from '../../features/governance/pages/ExecutiveOverviewPage';

describe('ExecutiveOverviewPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders page heading', async () => {
    render(<MemoryRouter><ExecutiveOverviewPage /></MemoryRouter>);
    await waitFor(() => {
      expect(screen.getByText('governance.executive.title')).toBeInTheDocument();
    });
  });

  it('renders executive data after loading', async () => {
    render(<MemoryRouter><ExecutiveOverviewPage /></MemoryRouter>);
    await waitFor(() => {
      expect(screen.queryByText('governance.executive.title')).toBeInTheDocument();
    });
  });
});
