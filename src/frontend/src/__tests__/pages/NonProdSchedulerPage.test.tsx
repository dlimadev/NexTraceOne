import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { NonProdSchedulerPage } from '../../features/platform-admin/pages/NonProdSchedulerPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { NonProdSchedulesResponse } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, _opts?: Record<string, unknown>) => key,
    i18n: { language: 'en' },
  }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getNonProdSchedules: vi.fn(),
    updateNonProdSchedule: vi.fn(),
    overrideNonProdSchedule: vi.fn(),
    getExternalHttpAudit: vi.fn(),
    getEnvironmentPolicies: vi.fn(),
    updateEnvironmentPolicy: vi.fn(),
    getProxyConfig: vi.fn(),
    updateProxyConfig: vi.fn(),
    testProxyConnectivity: vi.fn(),
    getGreenOpsReport: vi.fn(),
    updateGreenOpsConfig: vi.fn(),
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
    getAiGovernorStatus: vi.fn(),
    updateAiGovernorConfig: vi.fn(),
    getAiGovernanceDashboard: vi.fn(),
    updateAiGovernanceConfig: vi.fn(),
    getCapacityForecast: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
);

const mockSchedulesResponse: NonProdSchedulesResponse = {
  schedules: [
    {
      environmentId: 'env-staging',
      environmentName: 'staging-acme',
      enabled: true,
      activeDaysOfWeek: [1, 2, 3, 4, 5],
      activeFromHour: 8,
      activeToHour: 20,
      timezone: 'Europe/Lisbon',
      status: 'Active',
      estimatedSavingPercent: 57,
      updatedAt: '2026-04-10T09:00:00Z',
    },
    {
      environmentId: 'env-dev',
      environmentName: 'dev-acme',
      enabled: true,
      activeDaysOfWeek: [1, 2, 3, 4, 5],
      activeFromHour: 9,
      activeToHour: 18,
      timezone: 'Europe/Lisbon',
      status: 'OverriddenUntil',
      overrideUntil: '2026-04-15T23:59:00Z',
      overrideReason: 'Extended testing session',
      estimatedSavingPercent: 62,
      updatedAt: '2026-04-15T10:00:00Z',
    },
  ],
  totalEstimatedSavingPercent: 59,
  generatedAt: '2026-04-15T12:00:00Z',
  simulatedNote: 'Simulated schedule data',
};

describe('NonProdSchedulerPage', () => {
  beforeEach(() => {
    queryClient.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getNonProdSchedules).mockImplementation(() => new Promise(() => {}));
    render(<NonProdSchedulerPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state on API failure', async () => {
    vi.mocked(platformAdminApi.getNonProdSchedules).mockRejectedValue(new Error('Network error'));
    render(<NonProdSchedulerPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('renders page title', async () => {
    vi.mocked(platformAdminApi.getNonProdSchedules).mockResolvedValue(mockSchedulesResponse);
    render(<NonProdSchedulerPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('title')).toBeDefined());
  });

  it('renders environment names', async () => {
    vi.mocked(platformAdminApi.getNonProdSchedules).mockResolvedValue(mockSchedulesResponse);
    render(<NonProdSchedulerPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('staging-acme')).toBeDefined();
      expect(screen.getByText('dev-acme')).toBeDefined();
    });
  });

  it('shows total savings percentage', async () => {
    vi.mocked(platformAdminApi.getNonProdSchedules).mockResolvedValue(mockSchedulesResponse);
    render(<NonProdSchedulerPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('59%')).toBeDefined());
  });

  it('shows override active status badge', async () => {
    vi.mocked(platformAdminApi.getNonProdSchedules).mockResolvedValue(mockSchedulesResponse);
    render(<NonProdSchedulerPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('status.OverriddenUntil')).toBeDefined());
  });

  it('shows simulated note', async () => {
    vi.mocked(platformAdminApi.getNonProdSchedules).mockResolvedValue(mockSchedulesResponse);
    render(<NonProdSchedulerPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('Simulated schedule data')).toBeDefined());
  });
});
