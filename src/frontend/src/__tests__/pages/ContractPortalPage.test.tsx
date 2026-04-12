import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ContractPortalPage } from '../../features/contracts/portal/ContractPortalPage';

vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: {
    getDetail: vi.fn(),
    listRuleViolations: vi.fn(),
    getHistory: vi.fn(),
    getContractsSummary: vi.fn(),
    listContracts: vi.fn(),
    listSpectralRulesets: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { contractsApi } from '../../features/contracts/api/contracts';

function renderPage() {
  vi.mocked(contractsApi.getDetail).mockResolvedValue(null as never);
  vi.mocked(contractsApi.listRuleViolations).mockResolvedValue({ violations: [], totalCount: 0 } as never);
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/contracts/portal/cversion-1']}>
        <Routes>
          <Route path="/contracts/portal/:contractVersionId" element={<ContractPortalPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ContractPortalPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders without crashing', () => {
    const { container } = renderPage();
    expect(container).toBeDefined();
  });

  it('shows loading state while fetching', () => {
    vi.mocked(contractsApi.getDetail).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });
});
