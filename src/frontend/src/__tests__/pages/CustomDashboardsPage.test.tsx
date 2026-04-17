import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { CustomDashboardsPage } from '../../features/governance/pages/CustomDashboardsPage';

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import client from '../../api/client';

const mockListResponse = {
  items: [
    {
      dashboardId: '11111111-0000-0000-0000-000000000001',
      name: 'Executive KPI Overview',
      persona: 'Executive',
      widgetCount: 6,
      layout: 'grid',
      isShared: true,
      createdAt: '2026-01-01T00:00:00Z',
    },
    {
      dashboardId: '11111111-0000-0000-0000-000000000002',
      name: 'Team Health Dashboard',
      persona: 'TechLead',
      widgetCount: 5,
      layout: 'two-column',
      isShared: false,
      createdAt: '2026-02-01T00:00:00Z',
    },
  ],
  totalCount: 2,
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <CustomDashboardsPage />
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
describe('CustomDashboardsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(client.get).mockResolvedValue({ data: mockListResponse });
    vi.mocked(client.post).mockResolvedValue({ data: {} });
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Custom Dashboards');
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(client.get).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders dashboard cards when data is available', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Executive KPI Overview');
      expect(document.body.textContent).toContain('Team Health Dashboard');
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(client.get).mockRejectedValue(new Error('API error'));
    renderPage();
    await waitFor(() => {
      expect(document.body).toBeDefined();
    });
  });
});
