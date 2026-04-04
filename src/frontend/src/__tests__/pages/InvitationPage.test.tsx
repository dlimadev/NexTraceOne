import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';

vi.mock('../../features/identity-access/api', () => ({
  identityApi: {
    getInvitationDetails: vi.fn(),
    acceptInvitation: vi.fn(),
  },
}));

import { identityApi } from '../../features/identity-access/api';
import { InvitationPage } from '../../features/identity-access/pages/InvitationPage';

const mockInvitationDetails = {
  email: 'bob@example.com',
  organizationName: 'Acme Corp',
  roleName: 'Engineer',
  expiresAt: '2026-12-31T23:59:59Z',
};

function renderPage(search = '?token=valid-token-123') {
  return renderWithProviders(<InvitationPage />, {
    routerProps: { initialEntries: [`/invitation${search}`] },
  });
}

describe('InvitationPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(identityApi.getInvitationDetails).mockResolvedValue(mockInvitationDetails);
  });

  it('renders invitation details after loading', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('bob@example.com')).toBeInTheDocument();
    });
  });

  it('calls getInvitationDetails with token from URL', async () => {
    renderPage();
    await waitFor(() => expect(identityApi.getInvitationDetails).toHaveBeenCalledWith('valid-token-123'));
  });

  it('shows loading state while fetching', () => {
    vi.mocked(identityApi.getInvitationDetails).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('bob@example.com')).not.toBeInTheDocument();
  });

  it('shows invalid token state when no token provided', async () => {
    renderPage('');
    await waitFor(() => {
      expect(screen.getByText(/invitation link is invalid/i)).toBeInTheDocument();
    });
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getByText('bob@example.com'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });
});
