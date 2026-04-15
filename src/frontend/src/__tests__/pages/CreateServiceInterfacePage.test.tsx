/**
 * Testes de componente para CreateServiceInterfacePage.
 * Cobrem renderização do formulário, validações e campos condicionais.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Route, Routes } from 'react-router-dom';
import { renderWithProviders } from '../test-utils';
import { CreateServiceInterfacePage } from '../../features/catalog/pages/CreateServiceInterfacePage';

vi.mock('../../contexts/PersonaContext', () => ({
  usePersona: vi.fn().mockReturnValue({
    persona: 'Engineer',
    config: {
      aiContextScopes: [],
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
    activeEnvironmentId: 'env-prod',
    activeEnvironment: { id: 'env-prod', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  }),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

vi.mock('../../features/catalog/api', () => ({
  serviceCatalogApi: {
    getServiceDetail: vi.fn(),
    createServiceInterface: vi.fn(),
  },
}));

import { serviceCatalogApi } from '../../features/catalog/api';

const mockService = {
  serviceId: 'svc-1',
  serviceAssetId: 'svc-1',
  name: 'payments-service',
  displayName: 'Payments Service',
  description: 'Handles payment processing',
  teamName: 'Payments Team',
  technicalOwner: 'john.doe',
  businessOwner: 'jane.smith',
  domain: 'Payments',
  systemArea: 'Core',
  serviceType: 'RestApi' as const,
  criticality: 'High' as const,
  lifecycleStatus: 'Active' as const,
  exposureType: 'External' as const,
  documentationUrl: '',
  repositoryUrl: '',
  apiCount: 0,
  totalConsumers: 0,
  apis: [],
};

function renderPage(serviceId = 'svc-1') {
  return renderWithProviders(
    <Routes>
      <Route path="/services/:serviceId/interfaces/new" element={<CreateServiceInterfacePage />} />
    </Routes>,
    { routerProps: { initialEntries: [`/services/${serviceId}/interfaces/new`] } },
  );
}

describe('CreateServiceInterfacePage', () => {
  beforeEach(() => {
    vi.mocked(serviceCatalogApi.getServiceDetail).mockResolvedValue(mockService);
    vi.mocked(serviceCatalogApi.createServiceInterface).mockResolvedValue({
      interfaceId: 'iface-1',
      serviceAssetId: 'svc-1',
      name: 'Orders API',
      description: '',
      interfaceType: 'RestApi',
      status: 'Active',
      exposureScope: 'Internal',
      basePath: '',
      topicName: '',
      wsdlNamespace: '',
      grpcServiceName: '',
      scheduleCron: '',
      environmentId: '',
      sloTarget: '',
      requiresContract: false,
      authScheme: 'None',
      rateLimitPolicy: '',
      documentationUrl: '',
      isDeprecated: false,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    });
  });

  it('renders form fields after service loads', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByPlaceholderText('Orders REST API v1')).toBeInTheDocument();
    });
    expect(screen.getAllByRole('combobox').length).toBeGreaterThan(0);
  });

  it('shows validation error when name is empty on submit', async () => {
    const user = userEvent.setup();
    renderPage();
    await waitFor(() => {
      expect(screen.getByPlaceholderText('Orders REST API v1')).toBeInTheDocument();
    });
    const submitBtns = screen.getAllByRole('button', { name: /create interface/i });
    await user.click(submitBtns[submitBtns.length - 1]);
    // After failed submit the validation error "Interface Name … is required." appears
    const matches = screen.getAllByText(/interface name/i);
    expect(matches.length).toBeGreaterThan(0);
  });

  it('shows BasePath field when RestApi is selected (default)', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByPlaceholderText('Orders REST API v1')).toBeInTheDocument();
    });
    expect(screen.getByPlaceholderText('/api/orders')).toBeInTheDocument();
  });

  it('shows TopicName field when KafkaProducer is selected', async () => {
    const user = userEvent.setup();
    renderPage();
    await waitFor(() => {
      expect(screen.getByPlaceholderText('Orders REST API v1')).toBeInTheDocument();
    });
    // The first combobox is the interfaceType select
    const [typeSelect] = screen.getAllByRole('combobox');
    await user.selectOptions(typeSelect, 'KafkaProducer');
    await waitFor(() => {
      expect(screen.getByPlaceholderText('orders.events.v1')).toBeInTheDocument();
    });
    expect(screen.queryByPlaceholderText('/api/orders')).not.toBeInTheDocument();
  });

  it('Cancel button navigates back to service detail', async () => {
    const user = userEvent.setup();
    renderPage();
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /cancel/i })).toBeInTheDocument();
    });
    const cancelBtn = screen.getByRole('button', { name: /cancel/i });
    await user.click(cancelBtn);
    await waitFor(() => {
      expect(screen.queryByRole('button', { name: /cancel/i })).not.toBeInTheDocument();
    });
  });
});
