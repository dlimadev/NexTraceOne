import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { StartupReportPage } from '../../features/platform-admin/pages/StartupReportPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { StartupReportListResponse } from '../../features/platform-admin/api/platformAdmin';

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
    getStartupReports: vi.fn(),
    getSupportBundles: vi.fn(),
    generateSupportBundle: vi.fn(),
    getSupportBundleDownloadUrl: vi.fn(),
    getDatabaseHealth: vi.fn(),
    getHardwareAssessment: vi.fn(),
    getPreflight: vi.fn(),
    getConfigHealth: vi.fn(),
    getPendingMigrations: vi.fn(),
    getNetworkPolicy: vi.fn(),
    getBackupStatus: vi.fn(),
    updateBackupSchedule: vi.fn(),
    runBackupNow: vi.fn(),
    getResourceBudget: vi.fn(),
    updateTenantQuota: vi.fn(),
    getElasticsearchManager: vi.fn(),
    updateIlmPolicy: vi.fn(),
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

const mockReport = {
  id: 'rep-001',
  startedAt: '2026-04-15T09:00:00Z',
  version: '2.4.1',
  build: '20260415.1',
  environment: 'Production',
  hostname: 'nxt-prod-01',
  migrationsApplied: 0,
  migrationsTotal: 234,
  modulesRegistered: 12,
  configuration: {
    smtpConfigured: true,
    ollamaConfigured: false,
    elasticsearchConfigured: true,
    corsOrigins: ['https://app.acme.com'],
  },
  warnings: ['OTel Collector não acessível'],
};

const mockFull: StartupReportListResponse = { reports: [mockReport] };
const mockEmpty: StartupReportListResponse = { reports: [] };

describe('StartupReportPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders title and subtitle', () => {
    vi.mocked(platformAdminApi.getStartupReports).mockResolvedValue(mockFull);
    render(<StartupReportPage />, { wrapper: makeWrapper() });
    expect(screen.getByText('startupReport.title')).toBeInTheDocument();
    expect(screen.getByText('startupReport.subtitle')).toBeInTheDocument();
  });

  it('shows refresh button', () => {
    vi.mocked(platformAdminApi.getStartupReports).mockResolvedValue(mockEmpty);
    render(<StartupReportPage />, { wrapper: makeWrapper() });
    expect(screen.getByText('startupReport.refresh')).toBeInTheDocument();
  });

  it('shows empty message when no reports', async () => {
    vi.mocked(platformAdminApi.getStartupReports).mockResolvedValue(mockEmpty);
    render(<StartupReportPage />, { wrapper: makeWrapper() });
    await waitFor(() => {
      expect(screen.getByText('startupReport.empty')).toBeInTheDocument();
    });
  });

  it('shows report with version and build', async () => {
    vi.mocked(platformAdminApi.getStartupReports).mockResolvedValue(mockFull);
    render(<StartupReportPage />, { wrapper: makeWrapper() });
    await waitFor(() => {
      expect(screen.getByText(/2\.4\.1/)).toBeInTheDocument();
    });
  });

  it('shows "Latest" badge for most recent report', async () => {
    vi.mocked(platformAdminApi.getStartupReports).mockResolvedValue(mockFull);
    render(<StartupReportPage />, { wrapper: makeWrapper() });
    await waitFor(() => {
      expect(screen.getByText('startupReport.latest')).toBeInTheDocument();
    });
  });

  it('shows warnings section when warnings exist', async () => {
    vi.mocked(platformAdminApi.getStartupReports).mockResolvedValue(mockFull);
    render(<StartupReportPage />, { wrapper: makeWrapper() });
    await waitFor(() => {
      expect(screen.getByText('startupReport.warnings')).toBeInTheDocument();
    });
  });

  it('shows showing count when reports loaded', async () => {
    vi.mocked(platformAdminApi.getStartupReports).mockResolvedValue(mockFull);
    render(<StartupReportPage />, { wrapper: makeWrapper() });
    await waitFor(() => {
      expect(screen.getByText(/startupReport\.showing/)).toBeInTheDocument();
    });
  });
});
