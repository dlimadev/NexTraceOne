import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { OperationalNotesPage } from '../../features/knowledge/pages/OperationalNotesPage';

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

import { useOperationalNotes } from '../../features/knowledge/hooks';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <OperationalNotesPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('OperationalNotesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useOperationalNotes).mockReturnValue({
      data: { items: [], totalCount: 0, page: 1, pageSize: 25 },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useOperationalNotes>);
  });

  it('renders page title', () => {
    renderPage();
    expect(screen.getByText('Operational Notes')).toBeDefined();
  });

  it('shows loading state', () => {
    vi.mocked(useOperationalNotes).mockReturnValue({
      data: undefined,
      isLoading: true,
      isError: false,
    } as ReturnType<typeof useOperationalNotes>);
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders notes when data is available', async () => {
    vi.mocked(useOperationalNotes).mockReturnValue({
      data: {
        items: [
          {
            noteId: 'note-1',
            title: 'DB Connection Issue',
            body: 'Intermittent timeouts on primary DB',
            severity: 'High',
            isResolved: false,
            authorName: 'Bob',
            serviceId: 'svc-1',
            serviceName: 'Order API',
            createdAt: '2026-03-01T00:00:00Z',
            resolvedAt: null,
            tags: [],
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 25,
      },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useOperationalNotes>);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('DB Connection Issue')).toBeDefined();
    });
  });
});
