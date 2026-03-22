import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

vi.mock('../../features/identity-access/api', () => ({
  identityApi: {
    listActiveSessions: vi.fn().mockResolvedValue({ items: [] }),
    revoke: vi.fn(),
  },
}));

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({ user: { id: 'u1' }, tenantId: 't1' }),
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { MySessionsPage } from '../../features/identity-access/pages/MySessionsPage';

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter><MySessionsPage /></MemoryRouter>
    </QueryClientProvider>
  );
}

describe('MySessionsPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders page heading', () => {
    renderPage();
    expect(screen.getByText('sessions.title')).toBeInTheDocument();
  });

  it('shows empty state when no sessions', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('sessions.empty')).toBeInTheDocument();
    });
  });
});
