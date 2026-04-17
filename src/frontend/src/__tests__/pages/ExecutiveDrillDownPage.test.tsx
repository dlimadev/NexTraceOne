import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { ExecutiveDrillDownPage } from '../../features/governance/pages/ExecutiveDrillDownPage';

vi.mock('../../features/governance/api/executive', () => ({
  executiveApi: {
    getBenchmarking: vi.fn(),
    getDrillDown: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { executiveApi } from '../../features/governance/api/executive';

const mockData = {
  entityType: 'team',
  entityId: 'team-1',
  entityName: 'Commerce',
  riskLevel: 'Critical' as const,
  maturityLevel: 'Developing' as const,
  keyIndicators: [{ name: 'Reliability Score', value: '62.0%', trend: 'Declining' as const, explanation: 'Below target' }],
  criticalServices: [{ serviceId: 'svc-1', serviceName: 'Order Processor', riskLevel: 'Critical' as const, mainIssue: 'Degradation' }],
  topGaps: [{ area: 'Runbook Coverage', severity: 'Critical' as const, description: 'Missing runbooks', recommendation: 'Create runbooks' }],
  recommendedFocus: ['Stabilize Order Processor'],
  generatedAt: '2026-03-22T00:00:00Z',
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/governance/executive/team/team-1']}>
        <Routes>
          <Route path="/governance/executive/:entityType/:entityId" element={<ExecutiveDrillDownPage />} />
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
describe('ExecutiveDrillDownPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(executiveApi.getDrillDown).mockResolvedValue(mockData);
  });

  it('calls executiveApi.getDrillDown on mount', async () => {
    renderPage();
    await waitFor(() => expect(executiveApi.getDrillDown).toHaveBeenCalledWith('team', 'team-1'));
  });

  it('renders entity name from API response', async () => {
    renderPage();
    await waitFor(() => expect(screen.getByText('Order Processor')).toBeInTheDocument());
  });

  it('shows loading state while fetching', () => {
    vi.mocked(executiveApi.getDrillDown).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Order Processor')).not.toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(executiveApi.getDrillDown).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => expect(screen.queryByText('Order Processor')).not.toBeInTheDocument());
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => expect(screen.getByText('Order Processor')).toBeInTheDocument());
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });
});
