import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ContractHealthTimelinePage } from '../../features/contracts/governance/ContractHealthTimelinePage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
const getContractHealthTimeline = vi.fn(() => Promise.resolve({ apiAssetId: 'asset-1', points: [] }));
vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: { getContractHealthTimeline: (...a: unknown[]) => getContractHealthTimeline(...a) },
}));

function wrap(entry: string) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={[entry]}><ContractHealthTimelinePage /></MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ContractHealthTimelinePage preload', () => {
  it('pre-fills and auto-loads from ?apiAssetId=', async () => {
    wrap('/contracts/health/timeline?apiAssetId=asset-1');
    const input = screen.getByLabelText(/API Asset ID/i) as HTMLInputElement;
    expect(input.value).toBe('asset-1');
    await waitFor(() => expect(getContractHealthTimeline).toHaveBeenCalledWith('asset-1'));
  });
});
