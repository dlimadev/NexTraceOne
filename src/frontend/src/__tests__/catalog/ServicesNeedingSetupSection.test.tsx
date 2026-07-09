import { describe, it, expect, vi } from 'vitest';
import type { ReactNode } from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { ServicesNeedingSetupSection } from '../../features/catalog/components/ServicesNeedingSetupSection';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, o?: Record<string, unknown>) => (o?.name ? `${o.name} · ${o.status}` : k) }) }));

const listServices = vi.fn();
vi.mock('../../features/catalog/api', () => ({ serviceCatalogApi: { listServices: (...a: unknown[]) => listServices(...a) } }));

function wrap(ui: ReactNode) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={qc}><MemoryRouter>{ui}</MemoryRouter></QueryClientProvider>);
}

describe('ServicesNeedingSetupSection', () => {
  it('lists Planning services when present', async () => {
    listServices.mockResolvedValue({ items: [{ serviceId: 's1', displayName: 'orders-api', lifecycleStatus: 'Planning', domain: 'Commerce' }] });
    wrap(<ServicesNeedingSetupSection />);
    await waitFor(() => expect(screen.getByText(/orders-api/)).toBeInTheDocument());
  });

  it('renders nothing (honest-null) when there are none', async () => {
    listServices.mockResolvedValue({ items: [] });
    const { container } = wrap(<ServicesNeedingSetupSection />);
    await waitFor(() => expect(listServices).toHaveBeenCalled());
    expect(container.textContent).toBe('');
  });
});
