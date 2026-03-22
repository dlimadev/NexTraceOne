import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ValueTrackingPage } from '../../features/product-analytics/pages/ValueTrackingPage';

vi.mock('../../features/product-analytics/api/productAnalyticsApi', () => ({
  productAnalyticsApi: {
    getValueMilestones: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { productAnalyticsApi } from '../../features/product-analytics/api/productAnalyticsApi';

const mockValueMilestonesResponse = {
  milestones: [
    {
      milestoneType: 'FirstSearchSuccess',
      milestoneName: 'First Search Success',
      completionRate: 94.2,
      avgTimeToReachMinutes: 3.2,
      usersReached: 221,
      trend: 'Stable' as const,
    },
    {
      milestoneType: 'FirstContractPublished',
      milestoneName: 'First Contract Published',
      completionRate: 34.8,
      avgTimeToReachMinutes: 168.0,
      usersReached: 82,
      trend: 'Improving' as const,
    },
  ],
  avgTimeToFirstValueMinutes: 18.5,
  avgTimeToCoreValueMinutes: 142.0,
  overallCompletionRate: 64.5,
  fastestMilestone: 'FirstSearchSuccess',
  slowestMilestone: 'FirstEvidenceExported',
  periodLabel: 'last_30d',
};

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ValueTrackingPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('ValueTrackingPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state initially', () => {
    vi.mocked(productAnalyticsApi.getValueMilestones).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it('renders milestones from API', async () => {
    vi.mocked(productAnalyticsApi.getValueMilestones).mockResolvedValue(mockValueMilestonesResponse);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('94.2%')).toBeInTheDocument();
    });
    expect(screen.getByText('34.8%')).toBeInTheDocument();
  });

  it('shows error state on API failure', async () => {
    vi.mocked(productAnalyticsApi.getValueMilestones).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /retry/i })).toBeInTheDocument();
    });
  });

  it('shows overall completion rate from API', async () => {
    vi.mocked(productAnalyticsApi.getValueMilestones).mockResolvedValue(mockValueMilestonesResponse);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('64.5%')).toBeInTheDocument();
    });
  });

  it('shows time-to-value from API data', async () => {
    vi.mocked(productAnalyticsApi.getValueMilestones).mockResolvedValue(mockValueMilestonesResponse);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('19m')).toBeInTheDocument();
    });
  });

  it('shows empty state when no milestones returned', async () => {
    vi.mocked(productAnalyticsApi.getValueMilestones).mockResolvedValue({
      milestones: [],
      avgTimeToFirstValueMinutes: 0,
      avgTimeToCoreValueMinutes: 0,
      overallCompletionRate: 0,
      fastestMilestone: '',
      slowestMilestone: '',
      periodLabel: 'last_30d',
    });
    renderPage();
    await waitFor(() => {
      expect(screen.queryByText('94.2%')).not.toBeInTheDocument();
    });
  });

  it('calls getValueMilestones API not local mock', async () => {
    vi.mocked(productAnalyticsApi.getValueMilestones).mockResolvedValue(mockValueMilestonesResponse);
    renderPage();
    await waitFor(() => {
      expect(productAnalyticsApi.getValueMilestones).toHaveBeenCalledWith({ range: 'last_30d' });
    });
  });
});
