import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ContractMigrationPage } from '../../features/contracts/governance/ContractMigrationPage';

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
    listContracts: vi.fn(),
    generateMigrationPatch: vi.fn(),
  },
}));

import { contractsApi } from '../../features/contracts/api/contracts';

const mockContracts = {
  items: [
    {
      id: 'cv-001',
      apiAssetId: 'api-payments',
      version: '1.0.0',
      content: '{}',
      format: 'json',
      protocol: 'OpenApi',
      lifecycleState: 'Approved',
      isLocked: false,
      isAiGenerated: false,
      createdAt: '2024-01-01T00:00:00Z',
    },
    {
      id: 'cv-002',
      apiAssetId: 'api-payments',
      version: '2.0.0',
      content: '{}',
      format: 'json',
      protocol: 'OpenApi',
      lifecycleState: 'Approved',
      isLocked: false,
      isAiGenerated: false,
      createdAt: '2024-03-01T00:00:00Z',
    },
  ],
  totalCount: 2,
};

const mockPatchResult = {
  baseVersionId: 'cv-001',
  targetVersionId: 'cv-002',
  protocol: 'OpenApi',
  language: 'C#',
  changeLevel: 'Breaking',
  breakingChangeCount: 2,
  providerSuggestions: [
    {
      kind: 'breaking',
      side: 'provider',
      description: 'Endpoint /payments removed',
      codeHint: '// TODO: Remove or redirect /payments handler',
      severity: 'high',
    },
  ],
  consumerSuggestions: [
    {
      kind: 'breaking',
      side: 'consumer',
      description: 'Consumer must update client for /payments',
      codeHint: '// TODO: Update HttpClient call for breaking change',
      severity: 'high',
    },
  ],
  generatedAt: '2024-04-01T12:00:00Z',
};

function setup() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ContractMigrationPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ContractMigrationPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(contractsApi.listContracts).mockResolvedValue(mockContracts as unknown as ReturnType<typeof contractsApi.listContracts> extends Promise<infer T> ? T : never);
    vi.mocked(contractsApi.generateMigrationPatch).mockResolvedValue(mockPatchResult as unknown as ReturnType<typeof contractsApi.generateMigrationPatch> extends Promise<infer T> ? T : never);
  });

  it('renders the page title and description', async () => {
    setup();
    await waitFor(() => {
      expect(screen.getByText('Contract Migration Patch')).toBeInTheDocument();
    });
  });

  it('renders base and target version selects', async () => {
    setup();
    await waitFor(() => {
      expect(screen.getByLabelText('Select base version')).toBeInTheDocument();
      expect(screen.getByLabelText('Select target version')).toBeInTheDocument();
    });
  });

  it('renders contract version options after loading', async () => {
    setup();
    await waitFor(() => {
      expect(contractsApi.listContracts).toHaveBeenCalled();
    });
    const selects = screen.getAllByRole('combobox');
    expect(selects.length).toBeGreaterThanOrEqual(2);
  });

  it('generate button is disabled when no versions selected', async () => {
    setup();
    await waitFor(() => {
      expect(screen.getByText('Generate Migration Patch')).toBeInTheDocument();
    });
    const btn = screen.getByText('Generate Migration Patch').closest('button');
    expect(btn).toBeDisabled();
  });

  it('shows provider and consumer suggestions after generation', async () => {
    setup();
    // Wait for contract options to load
    await waitFor(() => {
      expect(screen.getAllByRole('option').length).toBeGreaterThan(2);
    });

    // Select base version
    const selects = screen.getAllByRole('combobox');
    fireEvent.change(selects[0], { target: { value: 'cv-001' } });
    fireEvent.change(selects[1], { target: { value: 'cv-002' } });

    const btn = screen.getByText('Generate Migration Patch').closest('button');
    await waitFor(() => expect(btn).not.toBeDisabled());
    fireEvent.click(btn!);

    await waitFor(() => {
      expect(contractsApi.generateMigrationPatch).toHaveBeenCalledWith({
        baseVersionId: 'cv-001',
        targetVersionId: 'cv-002',
        target: 'all',
        language: 'C#',
      });
    });

    await waitFor(() => {
      expect(screen.getByText('Provider Suggestions')).toBeInTheDocument();
      expect(screen.getByText('Consumer Suggestions')).toBeInTheDocument();
    });
  });

  it('shows summary stats after generation', async () => {
    setup();
    await waitFor(() => {
      expect(screen.getAllByRole('option').length).toBeGreaterThan(2);
    });

    const selects = screen.getAllByRole('combobox');
    fireEvent.change(selects[0], { target: { value: 'cv-001' } });
    fireEvent.change(selects[1], { target: { value: 'cv-002' } });
    const btn = screen.getByText('Generate Migration Patch').closest('button')!;
    await waitFor(() => expect(btn).not.toBeDisabled());
    fireEvent.click(btn);

    await waitFor(() => {
      expect(screen.getByText('OpenApi')).toBeInTheDocument();
      expect(screen.getByText('Breaking')).toBeInTheDocument();
    });
  });

  it('shows empty state when no suggestions returned', async () => {
    vi.mocked(contractsApi.generateMigrationPatch).mockResolvedValue({
      ...mockPatchResult,
      providerSuggestions: [],
      consumerSuggestions: [],
      breakingChangeCount: 0,
      changeLevel: 'NonBreaking',
    } as unknown as ReturnType<typeof contractsApi.generateMigrationPatch> extends Promise<infer T> ? T : never);

    setup();
    await waitFor(() => {
      expect(screen.getAllByRole('option').length).toBeGreaterThan(2);
    });

    const selects = screen.getAllByRole('combobox');
    fireEvent.change(selects[0], { target: { value: 'cv-001' } });
    fireEvent.change(selects[1], { target: { value: 'cv-002' } });
    const btn = screen.getByText('Generate Migration Patch').closest('button')!;
    await waitFor(() => expect(btn).not.toBeDisabled());
    fireEvent.click(btn);

    await waitFor(() => {
      expect(screen.getByText(/No migration suggestions/)).toBeInTheDocument();
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(contractsApi.generateMigrationPatch).mockRejectedValue(new Error('Server error'));

    setup();
    await waitFor(() => {
      expect(screen.getAllByRole('option').length).toBeGreaterThan(2);
    });

    const selects = screen.getAllByRole('combobox');
    fireEvent.change(selects[0], { target: { value: 'cv-001' } });
    fireEvent.change(selects[1], { target: { value: 'cv-002' } });
    const btn = screen.getByText('Generate Migration Patch').closest('button')!;
    await waitFor(() => expect(btn).not.toBeDisabled());
    fireEvent.click(btn);

    await waitFor(() => {
      expect(screen.getByText('Server error')).toBeInTheDocument();
    });
  });
});
