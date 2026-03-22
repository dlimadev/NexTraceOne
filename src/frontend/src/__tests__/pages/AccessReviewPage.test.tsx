import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

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

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { AccessReviewPage } from '../../features/identity-access/pages/AccessReviewPage';

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter><AccessReviewPage /></MemoryRouter>
    </QueryClientProvider>
  );
}

describe('AccessReviewPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders page heading', () => {
    renderPage();
    expect(screen.getByText('accessReview.title')).toBeInTheDocument();
  });

  it('renders start campaign button', () => {
    renderPage();
    expect(screen.getByText('accessReview.startCampaign')).toBeInTheDocument();
  });

  it('shows empty state when no campaigns', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('accessReview.noCampaigns')).toBeInTheDocument();
    });
  });
});
