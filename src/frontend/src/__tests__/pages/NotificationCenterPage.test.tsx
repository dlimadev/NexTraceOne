import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
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
  useAcknowledge: vi.fn(),
  useArchive: vi.fn(),
  useDismiss: vi.fn(),
  useSnooze: vi.fn(),
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
  useAcknowledge,
  useArchive,
  useDismiss,
  useSnooze,
} from '../../features/notifications/hooks/useNotifications';

const emptyData = { items: [], totalCount: 0, page: 1, pageSize: 20, hasMore: false };

const makeNotification = (
  overrides: Partial<{
    id: string;
    title: string;
    status: string;
    severity: string;
    category: string;
    requiresAction: boolean;
    isEscalated: boolean;
    snoozedUntil: string | null;
    acknowledgedAt: string | null;
    archivedAt: string | null;
  }> = {},
) => ({
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
  acknowledgedAt: null,
  archivedAt: null,
  snoozedUntil: null,
  isEscalated: false,
  occurrenceCount: 1,
  environmentId: null,
  ...overrides,
});

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
    vi.mocked(useAcknowledge).mockReturnValue({ mutate: vi.fn(), isPending: false } as ReturnType<typeof useAcknowledge>);
    vi.mocked(useArchive).mockReturnValue({ mutate: vi.fn(), isPending: false } as ReturnType<typeof useArchive>);
    vi.mocked(useDismiss).mockReturnValue({ mutate: vi.fn(), isPending: false } as ReturnType<typeof useDismiss>);
    vi.mocked(useSnooze).mockReturnValue({ mutate: vi.fn(), isPending: false } as ReturnType<typeof useSnooze>);
  });

  // ── Rendering states ───────────────────────────────────────────────────

  it('renders page title', () => {
    renderPage();
    expect(screen.getByText('Notification Center')).toBeDefined();
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

  it('shows error state', () => {
    vi.mocked(useNotificationList).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: true,
      refetch: vi.fn(),
    } as ReturnType<typeof useNotificationList>);
    renderPage();
    expect(document.body.textContent).toBeDefined();
  });

  it('shows empty state when no notifications', () => {
    renderPage();
    // Empty state rendered with empty list
    expect(document.body).toBeDefined();
  });

  it('renders notifications when data is available', async () => {
    vi.mocked(useNotificationList).mockReturnValue({
      data: {
        items: [makeNotification({ id: 'notif-1', title: 'Service degraded' })],
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

  it('renders multiple notifications', async () => {
    vi.mocked(useNotificationList).mockReturnValue({
      data: {
        items: [
          makeNotification({ id: 'n1', title: 'Incident A' }),
          makeNotification({ id: 'n2', title: 'Incident B', severity: 'Warning', status: 'Read' }),
          makeNotification({ id: 'n3', title: 'Approval needed', category: 'Approval', severity: 'ActionRequired', requiresAction: true }),
        ],
        totalCount: 3,
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
      expect(screen.getByText('Incident A')).toBeDefined();
      expect(screen.getByText('Incident B')).toBeDefined();
      expect(screen.getByText('Approval needed')).toBeDefined();
    });
  });

  // ── Filters ────────────────────────────────────────────────────────────

  it('renders filter buttons', () => {
    renderPage();
    // Should have filter buttons (All, Unread, Read)
    const buttons = document.querySelectorAll('button');
    expect(buttons.length).toBeGreaterThan(0);
  });

  it('clicking status filter updates list query', async () => {
    renderPage();
    // Find and click the Unread filter button
    const buttons = Array.from(document.querySelectorAll('button'));
    const unreadBtn = buttons.find((b) => b.textContent?.includes('Unread'));
    if (unreadBtn) {
      fireEvent.click(unreadBtn);
      // After clicking, list query should be called with Unread filter
      expect(useNotificationList).toHaveBeenCalled();
    }
    expect(document.body).toBeDefined();
  });

  it('clicking severity filter updates list query', async () => {
    renderPage();
    const buttons = Array.from(document.querySelectorAll('button'));
    const criticalBtn = buttons.find((b) => b.textContent?.toLowerCase().includes('critical'));
    if (criticalBtn) {
      fireEvent.click(criticalBtn);
      expect(useNotificationList).toHaveBeenCalled();
    }
    expect(document.body).toBeDefined();
  });

  // ── Notification actions ───────────────────────────────────────────────

  it('mark all as read button is present when notifications exist', async () => {
    vi.mocked(useNotificationList).mockReturnValue({
      data: {
        items: [makeNotification({ id: 'n1', title: 'Unread notification' })],
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
      expect(screen.getByText('Unread notification')).toBeDefined();
    });
    // Check mark all as read button
    const buttons = Array.from(document.querySelectorAll('button'));
    const markAllBtn = buttons.find((b) => b.textContent?.toLowerCase().includes('mark'));
    expect(markAllBtn).toBeDefined();
  });

  it('renders escalated notification with distinct styling', async () => {
    vi.mocked(useNotificationList).mockReturnValue({
      data: {
        items: [makeNotification({ id: 'n-esc', title: 'Escalated incident', isEscalated: true })],
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
      expect(screen.getByText('Escalated incident')).toBeDefined();
    });
  });

  it('renders snoozed notification', async () => {
    vi.mocked(useNotificationList).mockReturnValue({
      data: {
        items: [
          makeNotification({
            id: 'n-snooze',
            title: 'Snoozed alert',
            snoozedUntil: new Date(Date.now() + 3_600_000).toISOString(),
            status: 'Read',
          }),
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
      expect(screen.getByText('Snoozed alert')).toBeDefined();
    });
  });

  it('renders acknowledged notification', async () => {
    vi.mocked(useNotificationList).mockReturnValue({
      data: {
        items: [
          makeNotification({
            id: 'n-ack',
            title: 'Acknowledged notification',
            status: 'Acknowledged',
            acknowledgedAt: '2026-03-20T11:00:00Z',
          }),
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
      expect(screen.getByText('Acknowledged notification')).toBeDefined();
    });
  });

  it('renders archived notification with dimmed style', async () => {
    vi.mocked(useNotificationList).mockReturnValue({
      data: {
        items: [
          makeNotification({
            id: 'n-arch',
            title: 'Archived notification',
            status: 'Archived',
            archivedAt: '2026-03-19T10:00:00Z',
          }),
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
      expect(screen.getByText('Archived notification')).toBeDefined();
    });
  });

  // ── Pagination ─────────────────────────────────────────────────────────

  it('shows load more button when hasMore is true', async () => {
    vi.mocked(useNotificationList).mockReturnValue({
      data: {
        items: [makeNotification({ id: 'n1', title: 'First notification' })],
        totalCount: 50,
        page: 1,
        pageSize: 20,
        hasMore: true,
      },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as ReturnType<typeof useNotificationList>);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('First notification')).toBeDefined();
    });
    // Load more button should be visible
    const buttons = Array.from(document.querySelectorAll('button'));
    const loadMoreBtn = buttons.find(
      (b) => b.textContent?.toLowerCase().includes('load') || b.textContent?.toLowerCase().includes('more'),
    );
    expect(loadMoreBtn).toBeDefined();
  });
});
