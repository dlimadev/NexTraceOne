import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { WasteDetectionPage } from '../../features/governance/pages/WasteDetectionPage';
import { finOpsApi } from '../../features/governance/api/finOps';
import type { WasteSignalsResponse } from '../../features/governance/api/finOps';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en' },
  }),
}));

vi.mock('../../features/governance/api/finOps', () => ({
  finOpsApi: {
    getSummary: vi.fn(),
    getServiceFinOps: vi.fn(),
    getTeamFinOps: vi.fn(),
    getDomainFinOps: vi.fn(),
    getTrends: vi.fn(),
    getWasteSignals: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
);

const mockNoWaste: WasteSignalsResponse = {
  totalWaste: 0,
  signalCount: 0,
  signals: [],
  byType: [],
  generatedAt: new Date().toISOString(),
};

const mockWithWaste: WasteSignalsResponse = {
  totalWaste: 1250.5,
  signalCount: 2,
  signals: [
    {
      signalId: 'sig-001',
      serviceId: 'svc-001',
      serviceName: 'payment-service',
      domain: 'Finance',
      team: 'Payments',
      type: 'IdleResources',
      description: 'Service has low utilization',
      pattern: 'CPU < 5% for 7 days',
      estimatedWaste: 800,
      severity: 'High',
      detectedAt: new Date().toISOString(),
      correlatedCause: 'No traffic after last deploy',
    },
    {
      signalId: 'sig-002',
      serviceId: 'svc-002',
      serviceName: 'notification-service',
      domain: 'Platform',
      team: 'Core',
      type: 'OverProvisioned',
      description: 'Memory is over-provisioned',
      pattern: 'Memory usage < 10% for 14 days',
      estimatedWaste: 450.5,
      severity: 'Medium',
      detectedAt: new Date().toISOString(),
      correlatedCause: null,
    },
  ],
  byType: [
    { type: 'IdleResources', count: 1, totalWaste: 800 },
    { type: 'OverProvisioned', count: 1, totalWaste: 450.5 },
  ],
  generatedAt: new Date().toISOString(),
};

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
describe('WasteDetectionPage', () => {
  beforeEach(() => {
    queryClient.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(finOpsApi.getWasteSignals).mockImplementation(
      () => new Promise(() => {})
    );
    render(<WasteDetectionPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows no waste state when no signals', async () => {
    vi.mocked(finOpsApi.getWasteSignals).mockResolvedValue(mockNoWaste);
    render(<WasteDetectionPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('noWaste')).toBeDefined());
  });

  it('renders summary stats when waste detected', async () => {
    vi.mocked(finOpsApi.getWasteSignals).mockResolvedValue(mockWithWaste);
    render(<WasteDetectionPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('2')).toBeDefined());
  });

  it('renders waste signal cards', async () => {
    vi.mocked(finOpsApi.getWasteSignals).mockResolvedValue(mockWithWaste);
    render(<WasteDetectionPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('payment-service')).toBeDefined());
    expect(screen.getByText('notification-service')).toBeDefined();
  });

  it('shows correlated cause when present', async () => {
    vi.mocked(finOpsApi.getWasteSignals).mockResolvedValue(mockWithWaste);
    render(<WasteDetectionPage />, { wrapper: Wrapper });
    await waitFor(() =>
      expect(screen.getByText((content) => content.includes('No traffic after last deploy'))).toBeDefined()
    );
  });

  it('shows error state on API failure', async () => {
    vi.mocked(finOpsApi.getWasteSignals).mockRejectedValue(new Error('Network error'));
    render(<WasteDetectionPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });
});
