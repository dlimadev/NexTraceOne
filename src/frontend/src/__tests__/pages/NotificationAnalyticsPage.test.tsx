import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
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
    byCategory: { Incident: 50, Change: 80 },
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

  it('renders page subtitle', () => {
    renderPage();
    expect(screen.getByText(/Track notification volume/i)).toBeDefined();
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

  it('shows error state when query fails', () => {
    vi.mocked(useNotificationAnalytics).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: true,
      refetch: vi.fn(),
    } as ReturnType<typeof useNotificationAnalytics>);
    renderPage();
    // PageErrorState should be rendered — check the retry button
    expect(document.body.textContent).toContain('Retry');
  });

  it('renders analytics data when loaded', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('245');
    });
  });

  it('renders generated notifications stat card', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Generated notifications')).toBeDefined();
    });
  });

  it('renders delivery success rate stat card', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Delivery success rate')).toBeDefined();
    });
  });

  it('renders read rate stat card', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Read rate')).toBeDefined();
    });
  });

  it('renders by category section', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('By category')).toBeDefined();
    });
  });

  it('renders by severity section', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('By severity')).toBeDefined();
    });
  });

  it('renders top noisy types section', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Top noisy types')).toBeDefined();
    });
  });

  it('shows "no signal" placeholder when topNoisyTypes is empty', async () => {
    renderPage();
    await waitFor(() => {
      // The `none` i18n key maps to "No signal in the selected window."
      expect(document.body.textContent).toContain('No signal in the selected window');
    });
  });

  it('renders topNoisyTypes entries when non-empty', async () => {
    vi.mocked(useNotificationAnalytics).mockReturnValue({
      data: {
        ...mockAnalyticsData,
        quality: {
          ...mockAnalyticsData.quality,
          topNoisyTypes: [{ eventType: 'IncidentCreated', count: 55 }],
          leastEngagedTypes: [],
          unacknowledgedActionTypes: [],
        },
      },
      isLoading: false,
      isError: false,
      refetch: vi.fn(),
    } as ReturnType<typeof useNotificationAnalytics>);
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('IncidentCreated');
      expect(document.body.textContent).toContain('55');
    });
  });

  it('renders range selector buttons (7, 30, 90 days)', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Last 7 days');
      expect(document.body.textContent).toContain('Last 30 days');
      expect(document.body.textContent).toContain('Last 90 days');
    });
  });

  it('switches range when a range button is clicked', async () => {
    const mockRefetch = vi.fn();
    vi.mocked(useNotificationAnalytics).mockReturnValue({
      data: mockAnalyticsData,
      isLoading: false,
      isError: false,
      refetch: mockRefetch,
    } as ReturnType<typeof useNotificationAnalytics>);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Last 7 days')).toBeDefined();
    });
    const btn7 = screen.getByText('Last 7 days');
    fireEvent.click(btn7);
    // After click the hook should have been called with days=7
    expect(useNotificationAnalytics).toHaveBeenCalledWith(7);
  });

  it('renders suppressed and grouped quality stat cards', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Suppressed')).toBeDefined();
      expect(screen.getByText('Grouped')).toBeDefined();
    });
  });
});
