import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AiTestScenarioGeneratorPage } from '../../features/ai-hub/pages/AiTestScenarioGeneratorPage';

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
      <MemoryRouter><AiTestScenarioGeneratorPage /></MemoryRouter>
    </QueryClientProvider>
  );
};

describe('AiTestScenarioGeneratorPage', () => {
  it('renders title', async () => {
    renderPage();
    expect(await screen.findByText('aiTestScenario.title')).toBeDefined();
  });

  it('renders subtitle', async () => {
    renderPage();
    expect(await screen.findByText('aiTestScenario.subtitle')).toBeDefined();
  });

  it('renders key element', async () => {
    renderPage();
    expect(await screen.findByText('aiTestScenario.noScenarios')).toBeDefined();
  });
});
