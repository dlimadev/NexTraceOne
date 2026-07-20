import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ServiceSecurityScanTab } from '../../features/catalog/components/ServiceSecurityScanTab';

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(() => Promise.resolve({ data: {} })) },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

function renderTab() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <ServiceSecurityScanTab serviceId="service-001" />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ServiceSecurityScanTab', () => {
  it('renders the scan trigger', () => {
    renderTab();
    expect(screen.getByText('runScan')).toBeDefined();
  });

  it('renders the empty state before a scan runs', () => {
    renderTab();
    expect(screen.getByText('emptyState')).toBeDefined();
  });

  it('does not render the portfolio dashboard tab', () => {
    renderTab();
    expect(screen.queryByText('tabs.dashboard')).toBeNull();
  });
});
