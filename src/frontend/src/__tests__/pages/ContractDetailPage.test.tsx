import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ContractDetailPage } from '../../features/catalog/pages/ContractDetailPage';
import type { ContractVersionDetail, ContractVersion } from '../../types';

vi.mock('../../features/catalog/api/contracts', () => ({
  contractsApi: {
    getDetail: vi.fn(),
    getHistory: vi.fn(),
    listRuleViolations: vi.fn(),
  },
}));

import { contractsApi } from '../../features/catalog/api/contracts';

const mockDetail: ContractVersionDetail = {
  id: 'cv-001',
  apiAssetId: 'api-payments',
  semVer: '2.0.0',
  specContent: '{"openapi":"3.0.0","info":{"title":"Payments API"}}',
  format: 'json',
  protocol: 'OpenApi',
  lifecycleState: 'Approved',
  isLocked: false,
  signedBy: null as unknown as string,
  fingerprint: null as unknown as string,
  algorithm: null as unknown as string,
  importedFrom: 'upload',
  deprecationNotice: null as unknown as string,
  sunsetDate: null as unknown as string,
  createdAt: '2024-01-15T10:00:00Z',
  provenance: {
    importedBy: 'admin',
    parserUsed: 'OpenApiParser',
    isAiGenerated: false,
  },
};

const mockHistory: ContractVersion[] = [
  {
    id: 'cv-001',
    apiAssetId: 'api-payments',
    version: '2.0.0',
    content: '{}',
    format: 'json',
    protocol: 'OpenApi',
    lifecycleState: 'Approved',
    isLocked: false,
    isAiGenerated: false,
    createdAt: '2024-01-15T10:00:00Z',
  },
  {
    id: 'cv-002',
    apiAssetId: 'api-payments',
    version: '1.0.0',
    content: '{}',
    format: 'json',
    protocol: 'OpenApi',
    lifecycleState: 'Locked',
    isLocked: true,
    isAiGenerated: false,
    createdAt: '2024-01-10T08:00:00Z',
  },
];

const mockViolations = [
  {
    ruleName: 'OperationIdRequired',
    message: 'Every operation must have an operationId',
    severity: 'Warning',
    path: '/paths/~1payments/get',
    contractVersionId: 'cv-001',
  },
];

function renderContractDetail(contractVersionId = 'cv-001') {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/contracts/${contractVersionId}`]}>
        <Routes>
          <Route path="/contracts/:contractVersionId" element={<ContractDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ContractDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('exibe estado de loading', () => {
    vi.mocked(contractsApi.getDetail).mockReturnValue(new Promise(() => {}));
    renderContractDetail();
    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it('exibe dados do contrato', async () => {
    vi.mocked(contractsApi.getDetail).mockResolvedValue(mockDetail);
    vi.mocked(contractsApi.getHistory).mockResolvedValue(mockHistory);
    vi.mocked(contractsApi.listRuleViolations).mockResolvedValue([]);
    renderContractDetail();
    await waitFor(() => {
      expect(screen.getAllByText('api-payments').length).toBeGreaterThanOrEqual(1);
      expect(screen.getByText('v2.0.0')).toBeInTheDocument();
    });
  });

  it('exibe governança do contrato', async () => {
    vi.mocked(contractsApi.getDetail).mockResolvedValue(mockDetail);
    vi.mocked(contractsApi.getHistory).mockResolvedValue(mockHistory);
    vi.mocked(contractsApi.listRuleViolations).mockResolvedValue([]);
    renderContractDetail();
    await waitFor(() => {
      expect(screen.getAllByText('Approved').length).toBeGreaterThanOrEqual(1);
      expect(screen.getByText('Unlocked')).toBeInTheDocument();
    });
  });

  it('exibe histórico de versões', async () => {
    vi.mocked(contractsApi.getDetail).mockResolvedValue(mockDetail);
    vi.mocked(contractsApi.getHistory).mockResolvedValue(mockHistory);
    vi.mocked(contractsApi.listRuleViolations).mockResolvedValue([]);
    renderContractDetail();
    await waitFor(() => {
      expect(screen.getByText('v1.0.0')).toBeInTheDocument();
    });
  });

  it('exibe link para comparar versões', async () => {
    vi.mocked(contractsApi.getDetail).mockResolvedValue(mockDetail);
    vi.mocked(contractsApi.getHistory).mockResolvedValue(mockHistory);
    vi.mocked(contractsApi.listRuleViolations).mockResolvedValue([]);
    renderContractDetail();
    await waitFor(() => {
      expect(screen.getByText(/compare versions/i)).toBeInTheDocument();
    });
  });

  it('exibe proveniência do contrato', async () => {
    vi.mocked(contractsApi.getDetail).mockResolvedValue(mockDetail);
    vi.mocked(contractsApi.getHistory).mockResolvedValue(mockHistory);
    vi.mocked(contractsApi.listRuleViolations).mockResolvedValue([]);
    renderContractDetail();
    await waitFor(() => {
      expect(screen.getByText('admin')).toBeInTheDocument();
      expect(screen.getByText('OpenApiParser')).toBeInTheDocument();
    });
  });

  it('exibe violações de regras quando existem', async () => {
    vi.mocked(contractsApi.getDetail).mockResolvedValue(mockDetail);
    vi.mocked(contractsApi.getHistory).mockResolvedValue(mockHistory);
    vi.mocked(contractsApi.listRuleViolations).mockResolvedValue(mockViolations);
    renderContractDetail();
    await waitFor(() => {
      expect(screen.getByText('OperationIdRequired')).toBeInTheDocument();
    });
  });

  it('exibe mensagem de sem violações', async () => {
    vi.mocked(contractsApi.getDetail).mockResolvedValue(mockDetail);
    vi.mocked(contractsApi.getHistory).mockResolvedValue(mockHistory);
    vi.mocked(contractsApi.listRuleViolations).mockResolvedValue([]);
    renderContractDetail();
    await waitFor(() => {
      expect(screen.getByText(/no violations detected/i)).toBeInTheDocument();
    });
  });

  it('exibe estado de erro quando contrato não é encontrado', async () => {
    vi.mocked(contractsApi.getDetail).mockRejectedValue(new Error('Not found'));
    renderContractDetail();
    await waitFor(() => {
      expect(screen.getByText(/error/i)).toBeInTheDocument();
    });
  });
});
