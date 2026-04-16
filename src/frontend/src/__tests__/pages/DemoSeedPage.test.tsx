import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { DemoSeedPage } from '../../features/platform-admin/pages/DemoSeedPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { DemoSeedStatus, DemoSeedResult, DemoSeedClearResult } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string, _opts?: Record<string, unknown>) => key, i18n: { language: 'en' } }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getDemoSeedStatus: vi.fn(),
    runDemoSeed: vi.fn(),
    clearDemoData: vi.fn(),
  },
}));

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
const Wrapper = ({ children }: { children: ReactNode }) => (
  <QueryClientProvider client={qc}>{children}</QueryClientProvider>
);

const mockNotSeeded: DemoSeedStatus = {
  state: 'NotSeeded',
  entitiesCount: 0,
  servicesCount: 0,
  changesCount: 0,
  incidentsCount: 0,
  simulatedNote: 'Simulated status',
};

const mockSeeded: DemoSeedStatus = {
  state: 'Seeded',
  seededAt: '2026-04-01T10:00:00Z',
  entitiesCount: 120,
  servicesCount: 10,
  changesCount: 30,
  incidentsCount: 5,
  simulatedNote: 'Simulated status',
};

const mockSeedResult: DemoSeedResult = {
  success: true,
  durationMs: 1500,
  entitiesCreated: 120,
  message: 'Seed completed',
};

const mockClearResult: DemoSeedClearResult = {
  success: true,
  entitiesRemoved: 120,
  message: 'Cleared',
};

describe('DemoSeedPage', () => {
  beforeEach(() => {
    qc.clear();
    vi.clearAllMocks();
  });

  it('shows loading state', () => {
    vi.mocked(platformAdminApi.getDemoSeedStatus).mockImplementation(() => new Promise(() => {}));
    render(<DemoSeedPage />, { wrapper: Wrapper });
    expect(screen.getByText('loading')).toBeDefined();
  });

  it('shows error state', async () => {
    vi.mocked(platformAdminApi.getDemoSeedStatus).mockRejectedValue(new Error('fail'));
    render(<DemoSeedPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('error')).toBeDefined());
  });

  it('renders page title', async () => {
    vi.mocked(platformAdminApi.getDemoSeedStatus).mockResolvedValue(mockNotSeeded);
    render(<DemoSeedPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('title')).toBeDefined());
  });

  it('shows not-seeded message and seed button when state is NotSeeded', async () => {
    vi.mocked(platformAdminApi.getDemoSeedStatus).mockResolvedValue(mockNotSeeded);
    render(<DemoSeedPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('notSeededMsg')).toBeDefined();
      expect(screen.getByText('seedBtn')).toBeDefined();
    });
  });

  it('shows seeded banner and clear button when state is Seeded', async () => {
    vi.mocked(platformAdminApi.getDemoSeedStatus).mockResolvedValue(mockSeeded);
    render(<DemoSeedPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('seededBannerTitle')).toBeDefined();
      expect(screen.getByText('clearBtn')).toBeDefined();
    });
  });

  it('shows stats when seeded', async () => {
    vi.mocked(platformAdminApi.getDemoSeedStatus).mockResolvedValue(mockSeeded);
    render(<DemoSeedPage />, { wrapper: Wrapper });
    await waitFor(() => {
      expect(screen.getByText('statServices')).toBeDefined();
      expect(screen.getByText('statChanges')).toBeDefined();
    });
  });

  it('triggers seed mutation on seed button click', async () => {
    vi.mocked(platformAdminApi.getDemoSeedStatus).mockResolvedValue(mockNotSeeded);
    vi.mocked(platformAdminApi.runDemoSeed).mockResolvedValue(mockSeedResult);
    render(<DemoSeedPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByText('seedBtn'));
    fireEvent.click(screen.getByText('seedBtn'));
    await waitFor(() => expect(platformAdminApi.runDemoSeed).toHaveBeenCalledWith({}));
  });

  it('shows confirm UI on clear button click', async () => {
    vi.mocked(platformAdminApi.getDemoSeedStatus).mockResolvedValue(mockSeeded);
    render(<DemoSeedPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByText('clearBtn'));
    fireEvent.click(screen.getByText('clearBtn'));
    expect(screen.getByText('confirmClearMsg')).toBeDefined();
    expect(screen.getByText('confirmYes')).toBeDefined();
  });

  it('triggers clear mutation on confirm', async () => {
    vi.mocked(platformAdminApi.getDemoSeedStatus).mockResolvedValue(mockSeeded);
    vi.mocked(platformAdminApi.clearDemoData).mockResolvedValue(mockClearResult);
    render(<DemoSeedPage />, { wrapper: Wrapper });
    await waitFor(() => screen.getByText('clearBtn'));
    fireEvent.click(screen.getByText('clearBtn'));
    await waitFor(() => screen.getByText('confirmYes'));
    fireEvent.click(screen.getByText('confirmYes'));
    await waitFor(() => expect(platformAdminApi.clearDemoData).toHaveBeenCalled());
  });

  it('shows simulated note', async () => {
    vi.mocked(platformAdminApi.getDemoSeedStatus).mockResolvedValue(mockNotSeeded);
    render(<DemoSeedPage />, { wrapper: Wrapper });
    await waitFor(() => expect(screen.getByText('Simulated status')).toBeDefined());
  });
});
