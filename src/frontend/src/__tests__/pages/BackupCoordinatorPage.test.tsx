import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { BackupCoordinatorPage } from '../../features/platform-admin/pages/BackupCoordinatorPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { BackupCoordinatorResponse } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => {
      if (opts) return `${key}:${JSON.stringify(opts)}`;
      return key;
    },
    i18n: { language: 'en' },
  }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getBackupStatus: vi.fn(),
    updateBackupSchedule: vi.fn(),
    runBackupNow: vi.fn(),
    getSupportBundles: vi.fn(),
    generateSupportBundle: vi.fn(),
    getSupportBundleDownloadUrl: vi.fn(),
    getDatabaseHealth: vi.fn(),
    getHardwareAssessment: vi.fn(),
    getPreflight: vi.fn(),
    getConfigHealth: vi.fn(),
    getPendingMigrations: vi.fn(),
    getNetworkPolicy: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';

function makeWrapper() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={qc}>{children}</QueryClientProvider>
  );
}

const mockSchedule = {
  enabled: true,
  cronExpression: '0 3 * * *',
  retentionDays: 30,
  destination: '/data/backups',
  compressionEnabled: true,
};

const mockBackupRecord = {
  id: 'bk-001',
  startedAt: new Date().toISOString(),
  completedAt: new Date().toISOString(),
  status: 'Success' as const,
  fileSizeGb: 1.2,
  durationSeconds: 45,
  destination: '/data/backups',
  checksumSha256: 'abc123',
  errorMessage: null,
};

const mockResponse: BackupCoordinatorResponse = {
  schedule: mockSchedule,
  recentBackups: [mockBackupRecord],
  lastSuccessfulBackup: mockBackupRecord,
};

const mockResponseNoBackups: BackupCoordinatorResponse = {
  schedule: { ...mockSchedule, enabled: false },
  recentBackups: [],
  lastSuccessfulBackup: null,
};

describe('BackupCoordinatorPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the page title and subtitle', async () => {
    vi.mocked(platformAdminApi.getBackupStatus).mockResolvedValue(mockResponse);

    render(<BackupCoordinatorPage />, { wrapper: makeWrapper() });

    expect(screen.getByText('backup.title')).toBeInTheDocument();
    expect(screen.getByText('backup.subtitle')).toBeInTheDocument();
  });

  it('shows last successful backup summary when available', async () => {
    vi.mocked(platformAdminApi.getBackupStatus).mockResolvedValue(mockResponse);

    render(<BackupCoordinatorPage />, { wrapper: makeWrapper() });

    await waitFor(() => {
      expect(screen.getByText('backup.lastSuccessfulLabel')).toBeInTheDocument();
    });
  });

  it('shows no backup warning when no backups exist', async () => {
    vi.mocked(platformAdminApi.getBackupStatus).mockResolvedValue(mockResponseNoBackups);

    render(<BackupCoordinatorPage />, { wrapper: makeWrapper() });

    await waitFor(() => {
      expect(screen.getByText('backup.noBackupYet')).toBeInTheDocument();
    });
  });

  it('shows empty history message when no backups', async () => {
    vi.mocked(platformAdminApi.getBackupStatus).mockResolvedValue(mockResponseNoBackups);

    render(<BackupCoordinatorPage />, { wrapper: makeWrapper() });

    await waitFor(() => {
      expect(screen.getByText('backup.emptyHistory')).toBeInTheDocument();
    });
  });

  it('shows the schedule form with save button', async () => {
    vi.mocked(platformAdminApi.getBackupStatus).mockResolvedValue(mockResponse);

    render(<BackupCoordinatorPage />, { wrapper: makeWrapper() });

    await waitFor(() => {
      expect(screen.getByText('backup.scheduleTitle')).toBeInTheDocument();
      expect(screen.getByText('backup.save')).toBeInTheDocument();
    });
  });

  it('calls runBackupNow when Run Now is clicked', async () => {
    vi.mocked(platformAdminApi.getBackupStatus).mockResolvedValue(mockResponse);
    vi.mocked(platformAdminApi.runBackupNow).mockResolvedValue(mockBackupRecord);

    render(<BackupCoordinatorPage />, { wrapper: makeWrapper() });

    await waitFor(() => {
      expect(screen.getAllByText('backup.runNow')[0]).toBeInTheDocument();
    });

    fireEvent.click(screen.getAllByText('backup.runNow')[0]);

    await waitFor(() => {
      expect(platformAdminApi.runBackupNow).toHaveBeenCalledTimes(1);
    });
  });

  it('shows backup history when backups exist', async () => {
    vi.mocked(platformAdminApi.getBackupStatus).mockResolvedValue(mockResponse);

    render(<BackupCoordinatorPage />, { wrapper: makeWrapper() });

    await waitFor(() => {
      expect(screen.getByText('backup.historyTitle')).toBeInTheDocument();
    });
  });
});
