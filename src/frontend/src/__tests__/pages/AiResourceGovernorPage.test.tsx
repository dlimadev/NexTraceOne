import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { AiResourceGovernorPage } from '../../features/platform-admin/pages/AiResourceGovernorPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { AiGovernorStatus } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, _opts?: Record<string, unknown>) => key,
    i18n: { language: 'en' },
  }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getAiGovernorStatus: vi.fn(),
    updateAiGovernorConfig: vi.fn(),
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
    getAiGovernanceDashboard: vi.fn(),
    updateAiGovernanceConfig: vi.fn(),
    getProxyConfig: vi.fn(),
    updateProxyConfig: vi.fn(),
    testProxyConnectivity: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
);

const mockStatus: AiGovernorStatus = {
  config: {
    maxConcurrency: 3,
    inferenceTimeoutSeconds: 120,
    queueTimeoutSeconds: 30,
    circuitBreakerEnabled: true,
    circuitBreakerErrorThresholdPercent: 50,
    circuitBreakerResetAfterMinutes: 2,
    priorityQueueEnabled: true,
    updatedAt: '2026-04-15T10:00:00Z',
  },
  metrics: {
    activeRequests: 1,
    queueDepth: 0,
    circuitBreakerState: 'Closed',
    latencyP95Ms: 850,
    errorRatePercent: 2.5,
    totalRequestsLast5Min: 42,
    rejectedRequestsLast5Min: 0,
    sampledAt: '2026-04-15T10:30:00Z',
  },
};

const mockStatusCbOpen: AiGovernorStatus = {
  ...mockStatus,
  metrics: {
    ...mockStatus.metrics,
    circuitBreakerState: 'Open',
    circuitBreakerOpenSince: '2026-04-15T10:25:00Z',
    errorRatePercent: 65,
    rejectedRequestsLast5Min: 12,
  },
};

describe('AiResourceGovernorPage', () => {
  beforeEach(() => {
    queryClient.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getAiGovernorStatus).mockImplementation(() => new Promise(() => {}));
    render(<AiResourceGovernorPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state on API failure', async () => {
    vi.mocked(platformAdminApi.getAiGovernorStatus).mockRejectedValue(new Error('Network error'));
    render(<AiResourceGovernorPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('renders page title', async () => {
    vi.mocked(platformAdminApi.getAiGovernorStatus).mockResolvedValue(mockStatus);
    render(<AiResourceGovernorPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('title')).toBeDefined());
  });

  it('shows circuit breaker closed banner when CB is closed', async () => {
    vi.mocked(platformAdminApi.getAiGovernorStatus).mockResolvedValue(mockStatus);
    render(<AiResourceGovernorPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('cbClosedMsg')).toBeDefined());
  });

  it('shows circuit breaker open banner when CB is open', async () => {
    vi.mocked(platformAdminApi.getAiGovernorStatus).mockResolvedValue(mockStatusCbOpen);
    render(<AiResourceGovernorPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('cbOpenTitle')).toBeDefined());
  });

  it('renders metrics cards with values', async () => {
    vi.mocked(platformAdminApi.getAiGovernorStatus).mockResolvedValue(mockStatus);
    render(<AiResourceGovernorPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('850 ms')).toBeDefined();
      expect(screen.getByText('2.5%')).toBeDefined();
    });
  });

  it('shows config values in read mode', async () => {
    vi.mocked(platformAdminApi.getAiGovernorStatus).mockResolvedValue(mockStatus);
    render(<AiResourceGovernorPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('3')).toBeDefined();
      expect(screen.getByText('120s')).toBeDefined();
    });
  });
});
