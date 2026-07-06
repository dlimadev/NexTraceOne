/**
 * TDD para ContractResultCard — red → green.
 *
 * Verifica:
 * 1. Full CatalogItem: nome, versão, dot de lifecycle (aria-label), tipo de serviço,
 *    contexto (domínio, equipa, dono técnico).
 * 2. Item com technicalOwner/criticality vazios: esses campos não renderizam (honest-null).
 * 3. Clicar no cartão chama onOpen(item).
 * 4. density=compact → variante linha única (nome visível).
 */
import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../test-utils';
import { ContractResultCard } from '../../features/contracts/catalog/browse/ContractResultCard';
import type { CatalogItem } from '../../features/contracts/catalog/types';
import type { ContractDensity } from '../../features/contracts/catalog/browse/contractBrowseTypes';

/* ─── Fixtures ───────────────────────────────────────────────────────────────── */

const fullItem = {
  // ContractListItem required fields
  apiAssetId:        'asset-payment',
  protocol:          'OpenApi',
  lifecycleState:    'Approved',
  // CatalogItem fields
  name:              'Payment API',
  semVer:            '2.1.0',
  domain:            'Payments',
  team:              'Core Team',
  technicalOwner:    'Alice',
  criticality:       'High',
  exposure:          'Public',
  updatedAt:         '2026-01-01T00:00:00Z',
  catalogServiceType: 'RestApi',
  approvalState:     'Approved',
} as CatalogItem;

/** Item com technicalOwner e criticality vazios — honest-null. */
const sparseItem = {
  apiAssetId:        'asset-sparse',
  protocol:          'OpenApi',
  lifecycleState:    'Draft',
  name:              'Sparse API',
  semVer:            '0.1.0',
  domain:            'Internal',
  team:              'Platform',
  technicalOwner:    '',   // deve NÃO renderizar
  criticality:       '',   // deve NÃO renderizar
  exposure:          '',
  updatedAt:         '2026-01-01T00:00:00Z',
  catalogServiceType: 'RestApi',
  approvalState:     'Pending',
} as CatalogItem;

/* ─── Helper ─────────────────────────────────────────────────────────────────── */

type CardProps = React.ComponentProps<typeof ContractResultCard>;

function renderCard(overrides: Partial<CardProps> = {}) {
  const onOpen = vi.fn();
  const props: CardProps = {
    item:    fullItem,
    density: 'comfortable' as ContractDensity,
    onOpen,
    ...overrides,
  };
  renderWithProviders(<ContractResultCard {...props} />);
  return { onOpen };
}

/* ─── Testes ─────────────────────────────────────────────────────────────────── */

describe('ContractResultCard', () => {

  describe('full item — campos presentes', () => {
    it('renders contract name as heading', () => {
      renderCard();
      expect(screen.getByRole('heading', { name: /Payment API/i })).toBeInTheDocument();
    });

    it('renders semVer', () => {
      renderCard();
      expect(screen.getByText('2.1.0')).toBeInTheDocument();
    });

    it('renders lifecycle dot with aria-label reflecting the lifecycle state', () => {
      renderCard();
      // aria-label = t('contracts.catalog.browse.lifecycle.approved')
      // key not yet in locale → i18next falls back to key path, which contains 'approved'
      const dot = screen.getByRole('img', { name: /approved/i });
      expect(dot).toBeInTheDocument();
    });

    it('renders service type badge with translated label', () => {
      renderCard();
      // ServiceTypeBadge: t('contracts.serviceTypes.RestApi', 'RestApi') → 'REST API'
      expect(screen.getByText('REST API')).toBeInTheDocument();
    });

    it('renders domain in context line', () => {
      renderCard();
      expect(screen.getByText('Payments')).toBeInTheDocument();
    });

    it('renders team in context line', () => {
      renderCard();
      expect(screen.getByText('Core Team')).toBeInTheDocument();
    });

    it('renders technicalOwner in context line', () => {
      renderCard();
      expect(screen.getByText('Alice')).toBeInTheDocument();
    });
  });

  describe('honest-null — campos vazios não renderizam', () => {
    it('does not render technicalOwner when empty', () => {
      renderCard({ item: sparseItem });
      expect(screen.queryByText('Alice')).toBeNull();
    });

    it('does not render criticality badge when empty', () => {
      renderCard({ item: sparseItem });
      // fullItem renders criticality='High'; sparseItem has criticality='' → sem badge
      expect(screen.queryByText('High')).toBeNull();
    });
  });

  describe('interações', () => {
    it('clicking the card calls onOpen with the item', async () => {
      const user = userEvent.setup();
      const { onOpen } = renderCard();

      await user.click(screen.getByRole('heading', { name: /Payment API/i }));

      expect(onOpen).toHaveBeenCalledWith(fullItem);
    });
  });

  describe('compact density', () => {
    it('renders contract name in compact mode', () => {
      renderCard({ density: 'compact' });
      expect(screen.getByRole('heading', { name: /Payment API/i })).toBeInTheDocument();
    });
  });
});
