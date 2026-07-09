import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ContractPlaygroundPage } from '../../features/contracts/playground/ContractPlaygroundPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
const getDetail = vi.fn(() => Promise.resolve({ protocol: 'OpenApi', semVer: '1.0.0', spec: '{}' }));
vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: { getDetail: (...a: unknown[]) => getDetail(...a) },
}));

function wrap(entry: string) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={[entry]}><ContractPlaygroundPage /></MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ContractPlaygroundPage journey', () => {
  it('preloads from ?contractVersionId= and links back to the portal', async () => {
    wrap('/contracts/playground?contractVersionId=cv-1');
    const input = screen.getByLabelText(/Contract Version ID/i) as HTMLInputElement;
    expect(input.value).toBe('cv-1');
    await waitFor(() => expect(getDetail).toHaveBeenCalledWith('cv-1'));
    const back = screen.getByRole('link', { name: /back to portal/i });
    expect(back.getAttribute('href')).toBe('/contracts/portal/cv-1');
  });
});
