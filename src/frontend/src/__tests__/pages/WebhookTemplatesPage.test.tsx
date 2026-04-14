import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { WebhookTemplatesPage } from '../../features/configuration/pages/WebhookTemplatesPage';

vi.mock('../../api/client', () => ({
  default: {
    get: vi.fn().mockResolvedValue({ data: { items: [], totalCount: 0 } }),
    post: vi.fn().mockResolvedValue({ data: {} }),
    patch: vi.fn().mockResolvedValue({ data: {} }),
    delete: vi.fn().mockResolvedValue({ data: {} }),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

const renderPage = () => {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter><WebhookTemplatesPage /></MemoryRouter>
    </QueryClientProvider>
  );
};

describe('WebhookTemplatesPage', () => {
  it('renders title', async () => {
    renderPage();
    expect(await screen.findByText('webhookTemplates.title')).toBeDefined();
  });

  it('renders create button', async () => {
    renderPage();
    expect(await screen.findByText('webhookTemplates.create')).toBeDefined();
  });

  it('renders empty state when no templates exist', async () => {
    renderPage();
    expect(await screen.findByText('webhookTemplates.empty')).toBeDefined();
  });
});
