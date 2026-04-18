import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { IdeIntegrationsPage } from '../../features/ai-hub/pages/IdeIntegrationsPage';

vi.mock('../../features/ai-hub/api/aiGovernance', () => ({
  aiGovernanceApi: {
    getIdeSummary: vi.fn(),
    listIdeClients: vi.fn(),
    listIdeCapabilityPolicies: vi.fn(),
    getIdeCapabilities: vi.fn(),
    listIdeQuerySessions: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));

import { aiGovernanceApi } from '../../features/ai-hub/api/aiGovernance';

const mockSummary = {
  totalActiveClients: 5,
  activePolicies: 2,
  clientTypes: [
    { clientType: 'VsCode', activeClients: 3, hasCapabilityPolicy: true },
    { clientType: 'VisualStudio', activeClients: 2, hasCapabilityPolicy: true },
  ],
};

const mockClients = {
  items: [
    {
      registrationId: 'client-001',
      userId: 'user-001',
      userDisplayName: 'Alice Engineer',
      clientType: 'VsCode',
      clientVersion: '1.90.0',
      deviceIdentifier: 'ALICE-LAPTOP',
      lastAccessAt: new Date(Date.now() - 600000).toISOString(),
      isActive: true,
    },
    {
      registrationId: 'client-002',
      userId: 'user-002',
      userDisplayName: 'Bob Architect',
      clientType: 'VisualStudio',
      clientVersion: '17.10',
      deviceIdentifier: 'BOB-WORKSTATION',
      lastAccessAt: new Date(Date.now() - 3600000).toISOString(),
      isActive: true,
    },
  ],
};

const mockPolicies = {
  items: [
    {
      policyId: 'ide-pol-001',
      clientType: 'VsCode',
      persona: null,
      allowedCommands: 'Chat,ServiceLookup,ContractLookup,ContractGenerate,IncidentLookup',
      allowedContextScopes: 'General,Contract,Service',
      allowContractGeneration: true,
      allowIncidentTroubleshooting: true,
      allowExternalAI: false,
      maxTokensPerRequest: 4000,
      isActive: true,
    },
  ],
};

const mockCapabilities = {
  allowedCommands: ['Chat', 'ServiceLookup', 'ContractLookup', 'ContractGenerate', 'IncidentLookup'],
};

const mockQuerySessions = {
  items: [
    {
      sessionId: 'session-001',
      userId: 'user-001',
      ideClient: 'vscode',
      ideClientVersion: '1.90.0',
      queryType: 'ContractSuggestion',
      queryText: 'Suggest a REST contract for OrderService',
      status: 'Responded',
      modelUsed: 'llama3.2:3b',
      tokensUsed: 350,
      responseTimeMs: 1200,
      submittedAt: new Date(Date.now() - 300000).toISOString(),
    },
    {
      sessionId: 'session-002',
      userId: 'user-002',
      ideClient: 'visualstudio',
      ideClientVersion: '17.10',
      queryType: 'GeneralQuery',
      queryText: 'What services depend on PaymentService?',
      status: 'Processing',
      modelUsed: 'llama3.2:3b',
      tokensUsed: 0,
      responseTimeMs: null,
      submittedAt: new Date(Date.now() - 60000).toISOString(),
    },
  ],
};

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/ai/ide']}>
        <Routes>
          <Route path="/ai/ide" element={<IdeIntegrationsPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('IdeIntegrationsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state while fetching data', () => {
    vi.mocked(aiGovernanceApi.getIdeSummary).mockReturnValue(new Promise(() => {}));
    vi.mocked(aiGovernanceApi.listIdeClients).mockReturnValue(new Promise(() => {}));
    vi.mocked(aiGovernanceApi.listIdeCapabilityPolicies).mockReturnValue(new Promise(() => {}));
    vi.mocked(aiGovernanceApi.getIdeCapabilities).mockReturnValue(new Promise(() => {}));
    vi.mocked(aiGovernanceApi.listIdeQuerySessions).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Alice Engineer')).not.toBeInTheDocument();
  });

  it('renders IDE clients after loading', async () => {
    vi.mocked(aiGovernanceApi.getIdeSummary).mockResolvedValue(mockSummary);
    vi.mocked(aiGovernanceApi.listIdeClients).mockResolvedValue(mockClients);
    vi.mocked(aiGovernanceApi.listIdeCapabilityPolicies).mockResolvedValue(mockPolicies);
    vi.mocked(aiGovernanceApi.getIdeCapabilities).mockResolvedValue(mockCapabilities);
    vi.mocked(aiGovernanceApi.listIdeQuerySessions).mockResolvedValue({ items: [] });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Alice Engineer')).toBeInTheDocument();
    });
    expect(screen.getByText('Bob Architect')).toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(aiGovernanceApi.getIdeSummary).mockRejectedValue(new Error('Server error'));
    vi.mocked(aiGovernanceApi.listIdeClients).mockRejectedValue(new Error('Server error'));
    vi.mocked(aiGovernanceApi.listIdeCapabilityPolicies).mockRejectedValue(new Error('Server error'));
    vi.mocked(aiGovernanceApi.getIdeCapabilities).mockResolvedValue(mockCapabilities);
    vi.mocked(aiGovernanceApi.listIdeQuerySessions).mockResolvedValue({ items: [] });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/error/i)).toBeInTheDocument();
    });
  });

  it('shows empty state when no clients found', async () => {
    vi.mocked(aiGovernanceApi.getIdeSummary).mockResolvedValue({ ...mockSummary, totalActiveClients: 0 });
    vi.mocked(aiGovernanceApi.listIdeClients).mockResolvedValue({ items: [] });
    vi.mocked(aiGovernanceApi.listIdeCapabilityPolicies).mockResolvedValue({ items: [] });
    vi.mocked(aiGovernanceApi.getIdeCapabilities).mockResolvedValue(mockCapabilities);
    vi.mocked(aiGovernanceApi.listIdeQuerySessions).mockResolvedValue({ items: [] });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/no.*client/i)).toBeInTheDocument();
    });
  });

  it('displays stat cards with summary data', async () => {
    vi.mocked(aiGovernanceApi.getIdeSummary).mockResolvedValue(mockSummary);
    vi.mocked(aiGovernanceApi.listIdeClients).mockResolvedValue(mockClients);
    vi.mocked(aiGovernanceApi.listIdeCapabilityPolicies).mockResolvedValue(mockPolicies);
    vi.mocked(aiGovernanceApi.getIdeCapabilities).mockResolvedValue(mockCapabilities);
    vi.mocked(aiGovernanceApi.listIdeQuerySessions).mockResolvedValue({ items: [] });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Alice Engineer')).toBeInTheDocument();
    });
    // totalActiveClients = 5
    expect(screen.getByText('5')).toBeInTheDocument();
  });

  it('displays capability policy details', async () => {
    vi.mocked(aiGovernanceApi.getIdeSummary).mockResolvedValue(mockSummary);
    vi.mocked(aiGovernanceApi.listIdeClients).mockResolvedValue(mockClients);
    vi.mocked(aiGovernanceApi.listIdeCapabilityPolicies).mockResolvedValue(mockPolicies);
    vi.mocked(aiGovernanceApi.getIdeCapabilities).mockResolvedValue(mockCapabilities);
    vi.mocked(aiGovernanceApi.listIdeQuerySessions).mockResolvedValue({ items: [] });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Alice Engineer')).toBeInTheDocument();
    });
    // VsCode should be displayed in policy section
    const vscodeElements = screen.getAllByText('VsCode');
    expect(vscodeElements.length).toBeGreaterThanOrEqual(1);
  });

  it('displays query sessions audit table', async () => {
    vi.mocked(aiGovernanceApi.getIdeSummary).mockResolvedValue(mockSummary);
    vi.mocked(aiGovernanceApi.listIdeClients).mockResolvedValue(mockClients);
    vi.mocked(aiGovernanceApi.listIdeCapabilityPolicies).mockResolvedValue(mockPolicies);
    vi.mocked(aiGovernanceApi.getIdeCapabilities).mockResolvedValue(mockCapabilities);
    vi.mocked(aiGovernanceApi.listIdeQuerySessions).mockResolvedValue(mockQuerySessions);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Alice Engineer')).toBeInTheDocument();
    });
    expect(screen.getByText('ContractSuggestion')).toBeInTheDocument();
    expect(screen.getByText('GeneralQuery')).toBeInTheDocument();
    const modelCells = screen.getAllByText('llama3.2:3b');
    expect(modelCells.length).toBeGreaterThanOrEqual(1);
  });

  it('shows empty state for query sessions when none available', async () => {
    vi.mocked(aiGovernanceApi.getIdeSummary).mockResolvedValue(mockSummary);
    vi.mocked(aiGovernanceApi.listIdeClients).mockResolvedValue(mockClients);
    vi.mocked(aiGovernanceApi.listIdeCapabilityPolicies).mockResolvedValue(mockPolicies);
    vi.mocked(aiGovernanceApi.getIdeCapabilities).mockResolvedValue(mockCapabilities);
    vi.mocked(aiGovernanceApi.listIdeQuerySessions).mockResolvedValue({ items: [] });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Alice Engineer')).toBeInTheDocument();
    });
    expect(screen.getByText('No query sessions found.')).toBeInTheDocument();
  });
});
