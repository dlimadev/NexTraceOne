import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ContractHealthDashboardPage } from '../../features/contracts/governance/ContractHealthDashboardPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
const getHealthDashboard = vi.fn(() => Promise.resolve({
  totalContractVersions: 0, distinctContracts: 0, deprecatedVersions: 0, filteredCount: 0,
  percentWithExamples: 0, percentWithCanonicalEntities: 0, healthScore: 0, topViolations: [],
}));
vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: { getHealthDashboard: (...a: unknown[]) => getHealthDashboard(...a) },
}));

function wrap() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(<QueryClientProvider client={qc}><MemoryRouter><ContractHealthDashboardPage /></MemoryRouter></QueryClientProvider>);
}

describe('ContractHealthDashboardPage filters', () => {
  it('refetches with contractType when the type filter changes', async () => {
    wrap();
    await waitFor(() => expect(getHealthDashboard).toHaveBeenCalled());
    const select = screen.getByLabelText(/Type/i) as HTMLSelectElement;
    fireEvent.change(select, { target: { value: 'RestApi' } });
    await waitFor(() => expect(getHealthDashboard).toHaveBeenCalledWith(expect.objectContaining({ contractType: 'RestApi' })));
  });
});
