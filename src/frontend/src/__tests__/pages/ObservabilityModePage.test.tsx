import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ObservabilityModePage } from '../../features/platform-admin/pages/ObservabilityModePage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { ObservabilityModeConfig } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getObservabilityMode: vi.fn(),
    updateObservabilityMode: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={qc}>{children}</QueryClientProvider>
);

const mockFullMode: ObservabilityModeConfig = {
  currentMode: 'Full',
  elasticsearchConnected: true,
  elasticsearchVersion: '8.12.0',
  postgresAnalyticsEnabled: false,
  otelCollectorConnected: true,
  additionalRamUsageGb: 3.5,
  tradeOffs: [],
  updatedAt: '2026-04-01T10:00:00Z',
  simulatedNote: 'Simulated mode data',
};

const mockLiteMode: ObservabilityModeConfig = {
  currentMode: 'Lite',
  elasticsearchConnected: false,
  postgresAnalyticsEnabled: true,
  otelCollectorConnected: false,
  additionalRamUsageGb: 0,
  tradeOffs: ['Full-text search not available', 'Slower historical queries'],
  updatedAt: '2026-04-01T10:00:00Z',
  simulatedNote: 'Simulated mode data',
};

describe('ObservabilityModePage', () => {
  beforeEach(() => {
    qc.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getObservabilityMode).mockImplementation(() => new Promise(() => {}));
    render(<ObservabilityModePage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state', async () => {
    vi.mocked(platformAdminApi.getObservabilityMode).mockRejectedValue(new Error('fail'));
    render(<ObservabilityModePage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('renders page title', async () => {
    vi.mocked(platformAdminApi.getObservabilityMode).mockResolvedValue(mockFullMode);
    render(<ObservabilityModePage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('title')).toBeDefined());
  });

  it('shows current mode status card', async () => {
    vi.mocked(platformAdminApi.getObservabilityMode).mockResolvedValue(mockFullMode);
    render(<ObservabilityModePage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('currentMode')).toBeDefined();
      // mode.Full appears in both status card and mode buttons
      expect(screen.getAllByText('mode.Full').length).toBeGreaterThan(0);
    });
  });

  it('shows all 3 mode buttons', async () => {
    vi.mocked(platformAdminApi.getObservabilityMode).mockResolvedValue(mockFullMode);
    render(<ObservabilityModePage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getAllByText('mode.Full').length).toBeGreaterThan(0);
      expect(screen.getByText('mode.Lite')).toBeDefined();
      expect(screen.getByText('mode.Minimal')).toBeDefined();
    });
  });

  it('shows trade-offs when present', async () => {
    vi.mocked(platformAdminApi.getObservabilityMode).mockResolvedValue(mockLiteMode);
    render(<ObservabilityModePage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('tradeOffsTitle')).toBeDefined();
      expect(screen.getByText('• Full-text search not available')).toBeDefined();
    });
  });

  it('calls updateObservabilityMode when a different mode is selected', async () => {
    vi.mocked(platformAdminApi.getObservabilityMode).mockResolvedValue(mockFullMode);
    vi.mocked(platformAdminApi.updateObservabilityMode).mockResolvedValue(mockLiteMode);
    render(<ObservabilityModePage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByText('mode.Lite'));
    fireEvent.click(screen.getByLabelText('selectMode mode.Lite'));
    await waitFor(() =>
      expect(platformAdminApi.updateObservabilityMode).toHaveBeenCalledWith({ mode: 'Lite' })
    );
  });

  it('shows simulated note', async () => {
    vi.mocked(platformAdminApi.getObservabilityMode).mockResolvedValue(mockFullMode);
    render(<ObservabilityModePage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('Simulated mode data')).toBeDefined());
  });
});
