import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';

vi.mock('../../features/integrations/api/integrations', () => ({
  integrationsApi: {
    getConnector: vi.fn(),
    retryConnector: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { integrationsApi } from '../../features/integrations/api/integrations';
import { ConnectorDetailPage } from '../../features/integrations/pages/ConnectorDetailPage';

const mockConnector = {
  connectorId: 'conn-1',
  name: 'Azure DevOps Connector',
  description: 'Syncs data from Azure DevOps',
  provider: 'AzureDevOps',
  connectorType: 'CI/CD',
  status: 'Active',
  environment: 'Production',
  healthScore: 92,
  lastSyncAt: '2025-06-01T12:00:00Z',
  lastSuccessAt: '2025-06-01T12:00:00Z',
  itemsSynced: 1250,
  recentExecutions: [
    {
      executionId: 'exec-1',
      startedAt: '2025-06-01T12:00:00Z',
      finishedAt: '2025-06-01T12:05:00Z',
      result: 'Success',
      recordsProcessed: 150,
      errors: 0,
    },
  ],
  configuration: {
    endpoint: 'https://dev.azure.com/org',
    authMode: 'PAT',
    pollingMode: 'Scheduled',
    retryPolicy: '3x exponential',
    enabled: true,
    allowedDomains: ['Commerce', 'Identity'],
    sourceScope: 'Organization',
  },
  dataDomains: ['CI/CD', 'WorkItems'],
  sources: [],
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/integrations/connectors/conn-1']}>
        <Routes>
          <Route path="/integrations/connectors/:connectorId" element={<ConnectorDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ConnectorDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(integrationsApi.getConnector).mockResolvedValue(mockConnector);
  });

  it('renders connector name from API response', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Azure DevOps Connector')).toBeInTheDocument();
    });
  });

  it('calls integrationsApi.getConnector with correct id', async () => {
    renderPage();
    await waitFor(() => expect(integrationsApi.getConnector).toHaveBeenCalledWith('conn-1'));
  });

  it('shows loading state while fetching', () => {
    vi.mocked(integrationsApi.getConnector).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Azure DevOps Connector')).not.toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(integrationsApi.getConnector).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => expect(screen.queryByText('Azure DevOps Connector')).not.toBeInTheDocument());
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Azure DevOps Connector'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });
});
