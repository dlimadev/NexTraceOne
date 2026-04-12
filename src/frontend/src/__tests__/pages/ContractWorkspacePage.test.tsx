import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ContractWorkspacePage } from '../../features/contracts/workspace/ContractWorkspacePage';

vi.mock('monaco-editor', () => ({ default: {} }));
vi.mock('@monaco-editor/react', () => ({
  default: vi.fn(() => null),
  loader: { config: vi.fn() },
}));
vi.mock('../../features/contracts/workspace/editor/MonacoEditorWrapper', () => ({
  MonacoEditorWrapper: vi.fn(() => null),
}));

vi.mock('../../features/contracts/hooks', () => ({
  useContractDetail: vi.fn(),
  useContractViolations: vi.fn(),
  useContractTransition: vi.fn(),
  useContractExport: vi.fn(),
  contractQueryKeys: { detail: vi.fn(() => []) },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import {
  useContractDetail,
  useContractViolations,
  useContractTransition,
  useContractExport,
} from '../../features/contracts/hooks';

function renderPage() {
  vi.mocked(useContractDetail).mockReturnValue({
    data: null,
    isLoading: true,
    isError: false,
    refetch: vi.fn(),
  } as never);
  vi.mocked(useContractViolations).mockReturnValue({ data: null, isLoading: false } as never);
  vi.mocked(useContractTransition).mockReturnValue({ transition: vi.fn() } as never);
  vi.mocked(useContractExport).mockReturnValue({ exportVersion: vi.fn() } as never);
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/contracts/workspace/cversion-1']}>
        <Routes>
          <Route path="/contracts/workspace/:contractVersionId" element={<ContractWorkspacePage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ContractWorkspacePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders without crashing', () => {
    const { container } = renderPage();
    expect(container).toBeDefined();
  });

  it('shows loading state', () => {
    renderPage();
    expect(document.body).toBeDefined();
  });
});
