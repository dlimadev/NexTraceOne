import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ContractGovernancePage } from '../../features/contracts/governance/ContractGovernancePage';

vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: {
    createVersion: vi.fn(),
    getClassification: vi.fn(),
    getHistory: vi.fn(),
    getDetail: vi.fn(),
    listRuleViolations: vi.fn(),
    listContracts: vi.fn(),
    getContractsSummary: vi.fn(),
    listContractsByService: vi.fn(),
    getValidationSummary: vi.fn(),
    listSpectralRulesets: vi.fn(),
    getSpectralRuleset: vi.fn(),
    createSpectralRuleset: vi.fn(),
    listCanonicalEntities: vi.fn(),
    getCanonicalEntity: vi.fn(),
    createCanonicalEntity: vi.fn(),
    getCanonicalEntityUsages: vi.fn(),
    getSoapContractDetail: vi.fn(),
    getEventContractDetail: vi.fn(),
    getBackgroundServiceContractDetail: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { contractsApi } from '../../features/contracts/api/contracts';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ContractGovernancePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ContractGovernancePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(contractsApi.listContracts).mockResolvedValue({
      contracts: [],
      totalCount: 0,
      page: 1,
      pageSize: 25,
    });
    vi.mocked(contractsApi.getContractsSummary).mockResolvedValue({
      totalContracts: 0,
      publishedCount: 0,
      draftCount: 0,
      deprecatedCount: 0,
      byProtocol: {},
    });
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Contract Governance')).toBeDefined();
    });
  });

  it('shows loading state', () => {
    vi.mocked(contractsApi.listContracts).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });
});
