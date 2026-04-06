import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AgentMarketplacePage } from '../../features/ai-hub/pages/AgentMarketplacePage';

const { getMock } = vi.hoisted(() => ({
  getMock: vi.fn(),
}));

vi.mock('../../api/client', () => ({
  default: { get: getMock },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => {
      if (opts?.count !== undefined) return `${opts.count} executions`;
      return key;
    },
  }),
}));

const mockMarketplaceData = {
  data: {
    items: [
      {
        agentId: 'agent-001',
        name: 'contract-generator',
        displayName: 'Contract Generator',
        slug: 'contract-generator',
        description: 'Generates REST API contracts from service context.',
        category: 'ContractGovernance',
        isOfficial: true,
        isActive: true,
        capabilities: 'generation,validation',
        targetPersona: 'Engineer',
        icon: '📄',
        version: 1,
        executionCount: 42,
        publicationStatus: 'Published',
        ownershipType: 'System',
        tags: ['generation', 'validation'],
      },
    ],
    totalCount: 1,
    categories: ['ContractGovernance'],
  },
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AgentMarketplacePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('AgentMarketplacePage', () => {
  it('renders title', () => {
    getMock.mockResolvedValue(mockMarketplaceData);
    renderPage();
    expect(screen.getByText('agentMarketplace.title')).toBeDefined();
  });

  it('shows loading state initially', () => {
    getMock.mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.getByText('agentMarketplace.loading')).toBeDefined();
  });

  it('shows error state when request fails', async () => {
    getMock.mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('agentMarketplace.error')).toBeDefined();
    });
  });

  it('renders agent cards when data is available', async () => {
    getMock.mockResolvedValue(mockMarketplaceData);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Contract Generator')).toBeDefined();
    });
  });
});
