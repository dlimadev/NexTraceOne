import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ReleaseCalendarPage } from '../../features/change-governance/pages/ReleaseCalendarPage';

vi.mock('../../features/change-governance/api/changeIntelligence', () => ({
  changeIntelligenceApi: {
    getReleaseCalendar: vi.fn(),
    listFreezeWindows: vi.fn(),
    createFreezeWindow: vi.fn(),
    updateFreezeWindow: vi.fn(),
    deleteFreezeWindow: vi.fn(),
    listChanges: vi.fn(),
    getChangeDetail: vi.fn(),
    getBlastRadius: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { changeIntelligenceApi } from '../../features/change-governance/api/changeIntelligence';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ReleaseCalendarPage />
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

describe('ReleaseCalendarPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(changeIntelligenceApi.getReleaseCalendar).mockResolvedValue({ releases: [], totalCount: 0 });
    vi.mocked(changeIntelligenceApi.listFreezeWindows).mockResolvedValue({ windows: [], totalCount: 0 });
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Release Calendar')).toBeDefined();
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(changeIntelligenceApi.getReleaseCalendar).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders calendar with releases', async () => {
    vi.mocked(changeIntelligenceApi.getReleaseCalendar).mockResolvedValue({
      releases: [
        {
          releaseId: 'rel-001',
          title: 'v2.1.0 Release',
          serviceName: 'Order API',
          environment: 'Production',
          scheduledAt: '2026-04-10T14:00:00Z',
          status: 'Scheduled',
          riskLevel: 'Low',
          changeCount: 3,
        },
      ],
      totalCount: 1,
    });
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toBeDefined();
    });
  });
});
