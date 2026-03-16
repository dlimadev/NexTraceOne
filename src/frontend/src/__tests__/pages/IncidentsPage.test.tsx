import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { IncidentsPage } from '../../features/operations/pages/IncidentsPage';

vi.mock('../../features/operations/api/incidents', () => ({
  incidentsApi: {
    listIncidents: vi.fn(),
    getIncidentSummary: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));

import { incidentsApi } from '../../features/operations/api/incidents';

const mockSummary = {
  totalActive: 12,
  totalResolved: 45,
  bySeverity: { Critical: 2, High: 4, Medium: 3, Low: 3 },
  byStatus: { Open: 8, Investigating: 2, Mitigating: 1, Resolved: 1 },
};

const mockIncidentsList = {
  items: [
    {
      incidentId: 'inc-1',
      reference: 'INC-1042',
      title: 'Payment gateway timeout errors',
      incidentType: 'Performance',
      severity: 'High',
      status: 'Open',
      serviceId: 'svc-payment',
      serviceDisplayName: 'Payment Gateway',
      ownerTeam: 'payments-squad',
      environment: 'production',
      createdAt: '2026-03-15T10:00:00Z',
      hasCorrelatedChanges: true,
      correlationConfidence: 'High',
      mitigationStatus: 'InProgress',
    },
    {
      incidentId: 'inc-2',
      reference: 'INC-1043',
      title: 'Catalog search degradation',
      incidentType: 'Performance',
      severity: 'Medium',
      status: 'Investigating',
      serviceId: 'svc-catalog',
      serviceDisplayName: 'Catalog Service',
      ownerTeam: 'catalog-squad',
      environment: 'production',
      createdAt: '2026-03-15T11:00:00Z',
      hasCorrelatedChanges: false,
      correlationConfidence: 'Low',
      mitigationStatus: 'Pending',
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 50,
};

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/operations/incidents']}>
        <Routes>
          <Route path="/operations/incidents" element={<IncidentsPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('IncidentsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(incidentsApi.listIncidents).mockReturnValue(new Promise(() => {}));
    vi.mocked(incidentsApi.getIncidentSummary).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it('renders incidents list after loading', async () => {
    vi.mocked(incidentsApi.listIncidents).mockResolvedValue(mockIncidentsList);
    vi.mocked(incidentsApi.getIncidentSummary).mockResolvedValue(mockSummary);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('INC-1042')).toBeInTheDocument();
    });
    expect(screen.getByText('INC-1043')).toBeInTheDocument();
  });

  it('renders page title', async () => {
    vi.mocked(incidentsApi.listIncidents).mockResolvedValue(mockIncidentsList);
    vi.mocked(incidentsApi.getIncidentSummary).mockResolvedValue(mockSummary);
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText(/incident/i).length).toBeGreaterThanOrEqual(1);
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(incidentsApi.listIncidents).mockRejectedValue(new Error('Server error'));
    vi.mocked(incidentsApi.getIncidentSummary).mockRejectedValue(new Error('Server error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/error/i)).toBeInTheDocument();
    });
  });

  it('shows no results when incidents list is empty', async () => {
    vi.mocked(incidentsApi.listIncidents).mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 50 });
    vi.mocked(incidentsApi.getIncidentSummary).mockResolvedValue(mockSummary);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/no results/i)).toBeInTheDocument();
    });
  });
});
