import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AiAnalysisPage } from '../../features/ai-hub/pages/AiAnalysisPage';

// ── Hoisted mocks ──────────────────────────────────────────────────────────

const {
  useAuthMock,
  useEnvironmentMock,
  analyzeNonProdMock,
  compareEnvironmentsMock,
  assessReadinessMock,
} = vi.hoisted(() => ({
  useAuthMock: vi.fn(),
  useEnvironmentMock: vi.fn(),
  analyzeNonProdMock: vi.fn(),
  compareEnvironmentsMock: vi.fn(),
  assessReadinessMock: vi.fn(),
}));

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: useAuthMock,
}));

vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: useEnvironmentMock,
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

const defaultEnv = {
  activeEnvironment: { id: 'env-qa-001', name: 'QA', profile: 'validation', isProductionLike: false, isDefault: true },
  availableEnvironments: [
    { id: 'env-qa-001', name: 'QA', profile: 'validation', isProductionLike: false },
    { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
    { id: 'env-staging-001', name: 'Staging', profile: 'staging', isProductionLike: true },
  ],
  activeEnvironmentId: 'env-qa-001',
  isLoadingEnvironments: false,
  selectEnvironment: vi.fn(),
  clearEnvironment: vi.fn(),
};

const productionLikeEnv = {
  ...defaultEnv,
  activeEnvironment: { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true, isDefault: false },
  activeEnvironmentId: 'env-prod-001',
};

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

describe('AiAnalysisPage — Phase 8 Hardening', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAuthMock.mockReturnValue({ tenantId: 'tenant-acme-001', isAuthenticated: true, user: { id: 'user-1', tenantId: 'tenant-acme-001' } });
    useEnvironmentMock.mockReturnValue(defaultEnv);
  });

  it('should switch to Compare tab and show reference environment label', () => {
    renderPage();
    fireEvent.click(screen.getByText('Compare'));
    expect(screen.getByText('Reference environment')).toBeDefined();
  });

  it('should switch to Readiness tab and show service name input', () => {
    renderPage();
    fireEvent.click(screen.getByText('Readiness'));
    const input = screen.getByPlaceholderText(/payment-service/i);
    expect(input).toBeDefined();
  });

  it('should show context indicator with active environment name', () => {
    renderPage();
    expect(screen.getByText(/Analyzing: QA/)).toBeDefined();
  });

  it('should show non-production badge for non-prod environment', () => {
    renderPage();
    expect(screen.getByText('Non-production environment')).toBeDefined();
  });

  it('should NOT show non-production badge for production-like environment', () => {
    useEnvironmentMock.mockReturnValue(productionLikeEnv);
    renderPage();
    expect(screen.queryByText('Non-production environment')).toBeNull();
  });

  it('should NOT show Run Analysis button for production-like environment', () => {
    useEnvironmentMock.mockReturnValue(productionLikeEnv);
    renderPage();
    expect(screen.queryByText('Run Analysis')).toBeNull();
  });

  it('should show Run Analysis button for non-production environment', () => {
    renderPage();
    expect(screen.getByText('Run Analysis')).toBeDefined();
  });

  it('should show all 3 tabs', () => {
    renderPage();
    expect(screen.getByText('Non-Prod Risk')).toBeDefined();
    expect(screen.getByText('Compare')).toBeDefined();
    expect(screen.getByText('Readiness')).toBeDefined();
  });

  it('should show no-context message when tenantId is null', () => {
    useAuthMock.mockReturnValue({ tenantId: null, isAuthenticated: false, user: null });
    renderPage();
    expect(screen.getByText(/No active tenant or environment context/)).toBeDefined();
  });

  it('Readiness tab: Assess Readiness button is disabled when inputs are empty', () => {
    renderPage();
    fireEvent.click(screen.getByText('Readiness'));
    const btn = screen.getByText('Assess Readiness').closest('button');
    expect(btn).toBeDefined();
    expect(btn!.hasAttribute('disabled')).toBe(true);
  });

  it('Compare tab: Compare button is disabled when no reference environment is selected', () => {
    renderPage();
    fireEvent.click(screen.getByText('Compare'));
    // There are two 'Compare' buttons: the tab and the action button — pick the disabled action button
    const buttons = screen.getAllByText('Compare', { selector: 'button' });
    const actionButton = buttons.find(b => b.closest('.space-y-4'));
    expect(actionButton).toBeDefined();
    expect(actionButton!.hasAttribute('disabled')).toBe(true);
  });
});
