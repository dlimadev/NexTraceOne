// src/frontend/src/__tests__/catalog/ServiceRegistrationOverlay.test.tsx
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ServiceRegistrationOverlay } from '../../features/catalog/components/ServiceRegistrationOverlay';

vi.mock('../../features/catalog/api', () => ({
  serviceCatalogApi: {
    registerService: vi.fn().mockResolvedValue({ id: 'new-service-id' }),
  },
}));

import { serviceCatalogApi } from '../../features/catalog/api';

function renderOverlay(onClose = vi.fn(), onSuccess = vi.fn()) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <ServiceRegistrationOverlay onClose={onClose} onSuccess={onSuccess} />
    </QueryClientProvider>
  );
}

describe('ServiceRegistrationOverlay', () => {
  beforeEach(() => {
    vi.mocked(serviceCatalogApi.registerService).mockResolvedValue({ id: 'new-service-id' });
  });

  it('renders step 1 identity fields on mount', () => {
    renderOverlay();
    expect(screen.getByPlaceholderText(/payment-service/i)).toBeInTheDocument();
  });

  it('blocks advance when name is empty', async () => {
    const user = userEvent.setup();
    renderOverlay();
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    expect(screen.getByText(/name.*required|nome.*obrigatório/i)).toBeInTheDocument();
  });

  it('blocks advance when domain is empty', async () => {
    const user = userEvent.setup();
    renderOverlay();
    await user.type(screen.getByPlaceholderText(/payment-service/i), 'my-svc');
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    expect(screen.getByText(/domain.*required|domínio.*obrigatório/i)).toBeInTheDocument();
  });

  it('advances to step 2 after valid step 1', async () => {
    const user = userEvent.setup();
    renderOverlay();
    await user.type(screen.getByPlaceholderText(/payment-service/i), 'svc');
    await user.type(screen.getByPlaceholderText(/payments.*identity|pagamentos/i), 'finance');
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    await waitFor(() => {
      expect(screen.getAllByRole('option').length).toBeGreaterThan(5);
    });
  });

  it('calls registerService and onSuccess when submitted', async () => {
    const user = userEvent.setup();
    const onSuccess = vi.fn();
    renderOverlay(vi.fn(), onSuccess);

    // Step 1
    await user.type(screen.getByPlaceholderText(/payment-service/i), 'my-svc');
    await user.type(screen.getByPlaceholderText(/payments.*identity|pagamentos/i), 'finance');
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    // Step 2 — advance
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    // Step 3 — fill team
    await user.type(screen.getByPlaceholderText(/platform-team/i), 'finance-team');
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    // Step 4 — advance
    await user.click(screen.getByRole('button', { name: /next|avançar/i }));
    // Step 5 — submit
    await user.click(screen.getByRole('button', { name: /submit|salvar|guardar/i }));

    await waitFor(() => {
      expect(serviceCatalogApi.registerService).toHaveBeenCalledWith(
        expect.objectContaining({ name: 'my-svc', domain: 'finance', team: 'finance-team' })
      );
      expect(onSuccess).toHaveBeenCalledWith('new-service-id');
    });
  });
});
