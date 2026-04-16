import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { FeatureFlagsRuntimePage } from '../../features/platform-admin/pages/FeatureFlagsRuntimePage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type {
  FeatureFlagsRuntimeResponse,
  FeatureFlagRuntimeEntry,
} from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key, i18n: { language: 'en' } }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getFeatureFlagsRuntime: vi.fn(),
    setFeatureFlagRuntimeOverride: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={qc}>{children}</QueryClientProvider>
);

const makeFlag = (overrides: Partial<FeatureFlagRuntimeEntry> = {}): FeatureFlagRuntimeEntry => ({
  key: 'ai.assistant.enabled',
  displayName: 'AI Assistant',
  scope: 'System',
  enabled: true,
  defaultEnabled: true,
  hasOverride: false,
  ...overrides,
});

const mockData: FeatureFlagsRuntimeResponse = {
  evaluatedAt: '2026-04-15T12:00:00Z',
  flags: [
    makeFlag({ key: 'ai.assistant.enabled', displayName: 'AI Assistant', enabled: true }),
    makeFlag({
      key: 'legacy.enabled',
      displayName: 'Legacy Systems',
      scope: 'Tenant',
      enabled: false,
      defaultEnabled: false,
      hasOverride: true,
    }),
    makeFlag({
      key: 'feature.canary',
      displayName: 'Canary Feature',
      scope: 'Environment',
      enabled: true,
      hasOverride: false,
    }),
  ],
};

const mockEmptyData: FeatureFlagsRuntimeResponse = {
  evaluatedAt: '2026-04-15T12:00:00Z',
  flags: [],
};

describe('FeatureFlagsRuntimePage', () => {
  beforeEach(() => {
    qc.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getFeatureFlagsRuntime).mockImplementation(
      () => new Promise(() => {}),
    );
    render(<FeatureFlagsRuntimePage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state', async () => {
    vi.mocked(platformAdminApi.getFeatureFlagsRuntime).mockRejectedValue(
      new Error('fail'),
    );
    render(<FeatureFlagsRuntimePage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('renders page title and subtitle', async () => {
    vi.mocked(platformAdminApi.getFeatureFlagsRuntime).mockResolvedValue(
      mockData,
    );
    render(<FeatureFlagsRuntimePage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('title')).toBeDefined();
      expect(screen.getByText('subtitle')).toBeDefined();
    });
  });

  it('renders summary cards', async () => {
    vi.mocked(platformAdminApi.getFeatureFlagsRuntime).mockResolvedValue(
      mockData,
    );
    render(<FeatureFlagsRuntimePage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('totalFlags')).toBeDefined();
      expect(screen.getByText('enabledFlags')).toBeDefined();
      expect(screen.getByText('disabledFlags')).toBeDefined();
    });
  });

  it('renders flag display names in table', async () => {
    vi.mocked(platformAdminApi.getFeatureFlagsRuntime).mockResolvedValue(
      mockData,
    );
    render(<FeatureFlagsRuntimePage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('AI Assistant')).toBeDefined();
      expect(screen.getByText('Legacy Systems')).toBeDefined();
      expect(screen.getByText('Canary Feature')).toBeDefined();
    });
  });

  it('renders flag keys in table', async () => {
    vi.mocked(platformAdminApi.getFeatureFlagsRuntime).mockResolvedValue(
      mockData,
    );
    render(<FeatureFlagsRuntimePage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('ai.assistant.enabled')).toBeDefined();
      expect(screen.getByText('legacy.enabled')).toBeDefined();
    });
  });

  it('renders override indicator for flags with overrides', async () => {
    vi.mocked(platformAdminApi.getFeatureFlagsRuntime).mockResolvedValue(
      mockData,
    );
    render(<FeatureFlagsRuntimePage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('override')).toBeDefined();
    });
  });

  it('shows empty state when no flags', async () => {
    vi.mocked(platformAdminApi.getFeatureFlagsRuntime).mockResolvedValue(
      mockEmptyData,
    );
    render(<FeatureFlagsRuntimePage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('noFlags')).toBeDefined());
  });

  it('filters flags by search query', async () => {
    vi.mocked(platformAdminApi.getFeatureFlagsRuntime).mockResolvedValue(
      mockData,
    );
    render(<FeatureFlagsRuntimePage />, { wrapper: Wrapper });
    await waitFor(() =>
      expect(screen.getByText('AI Assistant')).toBeDefined(),
    );

    const searchInput = screen.getByPlaceholderText('searchPlaceholder');
    await userEvent.type(searchInput, 'Legacy');

    await waitFor(() => {
      expect(screen.queryByText('AI Assistant')).toBeNull();
      expect(screen.getByText('Legacy Systems')).toBeDefined();
    });
  });

  it('renders toggle button for each flag', async () => {
    vi.mocked(platformAdminApi.getFeatureFlagsRuntime).mockResolvedValue(
      mockData,
    );
    render(<FeatureFlagsRuntimePage />, { wrapper: Wrapper });
    await waitFor(() => {
      const disableButtons = screen.getAllByLabelText('disable');
      const enableButtons = screen.getAllByLabelText('enable');
      expect(disableButtons.length + enableButtons.length).toBe(3);
    });
  });

  it('calls setFeatureFlagRuntimeOverride on toggle', async () => {
    vi.mocked(platformAdminApi.getFeatureFlagsRuntime).mockResolvedValue(
      mockData,
    );
    vi.mocked(platformAdminApi.setFeatureFlagRuntimeOverride).mockResolvedValue(
      makeFlag({ enabled: false }),
    );
    render(<FeatureFlagsRuntimePage />, { wrapper: Wrapper });
    await waitFor(() =>
      expect(screen.getAllByLabelText('disable').length).toBeGreaterThan(0),
    );

    const [firstToggle] = screen.getAllByLabelText('disable');
    await userEvent.click(firstToggle);

    await waitFor(() =>
      expect(
        vi.mocked(platformAdminApi.setFeatureFlagRuntimeOverride),
      ).toHaveBeenCalledWith(
        expect.objectContaining({ key: 'ai.assistant.enabled', enabled: false }),
      ),
    );
  });

  it('renders refresh button', async () => {
    vi.mocked(platformAdminApi.getFeatureFlagsRuntime).mockResolvedValue(
      mockData,
    );
    render(<FeatureFlagsRuntimePage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('refresh')).toBeDefined());
  });

  it('renders column headers', async () => {
    vi.mocked(platformAdminApi.getFeatureFlagsRuntime).mockResolvedValue(
      mockData,
    );
    render(<FeatureFlagsRuntimePage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('colFlag')).toBeDefined();
      expect(screen.getByText('colScope')).toBeDefined();
      expect(screen.getByText('colDefault')).toBeDefined();
      expect(screen.getByText('colStatus')).toBeDefined();
      expect(screen.getByText('colToggle')).toBeDefined();
    });
  });
});
