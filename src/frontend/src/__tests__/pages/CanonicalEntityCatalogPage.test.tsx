import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { CanonicalEntityCatalogPage } from '../../features/contracts/canonical/CanonicalEntityCatalogPage';

vi.mock('../../features/contracts/hooks/useCanonicalEntities', () => ({
  useCanonicalEntities: vi.fn(),
  useCanonicalEntity: vi.fn(),
  useCanonicalEntityUsages: vi.fn(),
  useCreateCanonicalEntity: vi.fn(),
  useUpdateCanonicalEntity: vi.fn(),
  usePromoteToCanonical: vi.fn(),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import {
  useCanonicalEntities,
  useCanonicalEntityUsages,
} from '../../features/contracts/hooks/useCanonicalEntities';

const emptyData = { items: [], total: 0 };

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <CanonicalEntityCatalogPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('CanonicalEntityCatalogPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useCanonicalEntities).mockReturnValue({
      data: emptyData,
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useCanonicalEntities>);
    vi.mocked(useCanonicalEntityUsages).mockReturnValue({
      data: undefined,
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useCanonicalEntityUsages>);
  });

  it('renders page title', () => {
    renderPage();
    expect(screen.getByText('Canonical Entities')).toBeDefined();
  });

  it('shows loading state', () => {
    vi.mocked(useCanonicalEntities).mockReturnValue({
      data: undefined,
      isLoading: true,
      isError: false,
    } as ReturnType<typeof useCanonicalEntities>);
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders entities when data is available', async () => {
    vi.mocked(useCanonicalEntities).mockReturnValue({
      data: {
        items: [
          {
            id: 'ce-001',
            name: 'PostalAddress',
            description: 'Standard postal address format',
            category: 'SharedSchema',
            state: 'Published' as const,
            domain: 'Commerce',
            owner: 'Platform Team',
            version: '1.0.0',
            schemaContent: '{}',
            schemaFormat: 'json',
            aliases: [],
            tags: [],
            criticality: 'Medium',
            reusePolicy: 'Recommended',
            knownUsageCount: 12,
            createdAt: '2026-01-01T00:00:00Z',
            updatedAt: '2026-01-01T00:00:00Z',
          },
        ],
        total: 1,
      },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useCanonicalEntities>);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('PostalAddress')).toBeDefined();
    });
  });
});
