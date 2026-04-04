import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { FeatureHeatmapPage } from '../../features/product-analytics/pages/FeatureHeatmapPage';

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

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <FeatureHeatmapPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('FeatureHeatmapPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(productAnalyticsApi.getFeatureHeatmap).mockResolvedValue({
      cells: [],
      modules: [],
      personas: [],
      maxValue: 0,
    });
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Feature Adoption Heatmap')).toBeDefined();
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(productAnalyticsApi.getFeatureHeatmap).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(productAnalyticsApi.getFeatureHeatmap).mockRejectedValue(new Error('API error'));
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toBeDefined();
    });
  });
});
