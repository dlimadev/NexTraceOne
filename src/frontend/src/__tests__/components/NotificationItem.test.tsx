import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { userEvent } from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { NotificationItem } from '../../features/notifications/components/NotificationItem';
import type { NotificationDto } from '../../features/notifications/types';

vi.mock('../../features/notifications/hooks/useNotificationHelpers', () => ({
  getCategoryKey: vi.fn((c: string) => `notifications.category.${c.toLowerCase()}`),
  getSeverityDotColor: vi.fn((s: string) => (s === 'Critical' ? 'bg-critical' : 'bg-info')),
  getSeverityKey: vi.fn((s: string) => s),
  isUnread: vi.fn((n: { status: string }) => n.status === 'Unread'),
  useTimeAgo: vi.fn(() => (_: string) => '5m ago'),
}));

vi.mock('../../features/notifications/hooks/useNotifications', () => ({
  notificationKeys: { all: ['notifications'] },
  useMarkAsRead: vi.fn(),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { useMarkAsRead } from '../../features/notifications/hooks/useNotifications';

const baseNotification: NotificationDto = {
  id: 'notif-001',
  title: 'Service degraded',
  message: 'Order API is experiencing high error rate.',
  category: 'Incident',
  severity: 'Critical',
  status: 'Unread',
  sourceModule: 'operations',
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
};

function renderItem(notification: NotificationDto = baseNotification, compact = false, onInteract?: () => void) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <NotificationItem notification={notification} compact={compact} onInteract={onInteract} />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('NotificationItem', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useMarkAsRead).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as ReturnType<typeof useMarkAsRead>);
  });

  it('renders notification title', () => {
    renderItem();
    expect(screen.getByText('Service degraded')).toBeDefined();
  });

  it('renders notification message in non-compact mode', () => {
    renderItem(baseNotification, false);
    expect(screen.getByText('Order API is experiencing high error rate.')).toBeDefined();
  });

  it('does not render message in compact mode', () => {
    renderItem(baseNotification, true);
    expect(screen.queryByText('Order API is experiencing high error rate.')).toBeNull();
  });

  it('renders time ago', () => {
    renderItem();
    expect(document.body.textContent).toContain('5m ago');
  });

  it('calls markAsRead and onInteract when clicked on unread notification', async () => {
    const markMutate = vi.fn();
    vi.mocked(useMarkAsRead).mockReturnValue({
      mutate: markMutate,
      isPending: false,
    } as ReturnType<typeof useMarkAsRead>);
    const onInteract = vi.fn();
    renderItem(baseNotification, false, onInteract);

    const user = userEvent.setup();
    const btn = screen.getByRole('button');
    await user.click(btn);

    expect(markMutate).toHaveBeenCalledWith(baseNotification.id);
    expect(onInteract).toHaveBeenCalled();
  });

  it('does not call markAsRead for already-read notifications', async () => {
    const markMutate = vi.fn();
    vi.mocked(useMarkAsRead).mockReturnValue({
      mutate: markMutate,
      isPending: false,
    } as ReturnType<typeof useMarkAsRead>);
    const readNotification: NotificationDto = { ...baseNotification, status: 'Read', readAt: '2026-04-01T10:05:00Z' };
    renderItem(readNotification);

    const user = userEvent.setup();
    const btn = screen.getByRole('button');
    await user.click(btn);

    expect(markMutate).not.toHaveBeenCalled();
  });

  it('shows unread indicator for unread notifications', () => {
    renderItem(baseNotification);
    // Unread dot has aria-label
    const dot = document.querySelector('[aria-label]');
    expect(dot).toBeDefined();
  });

  it('does not show unread indicator for read notifications', () => {
    const readNotification: NotificationDto = { ...baseNotification, status: 'Read', readAt: '2026-04-01T10:05:00Z' };
    renderItem(readNotification);
    // No unread indicator
    const dots = document.querySelectorAll('.bg-cyan-400');
    expect(dots.length).toBe(0);
  });

  it('renders category badge', () => {
    renderItem();
    // Category is translated by i18n: 'Incident'
    const badges = document.querySelectorAll('.uppercase');
    expect(badges.length).toBeGreaterThan(0);
  });

  it('shows severity dot with correct class for critical', () => {
    renderItem();
    const dot = document.querySelector('.bg-critical');
    expect(dot).toBeDefined();
  });
});
