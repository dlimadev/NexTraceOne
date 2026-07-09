import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { PublicationCenterPage } from '../../features/contracts/publication/PublicationCenterPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
const entry = {
  publicationEntryId: 'pe-1', contractVersionId: 'cv-1', apiAssetId: 'a-1',
  contractTitle: 'orders-api', semVer: '1.0.0', status: 'Published', visibility: 'Public', publishedBy: 'me',
};
vi.mock('../../features/contracts/hooks/usePublicationCenter', () => ({
  usePublicationCenterEntries: () => ({ data: { items: [entry], totalCount: 1 }, isLoading: false, isError: false, refetch: vi.fn() }),
  useWithdrawContractFromPortal: () => ({ mutateAsync: vi.fn(), isPending: false }),
}));

function wrap() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(<QueryClientProvider client={qc}><MemoryRouter><PublicationCenterPage /></MemoryRouter></QueryClientProvider>);
}

describe('PublicationCenterPage hardening', () => {
  it('links the contract title to the workspace', () => {
    wrap();
    const link = screen.getByRole('link', { name: 'orders-api' });
    expect(link.getAttribute('href')).toBe('/contracts/cv-1');
  });

  it('opens the withdraw confirmation with a reason field', () => {
    wrap();
    // Fechado, o Modal não renderiza (retorna null) → sem campo de motivo.
    expect(screen.queryByLabelText(/Reason/i)).not.toBeInTheDocument();
    // Match exato "Withdraw" (o filtro "Withdrawn" também casaria /withdraw/i).
    fireEvent.click(screen.getByRole('button', { name: 'Withdraw' }));
    expect(screen.getByLabelText(/Reason/i)).toBeInTheDocument();
  });
});
