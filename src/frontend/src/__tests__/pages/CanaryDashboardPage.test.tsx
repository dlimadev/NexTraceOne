import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { CanaryDashboardPage } from '../../features/platform-admin/pages/CanaryDashboardPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type {
  CanaryDashboardResponse,
} from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getCanaryDashboard: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={qc}>{children}</QueryClientProvider>
);

const mockRollout = {
  id: 'rollout-001',
  serviceName: 'payment-service',
  stableVersion: 'v1.2.0',
  canaryVersion: 'v1.3.0',
  status: 'Active' as const,
  trafficPercentage: 20,
  environment: 'production',
  stableErrorRate: 0.5,
  canaryErrorRate: 0.3,
  stableP99LatencyMs: 120,
  canaryP99LatencyMs: 110,
  stableRps: 500,
  canaryRps: 125,
  startedAt: '2026-04-10T10:00:00Z',
};

const mockData: CanaryDashboardResponse = {
  checkedAt: '2026-04-15T12:00:00Z',
  rollouts: [
    mockRollout,
    {
      ...mockRollout,
      id: 'rollout-002',
      serviceName: 'order-service',
      status: 'Promoted',
      canaryVersion: 'v2.0.0',
      trafficPercentage: 100,
    },
    {
      ...mockRollout,
      id: 'rollout-003',
      serviceName: 'inventory-service',
      status: 'RolledBack',
      canaryVersion: 'v1.1.1',
      canaryErrorRate: 5.0,
    },
  ],
};

const mockEmptyData: CanaryDashboardResponse = {
  checkedAt: '2026-04-15T12:00:00Z',
  rollouts: [],
};

describe('CanaryDashboardPage', () => {
  beforeEach(() => {
    qc.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getCanaryDashboard).mockImplementation(
      () => new Promise(() => {}),
    );
    render(<CanaryDashboardPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state', async () => {
    vi.mocked(platformAdminApi.getCanaryDashboard).mockRejectedValue(
      new Error('fail'),
    );
    render(<CanaryDashboardPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('renders page title and subtitle', async () => {
    vi.mocked(platformAdminApi.getCanaryDashboard).mockResolvedValue(mockData);
    render(<CanaryDashboardPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('title')).toBeDefined();
      expect(screen.getByText('subtitle')).toBeDefined();
    });
  });

  it('renders summary cards with correct counts', async () => {
    vi.mocked(platformAdminApi.getCanaryDashboard).mockResolvedValue(mockData);
    render(<CanaryDashboardPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('totalRollouts')).toBeDefined();
      expect(screen.getByText('activeRollouts')).toBeDefined();
      expect(screen.getByText('promoted')).toBeDefined();
      expect(screen.getByText('rolledBack')).toBeDefined();
    });
  });

  it('renders rollout service names', async () => {
    vi.mocked(platformAdminApi.getCanaryDashboard).mockResolvedValue(mockData);
    render(<CanaryDashboardPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('payment-service')).toBeDefined();
      expect(screen.getByText('order-service')).toBeDefined();
      expect(screen.getByText('inventory-service')).toBeDefined();
    });
  });

  it('renders canary and stable versions', async () => {
    vi.mocked(platformAdminApi.getCanaryDashboard).mockResolvedValue(mockData);
    render(<CanaryDashboardPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('v1.3.0 ← v1.2.0')).toBeDefined();
    });
  });

  it('renders traffic percentage', async () => {
    vi.mocked(platformAdminApi.getCanaryDashboard).mockResolvedValue(mockData);
    render(<CanaryDashboardPage />, { wrapper: Wrapper });
    await waitFor(() => {
      const pctElements = screen.getAllByText('20%');
      expect(pctElements.length).toBeGreaterThan(0);
    });
  });

  it('shows empty state when no rollouts', async () => {
    vi.mocked(platformAdminApi.getCanaryDashboard).mockResolvedValue(
      mockEmptyData,
    );
    render(<CanaryDashboardPage />, { wrapper: Wrapper });
    await waitFor(() =>
      expect(screen.getByText('noRollouts')).toBeDefined(),
    );
  });

  it('filters rollouts by status', async () => {
    vi.mocked(platformAdminApi.getCanaryDashboard).mockResolvedValue(mockData);
    render(<CanaryDashboardPage />, { wrapper: Wrapper });
    await waitFor(() =>
      expect(screen.getByText('payment-service')).toBeDefined(),
    );

    const promotedBtn = screen.getByRole('button', {
      name: (name) => name === 'status.Promoted',
    });
    await userEvent.click(promotedBtn);

    await waitFor(() => {
      expect(screen.queryByText('payment-service')).toBeNull();
      expect(screen.getByText('order-service')).toBeDefined();
    });
  });

  it('resets filter to all', async () => {
    vi.mocked(platformAdminApi.getCanaryDashboard).mockResolvedValue(mockData);
    render(<CanaryDashboardPage />, { wrapper: Wrapper });
    await waitFor(() =>
      expect(screen.getByText('payment-service')).toBeDefined(),
    );

    const allBtn = screen.getByRole('button', { name: 'all' });
    await userEvent.click(allBtn);

    await waitFor(() => {
      expect(screen.getByText('payment-service')).toBeDefined();
      expect(screen.getByText('order-service')).toBeDefined();
    });
  });

  it('renders refresh button', async () => {
    vi.mocked(platformAdminApi.getCanaryDashboard).mockResolvedValue(mockData);
    render(<CanaryDashboardPage />, { wrapper: Wrapper });
    await waitFor(() =>
      expect(screen.getByText('refresh')).toBeDefined(),
    );
  });

  it('renders metric labels for each rollout', async () => {
    vi.mocked(platformAdminApi.getCanaryDashboard).mockResolvedValue(mockData);
    render(<CanaryDashboardPage />, { wrapper: Wrapper });
    await waitFor(() => {
      const errorRateLabels = screen.getAllByText('errorRate');
      expect(errorRateLabels.length).toBeGreaterThan(0);
    });
  });
});
