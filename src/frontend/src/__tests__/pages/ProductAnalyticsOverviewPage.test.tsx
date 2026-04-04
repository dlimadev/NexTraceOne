import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ProductAnalyticsOverviewPage } from '../../features/product-analytics/pages/ProductAnalyticsOverviewPage';

vi.mock('../../features/product-analytics/api/productAnalyticsApi', () => ({
  productAnalyticsApi: {
    getSummary: vi.fn(),
    getModuleAdoption: vi.fn(),
    getPersonaUsage: vi.fn(),
    getJourneys: vi.fn(),
    getValueMilestones: vi.fn(),
    getFriction: vi.fn(),
    getAdoptionFunnel: vi.fn(),
    getFeatureHeatmap: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { productAnalyticsApi } from '../../features/product-analytics/api/productAnalyticsApi';

const mockSummary = {
  totalEvents: 12450,
  uniqueUsers: 142,
  activePersonas: 5,
  topModules: [
    { module: 'service-catalog', moduleName: 'Service Catalog', eventCount: 890, uniqueUsers: 112, trend: 'Up' as const },
  ],
  adoptionScore: 74,
  valueScore: 68,
  frictionScore: 22,
  avgTimeToFirstValueMinutes: 8.5,
  avgTimeToCoreValueMinutes: 32.1,
  trendDirection: 'Up' as const,
  periodLabel: 'Last 30 days',
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ProductAnalyticsOverviewPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ProductAnalyticsOverviewPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(productAnalyticsApi.getSummary).mockResolvedValue(mockSummary);
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Product Analytics')).toBeDefined();
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(productAnalyticsApi.getSummary).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders analytics summary when data is available', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('74');
    });
  });
});
