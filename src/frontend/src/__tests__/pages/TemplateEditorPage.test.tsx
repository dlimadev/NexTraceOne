import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { TemplateEditorPage } from '../../features/catalog/pages/TemplateEditorPage';

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

function renderPage(id?: string) {
  vi.mocked(templatesApi.getById).mockResolvedValue(null as never);
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  const path = id ? `/catalog/templates/${id}/edit` : '/catalog/templates/new';
  const routePath = '/catalog/templates/:id/edit';
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[path]}>
        <Routes>
          <Route path={routePath} element={<TemplateEditorPage />} />
          <Route path="/catalog/templates/new" element={<TemplateEditorPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('TemplateEditorPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders without crashing (new template)', () => {
    const { container } = renderPage();
    expect(container).toBeDefined();
  });

  it('renders without crashing (edit mode)', () => {
    const { container } = renderPage('tmpl-1');
    expect(container).toBeDefined();
  });
});
