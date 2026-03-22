import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { EnvironmentComparisonPage } from '../../features/operations/pages/EnvironmentComparisonPage';

vi.mock('../../features/operations/api/runtimeIntelligence', () => ({
  runtimeIntelligenceApi: {
    compareReleaseRuntime: vi.fn(),
    getDriftFindings: vi.fn(),
    getReleaseHealthTimeline: vi.fn(),
    getObservabilityScore: vi.fn(),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn() },
}));

import { runtimeIntelligenceApi } from '../../features/operations/api/runtimeIntelligence';

const mockCompareResponse = {
  serviceName: 'payment-service',
  environment: 'production',
  beforeMetrics: { avgLatencyMs: 100, p99LatencyMs: 200, errorRate: 0.01, requestsPerSecond: 500, cpuUsagePercent: 30, memoryUsageMb: 512 },
  afterMetrics: { avgLatencyMs: 120, p99LatencyMs: 240, errorRate: 0.02, requestsPerSecond: 480, cpuUsagePercent: 35, memoryUsageMb: 600 },
  beforeDataPoints: 5,
  afterDataPoints: 5,
  latencyDeltaPercent: 20,
  errorRateDeltaPercent: 100,
  throughputDeltaPercent: -4,
};

const mockDriftFindings = {
  items: [
    {
      id: 'df-1',
      serviceName: 'payment-service',
      environment: 'production',
      metricName: 'AvgLatencyMs',
      expectedValue: 100,
      actualValue: 180,
      deviationPercent: 80,
      severity: 'High' as const,
      detectedAt: '2026-03-20T10:00:00Z',
      acknowledgedAt: null,
    },
  ],
  totalCount: 1,
  page: 1,
  pageSize: 20,
};

const mockScore = {
  serviceName: 'payment-service',
  environment: 'production',
  score: 82.5,
  grade: 'B',
  level: 'Good',
  breakdown: { latencyScore: 85, errorScore: 80, throughputScore: 83, resourceScore: 82 },
  computedAt: '2026-03-20T10:00:00Z',
};

const mockTimeline = {
  serviceName: 'payment-service',
  environment: 'production',
  points: [
    {
      releaseId: null,
      releaseName: 'v1.2.0',
      periodStart: '2026-03-14T00:00:00Z',
      periodEnd: '2026-03-20T00:00:00Z',
      avgLatencyMs: 110,
      errorRate: 0.015,
      requestsPerSecond: 490,
      snapshotCount: 5,
    },
  ],
};

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
    },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <EnvironmentComparisonPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('EnvironmentComparisonPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the page with comparison form in initial state', () => {
    renderPage();
    // Form should be visible without any results
    expect(screen.getByLabelText('environmentComparison.serviceName')).toBeTruthy();
    expect(screen.getByLabelText('environmentComparison.environment')).toBeTruthy();
    // Empty state message should be shown
    expect(screen.getByText('environmentComparison.emptyState')).toBeTruthy();
  });

  it('shows results after form submission with service name', async () => {
    vi.mocked(runtimeIntelligenceApi.compareReleaseRuntime).mockResolvedValue(mockCompareResponse);
    vi.mocked(runtimeIntelligenceApi.getDriftFindings).mockResolvedValue(mockDriftFindings);
    vi.mocked(runtimeIntelligenceApi.getObservabilityScore).mockResolvedValue(mockScore);
    vi.mocked(runtimeIntelligenceApi.getReleaseHealthTimeline).mockResolvedValue(mockTimeline);

    renderPage();

    const serviceInput = screen.getByLabelText('environmentComparison.serviceName');
    await userEvent.type(serviceInput, 'payment-service');

    const compareButton = screen.getByText('environmentComparison.compare');
    await userEvent.click(compareButton);

    await waitFor(() => {
      expect(runtimeIntelligenceApi.compareReleaseRuntime).toHaveBeenCalled();
    });
  });

  it('does not call API when service name is empty', async () => {
    renderPage();

    const compareButton = screen.getByText('environmentComparison.compare');
    await userEvent.click(compareButton);

    expect(runtimeIntelligenceApi.compareReleaseRuntime).not.toHaveBeenCalled();
  });

  it('shows drift findings severity badges when findings exist', async () => {
    vi.mocked(runtimeIntelligenceApi.compareReleaseRuntime).mockResolvedValue(mockCompareResponse);
    vi.mocked(runtimeIntelligenceApi.getDriftFindings).mockResolvedValue(mockDriftFindings);
    vi.mocked(runtimeIntelligenceApi.getObservabilityScore).mockResolvedValue(mockScore);
    vi.mocked(runtimeIntelligenceApi.getReleaseHealthTimeline).mockResolvedValue(mockTimeline);

    renderPage();

    const serviceInput = screen.getByLabelText('environmentComparison.serviceName');
    await userEvent.type(serviceInput, 'payment-service');

    const compareButton = screen.getByText('environmentComparison.compare');
    await userEvent.click(compareButton);

    await waitFor(() => {
      expect(runtimeIntelligenceApi.getDriftFindings).toHaveBeenCalledWith(
        expect.objectContaining({ serviceName: 'payment-service', unacknowledgedOnly: true })
      );
    });
  });

  it('shows no drift findings message when findings are empty', async () => {
    vi.mocked(runtimeIntelligenceApi.compareReleaseRuntime).mockResolvedValue(mockCompareResponse);
    vi.mocked(runtimeIntelligenceApi.getDriftFindings).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    });
    vi.mocked(runtimeIntelligenceApi.getObservabilityScore).mockResolvedValue(mockScore);
    vi.mocked(runtimeIntelligenceApi.getReleaseHealthTimeline).mockResolvedValue(mockTimeline);

    renderPage();

    const serviceInput = screen.getByLabelText('environmentComparison.serviceName');
    await userEvent.type(serviceInput, 'stable-service');

    const compareButton = screen.getByText('environmentComparison.compare');
    await userEvent.click(compareButton);

    await waitFor(() => {
      expect(screen.getByText('environmentComparison.noDriftFindings')).toBeTruthy();
    });
  });
});
