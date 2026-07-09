import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ContractGovernancePage } from '../../features/contracts/governance/ContractGovernancePage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: {
    getContractsSummary: vi.fn(() => Promise.resolve({})),
    listContracts: vi.fn(() => Promise.resolve({ items: [] })),
  },
}));
// Isola o hub: as views agregadas têm dependências de forma de dados próprias e não são o alvo deste teste.
vi.mock('../../features/contracts/governance/ContractGovernanceViews', () => ({
  OverviewView: () => null,
  ApprovalsView: () => null,
  ComplianceView: () => null,
  GapsView: () => null,
  AuditView: () => null,
}));

function wrap() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(<QueryClientProvider client={qc}><MemoryRouter><ContractGovernancePage /></MemoryRouter></QueryClientProvider>);
}

describe('ContractGovernancePage hub', () => {
  it('renders the governance tools launch grid', async () => {
    wrap();
    expect(await screen.findByText('Governance tools')).toBeInTheDocument();
    const hrefs = screen.getAllByRole('link').map((a) => a.getAttribute('href'));
    expect(hrefs).toEqual(expect.arrayContaining(['/contracts/playground', '/contracts/migration']));
  });
});
