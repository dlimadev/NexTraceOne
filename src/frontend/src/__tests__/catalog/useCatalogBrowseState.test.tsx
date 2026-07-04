/**
 * Testes TDD para useCatalogBrowseState.
 * Verifica leitura e escrita de filtros, viewMode, density e sort no URL.
 */
import * as React from 'react';
import { renderHook, act } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { useCatalogBrowseState } from '../../features/catalog/browse/useCatalogBrowseState';

const wrapper = ({ children }: { children: React.ReactNode }) => <MemoryRouter>{children}</MemoryRouter>;

it('lê e escreve filtros no URL', () => {
  const { result } = renderHook(() => useCatalogBrowseState(), { wrapper });
  act(() => result.current.setFilter('domains', ['payments']));
  expect(result.current.filters.domains).toEqual(['payments']);
  act(() => result.current.setViewMode('apis'));
  expect(result.current.viewMode).toBe('apis');
  act(() => result.current.clearAll());
  expect(result.current.filters.domains).toEqual([]);
  expect(result.current.filters.q).toBe('');
});

describe('defaults', () => {
  it('viewMode default é services, density comfortable, sort relevance', () => {
    const { result } = renderHook(() => useCatalogBrowseState(), { wrapper });
    expect(result.current.viewMode).toBe('services');
    expect(result.current.density).toBe('comfortable');
    expect(result.current.sort).toBe('relevance');
  });
});

describe('hasContract round-trip', () => {
  it('persiste hasContract como "1"/"0"/ausente e recupera como true/false/null', () => {
    const { result } = renderHook(() => useCatalogBrowseState(), { wrapper });

    act(() => result.current.setFilter('hasContract', true));
    expect(result.current.filters.hasContract).toBe(true);

    act(() => result.current.setFilter('hasContract', false));
    expect(result.current.filters.hasContract).toBe(false);

    act(() => result.current.setFilter('hasContract', null));
    expect(result.current.filters.hasContract).toBeNull();
  });
});

describe('clearAll preserva view e density', () => {
  it('limpa filtros mas mantém view e density após clearAll', () => {
    const { result } = renderHook(() => useCatalogBrowseState(), { wrapper });

    act(() => result.current.setViewMode('apis'));
    act(() => result.current.setDensity('compact'));
    act(() => result.current.setFilter('q', 'my-service'));
    act(() => result.current.setFilter('domains', ['payments', 'orders']));

    act(() => result.current.clearAll());

    expect(result.current.filters.q).toBe('');
    expect(result.current.filters.domains).toEqual([]);
    expect(result.current.viewMode).toBe('apis');
    expect(result.current.density).toBe('compact');
  });
});

describe('param preservation across setFilter calls', () => {
  it('setFilter preserva os outros parâmetros ao actualizar um único filtro', () => {
    const { result } = renderHook(() => useCatalogBrowseState(), { wrapper });

    act(() => result.current.setFilter('q', 'checkout'));
    act(() => result.current.setFilter('domains', ['payments']));

    expect(result.current.filters.q).toBe('checkout');
    expect(result.current.filters.domains).toEqual(['payments']);
  });
});
