import { describe, it, expect, vi } from 'vitest';
import { screen, fireEvent } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';
import { ServiceDetailPage } from '../../features/catalog/pages/ServiceDetailPage';

const navigateMock = vi.fn();
vi.mock('react-router-dom', async (orig) => ({
  ...(await orig<typeof import('react-router-dom')>()),
  useNavigate: () => navigateMock,
  useParams: () => ({ serviceId: 'svc-1' }),
  useSearchParams: () => [new URLSearchParams(), vi.fn()],
}));

vi.mock('../../features/catalog/api/serviceCatalog', () => ({
  serviceCatalogApi: {
    getServiceDetail: vi.fn().mockResolvedValue({
      serviceId: 'svc-1', name: 'payments', displayName: 'Payments API', domain: 'Billing',
      serviceType: 'RestApi', criticality: 'Critical', lifecycleStatus: 'Active', exposureType: 'External',
      teamName: 'Payments', technicalOwner: 'ana', apis: [], apiCount: 0,
    }),
    getServiceMaturity: vi.fn().mockResolvedValue(null),
  },
}));
vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: { listContractsByService: vi.fn().mockResolvedValue({ contracts: [], items: [], totalCount: 0 }) },
}));
vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: () => ({ activeEnvironment: null }),
}));
vi.mock('../../features/catalog/components/ServiceLifecyclePanel', () => ({ ServiceLifecyclePanel: () => null }));
vi.mock('../../features/catalog/components/ServiceLinksSection', () => ({ ServiceLinksSection: () => null }));
vi.mock('../../features/ai-hub/components/AssistantPanel', () => ({ AssistantPanel: () => null }));
vi.mock('../../features/catalog/components/ServiceContractDrawer', () => ({
  ServiceContractDrawer: ({ state }: { state: { mode: string } }) =>
    state.mode !== 'closed' ? <div data-testid="drawer">{state.mode}</div> : null,
}));
// Stub da tab de feature flags — evita montar o componente real com queries HTTP
vi.mock('../../features/catalog/components/ServiceFeatureFlagsTab', () => ({
  ServiceFeatureFlagsTab: () => <div data-testid="ff-tab" />,
}));

describe('ServiceDetailPage — feature flags tab', () => {
  it('exibe a tab "Feature Flags" e renderiza o stub ao clicar', async () => {
    renderWithProviders(<ServiceDetailPage />, { routerProps: { initialEntries: ['/services/svc-1'] } });

    // Aguarda o carregamento dos dados e a renderização da tab
    expect(await screen.findByRole('tab', { name: /feature flags/i })).toBeInTheDocument();

    // Clica na tab
    fireEvent.click(screen.getByRole('tab', { name: /feature flags/i }));

    // O stub deve aparecer
    expect(await screen.findByTestId('ff-tab')).toBeInTheDocument();
  });
});
