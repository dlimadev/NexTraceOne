import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    listGovernanceWaivers: vi.fn(),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { organizationGovernanceApi } from '../../features/governance/api/organizationGovernance';
import { WaiversPage } from '../../features/governance/pages/WaiversPage';

const mockData = {
  waivers: [
    {
      waiverId: 'w-1',
      packName: 'API Governance Pack',
      ruleName: 'Contract Required',
      scope: 'Service:order-svc',
      justification: 'Legacy service without contract',
      requestedBy: 'alice@example.com',
      status: 'Pending',
      expiresAt: '2026-06-01T00:00:00Z',
    },
  ],
  totalWaivers: 5,
  pendingCount: 2,
  approvedCount: 3,
};

describe('WaiversPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(organizationGovernanceApi.listGovernanceWaivers).mockResolvedValue(mockData);
  });

  it('renders page heading', async () => {
    render(<MemoryRouter><WaiversPage /></MemoryRouter>);
    await waitFor(() => {
      expect(screen.getByText('governancePacks.waivers.title')).toBeInTheDocument();
    });
  });

  it('renders data after loading', async () => {
    render(<MemoryRouter><WaiversPage /></MemoryRouter>);
    await waitFor(() => {
      expect(screen.getByText('Contract Required')).toBeInTheDocument();
    });
  });

  it('shows loading state while fetching', () => {
    vi.mocked(organizationGovernanceApi.listGovernanceWaivers).mockReturnValue(new Promise(() => {}));
    render(<MemoryRouter><WaiversPage /></MemoryRouter>);
    expect(screen.queryByText('Contract Required')).not.toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    render(<MemoryRouter><WaiversPage /></MemoryRouter>);
    await waitFor(() => screen.getByText('Contract Required'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });

  it('calls listGovernanceWaivers on mount', async () => {
    render(<MemoryRouter><WaiversPage /></MemoryRouter>);
    await waitFor(() => expect(organizationGovernanceApi.listGovernanceWaivers).toHaveBeenCalledTimes(1));
  });
});
