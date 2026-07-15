/**
 * Testes para o badge "API" nos tipos de interface consumíveis (ServiceInterfacesTab).
 * Assere que o badge aparece apenas para RestApi/GraphqlApi/GrpcService/SoapService.
 */
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';
import { ServiceInterfacesTab } from '../../features/catalog/components/ServiceInterfacesTab';
import type { ServiceInterface } from '../../types';

vi.mock('react-i18next', async () => {
  const actual = await vi.importActual<typeof import('react-i18next')>('react-i18next');
  return {
    ...actual,
    useTranslation: () => ({
      t: (key: string, fallback?: string) => fallback ?? key,
      i18n: { language: 'en' },
    }),
  };
});

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn(), put: vi.fn(), delete: vi.fn() },
}));

vi.mock('../../features/catalog/api', () => ({
  serviceCatalogApi: {
    listServiceInterfaces: vi.fn(),
  },
}));

import { serviceCatalogApi } from '../../features/catalog/api';

// Interface RestApi — deve exibir o badge "API"
const restApiInterface: ServiceInterface = {
  interfaceId: 'iface-rest-1',
  serviceAssetId: 'svc-1',
  name: 'Orders REST API',
  description: 'REST interface for order management',
  interfaceType: 'RestApi',
  status: 'Active',
  exposureScope: 'External',
  basePath: '/api/orders',
  topicName: '',
  wsdlNamespace: '',
  grpcServiceName: '',
  scheduleCron: '',
  environmentId: 'env-prod',
  sloTarget: '99.9',
  requiresContract: true,
  authScheme: 'OAuth2',
  rateLimitPolicy: '',
  documentationUrl: 'https://docs.example.com',
  isDeprecated: false,
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
};

// Interface KafkaProducer — NÃO deve exibir o badge "API"
const kafkaInterface: ServiceInterface = {
  interfaceId: 'iface-kafka-1',
  serviceAssetId: 'svc-1',
  name: 'Order Events',
  description: 'Kafka topic for order events',
  interfaceType: 'KafkaProducer',
  status: 'Active',
  exposureScope: 'Internal',
  basePath: '',
  topicName: 'orders.events.v1',
  wsdlNamespace: '',
  grpcServiceName: '',
  scheduleCron: '',
  environmentId: 'env-prod',
  sloTarget: '',
  requiresContract: false,
  authScheme: 'None',
  rateLimitPolicy: '',
  documentationUrl: '',
  isDeprecated: false,
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
};

describe('ServiceInterfacesTab — badge API', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('exibe o badge API apenas para tipos de interface consumíveis (RestApi sim, KafkaProducer não)', async () => {
    vi.mocked(serviceCatalogApi.listServiceInterfaces).mockResolvedValue([
      restApiInterface,
      kafkaInterface,
    ]);

    renderWithProviders(<ServiceInterfacesTab serviceId="svc-1" />);

    await waitFor(() => {
      expect(screen.getByText('Orders REST API')).toBeInTheDocument();
    });

    // a interface RestApi mostra o badge "API"; a KafkaProducer não
    const apiBadges = screen.getAllByText('API');
    expect(apiBadges.length).toBe(1);
  });
});
