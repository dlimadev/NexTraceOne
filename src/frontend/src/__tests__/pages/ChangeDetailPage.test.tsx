import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ChangeDetailPage } from '../../features/change-governance/pages/ChangeDetailPage';

beforeAll(() => {
  Element.prototype.scrollIntoView = vi.fn();
});

vi.mock('../../features/change-governance/api/changeConfidence', () => ({
  changeConfidenceApi: {
    getChange: vi.fn(),
    getIntelligence: vi.fn(),
    getAdvisory: vi.fn(),
    getDecisionHistory: vi.fn(),
    recordDecision: vi.fn(),
  },
}));

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: vi.fn().mockReturnValue({
    isAuthenticated: true,
    accessToken: 'test-token',
    user: { id: 'u1', email: 'test@example.com', fullName: 'Test User', roleName: 'Engineer', permissions: [] },
    tenantId: 'tenant-1',
    login: vi.fn(),
    logout: vi.fn(),
  }),
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

import { changeConfidenceApi } from '../../features/change-governance/api/changeConfidence';

const mockChange = {
  changeId: '11111111-1111-1111-1111-111111111111',
  apiAssetId: 'a1',
  serviceName: 'payments-service',
  version: '2.1.0',
  environment: 'prod',
  changeType: 'Deployment',
  deploymentStatus: 'Completed',
  changeLevel: 'Minor',
  confidenceStatus: 'Validated',
  validationStatus: 'Passed',
  changeScore: 0.25,
  teamName: 'Payments Team',
  domain: 'Payments',
  description: 'Add retry logic to payment processor',
  workItemReference: null,
  commitSha: 'abc123def456',
  createdAt: '2026-03-15T10:00:00Z',
};

const mockIntelligence = {
  release: mockChange,
  score: { score: 0.25, breakingChangeWeight: 0.1, blastRadiusWeight: 0.1, environmentWeight: 0.05, computedAt: '2026-03-15T10:00:00Z' },
  blastRadius: { totalAffected: 3, directConsumers: ['orders-service', 'billing-service'], transitiveConsumers: ['notifications-service'], calculatedAt: '2026-03-15T10:00:00Z' },
  markers: [],
  baseline: null,
  postReleaseReview: null,
  rollbackAssessment: null,
  timeline: [
    { timestamp: '2026-03-15T10:00:00Z', eventType: 'deployment_started', description: 'Deployment initiated' },
    { timestamp: '2026-03-15T10:05:00Z', eventType: 'deployment_completed', description: 'Deployment completed successfully' },
  ],
  validation: null,
};

const mockAdvisory = {
  releaseId: '11111111-1111-1111-1111-111111111111',
  recommendation: 'Approve' as const,
  rationale: 'All factors pass and the change score (0.25) is within safe thresholds. The change is recommended for approval.',
  overallConfidence: 0.75,
  factors: [
    { factorName: 'EvidenceCompleteness', status: 'Warning' as const, description: 'Some evidence sources are missing.', weight: 0.25 },
    { factorName: 'BlastRadiusScope', status: 'Pass' as const, description: 'Total affected consumers: 3.', weight: 0.25 },
    { factorName: 'ChangeScore', status: 'Pass' as const, description: 'Change score is 0.25.', weight: 0.25 },
    { factorName: 'RollbackReadiness', status: 'Unknown' as const, description: 'Rollback assessment has not been performed.', weight: 0.25 },
  ],
  generatedAt: '2026-03-15T10:10:00Z',
};

const mockDecisionHistory = {
  releaseId: '11111111-1111-1111-1111-111111111111',
  decisions: [
    {
      eventId: 'e1',
      eventType: 'governance_decision_approved',
      description: 'Decision: Approved. Rationale: Change looks safe for production.',
      source: 'john.doe',
      occurredAt: '2026-03-15T11:00:00Z',
    },
  ],
};

