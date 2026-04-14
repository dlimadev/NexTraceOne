import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ScheduledReportsPage } from '../../features/governance/pages/ScheduledReportsPage';

vi.mock('../../api/client', () => ({
  default: {
    get: vi.fn().mockResolvedValue({ data: { items: [], totalCount: 0 } }),
    post: vi.fn().mockResolvedValue({ data: {} }),
    patch: vi.fn().mockResolvedValue({ data: {} }),
    delete: vi.fn().mockResolvedValue({ data: {} }),
  },
}));

vi.mock('../../components/ExportModal', () => ({
  ExportModal: () => null,
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
      <MemoryRouter><ScheduledReportsPage /></MemoryRouter>
    </QueryClientProvider>
  );
};

describe('ScheduledReportsPage', () => {
  it('renders title', async () => {
    renderPage();
    expect(await screen.findByText('scheduledReports.title')).toBeDefined();
  });

  it('renders create button', async () => {
    renderPage();
    const elements = await screen.findAllByText('scheduledReports.create');
    expect(elements.length).toBeGreaterThanOrEqual(1);
  });

  it('renders empty state when no reports exist', async () => {
    renderPage();
    expect(await screen.findByText('scheduledReports.empty')).toBeDefined();
  });
});
