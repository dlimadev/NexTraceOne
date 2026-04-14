import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { TraceExplorerPage } from '../../features/operations/pages/TraceExplorerPage';

vi.mock('../../features/operations/api/telemetry', () => ({
  queryTraces: vi.fn().mockResolvedValue([]),
  getTraceDetail: vi.fn().mockResolvedValue({ spans: [], services: [], durationMs: 0 }),
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
      <MemoryRouter><TraceExplorerPage /></MemoryRouter>
    </QueryClientProvider>
  );
};

describe('TraceExplorerPage', () => {
  it('renders title', async () => {
    renderPage();
    expect(await screen.findByText('telemetryExplorer.traces.title')).toBeDefined();
  });

  it('renders subtitle', async () => {
    renderPage();
    expect(await screen.findByText('telemetryExplorer.traces.subtitle')).toBeDefined();
  });

  it('renders filter apply button', async () => {
    renderPage();
    expect(await screen.findByText('telemetryExplorer.filters.apply')).toBeDefined();
  });
});
