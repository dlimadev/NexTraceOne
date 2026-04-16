import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ElasticsearchManagerPage } from '../../features/platform-admin/pages/ElasticsearchManagerPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { ElasticsearchManagerResponse } from '../../features/platform-admin/api/platformAdmin';

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
    getElasticsearchManager: vi.fn(),
    updateIlmPolicy: vi.fn(),
    getStartupReports: vi.fn(),
    getResourceBudget: vi.fn(),
    updateTenantQuota: vi.fn(),
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

const mockData: ElasticsearchManagerResponse = {
  clusterHealth: {
    status: 'green',
    clusterName: 'nxt-cluster',
    numberOfNodes: 3,
    activeShards: 42,
    unassignedShards: 0,
    jvmHeapUsedPercent: 55,
    diskUsedPercent: 40,
    diskTotalGb: 200,
    diskUsedGb: 80,
    projectedDaysUntilFull: null,
    isReadOnly: false,
    checkedAt: new Date().toISOString(),
  },
  indices: [
    {
      name: 'nxt-logs-2026.04',
      docsCount: 1_500_000,
      storeSizeGb: 3.2,
      currentPhase: 'hot',
      createdAt: '2026-04-01T00:00:00Z',
      ilmPolicyName: 'nxt-default',
    },
  ],
  ilmPolicies: [
    {
      name: 'nxt-default',
      hotMaxAgeDays: 7,
      warmAfterDays: 30,
      deleteAfterDays: 90,
    },
  ],
};

describe('ElasticsearchManagerPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders title and subtitle', () => {
    vi.mocked(platformAdminApi.getElasticsearchManager).mockResolvedValue(mockData);
    render(<ElasticsearchManagerPage />, { wrapper: makeWrapper() });
    expect(screen.getByText('elasticsearchManager.title')).toBeInTheDocument();
    expect(screen.getByText('elasticsearchManager.subtitle')).toBeInTheDocument();
  });

  it('shows tab navigation', async () => {
    vi.mocked(platformAdminApi.getElasticsearchManager).mockResolvedValue(mockData);
    render(<ElasticsearchManagerPage />, { wrapper: makeWrapper() });
    await waitFor(() => {
      expect(screen.getByText('elasticsearchManager.tabHealth')).toBeInTheDocument();
      expect(screen.getByText('elasticsearchManager.tabIndices')).toBeInTheDocument();
      expect(screen.getByText('elasticsearchManager.tabPolicies')).toBeInTheDocument();
    });
  });

  it('shows cluster health data in health tab', async () => {
    vi.mocked(platformAdminApi.getElasticsearchManager).mockResolvedValue(mockData);
    render(<ElasticsearchManagerPage />, { wrapper: makeWrapper() });
    await waitFor(() => {
      expect(screen.getByText('nxt-cluster')).toBeInTheDocument();
    });
  });

  it('switches to indices tab and shows index', async () => {
    vi.mocked(platformAdminApi.getElasticsearchManager).mockResolvedValue(mockData);
    render(<ElasticsearchManagerPage />, { wrapper: makeWrapper() });
    await waitFor(() => {
      expect(screen.getByText('elasticsearchManager.tabIndices')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('elasticsearchManager.tabIndices'));
    await waitFor(() => {
      expect(screen.getByText('nxt-logs-2026.04')).toBeInTheDocument();
    });
  });

  it('switches to policies tab and shows policy name', async () => {
    vi.mocked(platformAdminApi.getElasticsearchManager).mockResolvedValue(mockData);
    render(<ElasticsearchManagerPage />, { wrapper: makeWrapper() });
    await waitFor(() => {
      expect(screen.getByText('elasticsearchManager.tabPolicies')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByText('elasticsearchManager.tabPolicies'));
    await waitFor(() => {
      expect(screen.getByText('nxt-default')).toBeInTheDocument();
    });
  });

  it('shows refresh button', () => {
    vi.mocked(platformAdminApi.getElasticsearchManager).mockResolvedValue(mockData);
    render(<ElasticsearchManagerPage />, { wrapper: makeWrapper() });
    expect(screen.getByText('elasticsearchManager.refresh')).toBeInTheDocument();
  });

  it('shows cluster status badge (green)', async () => {
    vi.mocked(platformAdminApi.getElasticsearchManager).mockResolvedValue(mockData);
    render(<ElasticsearchManagerPage />, { wrapper: makeWrapper() });
    await waitFor(() => {
      expect(screen.getByText('green')).toBeInTheDocument();
    });
  });
});
