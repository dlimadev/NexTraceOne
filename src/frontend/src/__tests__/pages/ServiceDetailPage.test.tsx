import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import { Route, Routes } from 'react-router-dom';
import { renderWithProviders } from '../test-utils';
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

vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn().mockReturnValue({
    activeEnvironmentId: 'tenant-1-prod',
    activeEnvironment: { id: 'tenant-1-prod', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
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
    getServiceMaturity: vi.fn(),
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
  return renderWithProviders(
    <Routes>
      <Route path="/services/:serviceId" element={<ServiceDetailPage />} />
    </Routes>,
    { routerProps: { initialEntries: [`/services/${serviceId}`] } },
  );
}

describe('ServiceDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Por defeito, sem scorecard de maturidade → célula de maturidade honest-null.
    vi.mocked(serviceCatalogApi.getServiceMaturity).mockRejectedValue(new Error('no maturity'));
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
      expect(screen.getAllByText('Payments Service')[0]).toBeInTheDocument();
    });
  });

  it('exibe ownership do serviço', async () => {
    vi.mocked(serviceCatalogApi.getServiceDetail).mockResolvedValue(mockService);
    vi.mocked(contractsApi.listContractsByService).mockResolvedValue(mockContracts);
    renderServiceDetail();
    await waitFor(() => {
      // teamName appears in both EntityHeader meta and Ownership card
      expect(screen.getAllByText('Payments Team').length).toBeGreaterThanOrEqual(1);
      // v5: technicalOwner appears in both the identity card (MetaRow) and the Ownership section (FieldRow)
      expect(screen.getAllByText('john.doe').length).toBeGreaterThanOrEqual(1);
      expect(screen.getAllByText('jane.smith').length).toBeGreaterThanOrEqual(1);
    });
  });

  it('exibe APIs associadas', async () => {
    vi.mocked(serviceCatalogApi.getServiceDetail).mockResolvedValue(mockService);
    vi.mocked(contractsApi.listContractsByService).mockResolvedValue(mockContracts);
    renderServiceDetail();
    // Wait for data to load, then navigate to APIs tab
    await waitFor(() => expect(screen.getAllByText('Payments Service')[0]).toBeInTheDocument());
    const apisTab = screen.getAllByRole('tab').find(el => el.textContent?.includes('APIs') || el.textContent?.match(/api/i));
    if (apisTab) fireEvent.click(apisTab);
    await waitFor(() => {
      expect(screen.getAllByText('/api/payments').length).toBeGreaterThanOrEqual(1);
      expect(screen.getAllByText('Payments API').length).toBeGreaterThanOrEqual(1);
    });
  });

  it('exibe contratos vinculados ao serviço', async () => {
    vi.mocked(serviceCatalogApi.getServiceDetail).mockResolvedValue(mockService);
    vi.mocked(contractsApi.listContractsByService).mockResolvedValue(mockContracts);
    renderServiceDetail();
    await waitFor(() => expect(screen.getAllByText('Payments Service')[0]).toBeInTheDocument());
    const contractsTab = screen.getAllByRole('tab').find(el => el.textContent?.match(/contract/i));
    if (contractsTab) fireEvent.click(contractsTab);
    await waitFor(() => {
      expect(screen.getByText('v2.0.0')).toBeInTheDocument();
      expect(screen.getByText('v1.0.0')).toBeInTheDocument();
    });
  });

  it('exibe link para visualizar contrato', async () => {
    vi.mocked(serviceCatalogApi.getServiceDetail).mockResolvedValue(mockService);
    vi.mocked(contractsApi.listContractsByService).mockResolvedValue(mockContracts);
    renderServiceDetail();
    await waitFor(() => expect(screen.getAllByText('Payments Service')[0]).toBeInTheDocument());
    const contractsTab = screen.getAllByRole('tab').find(el => el.textContent?.match(/contract/i));
    if (contractsTab) fireEvent.click(contractsTab);
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
    await waitFor(() => expect(screen.getAllByText('Payments Service')[0]).toBeInTheDocument());
    const contractsTab = screen.getAllByRole('tab').find(el => el.textContent?.match(/contract/i));
    if (contractsTab) fireEvent.click(contractsTab);
    await waitFor(() => {
      // v5: empty-contracts message appears in both the always-visible stacked section and the contracts tab content
      expect(screen.getAllByText(/no contracts linked/i).length).toBeGreaterThanOrEqual(1);
    });
  });

  it('health strip: liga maturidade + SLO reais quando disponíveis', async () => {
    vi.mocked(serviceCatalogApi.getServiceDetail).mockResolvedValue({ ...mockService, sloTarget: '99.9%' });
    vi.mocked(contractsApi.listContractsByService).mockResolvedValue(mockContracts);
    vi.mocked(serviceCatalogApi.getServiceMaturity).mockResolvedValue({
      serviceId: 's1', serviceName: 'payments-service', displayName: 'Payments Service',
      teamName: 'Payments Team', domain: 'Payments', level: 'Managed', overallScore: 82,
      dimensions: [], computedAt: '2026-01-01T00:00:00Z',
    });
    renderServiceDetail();
    await waitFor(() => {
      expect(screen.getByText('Managed')).toBeInTheDocument();
      expect(screen.getAllByText('99.9%').length).toBeGreaterThanOrEqual(1);
    });
  });

  it('health strip: honest-null — sem valores fabricados quando não há sinais', async () => {
    // service sem sloTarget + maturidade a falhar (beforeEach) → nenhum sinal.
    vi.mocked(serviceCatalogApi.getServiceDetail).mockResolvedValue(mockService);
    vi.mocked(contractsApi.listContractsByService).mockResolvedValue(mockContracts);
    renderServiceDetail();
    await waitFor(() => expect(screen.getAllByText('Payments Service')[0]).toBeInTheDocument());
    // O placeholder de maturidade fabricado anterior ("B+") não deve existir.
    expect(screen.queryByText('B+')).not.toBeInTheDocument();
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
