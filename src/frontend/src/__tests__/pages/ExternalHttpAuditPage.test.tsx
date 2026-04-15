import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ExternalHttpAuditPage } from '../../features/platform-admin/pages/ExternalHttpAuditPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { ExternalHttpAuditResponse } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, _opts?: Record<string, unknown>) => key,
    i18n: { language: 'en' },
  }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getExternalHttpAudit: vi.fn(),
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
    getEnvironmentPolicies: vi.fn(),
    updateEnvironmentPolicy: vi.fn(),
    getNonProdSchedules: vi.fn(),
    updateNonProdSchedule: vi.fn(),
    overrideNonProdSchedule: vi.fn(),
    getCapacityForecast: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
);

const mockAuditResponse: ExternalHttpAuditResponse = {
  entries: [
    {
      id: 'entry-1',
      eventType: 'ExternalHttpCall',
      timestamp: '2026-04-15T10:23:45Z',
      destination: 'https://api.openai.com',
      method: 'POST',
      path: '/v1/chat/completions',
      tenantId: 'acme-corp',
      userId: 'usr_abc123',
      context: 'AiAssistant',
      requestSizeBytes: 1240,
      responseStatus: 200,
      durationMs: 842,
      blocked: false,
    },
    {
      id: 'entry-2',
      eventType: 'BlockedByAirGap',
      timestamp: '2026-04-15T11:00:00Z',
      destination: 'https://api.openai.com',
      method: 'POST',
      path: '/v1/chat/completions',
      tenantId: 'acme-corp',
      context: 'AiAssistant',
      requestSizeBytes: 800,
      blocked: true,
    },
  ],
  total: 2,
  page: 1,
  pageSize: 20,
  generatedAt: '2026-04-15T12:00:00Z',
  simulatedNote: 'Simulated data',
};

describe('ExternalHttpAuditPage', () => {
  beforeEach(() => {
    queryClient.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getExternalHttpAudit).mockImplementation(() => new Promise(() => {}));
    render(<ExternalHttpAuditPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state on API failure', async () => {
    vi.mocked(platformAdminApi.getExternalHttpAudit).mockRejectedValue(new Error('Network error'));
    render(<ExternalHttpAuditPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('renders page title', async () => {
    vi.mocked(platformAdminApi.getExternalHttpAudit).mockResolvedValue(mockAuditResponse);
    render(<ExternalHttpAuditPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('title')).toBeDefined());
  });

  it('shows total calls count', async () => {
    vi.mocked(platformAdminApi.getExternalHttpAudit).mockResolvedValue(mockAuditResponse);
    render(<ExternalHttpAuditPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('2')).toBeDefined());
  });

  it('renders destination in the table', async () => {
    vi.mocked(platformAdminApi.getExternalHttpAudit).mockResolvedValue(mockAuditResponse);
    render(<ExternalHttpAuditPage />, { wrapper: Wrapper });
    await waitFor(() =>
      expect(screen.getAllByText('https://api.openai.com').length).toBeGreaterThan(0)
    );
  });

  it('shows blocked badge for blocked entries', async () => {
    vi.mocked(platformAdminApi.getExternalHttpAudit).mockResolvedValue(mockAuditResponse);
    render(<ExternalHttpAuditPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('blocked')).toBeDefined());
  });

  it('shows simulated note', async () => {
    vi.mocked(platformAdminApi.getExternalHttpAudit).mockResolvedValue(mockAuditResponse);
    render(<ExternalHttpAuditPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('Simulated data')).toBeDefined());
  });
});
