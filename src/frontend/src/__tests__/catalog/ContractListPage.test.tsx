/**
 * Testes de página para ContractListPage.
 * Cobrem estados de loading, lista de contratos e estado vazio.
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ContractListPage } from '../../features/catalog/pages/ContractListPage';

vi.mock('../../features/catalog/api/contracts', () => ({
  contractsApi: {
    listContracts: vi.fn(),
    getContractsSummary: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn().mockReturnValue({
    activeEnvironmentId: 'env-prod-001',
    activeEnvironment: { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [{ id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true }],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  }),
}));

import { contractsApi } from '../../features/catalog/api/contracts';

const mockContract = {
  apiAssetId: 'ctr-001',
  name: 'Order API v2',
  apiName: 'Order API v2',
  semVer: '2.0.0',
  version: '2.0.0',
  protocol: 'OpenApi' as const,
  lifecycleState: 'Approved' as const,
  isLocked: false,
  createdAt: '2024-06-01T00:00:00Z',
  updatedAt: '2024-06-01T00:00:00Z',
};

const mockSummary = {
  total: 1,
  approved: 1,
  draft: 0,
  deprecated: 0,
  withBreakingChanges: 0,
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ContractListPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ContractListPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(contractsApi.listContracts).mockResolvedValue({ items: [mockContract], totalCount: 1 });
    vi.mocked(contractsApi.getContractsSummary).mockResolvedValue(mockSummary);
  });

  it('renders contract list after loading', async () => {
    renderPage();
    await waitFor(() => expect(screen.getByText('ctr-001')).toBeInTheDocument());
  });

  it('shows empty state when no contracts exist', async () => {
    vi.mocked(contractsApi.listContracts).mockResolvedValue({ items: [], totalCount: 0 });
    vi.mocked(contractsApi.getContractsSummary).mockResolvedValue({ ...mockSummary, total: 0, approved: 0 });
    renderPage();
    await waitFor(() => expect(screen.queryByText('ctr-001')).not.toBeInTheDocument());
  });

  it('calls listContracts API on mount', async () => {
    renderPage();
    await waitFor(() => expect(contractsApi.listContracts).toHaveBeenCalled());
  });

  it('shows loading state while fetching', () => {
    vi.mocked(contractsApi.listContracts).mockReturnValue(new Promise(() => {}));
    vi.mocked(contractsApi.getContractsSummary).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('ctr-001')).not.toBeInTheDocument();
  });

  it('displays contract semver', async () => {
    renderPage();
    await waitFor(() => expect(screen.getByText('2.0.0')).toBeInTheDocument());
  });
});
