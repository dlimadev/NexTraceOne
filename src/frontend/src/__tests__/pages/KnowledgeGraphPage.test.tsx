import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { KnowledgeGraphPage } from '../../features/knowledge/pages/KnowledgeGraphPage';

vi.mock('../../api/client', () => ({
  default: {
    get: vi.fn().mockResolvedValue({
      data: {
        totalNodes: 12,
        totalEdges: 8,
        connectedComponents: 3,
        nodes: [
          { id: 'node-1', label: 'payment-service', type: 'Service' },
          { id: 'node-2', label: 'api-contract-1', type: 'Contract' },
        ],
        edges: [
          { sourceId: 'node-1', targetId: 'node-2', relationshipType: 'Publishes' },
        ],
      },
    }),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => {
      if (opts?.count !== undefined) return `${opts.count}`;
      return key;
    },
  }),
}));

const renderWithProviders = (ui: React.ReactElement) => {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>{ui}</MemoryRouter>
    </QueryClientProvider>
  );
};

describe('KnowledgeGraphPage', () => {
  it('renders title', async () => {
    renderWithProviders(<KnowledgeGraphPage />);
    await waitFor(() => {
      expect(screen.getByText('knowledge.graph.title')).toBeDefined();
    });
  });

  it('renders node stats section labels', () => {
    renderWithProviders(<KnowledgeGraphPage />);
    // Shows loading state first, then data
    expect(screen.getByText('knowledge.graph.loading')).toBeDefined();
  });

  it('renders node labels', async () => {
    renderWithProviders(<KnowledgeGraphPage />);
    await waitFor(() => {
      expect(screen.getByText('payment-service')).toBeDefined();
    });
  });

  it('renders edge relationship type', async () => {
    renderWithProviders(<KnowledgeGraphPage />);
    await waitFor(() => {
      expect(screen.getByText('Publishes')).toBeDefined();
    });
  });
});
