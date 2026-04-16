import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { SupportBundlePage } from '../../features/platform-admin/pages/SupportBundlePage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { SupportBundleEntry, SupportBundleListResponse } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => {
      if (opts && 'returnObjects' in opts) return [];
      if (opts) return `${key}:${JSON.stringify(opts)}`;
      return key;
    },
    i18n: { language: 'en' },
  }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getSupportBundles: vi.fn(),
    generateSupportBundle: vi.fn(),
    getSupportBundleDownloadUrl: vi.fn((id: string) => `/api/v1/admin/support-bundles/${id}/download`),
    getDatabaseHealth: vi.fn(),
    getHardwareAssessment: vi.fn(),
    getPreflight: vi.fn(),
    getConfigHealth: vi.fn(),
    getPendingMigrations: vi.fn(),
    getNetworkPolicy: vi.fn(),
    getBackupStatus: vi.fn(),
    updateBackupSchedule: vi.fn(),
    runBackupNow: vi.fn(),
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

const mockBundle: SupportBundleEntry = {
  id: 'bundle-abc',
  generatedAt: new Date().toISOString(),
  generatedBy: 'admin@acme.com',
  fileSizeKb: 512,
  includedFiles: ['system-info.json', 'recent-errors.txt'],
};

const mockList: SupportBundleListResponse = {
  bundles: [mockBundle],
};

const mockEmpty: SupportBundleListResponse = { bundles: [] };

describe('SupportBundlePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the page title and subtitle', async () => {
    vi.mocked(platformAdminApi.getSupportBundles).mockResolvedValue(mockList);

    render(<SupportBundlePage />, { wrapper: makeWrapper() });

    expect(screen.getByText('supportBundle.title')).toBeInTheDocument();
    expect(screen.getByText('supportBundle.subtitle')).toBeInTheDocument();
  });

  it('renders the generate button', async () => {
    vi.mocked(platformAdminApi.getSupportBundles).mockResolvedValue(mockEmpty);

    render(<SupportBundlePage />, { wrapper: makeWrapper() });

    expect(screen.getByText('supportBundle.generate')).toBeInTheDocument();
  });

  it('shows empty history message when no bundles exist', async () => {
    vi.mocked(platformAdminApi.getSupportBundles).mockResolvedValue(mockEmpty);

    render(<SupportBundlePage />, { wrapper: makeWrapper() });

    await waitFor(() => {
      expect(screen.getByText('supportBundle.emptyHistory')).toBeInTheDocument();
    });
  });

  it('shows bundle list when bundles exist', async () => {
    vi.mocked(platformAdminApi.getSupportBundles).mockResolvedValue(mockList);

    render(<SupportBundlePage />, { wrapper: makeWrapper() });

    await waitFor(() => {
      expect(screen.getByText('supportBundle.download')).toBeInTheDocument();
    });
  });

  it('calls generateSupportBundle on button click', async () => {
    vi.mocked(platformAdminApi.getSupportBundles).mockResolvedValue(mockEmpty);
    vi.mocked(platformAdminApi.generateSupportBundle).mockResolvedValue(mockBundle);

    render(<SupportBundlePage />, { wrapper: makeWrapper() });

    fireEvent.click(screen.getByText('supportBundle.generate'));

    await waitFor(() => {
      expect(platformAdminApi.generateSupportBundle).toHaveBeenCalledTimes(1);
    });
  });

  it('shows ready banner after successful generation', async () => {
    vi.mocked(platformAdminApi.getSupportBundles).mockResolvedValue(mockEmpty);
    vi.mocked(platformAdminApi.generateSupportBundle).mockResolvedValue(mockBundle);

    render(<SupportBundlePage />, { wrapper: makeWrapper() });

    fireEvent.click(screen.getByText('supportBundle.generate'));

    await waitFor(() => {
      expect(screen.getByText('supportBundle.readyTitle')).toBeInTheDocument();
    });
  });

  it('shows security note', async () => {
    vi.mocked(platformAdminApi.getSupportBundles).mockResolvedValue(mockEmpty);

    render(<SupportBundlePage />, { wrapper: makeWrapper() });

    expect(screen.getByText('supportBundle.securityNote')).toBeInTheDocument();
  });
});
