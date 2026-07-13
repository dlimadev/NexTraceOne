import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { renderWithProviders } from '../test-utils';
import { ContractCreateForm } from '../../features/contracts/create/ContractCreateForm';

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (k: string, d?: string) => d ?? k }),
  I18nextProvider: ({ children }: { children: React.ReactNode }) => children,
}));

vi.mock('../../features/catalog/api/serviceCatalog', () => ({
  serviceCatalogApi: { listServices: vi.fn().mockResolvedValue({ items: [] }) },
}));

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: vi.fn(() => ({
    user: { id: 'user-1', email: 'test@example.com', fullName: 'Test User' },
    isAuthenticated: true,
  })),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

describe('ContractCreateForm', () => {
  it('renders the create tabs and a cancel action without page chrome', () => {
    renderWithProviders(
      <ContractCreateForm prefilledServiceId="svc-1" onCreated={() => {}} onCancel={() => {}} hideIdentityCard />,
    );
    // A tab de tipo/modo aparece (prefilledServiceId salta para 'typeMode')
    expect(screen.getByText('Novo contrato')).toBeInTheDocument();
    expect(screen.getByText('Cancelar')).toBeInTheDocument();
  });
});
