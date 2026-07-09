import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { SelfServicePortalPage } from '../../features/catalog/pages/SelfServicePortalPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string) => k }) }));
vi.mock('../../features/catalog/api', () => ({ serviceCatalogApi: { listServices: vi.fn(() => Promise.resolve({ items: [] })) } }));

function wrap() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(<QueryClientProvider client={qc}><MemoryRouter><SelfServicePortalPage /></MemoryRouter></QueryClientProvider>);
}

describe('SelfServicePortalPage', () => {
  it('leads with the onboarding golden path linking to /services/onboard', () => {
    wrap();
    const links = screen.getAllByRole('link').map((a) => a.getAttribute('href'));
    expect(links).toContain('/services/onboard');
  });

  it('has no dead legacy links', () => {
    wrap();
    const links = screen.getAllByRole('link').map((a) => a.getAttribute('href'));
    expect(links).not.toContain('/catalog/services/create');
    expect(links).not.toContain('/catalog/scaffold');
    expect(links).not.toContain('/contracts/governance/health');
  });
});
