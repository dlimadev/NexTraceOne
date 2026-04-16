import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { CompliancePacksPage } from '../../features/platform-admin/pages/CompliancePacksPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { CompliancePacksResponse } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string, _opts?: Record<string, unknown>) => key, i18n: { language: 'en' } }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getCompliancePacks: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={qc}>{children}</QueryClientProvider>
);

const mockPacks: CompliancePacksResponse = {
  packs: [
    {
      id: 'pack-soc2',
      name: 'SOC 2 Type II',
      standard: 'SOC 2',
      version: '2023',
      totalControls: 5,
      passingControls: 4,
      failingControls: 1,
      warningControls: 0,
      compliancePercent: 80,
      controls: [
        {
          id: 'ctrl-cc6.1',
          code: 'CC6.1',
          title: 'Logical and Physical Access Controls',
          description: 'Access to data is restricted to authorized users.',
          status: 'Pass',
          evidence: 'RBAC enforced via ProtectedRoute',
        },
        {
          id: 'ctrl-cc7.2',
          code: 'CC7.2',
          title: 'System Monitoring',
          description: 'System activity is monitored for anomalies.',
          status: 'Fail',
          actionRequired: 'Configure anomaly detection alerts',
        },
      ],
      lastCheckedAt: '2026-04-15T12:00:00Z',
    },
    {
      id: 'pack-iso27001',
      name: 'ISO 27001',
      standard: 'ISO 27001',
      version: '2022',
      totalControls: 3,
      passingControls: 3,
      failingControls: 0,
      warningControls: 0,
      compliancePercent: 100,
      controls: [
        {
          id: 'ctrl-a.9.1',
          code: 'A.9.1',
          title: 'Access Control Policy',
          description: 'An access control policy shall be established.',
          status: 'Pass',
        },
      ],
      lastCheckedAt: '2026-04-15T12:00:00Z',
    },
  ],
  generatedAt: '2026-04-15T12:00:00Z',
  simulatedNote: 'Simulated compliance data',
};

describe('CompliancePacksPage', () => {
  beforeEach(() => {
    qc.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getCompliancePacks).mockImplementation(() => new Promise(() => {}));
    render(<CompliancePacksPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state', async () => {
    vi.mocked(platformAdminApi.getCompliancePacks).mockRejectedValue(new Error('fail'));
    render(<CompliancePacksPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('renders page title', async () => {
    vi.mocked(platformAdminApi.getCompliancePacks).mockResolvedValue(mockPacks);
    render(<CompliancePacksPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('title')).toBeDefined());
  });

  it('renders pack names', async () => {
    vi.mocked(platformAdminApi.getCompliancePacks).mockResolvedValue(mockPacks);
    render(<CompliancePacksPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('SOC 2 Type II')).toBeDefined();
      expect(screen.getByText('ISO 27001')).toBeDefined();
    });
  });

  it('shows compliance percentages', async () => {
    vi.mocked(platformAdminApi.getCompliancePacks).mockResolvedValue(mockPacks);
    render(<CompliancePacksPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('80%')).toBeDefined();
      expect(screen.getByText('100%')).toBeDefined();
    });
  });

  it('expands controls list on pack click', async () => {
    vi.mocked(platformAdminApi.getCompliancePacks).mockResolvedValue(mockPacks);
    render(<CompliancePacksPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByText('SOC 2 Type II'));
    fireEvent.click(screen.getByLabelText('togglePack SOC 2 Type II'));
    await waitFor(() => {
      expect(screen.getByText('CC6.1')).toBeDefined();
      expect(screen.getByText('Logical and Physical Access Controls')).toBeDefined();
    });
  });

  it('shows action required for failing control when expanded', async () => {
    vi.mocked(platformAdminApi.getCompliancePacks).mockResolvedValue(mockPacks);
    render(<CompliancePacksPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByText('SOC 2 Type II'));
    fireEvent.click(screen.getByLabelText('togglePack SOC 2 Type II'));
    await waitFor(() =>
      expect(screen.getByText('→ Configure anomaly detection alerts')).toBeDefined()
    );
  });

  it('shows simulated note', async () => {
    vi.mocked(platformAdminApi.getCompliancePacks).mockResolvedValue(mockPacks);
    render(<CompliancePacksPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('Simulated compliance data')).toBeDefined());
  });
});
