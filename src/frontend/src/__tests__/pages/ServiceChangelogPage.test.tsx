import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ServiceChangelogPage } from '../../features/catalog/pages/ServiceChangelogPage';

vi.mock('../../api/client', () => ({
  default: {
    get: vi.fn().mockResolvedValue({ data: {} }),
    post: vi.fn().mockResolvedValue({ data: {} }),
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
      <MemoryRouter><ServiceChangelogPage /></MemoryRouter>
    </QueryClientProvider>
  );
};

describe('ServiceChangelogPage', () => {
  it('renders title', async () => {
    renderPage();
    const titles = await screen.findAllByText('serviceChangelog.title');
    expect(titles.length).toBeGreaterThanOrEqual(1);
  });

  it('renders subtitle', async () => {
    renderPage();
    expect(await screen.findByText('serviceChangelog.subtitle')).toBeDefined();
  });

  it('renders empty state', async () => {
    renderPage();
    expect(await screen.findByText('serviceChangelog.noEntries')).toBeDefined();
  });
});
