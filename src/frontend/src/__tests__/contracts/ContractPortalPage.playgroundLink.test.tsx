import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { ContractPortalPage } from '../../features/contracts/portal/ContractPortalPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: {
    getDetail: vi.fn(() => Promise.resolve({
      apiAssetId: 'a-1', apiName: 'orders-api', protocol: 'OpenApi', semVer: '1.0.0',
      lifecycleState: 'Approved', routePattern: '/orders', createdAt: '2026-01-01T00:00:00Z',
    })),
    listRuleViolations: vi.fn(() => Promise.resolve([])),
    getHistory: vi.fn(() => Promise.resolve([])),
  },
}));
vi.mock('../../features/contracts/workspace/toStudioContract', () => ({
  toStudioContract: () => ({
    friendlyName: 'Orders API', functionalDescription: 'desc', owner: 'team', domain: 'Commerce',
    criticality: 'High', technicalName: 'orders-api',
    consumers: [], producers: [], operations: [], schemas: [], tags: [],
  }),
}));

function wrap() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={['/contracts/portal/cv-1']}>
        <Routes><Route path="/contracts/portal/:contractVersionId" element={<ContractPortalPage />} /></Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ContractPortalPage playground link', () => {
  it('links the header to the playground preloaded with the contract', async () => {
    wrap();
    const link = await screen.findByRole('link', { name: /try in playground/i });
    expect(link.getAttribute('href')).toBe('/contracts/playground?contractVersionId=cv-1');
  });
});
