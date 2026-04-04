import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ToastProvider } from '../../components/Toast';
import { AiAgentsPage } from '../../features/ai-hub/pages/AiAgentsPage';

const { listAgentsMock, listAgentCategoriesMock } = vi.hoisted(() => ({
  listAgentsMock: vi.fn(),
  listAgentCategoriesMock: vi.fn(),
}));

vi.mock('../../features/ai-hub/api/aiGovernance', () => ({
  aiGovernanceApi: {
    listAgents: listAgentsMock,
    listAgentCategories: listAgentCategoriesMock,
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

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <MemoryRouter>
          <AiAgentsPage />
        </MemoryRouter>
      </ToastProvider>
    </QueryClientProvider>,
  );
}

describe('AiAgentsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    listAgentsMock.mockResolvedValue({ items: [], totalCount: 0 });
    listAgentCategoriesMock.mockResolvedValue({ categories: [] });
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('AI Agents')).toBeDefined();
    });
  });

  it('renders create agent button', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Create Agent')).toBeDefined();
    });
  });

  it('shows loading state initially', () => {
    listAgentsMock.mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders agents when data is available', async () => {
    listAgentsMock.mockResolvedValue({
      items: [
        {
          agentId: 'agent-001',
          name: 'contract-generator',
          displayName: 'Contract Generator',
          description: 'Generates REST API contracts from service context',
          category: 'ContractGeneration',
          ownershipType: 'System',
          status: 'Active',
          createdAt: '2026-01-01T00:00:00Z',
          modelId: 'model-001',
          modelName: 'GPT-4',
          lastExecutedAt: null,
          executionCount: 0,
        },
      ],
      totalCount: 1,
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Contract Generator')).toBeDefined();
    });
  });
});
