import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { NetworkPolicyPage } from '../../features/platform-admin/pages/NetworkPolicyPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { NetworkPolicyResponse } from '../../features/platform-admin/api/platformAdmin';

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

const mockOff: NetworkPolicyResponse = {
  mode: 'Off',
  activeCalls: 3,
  blockedCalls: 0,
  auditedAt: new Date().toISOString(),
  calls: [
    { key: 'SMTP', description: 'Email notifications', envVar: 'Smtp__Host', configured: true, blocked: false },
    { key: 'OpenAI', description: 'External AI', envVar: 'AiRuntime__OpenAI__Enabled', configured: false, blocked: false },
  ],
};

const mockAirGap: NetworkPolicyResponse = {
  mode: 'AirGap',
  activeCalls: 0,
  blockedCalls: 2,
  auditedAt: new Date().toISOString(),
  calls: [
    { key: 'SMTP', description: 'Email notifications', envVar: 'Smtp__Host', configured: true, blocked: true },
    { key: 'OpenAI', description: 'External AI', envVar: 'AiRuntime__OpenAI__Enabled', configured: false, blocked: true },
  ],
};

describe('NetworkPolicyPage', () => {
  beforeEach(() => {
    queryClient.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getNetworkPolicy).mockImplementation(
      () => new Promise(() => {})
    );
    render(<NetworkPolicyPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('renders Off mode policy with stats', async () => {
    vi.mocked(platformAdminApi.getNetworkPolicy).mockResolvedValue(mockOff);
    render(<NetworkPolicyPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('modeOffTitle')).toBeDefined());
    expect(screen.getByText('Off')).toBeDefined();
    expect(screen.getByText('3')).toBeDefined();
  });

  it('renders AirGap mode with active banner', async () => {
    vi.mocked(platformAdminApi.getNetworkPolicy).mockResolvedValue(mockAirGap);
    render(<NetworkPolicyPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('modeAirGapTitle')).toBeDefined());
    expect(screen.getByText('activeLabel')).toBeDefined();
  });

  it('shows external calls table', async () => {
    vi.mocked(platformAdminApi.getNetworkPolicy).mockResolvedValue(mockOff);
    render(<NetworkPolicyPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('SMTP')).toBeDefined());
    expect(screen.getByText('OpenAI')).toBeDefined();
  });

  it('shows error on failure', async () => {
    vi.mocked(platformAdminApi.getNetworkPolicy).mockRejectedValue(new Error('Network error'));
    render(<NetworkPolicyPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });
});
