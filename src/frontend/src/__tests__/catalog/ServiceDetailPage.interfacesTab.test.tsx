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

// Stubs leves para evitar queries pesadas nos sub-componentes
vi.mock('../../features/catalog/components/ServiceApisSection', () => ({
  ServiceApisSection: () => <div data-testid="apis-section" />,
}));
vi.mock('../../features/catalog/components/ServiceInterfacesTab', () => ({
  ServiceInterfacesTab: () => <div data-testid="interfaces-tab" />,
}));

describe('ServiceDetailPage — aba unificada Interfaces & APIs', () => {
  it('exibe a aba unificada e não exibe a aba separada APIs', async () => {
    renderWithProviders(<ServiceDetailPage />, { routerProps: { initialEntries: ['/services/svc-1'] } });

    // A aba unificada deve existir
    expect(await screen.findByRole('tab', { name: /interfaces & apis/i })).toBeInTheDocument();

    // Não deve existir uma aba separada de APIs
    expect(screen.queryByRole('tab', { name: /^apis \(/i })).not.toBeInTheDocument();
  });

  it('ao clicar na aba unificada renderiza tanto apis-section quanto interfaces-tab', async () => {
    renderWithProviders(<ServiceDetailPage />, { routerProps: { initialEntries: ['/services/svc-1'] } });

    // Aguarda a aba estar disponível e clica
    fireEvent.click(await screen.findByRole('tab', { name: /interfaces & apis/i }));

    // Ambas as secções devem estar presentes
    expect(await screen.findByTestId('apis-section')).toBeInTheDocument();
    expect(screen.getByTestId('interfaces-tab')).toBeInTheDocument();
  });
});
