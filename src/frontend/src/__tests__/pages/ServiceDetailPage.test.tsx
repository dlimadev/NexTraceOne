import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ServiceDetailPage } from '../../features/catalog/pages/ServiceDetailPage';

beforeAll(() => {
  Element.prototype.scrollIntoView = vi.fn();
});

vi.mock('../../contexts/PersonaContext', () => ({
  usePersona: vi.fn().mockReturnValue({
    persona: 'Engineer',
    config: {
      aiContextScopes: ['services', 'contracts', 'incidents'],
      aiSuggestedPromptKeys: [],
      sectionOrder: [],
      highlightedSections: [],
      homeSubtitleKey: '',
      homeWidgets: [],
      quickActions: [],
    },
  }),
}));

vi.mock('../../features/ai-hub/api/aiGovernance', () => ({
  aiGovernanceApi: { sendMessage: vi.fn().mockRejectedValue(new Error('API not available')) },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));

vi.mock('../../features/catalog/api', () => ({
  serviceCatalogApi: {
    getServiceDetail: vi.fn(),
  },
}));

vi.mock('../../features/catalog/api/contracts', () => ({
  contractsApi: {
    listContractsByService: vi.fn(),
  },
}));

import { serviceCatalogApi } from '../../features/catalog/api';
import { contractsApi } from '../../features/catalog/api/contracts';

const mockService = {
  serviceAssetId: 's1',
  name: 'payments-service',
  displayName: 'Payments Service',
  description: 'Handles payment processing',
  teamName: 'Payments Team',
  technicalOwner: 'john.doe',
  businessOwner: 'jane.smith',
  domain: 'Payments',
  systemArea: 'Core',
  serviceType: 'RestApi',
  criticality: 'High' as const,
  lifecycleStatus: 'Active' as const,
  exposureType: 'External',
  documentationUrl: 'https://docs.example.com',
  repositoryUrl: 'https://github.com/example/payments',
  apiCount: 2,
  totalConsumers: 5,
  apis: [
    {
      apiId: 'a1',
      name: 'Payments API',
      routePattern: '/api/payments',
      version: '2.0.0',
      visibility: 'Public',
      consumerCount: 3,
      isDecommissioned: false,
    },
  ],
};

const mockContracts = {
  serviceId: 's1',
  contracts: [
    {
      versionId: 'cv-001',
      apiAssetId: 'a1',
      apiName: 'Payments API',
      apiRoutePattern: '/api/payments',
      semVer: '2.0.0',
      protocol: 'OpenApi',
      lifecycleState: 'Approved',
      isLocked: false,
      createdAt: '2024-01-15T10:00:00Z',
    },
    {
      versionId: 'cv-002',
      apiAssetId: 'a1',
      apiName: 'Payments API',
      apiRoutePattern: '/api/payments',
      semVer: '1.0.0',
      protocol: 'OpenApi',
      lifecycleState: 'Locked',
      isLocked: true,
      createdAt: '2024-01-10T08:00:00Z',
    },
  ],
  totalCount: 2,
};

function renderServiceDetail(serviceId = 's1') {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/services/${serviceId}`]}>
        <Routes>
          <Route path="/services/:serviceId" element={<ServiceDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ServiceDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('exibe estado de loading', () => {
    vi.mocked(serviceCatalogApi.getServiceDetail).mockReturnValue(new Promise(() => {}));
    vi.mocked(contractsApi.listContractsByService).mockReturnValue(new Promise(() => {}));
    renderServiceDetail();
    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it('exibe dados do serviço', async () => {
    vi.mocked(serviceCatalogApi.getServiceDetail).mockResolvedValue(mockService);
    vi.mocked(contractsApi.listContractsByService).mockResolvedValue(mockContracts);
    renderServiceDetail();
    await waitFor(() => {
      expect(screen.getByText('Payments Service')).toBeInTheDocument();
    });
  });

  it('exibe ownership do serviço', async () => {
    vi.mocked(serviceCatalogApi.getServiceDetail).mockResolvedValue(mockService);
    vi.mocked(contractsApi.listContractsByService).mockResolvedValue(mockContracts);
    renderServiceDetail();
    await waitFor(() => {
      expect(screen.getByText('Payments Team')).toBeInTheDocument();
      expect(screen.getByText('john.doe')).toBeInTheDocument();
      expect(screen.getByText('jane.smith')).toBeInTheDocument();
    });
  });

  it('exibe APIs associadas', async () => {
    vi.mocked(serviceCatalogApi.getServiceDetail).mockResolvedValue(mockService);
    vi.mocked(contractsApi.listContractsByService).mockResolvedValue(mockContracts);
    renderServiceDetail();
    await waitFor(() => {
      expect(screen.getAllByText('/api/payments').length).toBeGreaterThanOrEqual(1);
      expect(screen.getAllByText('Payments API').length).toBeGreaterThanOrEqual(1);
    });
  });

  it('exibe contratos vinculados ao serviço', async () => {
    vi.mocked(serviceCatalogApi.getServiceDetail).mockResolvedValue(mockService);
    vi.mocked(contractsApi.listContractsByService).mockResolvedValue(mockContracts);
    renderServiceDetail();
    await waitFor(() => {
      expect(screen.getByText('v2.0.0')).toBeInTheDocument();
      expect(screen.getByText('v1.0.0')).toBeInTheDocument();
    });
  });

  it('exibe link para visualizar contrato', async () => {
    vi.mocked(serviceCatalogApi.getServiceDetail).mockResolvedValue(mockService);
    vi.mocked(contractsApi.listContractsByService).mockResolvedValue(mockContracts);
    renderServiceDetail();
    await waitFor(() => {
      const viewLinks = screen.getAllByText(/view contract/i);
      expect(viewLinks.length).toBeGreaterThanOrEqual(1);
    });
  });

  it('exibe mensagem quando não há contratos', async () => {
    vi.mocked(serviceCatalogApi.getServiceDetail).mockResolvedValue(mockService);
    vi.mocked(contractsApi.listContractsByService).mockResolvedValue({
      serviceId: 's1',
      contracts: [],
      totalCount: 0,
    });
    renderServiceDetail();
    await waitFor(() => {
      expect(screen.getByText(/no contracts linked/i)).toBeInTheDocument();
    });
  });

  it('exibe estado de erro quando serviço não é encontrado', async () => {
    vi.mocked(serviceCatalogApi.getServiceDetail).mockRejectedValue(new Error('Not found'));
    vi.mocked(contractsApi.listContractsByService).mockRejectedValue(new Error('Not found'));
    renderServiceDetail();
    await waitFor(() => {
      expect(screen.getByText(/error/i)).toBeInTheDocument();
    });
  });
});
