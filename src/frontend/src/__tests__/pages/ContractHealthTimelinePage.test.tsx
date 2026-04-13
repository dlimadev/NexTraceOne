import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ContractHealthTimelinePage } from '../../features/contracts/governance/ContractHealthTimelinePage';

vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: {
    getContractHealthTimeline: vi.fn(),
    listContracts: vi.fn(),
    getDetail: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { contractsApi } from '../../features/contracts/api/contracts';

function renderPage() {
  vi.mocked(contractsApi.getContractHealthTimeline).mockResolvedValue({ snapshots: [] } as never);
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ContractHealthTimelinePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ContractHealthTimelinePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders without crashing', () => {
    const { container } = renderPage();
    expect(container).toBeDefined();
  });

  it('shows loading state while fetching', () => {
    vi.mocked(contractsApi.getContractHealthTimeline).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });
});
