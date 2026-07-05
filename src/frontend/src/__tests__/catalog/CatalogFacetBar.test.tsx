/**
 * Testes TDD para CatalogFacetBar (search + facetas + view/sort/density).
 *
 * Verifica:
 *  1. Clicar num chip de domínio chama onSetFilter('domains', ['payments'])
 *  2. Alterar a pesquisa chama onSetFilter('q', <valor>)
 *  3. Comutar para "Ver como: APIs" chama onViewMode('apis')
 */
import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../test-utils';
import { CatalogFacetBar } from '../../features/catalog/browse/CatalogFacetBar';
import type {
  FacetGroups,
  CatalogFilters,
  ResultViewMode,
  SortKey,
  Density,
} from '../../features/catalog/browse/catalogTypes';

/* ─── Fixtures ───────────────────────────────────────────────────────────────── */

const mockFacets: FacetGroups = {
  domains:    [{ value: 'payments',  label: 'payments',  count: 1 }],
  protocols:  [{ value: 'REST',      label: 'REST',      count: 2 }],
  exposures:  [{ value: 'Public',    label: 'Public',    count: 3 }],
  lifecycles: [{ value: 'Stable',    label: 'Stable',    count: 4 }],
  teams:      [],
};

const defaultFilters: CatalogFilters = {
  q:           '',
  domains:     [],
  protocols:   [],
  exposures:   [],
  lifecycles:  [],
  hasContract: null,
  teams:       [],
};

/* ─── Helper ─────────────────────────────────────────────────────────────────── */

type Props = React.ComponentProps<typeof CatalogFacetBar>;

function renderBar(overrides: Partial<Props> = {}) {
  const spies = {
    onSetFilter: vi.fn(),
    onViewMode:  vi.fn(),
    onSort:      vi.fn(),
    onDensity:   vi.fn(),
    onClearAll:  vi.fn(),
  };
  const props: Props = {
    facets:      mockFacets,
    filters:     defaultFilters,
    viewMode:    'services' as ResultViewMode,
    sort:        'relevance' as SortKey,
    density:     'comfortable' as Density,
    resultCount: 5,
    ...spies,
    ...overrides,
  };
  renderWithProviders(<CatalogFacetBar {...props} />);
  return spies;
}

/* ─── Testes ─────────────────────────────────────────────────────────────────── */

describe('CatalogFacetBar', () => {
  it('clicar no chip de domínio chama onSetFilter com o domínio adicionado', async () => {
    const user = userEvent.setup();
    const { onSetFilter } = renderBar();

    const chip = screen.getByRole('button', { name: /payments/i });
    await user.click(chip);

    expect(onSetFilter).toHaveBeenCalledWith('domains', ['payments']);
  });

  it('alterar a pesquisa chama onSetFilter com a chave q', () => {
    const { onSetFilter } = renderBar();

    const input = screen.getByRole('searchbox');
    fireEvent.change(input, { target: { value: 'checkout' } });

    expect(onSetFilter).toHaveBeenCalledWith('q', 'checkout');
  });

  it('comutar para a tab APIs chama onViewMode("apis")', async () => {
    const user = userEvent.setup();
    const { onViewMode } = renderBar();

    // O label da tab é a chave i18n (ainda sem tradução) → contém "apis"
    const apisTab = screen.getByRole('tab', { name: /apis/i });
    await user.click(apisTab);

    expect(onViewMode).toHaveBeenCalledWith('apis');
  });

  it('não mostra "clear all" quando não há filtros activos', () => {
    renderBar();
    // A chave i18n retorna o key path — contém "clearAll"
    expect(screen.queryByRole('button', { name: /clearAll|clear all/i })).toBeNull();
  });

  it('mostra "clear all" quando há um filtro activo', () => {
    renderBar({
      filters: { ...defaultFilters, q: 'payments' },
    });
    expect(screen.getByRole('button', { name: /clearAll|clear all|limpar/i })).toBeInTheDocument();
  });
});
