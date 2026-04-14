import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ConfigurationDriftPage } from '../../features/operations/pages/ConfigurationDriftPage';

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
      <MemoryRouter><ConfigurationDriftPage /></MemoryRouter>
    </QueryClientProvider>
  );
};

describe('ConfigurationDriftPage', () => {
  it('renders title', async () => {
    renderPage();
    expect(await screen.findByText('configDrift.title')).toBeDefined();
  });

  it('renders subtitle', async () => {
    renderPage();
    expect(await screen.findByText('configDrift.subtitle')).toBeDefined();
  });

  it('renders key element', async () => {
    renderPage();
    expect(await screen.findByText('configDrift.noDrifts')).toBeDefined();
  });
});
