import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { SelfServicePortalPage } from '../../features/catalog/pages/SelfServicePortalPage';

vi.mock('../../api/client', () => ({
  default: {
    get: vi.fn().mockResolvedValue({ data: {} }),
  },
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

const renderWithProviders = (ui: React.ReactElement) => {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>{ui}</MemoryRouter>
    </QueryClientProvider>
  );
};

describe('SelfServicePortalPage', () => {
  it('renders title', () => {
    renderWithProviders(<SelfServicePortalPage />);
    expect(screen.getByText('selfServicePortal.title')).toBeDefined();
  });

  it('renders all action groups', () => {
    renderWithProviders(<SelfServicePortalPage />);
    const groups = [
      'selfServicePortal.groups.services',
      'selfServicePortal.groups.contracts',
      'selfServicePortal.groups.changes',
      'selfServicePortal.groups.access',
      'selfServicePortal.groups.operations',
      'selfServicePortal.groups.ai',
    ];
    for (const group of groups) {
      expect(screen.getByText(group)).toBeDefined();
    }
  });

  it('renders action links', () => {
    renderWithProviders(<SelfServicePortalPage />);
    expect(screen.getByText('selfServicePortal.actions.createService')).toBeDefined();
    expect(screen.getByText('selfServicePortal.actions.publishRestContract')).toBeDefined();
    expect(screen.getByText('selfServicePortal.actions.promoteRelease')).toBeDefined();
    expect(screen.getByText('selfServicePortal.actions.requestJitAccess')).toBeDefined();
    expect(screen.getByText('selfServicePortal.actions.runRunbook')).toBeDefined();
    expect(screen.getByText('selfServicePortal.actions.aiAgentMarketplace')).toBeDefined();
  });

  it('all action tiles are rendered as links', () => {
    renderWithProviders(<SelfServicePortalPage />);
    const links = screen.getAllByRole('link');
    // 6 groups × 3 actions = 18 links
    expect(links.length).toBe(18);
  });
});
