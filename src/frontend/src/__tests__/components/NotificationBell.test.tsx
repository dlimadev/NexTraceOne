import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { userEvent } from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { NotificationBell } from '../../features/notifications/components/NotificationBell';

vi.mock('../../features/notifications/hooks/useNotifications', () => ({
  notificationKeys: { all: ['notifications'] },
  useNotificationList: vi.fn(),
  useUnreadCount: vi.fn(),
  useMarkAllAsRead: vi.fn(),
  useMarkAsRead: vi.fn(),
}));

vi.mock('../../features/notifications/hooks/useNotificationHelpers', () => ({
  getCategoryKey: vi.fn((c: string) => c),
  getSeverityDotColor: vi.fn(() => 'bg-critical'),
  getSeverityKey: vi.fn((s: string) => s),
  isUnread: vi.fn((n: { status: string }) => n.status === 'Unread'),
  useTimeAgo: vi.fn(() => () => '2m ago'),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import {
  useNotificationList,
  useUnreadCount,
  useMarkAllAsRead,
  useMarkAsRead,
} from '../../features/notifications/hooks/useNotifications';

const emptyList = { items: [], hasMore: false };

function renderBell() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <NotificationBell />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('NotificationBell', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useUnreadCount).mockReturnValue({
      data: { unreadCount: 0 },
      isLoading: false,
    } as ReturnType<typeof useUnreadCount>);
    vi.mocked(useNotificationList).mockReturnValue({
      data: emptyList,
      isLoading: false,
    } as ReturnType<typeof useNotificationList>);
    vi.mocked(useMarkAllAsRead).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as ReturnType<typeof useMarkAllAsRead>);
    vi.mocked(useMarkAsRead).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as ReturnType<typeof useMarkAsRead>);
  });

  it('renders the bell button', () => {
    renderBell();
    const btn = screen.getByRole('button');
    expect(btn).toBeDefined();
  });

  it('does not show badge when unread count is zero', () => {
    renderBell();
    expect(document.body.textContent).not.toContain('99+');
    // No count badge visible
    expect(document.querySelectorAll('.bg-critical').length).toBe(0);
  });

  it('shows unread count badge when there are unread notifications', () => {
    vi.mocked(useUnreadCount).mockReturnValue({
      data: { unreadCount: 5 },
      isLoading: false,
    } as ReturnType<typeof useUnreadCount>);
    renderBell();
    expect(document.body.textContent).toContain('5');
  });

  it('shows 99+ when unread count exceeds 99', () => {
    vi.mocked(useUnreadCount).mockReturnValue({
      data: { unreadCount: 150 },
      isLoading: false,
    } as ReturnType<typeof useUnreadCount>);
    renderBell();
    expect(document.body.textContent).toContain('99+');
  });

  it('opens dropdown on bell click', async () => {
    const user = userEvent.setup();
    renderBell();
    const btn = screen.getByRole('button');
    await user.click(btn);
    await waitFor(() => {
      expect(document.body.textContent).toContain('Notifications');
    });
  });

  it('shows empty state when no notifications', async () => {
    const user = userEvent.setup();
    vi.mocked(useNotificationList).mockReturnValue({
      data: emptyList,
      isLoading: false,
    } as ReturnType<typeof useNotificationList>);
    renderBell();
    const btn = screen.getByRole('button');
    await user.click(btn);
    await waitFor(() => {
      expect(document.body.textContent).toContain('No notifications');
    });
  });

  it('shows notification items when notifications exist', async () => {
    const user = userEvent.setup();
    vi.mocked(useNotificationList).mockReturnValue({
      data: {
        items: [
          {
            id: 'notif-1',
            title: 'Deploy completed for Order API',
            message: 'Version 2.4.1 deployed to production.',
            category: 'Change',
            severity: 'Info',
            status: 'Unread',
            sourceModule: 'changes',
            sourceEntityType: null,
            sourceEntityId: null,
            actionUrl: null,
            requiresAction: false,
            createdAt: '2026-04-01T10:00:00Z',
            readAt: null,
            acknowledgedAt: null,
            archivedAt: null,
            snoozedUntil: null,
            isEscalated: false,
            occurrenceCount: 1,
            environmentId: null,
          },
        ],
        hasMore: false,
      },
      isLoading: false,
    } as ReturnType<typeof useNotificationList>);
    renderBell();
    const btn = screen.getByRole('button');
    await user.click(btn);
    await waitFor(() => {
      expect(document.body.textContent).toContain('Deploy completed for Order API');
    });
  });

  it('shows mark-all-read button when there are unread notifications', async () => {
    const user = userEvent.setup();
    vi.mocked(useUnreadCount).mockReturnValue({
      data: { unreadCount: 3 },
      isLoading: false,
    } as ReturnType<typeof useUnreadCount>);
    renderBell();
    const btn = screen.getByRole('button');
    await user.click(btn);
    await waitFor(() => {
      expect(document.body.textContent).toContain('Mark all as read');
    });
  });
});
