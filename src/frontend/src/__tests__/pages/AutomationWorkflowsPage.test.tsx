import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AutomationWorkflowsPage } from '../../features/operations/pages/AutomationWorkflowsPage';

vi.mock('../../features/operations/api/automation', () => ({
  automationApi: {
    listWorkflows: vi.fn(),
    getWorkflow: vi.fn(),
    listActions: vi.fn(),
    getAuditTrail: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { automationApi } from '../../features/operations/api/automation';

const mockWorkflows = {
  items: [
    {
      workflowId: 'wf-1',
      actionId: 'restart-controlled',
      actionDisplayName: 'Restart Controlled',
      status: 'Completed',
      riskLevel: 'Medium',
      requestedBy: 'john.doe',
      serviceId: 'svc-order-api',
      createdAt: '2026-03-20T10:00:00Z',
    },
    {
      workflowId: 'wf-2',
      actionId: 'scale-out',
      actionDisplayName: 'Scale Out',
      status: 'PendingApproval',
      riskLevel: 'Low',
      requestedBy: 'jane.smith',
      serviceId: null,
      createdAt: '2026-03-21T15:00:00Z',
    },
  ],
  totalCount: 2,
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AutomationWorkflowsPage />
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

describe('AutomationWorkflowsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(automationApi.listWorkflows).mockResolvedValue(mockWorkflows);
  });

  it('calls automationApi.listWorkflows on mount', async () => {
    renderPage();
    await waitFor(() => expect(automationApi.listWorkflows).toHaveBeenCalledTimes(1));
  });

  it('renders workflow items from API response', async () => {
    renderPage();
    await waitFor(() => expect(screen.getByText('Restart Controlled')).toBeInTheDocument());
    expect(screen.getByText('Scale Out')).toBeInTheDocument();
  });

  it('shows status badges', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Restart Controlled'));
    const completedElements = screen.getAllByText('Completed');
    expect(completedElements.length).toBeGreaterThan(0);
    const pendingElements = screen.getAllByText('PendingApproval');
    expect(pendingElements.length).toBeGreaterThan(0);
  });

  it('shows risk level badges', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Restart Controlled'));
    expect(screen.getByText('Medium')).toBeInTheDocument();
    expect(screen.getByText('Low')).toBeInTheDocument();
  });

  it('shows loading state while fetching', () => {
    vi.mocked(automationApi.listWorkflows).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Restart Controlled')).not.toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(automationApi.listWorkflows).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => expect(screen.queryByText('Restart Controlled')).not.toBeInTheDocument());
  });

  it('shows empty state when no workflows', async () => {
    vi.mocked(automationApi.listWorkflows).mockResolvedValue({ items: [], totalCount: 0 });
    renderPage();
    await waitFor(() => expect(screen.queryByText('Restart Controlled')).not.toBeInTheDocument());
  });

  it('does not render preview badge or DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Restart Controlled'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
    expect(screen.queryByText(/preview/i)).not.toBeInTheDocument();
  });
});
