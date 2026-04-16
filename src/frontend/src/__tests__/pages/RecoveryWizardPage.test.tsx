import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { RecoveryWizardPage } from '../../features/platform-admin/pages/RecoveryWizardPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { RestorePointsResponse } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en' },
  }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getRestorePoints: vi.fn(),
    initiateRecovery: vi.fn(),
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
    getGreenOpsReport: vi.fn(),
    updateGreenOpsConfig: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
);

const mockRestorePoints: RestorePointsResponse = {
  totalCount: 2,
  oldestAvailable: '2026-04-01T03:00:00Z',
  latestAvailable: '2026-04-15T03:00:00Z',
  restorePoints: [
    {
      id: 'rp-001',
      timestamp: '2026-04-15T03:00:00Z',
      sizeMb: 2048,
      status: 'Available',
      checksum: 'sha256:abc123',
      version: '2.4.1',
      schemasIncluded: ['nextraceone_identity', 'nextraceone_catalog'],
    },
    {
      id: 'rp-002',
      timestamp: '2026-04-14T03:00:00Z',
      sizeMb: 2010,
      status: 'Available',
      checksum: 'sha256:def456',
      version: '2.4.0',
      schemasIncluded: ['nextraceone_identity', 'nextraceone_catalog', 'nextraceone_operations'],
    },
  ],
};

const mockEmpty: RestorePointsResponse = {
  totalCount: 0,
  restorePoints: [],
};

describe('RecoveryWizardPage', () => {
  beforeEach(() => {
    queryClient.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getRestorePoints).mockImplementation(
      () => new Promise(() => {})
    );
    render(<RecoveryWizardPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state on API failure', async () => {
    vi.mocked(platformAdminApi.getRestorePoints).mockRejectedValue(new Error('Network error'));
    render(<RecoveryWizardPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('shows no restore points message when empty', async () => {
    vi.mocked(platformAdminApi.getRestorePoints).mockResolvedValue(mockEmpty);
    render(<RecoveryWizardPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('noRestorePoints')).toBeDefined());
  });

  it('renders restore point list', async () => {
    vi.mocked(platformAdminApi.getRestorePoints).mockResolvedValue(mockRestorePoints);
    render(<RecoveryWizardPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getAllByText(/Available/).length).toBeGreaterThan(0));
  });

  it('shows page title', async () => {
    vi.mocked(platformAdminApi.getRestorePoints).mockResolvedValue(mockRestorePoints);
    render(<RecoveryWizardPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('title')).toBeDefined());
  });

  it('shows warning banner', async () => {
    vi.mocked(platformAdminApi.getRestorePoints).mockResolvedValue(mockRestorePoints);
    render(<RecoveryWizardPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('warningBanner')).toBeDefined());
  });

  it('renders step indicator labels', async () => {
    vi.mocked(platformAdminApi.getRestorePoints).mockResolvedValue(mockRestorePoints);
    render(<RecoveryWizardPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('step1Label')).toBeDefined());
  });
});
