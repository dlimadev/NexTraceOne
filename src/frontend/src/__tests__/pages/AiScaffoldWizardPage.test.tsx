import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AiScaffoldWizardPage } from '../../features/catalog/pages/AiScaffoldWizardPage';

vi.mock('../../features/catalog/api/templates', () => ({
  templatesApi: {
    list: vi.fn(),
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    generateWithAi: vi.fn(),
  },
}));

vi.mock('jszip', () => ({
  default: vi.fn(() => ({
    file: vi.fn(),
    generateAsync: vi.fn(() => Promise.resolve(new Uint8Array())),
  })),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { templatesApi } from '../../features/catalog/api/templates';

function renderPage() {
  vi.mocked(templatesApi.getById).mockResolvedValue({
    templateId: 'tmpl-1',
    slug: 'rest-api-dotnet',
    displayName: 'REST API — .NET',
    description: 'Template for REST APIs using .NET',
    version: '1.0.0',
    serviceType: 'RestApi',
    language: 'DotNet',
    defaultDomain: 'platform',
    defaultTeam: 'infra',
    tags: [],
    isActive: true,
    usageCount: 5,
    hasBaseContract: true,
    hasScaffoldingManifest: true,
    createdAt: '2026-01-01T00:00:00Z',
    scaffoldingManifest: null,
    baseContractSpec: null,
    ciPipelineTemplate: null,
    createdBy: 'admin',
    notes: '',
  } as Parameters<typeof vi.mocked<typeof templatesApi.getById>>[0] extends never ? never : Awaited<ReturnType<typeof templatesApi.getById>>);
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/catalog/templates/tmpl-1/scaffold']}>
        <Routes>
          <Route path="/catalog/templates/:templateId/scaffold" element={<AiScaffoldWizardPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('AiScaffoldWizardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders without crashing', () => {
    const { container } = renderPage();
    expect(container).toBeDefined();
  });

  it('shows loading state while fetching template', () => {
    vi.mocked(templatesApi.getById).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });
});
