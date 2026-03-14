import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { DashboardPage } from '../../features/shared/pages/DashboardPage';

vi.mock('../../features/catalog/api/engineeringGraph', () => ({
  engineeringGraphApi: {
    getGraph: vi.fn(),
  },
}));

import { engineeringGraphApi } from '../../features/catalog/api/engineeringGraph';

function renderDashboard() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <DashboardPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('DashboardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('exibe o título do dashboard', () => {
    vi.mocked(engineeringGraphApi.getGraph).mockResolvedValue({
      services: [],
      apis: [],
      relationships: [],
    });
    renderDashboard();
    expect(screen.getByRole('heading', { name: /dashboard/i })).toBeInTheDocument();
  });

  it('exibe os stat cards com labels corretos', () => {
    vi.mocked(engineeringGraphApi.getGraph).mockResolvedValue({
      services: [],
      apis: [],
      relationships: [],
    });
    renderDashboard();
    expect(screen.getByText('Active Services')).toBeInTheDocument();
    expect(screen.getAllByText('Registered APIs').length).toBeGreaterThan(0);
    expect(screen.getByText('Consumer Relations')).toBeInTheDocument();
    expect(screen.getByText('Total Contracts')).toBeInTheDocument();
    expect(screen.getByText('Pending Approvals')).toBeInTheDocument();
  });

  it('exibe serviços e APIs carregados do grafo', async () => {
    vi.mocked(engineeringGraphApi.getGraph).mockResolvedValue({
      services: [
        { id: 's1', name: 'payments-service', team: 'Payments', createdAt: '2024-01-01' },
        { id: 's2', name: 'auth-service', team: 'Identity', createdAt: '2024-01-01' },
      ],
      apis: [
        { id: 'a1', name: 'Payments API', baseUrl: '/api/payments', ownerServiceId: 's1', createdAt: '2024-01-01' },
      ],
      relationships: [],
    });
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText('payments-service')).toBeInTheDocument();
      expect(screen.getByText('auth-service')).toBeInTheDocument();
      expect(screen.getByText('Payments API')).toBeInTheDocument();
    });
  });

  it('exibe mensagem quando não há serviços registados', async () => {
    vi.mocked(engineeringGraphApi.getGraph).mockResolvedValue({
      services: [],
      apis: [],
      relationships: [],
    });
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText(/no services registered yet/i)).toBeInTheDocument();
    });
  });

  it('exibe mensagem quando não há APIs registadas', async () => {
    vi.mocked(engineeringGraphApi.getGraph).mockResolvedValue({
      services: [],
      apis: [],
      relationships: [],
    });
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText(/no apis registered yet/i)).toBeInTheDocument();
    });
  });
});
