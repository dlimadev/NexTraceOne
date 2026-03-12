import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { PromotionPage } from '../../pages/PromotionPage';
import type { PromotionRequest, PagedList } from '../../types';

vi.mock('../../api', () => ({
  promotionApi: {
    listRequests: vi.fn(),
    createRequest: vi.fn(),
    promote: vi.fn(),
    reject: vi.fn(),
  },
}));

import { promotionApi } from '../../api';

const mockRequests: PagedList<PromotionRequest> = {
  items: [
    {
      id: 'pr-1',
      releaseId: 'rel-001-0000-0000',
      sourceEnvironment: 'staging',
      targetEnvironment: 'production',
      status: 'Approved',
      gateResults: [
        { gateName: 'Linting Passed', passed: true },
        { gateName: 'CI Tests Passed', passed: true },
        { gateName: 'Blast Radius Acceptable', passed: false, message: 'Too many consumers' },
      ],
      createdAt: '2024-01-15T10:00:00Z',
    },
    {
      id: 'pr-2',
      releaseId: 'rel-002-0000-0000',
      sourceEnvironment: 'development',
      targetEnvironment: 'staging',
      status: 'Promoted',
      gateResults: [],
      createdAt: '2024-01-14T08:00:00Z',
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 20,
  totalPages: 1,
};

function renderPromotion() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <PromotionPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('PromotionPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('exibe o título da página', () => {
    vi.mocked(promotionApi.listRequests).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0,
    });
    renderPromotion();
    expect(screen.getByRole('heading', { name: 'Promotion' })).toBeInTheDocument();
  });

  it('exibe o pipeline de ambientes', () => {
    vi.mocked(promotionApi.listRequests).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0,
    });
    renderPromotion();
    expect(screen.getByText('development')).toBeInTheDocument();
    expect(screen.getByText('staging')).toBeInTheDocument();
    expect(screen.getByText('production')).toBeInTheDocument();
  });

  it('exibe o botão para criar nova requisição', () => {
    vi.mocked(promotionApi.listRequests).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0,
    });
    renderPromotion();
    expect(screen.getByRole('button', { name: /new promotion request/i })).toBeInTheDocument();
  });

  it('exibe o formulário ao clicar no botão de nova requisição', async () => {
    vi.mocked(promotionApi.listRequests).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0,
    });
    renderPromotion();
    await userEvent.click(screen.getByRole('button', { name: /new promotion request/i }));
    expect(screen.getByPlaceholderText(/uuid of the release/i)).toBeInTheDocument();
  });

  it('exibe as requisições carregadas da API', async () => {
    vi.mocked(promotionApi.listRequests).mockResolvedValue(mockRequests);
    renderPromotion();
    await waitFor(() => {
      expect(screen.getAllByText(/staging/i).length).toBeGreaterThan(0);
      expect(screen.getAllByText(/production/i).length).toBeGreaterThan(0);
    });
  });

  it('exibe os resultados de gates em requisições pendentes', async () => {
    vi.mocked(promotionApi.listRequests).mockResolvedValue(mockRequests);
    renderPromotion();
    await waitFor(() => {
      expect(screen.getByText('Linting Passed')).toBeInTheDocument();
      expect(screen.getByText('CI Tests Passed')).toBeInTheDocument();
    });
  });

  it('exibe mensagem quando não há requisições', async () => {
    vi.mocked(promotionApi.listRequests).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0,
    });
    renderPromotion();
    await waitFor(() => {
      expect(screen.getByText(/no promotion requests yet/i)).toBeInTheDocument();
    });
  });

  it('exibe mensagem de erro quando API falha', async () => {
    vi.mocked(promotionApi.listRequests).mockRejectedValue(new Error('Server error'));
    renderPromotion();
    await waitFor(() => {
      expect(screen.getByText(/failed to load promotion requests/i)).toBeInTheDocument();
    });
  });

  it('chama createRequest ao submeter o formulário', async () => {
    vi.mocked(promotionApi.listRequests).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0,
    });
    vi.mocked(promotionApi.createRequest).mockResolvedValue({ id: 'pr-new' });
    renderPromotion();
    await userEvent.click(screen.getByRole('button', { name: /new promotion request/i }));
    const releaseInput = screen.getByPlaceholderText(/uuid of the release/i);
    await userEvent.type(releaseInput, 'rel-abc-123');
    await userEvent.click(screen.getByRole('button', { name: /create request/i }));
    await waitFor(() => {
      expect(promotionApi.createRequest).toHaveBeenCalled();
      const [firstArg] = vi.mocked(promotionApi.createRequest).mock.calls[0];
      expect(firstArg).toMatchObject({ releaseId: 'rel-abc-123' });
    });
  });

  it('exibe o total de requisições', async () => {
    vi.mocked(promotionApi.listRequests).mockResolvedValue(mockRequests);
    renderPromotion();
    await waitFor(() => {
      expect(screen.getByText('2 total')).toBeInTheDocument();
    });
  });
});
