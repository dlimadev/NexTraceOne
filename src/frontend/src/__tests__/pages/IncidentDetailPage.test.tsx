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
    description: 'Elevated timeout rates on payment processing API',
    severity: 'High',
    status: 'Open',
    environment: 'production',
    domain: 'Payments',
    teamName: 'payments-squad',
    correlatedChangeId: 'chg-1',
    correlatedChangeDescription: 'Deployment v2.4.1',
    confidenceLevel: 0.87,
    mitigationStatus: 'InProgress',
    createdAt: '2026-03-15T10:00:00Z',
    updatedAt: '2026-03-15T12:00:00Z',
    resolvedAt: null,
  },
  linkedServices: [
    { serviceId: 'svc-payment', displayName: 'Payment Gateway', serviceType: 'Backend', criticality: 'Critical' },
  ],
  ownerTeam: 'payments-squad',
  impactedDomain: 'Payments',
  impactedEnvironment: 'production',
  timeline: [
    { timestamp: '2026-03-15T10:00:00Z', type: 'Created', description: 'Incident created by monitoring alert' },
    { timestamp: '2026-03-15T10:15:00Z', type: 'Updated', description: 'Severity escalated to High' },
  ],
  correlation: {
    confidence: 'High',
    reason: 'Deployment detected within blast radius',
    relatedChanges: [
      { changeId: 'chg-1', description: 'Deployment payment-gateway v2.4.1', changeType: 'Deployment', confidenceStatus: 'High', deployedAt: '2026-03-15T09:45:00Z' },
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
    { runbookId: 'rb-1', title: 'Payment Gateway Recovery', category: 'Recovery', lastUpdated: '2026-02-01' },
  ],
  relatedContracts: [
    { contractVersionId: 'cv-pay-1', name: 'payments-api', version: 'v2.0', protocol: 'OpenApi', lifecycleState: 'Approved' },
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

  it('displays evidence observations', async () => {
    vi.mocked(incidentsApi.getIncidentDetail).mockResolvedValue(mockIncident);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/timeout errors concentrated/i)).toBeInTheDocument();
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(incidentsApi.getIncidentDetail).mockRejectedValue(new Error('Not found'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/not found/i)).toBeInTheDocument();
    });
  });

  it('shows back to list link', async () => {
    vi.mocked(incidentsApi.getIncidentDetail).mockResolvedValue(mockIncident);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('INC-1042')).toBeInTheDocument();
    });
    const backLinks = screen.getAllByRole('link').filter(link => link.getAttribute('href')?.includes('/operations/incidents'));
    expect(backLinks.length).toBeGreaterThanOrEqual(1);
  });
});
