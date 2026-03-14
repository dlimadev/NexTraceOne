import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ReleasesPage } from '../../features/change-governance/pages/ReleasesPage';
import type { Release, PagedList } from '../../types';

vi.mock('../../features/change-governance/api', () => ({
  changeIntelligenceApi: {
    listReleases: vi.fn(),
    notifyDeployment: vi.fn(),
  },
}));

import { changeIntelligenceApi } from '../../features/change-governance/api';

const mockReleases: PagedList<Release> = {
  items: [
    {
      id: 'rel-001',
      apiAssetId: 'api-001',
      apiAssetName: 'Payments API',
      version: '2.1.0',
      environment: 'production',
      changeLevel: 3,
      deploymentState: 'Succeeded',
      riskScore: 0.75,
      createdAt: '2024-01-15T10:00:00Z',
    },
    {
      id: 'rel-002',
      apiAssetId: 'api-001',
      apiAssetName: 'Payments API',
      version: '2.0.0',
      environment: 'staging',
      changeLevel: 1,
      deploymentState: 'Failed',
      riskScore: 0.2,
      createdAt: '2024-01-14T09:00:00Z',
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 20,
  totalPages: 1,
};

function renderReleases() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ReleasesPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('ReleasesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('exibe o título da página', () => {
    renderReleases();
    expect(screen.getByRole('heading', { name: /releases/i })).toBeInTheDocument();
  });

  it('exibe o botão de Notify Deployment', () => {
    renderReleases();
    expect(screen.getByRole('button', { name: /notify deployment/i })).toBeInTheDocument();
  });

  it('exibe a instrução para inserir API Asset ID', () => {
    renderReleases();
    expect(screen.getByText(/enter an api asset id above/i)).toBeInTheDocument();
  });

  it('exibe o formulário ao clicar em Notify Deployment', async () => {
    renderReleases();
    await userEvent.click(screen.getByRole('button', { name: /notify deployment/i }));
    expect(screen.getByText('Notify New Deployment')).toBeInTheDocument();
    expect(screen.getByPlaceholderText(/uuid of the api asset/i)).toBeInTheDocument();
  });

  it('oculta o formulário ao clicar em Cancel', async () => {
    renderReleases();
    await userEvent.click(screen.getByRole('button', { name: /notify deployment/i }));
    await userEvent.click(screen.getByRole('button', { name: /cancel/i }));
    expect(screen.queryByText('Notify New Deployment')).not.toBeInTheDocument();
  });

  it('carrega e exibe releases ao inserir um API Asset ID', async () => {
    vi.mocked(changeIntelligenceApi.listReleases).mockResolvedValue(mockReleases);
    renderReleases();
    const input = screen.getByPlaceholderText(/filter by api asset id/i);
    await userEvent.type(input, 'api-001');
    await waitFor(() => {
      expect(changeIntelligenceApi.listReleases).toHaveBeenCalledWith('api-001', 1, 20);
    });
    await waitFor(() => {
      expect(screen.getByText('2.1.0')).toBeInTheDocument();
      expect(screen.getByText('2.0.0')).toBeInTheDocument();
    });
  });

  it('exibe badges de change level corretos', async () => {
    vi.mocked(changeIntelligenceApi.listReleases).mockResolvedValue(mockReleases);
    renderReleases();
    const input = screen.getByPlaceholderText(/filter by api asset id/i);
    await userEvent.type(input, 'api-001');
    await waitFor(() => {
      expect(screen.getByText('Breaking')).toBeInTheDocument();
      expect(screen.getByText('Non-Breaking')).toBeInTheDocument();
    });
  });

  it('exibe badges de deployment state corretos', async () => {
    vi.mocked(changeIntelligenceApi.listReleases).mockResolvedValue(mockReleases);
    renderReleases();
    const input = screen.getByPlaceholderText(/filter by api asset id/i);
    await userEvent.type(input, 'api-001');
    await waitFor(() => {
      expect(screen.getByText('Succeeded')).toBeInTheDocument();
      expect(screen.getByText('Failed')).toBeInTheDocument();
    });
  });

  it('chama notifyDeployment ao submeter o formulário', async () => {
    vi.mocked(changeIntelligenceApi.notifyDeployment).mockResolvedValue({ id: 'rel-new' });
    renderReleases();
    await userEvent.click(screen.getByRole('button', { name: /notify deployment/i }));
    await userEvent.type(screen.getByPlaceholderText(/uuid of the api asset/i), 'api-123');
    await userEvent.type(screen.getByPlaceholderText(/e.g. 1.2.3/i), '1.0.0');
    await userEvent.click(screen.getByRole('button', { name: /submit/i }));
    await waitFor(() => {
      expect(changeIntelligenceApi.notifyDeployment).toHaveBeenCalled();
      const [firstArg] = vi.mocked(changeIntelligenceApi.notifyDeployment).mock.calls[0];
      expect(firstArg).toMatchObject({ apiAssetId: 'api-123', version: '1.0.0' });
    });
  });
});
