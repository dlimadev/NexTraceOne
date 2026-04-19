import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { FinOpsConfigurationPage } from '../../features/governance/pages/FinOpsConfigurationPage';
import { FinOpsBudgetApprovalsPage } from '../../features/governance/pages/FinOpsBudgetApprovalsPage';

vi.mock('../../features/governance/api/finOps', () => ({
  finOpsApi: {
    getSummary: vi.fn(),
    getServiceFinOps: vi.fn(),
    getTeamFinOps: vi.fn(),
    getDomainFinOps: vi.fn(),
    getTrends: vi.fn(),
    getWasteSignals: vi.fn(),
    getEfficiency: vi.fn(),
    getConfiguration: vi.fn(),
    getBudgetApprovals: vi.fn(),
    createBudgetApproval: vi.fn(),
    resolveApproval: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn() },
}));

vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: () => ({ activeEnvironmentId: null }),
}));

import { finOpsApi } from '../../features/governance/api/finOps';

const mockConfig = {
  currency: 'USD',
  budgetGateEnabled: true,
  blockOnExceed: true,
  requireApproval: true,
  approvers: ['alice@corp.com', 'bob@corp.com'],
  alertThresholdPct: 80,
  anomalyDetectionEnabled: true,
  anomalyThresholds: '{"warning":20,"high":50,"critical":100}',
  comparisonWindowDays: 30,
  wasteDetectionEnabled: true,
  wasteThresholds: null,
  wasteCategories: ['IdleResources', 'Overprovisioned'],
  recommendationPolicy: null,
  notificationPolicy: null,
  showbackEnabled: false,
  chargebackEnabled: false,
  resolvedAt: '2026-04-18T12:00:00Z',
};

const mockApprovals = {
  items: [
    {
      approvalId: 'aaaa-0001',
      releaseId: 'bbbb-0001',
      serviceName: 'payment-api',
      environment: 'Production',
      actualCost: 1500,
      baselineCost: 1000,
      costDeltaPct: 50,
      currency: 'USD',
      status: 'Pending' as const,
      requestedBy: 'dev@corp.com',
      justification: 'Black Friday spike',
      resolvedBy: null,
      comment: null,
      requestedAt: '2026-04-18T10:00:00Z',
      resolvedAt: null,
    },
    {
      approvalId: 'aaaa-0002',
      releaseId: 'bbbb-0002',
      serviceName: 'order-service',
      environment: 'Production',
      actualCost: 3000,
      baselineCost: 2000,
      costDeltaPct: 50,
      currency: 'USD',
      status: 'Approved' as const,
      requestedBy: 'dev2@corp.com',
      justification: null,
      resolvedBy: 'manager@corp.com',
      comment: 'Critical release approved',
      requestedAt: '2026-04-17T10:00:00Z',
      resolvedAt: '2026-04-17T15:00:00Z',
    },
  ],
};

function renderConfigPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <FinOpsConfigurationPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

function renderApprovalsPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <FinOpsBudgetApprovalsPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('FinOpsConfigurationPage', () => {
  beforeEach(() => {
    vi.mocked(finOpsApi.getConfiguration).mockResolvedValue(mockConfig);
  });

  it('renders page title', async () => {
    renderConfigPage();
    await waitFor(() => {
      expect(screen.getByText('FinOps Configuration')).toBeTruthy();
    });
  });

  it('shows configured currency', async () => {
    renderConfigPage();
    await waitFor(() => {
      expect(screen.getByDisplayValue('USD')).toBeTruthy();
    });
  });

  it('shows gate enabled checkbox checked', async () => {
    renderConfigPage();
    await waitFor(() => {
      const checkboxes = screen.getAllByRole('checkbox');
      // First checkbox is budgetGateEnabled
      expect((checkboxes[0] as HTMLInputElement).checked).toBe(true);
    });
  });

  it('shows approvers list', async () => {
    renderConfigPage();
    await waitFor(() => {
      expect(screen.getByText('alice@corp.com')).toBeTruthy();
      expect(screen.getByText('bob@corp.com')).toBeTruthy();
    });
  });

  it('shows gate badge with correct status', async () => {
    renderConfigPage();
    await waitFor(() => {
      expect(screen.getByText('Approval required')).toBeTruthy();
    });
  });

  it('renders gate block on exceed checkbox', async () => {
    renderConfigPage();
    await waitFor(() => {
      expect(screen.getAllByRole('checkbox').length).toBeGreaterThanOrEqual(3);
    });
  });

  it('shows anomaly detection section', async () => {
    renderConfigPage();
    await waitFor(() => {
      expect(screen.getByText('Anomaly Detection')).toBeTruthy();
      // comparisonWindowDays = 30 days
      expect(screen.getByText((_, el) => el?.textContent?.includes('30') === true && el?.tagName === 'DD')).toBeTruthy();
    });
  });

  it('shows waste detection section with categories', async () => {
    renderConfigPage();
    await waitFor(() => {
      expect(screen.getByText('Waste Detection')).toBeTruthy();
      expect(screen.getByText('IdleResources')).toBeTruthy();
      expect(screen.getByText('Overprovisioned')).toBeTruthy();
    });
  });

  it('shows showback and chargeback section', async () => {
    renderConfigPage();
    await waitFor(() => {
      expect(screen.getByText('Showback & Chargeback')).toBeTruthy();
    });
  });
});

describe('FinOpsBudgetApprovalsPage', () => {
  beforeEach(() => {
    vi.mocked(finOpsApi.getBudgetApprovals).mockResolvedValue(mockApprovals);
    vi.mocked(finOpsApi.getConfiguration).mockResolvedValue(mockConfig);
  });

  it('renders page title', async () => {
    renderApprovalsPage();
    await waitFor(() => {
      expect(screen.getByText('finops.approvals.title')).toBeTruthy();
    });
  });

  it('shows all status filters', async () => {
    renderApprovalsPage();
    await waitFor(() => {
      expect(screen.getAllByText('finops.approvals.status.pending').length).toBeGreaterThanOrEqual(1);
      expect(screen.getAllByText('finops.approvals.status.approved').length).toBeGreaterThanOrEqual(1);
      expect(screen.getAllByText('finops.approvals.status.rejected').length).toBeGreaterThanOrEqual(1);
    });
  });

  it('lists approval items', async () => {
    renderApprovalsPage();
    await waitFor(() => {
      expect(screen.getByText('payment-api')).toBeTruthy();
      expect(screen.getByText('order-service')).toBeTruthy();
    });
  });

  it('expands approval on click and shows approve/reject actions', async () => {
    renderApprovalsPage();
    await waitFor(() => screen.getByText('payment-api'));

    fireEvent.click(screen.getByText('payment-api'));
    await waitFor(() => {
      expect(screen.getByText(/black friday spike/i)).toBeTruthy();
      expect(screen.getAllByRole('button').some((b) => b.textContent?.match(/approve/i))).toBe(true);
    });
  });

  it('shows result count', async () => {
    renderApprovalsPage();
    await waitFor(() => {
      // The result count span contains "2" + i18n key - use regex to find the span
      const countEl = document.body.querySelector('span.ml-auto');
      expect(countEl?.textContent).toContain('2');
    });
  });
});
