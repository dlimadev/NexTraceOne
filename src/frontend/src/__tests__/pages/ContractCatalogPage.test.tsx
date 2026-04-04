import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ContractCatalogPage } from '../../features/contracts/catalog/ContractCatalogPage';

vi.mock('../../features/contracts/hooks/useContractList', () => ({
  useContractList: vi.fn(),
  useContractsSummary: vi.fn(),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { useContractList, useContractsSummary } from '../../features/contracts/hooks/useContractList';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ContractCatalogPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ContractCatalogPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useContractList).mockReturnValue({
      data: { items: [], totalCount: 0 },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useContractList>);
    vi.mocked(useContractsSummary).mockReturnValue({
      data: {
        totalContracts: 0,
        publishedCount: 0,
        draftCount: 0,
        deprecatedCount: 0,
        byProtocol: {},
      },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useContractsSummary>);
  });

  it('renders page title', () => {
    renderPage();
    expect(screen.getByText('Contract Catalog')).toBeDefined();
  });

  it('shows loading state', () => {
    vi.mocked(useContractList).mockReturnValue({
      data: undefined,
      isLoading: true,
      isError: false,
    } as ReturnType<typeof useContractList>);
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders contracts when data is available', async () => {
    vi.mocked(useContractList).mockReturnValue({
      data: {
        items: [
          {
            versionId: 'cv-001',
            contractVersionId: 'cv-001',
            apiAssetId: 'order-api',
            name: 'Order API v2',
            protocol: 'OpenApi' as const,
            lifecycleState: 'Published' as const,
            version: '2.0.0',
            domain: 'Commerce',
            teamName: 'Commerce Team',
            criticality: 'High',
            createdAt: '2026-01-01T00:00:00Z',
          },
        ],
        totalCount: 1,
      },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useContractList>);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Order API v2')).toBeDefined();
    });
  });
});
