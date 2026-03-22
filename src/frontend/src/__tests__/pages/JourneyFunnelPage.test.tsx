import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { JourneyFunnelPage } from '../../features/product-analytics/pages/JourneyFunnelPage';

vi.mock('../../features/product-analytics/api/productAnalyticsApi', () => ({
  productAnalyticsApi: {
    getJourneys: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { productAnalyticsApi } from '../../features/product-analytics/api/productAnalyticsApi';

const mockJourneysResponse = {
  journeys: [
    {
      journeyId: 'search_to_entity',
      journeyName: 'Search to Entity View',
      steps: [
        { stepId: 'search_executed', stepName: 'Search Executed', completionPercent: 100.0, order: 0 },
        { stepId: 'entity_viewed', stepName: 'Entity Viewed', completionPercent: 61.8, order: 1 },
      ],
      completionRate: 61.8,
      avgDurationMinutes: 4.2,
      status: 'Completed',
      biggestDropOff: 'results_displayed → result_clicked',
    },
    {
      journeyId: 'ai_prompt_to_action',
      journeyName: 'AI Prompt to Useful Action',
      steps: [
        { stepId: 'assistant_opened', stepName: 'Assistant Opened', completionPercent: 100.0, order: 0 },
        { stepId: 'response_used', stepName: 'Response Used', completionPercent: 48.6, order: 1 },
      ],
      completionRate: 48.6,
      avgDurationMinutes: 6.8,
      status: 'Completed',
      biggestDropOff: 'response_received → response_used',
    },
  ],
  averageCompletionRate: 55.2,
  mostCompletedJourney: 'search_to_entity',
  highestDropOffJourney: 'ai_prompt_to_action',
  periodLabel: 'last_30d',
};

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <JourneyFunnelPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('JourneyFunnelPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state initially', () => {
    vi.mocked(productAnalyticsApi.getJourneys).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it('renders journey funnels from API', async () => {
    vi.mocked(productAnalyticsApi.getJourneys).mockResolvedValue(mockJourneysResponse);
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('Search to Entity View').length).toBeGreaterThan(0);
    });
    expect(screen.getAllByText('AI Prompt to Useful Action').length).toBeGreaterThan(0);
  });

  it('shows error state on API failure', async () => {
    vi.mocked(productAnalyticsApi.getJourneys).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument();
    });
  });

  it('shows funnel steps after loading', async () => {
    vi.mocked(productAnalyticsApi.getJourneys).mockResolvedValue(mockJourneysResponse);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Search Executed')).toBeInTheDocument();
    });
    expect(screen.getByText('Entity Viewed')).toBeInTheDocument();
  });

  it('shows the biggest drop-off insight', async () => {
    vi.mocked(productAnalyticsApi.getJourneys).mockResolvedValue(mockJourneysResponse);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/results_displayed/i)).toBeInTheDocument();
    });
  });

  it('shows empty state when no journeys returned', async () => {
    vi.mocked(productAnalyticsApi.getJourneys).mockResolvedValue({
      journeys: [],
      averageCompletionRate: 0,
      mostCompletedJourney: '',
      highestDropOffJourney: '',
      periodLabel: 'last_30d',
    });
    renderPage();
    await waitFor(() => {
      expect(screen.queryByText('Search to Entity View')).not.toBeInTheDocument();
    });
  });

  it('calls getJourneys API not local mock', async () => {
    vi.mocked(productAnalyticsApi.getJourneys).mockResolvedValue(mockJourneysResponse);
    renderPage();
    await waitFor(() => {
      expect(productAnalyticsApi.getJourneys).toHaveBeenCalledWith({ range: 'last_30d' });
    });
  });
});
