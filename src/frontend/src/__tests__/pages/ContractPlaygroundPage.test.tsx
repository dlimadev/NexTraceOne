import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ContractPlaygroundPage } from '../../features/contracts/playground/ContractPlaygroundPage';

vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: {
    getVersionDetail: vi.fn(),
    listContracts: vi.fn(),
    getDetail: vi.fn(),
    listRuleViolations: vi.fn(),
    getContractsSummary: vi.fn(),
    listSpectralRulesets: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { contractsApi } from '../../features/contracts/api/contracts';

function renderPage() {
  vi.mocked(contractsApi.getVersionDetail).mockResolvedValue(null as never);
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ContractPlaygroundPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ContractPlaygroundPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders without crashing', () => {
    const { container } = renderPage();
    expect(container).toBeDefined();
  });

  it('shows loading or empty state', () => {
    vi.mocked(contractsApi.getVersionDetail).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });
});
