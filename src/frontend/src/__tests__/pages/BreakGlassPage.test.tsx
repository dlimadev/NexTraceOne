import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

vi.mock('../../features/identity-access/api', () => ({
  identityApi: {
    listBreakGlassRequests: vi.fn().mockResolvedValue({ items: [] }),
    requestBreakGlass: vi.fn(),
    revokeBreakGlass: vi.fn(),
  },
}));

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({ tenantId: 't1', user: { id: 'u1' } }),
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { BreakGlassPage } from '../../features/identity-access/pages/BreakGlassPage';

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter><BreakGlassPage /></MemoryRouter>
    </QueryClientProvider>
  );
}

describe('BreakGlassPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders page heading', () => {
    renderPage();
    expect(screen.getByText('breakGlass.title')).toBeInTheDocument();
  });

  it('renders request button', () => {
    renderPage();
    expect(screen.getByText('breakGlass.requestAccess')).toBeInTheDocument();
  });

  it('shows empty state when no requests', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('breakGlass.noRequests')).toBeInTheDocument();
    });
  });
});
