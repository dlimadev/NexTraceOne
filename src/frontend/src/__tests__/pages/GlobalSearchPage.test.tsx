import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/catalog/api/globalSearch', () => ({
  globalSearchApi: {
    search: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

vi.mock('../../releaseScope', () => ({
  isRouteAvailableInFinalProductionScope: () => true,
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { globalSearchApi } from '../../features/catalog/api/globalSearch';
import { GlobalSearchPage } from '../../features/catalog/pages/GlobalSearchPage';

const mockSearchResults = {
  items: [
    {
      entityId: 'svc-1',
      route: '/services/svc-1',
      entityType: 'Service',
      title: 'Order Service',
      subtitle: 'Handles order processing',
      owner: 'Order Squad',
      status: 'active',
    },
  ],
  facetCounts: {
    services: 1,
    contracts: 0,
    runbooks: 0,
    docs: 0,
  },
  totalResults: 1,
};

function renderPage(search = '') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/search${search}`]}>
        <GlobalSearchPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn().mockReturnValue({
    activeEnvironmentId: 'env-prod-001',
    activeEnvironment: { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [{ id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true }],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  }),
}));

describe('GlobalSearchPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(globalSearchApi.search).mockResolvedValue(mockSearchResults);
  });

  it('renders page heading', () => {
    renderPage();
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('commandPalette.globalSearch.title');
  });

  it('renders search results when query provided', async () => {
    renderPage('?q=order');
    await waitFor(() => {
      expect(screen.getByText('Order Service')).toBeInTheDocument();
    });
  });

  it('shows empty state when no query', () => {
    renderPage();
    expect(screen.queryByText('Order Service')).not.toBeInTheDocument();
  });

  it('does not render DemoBanner', () => {
    renderPage();
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });

  it('calls globalSearchApi.search when query is present', async () => {
    renderPage('?q=order');
    await waitFor(() => expect(globalSearchApi.search).toHaveBeenCalled());
  });
});
