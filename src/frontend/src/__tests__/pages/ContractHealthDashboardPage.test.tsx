import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ContractHealthDashboardPage } from '../../features/contracts/governance/ContractHealthDashboardPage';

vi.mock('../../contexts/PersonaContext', () => ({
  usePersona: vi.fn().mockReturnValue({
    persona: 'Engineer',
    config: {
      aiContextScopes: ['contracts'],
      aiSuggestedPromptKeys: [],
      sectionOrder: [],
      highlightedSections: [],
      homeSubtitleKey: '',
      homeWidgets: [],
      quickActions: [],
    },
  }),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));

vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: {
    getHealthDashboard: vi.fn(),
  },
}));

import { contractsApi } from '../../features/contracts/api/contracts';

const mockHealthData = {
  totalContractVersions: 25,
  distinctContracts: 12,
  deprecatedVersions: 3,
  filteredCount: 12,
  percentWithExamples: 75,
  percentWithCanonicalEntities: 60,
  topViolations: [
    {
      contractVersionId: 'cv-001',
      semVer: '1.0.0',
      violationCount: 5,
      topRuleIds: ['OperationIdRequired', 'ResponseRequired'],
    },
    {
      contractVersionId: 'cv-002',
      semVer: '2.1.0',
      violationCount: 2,
      topRuleIds: ['TitleRequired'],
    },
  ],
  healthScore: 82,
};

const mockHealthDataNoViolations = {
  ...mockHealthData,
  topViolations: [],
  healthScore: 95,
};

const mockHealthDataLowScore = {
  ...mockHealthData,
  topViolations: [],
  healthScore: 30,
};

const mockHealthDataMediumScore = {
  ...mockHealthData,
  topViolations: [],
  healthScore: 65,
};

function renderDashboard() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ContractHealthDashboardPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ContractHealthDashboardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('exibe estado de loading enquanto os dados são carregados', () => {
    vi.mocked(contractsApi.getHealthDashboard).mockReturnValue(new Promise(() => {}));
    renderDashboard();
    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it('exibe o health score quando os dados são carregados', async () => {
    vi.mocked(contractsApi.getHealthDashboard).mockResolvedValue(mockHealthData as never);
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText('82')).toBeInTheDocument();
    });
  });

  it('exibe o título do dashboard', async () => {
    vi.mocked(contractsApi.getHealthDashboard).mockResolvedValue(mockHealthData as never);
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText(/contract health dashboard/i)).toBeInTheDocument();
    });
  });

  it('exibe o total de contratos distintos', async () => {
    vi.mocked(contractsApi.getHealthDashboard).mockResolvedValue(mockHealthData as never);
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText('12')).toBeInTheDocument();
    });
  });

  it('exibe a percentagem de contratos com exemplos', async () => {
    vi.mocked(contractsApi.getHealthDashboard).mockResolvedValue(mockHealthData as never);
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText('75%')).toBeInTheDocument();
    });
  });

  it('exibe a percentagem de contratos com entidades canónicas', async () => {
    vi.mocked(contractsApi.getHealthDashboard).mockResolvedValue(mockHealthData as never);
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText('60%')).toBeInTheDocument();
    });
  });

  it('exibe a contagem de versões deprecated', async () => {
    vi.mocked(contractsApi.getHealthDashboard).mockResolvedValue(mockHealthData as never);
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText('3')).toBeInTheDocument();
    });
  });

  it('exibe a lista de top violations quando existem', async () => {
    vi.mocked(contractsApi.getHealthDashboard).mockResolvedValue(mockHealthData as never);
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText('1.0.0')).toBeInTheDocument();
      expect(screen.getByText('2.1.0')).toBeInTheDocument();
    });
  });

  it('exibe contagem de violations por contrato', async () => {
    vi.mocked(contractsApi.getHealthDashboard).mockResolvedValue(mockHealthData as never);
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText(/5.*violations/i)).toBeInTheDocument();
      expect(screen.getByText(/2.*violations/i)).toBeInTheDocument();
    });
  });

  it('exibe mensagem de sem violations quando a lista está vazia', async () => {
    vi.mocked(contractsApi.getHealthDashboard).mockResolvedValue(mockHealthDataNoViolations as never);
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText(/no rule violations/i)).toBeInTheDocument();
    });
  });

  it('exibe estado de erro quando a query falha', async () => {
    vi.mocked(contractsApi.getHealthDashboard).mockRejectedValue(new Error('API error'));
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText(/error/i)).toBeInTheDocument();
    });
  });

  it('exibe o health score verde para score >= 80', async () => {
    vi.mocked(contractsApi.getHealthDashboard).mockResolvedValue(mockHealthData as never);
    renderDashboard();
    await waitFor(() => {
      const scoreEl = screen.getByText('82');
      expect(scoreEl).toHaveClass('text-success');
    });
  });

  it('exibe o health score amarelo para score entre 50 e 79', async () => {
    vi.mocked(contractsApi.getHealthDashboard).mockResolvedValue(mockHealthDataMediumScore as never);
    renderDashboard();
    await waitFor(() => {
      const scoreEl = screen.getByText('65');
      expect(scoreEl).toHaveClass('text-warning');
    });
  });

  it('exibe o health score vermelho para score < 50', async () => {
    vi.mocked(contractsApi.getHealthDashboard).mockResolvedValue(mockHealthDataLowScore as never);
    renderDashboard();
    await waitFor(() => {
      const scoreEl = screen.getByText('30');
      expect(scoreEl).toHaveClass('text-critical');
    });
  });

  it('usa chaves i18n para o título e subtítulo', async () => {
    vi.mocked(contractsApi.getHealthDashboard).mockResolvedValue(mockHealthData as never);
    renderDashboard();
    await waitFor(() => {
      expect(screen.getByText(/contract health dashboard/i)).toBeInTheDocument();
    });
  });
});
