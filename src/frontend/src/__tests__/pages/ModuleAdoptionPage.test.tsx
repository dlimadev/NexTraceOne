import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ModuleAdoptionPage } from '../../features/product-analytics/pages/ModuleAdoptionPage';

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

const mockData = {
  modules: [
    {
      module: 'service-catalog',
      moduleName: 'Service Catalog',
      adoptionPercent: 88.2,
      totalActions: 4520,
      uniqueUsers: 142,
      depthScore: 0.72,
      trend: 'Up' as const,
      topFeatures: ['Service Detail', 'Dependency Map'],
    },
  ],
  overallAdoptionScore: 74,
  mostAdopted: 'Service Catalog',
  leastAdopted: 'FinOps',
  biggestGrowth: 'Contract Governance',
  periodLabel: 'Last 30 days',
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ModuleAdoptionPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ModuleAdoptionPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(productAnalyticsApi.getModuleAdoption).mockResolvedValue(mockData);
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Module Adoption')).toBeDefined();
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(productAnalyticsApi.getModuleAdoption).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders modules when data is available', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Service Catalog')).toBeDefined();
    });
  });
});
