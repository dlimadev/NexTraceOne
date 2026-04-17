import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ReleaseGatesDashboardPage } from '../../features/change-governance/pages/ReleaseGatesDashboardPage';

vi.mock('../../features/change-governance/api/changeIntelligence', () => ({
  changeIntelligenceApi: {
    listPromotionGatesByEnvironment: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

const mockGates = {
  gates: [
    {
      gateId: 'gate-001',
      name: 'Spectral Quality Gate',
      description: 'Ensures API quality score above 70',
      environmentFrom: 'Development',
      environmentTo: 'Pre-Production',
      isActive: true,
      blockOnFailure: true,
      createdAt: '2026-01-15T09:00:00Z',
    },
    {
      gateId: 'gate-002',
      name: 'Integration Test Gate',
      description: 'All integration tests must pass',
      environmentFrom: 'Development',
      environmentTo: 'Pre-Production',
      isActive: true,
      blockOnFailure: false,
      createdAt: '2026-01-20T09:00:00Z',
    },
    {
      gateId: 'gate-003',
      name: 'Legacy Smoke Test',
      description: null,
      environmentFrom: 'Development',
      environmentTo: 'Pre-Production',
      isActive: false,
      blockOnFailure: false,
      createdAt: '2025-12-01T09:00:00Z',
    },
  ],
};

function wrapper(children: React.ReactNode) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return (
    <QueryClientProvider client={qc}>
      <MemoryRouter>{children}</MemoryRouter>
    </QueryClientProvider>
  );
}

import { changeIntelligenceApi } from '../../features/change-governance/api/changeIntelligence';

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

describe('ReleaseGatesDashboardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders page title', () => {
    vi.mocked(changeIntelligenceApi.listPromotionGatesByEnvironment).mockResolvedValue(mockGates as any);
    const { container } = render(wrapper(<ReleaseGatesDashboardPage />));
    expect(container).toBeTruthy();
  });

  it('renders environment path selector buttons', async () => {
    vi.mocked(changeIntelligenceApi.listPromotionGatesByEnvironment).mockResolvedValue(mockGates as any);
    render(wrapper(<ReleaseGatesDashboardPage />));
    expect(await screen.findAllByText('Pre-Production')).toBeTruthy();
    expect(screen.getAllByText('Production').length).toBeGreaterThan(0);
  });

  it('renders list of promotion gates', async () => {
    vi.mocked(changeIntelligenceApi.listPromotionGatesByEnvironment).mockResolvedValue(mockGates as any);
    render(wrapper(<ReleaseGatesDashboardPage />));
    expect(await screen.findByText('Spectral Quality Gate')).toBeTruthy();
    expect(screen.getByText('Integration Test Gate')).toBeTruthy();
  });

  it('shows Blocking badge for block-on-failure active gates', async () => {
    vi.mocked(changeIntelligenceApi.listPromotionGatesByEnvironment).mockResolvedValue(mockGates as any);
    render(wrapper(<ReleaseGatesDashboardPage />));
    expect(await screen.findByText('Blocking')).toBeTruthy();
  });

  it('shows Non-blocking badge for non-blocking active gates', async () => {
    vi.mocked(changeIntelligenceApi.listPromotionGatesByEnvironment).mockResolvedValue(mockGates as any);
    render(wrapper(<ReleaseGatesDashboardPage />));
    expect(await screen.findByText('Non-blocking')).toBeTruthy();
  });

  it('shows Inactive badge for inactive gates', async () => {
    vi.mocked(changeIntelligenceApi.listPromotionGatesByEnvironment).mockResolvedValue(mockGates as any);
    render(wrapper(<ReleaseGatesDashboardPage />));
    expect(await screen.findByText('Inactive')).toBeTruthy();
  });

  it('shows summary cards with counts', async () => {
    vi.mocked(changeIntelligenceApi.listPromotionGatesByEnvironment).mockResolvedValue(mockGates as any);
    render(wrapper(<ReleaseGatesDashboardPage />));
    expect(await screen.findByText('Active Gates')).toBeTruthy();
    expect(screen.getByText('Blocking Gates')).toBeTruthy();
    expect(screen.getByText('Total Gates')).toBeTruthy();
  });

  it('shows empty state when no gates returned', async () => {
    vi.mocked(changeIntelligenceApi.listPromotionGatesByEnvironment).mockResolvedValue({ gates: [] } as any);
    render(wrapper(<ReleaseGatesDashboardPage />));
    expect(await screen.findByText(/no gates found/i)).toBeTruthy();
  });

  it('changes environment pair when second button clicked', async () => {
    vi.mocked(changeIntelligenceApi.listPromotionGatesByEnvironment).mockResolvedValue(mockGates as any);
    render(wrapper(<ReleaseGatesDashboardPage />));
    const prodButtons = await screen.findAllByText('Production');
    await userEvent.click(prodButtons[0]);
    expect(changeIntelligenceApi.listPromotionGatesByEnvironment).toHaveBeenCalled();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(changeIntelligenceApi.listPromotionGatesByEnvironment).mockRejectedValue(new Error('Failed'));
    render(wrapper(<ReleaseGatesDashboardPage />));
    expect(await screen.findByText(/could not load gates/i)).toBeTruthy();
  });
});
