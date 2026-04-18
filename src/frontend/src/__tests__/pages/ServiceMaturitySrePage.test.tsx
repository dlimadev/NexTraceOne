/**
 * Tests for ServiceMaturitySrePage.
 *
 * @vitest-environment jsdom
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/operations/api/telemetry', () => ({
  getServiceMaturities: vi.fn(),
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

import { getServiceMaturities } from '../../features/operations/api/telemetry';
import { ServiceMaturitySrePage } from '../../features/operations/pages/ServiceMaturitySrePage';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ServiceMaturitySrePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const mockMaturities = [
  {
    id: 'mat-001',
    serviceName: 'order-service',
    teamName: 'Core Commerce',
    score: 82,
    maturityLevel: 'advanced' as const,
    hasOnCall: true,
    hasRunbook: true,
    hasSlo: true,
    hasDashboard: true,
    hasAlerts: true,
    hasProfiling: true,
    hasRecentPostMortem: true,
    environment: 'production',
  },
  {
    id: 'mat-002',
    serviceName: 'legacy-billing',
    teamName: 'Finance',
    score: 28,
    maturityLevel: 'initial' as const,
    hasOnCall: false,
    hasRunbook: false,
    hasSlo: false,
    hasDashboard: true,
    hasAlerts: true,
    hasProfiling: false,
    hasRecentPostMortem: false,
    environment: 'production',
  },
];

beforeEach(() => {
  vi.mocked(getServiceMaturities).mockResolvedValue(mockMaturities);
});

describe('ServiceMaturitySrePage', () => {
  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Service Maturity Score (SRE)')).toBeTruthy();
    });
  });

  it('renders service names', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('order-service')).toBeTruthy();
      expect(screen.getByText('legacy-billing')).toBeTruthy();
    });
  });

  it('renders team names', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Core Commerce')).toBeTruthy();
    });
  });

  it('renders advanced maturity badge', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('Advanced').length).toBeGreaterThan(0);
    });
  });

  it('renders initial maturity badge', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('Initial').length).toBeGreaterThan(0);
    });
  });

  it('renders overall score', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('82')).toBeTruthy();
    });
  });

  it('renders Advanced count hero stat', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('Advanced').length).toBeGreaterThan(0);
    });
  });

  it('shows fallback data when API returns empty array', async () => {
    vi.mocked(getServiceMaturities).mockResolvedValueOnce([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByRole('row').length).toBeGreaterThan(0);
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(getServiceMaturities).mockRejectedValue(new Error('network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Failed to load/i)).toBeTruthy();
    });
  });

  it('renders Avg Score hero stat', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Avg Score')).toBeTruthy();
    });
  });
});
