import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { LogExplorerPage } from '../../features/operations/pages/LogExplorerPage';

vi.mock('../../features/operations/api/telemetry', () => ({
  queryLogs: vi.fn().mockResolvedValue([]),
}));

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
      <MemoryRouter><LogExplorerPage /></MemoryRouter>
    </QueryClientProvider>
  );
};

describe('LogExplorerPage', () => {
  it('renders title', async () => {
    renderPage();
    expect(await screen.findByText('telemetryExplorer.logs.title')).toBeDefined();
  });

  it('renders subtitle', async () => {
    renderPage();
    expect(await screen.findByText('telemetryExplorer.logs.subtitle')).toBeDefined();
  });

  it('renders filter apply button', async () => {
    renderPage();
    expect(await screen.findByText('telemetryExplorer.filters.apply')).toBeDefined();
  });
});
