import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { PlatformAlertRulesPage } from '../../features/platform-admin/pages/PlatformAlertRulesPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { PlatformAlertsResponse } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en' },
  }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getPlatformAlerts: vi.fn(),
    updateAlertRule: vi.fn(),
    getNetworkPolicy: vi.fn(),
    getElasticsearchManager: vi.fn(),
    updateIlmPolicy: vi.fn(),
    getResourceBudgets: vi.fn(),
    updateTenantQuota: vi.fn(),
    getSupportBundle: vi.fn(),
    getBackupStatus: vi.fn(),
    triggerBackup: vi.fn(),
    getStartupReport: vi.fn(),
    getRestorePoints: vi.fn(),
    initiateRecovery: vi.fn(),
    getGreenOpsReport: vi.fn(),
    updateGreenOpsConfig: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
);

const mockData: PlatformAlertsResponse = {
  activeAlertCount: 1,
  suppressedUntil: undefined,
  rules: [
    {
      id: 'rule-001',
      name: 'Outbox Pending',
      metric: 'outbox_pending_count',
      warningThreshold: 500,
      criticalThreshold: 2000,
      unit: 'msgs',
      enabled: true,
      cooldownMinutes: 15,
      description: 'Alerts when outbox queue grows too large',
    },
    {
      id: 'rule-002',
      name: 'Disk Usage',
      metric: 'disk_used_percent',
      warningThreshold: 80,
      criticalThreshold: 95,
      unit: '%',
      enabled: false,
      cooldownMinutes: 30,
      description: 'Alerts when disk usage is high',
    },
  ],
  recentAlerts: [
    {
      id: 'alert-001',
      ruleId: 'rule-001',
      ruleName: 'Outbox Pending',
      severity: 'Warning',
      status: 'Active',
      triggeredAt: '2026-04-15T10:00:00Z',
      value: 620,
      unit: 'msgs',
      message: 'Outbox pending count exceeded warning threshold',
    },
  ],
};

const mockEmpty: PlatformAlertsResponse = {
  activeAlertCount: 0,
  rules: [],
  recentAlerts: [],
};

describe('PlatformAlertRulesPage', () => {
  beforeEach(() => {
    queryClient.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getPlatformAlerts).mockImplementation(
      () => new Promise(() => {})
    );
    render(<PlatformAlertRulesPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state on API failure', async () => {
    vi.mocked(platformAdminApi.getPlatformAlerts).mockRejectedValue(new Error('Network error'));
    render(<PlatformAlertRulesPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('renders summary cards with correct counts', async () => {
    vi.mocked(platformAdminApi.getPlatformAlerts).mockResolvedValue(mockData);
    render(<PlatformAlertRulesPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getAllByText('1').length).toBeGreaterThan(0));
    expect(screen.getAllByText('2').length).toBeGreaterThan(0);
  });

  it('renders rule rows', async () => {
    vi.mocked(platformAdminApi.getPlatformAlerts).mockResolvedValue(mockData);
    render(<PlatformAlertRulesPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getAllByText('Outbox Pending').length).toBeGreaterThan(0));
    expect(screen.getByText('Disk Usage')).toBeDefined();
  });

  it('renders alert history row', async () => {
    vi.mocked(platformAdminApi.getPlatformAlerts).mockResolvedValue(mockData);
    render(<PlatformAlertRulesPage />, { wrapper: Wrapper });
    await waitFor(() =>
      expect(screen.getAllByText('Outbox Pending').length).toBeGreaterThan(0)
    );
  });

  it('shows no history message when empty', async () => {
    vi.mocked(platformAdminApi.getPlatformAlerts).mockResolvedValue(mockEmpty);
    render(<PlatformAlertRulesPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('noHistory')).toBeDefined());
  });

  it('shows page title', async () => {
    vi.mocked(platformAdminApi.getPlatformAlerts).mockResolvedValue(mockData);
    render(<PlatformAlertRulesPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('title')).toBeDefined());
  });
});
