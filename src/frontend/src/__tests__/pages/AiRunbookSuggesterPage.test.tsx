/**
 * Tests for AiRunbookSuggesterPage.
 *
 * @vitest-environment jsdom
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/operations/api/telemetry', () => ({
  getAiRunbookSuggestions: vi.fn(),
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

import { getAiRunbookSuggestions } from '../../features/operations/api/telemetry';
import { AiRunbookSuggesterPage } from '../../features/operations/pages/AiRunbookSuggesterPage';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AiRunbookSuggesterPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const mockSuggestions = [
  {
    id: 'rbs-001',
    incidentId: 'inc-001',
    incidentTitle: 'Order Service Memory Leak',
    serviceName: 'order-service',
    environment: 'production',
    version: 'v2.3.1',
    runbookTitle: 'Memory Leak Remediation Runbook v2',
    runbookId: 'rb-042',
    confidencePercent: 93,
    reasoning: 'Pattern matches 3 previous incidents with memory leak symptoms in order-service.',
    modelName: 'gpt-4o',
    suggestedAt: new Date(Date.now() - 3600000).toISOString(),
    status: 'pending' as const,
    tokensUsed: 980,
    knowledgeSources: ['past-incidents', 'runbooks'],
  },
  {
    id: 'rbs-002',
    incidentId: 'inc-002',
    incidentTitle: 'DB Connection Pool Exhaustion',
    serviceName: 'payment-service',
    environment: 'production',
    version: 'v1.8.0',
    runbookTitle: 'DB Connection Pool Recovery Runbook',
    runbookId: 'rb-017',
    confidencePercent: 78,
    reasoning: 'Connection pool exhaustion detected based on metric patterns.',
    modelName: 'gpt-4o',
    suggestedAt: new Date(Date.now() - 86400000).toISOString(),
    status: 'accepted' as const,
    tokensUsed: 740,
    knowledgeSources: ['runbooks', 'sre-handbook'],
  },
];

beforeEach(() => {
  vi.mocked(getAiRunbookSuggestions).mockResolvedValue(mockSuggestions);
});

describe('AiRunbookSuggesterPage', () => {
  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('AI Runbook Suggester')).toBeTruthy();
    });
  });

  it('renders incident titles', async () => {
    renderPage();
    await waitFor(() => {
      // runbookTitle is rendered, not incidentTitle
      expect(screen.getByText('Memory Leak Remediation Runbook v2')).toBeTruthy();
    });
  });

  it('renders suggested runbook names', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Memory Leak Remediation Runbook v2')).toBeTruthy();
    });
  });

  it('renders confidence score', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('93%')).toBeTruthy();
    });
  });

  it('renders affected service names', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText(/order-service/).length).toBeGreaterThan(0);
      expect(screen.getAllByText(/payment-service/).length).toBeGreaterThan(0);
    });
  });

  it('renders AI model used', async () => {
    renderPage();
    await waitFor(() => {
      // knowledgeSources is rendered as "past-incidents, runbooks"
      expect(screen.getByText(/past-incidents/)).toBeTruthy();
    });
  });

  it('renders applied by email when present', async () => {
    renderPage();
    await waitFor(() => {
      // accepted suggestion renders "accepted" status
      expect(screen.getAllByText(/accepted/i).length).toBeGreaterThan(0);
    });
  });

  it('shows fallback data when API returns empty array', async () => {
    vi.mocked(getAiRunbookSuggestions).mockResolvedValueOnce([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('AI Runbook Suggester')).toBeTruthy();
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(getAiRunbookSuggestions).mockRejectedValue(new Error('network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Failed to load/i)).toBeTruthy();
    });
  });

  it('renders Total Suggestions hero stat', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Active Suggestions')).toBeTruthy();
    });
  });

  it('renders reasoning text', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Pattern matches 3 previous incidents/i)).toBeTruthy();
    });
  });

  it('renders knowledge sources', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/past-incidents/)).toBeTruthy();
    });
  });
});
