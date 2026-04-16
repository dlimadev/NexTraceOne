import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MigrationPreviewPage } from '../../features/platform-admin/pages/MigrationPreviewPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { MigrationPreviewResponse } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getMigrationPreview: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={qc}>{children}</QueryClientProvider>
);

const mockData: MigrationPreviewResponse = {
  pending: [
    {
      id: 'mig-001',
      name: '20260101_AddServiceContractVersion',
      timestamp: '20260101120000',
      module: 'Contracts',
      risk: 'Low',
      operations: ['AddColumn'],
      sqlPreview: 'ALTER TABLE "ContractVersions" ADD COLUMN "SchemaHash" TEXT;',
      estimatedDurationMs: 120,
    },
    {
      id: 'mig-002',
      name: '20260103_DropLegacyMetricsCache',
      timestamp: '20260103140000',
      module: 'Operations',
      risk: 'High',
      operations: ['DropTable'],
      sqlPreview: 'DROP TABLE "LegacyMetricsCache";',
      estimatedDurationMs: 80,
    },
  ],
  appliedCount: 47,
  generatedAt: '2026-04-15T12:00:00Z',
  simulatedNote: 'Simulated migration preview data',
};

const mockEmptyData: MigrationPreviewResponse = {
  pending: [],
  appliedCount: 47,
  generatedAt: '2026-04-15T12:00:00Z',
  simulatedNote: 'Simulated migration preview data',
};

describe('MigrationPreviewPage', () => {
  beforeEach(() => {
    qc.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getMigrationPreview).mockImplementation(() => new Promise(() => {}));
    render(<MigrationPreviewPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state', async () => {
    vi.mocked(platformAdminApi.getMigrationPreview).mockRejectedValue(new Error('fail'));
    render(<MigrationPreviewPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('renders page title', async () => {
    vi.mocked(platformAdminApi.getMigrationPreview).mockResolvedValue(mockData);
    render(<MigrationPreviewPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('title')).toBeDefined());
  });

  it('renders stat cards', async () => {
    vi.mocked(platformAdminApi.getMigrationPreview).mockResolvedValue(mockData);
    render(<MigrationPreviewPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('statPending')).toBeDefined();
      expect(screen.getByText('statHighRisk')).toBeDefined();
    });
  });

  it('renders migration names', async () => {
    vi.mocked(platformAdminApi.getMigrationPreview).mockResolvedValue(mockData);
    render(<MigrationPreviewPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('20260101_AddServiceContractVersion')).toBeDefined();
      expect(screen.getByText('20260103_DropLegacyMetricsCache')).toBeDefined();
    });
  });

  it('shows empty state when no pending migrations', async () => {
    vi.mocked(platformAdminApi.getMigrationPreview).mockResolvedValue(mockEmptyData);
    render(<MigrationPreviewPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('noPending')).toBeDefined());
  });

  it('shows simulated note', async () => {
    vi.mocked(platformAdminApi.getMigrationPreview).mockResolvedValue(mockData);
    render(<MigrationPreviewPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText(/Simulated migration preview data/)).toBeDefined());
  });
});
