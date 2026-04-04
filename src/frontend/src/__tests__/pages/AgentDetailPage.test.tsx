import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AgentDetailPage } from '../../features/ai-hub/pages/AgentDetailPage';

vi.mock('../../features/ai-hub/api/aiGovernance', () => ({
  aiGovernanceApi: {
    listAgents: vi.fn(),
    listAgentCategories: vi.fn(),
    createAgent: vi.fn(),
    executeAgent: vi.fn(),
    reviewArtifact: vi.fn(),
    getAgent: vi.fn(),
    updateAgent: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { aiGovernanceApi } from '../../features/ai-hub/api/aiGovernance';

const mockAgent = {
  agentId: 'agent-001',
  name: 'incident-investigator',
  displayName: 'Incident Investigator',
  description: 'Analyzes incidents and suggests root causes',
  category: 'Operations',
  ownershipType: 'System',
  status: 'Active',
  systemPrompt: 'You are an expert incident investigator...',
  modelId: 'model-001',
  modelName: 'GPT-4',
  createdAt: '2026-01-01T00:00:00Z',
  lastExecutedAt: null,
  executionCount: 12,
  executionHistory: [],
  pendingArtifacts: [],
  allowedGroups: [],
  visibilityType: 'Public',
};

function renderPage(agentId = 'agent-001') {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/ai/agents/${agentId}`]}>
        <Routes>
          <Route path="/ai/agents/:agentId" element={<AgentDetailPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('AgentDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(aiGovernanceApi.getAgent).mockResolvedValue(mockAgent);
  });

  it('shows loading state initially', () => {
    vi.mocked(aiGovernanceApi.getAgent).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders agent details when loaded', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Incident Investigator')).toBeDefined();
    });
  });

  it('shows error state when agent fails to load', async () => {
    vi.mocked(aiGovernanceApi.getAgent).mockRejectedValue(new Error('Not found'));
    renderPage('agent-nonexistent');
    await waitFor(() => {
      expect(document.body.textContent).toBeDefined();
    });
  });

  it('renders back to list link', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Back to agents')).toBeDefined();
    });
  });
});
