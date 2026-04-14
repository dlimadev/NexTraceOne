import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { CustomChartBuilderPage } from '../../features/operations/pages/CustomChartBuilderPage';

vi.mock('../../api/client', () => ({
  default: {
    get: vi.fn().mockResolvedValue({ data: { items: [], totalCount: 0 } }),
    post: vi.fn().mockResolvedValue({ data: {} }),
    delete: vi.fn().mockResolvedValue({ data: {} }),
  },
}));

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({
    user: { id: 'u1' },
    tenantId: 't1',
  }),
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
      <MemoryRouter><CustomChartBuilderPage /></MemoryRouter>
    </QueryClientProvider>
  );
};

describe('CustomChartBuilderPage', () => {
  it('renders title', async () => {
    renderPage();
    expect(await screen.findByText('customCharts.title')).toBeDefined();
  });

  it('renders subtitle', async () => {
    renderPage();
    expect(await screen.findByText('customCharts.subtitle')).toBeDefined();
  });

  it('renders empty state when no charts exist', async () => {
    renderPage();
    expect(await screen.findByText('customCharts.empty.title')).toBeDefined();
  });
});
