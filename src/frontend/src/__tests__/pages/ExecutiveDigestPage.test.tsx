import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ExecutiveDigestPage } from '../../features/governance/pages/ExecutiveDigestPage';

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
      <MemoryRouter><ExecutiveDigestPage /></MemoryRouter>
    </QueryClientProvider>
  );
};

describe('ExecutiveDigestPage', () => {
  it('renders title', async () => {
    renderPage();
    expect(await screen.findByText('executiveDigest.title')).toBeDefined();
  });

  it('renders subtitle', async () => {
    renderPage();
    expect(await screen.findByText('executiveDigest.subtitle')).toBeDefined();
  });

  it('renders configure button', async () => {
    renderPage();
    expect(await screen.findByText('executiveDigest.configure')).toBeDefined();
  });
});
