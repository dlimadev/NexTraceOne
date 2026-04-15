import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { PreflightPage } from '../../features/platform-admin/pages/PreflightPage';

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getPreflight: vi.fn(),
    getConfigHealth: vi.fn(),
    getPendingMigrations: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';

const mockPreflightReady = {
  overallStatus: 'Ok' as const,
  checks: [
    {
      name: 'PostgreSQL',
      status: 'Ok' as const,
      message: 'PostgreSQL accessible — PostgreSQL 16.2',
      suggestion: null,
      isRequired: true,
    },
    {
      name: 'JWT Secret',
      status: 'Ok' as const,
      message: 'JWT Secret configured — 64 characters.',
      suggestion: null,
      isRequired: true,
    },
    {
      name: 'Disk Space',
      status: 'Warning' as const,
      message: 'Low disk space: 4 GB available.',
      suggestion: 'Free up disk space.',
      isRequired: false,
    },
  ],
  isReadyToStart: true,
  checkedAt: '2026-04-15T10:00:00Z',
  version: '1.0.0',
};

const mockPreflightNotReady = {
  ...mockPreflightReady,
  overallStatus: 'Error' as const,
  checks: [
    {
      name: 'PostgreSQL',
      status: 'Error' as const,
      message: 'PostgreSQL not accessible: Connection refused',
      suggestion: 'Ensure PostgreSQL is running and the connection string is correct.',
      isRequired: true,
    },
  ],
  isReadyToStart: false,
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

describe('PreflightPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the page title', () => {
    vi.mocked(platformAdminApi.getPreflight).mockReturnValue(new Promise(() => {}));
    render(<PreflightPage />, { wrapper: createWrapper() });
    expect(screen.getByText(/System Preflight Check/i)).toBeDefined();
  });

  it('shows ready banner when all checks pass', async () => {
    vi.mocked(platformAdminApi.getPreflight).mockResolvedValue(mockPreflightReady);
    render(<PreflightPage />, { wrapper: createWrapper() });
    await waitFor(() => {
      expect(screen.getByText(/Platform is ready to start/i)).toBeDefined();
    });
  });

  it('shows not-ready banner when required checks fail', async () => {
    vi.mocked(platformAdminApi.getPreflight).mockResolvedValue(mockPreflightNotReady);
    render(<PreflightPage />, { wrapper: createWrapper() });
    await waitFor(() => {
      expect(screen.getByText(/Platform is not ready/i)).toBeDefined();
    });
  });

  it('renders individual check names', async () => {
    vi.mocked(platformAdminApi.getPreflight).mockResolvedValue(mockPreflightReady);
    render(<PreflightPage />, { wrapper: createWrapper() });
    await waitFor(() => {
      expect(screen.getByText('PostgreSQL')).toBeDefined();
      expect(screen.getByText('JWT Secret')).toBeDefined();
      expect(screen.getByText('Disk Space')).toBeDefined();
    });
  });

  it('shows suggestion text for warning checks', async () => {
    vi.mocked(platformAdminApi.getPreflight).mockResolvedValue(mockPreflightReady);
    render(<PreflightPage />, { wrapper: createWrapper() });
    await waitFor(() => {
      expect(screen.getByText(/Free up disk space/i)).toBeDefined();
    });
  });

  it('shows the subtitle text', () => {
    vi.mocked(platformAdminApi.getPreflight).mockReturnValue(new Promise(() => {}));
    render(<PreflightPage />, { wrapper: createWrapper() });
    expect(screen.getByText(/Checking all platform dependencies/i)).toBeDefined();
  });
});
