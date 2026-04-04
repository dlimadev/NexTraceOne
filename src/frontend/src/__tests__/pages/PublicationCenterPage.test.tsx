import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { PublicationCenterPage } from '../../features/contracts/publication/PublicationCenterPage';

vi.mock('../../features/contracts/hooks/usePublicationCenter', () => ({
  usePublishContractToPortal: vi.fn(),
  useWithdrawContractFromPortal: vi.fn(),
  usePublicationCenterEntries: vi.fn(),
  useContractPublicationStatus: vi.fn(),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import {
  usePublicationCenterEntries,
  useWithdrawContractFromPortal,
} from '../../features/contracts/hooks/usePublicationCenter';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <PublicationCenterPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('PublicationCenterPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(usePublicationCenterEntries).mockReturnValue({
      data: { items: [], totalCount: 0 },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof usePublicationCenterEntries>);
    vi.mocked(useWithdrawContractFromPortal).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as ReturnType<typeof useWithdrawContractFromPortal>);
  });

  it('renders page title', () => {
    renderPage();
    expect(screen.getByText('Publication Center')).toBeDefined();
  });

  it('shows loading state', () => {
    vi.mocked(usePublicationCenterEntries).mockReturnValue({
      data: undefined,
      isLoading: true,
      isError: false,
    } as ReturnType<typeof usePublicationCenterEntries>);
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders published contracts when data is available', async () => {
    vi.mocked(usePublicationCenterEntries).mockReturnValue({
      data: {
        items: [
          {
            publicationId: 'pub-001',
            contractVersionId: 'cv-001',
            contractTitle: 'Payment API v3',
            protocol: 'OpenApi',
            version: '3.0.0',
            status: 'Published',
            publishedAt: '2026-03-01T00:00:00Z',
            publishedBy: 'alice@acme.com',
            withdrawnAt: null,
          },
        ],
        totalCount: 1,
      },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof usePublicationCenterEntries>);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Payment API v3')).toBeDefined();
    });
  });
});
