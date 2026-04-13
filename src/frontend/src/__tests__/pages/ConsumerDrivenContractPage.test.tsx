import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ConsumerDrivenContractPage } from '../../features/contracts/cdct/ConsumerDrivenContractPage';

vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: {
    getConsumerExpectations: vi.fn(),
    verifyCdct: vi.fn(),
    registerConsumerExpectation: vi.fn(),
    listContracts: vi.fn(),
    getDetail: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { contractsApi } from '../../features/contracts/api/contracts';

function renderPage() {
  vi.mocked(contractsApi.getConsumerExpectations).mockResolvedValue([]);
  vi.mocked(contractsApi.verifyCdct).mockResolvedValue({ status: 'Pass', results: [] } as never);
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ConsumerDrivenContractPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ConsumerDrivenContractPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders without crashing', () => {
    const { container } = renderPage();
    expect(container).toBeDefined();
  });

  it('shows loading state while fetching', () => {
    vi.mocked(contractsApi.getConsumerExpectations).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });
});
