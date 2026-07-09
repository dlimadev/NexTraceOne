import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ContractHealthDashboardPage } from '../../features/contracts/governance/ContractHealthDashboardPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: {
    getHealthDashboard: vi.fn(() => Promise.resolve({
      totalContractVersions: 3, distinctContracts: 3, deprecatedVersions: 0,
      filteredCount: 3, percentWithExamples: 80, percentWithCanonicalEntities: 60,
      healthScore: 72,
      topViolations: [{ contractVersionId: 'cv-1', semVer: '1.2.0', violationCount: 4, topRuleIds: ['no-empty'] }],
    })),
  },
}));

function wrap() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(<QueryClientProvider client={qc}><MemoryRouter><ContractHealthDashboardPage /></MemoryRouter></QueryClientProvider>);
}

describe('ContractHealthDashboardPage wiring', () => {
  it('links a violation row to the contract portal', async () => {
    wrap();
    const row = await screen.findByRole('link', { name: /1\.2\.0/ });
    expect(row.getAttribute('href')).toBe('/contracts/portal/cv-1');
  });

  it('exposes a View timeline action', async () => {
    wrap();
    const link = await screen.findByRole('link', { name: /timeline/i });
    expect(link.getAttribute('href')).toBe('/contracts/health/timeline');
  });
});
