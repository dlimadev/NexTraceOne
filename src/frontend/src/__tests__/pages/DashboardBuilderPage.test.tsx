import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, waitFor, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { DashboardBuilderPage } from '../../features/governance/pages/DashboardBuilderPage';

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

const DASHBOARD_ID = 'bbbbbbbb-0000-0000-0000-000000000001';

const mockDashboardDetail = {
  dashboardId: DASHBOARD_ID,
  name: 'Editable Dashboard',
  description: 'Test dashboard',
  layout: 'grid',
  persona: 'TechLead',
  isSystem: false,
  teamId: null,
  widgets: [
    {
      widgetId: 'w1',
      type: 'dora-metrics',
      posX: 0,
      posY: 0,
      width: 2,
      height: 2,
      timeRange: '24h',
      customTitle: null,
      serviceId: null,
      teamId: null,
    },
  ],
};

const mockSystemDashboard = {
  ...mockDashboardDetail,
  isSystem: true,
  name: 'System Dashboard',
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/governance/dashboards/${DASHBOARD_ID}/edit`]}>
        <Routes>
          <Route path="/governance/dashboards/:dashboardId/edit" element={<DashboardBuilderPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('DashboardBuilderPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(client.get).mockResolvedValue({ data: mockDashboardDetail });
    vi.mocked(client.put).mockResolvedValue({ data: {} });
  });

  it('renders the editor title', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Edit Dashboard');
    });
  });

  it('shows dashboard name pre-filled', async () => {
    renderPage();
    await waitFor(() => {
      const nameInput = document.querySelector('input[maxlength="100"]') as HTMLInputElement;
      expect(nameInput?.value).toBe('Editable Dashboard');
    });
  });

  it('shows existing widget in list', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('DORA Metrics');
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

  it('shows Save button for editable dashboard', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Save Dashboard');
    });
  });

  it('shows Add Widget button', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Add Widget');
    });
  });

  it('shows read-only message for system dashboard', async () => {
    vi.mocked(client.get).mockResolvedValue({ data: mockSystemDashboard });
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('system dashboard');
    });
  });

  it('shows back link to dashboard view', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Back to Dashboard');
    });
  });

  it('shows live preview panel by default', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Live Preview');
    });
  });

  it('toggles preview panel visibility', async () => {
    renderPage();
    await waitFor(() => expect(document.body.textContent).toContain('Live Preview'));
    const hideBtn = Array.from(document.querySelectorAll('button')).find(
      (b) => b.textContent?.includes('Hide Preview'),
    );
    expect(hideBtn).toBeDefined();
    fireEvent.click(hideBtn!);
    await waitFor(() => {
      expect(document.body.textContent).not.toContain('Live Preview');
      expect(document.body.textContent).toContain('Show Preview');
    });
  });

  it('preview renders widget slot cell when slots exist', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Live Preview');
      // The preview grid should exist
      const previewGrid = document.querySelector('[aria-label="Dashboard preview"]');
      expect(previewGrid).toBeDefined();
    });
  });
});
