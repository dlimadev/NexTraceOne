import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { IntegrationMappingsPage } from '../../features/configuration/pages/IntegrationMappingsPage';

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
      <MemoryRouter><IntegrationMappingsPage /></MemoryRouter>
    </QueryClientProvider>
  );
};

describe('IntegrationMappingsPage', () => {
  it('renders title', async () => {
    renderPage();
    const elements = await screen.findAllByText('integrationMappings.title');
    expect(elements.length).toBeGreaterThanOrEqual(1);
  });

  it('renders preview badge', async () => {
    renderPage();
    expect(await screen.findByText('preview.label')).toBeDefined();
  });

  it('renders description text', async () => {
    renderPage();
    expect(await screen.findByText('integrationMappings.description')).toBeDefined();
  });
});
