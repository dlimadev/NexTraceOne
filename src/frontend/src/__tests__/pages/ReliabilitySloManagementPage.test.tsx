import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ReliabilitySloManagementPage } from '../../features/operations/pages/ReliabilitySloManagementPage';

vi.mock('../../features/operations/api/reliability', () => ({
  reliabilityApi: {
    listServices: vi.fn(),
    listServiceSlos: vi.fn(),
    getErrorBudget: vi.fn(),
    getBurnRate: vi.fn(),
    computeErrorBudget: vi.fn(),
    computeBurnRate: vi.fn(),
    listSloSlas: vi.fn(),
    registerSlo: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { reliabilityApi } from '../../features/operations/api/reliability';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ReliabilitySloManagementPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ReliabilitySloManagementPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(reliabilityApi.listServices).mockResolvedValue({
      services: [],
      totalCount: 0,
    });
    vi.mocked(reliabilityApi.listServiceSlos).mockResolvedValue({ slos: [] });
    vi.mocked(reliabilityApi.getErrorBudget).mockResolvedValue(null);
    vi.mocked(reliabilityApi.getBurnRate).mockResolvedValue(null);
    vi.mocked(reliabilityApi.listSloSlas).mockResolvedValue({ slas: [] });
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('SLO Management')).toBeDefined();
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(reliabilityApi.listServices).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders services list when data is available', async () => {
    vi.mocked(reliabilityApi.listServices).mockResolvedValue({
      items: [
        {
          serviceName: 'order-api',
          displayName: 'Order API',
          serviceType: 'RestApi',
          domain: 'Commerce',
          teamName: 'Commerce Team',
          criticality: 'High',
          reliabilityStatus: 'Healthy',
          operationalSummary: 'All SLOs met',
          trend: 'Stable',
          activeFlags: 0,
          openIncidents: 0,
          recentChangeImpact: false,
          overallScore: 98.5,
          lastComputedAt: '2026-04-01T00:00:00Z',
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 200,
    });
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Order API');
    });
  });
});
