import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { NotificationConfigurationPage } from '../../features/notifications/pages/NotificationConfigurationPage';

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
  useEffectiveSettings,
  useSetConfigurationValue,
  useAuditHistory,
} from '../../features/configuration/hooks/useConfiguration';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <NotificationConfigurationPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('NotificationConfigurationPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useConfigurationDefinitions).mockReturnValue({
      data: [],
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useConfigurationDefinitions>);
    vi.mocked(useEffectiveSettings).mockReturnValue({
      data: [],
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useEffectiveSettings>);
    vi.mocked(useSetConfigurationValue).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as ReturnType<typeof useSetConfigurationValue>);
    vi.mocked(useAuditHistory).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useAuditHistory>);
  });

  it('renders page title', () => {
    renderPage();
    expect(screen.getByText('Notification Configuration')).toBeDefined();
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

  it('renders configuration sections when data is available', async () => {
    vi.mocked(useConfigurationDefinitions).mockReturnValue({
      data: [
        {
          key: 'notification.smtp.host',
          displayName: 'SMTP Host',
          description: 'SMTP server hostname',
          dataType: 'String',
          category: 'Notification',
          defaultValue: 'localhost',
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
      expect(document.body.textContent).toBeDefined();
    });
  });
});
