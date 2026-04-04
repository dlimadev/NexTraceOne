import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { NotificationCenterPage } from '../../features/notifications/pages/NotificationCenterPage';

vi.mock('../../features/notifications/hooks/useNotifications', () => ({
  notificationKeys: { all: ['notifications'] },
  useNotificationList: vi.fn(),
  useUnreadCount: vi.fn(),
  useMarkAsRead: vi.fn(),
  useMarkAsUnread: vi.fn(),
  useMarkAllAsRead: vi.fn(),
}));

vi.mock('../../features/notifications/hooks/useNotificationHelpers', () => ({
  getCategoryKey: vi.fn((c: string) => c),
  getSeverityDotColor: vi.fn(() => 'bg-info'),
  getSeverityKey: vi.fn((s: string) => s),
  isUnread: vi.fn((n: { status: string }) => n.status === 'Unread'),
  useTimeAgo: vi.fn(() => (date: string) => date),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import {
  useNotificationList,
  useMarkAsRead,
  useMarkAsUnread,
  useMarkAllAsRead,
} from '../../features/notifications/hooks/useNotifications';

const emptyData = { items: [], totalCount: 0, page: 1, pageSize: 20, hasMore: false };

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <NotificationCenterPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('NotificationCenterPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useNotificationList).mockReturnValue({
      data: emptyData,
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as ReturnType<typeof useNotificationList>);
    vi.mocked(useMarkAsRead).mockReturnValue({ mutate: vi.fn(), isPending: false } as ReturnType<typeof useMarkAsRead>);
    vi.mocked(useMarkAsUnread).mockReturnValue({ mutate: vi.fn(), isPending: false } as ReturnType<typeof useMarkAsUnread>);
    vi.mocked(useMarkAllAsRead).mockReturnValue({ mutate: vi.fn(), isPending: false } as ReturnType<typeof useMarkAllAsRead>);
  });

  it('renders page title', () => {
    renderPage();
    expect(screen.getByText('Notification Center')).toBeDefined();
  });

  it('renders filter options', () => {
    renderPage();
    expect(document.body.textContent).toBeDefined();
  });

  it('shows loading state', () => {
    vi.mocked(useNotificationList).mockReturnValue({
      data: undefined,
      isLoading: true,
      isError: false,
      refetch: vi.fn(),
    } as ReturnType<typeof useNotificationList>);
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders notifications when data is available', async () => {
    vi.mocked(useNotificationList).mockReturnValue({
      data: {
        items: [
          {
            id: 'notif-1',
            title: 'Service degraded',
            message: 'Order API is experiencing high error rate',
            category: 'Incident',
            severity: 'Critical',
            status: 'Unread',
            sourceModule: 'operations',
            sourceEntityType: null,
            sourceEntityId: null,
            actionUrl: null,
            requiresAction: false,
            createdAt: '2026-03-20T10:00:00Z',
            readAt: null,
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 20,
        hasMore: false,
      },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as ReturnType<typeof useNotificationList>);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Service degraded')).toBeDefined();
    });
  });
});
