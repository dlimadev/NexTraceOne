import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ChangeChecklistsPage } from '../../features/configuration/pages/ChangeChecklistsPage';

vi.mock('../../api/client', () => ({
  default: {
    get: vi.fn().mockResolvedValue({ data: { items: [], totalCount: 0 } }),
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
      <MemoryRouter><ChangeChecklistsPage /></MemoryRouter>
    </QueryClientProvider>
  );
};

describe('ChangeChecklistsPage', () => {
  it('renders title', async () => {
    renderPage();
    expect(await screen.findByText('workflows.checklists.title')).toBeDefined();
  });

  it('renders create button', async () => {
    renderPage();
    const elements = await screen.findAllByText('workflows.checklists.create');
    expect(elements.length).toBeGreaterThanOrEqual(1);
  });

  it('renders empty state when no checklists exist', async () => {
    renderPage();
    expect(await screen.findByText('workflows.checklists.empty')).toBeDefined();
  });
});
