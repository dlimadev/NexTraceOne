import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AiRunbookCopilotPage } from '../../features/ai-hub/pages/AiRunbookCopilotPage';

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
      <MemoryRouter><AiRunbookCopilotPage /></MemoryRouter>
    </QueryClientProvider>
  );
};

describe('AiRunbookCopilotPage', () => {
  it('renders title', async () => {
    renderPage();
    expect(await screen.findByText('aiRunbookCopilot.title')).toBeDefined();
  });

  it('renders subtitle', async () => {
    renderPage();
    expect(await screen.findByText('aiRunbookCopilot.subtitle')).toBeDefined();
  });

  it('renders empty state for no runbooks', async () => {
    renderPage();
    const items = await screen.findAllByText('aiRunbookCopilot.noRunbook');
    expect(items.length).toBeGreaterThanOrEqual(1);
  });
});
