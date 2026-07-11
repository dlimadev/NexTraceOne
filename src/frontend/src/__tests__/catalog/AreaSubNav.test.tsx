import { describe, it, expect } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';
import { AreaSubNav, type AreaSubNavItem } from '../../features/catalog/components/AreaSubNav';

const items: AreaSubNavItem[] = [
  { labelKey: 'catalogAreaNav.catalog', to: '/services', end: true },
  { labelKey: 'catalogAreaNav.graph', to: '/services/graph' },
  { labelKey: 'catalogAreaNav.discovery', to: '/services/discovery' },
];

describe('AreaSubNav', () => {
  it('renders all tabs with translated labels', () => {
    renderWithProviders(<AreaSubNav items={items} ariaLabelKey="catalogAreaNav.ariaLabel" />, {
      routerProps: { initialEntries: ['/services'] },
    });
    expect(screen.getByText('Catalog')).toBeInTheDocument();
    expect(screen.getByText('Graph')).toBeInTheDocument();
    expect(screen.getByText('Discovery')).toBeInTheDocument();
  });

  it('marks only the exact list root active on /services (end)', () => {
    renderWithProviders(<AreaSubNav items={items} ariaLabelKey="catalogAreaNav.ariaLabel" />, {
      routerProps: { initialEntries: ['/services'] },
    });
    expect(screen.getByText('Catalog')).toHaveAttribute('data-active', 'true');
    expect(screen.getByText('Graph')).toHaveAttribute('data-active', 'false');
  });

  it('does not activate the list root on a sub-path, activates the sub-tab', () => {
    renderWithProviders(<AreaSubNav items={items} ariaLabelKey="catalogAreaNav.ariaLabel" />, {
      routerProps: { initialEntries: ['/services/discovery'] },
    });
    expect(screen.getByText('Catalog')).toHaveAttribute('data-active', 'false');
    expect(screen.getByText('Discovery')).toHaveAttribute('data-active', 'true');
  });
});
