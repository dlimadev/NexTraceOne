import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ServiceFeatureFlagsTab } from '../../features/catalog/components/ServiceFeatureFlagsTab';
import { serviceFeatureFlagsApi } from '../../features/catalog/api/featureFlags';

vi.mock('react-i18next', async (orig) => ({
  ...(await orig<typeof import('react-i18next')>()),
  useTranslation: () => ({ t: (_k: string, d?: string) => d ?? _k, i18n: { language: 'en' } }),
}));
vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: () => ({ activeEnvironmentId: 'env-prod' }),
}));
vi.mock('../../features/catalog/api/featureFlags', () => ({
  serviceFeatureFlagsApi: { getDashboard: vi.fn(), toggle: vi.fn() },
}));

const flags = [
  { id: 'f1', serviceId: 'svc-1', serviceName: 'A', flagKey: 'new_ui', displayName: 'New UI', enabled: true, environment: 'prod', updatedAt: '2026-01-01T00:00:00Z' },
  { id: 'f2', serviceId: 'svc-2', serviceName: 'B', flagKey: 'beta', displayName: 'Beta', enabled: false, environment: 'prod', updatedAt: '2026-01-01T00:00:00Z' },
];

function renderTab(serviceId = 'svc-1') {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter><ServiceFeatureFlagsTab serviceId={serviceId} /></MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ServiceFeatureFlagsTab', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (serviceFeatureFlagsApi.getDashboard as ReturnType<typeof vi.fn>).mockResolvedValue({
      totalFlags: 2, enabledFlags: 1, disabledFlags: 1, affectedServices: 2, flags,
    });
  });

  it('mostra só as flags do serviço dado', async () => {
    renderTab('svc-1');
    expect(await screen.findByText('New UI')).toBeInTheDocument();
    expect(screen.queryByText('Beta')).not.toBeInTheDocument();
  });

  it('tem deep-link ao dashboard de portefólio', async () => {
    renderTab('svc-1');
    await screen.findByText('New UI');
    const link = screen.getByRole('link', { name: /portfolio|portefólio|portfólio/i });
    expect(link).toHaveAttribute('href', '/services/feature-flags');
  });

  it('o toggle chama a mutation', async () => {
    (serviceFeatureFlagsApi.toggle as ReturnType<typeof vi.fn>).mockResolvedValue(undefined);
    renderTab('svc-1');
    await screen.findByText('New UI');
    const toggle = screen.getByRole('switch');
    fireEvent.click(toggle);
    await waitFor(() => expect(serviceFeatureFlagsApi.toggle).toHaveBeenCalledWith('f1', false));
  });

  it('estado vazio quando o serviço não tem flags', async () => {
    renderTab('svc-999');
    await waitFor(() => expect(serviceFeatureFlagsApi.getDashboard).toHaveBeenCalled());
    expect(await screen.findByText(/no registered feature flags|não tem feature flags|no tiene feature flags/i)).toBeInTheDocument();
  });
});
