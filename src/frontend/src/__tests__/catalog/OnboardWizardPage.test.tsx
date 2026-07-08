import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { OnboardWizardPage } from '../../features/catalog/onboard/OnboardWizardPage';

// ServiceIdentityForm usa t('onboard.identity.name') sem default → mapear a chave
// para que getByLabelText(/service name/i) resolva.
const translations: Record<string, string> = {
  'onboard.identity.name': 'Service name',
};
vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (k: string, d?: string) => translations[k] ?? d ?? k }),
}));
vi.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({ user: { email: 'tester@acme.com' } }),
}));
vi.mock('../../features/catalog/api/serviceCatalog', () => ({
  serviceCatalogApi: { listServices: vi.fn(() => Promise.resolve({ items: [] })) },
}));
vi.mock('../../features/catalog/api', () => ({
  serviceCatalogApi: {
    registerService: vi.fn(() => Promise.resolve({ id: 'svc-1' })),
    createServiceInterface: vi.fn(() => Promise.resolve({})),
    listServices: vi.fn(() => Promise.resolve({ items: [] })),
  },
}));

describe('OnboardWizardPage', () => {
  it('renders step 1 (identity) with the service name field', () => {
    const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    render(
      <QueryClientProvider client={qc}>
        <MemoryRouter><OnboardWizardPage /></MemoryRouter>
      </QueryClientProvider>,
    );
    expect(screen.getByLabelText(/service name/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /next/i })).toBeDisabled();
  });
});
