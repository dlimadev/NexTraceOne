import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { SessionSecurityPage } from '../../features/platform-admin/pages/SessionSecurityPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { SessionSecurityConfig } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getSessionSecurityConfig: vi.fn(),
    updateSessionSecurityConfig: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={qc}>{children}</QueryClientProvider>
);

const mockConfig: SessionSecurityConfig = {
  inactivityTimeoutMinutes: 480,
  maxConcurrentSessions: 5,
  requireReauthForSensitiveActions: true,
  detectAnomalousIpChange: true,
  sensitiveActions: ['user.delete', 'policy.update', 'break-glass.activate'],
  updatedAt: '2026-04-01T10:00:00Z',
};

describe('SessionSecurityPage', () => {
  beforeEach(() => {
    qc.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getSessionSecurityConfig).mockImplementation(() => new Promise(() => {}));
    render(<SessionSecurityPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state', async () => {
    vi.mocked(platformAdminApi.getSessionSecurityConfig).mockRejectedValue(new Error('fail'));
    render(<SessionSecurityPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('renders page title', async () => {
    vi.mocked(platformAdminApi.getSessionSecurityConfig).mockResolvedValue(mockConfig);
    render(<SessionSecurityPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('title')).toBeDefined());
  });

  it('shows summary cards', async () => {
    vi.mocked(platformAdminApi.getSessionSecurityConfig).mockResolvedValue(mockConfig);
    render(<SessionSecurityPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getAllByText('inactivityLabel').length).toBeGreaterThan(0);
      expect(screen.getAllByText('maxSessionsLabel').length).toBeGreaterThan(0);
    });
  });

  it('shows sensitive actions list', async () => {
    vi.mocked(platformAdminApi.getSessionSecurityConfig).mockResolvedValue(mockConfig);
    render(<SessionSecurityPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('sensitiveActionsTitle')).toBeDefined();
      expect(screen.getByText('user.delete')).toBeDefined();
    });
  });

  it('shows edit form on edit button click', async () => {
    vi.mocked(platformAdminApi.getSessionSecurityConfig).mockResolvedValue(mockConfig);
    render(<SessionSecurityPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByText('editBtn'));
    fireEvent.click(screen.getByText('editBtn'));
    expect(screen.getByText('save')).toBeDefined();
    expect(screen.getByText('cancel')).toBeDefined();
  });

  it('cancels edit on cancel click', async () => {
    vi.mocked(platformAdminApi.getSessionSecurityConfig).mockResolvedValue(mockConfig);
    render(<SessionSecurityPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByText('editBtn'));
    fireEvent.click(screen.getByText('editBtn'));
    fireEvent.click(screen.getByText('cancel'));
    await waitFor(() => expect(screen.getByText('editBtn')).toBeDefined());
  });

  it('calls update on save', async () => {
    vi.mocked(platformAdminApi.getSessionSecurityConfig).mockResolvedValue(mockConfig);
    vi.mocked(platformAdminApi.updateSessionSecurityConfig).mockResolvedValue(mockConfig);
    render(<SessionSecurityPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByText('editBtn'));
    fireEvent.click(screen.getByText('editBtn'));
    fireEvent.click(screen.getByText('save'));
    await waitFor(() => expect(platformAdminApi.updateSessionSecurityConfig).toHaveBeenCalled());
  });
});
