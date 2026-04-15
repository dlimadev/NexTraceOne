import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { CapacityForecastPage } from '../../features/platform-admin/pages/CapacityForecastPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { CapacityForecastResponse } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, _opts?: Record<string, unknown>) => key,
    i18n: { language: 'en' },
  }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getCapacityForecast: vi.fn(),
    getExternalHttpAudit: vi.fn(),
    getEnvironmentPolicies: vi.fn(),
    updateEnvironmentPolicy: vi.fn(),
    getNonProdSchedules: vi.fn(),
    updateNonProdSchedule: vi.fn(),
    overrideNonProdSchedule: vi.fn(),
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
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
);

const mockForecastAllGood: CapacityForecastResponse = {
  forecasts: [
    {
      resource: 'Elasticsearch Disk',
      current: 148,
      capacity: 500,
      unit: 'GB',
      weeklyGrowthRate: 12,
      estimatedFullDate: '2026-07-29T00:00:00Z',
      daysUntilFull: 105,
      riskLevel: 'Low',
      recommendation: 'Plan storage expansion before July 15.',
    },
    {
      resource: 'PostgreSQL Disk',
      current: 24,
      capacity: 100,
      unit: 'GB',
      weeklyGrowthRate: 2.1,
      estimatedFullDate: '2026-10-06T00:00:00Z',
      daysUntilFull: 174,
      riskLevel: 'Low',
    },
  ],
  analysisWeeks: 8,
  nextReviewDate: '2026-05-15T00:00:00Z',
  generatedAt: '2026-04-15T12:00:00Z',
  simulatedNote: 'Simulated forecast data',
};

const mockForecastCritical: CapacityForecastResponse = {
  forecasts: [
    {
      resource: 'Elasticsearch Disk',
      current: 480,
      capacity: 500,
      unit: 'GB',
      weeklyGrowthRate: 15,
      estimatedFullDate: '2026-05-01T00:00:00Z',
      daysUntilFull: 16,
      riskLevel: 'Critical',
      recommendation: 'Expand storage immediately.',
    },
  ],
  analysisWeeks: 8,
  nextReviewDate: '2026-05-15T00:00:00Z',
  generatedAt: '2026-04-15T12:00:00Z',
  simulatedNote: 'Simulated forecast data',
};

describe('CapacityForecastPage', () => {
  beforeEach(() => {
    queryClient.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getCapacityForecast).mockImplementation(() => new Promise(() => {}));
    render(<CapacityForecastPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state on API failure', async () => {
    vi.mocked(platformAdminApi.getCapacityForecast).mockRejectedValue(new Error('Network error'));
    render(<CapacityForecastPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('renders page title', async () => {
    vi.mocked(platformAdminApi.getCapacityForecast).mockResolvedValue(mockForecastAllGood);
    render(<CapacityForecastPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('title')).toBeDefined());
  });

  it('shows all-good banner when no high-risk resources', async () => {
    vi.mocked(platformAdminApi.getCapacityForecast).mockResolvedValue(mockForecastAllGood);
    render(<CapacityForecastPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('allGood')).toBeDefined());
  });

  it('shows attention banner when critical resources exist', async () => {
    vi.mocked(platformAdminApi.getCapacityForecast).mockResolvedValue(mockForecastCritical);
    render(<CapacityForecastPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('attentionNeeded')).toBeDefined());
  });

  it('renders resource names', async () => {
    vi.mocked(platformAdminApi.getCapacityForecast).mockResolvedValue(mockForecastAllGood);
    render(<CapacityForecastPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('Elasticsearch Disk')).toBeDefined();
      expect(screen.getByText('PostgreSQL Disk')).toBeDefined();
    });
  });

  it('renders recommendation text', async () => {
    vi.mocked(platformAdminApi.getCapacityForecast).mockResolvedValue(mockForecastAllGood);
    render(<CapacityForecastPage />, { wrapper: Wrapper });
    await waitFor(() =>
      expect(screen.getByText('💡 Plan storage expansion before July 15.')).toBeDefined()
    );
  });

  it('shows simulated note', async () => {
    vi.mocked(platformAdminApi.getCapacityForecast).mockResolvedValue(mockForecastAllGood);
    render(<CapacityForecastPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('Simulated forecast data')).toBeDefined());
  });
});
