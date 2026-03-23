import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../features/catalog/api/contracts', () => ({
  contractsApi: {
    listContracts: vi.fn(),
    getContractsSummary: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { contractsApi } from '../../features/catalog/api/contracts';
import { ContractListPage } from '../../features/catalog/pages/ContractListPage';

const mockList = {
  items: [
    {
      versionId: 'cv-1',
      apiAssetId: 'api-1',
      semVer: '1.2.0',
      protocol: 'OpenApi',
      lifecycleState: 'Approved',
      format: 'json',
      isSigned: true,
      createdAt: '2025-01-15T10:00:00Z',
    },
  ],
  totalCount: 1,
};

const mockSummary = {
  totalVersions: 25,
  distinctContracts: 10,
  draftCount: 5,
  approvedCount: 12,
  lockedCount: 3,
  deprecatedCount: 2,
  sunsetCount: 1,
  retiredCount: 2,
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter><ContractListPage /></MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ContractListPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(contractsApi.listContracts).mockResolvedValue(mockList);
    vi.mocked(contractsApi.getContractsSummary).mockResolvedValue(mockSummary);
  });

  it('renders page heading', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('contractGov.title');
    });
  });

  it('renders data after loading', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('1.2.0')).toBeInTheDocument();
    });
  });

  it('shows loading state while fetching', () => {
    vi.mocked(contractsApi.listContracts).mockReturnValue(new Promise(() => {}));
    vi.mocked(contractsApi.getContractsSummary).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('1.2.0')).not.toBeInTheDocument();
  });

  it('does not render DemoBanner', async () => {
    renderPage();
    await waitFor(() => screen.getByText('1.2.0'));
    expect(screen.queryByTestId('demo-banner')).not.toBeInTheDocument();
  });

  it('calls contractsApi on mount', async () => {
    renderPage();
    await waitFor(() => expect(contractsApi.listContracts).toHaveBeenCalled());
    expect(contractsApi.getContractsSummary).toHaveBeenCalled();
  });
});
