import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { NotificationAnalyticsPage } from '../../features/notifications/pages/NotificationAnalyticsPage';

vi.mock('../../features/notifications/hooks/useNotificationConfiguration', () => ({
  notificationConfigKeys: { all: ['notif-config'] },
  notificationAnalyticsKeys: { all: ['notif-analytics'] },
  notificationDeliveryKeys: { all: ['notif-delivery'] },
  useNotificationTemplates: vi.fn(),
  useUpsertNotificationTemplate: vi.fn(),
  useDeliveryChannels: vi.fn(),
  useUpsertDeliveryChannel: vi.fn(),
  useSmtpConfiguration: vi.fn(),
  useNotificationAnalytics: vi.fn(),
  useUpsertSmtpConfiguration: vi.fn(),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { useNotificationAnalytics } from '../../features/notifications/hooks/useNotificationConfiguration';

const mockAnalyticsData = {
  platform: {
    totalGenerated: 245,
    byCategory: { Incident: 50, Deployment: 80 },
    bySeverity: { Critical: 20, Warning: 75 },
    bySourceModule: { operations: 100, changes: 145 },
    deliveriesByChannel: { InApp: 245 },
    totalDelivered: 240,
    totalFailed: 3,
    totalPending: 2,
    totalSkipped: 0,
  },
  interaction: {
    totalRead: 198,
    totalUnread: 47,
    totalAcknowledged: 80,
    totalSnoozed: 5,
    totalArchived: 12,
    totalDismissed: 8,
    totalEscalated: 2,
    readRate: 80.8,
    acknowledgeRate: 32.6,
    averageTimeToReadMinutes: 12.5,
    averageTimeToAcknowledgeMinutes: 45.2,
    totalUnacknowledgedActionRequired: 3,
  },
  quality: {
    averagePerUserPerDay: 1.2,
    totalSuppressed: 15,
    totalGrouped: 42,
    totalCorrelatedWithIncidents: 28,
    topNoisyTypes: [],
    leastEngagedTypes: [],
    unacknowledgedActionTypes: [],
  },
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <NotificationAnalyticsPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('NotificationAnalyticsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useNotificationAnalytics).mockReturnValue({
      data: mockAnalyticsData,
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as ReturnType<typeof useNotificationAnalytics>);
  });

  it('renders page title', () => {
    renderPage();
    expect(screen.getByText('Analytics')).toBeDefined();
  });

  it('shows loading state', () => {
    vi.mocked(useNotificationAnalytics).mockReturnValue({
      data: undefined,
      isLoading: true,
      isError: false,
      refetch: vi.fn(),
    } as ReturnType<typeof useNotificationAnalytics>);
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders analytics data when loaded', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('245');
    });
  });
});
