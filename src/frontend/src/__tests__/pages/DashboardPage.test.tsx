import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { DashboardPage } from '../../features/shared/pages/DashboardPage';

vi.mock('../../features/catalog/api/serviceCatalog', () => ({
  serviceCatalogApi: {
    getGraph: vi.fn(),
  },
}));

vi.mock('../../features/catalog/api/contracts', () => ({
  contractsApi: {
    getContractsSummary: vi.fn().mockResolvedValue({
      totalVersions: 12,
      distinctContracts: 5,
      draftCount: 3,
      inReviewCount: 2,
      approvedCount: 5,
      lockedCount: 1,
      deprecatedCount: 1,
      byProtocol: [],
    }),
  },
}));

vi.mock('../../features/change-governance/api/changeConfidence', () => ({
  changeConfidenceApi: {
    getSummary: vi.fn().mockResolvedValue({
      totalChanges: 24,
      validatedChanges: 18,
      changesNeedingAttention: 4,
      suspectedRegressions: 2,
      changesCorrelatedWithIncidents: 3,
    }),
  },
}));

vi.mock('../../features/operations/api/incidents', () => ({
  incidentsApi: {
    getIncidentSummary: vi.fn().mockResolvedValue({
      totalOpen: 5,
      criticalIncidents: 1,
      withCorrelatedChanges: 2,
      withMitigationAvailable: 3,
      servicesImpacted: 4,
      severityBreakdown: { critical: 1, major: 2, minor: 1, warning: 1 },
      statusBreakdown: { open: 3, investigating: 1, mitigating: 1, monitoring: 0, resolved: 8, closed: 4 },
    }),
  },
}));

// Mock usePersona to avoid needing AuthProvider + PersonaProvider
vi.mock('../../contexts/PersonaContext', () => ({
  usePersona: vi.fn(() => ({
    persona: 'Engineer',
    config: {
      homeSubtitleKey: 'persona.Engineer.homeSubtitle',
      homeWidgets: [],
      quickActions: [],
      navigationOrder: [],
      highlightedSections: [],
      sectionOrder: [],
      aiContextScope: [],
      aiSuggestedPrompts: [],
    },
  })),
}));

import { serviceCatalogApi } from '../../features/catalog/api/serviceCatalog';

function renderDashboard() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <DashboardPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('DashboardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('exibe o título do dashboard', () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue({
      services: [],
      apis: [],
    });
    renderDashboard();
    expect(screen.getByRole('heading', { name: /dashboard/i })).toBeInTheDocument();
  });

  it('exibe os stat cards com labels de fluxos core', () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue({
      services: [],
      apis: [],
    });
    renderDashboard();
    expect(screen.getByText('Active Services')).toBeInTheDocument();
    expect(screen.getByText('Total Contracts')).toBeInTheDocument();
    expect(screen.getByText('Recent Changes')).toBeInTheDocument();
    expect(screen.getByText('Open Incidents')).toBeInTheDocument();
    expect(screen.getByText('Registered APIs')).toBeInTheDocument();
  });

  it('exibe serviços carregados do grafo com links de navegação', async () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue({
      services: [
        { serviceAssetId: 's1', name: 'payments-service', teamName: 'Payments', domain: 'Payments', serviceType: 'RestApi', criticality: 'High', lifecycleStatus: 'Active' },
        { serviceAssetId: 's2', name: 'auth-service', teamName: 'Identity', domain: 'Identity', serviceType: 'RestApi', criticality: 'Critical', lifecycleStatus: 'Active' },
      ],
      apis: [],
    });
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText('payments-service')).toBeInTheDocument();
      expect(screen.getByText('auth-service')).toBeInTheDocument();
    });
  });

  it('exibe mensagem quando não há serviços registados', async () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue({
      services: [],
      apis: [],
    });
    renderDashboard();
    await waitFor(() => {
      expect(screen.getAllByText(/no services registered yet/i).length).toBeGreaterThan(0);
    });
  });

  it('exibe visão de saúde dos contratos com métricas reais', async () => {
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue({
      services: [],
      apis: [],
    });
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText('Contract Health')).toBeInTheDocument();
    });
  });
});
