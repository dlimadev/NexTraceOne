import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { CanonicalEntityPicker } from '../../features/contracts/workspace/builders/shared/CanonicalEntityPicker';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: unknown) => (typeof f === 'string' ? f : k) }) }));

const listCanonicalEntities = vi.fn().mockResolvedValue({
  items: [{ id: 'e1', name: 'Payment', domain: 'Billing', category: 'Core' }],
});
vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: { listCanonicalEntities: (...a: unknown[]) => listCanonicalEntities(...a) },
}));

function renderPicker(onSelect = vi.fn(), onClose = vi.fn()) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={qc}>
      <CanonicalEntityPicker onSelect={onSelect} onClose={onClose} />
    </QueryClientProvider>,
  );
  return { onSelect, onClose };
}

describe('CanonicalEntityPicker (DS Modal)', () => {
  it('renderiza como DS Modal com título e a lista de entidades', async () => {
    renderPicker();
    expect(screen.getByText('Browse Canonical Entities')).toBeInTheDocument();
    expect(await screen.findByText('Payment')).toBeInTheDocument();
  });

  it('selecionar uma entidade devolve o $ref e fecha', async () => {
    const { onSelect, onClose } = renderPicker();
    await screen.findByText('Payment');
    fireEvent.click(screen.getByRole('button', { name: /Select/ }));
    await waitFor(() => expect(onSelect).toHaveBeenCalledWith('#/components/schemas/Payment'));
    expect(onClose).toHaveBeenCalled();
  });
});
