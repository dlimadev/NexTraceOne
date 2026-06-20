import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { CreateContractPage } from '../../features/contracts/create/CreateContractPage';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (k: string, d?: string) => d ?? k }),
}));

vi.mock('../../features/contracts/api/contractStudio', () => ({
  contractStudioApi: {
    createDraft: vi.fn(),
    getDraft: vi.fn(),
    submitForReview: vi.fn(),
  },
}));

vi.mock('../../features/catalog/api/serviceCatalog', () => ({
  serviceCatalogApi: {
    listServices: vi.fn(),
    getServiceDetail: vi.fn(),
    getServicesSummary: vi.fn(),
  },
}));

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: vi.fn(() => ({
    user: { id: 'user-1', name: 'Test User', email: 'test@example.com', roles: [] },
    isAuthenticated: true,
  })),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { serviceCatalogApi } from '../../features/catalog/api/serviceCatalog';

function renderPage() {
  // O hook lê `.items` da resposta de listServices.
  vi.mocked(serviceCatalogApi.listServices).mockResolvedValue({ items: [], totalCount: 0 } as never);
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <CreateContractPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('CreateContractPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders without crashing', () => {
    const { container } = renderPage();
    expect(container).toBeDefined();
  });

  it('renders the live identity card and the four form tabs', async () => {
    renderPage();
    expect(await screen.findByText('Resumo atualiza ao vivo')).toBeInTheDocument();
    expect(screen.getByText('Serviço')).toBeInTheDocument();
    expect(screen.getByText('Tipo & Modo')).toBeInTheDocument();
    expect(screen.getByText('Detalhes')).toBeInTheDocument();
    expect(screen.getByText('Confirmar')).toBeInTheDocument();
  });
});
