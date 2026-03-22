import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({
    requiresTenantSelection: true,
    availableTenants: [
      { tenantId: 't1', name: 'Acme Corp', slug: 'acme', role: 'Admin', status: 'Active' },
      { tenantId: 't2', name: 'Beta Inc', slug: 'beta', role: 'Engineer', status: 'Active' },
    ],
    selectTenant: vi.fn(),
  }),
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}));

import { TenantSelectionPage } from '../../features/identity-access/pages/TenantSelectionPage';

describe('TenantSelectionPage', () => {
  beforeEach(() => { vi.clearAllMocks(); });

  it('renders tenant selection heading', () => {
    render(<MemoryRouter><TenantSelectionPage /></MemoryRouter>);
    expect(screen.getByText('tenantSelection.title')).toBeInTheDocument();
  });

  it('renders available tenants', () => {
    render(<MemoryRouter><TenantSelectionPage /></MemoryRouter>);
    expect(screen.getByText('Acme Corp')).toBeInTheDocument();
    expect(screen.getByText('Beta Inc')).toBeInTheDocument();
  });

  it('shows role for each tenant', () => {
    render(<MemoryRouter><TenantSelectionPage /></MemoryRouter>);
    expect(screen.getByText('Admin')).toBeInTheDocument();
    expect(screen.getByText('Engineer')).toBeInTheDocument();
  });
});
