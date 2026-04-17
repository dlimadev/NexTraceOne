import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { IngestionExecutionsPage } from '../../features/integrations/pages/IngestionExecutionsPage';

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
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { integrationsApi } from '../../features/integrations/api/integrations';

const mockExecutions = {
  executions: [
    {
      executionId: 'ex-1',
      connectorId: 'conn-jira',
      connectorName: 'Jira',
      sourceId: 'src-1',
      sourceName: 'Jira Cloud',
      result: 'Success',
      recordsProcessed: 142,
      recordsFailed: 0,
      startedAt: '2026-03-20T10:00:00Z',
      completedAt: '2026-03-20T10:02:30Z',
      durationMs: 150000,
      errorMessage: null,
    },
    {
      executionId: 'ex-2',
      connectorId: 'conn-github',
      connectorName: 'GitHub',
      sourceId: 'src-2',
      sourceName: 'GitHub Enterprise',
      result: 'Failed',
      recordsProcessed: 0,
      recordsFailed: 3,
      startedAt: '2026-03-20T11:00:00Z',
      completedAt: '2026-03-20T11:01:00Z',
      durationMs: 60000,
      errorMessage: 'Connection timeout',
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 50,
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <IngestionExecutionsPage />
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
describe('IngestionExecutionsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(integrationsApi.listExecutions).mockResolvedValue(mockExecutions);
  });

  it('calls integrationsApi.listExecutions on mount', async () => {
    renderPage();
    await waitFor(() => expect(integrationsApi.listExecutions).toHaveBeenCalledTimes(1));
  });

  it('renders execution items from API response', async () => {
    renderPage();
    await waitFor(() => {
      const jiraElements = screen.getAllByText('Jira');
      expect(jiraElements.length).toBeGreaterThan(0);
    });
    const githubElements = screen.getAllByText('GitHub');
    expect(githubElements.length).toBeGreaterThan(0);
  });

  it('shows result badges', async () => {
    renderPage();
    await waitFor(() => screen.getAllByText('Jira'));
    const successBadges = screen.getAllByText('Success');
    expect(successBadges.length).toBeGreaterThan(0);
    const failedBadges = screen.getAllByText('Failed');
    expect(failedBadges.length).toBeGreaterThan(0);
  });

  it('shows loading state while fetching', () => {
    vi.mocked(integrationsApi.listExecutions).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Jira')).not.toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(integrationsApi.listExecutions).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => expect(screen.queryByText('Jira')).not.toBeInTheDocument());
  });

  it('shows empty state when no executions', async () => {
    vi.mocked(integrationsApi.listExecutions).mockResolvedValue({
      executions: [], totalCount: 0, page: 1, pageSize: 50,
    });
    renderPage();
    await waitFor(() => expect(screen.queryByText('Jira')).not.toBeInTheDocument());
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getAllByText('Jira'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });
});
