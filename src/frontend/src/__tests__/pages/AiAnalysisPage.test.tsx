import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AiAnalysisPage } from '../../features/ai-hub/pages/AiAnalysisPage';

// ── Hoisted mocks ──────────────────────────────────────────────────────────

const {
  useAuthMock,
  analyzeNonProdMock,
  compareEnvironmentsMock,
  assessReadinessMock,
} = vi.hoisted(() => ({
  useAuthMock: vi.fn(),
  analyzeNonProdMock: vi.fn(),
  compareEnvironmentsMock: vi.fn(),
  assessReadinessMock: vi.fn(),
}));

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: useAuthMock,
}));

vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn().mockReturnValue({
    activeEnvironment: {
      id: 'env-qa-001',
      name: 'QA',
      profile: 'validation',
      isProductionLike: false,
      isDefault: true,
    },
    availableEnvironments: [
      { id: 'env-qa-001', name: 'QA', profile: 'validation', isProductionLike: false },
      { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
      { id: 'env-staging-001', name: 'Staging', profile: 'staging', isProductionLike: true },
    ],
    activeEnvironmentId: 'env-qa-001',
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  }),
}));

vi.mock('../../features/ai-hub/api/aiGovernance', () => ({
  aiGovernanceApi: {
    analyzeNonProdEnvironment: analyzeNonProdMock,
    compareEnvironments: compareEnvironmentsMock,
    assessPromotionReadiness: assessReadinessMock,
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

// ── Helpers ────────────────────────────────────────────────────────────────

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AiAnalysisPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

// ── Tests ──────────────────────────────────────────────────────────────────

describe('AiAnalysisPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthMock.mockReturnValue({
      tenantId: 'tenant-acme-001',
      isAuthenticated: true,
      user: { id: 'user-1', tenantId: 'tenant-acme-001' },
    });
  });

  it('renders page title and environment context', () => {
    renderPage();
    expect(screen.getByText('AI Analysis')).toBeDefined();
    expect(screen.getByText(/Analyzing: QA/)).toBeDefined();
  });

  it('renders all three tabs', () => {
    renderPage();
    expect(screen.getByText('Non-Prod Risk')).toBeDefined();
    expect(screen.getByText('Compare')).toBeDefined();
    expect(screen.getByText('Readiness')).toBeDefined();
  });

  it('shows non-prod mode badge for non-production environment', () => {
    renderPage();
    expect(screen.getByText('Non-production environment')).toBeDefined();
  });

  it('shows run analysis button on non-prod tab', () => {
    renderPage();
    expect(screen.getByText('Run Analysis')).toBeDefined();
  });

  it('calls analyzeNonProdEnvironment and shows result', async () => {
    analyzeNonProdMock.mockResolvedValue({
      overallRiskLevel: 'HIGH',
      recommendation: 'Block promotion.',
      findings: [
        { severity: 'HIGH', category: 'contract-drift', description: 'Breaking change detected' },
      ],
      isFallback: false,
      correlationId: 'corr-abc-123',
    });

    renderPage();
    await userEvent.click(screen.getByText('Run Analysis'));

    const highElements = await screen.findAllByText('HIGH');
    expect(highElements.length).toBeGreaterThan(0);
    expect(await screen.findByText('Block promotion.')).toBeDefined();
    expect(analyzeNonProdMock).toHaveBeenCalledWith(
      expect.objectContaining({
        tenantId: 'tenant-acme-001',
        environmentId: 'env-qa-001',
        environmentName: 'QA',
      }),
    );
  });

  it('shows no-context message when tenantId is null', () => {
    useAuthMock.mockReturnValueOnce({
      tenantId: null,
      isAuthenticated: false,
      isLoadingUser: false,
      accessToken: null,
      user: null,
      requiresTenantSelection: false,
      availableTenants: [],
      login: vi.fn(),
      selectTenant: vi.fn(),
      logout: vi.fn(),
    });

    renderPage();
    expect(screen.getByText(/No active tenant or environment context/)).toBeDefined();
  });
});
