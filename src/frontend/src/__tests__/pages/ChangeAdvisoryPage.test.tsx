import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ChangeAdvisoryPage } from '../../features/change-governance/pages/ChangeAdvisoryPage';

vi.mock('../../features/change-governance/api/changeConfidence', () => ({
  changeConfidenceApi: {
    listChanges: vi.fn(),
    getFeatureFlagAwareness: vi.fn(),
    getHistoricalPattern: vi.fn(),
    getDoraMetrics: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { changeConfidenceApi } from '../../features/change-governance/api/changeConfidence';

const mockChanges = {
  items: [
    {
      id: 'change-001',
      serviceName: 'PaymentService',
      teamName: 'Payments Team',
      version: '2.5.0',
      environment: 'Production',
      changeType: 'Breaking',
      changeScore: 0.82,
      confidenceStatus: 'NeedsAttention',
      deploymentStatus: 'Succeeded',
      createdAt: '2026-04-10T14:00:00Z',
    },
    {
      id: 'change-002',
      serviceName: 'NotificationService',
      teamName: 'Platform Team',
      version: '1.3.0',
      environment: 'Staging',
      changeType: 'NonBreaking',
      changeScore: 0.25,
      confidenceStatus: 'Validated',
      deploymentStatus: 'Succeeded',
      createdAt: '2026-04-11T09:00:00Z',
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 30,
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ChangeAdvisoryPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn().mockReturnValue({
    activeEnvironmentId: 'env-prod-001',
    activeEnvironment: { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [{ id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true }],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  }),
}));

describe('ChangeAdvisoryPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(changeConfidenceApi.listChanges).mockResolvedValue(mockChanges as any);
  });

  it('renders the page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Change Advisory Board')).toBeInTheDocument();
    });
  });

  it('renders the filters section', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Filters')).toBeInTheDocument();
    });
  });

  it('renders changes after loading', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('PaymentService')).toBeInTheDocument();
    });
    expect(screen.getByText('NotificationService')).toBeInTheDocument();
  });

  it('shows the total count metric card', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Total Changes')).toBeInTheDocument();
    });
  });

  it('shows view details links for each change', async () => {
    renderPage();
    await waitFor(() => {
      const viewLinks = screen.getAllByText('View Details');
      expect(viewLinks).toHaveLength(2);
    });
  });

  it('shows the risk score badge for high risk change', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('82%')).toBeInTheDocument();
    });
  });

  it('shows version columns', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('2.5.0')).toBeInTheDocument();
    });
    expect(screen.getByText('1.3.0')).toBeInTheDocument();
  });

  it('shows empty state when no changes match filters', async () => {
    vi.mocked(changeConfidenceApi.listChanges).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 30,
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('No changes matching the current filters')).toBeInTheDocument();
    });
  });

  it('shows error state on API failure', async () => {
    vi.mocked(changeConfidenceApi.listChanges).mockRejectedValue(new Error('Network Error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Failed to load changes')).toBeInTheDocument();
    });
  });

  it('shows the guidance section', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('About Change Advisory')).toBeInTheDocument();
    });
  });
});
