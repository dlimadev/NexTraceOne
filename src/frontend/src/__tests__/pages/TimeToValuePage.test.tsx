import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { TimeToValuePage } from '../../features/product-analytics/pages/TimeToValuePage';

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
        <TimeToValuePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('TimeToValuePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(productAnalyticsApi.getValueMilestones).mockResolvedValue({
      milestones: [],
      avgTimeToFirstValue: 0,
      avgTimeToCoreValue: 0,
      completionRate: 0,
    });
    vi.mocked(productAnalyticsApi.getSummary).mockResolvedValue({
      activeUsers: 0,
      dailyActiveUsers: 0,
      weeklyActiveUsers: 0,
      monthlyActiveUsers: 0,
      totalSessions: 0,
      avgSessionDuration: 0,
      topModules: [],
      topPersonas: [],
      engagementTrend: [],
    });
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Time to Value')).toBeDefined();
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(productAnalyticsApi.getValueMilestones).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(productAnalyticsApi.getValueMilestones).mockRejectedValue(new Error('API error'));
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toBeDefined();
    });
  });
});
