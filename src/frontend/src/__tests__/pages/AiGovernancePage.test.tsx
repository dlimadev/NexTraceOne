import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { AiGovernancePage } from '../../features/platform-admin/pages/AiGovernancePage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { AiGovernanceDashboard } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, _opts?: Record<string, unknown>) => key,
    i18n: { language: 'en' },
  }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getAiGovernanceDashboard: vi.fn(),
    updateAiGovernanceConfig: vi.fn(),
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

const mockDashboard: AiGovernanceDashboard = {
  config: {
    groundingCheckEnabled: true,
    hallucinationFlagThreshold: 0.4,
    feedbackEnabled: true,
    autoSuspendOnHighHallucinationRate: false,
    highHallucinationThresholdPercent: 20,
    updatedAt: '2026-04-15T10:00:00Z',
  },
  modelStats: [
    {
      modelName: 'deepseek-r1:1.5b',
      totalResponses: 1240,
      goodPercent: 81.2,
      lowConfidencePercent: 12.4,
      hallucinationPercent: 6.4,
      negativeFeedbackCount: 18,
      averageConfidenceScore: 0.78,
      lastUpdated: '2026-04-15T10:00:00Z',
    },
    {
      modelName: 'llama3.2:3b',
      totalResponses: 320,
      goodPercent: 74.0,
      lowConfidencePercent: 18.0,
      hallucinationPercent: 8.0,
      negativeFeedbackCount: 7,
      averageConfidenceScore: 0.71,
      lastUpdated: '2026-04-15T10:00:00Z',
    },
  ],
  totalFeedbackCount: 1560,
  negativeFeedbackPercent: 8.5,
  topHallucinationPatterns: [
    'Referenced non-existent service "user-svc-v3"',
    'Cited breaking change in contract that does not exist',
  ],
  generatedAt: '2026-04-15T10:30:00Z',
  simulatedNote: 'Simulated data — connect real metrics for accuracy.',
};

const mockDashboardHighHallucination: AiGovernanceDashboard = {
  ...mockDashboard,
  modelStats: [
    {
      ...mockDashboard.modelStats[0],
      hallucinationPercent: 28.0,
    },
  ],
};

describe('AiGovernancePage', () => {
  beforeEach(() => {
    queryClient.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getAiGovernanceDashboard).mockImplementation(
      () => new Promise(() => {})
    );
    render(<AiGovernancePage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state on API failure', async () => {
    vi.mocked(platformAdminApi.getAiGovernanceDashboard).mockRejectedValue(new Error('Network error'));
    render(<AiGovernancePage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('renders page title', async () => {
    vi.mocked(platformAdminApi.getAiGovernanceDashboard).mockResolvedValue(mockDashboard);
    render(<AiGovernancePage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('title')).toBeDefined());
  });

  it('shows quality OK banner when within thresholds', async () => {
    vi.mocked(platformAdminApi.getAiGovernanceDashboard).mockResolvedValue(mockDashboard);
    render(<AiGovernancePage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('qualityOkMsg')).toBeDefined());
  });

  it('shows high hallucination warning when threshold exceeded', async () => {
    vi.mocked(platformAdminApi.getAiGovernanceDashboard).mockResolvedValue(
      mockDashboardHighHallucination
    );
    render(<AiGovernancePage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('highHallucinationWarning')).toBeDefined());
  });

  it('renders model quality table with model names', async () => {
    vi.mocked(platformAdminApi.getAiGovernanceDashboard).mockResolvedValue(mockDashboard);
    render(<AiGovernancePage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('deepseek-r1:1.5b')).toBeDefined();
      expect(screen.getByText('llama3.2:3b')).toBeDefined();
    });
  });

  it('renders top hallucination patterns', async () => {
    vi.mocked(platformAdminApi.getAiGovernanceDashboard).mockResolvedValue(mockDashboard);
    render(<AiGovernancePage />, { wrapper: Wrapper });
    await waitFor(() =>
      expect(
        screen.getByText('Referenced non-existent service "user-svc-v3"')
      ).toBeDefined()
    );
  });

  it('renders simulated note', async () => {
    vi.mocked(platformAdminApi.getAiGovernanceDashboard).mockResolvedValue(mockDashboard);
    render(<AiGovernancePage />, { wrapper: Wrapper });
    await waitFor(() =>
      expect(
        screen.getByText('Simulated data — connect real metrics for accuracy.')
      ).toBeDefined()
    );
  });
});
