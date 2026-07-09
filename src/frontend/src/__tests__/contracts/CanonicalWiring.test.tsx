import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { CanonicalEntityImpactCascadePage } from '../../features/contracts/canonical/CanonicalEntityImpactCascadePage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: string) => f ?? k }) }));
vi.mock('../../features/contracts/api/contracts', () => ({
  contractsApi: { getCanonicalEntityImpactCascade: vi.fn(() => new Promise(() => {})) },
}));

function wrapCascade(entry: string) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={[entry]}>
        <CanonicalEntityImpactCascadePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('CanonicalEntityImpactCascadePage query param', () => {
  it('pre-fills the entity id from ?entityId=', () => {
    wrapCascade('/contracts/canonical/impact-cascade?entityId=ent-42');
    const input = screen.getByLabelText(/Canonical Entity ID/i) as HTMLInputElement;
    expect(input.value).toBe('ent-42');
  });
});
