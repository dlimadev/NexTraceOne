import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { ServiceDetailPage } from '../../features/catalog/pages/ServiceDetailPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, d?: string) => (typeof d === 'string' ? d : k) }) }));
vi.mock('../../features/catalog/components/ServiceContractDrawer', () => ({ ServiceContractDrawer: () => null }));
vi.mock('../../contexts/EnvironmentContext', () => ({ useEnvironment: () => ({ activeEnvironment: null }) }));
// Painéis pesados exigem providers (Toast, etc.) — irrelevantes para o checklist.
vi.mock('../../features/catalog/components/ServiceLifecyclePanel', () => ({ ServiceLifecyclePanel: () => null }));
vi.mock('../../features/catalog/components/ServiceLinksSection', () => ({ ServiceLinksSection: () => null }));
vi.mock('../../features/ai-hub/components/AssistantPanel', () => ({ AssistantPanel: () => null }));

const service = {
  id: 'svc-1', name: 'orders-api', displayName: 'orders-api', domain: 'Commerce',
  serviceType: 'RestApi', criticality: 'Medium', exposureType: 'Internal',
  lifecycleStatus: 'Planning', teamName: 'Orders', technicalOwner: '', apis: [], apiCount: 0,
};
vi.mock('../../features/catalog/api', () => ({
  serviceCatalogApi: {
    getServiceDetail: vi.fn(() => Promise.resolve(service)),
    getServiceMaturity: vi.fn(() => Promise.resolve({ level: 'Bronze', dimensions: [] })),
  },
  contractsApi: { listContractsByService: vi.fn(() => Promise.resolve({ contracts: [], totalCount: 0 })) },
}));

function renderAt(id: string) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={[`/services/${id}`]}>
        <Routes><Route path="/services/:serviceId" element={<ServiceDetailPage />} /></Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ServiceDetailPage setup checklist', () => {
  it('renders the setup checklist for a Planning service', async () => {
    renderAt('svc-1');
    await waitFor(() => expect(screen.getByText('serviceSetup.title')).toBeInTheDocument());
  });
});
