import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';

vi.mock('../../features/identity-access/api', () => ({
  identityApi: {
    listAccessReviewCampaigns: vi.fn().mockResolvedValue({ items: [] }),
    getAccessReviewCampaign: vi.fn(),
    startAccessReviewCampaign: vi.fn(),
  },
}));

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({ tenantId: 't1', user: { id: 'u1' } }),
}));

import { AccessReviewPage } from '../../features/identity-access/pages/AccessReviewPage';

function renderPage() {
  return renderWithProviders(<AccessReviewPage />);
}

describe('AccessReviewPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders page heading', () => {
    renderPage();
    expect(screen.getByText('Access Review')).toBeInTheDocument();
  });

  it('renders start campaign button', () => {
    renderPage();
    expect(screen.getByText('Start Campaign')).toBeInTheDocument();
  });

  it('shows empty state when no campaigns', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/no access review campaigns/i)).toBeInTheDocument();
    });
  });
});
