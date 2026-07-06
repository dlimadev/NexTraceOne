/**
 * Testes TDD para useContractBrowseState.
 * Verifica leitura e escrita de filtros, viewMode, density e sort no URL.
 */
import * as React from 'react';
import { renderHook, act } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { useContractBrowseState } from '../../features/contracts/catalog/browse/useContractBrowseState';

const wrapper = ({ children }: { children: React.ReactNode }) => <MemoryRouter>{children}</MemoryRouter>;

it('lê e escreve filtros de array no URL (lifecycles)', () => {
  const { result } = renderHook(() => useContractBrowseState(), { wrapper });
  act(() => result.current.setFilter('lifecycles', ['stable', 'deprecated']));
  expect(result.current.filters.lifecycles).toEqual(['stable', 'deprecated']);
});

describe('defaults', () => {
  it('viewMode default é table, density comfortable, sort relevance', () => {
    const { result } = renderHook(() => useContractBrowseState(), { wrapper });
    expect(result.current.viewMode).toBe('table');
    expect(result.current.density).toBe('comfortable');
    expect(result.current.sort).toBe('relevance');
  });
});

describe('setViewMode', () => {
  it('actualiza viewMode para cards', () => {
    const { result } = renderHook(() => useContractBrowseState(), { wrapper });
    act(() => result.current.setViewMode('cards'));
    expect(result.current.viewMode).toBe('cards');
  });
});

describe('clearAll', () => {
  it('limpa todos os filtros mas mantém view e density', () => {
    const { result } = renderHook(() => useContractBrowseState(), { wrapper });

    act(() => result.current.setViewMode('cards'));
    act(() => result.current.setDensity('compact'));
    act(() => result.current.setFilter('q', 'payment-api'));
    act(() => result.current.setFilter('lifecycles', ['stable']));
    act(() => result.current.setFilter('domains', ['payments', 'orders']));

    act(() => result.current.clearAll());

    expect(result.current.filters.q).toBe('');
    expect(result.current.filters.lifecycles).toEqual([]);
    expect(result.current.filters.domains).toEqual([]);
    expect(result.current.viewMode).toBe('cards');
    expect(result.current.density).toBe('compact');
  });

  it('sort volta a relevance após clearAll', () => {
    const { result } = renderHook(() => useContractBrowseState(), { wrapper });
    act(() => result.current.setSort('name'));
    expect(result.current.sort).toBe('name');

    act(() => result.current.clearAll());
    // sort não é preservado pelo clearAll — volta ao default
    expect(result.current.sort).toBe('relevance');
  });
});

describe('viewMode whitelist guard', () => {
  it('valor inválido em ?view=garbage resulta em viewMode === table', () => {
    const invalidWrapper = ({ children }: { children: React.ReactNode }) => (
      <MemoryRouter initialEntries={['/?view=garbage']}>{children}</MemoryRouter>
    );
    const { result } = renderHook(() => useContractBrowseState(), { wrapper: invalidWrapper });
    expect(result.current.viewMode).toBe('table');
  });
});

describe('setFilter — preservação de outros parâmetros', () => {
  it('setFilter preserva os outros parâmetros ao actualizar um único filtro', () => {
    const { result } = renderHook(() => useContractBrowseState(), { wrapper });

    act(() => result.current.setFilter('q', 'checkout'));
    act(() => result.current.setFilter('serviceTypes', ['rest', 'grpc']));

    expect(result.current.filters.q).toBe('checkout');
    expect(result.current.filters.serviceTypes).toEqual(['rest', 'grpc']);
  });

  it('apaga o param quando array fica vazio', () => {
    const { result } = renderHook(() => useContractBrowseState(), { wrapper });

    act(() => result.current.setFilter('teams', ['platform']));
    expect(result.current.filters.teams).toEqual(['platform']);

    act(() => result.current.setFilter('teams', []));
    expect(result.current.filters.teams).toEqual([]);
  });
});
