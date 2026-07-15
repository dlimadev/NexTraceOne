import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ServiceApisSection } from '../../features/catalog/components/ServiceApisSection';
import type { ServiceApiSummary } from '../../types';

vi.mock('react-i18next', async (orig) => ({
  ...(await orig<typeof import('react-i18next')>()),
  useTranslation: () => ({ t: (_k: string, d?: string) => d ?? _k, i18n: { language: 'en' } }),
}));

const api: ServiceApiSummary = {
  apiId: 'a1', name: 'payments-api', routePattern: '/v1/payments', version: '1.2.0',
  visibility: 'Public', isDecommissioned: false, consumerCount: 3,
};

describe('ServiceApisSection', () => {
  it('renderiza uma linha por API', () => {
    render(<ServiceApisSection apis={[api]} />);
    expect(screen.getByText('payments-api')).toBeInTheDocument();
    expect(screen.getByText('/v1/payments')).toBeInTheDocument();
  });

  it('mostra estado vazio quando não há APIs', () => {
    render(<ServiceApisSection apis={[]} />);
    expect(screen.getByText(/no.*api/i)).toBeInTheDocument();
  });
});
