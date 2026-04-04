import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';

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

import { BreakGlassPage } from '../../features/identity-access/pages/BreakGlassPage';

function renderPage() {
  return renderWithProviders(<BreakGlassPage />);
}

describe('BreakGlassPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders page heading', () => {
    renderPage();
    expect(screen.getByText('Break Glass Access')).toBeInTheDocument();
  });

  it('renders request button', () => {
    renderPage();
    expect(screen.getByText('Request Emergency Access')).toBeInTheDocument();
  });

  it('shows empty state when no requests', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('No break glass requests found.')).toBeInTheDocument();
    });
  });
});
