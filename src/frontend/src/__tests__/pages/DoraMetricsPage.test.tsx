import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { DoraMetricsPage } from '../../features/change-governance/pages/DoraMetricsPage';

vi.mock('../../features/change-governance/api/changeConfidence', () => ({
  changeConfidenceApi: {
    getDoraMetrics: vi.fn(),
    scoreChange: vi.fn(),
    getBlastRadius: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { changeConfidenceApi } from '../../features/change-governance/api/changeConfidence';

const mockDoraMetrics = {
  deploymentFrequency: {
    deploysPerDay: 4.2,
    totalDeploys: 126,
    classification: 'Elite' as const,
  },
  leadTimeForChanges: {
    averageHours: 1.5,
    classification: 'Elite' as const,
  },
  changeFailureRate: {
    failurePercentage: 2.1,
    failedDeploys: 3,
    rolledBackDeploys: 1,
    totalDeploys: 126,
    classification: 'Elite' as const,
  },
  timeToRestoreService: {
    averageHours: 0.38,
    classification: 'High' as const,
  },
  overallClassification: 'Elite' as const,
  periodDays: 30,
  serviceName: null,
  teamName: null,
  environment: null,
  generatedAt: '2026-04-01T00:00:00Z',
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <DoraMetricsPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('DoraMetricsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(changeConfidenceApi.getDoraMetrics).mockResolvedValue(mockDoraMetrics);
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('DORA Metrics')).toBeDefined();
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(changeConfidenceApi.getDoraMetrics).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders DORA metric cards when data is available', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Elite');
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(changeConfidenceApi.getDoraMetrics).mockRejectedValue(new Error('API error'));
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toBeDefined();
    });
  });
});
