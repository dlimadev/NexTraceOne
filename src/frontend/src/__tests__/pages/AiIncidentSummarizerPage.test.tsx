/**
 * Tests for AiIncidentSummarizerPage.
 *
 * @vitest-environment jsdom
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/operations/api/telemetry', () => ({
  getAiIncidentSummaries: vi.fn(),
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

import { getAiIncidentSummaries } from '../../features/operations/api/telemetry';
import { AiIncidentSummarizerPage } from '../../features/operations/pages/AiIncidentSummarizerPage';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AiIncidentSummarizerPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const mockSummaries = [
  {
    id: 'ais-001',
    incidentId: 'inc-001',
    incidentTitle: 'Order Service P1 Outage',
    severity: 'critical',
    serviceName: 'order-service',
    summaryText: 'Root cause identified as memory leak in v2.3.1 deploy. Rollback restored service at 14:42 UTC.',
    generatedAt: new Date(Date.now() - 3600000).toISOString(),
    modelName: 'gpt-4o',
    confidencePercent: 91,
    tokensUsed: 1842,
    requestedBy: 'tech-lead@example.com',
    environment: 'production',
  },
  {
    id: 'ais-002',
    incidentId: 'inc-002',
    incidentTitle: 'Payment Latency Spike',
    severity: 'high',
    serviceName: 'payment-service',
    summaryText: 'Latency spike caused by upstream database connection pool exhaustion.',
    generatedAt: new Date(Date.now() - 86400000).toISOString(),
    modelName: 'gpt-4o',
    confidencePercent: 85,
    tokensUsed: 1120,
    requestedBy: 'sre@example.com',
    environment: 'production',
  },
];

beforeEach(() => {
  vi.mocked(getAiIncidentSummaries).mockResolvedValue(mockSummaries);
});

describe('AiIncidentSummarizerPage', () => {
  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('AI Incident Summarizer')).toBeTruthy();
    });
  });

  it('renders incident titles', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Order Service P1 Outage')).toBeTruthy();
      expect(screen.getByText('Payment Latency Spike')).toBeTruthy();
    });
  });

  it('renders severity badge', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('critical')).toBeTruthy();
    });
  });

  it('renders affected service name', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/order-service/)).toBeTruthy();
    });
  });

  it('renders AI confidence percentage', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/91%/)).toBeTruthy();
    });
  });

  it('renders AI model used', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText(/gpt-4o/).length).toBeGreaterThan(0);
    });
  });

  it('renders post-mortem draft available indicator', async () => {
    renderPage();
    await waitFor(() => {
      // summaryText rendered
      expect(screen.getByText(/Root cause identified/i)).toBeTruthy();
    });
  });

  it('renders tokens used', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/1,842/)).toBeTruthy();
    });
  });

  it('renders requested by email', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/tech-lead@example.com/)).toBeTruthy();
    });
  });

  it('renders summary text', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Root cause identified as memory leak/i)).toBeTruthy();
    });
  });
});
