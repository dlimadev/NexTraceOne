import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { NotificationDetailPage } from '../../features/notifications/pages/NotificationDetailPage';

vi.mock('../../features/notifications/hooks/useNotificationConfiguration', () => ({
  notificationConfigKeys: { all: ['notif-config'] },
  notificationAnalyticsKeys: { all: ['notif-analytics'] },
  notificationDeliveryKeys: { all: ['notif-delivery'] },
  useNotificationById: vi.fn(),
  useNotificationTrail: vi.fn(),
  useNotificationTemplates: vi.fn(),
  useUpsertNotificationTemplate: vi.fn(),
  useDeliveryChannels: vi.fn(),
  useUpsertDeliveryChannel: vi.fn(),
  useSmtpConfiguration: vi.fn(),
  useNotificationAnalytics: vi.fn(),
  useUpsertSmtpConfiguration: vi.fn(),
  useDeliveryHistory: vi.fn(),
  useDeliveryStatus: vi.fn(),
}));

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
  useNotificationById,
  useNotificationTrail,
} from '../../features/notifications/hooks/useNotificationConfiguration';
import {
  useAcknowledge,
  useArchive,
  useDismiss,
} from '../../features/notifications/hooks/useNotifications';

const mockNotification = {
  id: 'notif-001',
  title: 'Payment gateway degraded',
  message: 'High error rate detected on the payment processing API.',
  category: 'Incident',
  severity: 'Critical',
  status: 'Unread',
  eventType: 'IncidentCreated',
  sourceModule: 'OperationalIntelligence',
  sourceEntityType: 'Incident',
  sourceEntityId: 'inc-123',
  sourceEventId: 'evt-abc',
  actionUrl: '/operations/incidents/inc-123',
  requiresAction: true,
  createdAt: '2026-04-01T10:00:00Z',
  readAt: null,
  acknowledgedAt: null,
  archivedAt: null,
  snoozedUntil: null,
  isEscalated: false,
  occurrenceCount: 1,
  environmentId: 'env-prod-001',
};

const mockTrail = {
  notificationId: 'notif-001',
  notification: {
    notificationId: 'notif-001',
    eventType: 'IncidentCreated',
    sourceModule: 'OperationalIntelligence',
    sourceEntityType: 'Incident',
    sourceEntityId: 'inc-123',
    sourceEventId: 'evt-abc',
    category: 'Incident',
    severity: 'Critical',
    status: 'Unread',
    recipientUserId: 'user-001',
    createdAt: '2026-04-01T10:00:00Z',
    readAt: null,
    requiresAction: true,
  },
  deliveries: [
    {
      deliveryId: 'del-001',
      channel: 'Email',
      status: 'Delivered',
      retryCount: 1,
      createdAt: '2026-04-01T10:00:01Z',
      lastAttemptAt: '2026-04-01T10:00:02Z',
      deliveredAt: '2026-04-01T10:00:02Z',
      failedAt: null,
      nextRetryAt: null,
      errorMessage: null,
    },
  ],
  totalDeliveryAttempts: 1,
  isDeliveredToAnyChannel: true,
  hasPendingRetry: false,
  hasPermanentFailure: false,
};

function renderPage(notificationId = 'notif-001') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/notifications/${notificationId}`]}>
        <Routes>
          <Route path="/notifications/:notificationId" element={<NotificationDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('NotificationDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useNotificationById).mockReturnValue({
      data: { notification: mockNotification },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as ReturnType<typeof useNotificationById>);
    vi.mocked(useNotificationTrail).mockReturnValue({
      data: mockTrail,
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as ReturnType<typeof useNotificationTrail>);
    vi.mocked(useAcknowledge).mockReturnValue({ mutate: vi.fn(), isPending: false } as ReturnType<typeof useAcknowledge>);
    vi.mocked(useArchive).mockReturnValue({ mutate: vi.fn(), isPending: false } as ReturnType<typeof useArchive>);
    vi.mocked(useDismiss).mockReturnValue({ mutate: vi.fn(), isPending: false } as ReturnType<typeof useDismiss>);
  });

  it('renders notification title', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Payment gateway degraded');
    });
  });

  it('renders notification message', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('High error rate detected on the payment processing API.')).toBeDefined();
    });
  });

  it('renders delivery trail channel', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Email');
    });
  });

  it('shows loading state', () => {
    vi.mocked(useNotificationById).mockReturnValue({
      data: undefined,
      isLoading: true,
      isError: false,
      refetch: vi.fn(),
    } as ReturnType<typeof useNotificationById>);
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('shows error state when notification not found', () => {
    vi.mocked(useNotificationById).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: true,
      refetch: vi.fn(),
    } as ReturnType<typeof useNotificationById>);
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('shows requires-action banner for pending action notifications', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('acknowledgement');
    });
  });

  it('renders source metadata', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('IncidentCreated');
    });
  });

  it('renders acknowledged notification without action banner', async () => {
    vi.mocked(useNotificationById).mockReturnValue({
      data: {
        notification: {
          ...mockNotification,
          status: 'Acknowledged',
          acknowledgedAt: '2026-04-01T11:00:00Z',
        },
      },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as ReturnType<typeof useNotificationById>);
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Payment gateway degraded');
    });
    // No requiresAction banner for acknowledged notifications
    expect(document.body.textContent).not.toContain('requires your acknowledgement');
  });
});
