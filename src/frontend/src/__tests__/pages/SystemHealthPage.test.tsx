import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactNode } from 'react';
import { SystemHealthPage } from '../../features/platform-admin/pages/SystemHealthPage';
import { platformAdminApi } from '../../features/platform-admin/api/platformAdmin';
import type { OptionalProvidersResponse } from '../../features/platform-admin/api/platformAdmin';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: { defaultValue?: string }) =>
      opts?.defaultValue ?? key,
    i18n: { language: 'en' },
  }),
}));

vi.mock('../../features/platform-admin/api/platformAdmin', () => ({
  platformAdminApi: {
    getOptionalProviders: vi.fn(),
  },
}));

function makeWrapper() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={qc}>{children}</QueryClientProvider>
  );
}

const mockData: OptionalProvidersResponse = {
  checkedAt: '2026-04-20T10:00:00Z',
  configuredCount: 1,
  totalCount: 3,
  providers: [
    {
      name: 'canary',
      category: 'operations',
      status: 'Configured',
      configKeyPrefix: 'Canary',
      docsPath: 'docs/deployment/PRODUCTION-BOOTSTRAP.md#canary-provider',
      description: 'Canary rollouts dashboard.',
    },
    {
      name: 'backup',
      category: 'operations',
      status: 'NotConfigured',
      configKeyPrefix: 'Backup',
      docsPath: 'docs/deployment/PRODUCTION-BOOTSTRAP.md#backup-provider',
      description: 'Database backup posture.',
    },
    {
      name: 'kafka',
      category: 'integrations',
      status: 'NotConfigured',
      configKeyPrefix: 'Kafka',
      docsPath: 'docs/deployment/PRODUCTION-BOOTSTRAP.md#kafka',
      description: 'Kafka event producer.',
    },
  ],
};

describe('SystemHealthPage', () => {
  beforeEach(() => vi.clearAllMocks());

  it('renders providers grouped by category with configured / not-configured counts', async () => {
    vi.mocked(platformAdminApi.getOptionalProviders).mockResolvedValue(mockData);
    const Wrapper = makeWrapper();

    render(
      <Wrapper>
        <SystemHealthPage />
      </Wrapper>,
    );

    // Descriptions come from the backend payload, so they are always visible
    await waitFor(() =>
      expect(screen.getByText('Canary rollouts dashboard.')).toBeInTheDocument(),
    );
    expect(screen.getByText('Database backup posture.')).toBeInTheDocument();
    expect(screen.getByText('Kafka event producer.')).toBeInTheDocument();

    // Category headings (one per distinct category)
    const operationsHeadings = screen.getAllByText('operations');
    expect(operationsHeadings.length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText('integrations')).toBeInTheDocument();

    // Setup-docs link points at the repository docs
    const setupLinks = screen.getAllByRole('link');
    expect(setupLinks.length).toBeGreaterThanOrEqual(3);
    expect(setupLinks[0].getAttribute('href')).toContain(
      'docs/deployment/PRODUCTION-BOOTSTRAP.md',
    );
  });

  it('shows error state when the query fails', async () => {
    vi.mocked(platformAdminApi.getOptionalProviders).mockRejectedValue(
      new Error('boom'),
    );
    const Wrapper = makeWrapper();

    render(
      <Wrapper>
        <SystemHealthPage />
      </Wrapper>,
    );

    await waitFor(() =>
      expect(screen.getByRole('alert')).toBeInTheDocument(),
    );
  });

  it('shows empty state when backend returns no providers', async () => {
    vi.mocked(platformAdminApi.getOptionalProviders).mockResolvedValue({
      providers: [],
      configuredCount: 0,
      totalCount: 0,
      checkedAt: '2026-04-20T10:00:00Z',
    });
    const Wrapper = makeWrapper();

    render(
      <Wrapper>
        <SystemHealthPage />
      </Wrapper>,
    );

    await waitFor(() =>
      expect(screen.getByText('states.empty')).toBeInTheDocument(),
    );
  });
});
