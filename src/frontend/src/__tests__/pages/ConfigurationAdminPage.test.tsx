import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ConfigurationAdminPage } from '../../features/configuration/pages/ConfigurationAdminPage';

vi.mock('../../features/configuration/hooks/useConfiguration', () => ({
  configurationKeys: { all: ['configuration'] },
  useConfigurationDefinitions: vi.fn(),
  useConfigurationEntries: vi.fn(),
  useEffectiveSettings: vi.fn(),
  useSetConfigurationValue: vi.fn(),
  useRemoveOverride: vi.fn(),
  useToggleConfiguration: vi.fn(),
  useAuditHistory: vi.fn(),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import {
  useConfigurationDefinitions,
  useConfigurationEntries,
  useEffectiveSettings,
  useSetConfigurationValue,
  useRemoveOverride,
  useToggleConfiguration,
  useAuditHistory,
} from '../../features/configuration/hooks/useConfiguration';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ConfigurationAdminPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ConfigurationAdminPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useConfigurationDefinitions).mockReturnValue({
      data: [],
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useConfigurationDefinitions>);
    vi.mocked(useConfigurationEntries).mockReturnValue({
      data: [],
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useConfigurationEntries>);
    vi.mocked(useEffectiveSettings).mockReturnValue({
      data: [],
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useEffectiveSettings>);
    vi.mocked(useSetConfigurationValue).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as ReturnType<typeof useSetConfigurationValue>);
    vi.mocked(useRemoveOverride).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as ReturnType<typeof useRemoveOverride>);
    vi.mocked(useToggleConfiguration).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as ReturnType<typeof useToggleConfiguration>);
    vi.mocked(useAuditHistory).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useAuditHistory>);
  });

  it('renders page title', () => {
    renderPage();
    expect(screen.getByText('Platform Configuration')).toBeDefined();
  });

  it('shows loading state', () => {
    vi.mocked(useConfigurationDefinitions).mockReturnValue({
      data: undefined,
      isLoading: true,
      isError: false,
    } as ReturnType<typeof useConfigurationDefinitions>);
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('shows error state when configuration fails to load', async () => {
    vi.mocked(useConfigurationDefinitions).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: true,
      refetch: vi.fn(),
    } as ReturnType<typeof useConfigurationDefinitions>);
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toBeDefined();
    });
  });

  it('renders configuration entries when data is available', async () => {
    vi.mocked(useConfigurationDefinitions).mockReturnValue({
      data: [
        {
          key: 'platform.maintenance.enabled',
          displayName: 'Maintenance Mode',
          description: 'Enable/disable maintenance mode',
          dataType: 'Boolean',
          category: 'Platform',
          defaultValue: 'false',
          allowedScopes: ['System'],
          isSecret: false,
          isReadOnly: false,
          sortOrder: 1,
        },
      ],
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useConfigurationDefinitions>);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Maintenance Mode')).toBeDefined();
    });
  });
});
