/**
 * Tests for PostIncidentPage.
 *
 * @vitest-environment jsdom
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/operations/api/telemetry', () => ({
  getPostMortems: vi.fn(),
  getSreSummary: vi.fn(),
  getSreTimeSeries: vi.fn(),
  getSreTopRequests: vi.fn(),
  getSreTopQueries: vi.fn(),
  queryLogs: vi.fn(),
  queryTraces: vi.fn(),
  getTraceDetail: vi.fn(),
  queryMetrics: vi.fn(),
  getTopErrors: vi.fn(),
  compareLatency: vi.fn(),
  correlateByTraceId: vi.fn(),
  getTelemetryHealth: vi.fn(),
}));

vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn().mockReturnValue({
    activeEnvironmentId: 'env-prod-001',
    activeEnvironment: { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [{ id: 'env-prod-001', name: 'Production' }],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  }),
}));

import { getPostMortems } from '../../features/operations/api/telemetry';
import { PostIncidentPage } from '../../features/operations/pages/PostIncidentPage';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <PostIncidentPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const mockPostMortems = [
  {
    id: 'pm-001',
    title: 'Order Service Outage — 2026-01-15',
    incidentId: 'inc-001',
    incidentTitle: 'Order Service P1 Outage',
    status: 'published' as const,
    author: 'alice@example.com',
    severity: 'critical',
    actionItemsCount: 5,
    openActionItemsCount: 2,
    createdAt: new Date(Date.now() - 2592000000).toISOString(),
    patternCount: 3,
    environment: 'production',
  },
  {
    id: 'pm-002',
    title: 'Payment Gateway Latency Spike',
    incidentId: 'inc-002',
    incidentTitle: 'Payment Latency P2',
    status: 'review' as const,
    author: 'bob@example.com',
    severity: 'high',
    actionItemsCount: 3,
    openActionItemsCount: 3,
    createdAt: new Date(Date.now() - 864000000).toISOString(),
    patternCount: 1,
    environment: 'production',
  },
];

beforeEach(() => {
  vi.mocked(getPostMortems).mockResolvedValue(mockPostMortems);
});

describe('PostIncidentPage', () => {
  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Post-Incident Learning Hub')).toBeTruthy();
    });
  });

  it('renders post-mortem titles', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Order Service Outage — 2026-01-15')).toBeTruthy();
    });
  });

  it('renders completed status badge', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Published')).toBeTruthy();
    });
  });

  it('renders in-progress status badge', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('In Review')).toBeTruthy();
    });
  });

  it('renders severity label', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('critical')).toBeTruthy();
    });
  });

  it('renders author name', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('alice@example.com')).toBeTruthy();
    });
  });

  it('renders open action items badge (openActionItemsCount/actionItemsCount)', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('2/5')).toBeTruthy();
    });
  });

  it('renders action items count', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('2/5')).toBeTruthy();
    });
  });

  it('shows fallback data when API returns empty array', async () => {
    vi.mocked(getPostMortems).mockResolvedValueOnce([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByRole('row').length).toBeGreaterThan(0);
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(getPostMortems).mockRejectedValue(new Error('network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Failed to load/i)).toBeTruthy();
    });
  });
});
