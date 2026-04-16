import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { GracefulShutdownPage } from '../../features/platform-admin/pages/GracefulShutdownPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { GracefulShutdownConfig } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getGracefulShutdownConfig: vi.fn(),
    updateGracefulShutdownConfig: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={qc}>{children}</QueryClientProvider>
);

const mockConfig: GracefulShutdownConfig = {
  requestDrainTimeoutSeconds: 30,
  outboxDrainTimeoutSeconds: 60,
  healthCheckReturns503OnShutdown: true,
  auditShutdownEvents: true,
  updatedAt: '2026-04-01T10:00:00Z',
};

describe('GracefulShutdownPage', () => {
  beforeEach(() => {
    qc.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getGracefulShutdownConfig).mockImplementation(() => new Promise(() => {}));
    render(<GracefulShutdownPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state', async () => {
    vi.mocked(platformAdminApi.getGracefulShutdownConfig).mockRejectedValue(new Error('fail'));
    render(<GracefulShutdownPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('renders page title', async () => {
    vi.mocked(platformAdminApi.getGracefulShutdownConfig).mockResolvedValue(mockConfig);
    render(<GracefulShutdownPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('title')).toBeDefined());
  });

  it('shows config values', async () => {
    vi.mocked(platformAdminApi.getGracefulShutdownConfig).mockResolvedValue(mockConfig);
    render(<GracefulShutdownPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('30s')).toBeDefined();
      expect(screen.getByText('60s')).toBeDefined();
    });
  });

  it('shows sequence steps', async () => {
    vi.mocked(platformAdminApi.getGracefulShutdownConfig).mockResolvedValue(mockConfig);
    render(<GracefulShutdownPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('sequenceTitle')).toBeDefined());
  });

  it('shows edit form on edit button click', async () => {
    vi.mocked(platformAdminApi.getGracefulShutdownConfig).mockResolvedValue(mockConfig);
    render(<GracefulShutdownPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByText('editBtn'));
    fireEvent.click(screen.getByText('editBtn'));
    expect(screen.getByText('save')).toBeDefined();
    expect(screen.getByText('cancel')).toBeDefined();
  });

  it('cancels edit on cancel click', async () => {
    vi.mocked(platformAdminApi.getGracefulShutdownConfig).mockResolvedValue(mockConfig);
    render(<GracefulShutdownPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByText('editBtn'));
    fireEvent.click(screen.getByText('editBtn'));
    fireEvent.click(screen.getByText('cancel'));
    await waitFor(() => expect(screen.getByText('editBtn')).toBeDefined());
  });

  it('calls update on save', async () => {
    vi.mocked(platformAdminApi.getGracefulShutdownConfig).mockResolvedValue(mockConfig);
    vi.mocked(platformAdminApi.updateGracefulShutdownConfig).mockResolvedValue(mockConfig);
    render(<GracefulShutdownPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByText('editBtn'));
    fireEvent.click(screen.getByText('editBtn'));
    fireEvent.click(screen.getByText('save'));
    await waitFor(() => expect(platformAdminApi.updateGracefulShutdownConfig).toHaveBeenCalled());
  });
});
