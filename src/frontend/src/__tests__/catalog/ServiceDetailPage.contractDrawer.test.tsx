import { describe, it, expect, vi } from 'vitest';
import { screen, fireEvent, waitFor } from '@testing-library/react';
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
// Drawer mockado para asserir o modo sem montar o editor pesado
vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: () => ({ activeEnvironment: null }),
}));
vi.mock('../../features/catalog/components/ServiceLifecyclePanel', () => ({ ServiceLifecyclePanel: () => null }));
vi.mock('../../features/catalog/components/ServiceLinksSection', () => ({ ServiceLinksSection: () => null }));
vi.mock('../../features/ai-hub/components/AssistantPanel', () => ({ AssistantPanel: () => null }));
// Drawer mockado para asserir o modo sem montar o editor pesado
vi.mock('../../features/catalog/components/ServiceContractDrawer', () => ({
  ServiceContractDrawer: ({ state }: { state: { mode: string } }) =>
    state.mode !== 'closed' ? <div data-testid="drawer">{state.mode}</div> : null,
}));

describe('ServiceDetailPage — contract drawer', () => {
  it('opens the drawer in create mode from Add Contract instead of navigating', async () => {
    navigateMock.mockClear();
    renderWithProviders(<ServiceDetailPage />, { routerProps: { initialEntries: ['/services/svc-1'] } });
    // Ir para a aba Contratos e clicar em Add Contract
    fireEvent.click(await screen.findByRole('tab', { name: /contracts/i }));
    const addBtn = await screen.findByText(/Add Contract/i);
    fireEvent.click(addBtn);
    expect(await screen.findByTestId('drawer')).toHaveTextContent('create');
    expect(navigateMock).not.toHaveBeenCalledWith(expect.stringContaining('/contracts/new'));
  });
});
