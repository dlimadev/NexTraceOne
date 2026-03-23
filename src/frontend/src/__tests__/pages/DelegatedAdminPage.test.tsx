import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/governance/api/organizationGovernance', () => ({
  organizationGovernanceApi: {
    listDelegations: vi.fn(),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { organizationGovernanceApi } from '../../features/governance/api/organizationGovernance';
import { DelegatedAdminPage } from '../../features/governance/pages/DelegatedAdminPage';

const mockData = {
  delegations: [
    {
      delegationId: 'del-1',
      granteeDisplayName: 'Alice Johnson',
      scope: 'TeamAdmin' as const,
      isActive: true,
      teamId: 'team-1',
      teamName: 'Order Squad',
      domainId: null,
      domainName: null,
      reason: 'Team lead delegation',
      grantedAt: '2025-01-15T10:00:00Z',
      expiresAt: null,
    },
  ],
  totalCount: 1,
  activeCount: 1,
  teamScopedCount: 1,
  domainScopedCount: 0,
};

describe('DelegatedAdminPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(organizationGovernanceApi.listDelegations).mockResolvedValue(mockData);
  });

  it('renders page heading', async () => {
    render(<MemoryRouter><DelegatedAdminPage /></MemoryRouter>);
    await waitFor(() => {
      expect(screen.getByText('organization.delegatedAdmin.title')).toBeInTheDocument();
    });
  });

  it('renders data after loading', async () => {
    render(<MemoryRouter><DelegatedAdminPage /></MemoryRouter>);
    await waitFor(() => {
      expect(screen.getByText('Alice Johnson')).toBeInTheDocument();
    });
  });

  it('shows loading state while fetching', () => {
    vi.mocked(organizationGovernanceApi.listDelegations).mockReturnValue(new Promise(() => {}));
    render(<MemoryRouter><DelegatedAdminPage /></MemoryRouter>);
    expect(screen.queryByText('Alice Johnson')).not.toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    render(<MemoryRouter><DelegatedAdminPage /></MemoryRouter>);
    await waitFor(() => screen.getByText('Alice Johnson'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });

  it('calls listDelegations on mount', async () => {
    render(<MemoryRouter><DelegatedAdminPage /></MemoryRouter>);
    await waitFor(() => expect(organizationGovernanceApi.listDelegations).toHaveBeenCalledTimes(1));
  });
});
