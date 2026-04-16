import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { RightsizingPage } from '../../features/platform-admin/pages/RightsizingPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { RightsizingReport } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getRightsizingReport: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={qc}>{children}</QueryClientProvider>
);

const mockReport: RightsizingReport = {
  recommendations: [
    {
      serviceId: 'svc-1',
      serviceName: 'Order Service',
      teamName: 'Platform Team',
      resource: 'CPU',
      currentAllocation: 4,
      recommendedAllocation: 2,
      unit: 'cores',
      p95Usage: 1.2,
      p99Usage: 1.8,
      safetyMarginPercent: 20,
      savingPercent: 50,
      reliabilityImpact: 'Low',
      oomEventsLast30Days: 0,
      sloAtRisk: false,
      analysisDays: 30,
    },
    {
      serviceId: 'svc-2',
      serviceName: 'Notification Service',
      teamName: 'Core Team',
      resource: 'Memory',
      currentAllocation: 8,
      recommendedAllocation: 4,
      unit: 'GB',
      p95Usage: 2.8,
      p99Usage: 3.4,
      safetyMarginPercent: 20,
      savingPercent: 50,
      reliabilityImpact: 'Medium',
      oomEventsLast30Days: 0,
      sloAtRisk: false,
      analysisDays: 30,
    },
  ],
  totalServicesAnalysed: 15,
  totalSavingEstimateCpuPercent: 25,
  totalSavingEstimateMemoryPercent: 18,
  safetyMarginPercent: 20,
  generatedAt: '2026-04-15T12:00:00Z',
  simulatedNote: 'Simulated rightsizing data',
};

const mockEmptyReport: RightsizingReport = {
  recommendations: [],
  totalServicesAnalysed: 10,
  totalSavingEstimateCpuPercent: 0,
  totalSavingEstimateMemoryPercent: 0,
  safetyMarginPercent: 20,
  generatedAt: '2026-04-15T12:00:00Z',
  simulatedNote: 'Simulated rightsizing data',
};

describe('RightsizingPage', () => {
  beforeEach(() => {
    qc.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getRightsizingReport).mockImplementation(() => new Promise(() => {}));
    render(<RightsizingPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state', async () => {
    vi.mocked(platformAdminApi.getRightsizingReport).mockRejectedValue(new Error('fail'));
    render(<RightsizingPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('renders page title', async () => {
    vi.mocked(platformAdminApi.getRightsizingReport).mockResolvedValue(mockReport);
    render(<RightsizingPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('title')).toBeDefined());
  });

  it('renders summary cards', async () => {
    vi.mocked(platformAdminApi.getRightsizingReport).mockResolvedValue(mockReport);
    render(<RightsizingPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('statServices')).toBeDefined();
      expect(screen.getByText('statRecs')).toBeDefined();
    });
  });

  it('renders service names in table', async () => {
    vi.mocked(platformAdminApi.getRightsizingReport).mockResolvedValue(mockReport);
    render(<RightsizingPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('Order Service')).toBeDefined();
      expect(screen.getByText('Notification Service')).toBeDefined();
    });
  });

  it('shows empty state when no recommendations', async () => {
    vi.mocked(platformAdminApi.getRightsizingReport).mockResolvedValue(mockEmptyReport);
    render(<RightsizingPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('noRecs')).toBeDefined());
  });

  it('shows simulated note', async () => {
    vi.mocked(platformAdminApi.getRightsizingReport).mockResolvedValue(mockReport);
    render(<RightsizingPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText(/Simulated rightsizing data/)).toBeDefined());
  });
});
