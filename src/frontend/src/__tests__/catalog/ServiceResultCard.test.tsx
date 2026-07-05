/**
 * TDD para ServiceResultCard — red → green.
 *
 * Verifica:
 * 1. Full ServiceVM: nome, descrição, dot de lifecycle (aria-label), exposição,
 *    contexto (domínio, equipa, dono), chips de API.
 * 2. Sparse ServiceVM (description/health/owner ausentes): esses campos não renderizam.
 * 3. Clicar no corpo do cartão chama onOpenService(id).
 * 4. Clicar num chip chama onOpenApi(apiId) e NÃO chama onOpenService.
 */
import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../test-utils';
import { ServiceResultCard } from '../../features/catalog/browse/ServiceResultCard';
import type { ServiceVM, ApiVM } from '../../features/catalog/browse/catalogTypes';

/* ─── Fixtures ───────────────────────────────────────────────────────────────── */

const api1: ApiVM = {
  id: 'api-1',
  name: 'Orders API',
  routePattern: '/v1/orders',
  exposure: 'Public',
  version: 'v1',
  hasContract: true,
};

const api2: ApiVM = {
  id: 'api-2',
  name: 'Events API',
  routePattern: '/v1/events',
  exposure: 'Internal',
  version: 'v2',
  hasContract: false,
};

const fullService: ServiceVM = {
  id: 'svc-1',
  name: 'Order Service',
  description: 'Handles all order operations',
  domain: 'Payments',
  team: 'Order Team',
  owner: 'Alice',
  lifecycle: 'Stable',
  exposure: 'Public',
  health: 'Ok',
  apis: [api1, api2],
  contractCount: 1,
};

const sparseService: ServiceVM = {
  id: 'svc-2',
  name: 'Sparse Service',
  lifecycle: 'Beta',
  apis: [],
  contractCount: 0,
};

/* ─── Helper ─────────────────────────────────────────────────────────────────── */

type CardProps = React.ComponentProps<typeof ServiceResultCard>;

function renderCard(overrides: Partial<CardProps> = {}) {
  const spies = {
    onOpenService:  vi.fn(),
    onOpenApi:      vi.fn(),
    onViewContract: vi.fn(),
  };
  const props: CardProps = {
    service:  fullService,
    density:  'comfortable',
    ...spies,
    ...overrides,
  };
  renderWithProviders(<ServiceResultCard {...props} />);
  return spies;
}

/* ─── Testes ─────────────────────────────────────────────────────────────────── */

describe('ServiceResultCard', () => {

  describe('full service VM — campos presentes', () => {
    it('renders service name as heading', () => {
      renderCard();
      expect(screen.getByRole('heading', { name: /Order Service/i })).toBeInTheDocument();
    });

    it('renders description', () => {
      renderCard();
      expect(screen.getByText(/Handles all order operations/i)).toBeInTheDocument();
    });

    it('renders lifecycle dot with accessible aria-label', () => {
      renderCard();
      // t('serviceCatalog.browse.lifecycle.stable') → 'Stable'
      expect(screen.getByRole('img', { name: /^Stable$/i })).toBeInTheDocument();
    });

    it('renders exposure badge', () => {
      renderCard();
      // t('serviceCatalog.browse.exposure.public') → 'Public'
      expect(screen.getByText(/^Public$/i)).toBeInTheDocument();
    });

    it('renders domain in context line', () => {
      renderCard();
      expect(screen.getByText('Payments')).toBeInTheDocument();
    });

    it('renders team in context line', () => {
      renderCard();
      expect(screen.getByText('Order Team')).toBeInTheDocument();
    });

    it('renders owner in context line', () => {
      renderCard();
      expect(screen.getByText('Alice')).toBeInTheDocument();
    });

    it('renders an API chip for each API', () => {
      renderCard();
      expect(screen.getByRole('button', { name: /Orders API/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /Events API/i })).toBeInTheDocument();
    });
  });

  describe('honest-null — sparse service VM (campos opcionais ausentes)', () => {
    it('does not render description when absent', () => {
      renderCard({ service: sparseService });
      expect(screen.queryByText(/Handles all order operations/i)).toBeNull();
    });

    it('does not render owner when absent', () => {
      renderCard({ service: sparseService });
      expect(screen.queryByText('Alice')).toBeNull();
    });

    it('does not render health indicator when absent', () => {
      renderCard({ service: sparseService });
      // fullService tem health: 'Ok' — sparseService não tem health
      expect(screen.queryByText('Ok')).toBeNull();
    });

    it('does not render exposure badge when absent', () => {
      renderCard({ service: sparseService });
      // sparseService não tem exposure → sem badge com texto 'exposure.'
      expect(screen.queryByText(/exposure\./i)).toBeNull();
    });
  });

  describe('interações', () => {
    it('clicking card heading calls onOpenService with the service id', async () => {
      const user = userEvent.setup();
      const { onOpenService } = renderCard();

      await user.click(screen.getByRole('heading', { name: /Order Service/i }));

      expect(onOpenService).toHaveBeenCalledWith('svc-1');
    });

    it('clicking API chip calls onOpenApi and does NOT call onOpenService', async () => {
      const user = userEvent.setup();
      const { onOpenApi, onOpenService } = renderCard();

      await user.click(screen.getByRole('button', { name: /Orders API/i }));

      expect(onOpenApi).toHaveBeenCalledWith('api-1');
      expect(onOpenService).not.toHaveBeenCalled();
    });
  });
});
