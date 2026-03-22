import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

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

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { UsersPage } from '../../features/identity-access/pages/UsersPage';

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter><UsersPage /></MemoryRouter>
    </QueryClientProvider>
  );
}

describe('UsersPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders page heading', () => {
    renderPage();
    expect(screen.getByText('users.title')).toBeInTheDocument();
  });

  it('renders create user button', () => {
    renderPage();
    expect(screen.getByText('users.createUser')).toBeInTheDocument();
  });

  it('shows empty state when no users', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('users.noUsers')).toBeInTheDocument();
    });
  });
});
