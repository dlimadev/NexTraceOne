import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { PostReleaseReviewPage } from '../../features/change-governance/pages/PostReleaseReviewPage';

vi.mock('../../features/change-governance/api/changeIntelligence', () => ({
  changeIntelligenceApi: {
    getPostReleaseReview: vi.fn(),
    startPostReleaseReview: vi.fn(),
    progressPostReleaseReview: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { changeIntelligenceApi } from '../../features/change-governance/api/changeIntelligence';

const mockReview = {
  reviewId: 'review-001',
  releaseId: 'release-abc',
  serviceName: 'payment-service',
  version: '1.2.0',
  environment: 'Production',
  currentPhase: 'Analysis',
  outcome: 'Pass',
  confidenceScore: 0.87,
  summary: 'All post-deploy metrics within acceptable thresholds.',
  isCompleted: false,
  startedAt: '2026-04-10T08:00:00Z',
  completedAt: null,
  baseline: {
    id: 'baseline-001',
    requestsPerMinute: 412.5,
    errorRate: 0.002,
    avgLatencyMs: 45,
    p95LatencyMs: 98,
    p99LatencyMs: 180,
    throughput: 5200,
    collectedFrom: '2026-04-09T08:00:00Z',
    collectedTo: '2026-04-10T08:00:00Z',
    capturedAt: '2026-04-10T08:05:00Z',
  },
  observationWindows: [
    {
      id: 'window-001',
      phase: 'Immediate',
      startsAt: '2026-04-10T08:00:00Z',
      endsAt: '2026-04-10T09:00:00Z',
      isCollected: true,
      collectedAt: '2026-04-10T09:01:00Z',
      requestsPerMinute: 405,
      errorRate: 0.003,
      avgLatencyMs: 47,
      p95LatencyMs: 100,
      p99LatencyMs: 185,
      throughput: 5100,
    },
  ],
};

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <PostReleaseReviewPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('PostReleaseReviewPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(changeIntelligenceApi.getPostReleaseReview).mockResolvedValue(mockReview);
    vi.mocked(changeIntelligenceApi.startPostReleaseReview).mockResolvedValue({ reviewId: 'review-001' });
    vi.mocked(changeIntelligenceApi.progressPostReleaseReview).mockResolvedValue({});
  });

  it('renders page title and subtitle', () => {
    renderPage();
    expect(screen.getAllByText(/Post-Release Review/i).length).toBeGreaterThan(0);
  });

  it('renders search input and button', () => {
    renderPage();
    expect(screen.getByPlaceholderText(/Release ID/i)).toBeTruthy();
    expect(screen.getByText('Search')).toBeTruthy();
  });

  it('shows empty state when no release ID is entered', () => {
    renderPage();
    expect(screen.getByText(/No review loaded/i)).toBeTruthy();
  });

  it('loads and shows review data after searching', async () => {
    renderPage();
    const input = screen.getByPlaceholderText(/Release ID/i);
    await userEvent.type(input, 'release-abc');
    await userEvent.click(screen.getByText('Search'));

    await waitFor(() => {
      expect(screen.getByText(/payment-service/i)).toBeTruthy();
    });
    expect(screen.getByText(/1\.2\.0/)).toBeTruthy();
  });

  it('displays confidence score as percentage', async () => {
    renderPage();
    const input = screen.getByPlaceholderText(/Release ID/i);
    await userEvent.type(input, 'release-abc');
    await userEvent.click(screen.getByText('Search'));

    await waitFor(() => {
      expect(screen.getByText('87%')).toBeTruthy();
    });
  });

  it('shows observation windows table', async () => {
    renderPage();
    const input = screen.getByPlaceholderText(/Release ID/i);
    await userEvent.type(input, 'release-abc');
    await userEvent.click(screen.getByText('Search'));

    await waitFor(() => {
      expect(screen.getAllByText(/Observation Windows/i).length).toBeGreaterThan(0);
    });
    expect(screen.getByText('Immediate')).toBeTruthy();
  });

  it('shows baseline performance section', async () => {
    renderPage();
    const input = screen.getByPlaceholderText(/Release ID/i);
    await userEvent.type(input, 'release-abc');
    await userEvent.click(screen.getByText('Search'));

    await waitFor(() => {
      expect(screen.getAllByText(/Baseline/i).length).toBeGreaterThan(0);
    });
  });

  it('shows progress review button for incomplete review', async () => {
    renderPage();
    const input = screen.getByPlaceholderText(/Release ID/i);
    await userEvent.type(input, 'release-abc');
    await userEvent.click(screen.getByText('Search'));

    await waitFor(() => {
      expect(screen.getByText(/Progress Review/i)).toBeTruthy();
    });
  });

  it('shows Start Review button when review is not found', async () => {
    vi.mocked(changeIntelligenceApi.getPostReleaseReview).mockRejectedValue(new Error('404'));
    renderPage();
    const input = screen.getByPlaceholderText(/Release ID/i);
    await userEvent.type(input, 'release-not-found');
    await userEvent.click(screen.getByText('Search'));

    await waitFor(() => {
      expect(screen.getByText(/Start Post-Release Review/i)).toBeTruthy();
    });
  });

  it('calls startPostReleaseReview on button click', async () => {
    vi.mocked(changeIntelligenceApi.getPostReleaseReview).mockRejectedValue(new Error('404'));
    renderPage();
    const input = screen.getByPlaceholderText(/Release ID/i);
    await userEvent.type(input, 'release-new');
    await userEvent.click(screen.getByText('Search'));

    await waitFor(() => {
      expect(screen.getByText(/Start Post-Release Review/i)).toBeTruthy();
    });
    await userEvent.click(screen.getByText(/Start Post-Release Review/i));
    await waitFor(() => {
      expect(changeIntelligenceApi.startPostReleaseReview).toHaveBeenCalledWith('release-new');
    });
  });
});
