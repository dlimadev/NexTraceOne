import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ContractsPage } from '../../features/catalog/pages/ContractsPage';
import type { ContractVersion } from '../../types';

vi.mock('../../features/catalog/api', () => ({
  contractsApi: {
    getHistory: vi.fn(),
    importContract: vi.fn(),
    lockVersion: vi.fn(),
    createVersion: vi.fn(),
    listRuleViolations: vi.fn(),
    searchContracts: vi.fn(),
    computeDiff: vi.fn(),
    getDetail: vi.fn(),
    signVersion: vi.fn(),
    verifySignature: vi.fn(),
    transitionLifecycle: vi.fn(),
    deprecateVersion: vi.fn(),
    exportVersion: vi.fn(),
    getClassification: vi.fn(),
    suggestVersion: vi.fn(),
  },
}));

import { contractsApi } from '../../features/catalog/api';

const mockHistory: ContractVersion[] = [
  {
    id: 'cv-001',
    apiAssetId: 'api-001',
    version: '2.1.0',
    content: '{}',
    format: 'json',
    protocol: 'OpenApi',
    lifecycleState: 'Draft',
    isLocked: false,
    isAiGenerated: false,
    createdAt: '2024-01-15T10:00:00Z',
  },
  {
    id: 'cv-002',
    apiAssetId: 'api-001',
    version: '2.0.0',
    content: '{}',
    format: 'json',
    protocol: 'OpenApi',
    lifecycleState: 'Locked',
    isLocked: true,
    isAiGenerated: false,
    createdAt: '2024-01-10T08:00:00Z',
  },
];

function renderContracts() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ContractsPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('ContractsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('exibe o título da página', () => {
    renderContracts();
    expect(screen.getByRole('heading', { name: /contracts/i })).toBeInTheDocument();
  });

  it('exibe o botão de Import Contract', () => {
    renderContracts();
    expect(screen.getByRole('button', { name: /import contract/i })).toBeInTheDocument();
  });

  it('exibe a instrução para inserir API Asset ID', () => {
    renderContracts();
    expect(screen.getByText(/enter an api asset id to view contract history/i)).toBeInTheDocument();
  });

  it('exibe o formulário de importação ao clicar no botão', async () => {
    renderContracts();
    await userEvent.click(screen.getByRole('button', { name: /import contract/i }));
    expect(screen.getByText('Import Contract', { selector: 'h2' })).toBeInTheDocument();
  });

  it('carrega e exibe histórico de contratos ao inserir API Asset ID', async () => {
    vi.mocked(contractsApi.getHistory).mockResolvedValue(mockHistory);
    renderContracts();
    const input = screen.getByPlaceholderText(/enter uuid to view contract history/i);
    await userEvent.type(input, 'api-001');
    await waitFor(() => {
      expect(contractsApi.getHistory).toHaveBeenCalledWith('api-001');
    });
    await waitFor(() => {
      expect(screen.getByText('2.1.0')).toBeInTheDocument();
      expect(screen.getByText('2.0.0')).toBeInTheDocument();
    });
  });

  it('exibe badge Draft para versões em rascunho', async () => {
    vi.mocked(contractsApi.getHistory).mockResolvedValue(mockHistory);
    renderContracts();
    const input = screen.getByPlaceholderText(/enter uuid to view contract history/i);
    await userEvent.type(input, 'api-001');
    await waitFor(() => {
      expect(screen.getByText('Draft')).toBeInTheDocument();
    });
  });

  it('exibe badge Locked para versões bloqueadas', async () => {
    vi.mocked(contractsApi.getHistory).mockResolvedValue(mockHistory);
    renderContracts();
    const input = screen.getByPlaceholderText(/enter uuid to view contract history/i);
    await userEvent.type(input, 'api-001');
    await waitFor(() => {
      expect(screen.getByText('Locked')).toBeInTheDocument();
    });
  });

  it('exibe botão Lock para versões não bloqueadas', async () => {
    vi.mocked(contractsApi.getHistory).mockResolvedValue(mockHistory);
    renderContracts();
    const input = screen.getByPlaceholderText(/enter uuid to view contract history/i);
    await userEvent.type(input, 'api-001');
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /lock/i })).toBeInTheDocument();
    });
  });

  it('chama lockVersion ao clicar em Lock', async () => {
    vi.mocked(contractsApi.getHistory).mockResolvedValue(mockHistory);
    vi.mocked(contractsApi.lockVersion).mockResolvedValue({});
    renderContracts();
    const input = screen.getByPlaceholderText(/enter uuid to view contract history/i);
    await userEvent.type(input, 'api-001');
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /lock/i })).toBeInTheDocument();
    });
    await userEvent.click(screen.getByRole('button', { name: /lock/i }));
    expect(contractsApi.lockVersion).toHaveBeenCalledWith('cv-001', 'Locked via UI');
  });

  it('chama importContract ao submeter o formulário', async () => {
    vi.mocked(contractsApi.importContract).mockResolvedValue({ id: 'cv-new' });
    renderContracts();
    await userEvent.click(screen.getByRole('button', { name: /import contract/i }));
    await userEvent.type(screen.getByPlaceholderText('UUID'), 'api-abc');
    await userEvent.type(screen.getByPlaceholderText('1.0.0'), '3.0.0');
    await userEvent.type(screen.getByPlaceholderText(/paste your specification here/i), 'openapi-spec-content');
    await userEvent.click(screen.getByRole('button', { name: /^import$/i }));
    await waitFor(() => {
      expect(contractsApi.importContract).toHaveBeenCalled();
      const [firstArg] = vi.mocked(contractsApi.importContract).mock.calls[0];
      expect(firstArg).toMatchObject({ apiAssetId: 'api-abc', version: '3.0.0' });
    });
  });
});
