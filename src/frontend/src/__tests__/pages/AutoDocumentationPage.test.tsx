import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AutoDocumentationPage } from '../../features/knowledge/pages/AutoDocumentationPage';

vi.mock('../../api/client', () => ({
  default: {
    get: vi.fn().mockResolvedValue({
      data: {
        serviceName: 'payment-service',
        generatedAt: '2026-04-06T12:00:00Z',
        sections: [],
        runbookCount: 3,
        documentCount: 5,
        lastUpdated: null,
      },
    }),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => {
      if (opts?.count !== undefined) return `${opts.count}`;
      if (opts?.date) return `Generated ${opts.date}`;
      return key;
    },
  }),
}));

const renderWithProviders = (ui: React.ReactElement) => {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>{ui}</MemoryRouter>
    </QueryClientProvider>
  );
};

describe('AutoDocumentationPage', () => {
  it('renders title', () => {
    renderWithProviders(<AutoDocumentationPage />);
    expect(screen.getByText('knowledge.autoDoc.title')).toBeDefined();
  });

  it('renders search input', () => {
    renderWithProviders(<AutoDocumentationPage />);
    expect(screen.getByPlaceholderText('knowledge.autoDoc.serviceNamePlaceholder')).toBeDefined();
  });

  it('renders enter-service-name empty state initially', () => {
    renderWithProviders(<AutoDocumentationPage />);
    expect(screen.getByText('knowledge.autoDoc.enterServiceName')).toBeDefined();
  });
});
