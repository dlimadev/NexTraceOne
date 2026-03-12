import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { EngineeringGraphPage } from '../../pages/EngineeringGraphPage';
import type { AssetGraph } from '../../types';

vi.mock('../../api', () => ({
  engineeringGraphApi: {
    getGraph: vi.fn(),
    registerService: vi.fn(),
    registerApi: vi.fn(),
  },
}));

import { engineeringGraphApi } from '../../api';

const mockGraph: AssetGraph = {
  services: [
    { id: 's1', name: 'payments-service', team: 'Payments', createdAt: '2024-01-01T00:00:00Z' },
    { id: 's2', name: 'auth-service', team: 'Identity', createdAt: '2024-01-01T00:00:00Z' },
  ],
  apis: [
    { id: 'a1', name: 'Payments API', baseUrl: '/api/payments', ownerServiceId: 's1', createdAt: '2024-01-01T00:00:00Z' },
  ],
  relationships: [
    { apiAssetId: 'a1', consumerServiceId: 's2', trustLevel: 'High' },
  ],
};

function renderGraph() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <EngineeringGraphPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('EngineeringGraphPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('exibe o título da página', () => {
    vi.mocked(engineeringGraphApi.getGraph).mockResolvedValue(mockGraph);
    renderGraph();
    expect(screen.getByRole('heading', { name: /engineering graph/i })).toBeInTheDocument();
  });

  it('exibe as abas de navegação', () => {
    vi.mocked(engineeringGraphApi.getGraph).mockResolvedValue(mockGraph);
    renderGraph();
    expect(screen.getByRole('button', { name: /services/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /apis/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /dependencies/i })).toBeInTheDocument();
  });

  it('exibe os serviços carregados na aba Services', async () => {
    vi.mocked(engineeringGraphApi.getGraph).mockResolvedValue(mockGraph);
    renderGraph();
    await waitFor(() => {
      expect(screen.getByText('payments-service')).toBeInTheDocument();
      expect(screen.getByText('auth-service')).toBeInTheDocument();
    });
  });

  it('navega para a aba APIs e exibe as APIs', async () => {
    vi.mocked(engineeringGraphApi.getGraph).mockResolvedValue(mockGraph);
    renderGraph();
    await userEvent.click(screen.getByRole('button', { name: /apis/i }));
    await waitFor(() => {
      expect(screen.getByText('Payments API')).toBeInTheDocument();
      expect(screen.getByText('/api/payments')).toBeInTheDocument();
    });
  });

  it('navega para a aba Dependencies e exibe relacionamentos', async () => {
    vi.mocked(engineeringGraphApi.getGraph).mockResolvedValue(mockGraph);
    renderGraph();
    await userEvent.click(screen.getByRole('button', { name: /dependencies/i }));
    await waitFor(() => {
      expect(screen.getByText('High')).toBeInTheDocument();
    });
  });

  it('exibe formulário de serviço ao clicar em + Service', async () => {
    vi.mocked(engineeringGraphApi.getGraph).mockResolvedValue(mockGraph);
    renderGraph();
    const serviceBtn = screen.getAllByRole('button', { name: /service/i }).find(
      (btn) => btn.textContent?.trim() === 'Service'
    )!;
    await userEvent.click(serviceBtn);
    expect(screen.getByText('Register Service')).toBeInTheDocument();
  });

  it('exibe formulário de API ao clicar em + API', async () => {
    vi.mocked(engineeringGraphApi.getGraph).mockResolvedValue(mockGraph);
    renderGraph();
    await userEvent.click(screen.getByRole('button', { name: 'API' }));
    expect(screen.getByText('Register API Asset')).toBeInTheDocument();
  });

  it('chama registerService ao submeter o formulário de serviço', async () => {
    vi.mocked(engineeringGraphApi.getGraph).mockResolvedValue({ services: [], apis: [], relationships: [] });
    vi.mocked(engineeringGraphApi.registerService).mockResolvedValue({ id: 's-new' });
    renderGraph();
    const serviceBtn = screen.getAllByRole('button', { name: /service/i }).find(
      (btn) => btn.textContent?.trim() === 'Service'
    )!;
    await userEvent.click(serviceBtn);
    await userEvent.type(screen.getAllByRole('textbox')[0], 'new-service');
    await userEvent.type(screen.getAllByRole('textbox')[1], 'Platform');
    await userEvent.click(screen.getByRole('button', { name: /register/i }));
    await waitFor(() => {
      expect(engineeringGraphApi.registerService).toHaveBeenCalled();
      const [firstArg] = vi.mocked(engineeringGraphApi.registerService).mock.calls[0];
      expect(firstArg).toMatchObject({ name: 'new-service', team: 'Platform' });
    });
  });

  it('exibe mensagem quando não há serviços registados', async () => {
    vi.mocked(engineeringGraphApi.getGraph).mockResolvedValue({ services: [], apis: [], relationships: [] });
    renderGraph();
    await waitFor(() => {
      expect(screen.getByText(/no services registered/i)).toBeInTheDocument();
    });
  });
});
