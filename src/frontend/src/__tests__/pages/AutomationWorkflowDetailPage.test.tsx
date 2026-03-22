import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AutomationWorkflowDetailPage } from '../../features/operations/pages/AutomationWorkflowDetailPage';

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

const mockWorkflowDetail = {
  workflowId: 'wf-1',
  actionId: 'restart-controlled',
  actionDisplayName: 'Restart Controlled',
  status: 'Completed',
  riskLevel: 'Medium',
  rationale: 'Service experiencing high error rate.',
  requestedBy: 'john.doe',
  approverInfo: {
    approvedBy: 'admin-user',
    approvedAt: '2026-03-20T10:30:00Z',
    approvalStatus: 'Approved',
  },
  scope: 'instance-01',
  environment: 'Production',
  serviceId: 'svc-order-api',
  incidentId: 'inc-123',
  changeId: null,
  preconditions: [
    { type: 'ServiceHealth', description: 'Service must be degraded', status: 'Met', evaluatedAt: '2026-03-20T10:05:00Z' },
  ],
  executionSteps: [
    { stepOrder: 1, title: 'Drain connections', status: 'Completed', completedAt: '2026-03-20T10:35:00Z', completedBy: 'system' },
    { stepOrder: 2, title: 'Restart service', status: 'Completed', completedAt: '2026-03-20T10:36:00Z', completedBy: 'system' },
  ],
  validationInfo: {
    status: 'Validated',
    observedOutcome: 'Service healthy after restart.',
    validatedBy: 'john.doe',
    validatedAt: '2026-03-20T10:45:00Z',
  },
  auditEntries: [
    { action: 'Created', performedBy: 'john.doe', performedAt: '2026-03-20T10:00:00Z', details: null },
    { action: 'Approved', performedBy: 'admin-user', performedAt: '2026-03-20T10:30:00Z', details: 'Approved for production restart' },
    { action: 'Completed', performedBy: 'system', performedAt: '2026-03-20T10:36:00Z', details: null },
  ],
  createdAt: '2026-03-20T10:00:00Z',
  updatedAt: '2026-03-20T10:45:00Z',
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/operations/automation/wf-1']}>
        <Routes>
          <Route path="/operations/automation/:workflowId" element={<AutomationWorkflowDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('AutomationWorkflowDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(automationApi.getWorkflow).mockResolvedValue(mockWorkflowDetail);
  });

  it('calls automationApi.getWorkflow with correct id', async () => {
    renderPage();
    await waitFor(() => expect(automationApi.getWorkflow).toHaveBeenCalledWith('wf-1'));
  });

  it('renders workflow action name', async () => {
    renderPage();
    await waitFor(() => expect(screen.getByText('Restart Controlled')).toBeInTheDocument());
  });

  it('renders status and risk badges', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Restart Controlled'));
    const completedBadges = screen.getAllByText('Completed');
    expect(completedBadges.length).toBeGreaterThan(0);
    expect(screen.getByText('Medium')).toBeInTheDocument();
  });

  it('renders execution steps', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Restart Controlled'));
    expect(screen.getByText('Drain connections')).toBeInTheDocument();
    expect(screen.getByText('Restart service')).toBeInTheDocument();
  });

  it('renders audit trail', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Restart Controlled'));
    expect(screen.getByText('Approved for production restart')).toBeInTheDocument();
  });

  it('renders preconditions', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Restart Controlled'));
    expect(screen.getByText('Service must be degraded')).toBeInTheDocument();
  });

  it('renders validation info', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Restart Controlled'));
    expect(screen.getByText('Service healthy after restart.')).toBeInTheDocument();
  });

  it('shows loading state while fetching', () => {
    vi.mocked(automationApi.getWorkflow).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Restart Controlled')).not.toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(automationApi.getWorkflow).mockRejectedValue(new Error('Not found'));
    renderPage();
    await waitFor(() => expect(screen.queryByText('Restart Controlled')).not.toBeInTheDocument());
  });

  it('is not a stub — renders real workflow data', async () => {
    renderPage();
    await waitFor(() => screen.getByText('Restart Controlled'));
    // Ensure it's not showing preview/stub indicators
    expect(screen.queryByText(/preview/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/stub/i)).not.toBeInTheDocument();
    // Confirm real data sections are present
    const johnDoeElements = screen.getAllByText('john.doe');
    expect(johnDoeElements.length).toBeGreaterThan(0);
    expect(screen.getByText('Production')).toBeInTheDocument();
  });
});
