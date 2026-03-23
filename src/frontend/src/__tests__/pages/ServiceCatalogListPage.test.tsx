import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/catalog/api', () => ({
  serviceCatalogApi: {
    listServices: vi.fn(),
    getServicesSummary: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { serviceCatalogApi } from '../../features/catalog/api';
import { ServiceCatalogListPage } from '../../features/catalog/pages/ServiceCatalogListPage';

const mockList = {
  items: [
    {
      serviceId: 'svc-1',
      name: 'order-processor',
      displayName: 'Order Processor',
      description: 'Processes customer orders',
      serviceType: 'RestApi',
      domain: 'Commerce',
      teamName: 'Order Squad',
      criticality: 'High',
      lifecycleStatus: 'Active',
      exposureType: 'Internal',
    },
  ],
  totalCount: 1,
};

const mockSummary = {
  totalCount: 42,
  criticalCount: 5,
  highCriticalityCount: 12,
  activeCount: 35,
  deprecatedCount: 4,
  retiredCount: 3,
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter><ServiceCatalogListPage /></MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ServiceCatalogListPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(serviceCatalogApi.listServices).mockResolvedValue(mockList);
    vi.mocked(serviceCatalogApi.getServicesSummary).mockResolvedValue(mockSummary);
  });

  it('renders page heading', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('catalog.title');
    });
  });

  it('renders data after loading', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Order Processor')).toBeInTheDocument();
    });
  });

  it('shows loading state while fetching', () => {
    vi.mocked(serviceCatalogApi.listServices).mockReturnValue(new Promise(() => {}));
    vi.mocked(serviceCatalogApi.getServicesSummary).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Order Processor')).not.toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Order Processor'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });

  it('calls serviceCatalogApi on mount', async () => {
    renderPage();
    await waitFor(() => expect(serviceCatalogApi.listServices).toHaveBeenCalled());
    expect(serviceCatalogApi.getServicesSummary).toHaveBeenCalled();
  });
});
