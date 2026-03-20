import * as React from 'react';
import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { IncidentDetailPage } from '../../features/operations/pages/IncidentDetailPage';

beforeAll(() => {
  Element.prototype.scrollIntoView = vi.fn();
});

vi.mock('../../features/operations/api/incidents', () => ({
  incidentsApi: {
    getIncidentDetail: vi.fn(),
    refreshIncidentCorrelation: vi.fn(),
  },
}));

vi.mock('../../contexts/PersonaContext', () => ({
  usePersona: vi.fn().mockReturnValue({
    persona: 'Engineer',
    config: {
      aiContextScopes: ['services', 'contracts', 'incidents'],
      aiSuggestedPromptKeys: [],
      sectionOrder: [],
      highlightedSections: [],
      homeSubtitleKey: '',
      homeWidgets: [],
      quickActions: [],
    },
  }),
}));

vi.mock('../../features/ai-hub/api/aiGovernance', () => ({
  aiGovernanceApi: { sendMessage: vi.fn().mockRejectedValue(new Error('API not available')) },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));

import { incidentsApi } from '../../features/operations/api/incidents';

const mockIncident = {
  identity: {
    incidentId: '22222222-2222-2222-2222-222222222222',
    reference: 'INC-1042',
    title: 'Payment gateway timeout errors',
    summary: 'Elevated timeout rates on payment processing API',
    incidentType: 'ServiceDegradation',
    severity: 'Major',
    status: 'Open',
    createdAt: '2026-03-15T10:00:00Z',
    updatedAt: '2026-03-15T12:00:00Z',
  },
  linkedServices: [
    { serviceId: 'svc-payment', displayName: 'Payment Gateway', serviceType: 'Backend', criticality: 'Critical' },
  ],
  ownerTeam: 'payments-squad',
  impactedDomain: 'Payments',
  impactedEnvironment: 'Production',
  timeline: [
    { timestamp: '2026-03-15T10:00:00Z', description: 'Incident created by monitoring alert' },
    { timestamp: '2026-03-15T10:15:00Z', description: 'Severity escalated to Major' },
  ],
  correlation: {
    confidence: 'High',
    reason: 'Deployment detected within blast radius',
    relatedChanges: [
      { changeId: '11111111-1111-1111-1111-111111111111', description: 'Deployment payment-gateway v2.4.1', changeType: 'Deployment', confidenceStatus: 'HighEvidence', deployedAt: '2026-03-15T09:45:00Z' },
    ],
    relatedServices: [
      { serviceId: 'svc-payment', displayName: 'Payment Gateway', impactDescription: 'Direct impact on payment processing' },
    ],
  },
  evidence: {
    operationalSignalsSummary: 'Elevated P99 latency from 200ms to 1800ms',
    degradationSummary: 'Payment success rate dropped from 99.8% to 94.2%',
    observations: [
      { title: 'Timeout errors concentrated in EU region', description: 'Elevated timeout rates affecting EU customers' },
      { title: 'Database connection pool saturation', description: 'Connection pool saturation detected' },
    ],
  },
  mitigation: {
    status: 'InProgress',
    actions: [
      { description: 'Scale out payment-gateway pods', status: 'Pending', completed: false },
    ],
    rollbackGuidance: 'Rollback to v2.3.9 if scaling does not resolve within 30 minutes',
    rollbackRelevant: true,
    escalationGuidance: 'Escalate to platform team if issue persists after rollback',
  },
  runbooks: [
    { title: 'Payment Gateway Recovery', url: 'https://docs.internal/runbooks/payment-gateway-recovery' },
  ],
  relatedContracts: [
    { contractVersionId: '33333333-3333-3333-3333-333333333333', name: 'payments-api', version: 'v2.0', protocol: 'OpenApi', lifecycleState: 'Approved' },
  ],
};

function renderPage(incidentId = '22222222-2222-2222-2222-222222222222') {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/operations/incidents/${incidentId}`]}>
        <Routes>
          <Route path="/operations/incidents/:incidentId" element={<IncidentDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('IncidentDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(incidentsApi.refreshIncidentCorrelation).mockResolvedValue({
      incidentId: '22222222-2222-2222-2222-222222222222',
      confidence: 'High',
      score: 87,
      reason: 'Correlation refreshed',
      relatedChanges: [],
      relatedServices: [],
      relatedDependencies: [],
      possibleImpactedContracts: [],
    });
  });

  it('shows loading state', () => {
    vi.mocked(incidentsApi.getIncidentDetail).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it('renders incident details after loading', async () => {
    vi.mocked(incidentsApi.getIncidentDetail).mockResolvedValue(mockIncident);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('INC-1042')).toBeInTheDocument();
    });
    expect(screen.getAllByText(/payment gateway timeout/i).length).toBeGreaterThanOrEqual(1);
  });

  it('displays linked services', async () => {
    vi.mocked(incidentsApi.getIncidentDetail).mockResolvedValue(mockIncident);
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText('Payment Gateway').length).toBeGreaterThanOrEqual(1);
    });
  });

  it('displays correlated changes', async () => {
    vi.mocked(incidentsApi.getIncidentDetail).mockResolvedValue(mockIncident);
    renderPage();
    await waitFor(() => {
      expect(screen.getAllByText(/payment-gateway/).length).toBeGreaterThanOrEqual(1);
    });
  });

  it('displays related contracts', async () => {
    vi.mocked(incidentsApi.getIncidentDetail).mockResolvedValue(mockIncident);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/payments-api/)).toBeInTheDocument();
    });
  });

  it('shows honest correlation empty state when no changes are linked', async () => {
    vi.mocked(incidentsApi.getIncidentDetail).mockResolvedValue({
      ...mockIncident,
      correlation: {
        ...mockIncident.correlation,
        relatedChanges: [],
        relatedServices: [],
      },
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/no correlated changes were confirmed/i)).toBeInTheDocument();
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(incidentsApi.getIncidentDetail).mockRejectedValue(new Error('Not found'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/not found/i)).toBeInTheDocument();
    });
  });
});
