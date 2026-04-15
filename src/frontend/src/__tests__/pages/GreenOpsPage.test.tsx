import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { GreenOpsPage } from '../../features/platform-admin/pages/GreenOpsPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { GreenOpsReport } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, _opts?: Record<string, unknown>) => key,
    i18n: { language: 'en' },
  }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
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
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
);

const mockReport: GreenOpsReport = {
  generatedAt: '2026-04-15T00:00:00Z',
  period: 'April 2026',
  totalKgCo2: 33.6,
  equivalentKmByCar: 168,
  esgTargetKgCo2: 30,
  percentAboveTarget: 12,
  config: {
    intensityFactorKgPerKwh: 0.233,
    esgTargetKgCo2PerMonth: 30,
    datacenterRegion: 'EU West',
    updatedAt: '2026-01-01T00:00:00Z',
  },
  topServices: [
    {
      serviceId: 'svc-001',
      serviceName: 'payment-api',
      teamName: 'Payments',
      carbonKgCo2: 12.4,
      changePercent: 8,
      cpuHours: 840,
      memoryGbHours: 1200,
      networkGb: 50,
      period: 'April 2026',
    },
    {
      serviceId: 'svc-002',
      serviceName: 'order-service',
      teamName: 'Orders',
      carbonKgCo2: 8.1,
      changePercent: -5,
      cpuHours: 600,
      memoryGbHours: 800,
      networkGb: 30,
      period: 'April 2026',
    },
  ],
  trend: [
    { month: 'January', totalKgCo2: 28 },
    { month: 'February', totalKgCo2: 30 },
    { month: 'March', totalKgCo2: 31 },
    { month: 'April', totalKgCo2: 33.6 },
  ],
  simulatedNote: 'Simulated data — connect real metrics for accuracy.',
};

describe('GreenOpsPage', () => {
  beforeEach(() => {
    queryClient.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getGreenOpsReport).mockImplementation(
      () => new Promise(() => {})
    );
    render(<GreenOpsPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state on API failure', async () => {
    vi.mocked(platformAdminApi.getGreenOpsReport).mockRejectedValue(new Error('Network error'));
    render(<GreenOpsPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('shows page title', async () => {
    vi.mocked(platformAdminApi.getGreenOpsReport).mockResolvedValue(mockReport);
    render(<GreenOpsPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('title')).toBeDefined());
  });

  it('renders top services table', async () => {
    vi.mocked(platformAdminApi.getGreenOpsReport).mockResolvedValue(mockReport);
    render(<GreenOpsPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('payment-api')).toBeDefined());
    expect(screen.getByText('order-service')).toBeDefined();
  });

  it('shows above-target warning banner when over ESG goal', async () => {
    vi.mocked(platformAdminApi.getGreenOpsReport).mockResolvedValue(mockReport);
    render(<GreenOpsPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('aboveTargetMsg')).toBeDefined());
  });

  it('shows below-target banner when meeting ESG goal', async () => {
    vi.mocked(platformAdminApi.getGreenOpsReport).mockResolvedValue({
      ...mockReport,
      percentAboveTarget: 0,
    });
    render(<GreenOpsPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('belowTargetMsg')).toBeDefined());
  });

  it('renders simulated note', async () => {
    vi.mocked(platformAdminApi.getGreenOpsReport).mockResolvedValue(mockReport);
    render(<GreenOpsPage />, { wrapper: Wrapper });
    await waitFor(() =>
      expect(screen.getByText('Simulated data — connect real metrics for accuracy.')).toBeDefined()
    );
  });
});
