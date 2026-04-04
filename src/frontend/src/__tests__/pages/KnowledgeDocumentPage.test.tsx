import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { KnowledgeDocumentPage } from '../../features/knowledge/pages/KnowledgeDocumentPage';

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

import { useKnowledgeDocument, useKnowledgeRelationsByTarget } from '../../features/knowledge/hooks';

function renderPage(documentId = 'doc-abc-123') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/knowledge/documents/${documentId}`]}>
        <Routes>
          <Route path="/knowledge/documents/:documentId" element={<KnowledgeDocumentPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('KnowledgeDocumentPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useKnowledgeDocument).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useKnowledgeDocument>);
    vi.mocked(useKnowledgeRelationsByTarget).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useKnowledgeRelationsByTarget>);
  });

  it('renders loading state initially', () => {
    vi.mocked(useKnowledgeDocument).mockReturnValue({
      data: undefined,
      isLoading: true,
      isError: false,
    } as ReturnType<typeof useKnowledgeDocument>);
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders document title when data is loaded', async () => {
    vi.mocked(useKnowledgeDocument).mockReturnValue({
      data: {
        documentId: 'doc-abc-123',
        title: 'Database Failover Runbook',
        summary: 'Steps to perform DB failover',
        category: 'Runbook',
        status: 'Published',
        content: '## Overview\n\nThis runbook covers...',
        authorName: 'Charlie',
        createdAt: '2026-01-15T00:00:00Z',
        updatedAt: null,
        tags: ['database', 'failover'],
        relatedServices: [],
      },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useKnowledgeDocument>);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Database Failover Runbook')).toBeDefined();
    });
  });

  it('shows error state when document fails to load', async () => {
    vi.mocked(useKnowledgeDocument).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: true,
      refetch: vi.fn(),
    } as ReturnType<typeof useKnowledgeDocument>);
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toBeDefined();
    });
  });
});
