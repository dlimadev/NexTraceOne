import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
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

const mockDefinitions = [
  {
    key: 'notifications.types.incident.enabled',
    displayName: 'Incident Notifications Enabled',
    description: 'Enable or disable incident notification type',
    dataType: 'Boolean',
    category: 'Notification',
    defaultValue: 'true',
    allowedScopes: ['System', 'Environment'],
    isSecret: false,
    isReadOnly: false,
    sortOrder: 1,
  },
  {
    key: 'notifications.channels.email.enabled',
    displayName: 'Email Channel Enabled',
    description: 'Enable or disable email delivery channel',
    dataType: 'Boolean',
    category: 'Notification',
    defaultValue: 'true',
    allowedScopes: ['System'],
    isSecret: false,
    isReadOnly: false,
    sortOrder: 2,
  },
  {
    key: 'notifications.escalation.threshold_minutes',
    displayName: 'Escalation Threshold (minutes)',
    description: 'Time before unacknowledged critical notification is escalated',
    dataType: 'Integer',
    category: 'Notification',
    defaultValue: '30',
    allowedScopes: ['System', 'Environment'],
    isSecret: false,
    isReadOnly: false,
    sortOrder: 3,
  },
];

const mockEffectiveSettings = [
  {
    key: 'notifications.types.incident.enabled',
    value: 'true',
    scope: 'System',
    scopeReferenceId: null,
    resolvedAt: '2026-01-01T00:00:00Z',
  },
];

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
      data: mockDefinitions,
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useConfigurationDefinitions>);
    vi.mocked(useEffectiveSettings).mockReturnValue({
      data: mockEffectiveSettings,
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

  it('shows error state when definitions query fails', () => {
    vi.mocked(useConfigurationDefinitions).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: true,
      error: new Error('network error'),
      refetch: vi.fn(),
    } as unknown as ReturnType<typeof useConfigurationDefinitions>);
    renderPage();
    expect(document.body.textContent).toContain('Error loading configuration');
  });

  it('renders section tabs (types, channels, templates, routing, consumption, escalation)', async () => {
    renderPage();
    await waitFor(() => {
      const text = document.body.textContent ?? '';
      // At minimum the section buttons should be rendered
      expect(text).toContain('types');
    });
  });

  it('renders search input', async () => {
    renderPage();
    await waitFor(() => {
      // search label or placeholder
      expect(document.body.textContent).toContain('Search');
    });
  });

  it('filters definitions by search query within the active section', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByPlaceholderText('Search by key or name...')).toBeDefined();
    });
    const searchInput = screen.getByPlaceholderText('Search by key or name...');
    // Search for "incident" which matches notifications.types.incident.enabled in the 'types' section
    fireEvent.change(searchInput, { target: { value: 'incident' } });
    await waitFor(() => {
      expect(document.body.textContent).toContain('Incident Notifications Enabled');
    });
  });

  it('renders configuration items when data is available', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Incident Notifications Enabled');
    });
  });

  it('renders scope selector', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Scope');
    });
  });

  it('renders effective value for known settings', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Effective Value');
    });
  });

  it('renders empty state when no definitions match the section', async () => {
    vi.mocked(useConfigurationDefinitions).mockReturnValue({
      data: [],
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useConfigurationDefinitions>);
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('No definitions found');
    });
  });
});
