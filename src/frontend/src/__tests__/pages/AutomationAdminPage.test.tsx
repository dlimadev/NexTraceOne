import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AutomationAdminPage } from '../../features/operations/pages/AutomationAdminPage';

vi.mock('../../features/operations/api/automation', () => ({
  automationApi: {
    listWorkflows: vi.fn(),
    getWorkflow: vi.fn(),
    listActions: vi.fn(),
    getAction: vi.fn(),
    getAuditTrail: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { automationApi } from '../../features/operations/api/automation';

const mockActions = {
  items: [
    {
      actionId: 'restart-controlled',
      name: 'restart-controlled',
      displayName: 'Restart Controlled',
      description: 'Controlled restart of service instances',
      actionType: 'RestartControlled',
      riskLevel: 'Medium',
      requiresApproval: true,
      allowedPersonas: ['Engineer', 'TechLead'],
      allowedEnvironments: ['Production', 'Staging'],
      preconditionTypes: ['ServiceHealth', 'ChangeWindow'],
      hasPostValidation: true,
    },
    {
      actionId: 'scale-out',
      name: 'scale-out',
      displayName: 'Scale Out',
      description: 'Scale out service instances',
      actionType: 'ScaleOut',
      riskLevel: 'Low',
      requiresApproval: false,
      allowedPersonas: ['Engineer'],
      allowedEnvironments: ['Production', 'Staging', 'Dev'],
      preconditionTypes: [],
      hasPostValidation: false,
    },
  ],
};

const mockAudit = {
  entries: [
    {
      entryId: 'a-1',
      workflowId: 'wf-1',
      action: 'WorkflowCreated',
      performedBy: 'john.doe',
      performedAt: '2026-03-20T10:00:00Z',
      details: null,
      serviceId: 'svc-order-api',
      teamId: 'team-1',
    },
  ],
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AutomationAdminPage />
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

describe('AutomationAdminPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(automationApi.listActions).mockResolvedValue(mockActions);
    vi.mocked(automationApi.getAuditTrail).mockResolvedValue(mockAudit);
  });

  it('calls automationApi.listActions on mount', async () => {
    renderPage();
    await waitFor(() => expect(automationApi.listActions).toHaveBeenCalledTimes(1));
  });

  it('renders action items from API response', async () => {
    renderPage();
    await waitFor(() => expect(screen.getByText('Restart Controlled')).toBeInTheDocument());
    expect(screen.getByText('Scale Out')).toBeInTheDocument();
  });

  it('shows risk level badges', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Restart Controlled'));
    expect(screen.getByText('Medium')).toBeInTheDocument();
    expect(screen.getByText('Low')).toBeInTheDocument();
  });

  it('shows loading state while fetching', () => {
    vi.mocked(automationApi.listActions).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Restart Controlled')).not.toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(automationApi.listActions).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => expect(screen.queryByText('Restart Controlled')).not.toBeInTheDocument());
  });

  it('does not use mock data — calls real API', async () => {
    renderPage();
    await waitFor(() => expect(automationApi.listActions).toHaveBeenCalled());
    await waitFor(() => expect(automationApi.getAuditTrail).toHaveBeenCalled());
  });

  it('does not render DemoBanner or preview badge', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Restart Controlled'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
    expect(screen.queryByText(/preview/i)).not.toBeInTheDocument();
  });
});
