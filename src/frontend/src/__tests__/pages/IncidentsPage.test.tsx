import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fireEvent, screen, waitFor } from '@testing-library/react';
import { Route, Routes } from 'react-router-dom';
import { renderWithProviders } from '../test-utils';
import { IncidentsPage } from '../../features/operations/pages/IncidentsPage';

vi.mock('../../features/operations/api/incidents', () => ({
  incidentsApi: {
    listIncidents: vi.fn(),
    getIncidentSummary: vi.fn(),
    createIncident: vi.fn(),
  },
}));

vi.mock('../../hooks/usePermissions', () => ({
  usePermissions: vi.fn(() => ({
    can: (permission: string) => permission === 'operations:incidents:write',
    roleName: 'PlatformAdmin',
    permissions: ['operations:incidents:read', 'operations:incidents:write'],
  }))
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));

import { incidentsApi } from '../../features/operations/api/incidents';
import { usePermissions } from '../../hooks/usePermissions';

const mockSummary = {
  totalOpen: 12,
  criticalIncidents: 2,
  withCorrelatedChanges: 4,
  withMitigationAvailable: 3,
  servicesImpacted: 5,
  severityBreakdown: { critical: 2, major: 4, minor: 3, warning: 3 },
  statusBreakdown: { open: 8, investigating: 2, mitigating: 1, monitoring: 1, resolved: 0, closed: 0 },
};

const mockIncidentsList = {
  items: [
    {
      incidentId: '22222222-2222-2222-2222-222222222221',
      reference: 'INC-1042',
      title: 'Payment gateway timeout errors',
      incidentType: 'ServiceDegradation',
      severity: 'Major',
      status: 'Open',
      serviceId: 'svc-payment',
      serviceDisplayName: 'Payment Gateway',
      ownerTeam: 'payments-squad',
      environment: 'Production',
      createdAt: '2026-03-15T10:00:00Z',
      hasCorrelatedChanges: true,
      correlationConfidence: 'High',
      mitigationStatus: 'InProgress',
    },
    {
      incidentId: '22222222-2222-2222-2222-222222222222',
      reference: 'INC-1043',
      title: 'Catalog search degradation',
      incidentType: 'OperationalRegression',
      severity: 'Minor',
      status: 'Investigating',
      serviceId: 'svc-catalog',
      serviceDisplayName: 'Catalog Service',
      ownerTeam: 'catalog-squad',
      environment: 'Production',
      createdAt: '2026-03-15T11:00:00Z',
      hasCorrelatedChanges: false,
      correlationConfidence: 'Low',
      mitigationStatus: 'NotStarted',
    },
  ],
  totalCount: 24,
  page: 1,
  pageSize: 20,
};

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route path="/operations/incidents" element={<IncidentsPage />} />
    </Routes>,
    { routerProps: { initialEntries: ['/operations/incidents'] } },
  );
}


vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn().mockReturnValue({
    activeEnvironmentId: 'env-prod-001',
    activeEnvironment: { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [
      { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
      { id: 'env-staging-001', name: 'Staging', profile: 'staging', isProductionLike: false },
    ],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  }),
}));

describe('IncidentsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(usePermissions).mockReturnValue({
      can: (permission: string) => permission === 'operations:incidents:write',
      roleName: 'PlatformAdmin',
      permissions: ['operations:incidents:read', 'operations:incidents:write'],
    });
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
    expect(screen.getByText(/page 1 of 2/i)).toBeInTheDocument();
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
    vi.mocked(incidentsApi.listIncidents).mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });
    vi.mocked(incidentsApi.getIncidentSummary).mockResolvedValue(mockSummary);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/no incidents found/i)).toBeInTheDocument();
    });
  });

  it('hides create action for read-only users', async () => {
    vi.mocked(usePermissions).mockReturnValue({
      can: () => false,
      roleName: 'Auditor',
      permissions: ['operations:incidents:read'],
    });
    vi.mocked(incidentsApi.listIncidents).mockResolvedValue(mockIncidentsList);
    vi.mocked(incidentsApi.getIncidentSummary).mockResolvedValue(mockSummary);

    renderPage();

    await waitFor(() => {
      expect(screen.getByText(/cannot create new ones/i)).toBeInTheDocument();
    });
    expect(screen.queryByRole('button', { name: /create incident/i })).not.toBeInTheDocument();
  });

  it('disables create submit until required fields are filled', async () => {
    vi.mocked(incidentsApi.listIncidents).mockResolvedValue(mockIncidentsList);
    vi.mocked(incidentsApi.getIncidentSummary).mockResolvedValue(mockSummary);
    renderPage();

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /create incident/i })).toBeInTheDocument();
    });

    fireEvent.click(screen.getByRole('button', { name: /create incident/i }));
    expect(screen.getByRole('button', { name: /^create$/i })).toBeDisabled();
  });
});
