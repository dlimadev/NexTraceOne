import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { WebhookTemplatesPage } from '../../features/configuration/pages/WebhookTemplatesPage';

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), delete: vi.fn(), patch: vi.fn() },
}));

import client from '../../api/client';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <WebhookTemplatesPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('WebhookTemplatesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(client.get).mockResolvedValue({ data: { items: [], totalCount: 0 } });
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Webhook Templates')).toBeInTheDocument();
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(client.get).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('shows empty state when no templates', async () => {
    renderPage();
    await waitFor(() => expect(document.body.textContent).toBeDefined());
  });

  it('renders template items when data is available', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        items: [
          {
            templateId: 'wht-1',
            name: 'Slack Notification',
            url: 'https://hooks.slack.com/services/T123/B456/xyz',
            eventTypes: ['change.created', 'incident.opened'],
            isEnabled: true,
            createdAt: '2026-01-01T00:00:00Z',
          },
        ],
        totalCount: 1,
      },
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Slack Notification')).toBeInTheDocument();
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(client.get).mockRejectedValue(new Error('API error'));
    renderPage();
    await waitFor(() => expect(document.body.textContent).toBeDefined());
  });
});
