import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/identity-access/api', () => ({
  identityApi: {
    listDelegations: vi.fn(),
    createDelegation: vi.fn(),
    revokeDelegation: vi.fn(),
  },
}));

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({
    isAuthenticated: true,
    tenantId: 'tenant-1',
    user: { id: 'user-1', email: 'test@example.com' },
  }),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { identityApi } from '../../features/identity-access/api';
import { DelegationPage } from '../../features/identity-access/pages/DelegationPage';

const mockDelegations = [
  {
    delegationId: 'del-1',
    delegateeId: 'user-2',
    delegateeName: 'Bob Smith',
    permissions: ['read:services', 'write:contracts'],
    reason: 'Vacation coverage',
    validFrom: '2025-06-01T00:00:00Z',
    validUntil: '2025-06-15T00:00:00Z',
    status: 'Active',
    createdAt: '2025-05-30T00:00:00Z',
  },
];

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter><DelegationPage /></MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('DelegationPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(identityApi.listDelegations).mockResolvedValue(mockDelegations);
  });

  it('renders page heading', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('identity.delegation.title')).toBeInTheDocument();
    });
  });

  it('renders data after loading', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Bob Smith')).toBeInTheDocument();
    });
  });

  it('shows loading state while fetching', () => {
    vi.mocked(identityApi.listDelegations).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Bob Smith')).not.toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Bob Smith'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });

  it('calls identityApi.listDelegations on mount', async () => {
    renderPage();
    await waitFor(() => expect(identityApi.listDelegations).toHaveBeenCalled());
  });
});
