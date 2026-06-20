import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import * as React from 'react';

vi.mock('../../features/contracts/api/contractStudio', () => ({
  contractStudioApi: { createDraft: vi.fn().mockResolvedValue({ draftId: 'd-1' }) },
}));
vi.mock('../../features/catalog/api/serviceCatalog', () => ({
  serviceCatalogApi: { listServices: vi.fn().mockResolvedValue({ items: [] }) },
}));
vi.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({ user: { email: 'me@x.io' } }),
}));

import { useContractDraftForm } from '../../features/contracts/create/useContractDraftForm';

function wrapper({ children }: { children: React.ReactNode }) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return (
    <QueryClientProvider client={qc}>
      <MemoryRouter>{children}</MemoryRouter>
    </QueryClientProvider>
  );
}

describe('useContractDraftForm', () => {
  beforeEach(() => vi.clearAllMocks());

  it('summary reflects live form fields', () => {
    const { result } = renderHook(() => useContractDraftForm({}), { wrapper });
    act(() => result.current.setField('title')({ target: { value: 'Orders API' } } as never));
    expect(result.current.summary.title).toBe('Orders API');
  });

  it('pre-seeds type and mode from initial args', () => {
    const { result } = renderHook(() => useContractDraftForm({ initialType: 'RestApi', initialMode: 'import' }), { wrapper });
    expect(result.current.selectedType).toBe('RestApi');
    expect(result.current.selectedMode).toBe('import');
  });
});
