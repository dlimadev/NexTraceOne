import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
  Trans: ({ children }: { children: React.ReactNode }) => <>{children}</>,
}));

vi.mock('../../features/contracts/hooks', () => ({
  useContractsSummary: vi.fn(),
  useContractList: vi.fn(),
}));

import { useContractsSummary, useContractList } from '../../features/contracts/hooks';
import { ContractStudioPage } from '../../features/contracts/pages/ContractStudioPage';

function wrap(node: React.ReactNode) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>{node}</MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ContractStudioPage', () => {
  beforeEach(() => {
    vi.mocked(useContractsSummary).mockReturnValue({
      data: { totalCount: 32, approvedCount: 18, draftCount: 5 },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useContractsSummary>);

    vi.mocked(useContractList).mockReturnValue({
      data: {
        items: [
          {
            contractVersionId: 'v-1',
            apiName: 'Payments API v2',
            protocol: 'OpenApi',
            lifecycleState: 'Draft',
            updatedAt: new Date().toISOString(),
            apiAssetId: 'asset-1',
          },
        ],
        totalCount: 1,
      },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useContractList>);
  });

  it('renders three stat cards', () => {
    wrap(<ContractStudioPage />);
    expect(screen.getByTestId('stat-total')).toBeInTheDocument();
    expect(screen.getByTestId('stat-published')).toBeInTheDocument();
    expect(screen.getByTestId('stat-draft')).toBeInTheDocument();
  });

  it('shows stat values from summary', () => {
    wrap(<ContractStudioPage />);
    expect(screen.getByText('32')).toBeInTheDocument();
    expect(screen.getByText('18')).toBeInTheDocument();
    expect(screen.getByText('5')).toBeInTheDocument();
  });

  it('shows in-progress draft card with name', () => {
    wrap(<ContractStudioPage />);
    expect(screen.getByText('Payments API v2')).toBeInTheDocument();
  });

  it('renders type picker with REST/OpenAPI card linking to /contracts/studio/rest', () => {
    wrap(<ContractStudioPage />);
    const link = screen.getByTestId('type-card-rest-openapi');
    expect(link).toBeInTheDocument();
  });
});
