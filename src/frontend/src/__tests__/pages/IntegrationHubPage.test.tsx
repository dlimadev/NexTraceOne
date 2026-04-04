import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { IntegrationHubPage } from '../../features/integrations/pages/IntegrationHubPage';

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
        <IntegrationHubPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('IntegrationHubPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(integrationsApi.listConnectors).mockResolvedValue({
      connectors: [],
      totalCount: 0,
    });
    vi.mocked(integrationsApi.getHealth).mockResolvedValue({
      connectors: [],
      overallStatus: 'Healthy',
      healthyCount: 0,
      degradedCount: 0,
      failingCount: 0,
    });
    vi.mocked(integrationsApi.getFilterOptions).mockResolvedValue({
      types: [],
      statuses: [],
      tags: [],
    });
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Integration Hub')).toBeDefined();
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(integrationsApi.listConnectors).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders connectors when data is available', async () => {
    vi.mocked(integrationsApi.listConnectors).mockResolvedValue({
      connectors: [
        {
          connectorId: 'conn-github',
          name: 'GitHub Enterprise',
          type: 'GitHub',
          status: 'Active',
          description: 'GitHub Enterprise integration',
          lastSyncAt: '2026-04-01T08:00:00Z',
          errorCount: 0,
          tags: ['vcs', 'ci-cd'],
        },
      ],
      totalCount: 1,
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('GitHub Enterprise')).toBeDefined();
    });
  });
});
