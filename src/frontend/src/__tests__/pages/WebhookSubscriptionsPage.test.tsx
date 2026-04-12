import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';

vi.mock('../../features/integrations/api/integrations', () => ({
  integrationsApi: {
    getWebhookEventTypes: vi.fn().mockResolvedValue({
      eventTypes: [
        { code: 'incident.created', category: 'Incidents', description: 'Incident created' },
        { code: 'incident.resolved', category: 'Incidents', description: 'Incident resolved' },
        { code: 'change.deployed', category: 'Changes', description: 'Change deployed' },
        { code: 'change.promoted', category: 'Changes', description: 'Change promoted' },
        { code: 'contract.published', category: 'Contracts', description: 'Contract published' },
        { code: 'contract.deprecated', category: 'Contracts', description: 'Contract deprecated' },
        { code: 'service.registered', category: 'Services', description: 'Service registered' },
        { code: 'alert.triggered', category: 'Alerts', description: 'Alert triggered' },
      ],
    }),
    listWebhookSubscriptions: vi.fn().mockResolvedValue({
      items: [
        {
          subscriptionId: 'a1b2c3d4-0001-0000-0000-000000000001',
          name: 'Incident Alerts — PagerDuty',
          targetUrl: 'https://events.pagerduty.com/integration/v1/alerts',
          eventTypes: ['incident.created', 'incident.resolved'],
          hasSecret: true,
          isActive: true,
          eventCount: 2,
          createdAt: '2025-01-10T08:00:00Z',
          lastTriggeredAt: '2025-01-15T08:00:00Z',
        },
      ],
      totalCount: 1,
    }),
    registerWebhookSubscription: vi.fn().mockResolvedValue({}),
  },
}));

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({ tenantId: 't1', user: { id: 'u1' } }),
}));

import { WebhookSubscriptionsPage } from '../../features/integrations/pages/WebhookSubscriptionsPage';

function renderPage() {
  return renderWithProviders(<WebhookSubscriptionsPage />);
}

describe('WebhookSubscriptionsPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders page heading', async () => {
    renderPage();
    expect(await screen.findByText('Webhook Subscriptions')).toBeInTheDocument();
  });

  it('renders available event types section', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Available Event Types')).toBeInTheDocument();
    });
  });

  it('renders register webhook button', async () => {
    renderPage();
    expect(await screen.findByText('Register Webhook')).toBeInTheDocument();
  });

  it('shows existing subscriptions after load', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Incident Alerts — PagerDuty')).toBeInTheDocument();
    });
  });
});
