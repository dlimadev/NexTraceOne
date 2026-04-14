import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { APIKeysPage } from '../../features/configuration/pages/APIKeysPage';

vi.mock('../../api/client', () => ({
  default: {
    get: vi.fn().mockResolvedValue({ data: { items: [] } }),
    post: vi.fn().mockResolvedValue({ data: {} }),
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
      <MemoryRouter><APIKeysPage /></MemoryRouter>
    </QueryClientProvider>
  );
};

describe('APIKeysPage', () => {
  it('renders title', async () => {
    renderPage();
    expect(await screen.findByText('apiKeys.title')).toBeDefined();
  });

  it('renders create button', async () => {
    renderPage();
    expect(await screen.findByText('apiKeys.create')).toBeDefined();
  });

  it('renders empty state when no keys exist', async () => {
    renderPage();
    expect(await screen.findByText('apiKeys.empty')).toBeDefined();
  });
});
