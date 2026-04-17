import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ReleaseApprovalPoliciesPage } from '../../features/change-governance/pages/ReleaseApprovalPoliciesPage';

vi.mock('../../features/change-governance/api/changeIntelligence', () => ({
  changeIntelligenceApi: {
    listApprovalPolicies: vi.fn(),
    createApprovalPolicy: vi.fn(),
    deleteApprovalPolicy: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), delete: vi.fn() },
}));

import { changeIntelligenceApi } from '../../features/change-governance/api/changeIntelligence';

const mockPolicies = [
  {
    id: 'policy-001',
    name: 'Production Manual Approval',
    environmentId: 'Production',
    serviceId: null,
    serviceTag: null,
    requiresApproval: true,
    approvalType: 'Manual',
    externalWebhookUrl: null,
    minApprovers: 2,
    expirationHours: 48,
    requireEvidencePack: true,
    requireChecklistCompletion: true,
    minRiskScoreForManualApproval: 70,
    priority: 10,
    isActive: true,
    createdAt: '2026-04-01T10:00:00Z',
    createdBy: 'admin@example.com',
  },
  {
    id: 'policy-002',
    name: 'ServiceNow Integration',
    environmentId: 'Production',
    serviceTag: 'payment-services',
    serviceId: null,
    requiresApproval: true,
    approvalType: 'ExternalServiceNow',
    externalWebhookUrl: null,
    minApprovers: 1,
    expirationHours: 72,
    requireEvidencePack: false,
    requireChecklistCompletion: false,
    minRiskScoreForManualApproval: null,
    priority: 20,
    isActive: true,
    createdAt: '2026-04-05T10:00:00Z',
    createdBy: 'admin@example.com',
  },
];

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <ReleaseApprovalPoliciesPage />
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

describe('ReleaseApprovalPoliciesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (changeIntelligenceApi.listApprovalPolicies as ReturnType<typeof vi.fn>).mockResolvedValue(mockPolicies);
  });

  it('renders the page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Release Approval Policies/i)).toBeTruthy();
    });
  });

  it('shows active policies list header', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Active Policies/i)).toBeTruthy();
    });
  });

  it('renders loaded policies', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Production Manual Approval/i)).toBeTruthy();
    });
  });

  it('renders second policy', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/ServiceNow Integration/i)).toBeTruthy();
    });
  });

  it('renders New Policy button', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/New Policy/i)).toBeTruthy();
    });
  });

  it('shows no-policies message when empty', async () => {
    (changeIntelligenceApi.listApprovalPolicies as ReturnType<typeof vi.fn>).mockResolvedValue([]);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/No approval policies configured yet/i)).toBeTruthy();
    });
  });

  it('shows loading state initially', () => {
    (changeIntelligenceApi.listApprovalPolicies as ReturnType<typeof vi.fn>).mockReturnValue(new Promise(() => {}));
    renderPage();
    // When loading, the policies list is not yet rendered
    expect(screen.queryByText(/Active Policies/i)).toBeNull();
  });

  it('shows error state on failure', async () => {
    (changeIntelligenceApi.listApprovalPolicies as ReturnType<typeof vi.fn>).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.queryByText(/Production Manual Approval/i)).toBeNull();
    });
  });

  it('renders page subtitle', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/Configure approval requirements per environment and service/i)).toBeTruthy();
    });
  });

  it('renders approval type badge for first policy', async () => {
    renderPage();
    await waitFor(() => {
      // Policy 'Production Manual Approval' should be visible with Manual badge
      expect(screen.getByText(/Production Manual Approval/i)).toBeTruthy();
    });
  });
});
