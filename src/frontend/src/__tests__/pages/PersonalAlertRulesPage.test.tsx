import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { PersonalAlertRulesPage } from '../../features/configuration/pages/PersonalAlertRulesPage';

vi.mock('../../api/client', () => ({
  default: {
    get: vi.fn().mockResolvedValue({ data: { items: [], totalCount: 0 } }),
    post: vi.fn().mockResolvedValue({ data: {} }),
    patch: vi.fn().mockResolvedValue({ data: {} }),
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
      <MemoryRouter><PersonalAlertRulesPage /></MemoryRouter>
    </QueryClientProvider>
  );
};

describe('PersonalAlertRulesPage', () => {
  it('renders title', async () => {
    renderPage();
    expect(await screen.findByText('alertRules.title')).toBeDefined();
  });

  it('renders subtitle', async () => {
    renderPage();
    expect(await screen.findByText('alertRules.subtitle')).toBeDefined();
  });

  it('renders empty state when no rules exist', async () => {
    renderPage();
    expect(await screen.findByText('alertRules.empty.title')).toBeDefined();
  });
});
