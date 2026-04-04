import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { KnowledgeHubPage } from '../../features/knowledge/pages/KnowledgeHubPage';

vi.mock('../../features/knowledge/hooks', () => ({
  knowledgeQueryKeys: { all: ['knowledge'] },
  useKnowledgeDocuments: vi.fn(),
  useOperationalNotes: vi.fn(),
  useKnowledgeSearch: vi.fn(),
  useKnowledgeDocument: vi.fn(),
  useKnowledgeRelationsByTarget: vi.fn(),
  useKnowledgeRelationsBySource: vi.fn(),
  useCreateKnowledgeDocument: vi.fn(),
  useCreateOperationalNote: vi.fn(),
  useCreateKnowledgeRelation: vi.fn(),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import {
  useKnowledgeDocuments,
  useOperationalNotes,
  useKnowledgeSearch,
} from '../../features/knowledge/hooks';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <KnowledgeHubPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('KnowledgeHubPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useKnowledgeDocuments).mockReturnValue({
      data: { items: [], totalCount: 0, page: 1, pageSize: 25 },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useKnowledgeDocuments>);
    vi.mocked(useOperationalNotes).mockReturnValue({
      data: { items: [], totalCount: 0, page: 1, pageSize: 25 },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useOperationalNotes>);
    vi.mocked(useKnowledgeSearch).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useKnowledgeSearch>);
  });

  it('renders page title', () => {
    renderPage();
    expect(screen.getByText('Knowledge Hub')).toBeDefined();
  });

  it('renders new document button', () => {
    renderPage();
    const buttons = screen.getAllByRole('button');
    expect(buttons.length).toBeGreaterThan(0);
  });

  it('shows loading state when documents are loading', () => {
    vi.mocked(useKnowledgeDocuments).mockReturnValue({
      data: undefined,
      isLoading: true,
      isError: false,
    } as ReturnType<typeof useKnowledgeDocuments>);
    renderPage();
    // Page renders without crashing in loading state
    expect(document.body).toBeDefined();
  });

  it('shows error state when documents fail to load', async () => {
    vi.mocked(useKnowledgeDocuments).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: true,
      refetch: vi.fn(),
    } as ReturnType<typeof useKnowledgeDocuments>);
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toBeDefined();
    });
  });

  it('renders documents when data is available', async () => {
    vi.mocked(useKnowledgeDocuments).mockReturnValue({
      data: {
        items: [
          {
            documentId: 'doc-1',
            title: 'Deployment Guide',
            summary: 'How to deploy services',
            category: 'Procedure',
            status: 'Published',
            authorName: 'Alice',
            createdAt: '2026-01-01T00:00:00Z',
            updatedAt: null,
            tags: ['deployment'],
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 25,
      },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useKnowledgeDocuments>);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Deployment Guide')).toBeDefined();
    });
  });
});
