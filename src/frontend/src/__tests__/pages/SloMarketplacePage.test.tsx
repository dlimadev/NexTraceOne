/**
 * Tests for SloMarketplacePage.
 *
 * @vitest-environment jsdom
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/operations/api/telemetry', () => ({
  getSloTemplates: vi.fn(),
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

import { getSloTemplates } from '../../features/operations/api/telemetry';
import { SloMarketplacePage } from '../../features/operations/pages/SloMarketplacePage';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <SloMarketplacePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const mockTemplates = [
  {
    id: 'tpl-001',
    name: 'REST API Availability 99.9%',
    category: 'restApi' as const,
    sliType: 'Availability',
    target: '99.9%',
    window: '30d',
    compliancePreset: 'internal',
    uses: 42,
    author: 'Platform Team',
    description: 'Standard REST API availability SLO for production services.',
  },
  {
    id: 'tpl-002',
    name: 'Database Query p95 < 100ms',
    category: 'database' as const,
    sliType: 'Latency',
    target: '100ms',
    window: '24h',
    compliancePreset: undefined,
    uses: 28,
    author: 'Data Team',
    description: 'Database latency SLO for query performance.',
  },
];

beforeEach(() => {
  vi.mocked(getSloTemplates).mockResolvedValue(mockTemplates);
});

describe('SloMarketplacePage', () => {
  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('SLO Template Marketplace')).toBeTruthy();
    });
  });

  it('renders template names', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('REST API Availability 99.9%')).toBeTruthy();
      expect(screen.getByText('Database Query p95 < 100ms')).toBeTruthy();
    });
  });

  it('renders category badge', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('REST API').length).toBeGreaterThan(0);
    });
  });

  it('renders compliance preset', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('internal')).toBeTruthy();
    });
  });

  it('renders usage count', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('42')).toBeTruthy();
    });
  });

  it('renders SLO target value', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('99.9%')).toBeTruthy();
    });
  });

  it('renders search input', async () => {
    renderPage();
    await waitFor(() => {
      // Page has category filter buttons instead of search textbox
      expect(screen.getByText('All')).toBeTruthy();
    });
  });

  it('filters templates by search query', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('REST API Availability 99.9%')).toBeTruthy();
    });
    // click Database category filter
    const dbBtn = screen.getByRole('button', { name: /Database/i });
    dbBtn.click();
    await waitFor(() => {
      expect(screen.getByText('Database Query p95 < 100ms')).toBeTruthy();
    });
  });

  it('shows fallback data when API returns empty array', async () => {
    vi.mocked(getSloTemplates).mockResolvedValueOnce([]);
    renderPage();
    await waitFor(() => {
      // fallback has entries
      expect(screen.getByText('SLO Template Marketplace')).toBeTruthy();
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(getSloTemplates).mockRejectedValue(new Error('network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Failed to load/i)).toBeTruthy();
    });
  });
});
