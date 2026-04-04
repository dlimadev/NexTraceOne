import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';

vi.mock('../../features/identity-access/api', () => ({
  identityApi: {
    listActiveSessions: vi.fn().mockResolvedValue({ items: [] }),
    revoke: vi.fn(),
  },
}));

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'u1' }, tenantId: 't1' }),
}));

import { MySessionsPage } from '../../features/identity-access/pages/MySessionsPage';

function renderPage() {
  return renderWithProviders(<MySessionsPage />);
}

describe('MySessionsPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders page heading', () => {
    renderPage();
    expect(screen.getAllByText('Active Sessions').length).toBeGreaterThanOrEqual(1);
  });

  it('shows empty state when no sessions', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/no active sessions/i)).toBeInTheDocument();
    });
  });
});
