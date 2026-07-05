/**
 * Testes do reshell "browse-first" da ServiceCatalogPage.
 *
 * Cobrem o comportamento novo introduzido na Task 6:
 *  - O segmento por defeito é "Browse" (a barra de pesquisa do ServiceBrowseSurface
 *    está visível e a aba de análise "overview" NÃO é a vista por defeito).
 *  - Existe um controlo de segmento "Explorar".
 *  - A CTA "Registar serviço" está presente e é secundária (ghost, não primary).
 */
import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '../test-utils';
import { ServiceCatalogPage } from '../../features/catalog/pages/ServiceCatalogPage';

vi.mock('../../features/catalog/api', () => ({
  serviceCatalogApi: {
    getGraph: vi.fn(),
    getNodeHealth: vi.fn(),
    getImpactPropagation: vi.fn(),
    listSnapshots: vi.fn(),
    getTemporalDiff: vi.fn(),
    createSnapshot: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn(), put: vi.fn(), delete: vi.fn() },
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

import { serviceCatalogApi } from '../../features/catalog/api';

const mockGraph = {
  services: [
    {
      serviceAssetId: 'svc-001',
      name: 'Order Service',
      domain: 'Commerce',
      teamName: 'Commerce Team',
      serviceType: 'RestApi',
      lifecycleStatus: 'Active',
    },
  ],
  apis: [
    {
      apiAssetId: 'api-001',
      name: 'Order API',
      routePattern: '/api/v1/orders',
      version: '1',
      visibility: 'Public',
      ownerServiceAssetId: 'svc-001',
      consumers: [],
    },
  ],
};

describe('ServiceCatalogPage — reshell Browse | Explorar', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(serviceCatalogApi.getGraph).mockResolvedValue(mockGraph as never);
    vi.mocked(serviceCatalogApi.getNodeHealth).mockResolvedValue({ nodes: [] } as never);
    vi.mocked(serviceCatalogApi.listSnapshots).mockResolvedValue({ items: [] } as never);
  });

  it('mounts with the Browse segment active — search bar visible, analysis "overview" not the default view', async () => {
    render(<ServiceCatalogPage />);

    // A barra de pesquisa do ServiceBrowseSurface (SearchInput type="search") deve aparecer.
    expect(await screen.findByRole('searchbox')).toBeInTheDocument();

    // A aba de análise "overview" só existe dentro do segmento Explorar; no arranque não deve estar presente.
    expect(screen.queryByRole('tab', { name: /overview/i })).not.toBeInTheDocument();
  });

  it('exposes an "Explorar" segment control', async () => {
    render(<ServiceCatalogPage />);
    // t('serviceCatalog.browse.segment.explore') → 'Explore'
    expect(await screen.findByRole('tab', { name: /^Explore$/i })).toBeInTheDocument();
  });

  it('renders the "Register Service" CTA as a secondary (ghost) button, not primary', async () => {
    render(<ServiceCatalogPage />);
    const cta = await screen.findByRole('button', { name: /register service/i });
    expect(cta).toBeInTheDocument();
    // Ghost aplica text-muted; primary aplicaria bg-accent.
    expect(cta).toHaveClass('text-muted');
    expect(cta).not.toHaveClass('bg-accent');
  });
});
