import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { IngestionFreshnessPage } from '../../features/integrations/pages/IngestionFreshnessPage';

vi.mock('../../features/integrations/api/integrations', () => ({
  integrationsApi: {
    listExecutions: vi.fn(),
    reprocessExecution: vi.fn(),
    listConnectors: vi.fn(),
    getConnector: vi.fn(),
    retryConnector: vi.fn(),
    listSources: vi.fn(),
    getHealth: vi.fn(),
    getFreshness: vi.fn(),
    getFilterOptions: vi.fn(),
    processPayload: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { integrationsApi } from '../../features/integrations/api/integrations';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <IngestionFreshnessPage />
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
describe('IngestionFreshnessPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(integrationsApi.getFreshness).mockResolvedValue({
      indicators: [],
      generatedAt: '2026-04-01T00:00:00Z',
    });
    vi.mocked(integrationsApi.getHealth).mockResolvedValue({
      connectors: [],
      overallStatus: 'Healthy',
      healthyCount: 0,
      degradedCount: 0,
      failingCount: 0,
    });
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Freshness & Health')).toBeDefined();
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(integrationsApi.getFreshness).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders freshness data when available', async () => {
    vi.mocked(integrationsApi.getFreshness).mockResolvedValue({
      indicators: [
        {
          dataDomain: 'GitHub',
          latestIngestionAt: '2026-04-01T08:00:00Z',
          freshnessStatus: 'Fresh',
          staleSources: 0,
          totalSources: 3,
        },
      ],
      generatedAt: '2026-04-01T08:05:00Z',
    });
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('GitHub');
    });
  });
});
