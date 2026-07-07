/**
 * TDD para ContractBrowseSurface — red → green.
 *
 * Verifica 4 cenários:
 * (a) Modo tabela (default sem URL params): renderTable é chamado e o marcador
 *     aparece; nomes dos itens NÃO aparecem como headings (sem cartões).
 * (b) Modo cartões (?view=cards): os nomes dos itens aparecem como headings h3.
 * (c) Sem resultados (?lifecycle=zzz-nope): estado "sem resultados" e pelo menos
 *     um botão de limpar filtros.
 * (d) items vazio: EmptyState com mensagem de onboarding.
 *
 * Configuração: usa renderWithProviders com routerProps.initialEntries para
 * simular parâmetros de URL sem interações de utilizador.
 * i18n: chaves contracts.catalog.browse.* existem no locale en.json; as
 * assertions usam as strings traduzidas.
 */
import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';
import { ContractBrowseSurface } from '../../features/contracts/catalog/browse/ContractBrowseSurface';
import type { CatalogItem } from '../../features/contracts/catalog/types';

/* ─── Fixtures ───────────────────────────────────────────────────────────────── */

function makeItem(id: string, name: string): CatalogItem {
  return {
    apiAssetId: id,
    protocol: 'OpenApi',
    lifecycleState: 'Approved',
    name,
    semVer: '1.0.0',
    domain: 'payments',
    team: 'Test Team',
    technicalOwner: 'Owner',
    criticality: 'High',
    exposure: 'Public',
    updatedAt: '2026-01-01T00:00:00Z',
    catalogServiceType: 'RestApi',
    approvalState: 'Approved',
  } as CatalogItem;
}

const ITEMS: CatalogItem[] = [
  makeItem('item-1', 'Payment API'),
  makeItem('item-2', 'Order API'),
];

/* ─── Testes ─────────────────────────────────────────────────────────────────── */

describe('ContractBrowseSurface', () => {
  it('(a) tabela: renderTable é chamado e o marcador aparece; sem headings de cartões', () => {
    const renderTable = vi.fn(() => (
      <div data-testid="table-marker">Table content</div>
    ));

    renderWithProviders(
      <ContractBrowseSurface items={ITEMS} onOpen={vi.fn()} renderTable={renderTable} />,
    );

    // Marcador da tabela deve estar visível
    expect(screen.getByTestId('table-marker')).toBeInTheDocument();
    // renderTable deve ter sido chamado (com os itens filtrados)
    expect(renderTable).toHaveBeenCalled();
    // Cartões NÃO devem aparecer no modo tabela
    expect(screen.queryByRole('heading', { name: /Payment API/i })).toBeNull();
    // O Select de ordenação fica oculto no modo tabela (cabeçalhos ordenam).
    expect(screen.queryByRole('combobox')).toBeNull();
  });

  it('(b) cartões: os nomes dos itens aparecem como headings h3', () => {
    const renderTable = vi.fn(() => null);

    renderWithProviders(
      <ContractBrowseSurface items={ITEMS} onOpen={vi.fn()} renderTable={renderTable} />,
      { routerProps: { initialEntries: ['/?view=cards'] } },
    );

    expect(screen.getByRole('heading', { name: /Payment API/i })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /Order API/i })).toBeInTheDocument();
    // O Select de ordenação está presente no modo cartões.
    expect(screen.getByRole('combobox')).toBeInTheDocument();
  });

  it('(c) sem resultados: mostra estado noResults e pelo menos um botão de limpar', () => {
    const renderTable = vi.fn(() => null);

    renderWithProviders(
      <ContractBrowseSurface items={ITEMS} onOpen={vi.fn()} renderTable={renderTable} />,
      { routerProps: { initialEntries: ['/?lifecycle=zzz-nope'] } },
    );

    // Título do estado sem resultados (da chave i18n traduzida)
    expect(screen.getByText(/No contracts match/i)).toBeInTheDocument();
    // Pelo menos um botão de limpar filtros (pode ser do FacetBar + EmptyState action)
    expect(screen.getAllByRole('button', { name: /clear all/i }).length).toBeGreaterThanOrEqual(1);
  });

  it('(d) items vazio: EmptyState com mensagem de onboarding', () => {
    const renderTable = vi.fn(() => null);

    renderWithProviders(
      <ContractBrowseSurface items={[]} onOpen={vi.fn()} renderTable={renderTable} />,
    );

    expect(screen.getByText(/No contracts registered yet/i)).toBeInTheDocument();
  });
});
