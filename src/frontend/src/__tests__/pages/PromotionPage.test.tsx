import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../test-utils';
import { PromotionPage } from '../../features/change-governance/pages/PromotionPage';
import type { PromotionRequest, PagedList } from '../../types';

vi.mock('../../features/change-governance/api', () => ({
  promotionApi: {
    listRequests: vi.fn(),
    createRequest: vi.fn(),
    promote: vi.fn(),
    reject: vi.fn(),
  },
  changeIntelligenceApi: {
    listRecentReleases: vi.fn(),
  },
}));

vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn().mockReturnValue({
    activeEnvironmentId: 'tenant-1-prod',
    activeEnvironment: { id: 'tenant-1-prod', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [
      { id: 'tenant-1-dev', name: 'Development', profile: 'development', isProductionLike: false },
      { id: 'tenant-1-stg', name: 'Staging', profile: 'staging', isProductionLike: false },
      { id: 'tenant-1-prod', name: 'Production', profile: 'production', isProductionLike: true },
    ],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  }),
}));

import { promotionApi, changeIntelligenceApi } from '../../features/change-governance/api';

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
  return renderWithProviders(<PromotionPage />);
}

describe('PromotionPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(changeIntelligenceApi.listRecentReleases).mockResolvedValue({
      items: [
        { id: 'rel-abc-123', apiAssetId: 'asset-1', serviceName: 'order-service', version: '1.2.0', environment: 'staging', status: 'Deployed', changeLevel: 'Minor', changeScore: 75, workItemReference: null, deployedAt: '2024-01-15T10:00:00Z', createdAt: '2024-01-15T10:00:00Z' },
        { id: 'rel-def-456', apiAssetId: 'asset-2', serviceName: 'payment-service', version: '2.0.0', environment: 'production', status: 'Deployed', changeLevel: 'Major', changeScore: 50, workItemReference: null, deployedAt: '2024-01-14T08:00:00Z', createdAt: '2024-01-14T08:00:00Z' },
      ],
      totalCount: 2,
      page: 1,
      pageSize: 50,
      totalPages: 1,
    });
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
    expect(screen.getByText('Development')).toBeInTheDocument();
    expect(screen.getByText('Staging')).toBeInTheDocument();
    expect(screen.getByText('Production')).toBeInTheDocument();
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
    await waitFor(() => {
      expect(screen.getByText(/select a release/i)).toBeInTheDocument();
    });
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
    await waitFor(() => {
      expect(screen.getByText(/select a release/i)).toBeInTheDocument();
    });
    const releaseSelect = screen.getByText(/select a release/i).closest('select')!;
    await userEvent.selectOptions(releaseSelect, 'rel-abc-123');
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
