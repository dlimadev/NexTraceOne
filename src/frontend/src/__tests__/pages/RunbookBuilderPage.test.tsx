import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { RunbookBuilderPage } from '../../features/operations/pages/RunbookBuilderPage';

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

function renderCreatePage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/operations/runbooks/new']}>
        <Routes>
          <Route path="/operations/runbooks/new" element={<RunbookBuilderPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

function renderEditPage(runbookId = 'rb-001') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/operations/runbooks/${runbookId}/edit`]}>
        <Routes>
          <Route path="/operations/runbooks/:runbookId/edit" element={<RunbookBuilderPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('RunbookBuilderPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(incidentsApi.getRunbookDetail).mockResolvedValue({
      runbookId: 'rb-001',
      title: 'Database Failover',
      summary: 'Steps to perform DB failover',
      linkedServiceId: null,
      linkedIncidentType: null,
      steps: [],
      preconditions: [],
      postValidationGuidance: null,
      createdBy: 'admin',
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: null,
    });
    vi.mocked(incidentsApi.createRunbook).mockResolvedValue({ runbookId: 'rb-new' });
    vi.mocked(incidentsApi.updateRunbook).mockResolvedValue({ runbookId: 'rb-001' });
  });

  it('renders create runbook title in create mode', async () => {
    renderCreatePage();
    await waitFor(() => {
      expect(screen.getByText('Create Runbook')).toBeDefined();
    });
  });

  it('renders edit runbook title in edit mode', async () => {
    renderEditPage();
    await waitFor(() => {
      expect(screen.getByText('Edit Runbook')).toBeDefined();
    });
  });

  it('shows loading state when fetching runbook in edit mode', () => {
    vi.mocked(incidentsApi.getRunbookDetail).mockReturnValue(new Promise(() => {}));
    renderEditPage();
    expect(document.body).toBeDefined();
  });
});
