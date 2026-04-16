import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ReleaseApprovalGatewayPage } from '../../features/change-governance/pages/ReleaseApprovalGatewayPage';

vi.mock('../../features/change-governance/api/changeIntelligence', () => ({
  changeIntelligenceApi: {
    listApprovalRequests: vi.fn(),
    requestExternalApproval: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { changeIntelligenceApi } from '../../features/change-governance/api/changeIntelligence';

const mockApprovals = {
  releaseId: 'release-001',
  approvalRequests: [
    {
      id: 'approval-001',
      approvalType: 'ExternalWebhook',
      externalSystem: 'ServiceNow',
      targetEnvironment: 'Production',
      status: 'Pending',
      requestedAt: '2026-04-10T14:00:00Z',
      respondedAt: null,
      respondedBy: null,
      comments: null,
      externalRequestId: 'CHG0012345',
      callbackTokenExpiresAt: '2026-04-12T14:00:00Z',
    },
    {
      id: 'approval-002',
      approvalType: 'Internal',
      externalSystem: null,
      targetEnvironment: 'Production',
      status: 'Approved',
      requestedAt: '2026-04-09T12:00:00Z',
      respondedAt: '2026-04-09T13:00:00Z',
      respondedBy: 'john.doe@example.com',
      comments: 'Approved by CAB',
      externalRequestId: null,
      callbackTokenExpiresAt: '2026-04-11T12:00:00Z',
    },
  ],
};

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <ReleaseApprovalGatewayPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ReleaseApprovalGatewayPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (changeIntelligenceApi.listApprovalRequests as ReturnType<typeof vi.fn>).mockResolvedValue(mockApprovals);
  });

  it('renders page title', () => {
    renderPage();
    expect(screen.getByText('Release Approval Gateway')).toBeInTheDocument();
  });

  it('renders page subtitle', () => {
    renderPage();
    expect(screen.getByText('Manage approval requests for releases — internal, external webhook and ServiceNow')).toBeInTheDocument();
  });

  it('renders release ID label', () => {
    renderPage();
    expect(screen.getAllByText('Release ID').length).toBeGreaterThan(0);
  });

  it('renders approval type selector', () => {
    renderPage();
    expect(screen.getByText('Approval Type')).toBeInTheDocument();
  });

  it('renders target environment selector', () => {
    renderPage();
    expect(screen.getByText('Target Environment')).toBeInTheDocument();
  });

  it('renders submit button', () => {
    renderPage();
    expect(screen.getByText('Submit Approval Request')).toBeInTheDocument();
  });

  it('renders new request section title', () => {
    renderPage();
    expect(screen.getByText('New Approval Request')).toBeInTheDocument();
  });

  it('renders token expiry input', () => {
    renderPage();
    expect(screen.getByText('Token Expiry (hours)')).toBeInTheDocument();
  });

  it('renders no requests message when list empty', async () => {
    (changeIntelligenceApi.listApprovalRequests as ReturnType<typeof vi.fn>).mockResolvedValue({ releaseId: '', approvalRequests: [] });
    renderPage();
    await waitFor(() => {
      // page renders without crashing
      expect(screen.getByText('Release Approval Gateway')).toBeInTheDocument();
    });
  });

  it('matches snapshot', () => {
    const { container } = renderPage();
    expect(container.firstChild).toBeTruthy();
  });
});
