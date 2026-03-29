import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AiAuditPage } from '../../features/ai-hub/pages/AiAuditPage';

vi.mock('../../features/ai-hub/api/aiGovernance', () => ({
  aiGovernanceApi: {
    listAuditEntries: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));

import { aiGovernanceApi } from '../../features/ai-hub/api/aiGovernance';

const mockAuditEntries = {
  items: [
    {
      entryId: 'audit-001',
      userId: 'user-001',
      userDisplayName: 'Alice Engineer',
      modelId: 'model-001',
      modelName: 'GPT-4o',
      provider: 'OpenAI',
      isInternal: false,
      timestamp: new Date(Date.now() - 300000).toISOString(),
      promptTokens: 500,
      completionTokens: 1200,
      totalTokens: 1700,
      result: 'Allowed',
      clientType: 'Web',
      policyName: 'Engineering Default',
      contextScope: 'General',
      correlationId: 'corr-001',
      conversationId: 'conv-001',
    },
    {
      entryId: 'audit-002',
      userId: 'user-002',
      userDisplayName: 'Bob Intern',
      modelId: 'model-002',
      modelName: 'NexTra Local',
      provider: 'NexTraceOne',
      isInternal: true,
      timestamp: new Date(Date.now() - 600000).toISOString(),
      promptTokens: 200,
      completionTokens: 0,
      totalTokens: 200,
      result: 'Blocked',
      clientType: 'VsCode',
      policyName: null,
      contextScope: 'ContractGeneration',
      correlationId: 'corr-002',
      conversationId: null,
    },
  ],
};

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/ai/audit']}>
        <Routes>
          <Route path="/ai/audit" element={<AiAuditPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('AiAuditPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state while fetching audit entries', () => {
    vi.mocked(aiGovernanceApi.listAuditEntries).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Alice Engineer')).not.toBeInTheDocument();
  });

  it('renders audit table after loading', async () => {
    vi.mocked(aiGovernanceApi.listAuditEntries).mockResolvedValue(mockAuditEntries);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Alice Engineer')).toBeInTheDocument();
    });
    expect(screen.getByText('Bob Intern')).toBeInTheDocument();
    expect(screen.getByText('GPT-4o')).toBeInTheDocument();
    expect(screen.getByText('NexTra Local')).toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(aiGovernanceApi.listAuditEntries).mockRejectedValue(new Error('Server error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/error/i)).toBeInTheDocument();
    });
  });

  it('shows empty state when no audit entries found', async () => {
    vi.mocked(aiGovernanceApi.listAuditEntries).mockResolvedValue({ items: [] });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/no.*audit/i)).toBeInTheDocument();
    });
  });

  it('displays result badges for allowed and blocked entries', async () => {
    vi.mocked(aiGovernanceApi.listAuditEntries).mockResolvedValue(mockAuditEntries);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Alice Engineer')).toBeInTheDocument();
    });
    expect(screen.getAllByText('Allowed').length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByText('Blocked').length).toBeGreaterThanOrEqual(1);
  });

  it('displays token counts', async () => {
    vi.mocked(aiGovernanceApi.listAuditEntries).mockResolvedValue(mockAuditEntries);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Alice Engineer')).toBeInTheDocument();
    });
    expect(screen.getByText('1,700')).toBeInTheDocument();
    expect(screen.getByText('200')).toBeInTheDocument();
  });

  it('displays client type badges', async () => {
    vi.mocked(aiGovernanceApi.listAuditEntries).mockResolvedValue(mockAuditEntries);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Alice Engineer')).toBeInTheDocument();
    });
    expect(screen.getByText('Web')).toBeInTheDocument();
    expect(screen.getByText('VsCode')).toBeInTheDocument();
  });

  it('displays policy name or dash for null', async () => {
    vi.mocked(aiGovernanceApi.listAuditEntries).mockResolvedValue(mockAuditEntries);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Alice Engineer')).toBeInTheDocument();
    });
    expect(screen.getByText('Engineering Default')).toBeInTheDocument();
    expect(screen.getByText('—')).toBeInTheDocument();
  });
});
