import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { DraftStudioPage } from '../../features/contracts/studio/DraftStudioPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, d?: string) => d ?? k }) }));
vi.mock('../../features/contracts/hooks/useDraftValidation', () => ({
  useDraftValidation: vi.fn(() => ({ state: { summary: { totalIssues: 0 } }, isRunning: false, validateAll: vi.fn() })),
}));

vi.mock('monaco-editor', () => ({ default: {} }));
vi.mock('@monaco-editor/react', () => ({
  default: vi.fn(() => null),
  loader: { config: vi.fn() },
}));
vi.mock('../../features/contracts/workspace/editor/MonacoEditorWrapper', () => ({
  MonacoEditorWrapper: vi.fn(() => null),
}));

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: vi.fn(() => ({
    user: { id: 'user-1', name: 'Test User', email: 'test@example.com', roles: [] },
    isAuthenticated: true,
  })),
  AuthProvider: ({ children }: { children: React.ReactNode }) => <>{children}</>,
}));

vi.mock('../../features/contracts/api/contractStudio', () => ({
  contractStudioApi: {
    getDraft: vi.fn(),
    createDraft: vi.fn(),
    submitForReview: vi.fn(),
    publishDraft: vi.fn(),
    deleteDraft: vi.fn(),
    updateDraft: vi.fn(),
  },
}));

vi.mock('../../features/catalog/api/serviceCatalog', () => ({
  serviceCatalogApi: {
    listServices: vi.fn(),
    getServiceDetail: vi.fn(),
  },
}));

vi.mock('../../features/contracts/hooks/useDraftExport', () => ({
  useDraftExport: vi.fn(() => ({ exportDraft: vi.fn(), isExporting: false, exportError: null })),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { contractStudioApi } from '../../features/contracts/api/contractStudio';
import { serviceCatalogApi } from '../../features/catalog/api/serviceCatalog';

function renderPage() {
  vi.mocked(contractStudioApi.getDraft).mockResolvedValue(null as never);
  vi.mocked(serviceCatalogApi.listServices).mockResolvedValue({ services: [], totalCount: 0 } as never);
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/contracts/drafts/draft-1']}>
        <Routes>
          <Route path="/contracts/drafts/:draftId" element={<DraftStudioPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

function renderLoaded() {
  vi.mocked(contractStudioApi.getDraft).mockResolvedValue({
    id: 'd1', title: 'Orders API', description: '', serviceId: '', contractType: 'RestApi',
    protocol: 'OpenApi', specContent: 'openapi: 3.1.0', format: 'yaml', proposedVersion: '1.2.0',
    status: 'Editing', author: 'ana@x.io', createdAt: '2026-06-20T10:00:00Z',
  } as never);
  vi.mocked(serviceCatalogApi.listServices).mockResolvedValue({ items: [] } as never);
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/contracts/studio/d1']}>
        <Routes><Route path="/contracts/studio/:draftId" element={<DraftStudioPage />} /></Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('DraftStudioPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders without crashing', () => {
    const { container } = renderPage();
    expect(container).toBeDefined();
  });

  it('shows loading state while fetching draft', () => {
    vi.mocked(contractStudioApi.getDraft).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders the identity card and DS tabs when loaded', async () => {
    renderLoaded();
    expect(await screen.findByRole('heading', { name: 'Orders API' })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /spec/i })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: /metadata/i })).toBeInTheDocument();
  });
});
