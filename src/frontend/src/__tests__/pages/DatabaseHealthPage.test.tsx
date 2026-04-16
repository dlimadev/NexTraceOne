import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { DatabaseHealthPage } from '../../features/platform-admin/pages/DatabaseHealthPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { DatabaseHealthReport } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
    i18n: { language: 'en' },
  }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getHardwareAssessment: vi.fn(),
    getPreflight: vi.fn(),
    getConfigHealth: vi.fn(),
    getPendingMigrations: vi.fn(),
    getNetworkPolicy: vi.fn(),
    getDatabaseHealth: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
);

const mockHealthy: DatabaseHealthReport = {
  available: true,
  error: null,
  version: 'PostgreSQL 16.2',
  uptimeMinutes: 14400,
  activeConnections: 5,
  maxConnections: 100,
  totalSizeGb: 0.45,
  schemas: [
    { schema: 'catalog', sizeGb: 0.3, tableCount: 12 },
    { schema: 'ai_knowledge', sizeGb: 0.15, tableCount: 8 },
  ],
  bloatSignals: [],
  slowQueryCount: 0,
  slowQueries: [],
  checkedAt: new Date().toISOString(),
};

const mockWithIssues: DatabaseHealthReport = {
  ...mockHealthy,
  bloatSignals: [
    { schema: 'catalog', table: 'services', bloatPct: 35, severity: 'High' },
  ],
  slowQueries: [
    { queryPreview: 'SELECT * FROM services WHERE ...', meanMs: 2500, calls: 100 },
  ],
  slowQueryCount: 1,
};

const mockUnavailable: DatabaseHealthReport = {
  available: false,
  error: 'Connection refused',
  version: null,
  uptimeMinutes: 0,
  activeConnections: 0,
  maxConnections: 0,
  totalSizeGb: 0,
  schemas: [],
  bloatSignals: [],
  slowQueryCount: 0,
  slowQueries: [],
  checkedAt: new Date().toISOString(),
};

describe('DatabaseHealthPage', () => {
  beforeEach(() => {
    queryClient.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getDatabaseHealth).mockImplementation(
      () => new Promise(() => {})
    );
    render(<DatabaseHealthPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('renders healthy database info', async () => {
    vi.mocked(platformAdminApi.getDatabaseHealth).mockResolvedValue(mockHealthy);
    render(<DatabaseHealthPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('PostgreSQL 16.2')).toBeDefined());
    expect(screen.getByText('healthy')).toBeDefined();
  });

  it('renders schema sizes table', async () => {
    vi.mocked(platformAdminApi.getDatabaseHealth).mockResolvedValue(mockHealthy);
    render(<DatabaseHealthPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('catalog')).toBeDefined());
    expect(screen.getByText('ai_knowledge')).toBeDefined();
  });

  it('renders bloat signals when present', async () => {
    vi.mocked(platformAdminApi.getDatabaseHealth).mockResolvedValue(mockWithIssues);
    render(<DatabaseHealthPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('bloatTitle')).toBeDefined());
    expect(screen.getByText('High')).toBeDefined();
  });

  it('renders slow queries when present', async () => {
    vi.mocked(platformAdminApi.getDatabaseHealth).mockResolvedValue(mockWithIssues);
    render(<DatabaseHealthPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('slowQueriesTitle (1)')).toBeDefined());
  });

  it('shows unavailable state', async () => {
    vi.mocked(platformAdminApi.getDatabaseHealth).mockResolvedValue(mockUnavailable);
    render(<DatabaseHealthPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('unavailable')).toBeDefined());
    expect(screen.getByText('Connection refused')).toBeDefined();
  });

  it('shows API error state', async () => {
    vi.mocked(platformAdminApi.getDatabaseHealth).mockRejectedValue(new Error('Network error'));
    render(<DatabaseHealthPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });
});
