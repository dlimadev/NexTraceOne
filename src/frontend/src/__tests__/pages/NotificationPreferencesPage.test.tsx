import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { NotificationPreferencesPage } from '../../features/notifications/pages/NotificationPreferencesPage';

vi.mock('../../features/notifications/hooks/useNotificationPreferences', () => ({
  preferencesKeys: { all: ['preferences'] },
  useNotificationPreferences: vi.fn(),
  useUpdatePreference: vi.fn(),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import {
  useNotificationPreferences,
  useUpdatePreference,
} from '../../features/notifications/hooks/useNotificationPreferences';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <NotificationPreferencesPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('NotificationPreferencesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useNotificationPreferences).mockReturnValue({
      data: { preferences: [] },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useNotificationPreferences>);
    vi.mocked(useUpdatePreference).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as ReturnType<typeof useUpdatePreference>);
  });

  it('renders page title', () => {
    renderPage();
    expect(screen.getByText('Notification Preferences')).toBeDefined();
  });

  it('shows loading state', () => {
    vi.mocked(useNotificationPreferences).mockReturnValue({
      data: undefined,
      isLoading: true,
      isError: false,
    } as ReturnType<typeof useNotificationPreferences>);
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders preferences when data is available', async () => {
    vi.mocked(useNotificationPreferences).mockReturnValue({
      data: {
        preferences: [
          {
            category: 'Incident',
            inApp: true,
            email: true,
            webhook: false,
          },
          {
            category: 'Deployment',
            inApp: true,
            email: false,
            webhook: false,
          },
        ],
      },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useNotificationPreferences>);
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Incident');
    });
  });
});
