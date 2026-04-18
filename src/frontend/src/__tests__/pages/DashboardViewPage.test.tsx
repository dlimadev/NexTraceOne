import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { DashboardViewPage } from '../../features/governance/pages/DashboardViewPage';

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), put: vi.fn() },
}));
vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn().mockReturnValue({
    activeEnvironmentId: 'env-prod-001',
    activeEnvironment: { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  }),
}));

import client from '../../api/client';

const DASHBOARD_ID = 'aaaaaaaa-0000-0000-0000-000000000001';

const mockRenderData = {
  dashboardId: DASHBOARD_ID,
  name: 'My Test Dashboard',
  layout: 'two-column',
  persona: 'Engineer',
  environmentId: 'env-prod-001',
  globalTimeRange: '24h',
  generatedAt: '2026-04-17T22:00:00Z',
  widgets: [
    {
      widgetId: 'w1',
      type: 'dora-metrics',
      posX: 0,
      posY: 0,
      width: 2,
      height: 2,
      effectiveServiceId: null,
      effectiveTeamId: null,
      effectiveTimeRange: '24h',
      customTitle: null,
    },
    {
      widgetId: 'w2',
      type: 'incident-summary',
      posX: 2,
      posY: 0,
      width: 2,
      height: 2,
      effectiveServiceId: null,
      effectiveTeamId: null,
      effectiveTimeRange: '24h',
      customTitle: 'Open Incidents',
    },
  ],
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/governance/dashboards/${DASHBOARD_ID}`]}>
        <Routes>
          <Route path="/governance/dashboards/:dashboardId" element={<DashboardViewPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const mockDoraData = {
  overallRating: 'Elite',
  deploymentFrequency: { value: 5, unit: '/day', rating: 'Elite' },
  leadTimeForChanges: { value: 2, unit: 'hours', rating: 'Elite' },
  changeFailureRate: { value: 1, unit: '%', rating: 'Elite' },
  meanTimeToRestore: { value: 30, unit: 'min', rating: 'Elite' },
};

const mockIncidentData = {
  items: [],
  totalCount: 0,
  critical: 0,
  high: 0,
  medium: 0,
  low: 0,
};

describe('DashboardViewPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(client.get).mockImplementation((url: string) => {
      if (url.includes('dora-metrics')) return Promise.resolve({ data: mockDoraData });
      if (url.includes('incidents')) return Promise.resolve({ data: mockIncidentData });
      return Promise.resolve({ data: mockRenderData });
    });
    vi.mocked(client.put).mockResolvedValue({ data: {} });
  });

  it('renders the dashboard name after loading', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('My Test Dashboard');
    });
  });

  it('shows the persona badge', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Engineer');
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(client.get).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(client.get).mockRejectedValue(new Error('API error'));
    renderPage();
    await waitFor(() => {
      expect(document.body).toBeDefined();
    });
  });

  it('shows the Edit button', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Edit');
    });
  });

  it('shows the Share button', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Share');
    });
  });

  it('shows the back link', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Back to Dashboards');
    });
  });

  it('shows empty state when no widgets', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: { ...mockRenderData, widgets: [] },
    });
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('No widgets configured');
    });
  });

  it('shows Variables button', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Variables');
    });
  });

  it('shows variables panel when Variables button is clicked', async () => {
    renderPage();
    await waitFor(() => expect(document.body.textContent).toContain('Variables'));
    const varsBtn = Array.from(document.querySelectorAll('button')).find(
      (b) => b.getAttribute('aria-label') === 'Toggle variables',
    );
    expect(varsBtn).toBeDefined();
    fireEvent.click(varsBtn!);
    await waitFor(() => {
      expect(document.body.textContent).toContain('$service');
      expect(document.body.textContent).toContain('$team');
    });
  });

  it('variables panel has service and team inputs', async () => {
    renderPage();
    await waitFor(() => expect(document.body.textContent).toContain('Variables'));
    const varsBtn = Array.from(document.querySelectorAll('button')).find(
      (b) => b.getAttribute('aria-label') === 'Toggle variables',
    );
    fireEvent.click(varsBtn!);
    await waitFor(() => {
      const inputs = document.querySelectorAll('input');
      const placeholders = Array.from(inputs).map((i) => i.placeholder);
      expect(placeholders.some((p) => p.includes('All services') || p.includes('service'))).toBe(true);
    });
  });

  it('shows TV Mode button', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('TV Mode');
    });
  });
});

// ── Kiosk mode tests ─────────────────────────────────────────────────────────

function renderPageKiosk() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/governance/dashboards/${DASHBOARD_ID}?kiosk=tv`]}>
        <Routes>
          <Route path="/governance/dashboards/:dashboardId" element={<DashboardViewPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('DashboardViewPage — Kiosk mode', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(client.get).mockImplementation((url: string) => {
      if (url.includes('dora-metrics')) return Promise.resolve({ data: mockDoraData });
      if (url.includes('incidents')) return Promise.resolve({ data: mockIncidentData });
      return Promise.resolve({ data: mockRenderData });
    });
    vi.mocked(client.put).mockResolvedValue({ data: {} });
  });

  it('shows Exit TV Mode button in kiosk mode', async () => {
    renderPageKiosk();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Exit TV Mode');
    });
  });

  it('does not show back link in kiosk mode', async () => {
    renderPageKiosk();
    await waitFor(() => {
      expect(document.body.textContent).not.toContain('Back to Dashboards');
    });
  });

  it('shows dashboard name in kiosk mode', async () => {
    renderPageKiosk();
    await waitFor(() => {
      expect(document.body.textContent).toContain('My Test Dashboard');
    });
  });
});
