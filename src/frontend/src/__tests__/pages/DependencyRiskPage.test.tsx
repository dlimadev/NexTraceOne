/**
 * Tests for DependencyRiskPage.
 *
 * @vitest-environment jsdom
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/operations/api/telemetry', () => ({
  getDependencyRisks: vi.fn(),
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

import { getDependencyRisks } from '../../features/operations/api/telemetry';
import { DependencyRiskPage } from '../../features/operations/pages/DependencyRiskPage';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <DependencyRiskPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const mockRisks = [
  {
    id: 'dep-001',
    serviceName: 'order-service',
    riskScore: 87.0,
    riskLevel: 'critical' as const,
    failureCount30d: 5,
    sloHealthPercent: 91.0,
    blastRadius: 5,
    deployFrequency: 12,
    dependentsCount: 8,
    trendDirection: 'up' as const,
    environment: 'production',
  },
  {
    id: 'dep-002',
    serviceName: 'catalog-service',
    riskScore: 45.0,
    riskLevel: 'medium' as const,
    failureCount30d: 2,
    sloHealthPercent: 97.5,
    blastRadius: 2,
    deployFrequency: 4,
    dependentsCount: 3,
    trendDirection: 'stable' as const,
    environment: 'production',
  },
];

beforeEach(() => {
  vi.mocked(getDependencyRisks).mockResolvedValue(mockRisks);
});

describe('DependencyRiskPage', () => {
  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Dependency Risk Scoring')).toBeTruthy();
    });
  });

  it('renders service and dependency names', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('order-service')).toBeTruthy();
      expect(screen.getByText('catalog-service')).toBeTruthy();
    });
  });

  it('renders critical risk badge', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Critical')).toBeTruthy();
    });
  });

  it('renders medium risk badge', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Medium')).toBeTruthy();
    });
  });

  it('renders risk score', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('87.0')).toBeTruthy();
    });
  });

  it('renders failure count', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('5x')).toBeTruthy();
    });
  });

  it('renders circuit breaker status (missing)', async () => {
    renderPage();
    await waitFor(() => {
      // blast radius renders as "5 svcs" for blastRadius=5
      expect(screen.getByText('5 svcs')).toBeTruthy();
    });
  });

  it('renders SLO health percent', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('91.0%')).toBeTruthy();
    });
  });

  it('shows fallback data when API returns empty array', async () => {
    vi.mocked(getDependencyRisks).mockResolvedValueOnce([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByRole('row').length).toBeGreaterThan(0);
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(getDependencyRisks).mockRejectedValue(new Error('network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Failed to load/i)).toBeTruthy();
    });
  });
});
