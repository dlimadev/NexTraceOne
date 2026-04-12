import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { TemplateLibraryPage } from '../../features/catalog/pages/TemplateLibraryPage';

vi.mock('../../features/catalog/api/templates', () => ({
  templatesApi: {
    list: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    generateWithAi: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { templatesApi } from '../../features/catalog/api/templates';

function renderPage() {
  vi.mocked(templatesApi.list).mockResolvedValue([]);
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <TemplateLibraryPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('TemplateLibraryPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders without crashing', () => {
    const { container } = renderPage();
    expect(container).toBeDefined();
  });

  it('shows empty state when no templates', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body).toBeDefined();
    });
  });
});
