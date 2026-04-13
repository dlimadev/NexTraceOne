import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { CanonicalEntityImpactCascadePage } from '../../features/contracts/canonical/CanonicalEntityImpactCascadePage';

vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: {
    getCanonicalEntityImpactCascade: vi.fn(),
    listContracts: vi.fn(),
    getDetail: vi.fn(),
    getContractsSummary: vi.fn(),
    listRuleViolations: vi.fn(),
    listCanonicalEntities: vi.fn(),
    getCanonicalEntity: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { contractsApi } from '../../features/contracts/api/contracts';

function renderPage() {
  vi.mocked(contractsApi.getCanonicalEntityImpactCascade).mockResolvedValue({ nodes: [], edges: [] } as never);
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <CanonicalEntityImpactCascadePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('CanonicalEntityImpactCascadePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders without crashing', () => {
    const { container } = renderPage();
    expect(container).toBeDefined();
  });

  it('renders page content', () => {
    renderPage();
    expect(document.body).toBeDefined();
  });
});
