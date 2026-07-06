/**
 * Testes TDD para ContractFacetBar (search + facetas + view/sort/density).
 *
 * Verifica:
 *  1. Clicar num chip de lifecycle chama onSetFilter('lifecycles', ['Stable'])
 *  2. Alterar a pesquisa chama onSetFilter('q', <valor>)
 *  3. Comutar para "Cartões" chama onViewMode('cards')
 *  4. "clear all" não aparece quando não há filtros activos
 *  5. "clear all" aparece quando há um filtro activo
 */
import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../test-utils';
import { ContractFacetBar } from '../../features/contracts/catalog/browse/ContractFacetBar';
import type {
  ContractFacetGroups,
  ContractBrowseFilters,
  ContractViewMode,
  ContractSortKey,
  ContractDensity,
} from '../../features/contracts/catalog/browse/contractBrowseTypes';

/* ─── Fixtures ───────────────────────────────────────────────────────────────── */

const mockFacets: ContractFacetGroups = {
  serviceTypes:  [{ value: 'REST',     label: 'REST',     count: 5 }],
  lifecycles:    [{ value: 'Stable',   label: 'Stable',   count: 4 }],
  domains:       [{ value: 'payments', label: 'payments', count: 1 }],
  teams:         [],
  criticalities: [{ value: 'High',     label: 'High',     count: 2 }],
  exposures:     [{ value: 'Public',   label: 'Public',   count: 3 }],
  approvals:     [],
};

const defaultFilters: ContractBrowseFilters = {
  q:             '',
  serviceTypes:  [],
  lifecycles:    [],
  domains:       [],
  teams:         [],
  criticalities: [],
  exposures:     [],
  approvals:     [],
};

/* ─── Helper ─────────────────────────────────────────────────────────────────── */

type Props = React.ComponentProps<typeof ContractFacetBar>;

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
    viewMode:    'table' as ContractViewMode,
    sort:        'relevance' as ContractSortKey,
    density:     'comfortable' as ContractDensity,
    resultCount: 5,
    ...spies,
    ...overrides,
  };
  renderWithProviders(<ContractFacetBar {...props} />);
  return spies;
}

/* ─── Testes ─────────────────────────────────────────────────────────────────── */

describe('ContractFacetBar', () => {
  it('clicar no chip de lifecycle chama onSetFilter com o lifecycle adicionado', async () => {
    const user = userEvent.setup();
    const { onSetFilter } = renderBar();

    const chip = screen.getByRole('button', { name: /stable/i });
    await user.click(chip);

    expect(onSetFilter).toHaveBeenCalledWith('lifecycles', ['Stable']);
  });

  it('alterar a pesquisa chama onSetFilter com a chave q', () => {
    const { onSetFilter } = renderBar();

    const input = screen.getByRole('searchbox');
    fireEvent.change(input, { target: { value: 'checkout' } });

    expect(onSetFilter).toHaveBeenCalledWith('q', 'checkout');
  });

  it('comutar para a tab Cartões chama onViewMode("cards")', async () => {
    const user = userEvent.setup();
    const { onViewMode } = renderBar();

    // O label da tab é a chave i18n (ainda sem tradução) → contém "cards"
    const cardsTab = screen.getByRole('tab', { name: /cards/i });
    await user.click(cardsTab);

    expect(onViewMode).toHaveBeenCalledWith('cards');
  });

  it('não mostra "clear all" quando não há filtros activos', () => {
    renderBar();
    expect(screen.queryByRole('button', { name: /clearAll|clear all/i })).toBeNull();
  });

  it('mostra "clear all" quando há um filtro activo', () => {
    renderBar({
      filters: { ...defaultFilters, q: 'payments' },
    });
    expect(screen.getByRole('button', { name: /clearAll|clear all|limpar/i })).toBeInTheDocument();
  });
});
