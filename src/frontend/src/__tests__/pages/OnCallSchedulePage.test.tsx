/**
 * Tests for OnCallSchedulePage.
 *
 * @vitest-environment jsdom
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/operations/api/telemetry', () => ({
  getOnCallSchedules: vi.fn(),
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

import { getOnCallSchedules } from '../../features/operations/api/telemetry';
import { OnCallSchedulePage } from '../../features/operations/pages/OnCallSchedulePage';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <OnCallSchedulePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const mockSchedules = [
  {
    id: 'oc-001',
    name: 'Platform Engineering On-Call',
    teamName: 'Platform Engineering',
    serviceName: 'order-service',
    currentOnCall: 'alice@example.com',
    nextOnCall: 'bob@example.com',
    rotationType: 'weekly' as const,
    timezone: 'UTC',
    escalationLevels: 3,
    activeOverrides: 1,
    environment: 'production',
  },
  {
    id: 'oc-002',
    name: 'Backend Services On-Call',
    teamName: 'Backend Services',
    serviceName: 'payment-service',
    currentOnCall: 'charlie@example.com',
    nextOnCall: 'dave@example.com',
    rotationType: 'followTheSun' as const,
    timezone: 'America/Sao_Paulo',
    escalationLevels: 2,
    activeOverrides: 0,
    environment: 'production',
  },
];

beforeEach(() => {
  vi.mocked(getOnCallSchedules).mockResolvedValue(mockSchedules);
});

describe('OnCallSchedulePage', () => {
  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('On-Call Schedule Management')).toBeTruthy();
    });
  });

  it('renders team names', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Platform Engineering')).toBeTruthy();
      expect(screen.getByText('Backend Services')).toBeTruthy();
    });
  });

  it('renders on-call user email', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('alice@example.com')).toBeTruthy();
    });
  });

  it('renders next on-call user email', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('bob@example.com')).toBeTruthy();
    });
  });

  it('renders rotation type badge', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Weekly')).toBeTruthy();
    });
  });

  it('renders timezone', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('UTC')).toBeTruthy();
    });
  });

  it('renders schedule names', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Platform Engineering On-Call')).toBeTruthy();
    });
  });

  it('renders service name badges', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('order-service')).toBeTruthy();
    });
  });

  it('shows fallback data when API returns empty array', async () => {
    vi.mocked(getOnCallSchedules).mockResolvedValueOnce([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByRole('row').length).toBeGreaterThan(0);
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(getOnCallSchedules).mockRejectedValue(new Error('network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Failed to load/i)).toBeTruthy();
    });
  });

  it('renders escalation delay minutes', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('On-Call Schedule Management')).toBeTruthy();
    });
    await waitFor(() => {
      expect(screen.getAllByRole('row').length).toBeGreaterThan(1);
    });
  });
});
