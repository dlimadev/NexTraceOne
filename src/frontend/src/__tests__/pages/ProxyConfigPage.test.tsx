import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ProxyConfigPage } from '../../features/platform-admin/pages/ProxyConfigPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { ProxyConfig } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, _opts?: Record<string, unknown>) => key,
    i18n: { language: 'en' },
  }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
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

const mockConfigNotSet: ProxyConfig = {
  bypassList: [],
  hasPassword: false,
  hasCaCertificate: false,
  status: 'NotConfigured',
};

const mockConfigOk: ProxyConfig = {
  proxyUrl: 'http://proxy.acme.com:3128',
  bypassList: ['localhost', '*.acme.internal'],
  username: 'svc-nextraceone',
  hasPassword: true,
  customCaCertificatePath: '/etc/nextraceone/custom-ca.pem',
  hasCaCertificate: true,
  status: 'TestPassed',
  lastTestedAt: '2026-04-15T10:00:00Z',
  updatedAt: '2026-04-14T09:00:00Z',
};

const mockConfigFailed: ProxyConfig = {
  ...mockConfigOk,
  status: 'TestFailed',
  lastTestError: 'Connection refused: proxy.acme.com:3128',
};

describe('ProxyConfigPage', () => {
  beforeEach(() => {
    queryClient.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getProxyConfig).mockImplementation(() => new Promise(() => {}));
    render(<ProxyConfigPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state on API failure', async () => {
    vi.mocked(platformAdminApi.getProxyConfig).mockRejectedValue(new Error('Network error'));
    render(<ProxyConfigPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('renders page title', async () => {
    vi.mocked(platformAdminApi.getProxyConfig).mockResolvedValue(mockConfigNotSet);
    render(<ProxyConfigPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('title')).toBeDefined());
  });

  it('shows not-configured banner when proxy is not set', async () => {
    vi.mocked(platformAdminApi.getProxyConfig).mockResolvedValue(mockConfigNotSet);
    render(<ProxyConfigPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('notConfiguredMsg')).toBeDefined());
  });

  it('shows test-passed banner when proxy test succeeds', async () => {
    vi.mocked(platformAdminApi.getProxyConfig).mockResolvedValue(mockConfigOk);
    render(<ProxyConfigPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('testPassedBanner')).toBeDefined());
  });

  it('shows test-failed banner with error detail', async () => {
    vi.mocked(platformAdminApi.getProxyConfig).mockResolvedValue(mockConfigFailed);
    render(<ProxyConfigPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('testFailedBanner')).toBeDefined();
      expect(screen.getByText('Connection refused: proxy.acme.com:3128')).toBeDefined();
    });
  });

  it('displays proxy URL when configured', async () => {
    vi.mocked(platformAdminApi.getProxyConfig).mockResolvedValue(mockConfigOk);
    render(<ProxyConfigPage />, { wrapper: Wrapper });
    await waitFor(() =>
      expect(screen.getByText('http://proxy.acme.com:3128')).toBeDefined()
    );
  });

  it('displays CA certificate path when configured', async () => {
    vi.mocked(platformAdminApi.getProxyConfig).mockResolvedValue(mockConfigOk);
    render(<ProxyConfigPage />, { wrapper: Wrapper });
    await waitFor(() =>
      expect(screen.getByText('/etc/nextraceone/custom-ca.pem')).toBeDefined()
    );
  });
});
