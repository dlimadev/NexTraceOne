/**
 * TDD para ServiceBrowseSurface — red → green.
 *
 * Verifica 4 cenários:
 * 1. Renderiza cartões de serviço para um graph com dados.
 * 2. Filtrar por domínio inexistente mostra estado "sem resultados" com botão limpar.
 * 3. Graph vazio mostra EmptyState (onboarding).
 * 4. Modo "apis" renderiza linhas ApiResultRow com os nomes das APIs.
 *
 * Configuração: usa renderWithProviders com routerProps.initialEntries para
 * simular parâmetros de URL sem interações de utilizador.
 * i18n: chaves serviceCatalog.browse.* não existem no locale → i18next devolve
 * o key path completo; assertions usam regex parcial sobre esse key path.
 */
import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';
import { ServiceBrowseSurface } from '../../features/catalog/browse/ServiceBrowseSurface';
import type { AssetGraph } from '../../types';

/* ─── Fixtures ───────────────────────────────────────────────────────────────── */

/** Graph com 2 serviços e 2 APIs — cobre cenários 1, 2, 4. */
const MOCK_GRAPH: AssetGraph = {
  services: [
    {
      serviceAssetId: 'svc-1',
      name: 'Order Service',
      domain: 'payments',
      teamName: 'Order Team',
      lifecycleStatus: 'Active',
      serviceType: 'RestApi',
      criticality: 'High',
    },
    {
      serviceAssetId: 'svc-2',
      name: 'Inventory Service',
      domain: 'warehouse',
      teamName: 'Warehouse Team',
      lifecycleStatus: 'Active',
      serviceType: 'RestApi',
      criticality: 'Medium',
    },
  ],
  apis: [
    {
      apiAssetId: 'api-1',
      name: 'Orders API',
      routePattern: '/v1/orders',
      version: 'v1',
      visibility: 'Public',
      ownerServiceAssetId: 'svc-1',
      consumers: [],
    },
    {
      apiAssetId: 'api-2',
      name: 'Inventory API',
      routePattern: '/v1/inventory',
      version: 'v1',
      visibility: 'Internal',
      ownerServiceAssetId: 'svc-2',
      consumers: [],
    },
  ],
};

/** Graph sem serviços — cobre cenário 3. */
const EMPTY_GRAPH: AssetGraph = { services: [], apis: [] };

/* ─── Handlers ───────────────────────────────────────────────────────────────── */

const DEFAULT_HANDLERS = {
  onOpenService:  vi.fn(),
  onOpenApi:      vi.fn(),
  onViewContract: vi.fn(),
};

/* ─── Testes ─────────────────────────────────────────────────────────────────── */

describe('ServiceBrowseSurface', () => {

  it('renders service cards when graph has services', () => {
    renderWithProviders(
      <ServiceBrowseSurface graph={MOCK_GRAPH} {...DEFAULT_HANDLERS} />,
    );
    // Os nomes dos serviços aparecem como headings dentro de ServiceResultCard
    expect(screen.getByRole('heading', { name: /Order Service/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /Inventory Service/i })).toBeInTheDocument();
  });

  it('shows no-results state with clear button when domain filter matches nothing', () => {
    renderWithProviders(
      <ServiceBrowseSurface graph={MOCK_GRAPH} {...DEFAULT_HANDLERS} />,
      { routerProps: { initialEntries: ['/?domain=zzz-nope'] } },
    );
    // i18next devolve key path → 'serviceCatalog.browse.noResults.title' → regex parcial
    expect(screen.getByText(/noResults\.title/i)).toBeInTheDocument();
    // CatalogFacetBar + EmptyState action renderizam ambos o botão clearAll
    // (quando hasActiveFilters=true, o FacetBar mostra o seu próprio ghost-button)
    expect(screen.getAllByRole('button', { name: /clearAll/i }).length).toBeGreaterThanOrEqual(1);
  });

  it('shows EmptyState with onboarding message when graph has no services', () => {
    renderWithProviders(
      <ServiceBrowseSurface graph={EMPTY_GRAPH} {...DEFAULT_HANDLERS} />,
    );
    // i18next devolve key path → 'serviceCatalog.browse.empty.title' → regex parcial
    expect(screen.getByText(/browse\.empty\.title/i)).toBeInTheDocument();
  });

  it('shows API rows when view mode is apis', () => {
    renderWithProviders(
      <ServiceBrowseSurface graph={MOCK_GRAPH} {...DEFAULT_HANDLERS} />,
      { routerProps: { initialEntries: ['/?view=apis'] } },
    );
    // ApiResultRow renderiza o nome da API como span de texto
    expect(screen.getByText('Orders API')).toBeInTheDocument();
    expect(screen.getByText('Inventory API')).toBeInTheDocument();
  });

});
