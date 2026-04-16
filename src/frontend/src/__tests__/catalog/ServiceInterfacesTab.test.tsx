/**
 * Testes de componente para ServiceInterfacesTab.
 * Cobrem estados de loading, lista de interfaces e estado vazio.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';
import { ServiceInterfacesTab } from '../../features/catalog/components/ServiceInterfacesTab';

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

vi.mock('../../features/catalog/api', () => ({
  serviceCatalogApi: {
    listServiceInterfaces: vi.fn(),
  },
}));

import { serviceCatalogApi } from '../../features/catalog/api';

const mockInterfaces = [
  {
    interfaceId: 'iface-1',
    serviceAssetId: 'svc-1',
    name: 'Orders REST API',
    description: 'REST interface for order management',
    interfaceType: 'RestApi' as const,
    status: 'Active' as const,
    exposureScope: 'External' as const,
    basePath: '/api/orders',
    topicName: '',
    wsdlNamespace: '',
    grpcServiceName: '',
    scheduleCron: '',
    environmentId: '',
    sloTarget: '99.9',
    requiresContract: true,
    authScheme: 'OAuth2' as const,
    rateLimitPolicy: '',
    documentationUrl: 'https://docs.example.com',
    isDeprecated: false,
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z',
  },
  {
    interfaceId: 'iface-2',
    serviceAssetId: 'svc-1',
    name: 'Order Events',
    description: 'Kafka topic for order events',
    interfaceType: 'KafkaProducer' as const,
    status: 'Active' as const,
    exposureScope: 'Internal' as const,
    basePath: '',
    topicName: 'orders.events.v1',
    wsdlNamespace: '',
    grpcServiceName: '',
    scheduleCron: '',
    environmentId: '',
    sloTarget: '',
    requiresContract: true,
    authScheme: 'None' as const,
    rateLimitPolicy: '',
    documentationUrl: '',
    isDeprecated: false,
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z',
  },
];

function renderTab(serviceId = 'svc-1') {
  return renderWithProviders(<ServiceInterfacesTab serviceId={serviceId} />);
}

describe('ServiceInterfacesTab', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders loading state initially', () => {
    vi.mocked(serviceCatalogApi.listServiceInterfaces).mockImplementation(
      () => new Promise(() => {}), // never resolves
    );
    const { container } = renderTab();
    // Either a spinner or empty content — page should not show interface list yet
    expect(screen.queryByText('Orders REST API')).not.toBeInTheDocument();
    expect(container).toBeDefined();
  });

  it('renders interface list when data is available', async () => {
    vi.mocked(serviceCatalogApi.listServiceInterfaces).mockResolvedValue(mockInterfaces);
    renderTab();
    await waitFor(() => {
      expect(screen.getByText('Orders REST API')).toBeInTheDocument();
    });
    expect(screen.getByText('Order Events')).toBeInTheDocument();
    expect(screen.getByText('/api/orders')).toBeInTheDocument();
    expect(screen.getByText('orders.events.v1')).toBeInTheDocument();
  });

  it('shows empty state when no interfaces', async () => {
    vi.mocked(serviceCatalogApi.listServiceInterfaces).mockResolvedValue([]);
    renderTab();
    await waitFor(() => {
      expect(screen.getByText(/no interfaces registered/i)).toBeInTheDocument();
    });
  });

  it('"Add Interface" button is visible', async () => {
    vi.mocked(serviceCatalogApi.listServiceInterfaces).mockResolvedValue([]);
    renderTab();
    await waitFor(() => {
      expect(screen.getAllByRole('button', { name: /add interface/i }).length).toBeGreaterThan(0);
    });
  });

  it('shows "Add Interface" button even when interfaces exist', async () => {
    vi.mocked(serviceCatalogApi.listServiceInterfaces).mockResolvedValue(mockInterfaces);
    renderTab();
    await waitFor(() => {
      expect(screen.getByText('Orders REST API')).toBeInTheDocument();
    });
    expect(screen.getByRole('button', { name: /add interface/i })).toBeInTheDocument();
  });
});
