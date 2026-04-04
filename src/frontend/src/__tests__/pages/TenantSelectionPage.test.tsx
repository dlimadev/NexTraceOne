import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({
    requiresTenantSelection: true,
    availableTenants: [
      { id: 't1', name: 'Acme Corp', slug: 'acme', roleName: 'Admin', isActive: true },
      { id: 't2', name: 'Beta Inc', slug: 'beta', roleName: 'Engineer', isActive: true },
    ],
    selectTenant: vi.fn(),
  }),
}));

import { TenantSelectionPage } from '../../features/identity-access/pages/TenantSelectionPage';

describe('TenantSelectionPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders tenant selection heading', () => {
    renderWithProviders(<TenantSelectionPage />);
    expect(screen.getByText('Select Organization')).toBeInTheDocument();
  });

  it('renders available tenants', () => {
    renderWithProviders(<TenantSelectionPage />);
    expect(screen.getByText('Acme Corp')).toBeInTheDocument();
    expect(screen.getByText('Beta Inc')).toBeInTheDocument();
  });

  it('shows role for each tenant', () => {
    renderWithProviders(<TenantSelectionPage />);
    expect(screen.getByText('Admin')).toBeInTheDocument();
    expect(screen.getByText('Engineer')).toBeInTheDocument();
  });
});
