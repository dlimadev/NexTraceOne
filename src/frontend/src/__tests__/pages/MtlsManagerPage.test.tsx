import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { MtlsManagerPage } from '../../features/platform-admin/pages/MtlsManagerPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { MtlsManagerResponse } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getMtlsManager: vi.fn(),
    revokeMtlsCert: vi.fn(),
    updateMtlsPolicy: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={qc}>{children}</QueryClientProvider>
);

const mockData: MtlsManagerResponse = {
  policy: {
    mode: 'PerService',
    rootCaCertPresent: true,
    rootCaCertExpiry: '2027-06-01T00:00:00Z',
  },
  certificates: [
    {
      id: 'cert-001',
      serviceName: 'api-gateway',
      fingerprint: 'AA:BB:CC:DD:EE:FF',
      validFrom: '2025-01-01T00:00:00Z',
      validTo: '2026-01-01T00:00:00Z',
      status: 'Valid',
      daysUntilExpiry: 120,
      issuer: 'NexTraceOne Root CA',
    },
    {
      id: 'cert-002',
      serviceName: 'order-service',
      fingerprint: 'BB:CC:DD:EE:FF:AA',
      validFrom: '2025-01-01T00:00:00Z',
      validTo: '2025-12-15T00:00:00Z',
      status: 'Expiring',
      daysUntilExpiry: 18,
      issuer: 'NexTraceOne Root CA',
    },
  ],
  lastSyncAt: '2026-04-15T12:00:00Z',
  simulatedNote: 'Simulated mTLS data',
};

const mockEmptyData: MtlsManagerResponse = {
  policy: { mode: 'Disabled', rootCaCertPresent: false },
  certificates: [],
  lastSyncAt: '2026-04-15T12:00:00Z',
  simulatedNote: 'Simulated mTLS data',
};

describe('MtlsManagerPage', () => {
  beforeEach(() => {
    qc.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getMtlsManager).mockImplementation(() => new Promise(() => {}));
    render(<MtlsManagerPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state', async () => {
    vi.mocked(platformAdminApi.getMtlsManager).mockRejectedValue(new Error('fail'));
    render(<MtlsManagerPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('renders page title', async () => {
    vi.mocked(platformAdminApi.getMtlsManager).mockResolvedValue(mockData);
    render(<MtlsManagerPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('title')).toBeDefined());
  });

  it('renders stat cards', async () => {
    vi.mocked(platformAdminApi.getMtlsManager).mockResolvedValue(mockData);
    render(<MtlsManagerPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('statTotal')).toBeDefined();
      expect(screen.getByText('statValid')).toBeDefined();
    });
  });

  it('renders certificate service names', async () => {
    vi.mocked(platformAdminApi.getMtlsManager).mockResolvedValue(mockData);
    render(<MtlsManagerPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('api-gateway')).toBeDefined();
      expect(screen.getByText('order-service')).toBeDefined();
    });
  });

  it('shows expiry alert for expiring certificates', async () => {
    vi.mocked(platformAdminApi.getMtlsManager).mockResolvedValue(mockData);
    render(<MtlsManagerPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('expiryAlert')).toBeDefined());
  });

  it('shows empty state when no certificates', async () => {
    vi.mocked(platformAdminApi.getMtlsManager).mockResolvedValue(mockEmptyData);
    render(<MtlsManagerPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('noCerts')).toBeDefined());
  });

  it('shows simulated note', async () => {
    vi.mocked(platformAdminApi.getMtlsManager).mockResolvedValue(mockData);
    render(<MtlsManagerPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText(/Simulated mTLS data/)).toBeDefined());
  });
});
