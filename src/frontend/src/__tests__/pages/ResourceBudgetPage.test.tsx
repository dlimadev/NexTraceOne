import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ResourceBudgetPage } from '../../features/platform-admin/pages/ResourceBudgetPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { ResourceBudgetResponse } from '../../features/platform-admin/api/platformAdmin';

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
    getResourceBudget: vi.fn(),
    updateTenantQuota: vi.fn(),
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

const mockHealthyTenant = {
  tenantId: 'tenant-acme',
  tenantName: 'Acme Corp',
  quota: {
    maxCpuCores: 16,
    maxMemoryGb: 32,
    maxDiskGb: 500,
    maxAiTokensPerMonth: 1_000_000,
    maxConnections: 100,
  },
  usage: {
    cpuRequestsCores: 4,
    memoryRequestsGb: 8,
    diskUsageGb: 120,
    aiTokensUsedThisMonth: 300_000,
    activeConnections: 20,
  },
  isBlocked: false,
  blockReason: null,
  overrideUntil: null,
  overrideReason: null,
};

const mockBlockedTenant = {
  ...mockHealthyTenant,
  tenantId: 'tenant-blocked',
  tenantName: 'Blocked Inc',
  isBlocked: true,
  blockReason: 'Exceeded disk quota',
};

const mockData: ResourceBudgetResponse = {
  tenants: [mockHealthyTenant, mockBlockedTenant],
  updatedAt: new Date().toISOString(),
};

const mockEmpty: ResourceBudgetResponse = { tenants: [], updatedAt: new Date().toISOString() };

describe('ResourceBudgetPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders title and subtitle', () => {
    vi.mocked(platformAdminApi.getResourceBudget).mockResolvedValue(mockData);
    render(<ResourceBudgetPage />, { wrapper: makeWrapper() });
    expect(screen.getByText('resourceBudget.title')).toBeInTheDocument();
    expect(screen.getByText('resourceBudget.subtitle')).toBeInTheDocument();
  });

  it('shows refresh button', () => {
    vi.mocked(platformAdminApi.getResourceBudget).mockResolvedValue(mockEmpty);
    render(<ResourceBudgetPage />, { wrapper: makeWrapper() });
    expect(screen.getByText('resourceBudget.refresh')).toBeInTheDocument();
  });

  it('shows empty message when no tenants', async () => {
    vi.mocked(platformAdminApi.getResourceBudget).mockResolvedValue(mockEmpty);
    render(<ResourceBudgetPage />, { wrapper: makeWrapper() });
    await waitFor(() => {
      expect(screen.getByText('resourceBudget.empty')).toBeInTheDocument();
    });
  });

  it('shows summary cards with tenant counts', async () => {
    vi.mocked(platformAdminApi.getResourceBudget).mockResolvedValue(mockData);
    render(<ResourceBudgetPage />, { wrapper: makeWrapper() });
    await waitFor(() => {
      expect(screen.getByText('resourceBudget.totalTenants')).toBeInTheDocument();
      expect(screen.getByText('resourceBudget.blockedTenants')).toBeInTheDocument();
    });
  });

  it('shows tenant names from data', async () => {
    vi.mocked(platformAdminApi.getResourceBudget).mockResolvedValue(mockData);
    render(<ResourceBudgetPage />, { wrapper: makeWrapper() });
    await waitFor(() => {
      expect(screen.getByText('Acme Corp')).toBeInTheDocument();
      expect(screen.getByText('Blocked Inc')).toBeInTheDocument();
    });
  });

  it('shows blocked badge for blocked tenants', async () => {
    vi.mocked(platformAdminApi.getResourceBudget).mockResolvedValue(mockData);
    render(<ResourceBudgetPage />, { wrapper: makeWrapper() });
    await waitFor(() => {
      expect(screen.getByText('resourceBudget.blocked')).toBeInTheDocument();
    });
  });

  it('shows block reason for blocked tenants', async () => {
    vi.mocked(platformAdminApi.getResourceBudget).mockResolvedValue(mockData);
    render(<ResourceBudgetPage />, { wrapper: makeWrapper() });
    await waitFor(() => {
      expect(screen.getByText('Exceeded disk quota')).toBeInTheDocument();
    });
  });
});
