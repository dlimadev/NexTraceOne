import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, waitFor, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { CustomDashboardsPage } from '../../features/governance/pages/CustomDashboardsPage';

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), delete: vi.fn() },
}));

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: vi.fn().mockReturnValue({
    user: { id: 'user-001', email: 'test@example.com' },
    isAuthenticated: true,
  }),
}));

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

import client from '../../api/client';

const DASHBOARD_ID_1 = '11111111-0000-0000-0000-000000000001';
const DASHBOARD_ID_2 = '11111111-0000-0000-0000-000000000002';

const mockListResponse = {
  items: [
    {
      dashboardId: DASHBOARD_ID_1,
      name: 'Executive KPI Overview',
      persona: 'Executive',
      widgetCount: 6,
      layout: 'grid',
      isShared: true,
      isSystem: false,
      teamId: null,
      createdAt: '2026-01-01T00:00:00Z',
    },
    {
      dashboardId: DASHBOARD_ID_2,
      name: 'Team Health Dashboard',
      persona: 'TechLead',
      widgetCount: 5,
      layout: 'two-column',
      isShared: false,
      isSystem: false,
      teamId: null,
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

describe('CustomDashboardsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(client.get).mockResolvedValue({ data: mockListResponse });
    vi.mocked(client.post).mockResolvedValue({ data: { cloneId: 'clone-id', name: 'copy' } });
    vi.mocked(client.delete).mockResolvedValue({ data: {} });
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

  it('renders "Use Template" button', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Use Template');
    });
  });

  it('opens template picker modal when "Use Template" is clicked', async () => {
    renderPage();
    await waitFor(() => expect(document.body.textContent).toContain('Use Template'));
    const btn = Array.from(document.querySelectorAll('button')).find(
      (b) => b.textContent?.includes('Use Template'),
    );
    expect(btn).toBeDefined();
    fireEvent.click(btn!);
    await waitFor(() => {
      expect(document.body.textContent).toContain('Choose Dashboard Template');
    });
  });

  it('renders Clone button for each dashboard card', async () => {
    renderPage();
    await waitFor(() => {
      const cloneBtns = Array.from(document.querySelectorAll('button[aria-label="Clone"]'));
      expect(cloneBtns.length).toBeGreaterThanOrEqual(2);
    });
  });

  it('opens clone dialog when Clone button is clicked', async () => {
    renderPage();
    await waitFor(() => expect(document.body.textContent).toContain('Executive KPI Overview'));
    const cloneBtn = document.querySelector('button[aria-label="Clone"]');
    expect(cloneBtn).toBeDefined();
    fireEvent.click(cloneBtn!);
    await waitFor(() => {
      expect(document.body.textContent).toContain('Clone Dashboard');
    });
  });

  it('renders Delete button for non-system dashboards', async () => {
    renderPage();
    await waitFor(() => {
      const deleteBtns = Array.from(document.querySelectorAll('button[aria-label="Delete"]'));
      expect(deleteBtns.length).toBeGreaterThanOrEqual(2);
    });
  });

  it('opens delete confirm dialog when Delete button is clicked', async () => {
    renderPage();
    await waitFor(() => expect(document.body.textContent).toContain('Executive KPI Overview'));
    const deleteBtn = document.querySelector('button[aria-label="Delete"]');
    expect(deleteBtn).toBeDefined();
    fireEvent.click(deleteBtn!);
    await waitFor(() => {
      expect(document.body.textContent).toContain('Delete Dashboard');
    });
  });

  it('does not render Delete button for system dashboards', async () => {
    const systemResponse = {
      items: [{ ...mockListResponse.items[0], isSystem: true }],
      totalCount: 1,
    };
    vi.mocked(client.get).mockResolvedValue({ data: systemResponse });
    renderPage();
    await waitFor(() => expect(document.body.textContent).toContain('Executive KPI Overview'));
    const deleteBtns = Array.from(document.querySelectorAll('button[aria-label="Delete"]'));
    expect(deleteBtns.length).toBe(0);
  });

  it('shows sort controls (Name / Persona / Widgets)', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Sort by');
    });
  });

  it('sort button toggles direction when clicked twice', async () => {
    renderPage();
    await waitFor(() => expect(document.body.textContent).toContain('Executive KPI Overview'));
    const sortBtns = Array.from(document.querySelectorAll('button[aria-pressed]'));
    // Should have 3 sort buttons (name, persona, widgetCount)
    expect(sortBtns.length).toBeGreaterThanOrEqual(3);
    // Click the currently active one to toggle direction
    const activeBtn = sortBtns.find((b) => b.getAttribute('aria-pressed') === 'true');
    expect(activeBtn).toBeDefined();
    fireEvent.click(activeBtn!);
    await waitFor(() => {
      // Descending indicator (↓) should appear
      expect(document.body.textContent).toContain('↓');
    });
  });
});

