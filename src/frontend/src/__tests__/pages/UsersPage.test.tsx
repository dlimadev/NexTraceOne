import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';

vi.mock('../../features/identity-access/api', () => ({
  identityApi: {
    listTenantUsers: vi.fn().mockResolvedValue({ items: [], totalCount: 0 }),
    listRoles: vi.fn().mockResolvedValue({ items: [{ id: 'r1', name: 'Admin' }] }),
    createUser: vi.fn(),
  },
}));

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({ tenantId: 't1', user: { id: 'u1' } }),
}));

import { UsersPage } from '../../features/identity-access/pages/UsersPage';

function renderPage() {
  return renderWithProviders(<UsersPage />);
}

describe('UsersPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders page heading', () => {
    renderPage();
    expect(screen.getByText('Users')).toBeInTheDocument();
  });

  it('renders create user button', () => {
    renderPage();
    expect(screen.getByText('Create User')).toBeInTheDocument();
  });

  it('shows empty state when no users', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/no users found/i)).toBeInTheDocument();
    });
  });
});
