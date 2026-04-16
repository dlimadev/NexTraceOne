import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { DoraAdminDashboardPage } from '../../features/platform-admin/pages/DoraAdminDashboardPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { DoraAdminMetricsResponse } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getDoraAdminMetrics: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={qc}>{children}</QueryClientProvider>
);

const mockMetric = (name: string) => ({
  name,
  value: '3.5',
  unit: 'per day',
  rating: 'Elite' as const,
  trend: 5.2,
  trendDirection: 'up' as const,
});

const mockData: DoraAdminMetricsResponse = {
  deploymentFrequency: mockMetric('Deployment Frequency'),
  leadTime: mockMetric('Lead Time'),
  mttr: { ...mockMetric('MTTR'), unit: 'minutes', trendDirection: 'down' },
  changeFailureRate: { ...mockMetric('CFR'), unit: '%', trendDirection: 'down' },
  environment: 'production',
  timeRangeDays: 30,
  dataSource: 'PostgreSQL (simulated)',
  lastUpdatedAt: '2026-04-15T12:00:00Z',
  simulatedNote: 'Simulated DORA metrics data',
};

describe('DoraAdminDashboardPage', () => {
  beforeEach(() => {
    qc.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getDoraAdminMetrics).mockImplementation(() => new Promise(() => {}));
    render(<DoraAdminDashboardPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state', async () => {
    vi.mocked(platformAdminApi.getDoraAdminMetrics).mockRejectedValue(new Error('fail'));
    render(<DoraAdminDashboardPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('renders page title', async () => {
    vi.mocked(platformAdminApi.getDoraAdminMetrics).mockResolvedValue(mockData);
    render(<DoraAdminDashboardPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('title')).toBeDefined());
  });

  it('renders DORA metric cards', async () => {
    vi.mocked(platformAdminApi.getDoraAdminMetrics).mockResolvedValue(mockData);
    render(<DoraAdminDashboardPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('metricDeployFreq')).toBeDefined();
      expect(screen.getByText('metricLeadTime')).toBeDefined();
      expect(screen.getByText('metricMttr')).toBeDefined();
      expect(screen.getByText('metricCfr')).toBeDefined();
    });
  });

  it('renders metric values', async () => {
    vi.mocked(platformAdminApi.getDoraAdminMetrics).mockResolvedValue(mockData);
    render(<DoraAdminDashboardPage />, { wrapper: Wrapper });
    await waitFor(() => {
      const values = screen.getAllByText('3.5');
      expect(values.length).toBeGreaterThan(0);
    });
  });

  it('renders data freshness section', async () => {
    vi.mocked(platformAdminApi.getDoraAdminMetrics).mockResolvedValue(mockData);
    render(<DoraAdminDashboardPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('PostgreSQL (simulated)')).toBeDefined();
    });
  });

  it('shows simulated note', async () => {
    vi.mocked(platformAdminApi.getDoraAdminMetrics).mockResolvedValue(mockData);
    render(<DoraAdminDashboardPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText(/Simulated DORA metrics data/)).toBeDefined());
  });
});
