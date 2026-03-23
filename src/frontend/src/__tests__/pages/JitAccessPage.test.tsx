import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/identity-access/api', () => ({
  identityApi: {
    listPendingJitRequests: vi.fn(),
    requestJitAccess: vi.fn(),
    decideJitAccess: vi.fn(),
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
import { JitAccessPage } from '../../features/identity-access/pages/JitAccessPage';

const mockRequests = [
  {
    id: 'jit-1',
    requestedBy: 'alice@example.com',
    permissionCode: 'admin:write',
    scope: 'Service:order-svc',
    justification: 'Need to deploy hotfix',
    status: 'Pending' as const,
    requestedAt: '2025-06-01T10:00:00Z',
    approvalDeadline: '2025-06-02T10:00:00Z',
    grantedFrom: null,
    grantedUntil: null,
  },
];

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter><JitAccessPage /></MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('JitAccessPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(identityApi.listPendingJitRequests).mockResolvedValue(mockRequests);
  });

  it('renders page heading', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('identity.jitAccess.title')).toBeInTheDocument();
    });
  });

  it('renders data after loading', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('admin:write')).toBeInTheDocument();
    });
  });

  it('shows loading state while fetching', () => {
    vi.mocked(identityApi.listPendingJitRequests).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('admin:write')).not.toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getByText('admin:write'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });

  it('calls listPendingJitRequests on mount', async () => {
    renderPage();
    await waitFor(() => expect(identityApi.listPendingJitRequests).toHaveBeenCalled());
  });
});
