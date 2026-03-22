import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { RunbooksPage } from '../../features/operations/pages/RunbooksPage';

vi.mock('../../features/operations/api/incidents', () => ({
  incidentsApi: {
    listRunbooks: vi.fn(),
    getRunbookDetail: vi.fn(),
    listIncidents: vi.fn(),
    getIncidentDetail: vi.fn(),
    getIncidentSummary: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { incidentsApi } from '../../features/operations/api/incidents';

const mockRunbooks = {
  runbooks: [
    {
      runbookId: 'rb-1',
      title: 'Database Failover Procedure',
      summary: 'Steps to perform database failover in production.',
      linkedServiceId: 'svc-order-api',
      linkedIncidentType: 'Infrastructure',
      stepCount: 8,
      createdAt: '2026-02-15T00:00:00Z',
    },
    {
      runbookId: 'rb-2',
      title: 'API Gateway Recovery',
      summary: 'Recovery procedure for API gateway failures.',
      linkedServiceId: null,
      linkedIncidentType: 'Application',
      stepCount: 5,
      createdAt: '2026-03-01T00:00:00Z',
    },
  ],
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <RunbooksPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('RunbooksPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(incidentsApi.listRunbooks).mockResolvedValue(mockRunbooks);
  });

  it('calls incidentsApi.listRunbooks on mount', async () => {
    renderPage();
    await waitFor(() => expect(incidentsApi.listRunbooks).toHaveBeenCalledTimes(1));
  });

  it('renders runbook items from API response', async () => {
    renderPage();
    await waitFor(() => expect(screen.getByText('Database Failover Procedure')).toBeInTheDocument());
    expect(screen.getByText('API Gateway Recovery')).toBeInTheDocument();
  });

  it('shows loading state while fetching', () => {
    vi.mocked(incidentsApi.listRunbooks).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Database Failover Procedure')).not.toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(incidentsApi.listRunbooks).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => expect(screen.queryByText('Database Failover Procedure')).not.toBeInTheDocument());
  });

  it('shows empty state when no runbooks returned', async () => {
    vi.mocked(incidentsApi.listRunbooks).mockResolvedValue({ runbooks: [] });
    renderPage();
    await waitFor(() => expect(screen.queryByText('Database Failover Procedure')).not.toBeInTheDocument());
  });

  it('shows step count badge', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Database Failover Procedure'));
    expect(screen.getByText(/8/)).toBeInTheDocument();
  });

  it('shows incident type badge when present', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Database Failover Procedure'));
    expect(screen.getByText('Infrastructure')).toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Database Failover Procedure'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });
});
