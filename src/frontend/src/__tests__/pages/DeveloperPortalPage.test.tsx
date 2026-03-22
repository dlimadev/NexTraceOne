import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { DeveloperPortalPage } from '../../features/catalog/pages/DeveloperPortalPage';

vi.mock('../../features/catalog/api/developerPortal', () => ({
  developerPortalApi: {
    searchCatalog: vi.fn(),
    getMyApis: vi.fn(),
    getConsuming: vi.fn(),
    getApiDetail: vi.fn(),
    getApiHealth: vi.fn(),
    getApiTimeline: vi.fn(),
    getApiConsumers: vi.fn(),
    renderContract: vi.fn(),
    listSubscriptions: vi.fn(),
    createSubscription: vi.fn(),
    deleteSubscription: vi.fn(),
    executePlayground: vi.fn(),
    getPlaygroundHistory: vi.fn(),
    generateCode: vi.fn(),
    getAnalytics: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn(), delete: vi.fn() },
}));

import { developerPortalApi } from '../../features/catalog/api/developerPortal';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/portal']}>
        <DeveloperPortalPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('DeveloperPortalPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(developerPortalApi.searchCatalog).mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    vi.mocked(developerPortalApi.listSubscriptions).mockResolvedValue({ items: [], totalCount: 0 });
    vi.mocked(developerPortalApi.getPlaygroundHistory).mockResolvedValue({ items: [] });
    vi.mocked(developerPortalApi.getAnalytics).mockResolvedValue({ summary: {} });
    vi.mocked(developerPortalApi.getMyApis).mockResolvedValue({ items: [], totalCount: 0 });
    vi.mocked(developerPortalApi.getConsuming).mockResolvedValue({ items: [], totalCount: 0 });
  });

  it('renders the developer portal page', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Developer Portal')).toBeInTheDocument();
    });
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
    });
  });

  it('does not show preview badge', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.queryByText(/preview/i)).not.toBeInTheDocument();
    });
  });
});
