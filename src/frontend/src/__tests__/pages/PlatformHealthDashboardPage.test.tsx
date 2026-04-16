import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { PlatformHealthDashboardPage } from '../../features/platform-admin/pages/PlatformHealthDashboardPage';

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getPreflight: vi.fn(),
    getConfigHealth: vi.fn(),
    getPendingMigrations: vi.fn(),
  },
}));

vi.mock('../../features/operations/api/platformOps', () => ({
  platformOpsApi: {
    getHealth: vi.fn(),
    getJobs: vi.fn(),
    getQueues: vi.fn(),
    getEvents: vi.fn(),
    getConfig: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import { platformOpsApi } from '../../features/operations/api/platformOps';

const mockPreflight = {
  overallStatus: 'Ok' as const,
  checks: [
    { name: 'PostgreSQL', status: 'Ok' as const, message: 'OK', suggestion: null, isRequired: true },
  ],
  isReadyToStart: true,
  checkedAt: '2026-04-15T10:00:00Z',
  version: '1.0.0',
};

const mockHealth = {
  overallStatus: 'Healthy',
  subsystems: [
    { name: 'API', status: 'Healthy', description: 'All endpoints OK', lastCheckedAt: '2026-04-15T10:00:00Z' },
    { name: 'Database', status: 'Healthy', description: 'PostgreSQL operational', lastCheckedAt: '2026-04-15T10:00:00Z' },
  ],
  uptimeSeconds: 3600,
  version: '1.0.0',
  checkedAt: '2026-04-15T10:00:00Z',
};

const mockConfigHealth = {
  status: 'ok' as const,
  checks: [
    { key: 'Jwt__Secret', status: 'ok' as const, message: 'JWT Secret configured — 64 chars', suggestion: null },
    { key: 'Smtp__Host', status: 'warning' as const, message: 'SMTP not configured', suggestion: 'Set Smtp__Host.' },
  ],
  generatedAt: '2026-04-15T10:00:00Z',
};

const mockMigrations = {
  totalPending: 0,
  isSafeToApply: true,
  migrations: [],
  checkedAt: '2026-04-15T10:00:00Z',
};

const createWrapper = () => {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return function Wrapper({ children }: { children: React.ReactNode }) {
    return (
      <MemoryRouter>
        <QueryClientProvider client={qc}>{children}</QueryClientProvider>
      </MemoryRouter>
    );
  };
};

describe('PlatformHealthDashboardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(platformAdminApi.getPreflight).mockResolvedValue(mockPreflight);
    vi.mocked(platformAdminApi.getConfigHealth).mockResolvedValue(mockConfigHealth);
    vi.mocked(platformAdminApi.getPendingMigrations).mockResolvedValue(mockMigrations);
    vi.mocked(platformOpsApi.getHealth).mockResolvedValue(mockHealth as never);
  });

  it('renders the page title', () => {
    render(<PlatformHealthDashboardPage />, { wrapper: createWrapper() });
    expect(screen.getByText(/Platform Health Dashboard/i)).toBeDefined();
  });

  it('renders the overview tab by default', () => {
    render(<PlatformHealthDashboardPage />, { wrapper: createWrapper() });
    expect(screen.getByRole('button', { name: /Overview/i })).toBeDefined();
  });

  it('shows subsystems after loading', async () => {
    render(<PlatformHealthDashboardPage />, { wrapper: createWrapper() });
    await waitFor(() => {
      expect(screen.getByText('API')).toBeDefined();
      expect(screen.getByText('Database')).toBeDefined();
    });
  });

  it('switches to Config Health tab and shows checks', async () => {
    render(<PlatformHealthDashboardPage />, { wrapper: createWrapper() });
    const user = userEvent.setup();
    await user.click(screen.getByRole('button', { name: /Config Health/i }));
    await waitFor(() => {
      expect(screen.getByText('Jwt__Secret')).toBeDefined();
    });
  });

  it('switches to Migrations tab and shows up-to-date status', async () => {
    render(<PlatformHealthDashboardPage />, { wrapper: createWrapper() });
    const user = userEvent.setup();
    await user.click(screen.getByRole('button', { name: /Migrations/i }));
    await waitFor(() => {
      expect(screen.getByText(/All migrations are up to date/i)).toBeDefined();
    });
  });

  it('shows SMTP warning suggestion in config tab', async () => {
    render(<PlatformHealthDashboardPage />, { wrapper: createWrapper() });
    const user = userEvent.setup();
    await user.click(screen.getByRole('button', { name: /Config Health/i }));
    await waitFor(() => {
      expect(screen.getByText(/Set Smtp__Host/i)).toBeDefined();
    });
  });
});
