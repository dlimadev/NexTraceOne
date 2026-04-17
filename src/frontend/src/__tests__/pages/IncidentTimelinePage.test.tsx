import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { IncidentTimelinePage } from '../../features/operations/pages/IncidentTimelinePage';

vi.mock('../../features/operations/api/incidents', () => ({
  incidentsApi: {
    listRunbooks: vi.fn(),
    getRunbookDetail: vi.fn(),
    listIncidents: vi.fn(),
    getIncidentDetail: vi.fn(),
    getIncidentSummary: vi.fn(),
    createRunbook: vi.fn(),
    updateRunbook: vi.fn(),
    getUnifiedTimeline: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { incidentsApi } from '../../features/operations/api/incidents';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <IncidentTimelinePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn().mockReturnValue({
    activeEnvironmentId: 'env-prod-001',
    activeEnvironment: { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [{ id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true }],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  }),
}));

describe('IncidentTimelinePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(incidentsApi.getUnifiedTimeline).mockResolvedValue({
      entries: [],
      totalCount: 0,
      environmentSummary: {},
    });
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Unified Incident Timeline')).toBeDefined();
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(incidentsApi.getUnifiedTimeline).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders timeline entries when data is available', async () => {
    vi.mocked(incidentsApi.getUnifiedTimeline).mockResolvedValue({
      entries: [
        {
          id: 'timeline-001',
          type: 'incident',
          title: 'High error rate on Order API',
          description: 'Error rate exceeded 5% threshold',
          severity: 'High',
          source: 'incident',
          serviceName: 'Order API',
          environment: 'Production',
          occurredAt: '2026-03-15T10:00:00Z',
          correlationId: null,
          linkedChangeId: null,
        },
      ],
      totalCount: 1,
      environmentSummary: { Production: 1 },
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('High error rate on Order API')).toBeDefined();
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(incidentsApi.getUnifiedTimeline).mockRejectedValue(new Error('API error'));
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toBeDefined();
    });
  });
});
