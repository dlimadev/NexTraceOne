/**
 * Tests for RequestExplorerPage.
 *
 * @vitest-environment jsdom
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/operations/api/telemetry', () => ({
  getRequests: vi.fn(),
  getRequestFacets: vi.fn(),
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

import { getRequests, getRequestFacets } from '../../features/operations/api/telemetry';
import { RequestExplorerPage } from '../../features/operations/pages/RequestExplorerPage';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <RequestExplorerPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const mockFacets = {
  services: ['astroshop-frontend', 'AdService', 'CartService'],
  endpoints: ['/api/checkout', '/api/cart', '/api/product'],
  processGroups: ['frontend-599f966557-zklkh'],
  k8sNamespaces: ['astroshop'],
  k8sWorkloads: ['frontend'],
};

const mockHistogram = [
  { durationLabel: '8.89 ms', successCount: 15, failureCount: 3 },
  { durationLabel: '24.15 ms', successCount: 20, failureCount: 5 },
  { durationLabel: '65.66 ms', successCount: 10, failureCount: 8 },
];

const mockItems = [
  {
    startTime: '2026-01-06T20:35:48.247Z',
    endpoint: '/api/checkout',
    service: 'astroshop-frontend',
    durationMs: 188.83,
    requestStatus: 'Failure' as const,
    httpCode: 500,
    processGroup: 'frontend-599f966557-zklkh',
    k8sWorkload: 'frontend',
    k8sNamespace: 'astroshop',
  },
  {
    startTime: '2026-01-06T20:35:45.860Z',
    endpoint: '/api/checkout',
    service: 'astroshop-frontend',
    durationMs: 15.59,
    requestStatus: 'Success' as const,
    httpCode: 200,
    processGroup: 'frontend-599f966557-zklkh',
    k8sWorkload: 'frontend',
    k8sNamespace: 'astroshop',
  },
];

const mockResult = {
  items: mockItems,
  total: 458,
  page: 1,
  pageSize: 20,
  histogram: mockHistogram,
};

beforeEach(() => {
  vi.mocked(getRequests).mockResolvedValue(mockResult);
  vi.mocked(getRequestFacets).mockResolvedValue(mockFacets);
});

describe('RequestExplorerPage', () => {
  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Request Explorer')).toBeTruthy();
    });
  });

  it('renders Requests and Spans view toggle buttons', async () => {
    renderPage();
    await waitFor(() => {
      const requestBtns = screen.getAllByText('Requests');
      expect(requestBtns.length).toBeGreaterThan(0);
      const spansBtns = screen.getAllByText('Spans');
      expect(spansBtns.length).toBeGreaterThan(0);
    });
  });

  it('renders time range selector with default 24h', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Last 24 hours')).toBeTruthy();
    });
  });

  it('opens time range dropdown and allows selection', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('Last 24 hours').length).toBeGreaterThan(0);
    });
    fireEvent.click(screen.getAllByText('Last 24 hours')[0]);
    await waitFor(() => {
      expect(screen.getByText('Last 1 hour')).toBeTruthy();
    });
    fireEvent.click(screen.getByText('Last 1 hour'));
    await waitFor(() => {
      // After selection, "Last 1 hour" should appear in the trigger button
      expect(screen.getAllByText('Last 1 hour').length).toBeGreaterThan(0);
    });
  });

  it('renders facet panel sections', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('Core').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Service').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Endpoint').length).toBeGreaterThan(0);
    });
  });

  it('renders service facets from API', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('astroshop-frontend').length).toBeGreaterThan(0);
      expect(screen.getAllByText('AdService').length).toBeGreaterThan(0);
    });
  });

  it('renders request table columns', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('Start time').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Endpoint').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Service').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Duration').length).toBeGreaterThan(0);
      expect(screen.getAllByText('HTTP').length).toBeGreaterThan(0);
    });
  });

  it('renders request rows with service name', async () => {
    renderPage();
    await waitFor(() => {
      const cells = screen.getAllByText('astroshop-frontend');
      expect(cells.length).toBeGreaterThan(0);
    });
  });

  it('renders Success and Failure status badges', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('Failure').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Success').length).toBeGreaterThan(0);
    });
  });

  it('renders histogram chart toggle buttons', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Timeseries')).toBeTruthy();
      expect(screen.getByText('Histogram')).toBeTruthy();
    });
  });

  it('renders active filter chip when service is selected', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('astroshop-frontend').length).toBeGreaterThan(0);
    });
    // Click the astroshop-frontend checkbox label
    const checkbox = screen.getAllByRole('checkbox')[0];
    fireEvent.click(checkbox);
    await waitFor(() => {
      expect(screen.getByText(/Service:.*astroshop-frontend/)).toBeTruthy();
    });
  });

  it('shows error state when query fails', async () => {
    vi.mocked(getRequests).mockRejectedValue(new Error('network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Failed to load requests. Please try again.')).toBeTruthy();
    });
  });
});