function renderChangeDetail(changeId = '11111111-1111-1111-1111-111111111111') {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/changes/${changeId}`]}>
        <Routes>
          <Route path="/changes/:changeId" element={<ChangeDetailPage />} />
        </Routes>
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

describe('ChangeDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('exibe estado de loading', () => {
    vi.mocked(changeConfidenceApi.getChange).mockReturnValue(new Promise(() => {}));
    vi.mocked(changeConfidenceApi.getIntelligence).mockReturnValue(new Promise(() => {}));
    vi.mocked(changeConfidenceApi.getAdvisory).mockReturnValue(new Promise(() => {}));
    vi.mocked(changeConfidenceApi.getDecisionHistory).mockReturnValue(new Promise(() => {}));
    renderChangeDetail();
    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it('exibe dados da mudança após carregamento', async () => {
    vi.mocked(changeConfidenceApi.getChange).mockResolvedValue(mockChange);
    vi.mocked(changeConfidenceApi.getIntelligence).mockResolvedValue(mockIntelligence);
    vi.mocked(changeConfidenceApi.getAdvisory).mockResolvedValue(mockAdvisory);
    vi.mocked(changeConfidenceApi.getDecisionHistory).mockResolvedValue(mockDecisionHistory);
    renderChangeDetail();
    await waitFor(() => {
      expect(screen.getAllByText('payments-service').length).toBeGreaterThanOrEqual(1);
    });
  });

  it('exibe painel de advisory com recomendação', async () => {
    vi.mocked(changeConfidenceApi.getChange).mockResolvedValue(mockChange);
    vi.mocked(changeConfidenceApi.getIntelligence).mockResolvedValue(mockIntelligence);
    vi.mocked(changeConfidenceApi.getAdvisory).mockResolvedValue(mockAdvisory);
    vi.mocked(changeConfidenceApi.getDecisionHistory).mockResolvedValue(mockDecisionHistory);
    renderChangeDetail();
    await waitFor(() => {
      expect(screen.getByText(/advisory/i)).toBeInTheDocument();
    });
    await waitFor(() => {
      expect(screen.getByText(/all factors pass/i)).toBeInTheDocument();
    });
  });

  it('exibe factores do advisory', async () => {
    vi.mocked(changeConfidenceApi.getChange).mockResolvedValue(mockChange);
    vi.mocked(changeConfidenceApi.getIntelligence).mockResolvedValue(mockIntelligence);
    vi.mocked(changeConfidenceApi.getAdvisory).mockResolvedValue(mockAdvisory);
    vi.mocked(changeConfidenceApi.getDecisionHistory).mockResolvedValue(mockDecisionHistory);
    renderChangeDetail();
    await waitFor(() => {
      expect(screen.getAllByText(/evidence completeness/i).length).toBeGreaterThanOrEqual(1);
      expect(screen.getAllByText(/blast radius scope/i).length).toBeGreaterThanOrEqual(1);
    });
  });

  it('exibe botões de decisão', async () => {
    vi.mocked(changeConfidenceApi.getChange).mockResolvedValue(mockChange);
    vi.mocked(changeConfidenceApi.getIntelligence).mockResolvedValue(mockIntelligence);
    vi.mocked(changeConfidenceApi.getAdvisory).mockResolvedValue(mockAdvisory);
    vi.mocked(changeConfidenceApi.getDecisionHistory).mockResolvedValue(mockDecisionHistory);
    renderChangeDetail();
    await waitFor(() => {
      const approveButtons = screen.getAllByText(/^approve$/i);
      expect(approveButtons.length).toBeGreaterThanOrEqual(1);
      const rejectButtons = screen.getAllByText(/^reject$/i);
      expect(rejectButtons.length).toBeGreaterThanOrEqual(1);
      expect(screen.getAllByText(/approve conditionally/i).length).toBeGreaterThanOrEqual(1);
    });
  });

  it('mostra formulário de rationale ao selecionar decisão', async () => {
    const user = userEvent.setup();
    vi.mocked(changeConfidenceApi.getChange).mockResolvedValue(mockChange);
    vi.mocked(changeConfidenceApi.getIntelligence).mockResolvedValue(mockIntelligence);
    vi.mocked(changeConfidenceApi.getAdvisory).mockResolvedValue(mockAdvisory);
    vi.mocked(changeConfidenceApi.getDecisionHistory).mockResolvedValue(mockDecisionHistory);
    renderChangeDetail();
    await waitFor(() => {
      expect(screen.getAllByText(/^approve$/i).length).toBeGreaterThanOrEqual(1);
    });
    // Click the Approve button (the button element, not a badge)
    const approveButtons = screen.getAllByRole('button').filter(
      btn => btn.textContent?.match(/^(.*)?approve(.*)?$/i) && !btn.textContent?.match(/conditionally/i)
    );
    expect(approveButtons.length).toBeGreaterThanOrEqual(1);
    await user.click(approveButtons[0]);
    await waitFor(() => {
      expect(screen.getByPlaceholderText(/explain the reason/i)).toBeInTheDocument();
    });
  });

  it('exibe histórico de decisões', async () => {
    vi.mocked(changeConfidenceApi.getChange).mockResolvedValue(mockChange);
    vi.mocked(changeConfidenceApi.getIntelligence).mockResolvedValue(mockIntelligence);
    vi.mocked(changeConfidenceApi.getAdvisory).mockResolvedValue(mockAdvisory);
    vi.mocked(changeConfidenceApi.getDecisionHistory).mockResolvedValue(mockDecisionHistory);
    renderChangeDetail();
    await waitFor(() => {
      expect(screen.getByText(/decision history/i)).toBeInTheDocument();
    });
    await waitFor(() => {
      expect(screen.getByText(/governance_decision_approved/i)).toBeInTheDocument();
    });
  });

  it('exibe blast radius consumers', async () => {
    vi.mocked(changeConfidenceApi.getChange).mockResolvedValue(mockChange);
    vi.mocked(changeConfidenceApi.getIntelligence).mockResolvedValue(mockIntelligence);
    vi.mocked(changeConfidenceApi.getAdvisory).mockResolvedValue(mockAdvisory);
    vi.mocked(changeConfidenceApi.getDecisionHistory).mockResolvedValue(mockDecisionHistory);
    renderChangeDetail();
    await waitFor(() => {
      expect(screen.getAllByText('orders-service').length).toBeGreaterThanOrEqual(1);
      expect(screen.getAllByText('billing-service').length).toBeGreaterThanOrEqual(1);
    });
  });

  it('exibe estado de erro quando mudança não é encontrada', async () => {
    vi.mocked(changeConfidenceApi.getChange).mockRejectedValue(new Error('Not found'));
    vi.mocked(changeConfidenceApi.getIntelligence).mockRejectedValue(new Error('Not found'));
    vi.mocked(changeConfidenceApi.getAdvisory).mockRejectedValue(new Error('Not found'));
    vi.mocked(changeConfidenceApi.getDecisionHistory).mockRejectedValue(new Error('Not found'));
    renderChangeDetail();
    await waitFor(() => {
      expect(screen.getByText(/error/i)).toBeInTheDocument();
    });
  });

  it('exibe empty state quando não há decisões', async () => {
    vi.mocked(changeConfidenceApi.getChange).mockResolvedValue(mockChange);
    vi.mocked(changeConfidenceApi.getIntelligence).mockResolvedValue(mockIntelligence);
    vi.mocked(changeConfidenceApi.getAdvisory).mockResolvedValue(mockAdvisory);
    vi.mocked(changeConfidenceApi.getDecisionHistory).mockResolvedValue({ releaseId: mockChange.changeId, decisions: [] });
    renderChangeDetail();
    await waitFor(() => {
      expect(screen.getByText(/no decisions recorded/i)).toBeInTheDocument();
    });
  });
});
