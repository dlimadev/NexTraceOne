import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ServiceScorecardPage } from '../../features/catalog/pages/ServiceScorecardPage';

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (k: string, f?: unknown) => (typeof f === 'string' ? f : k) }) }));
vi.mock('../../contexts/EnvironmentContext', () => ({ useEnvironment: () => ({ activeEnvironmentId: 'env-1' }) }));

const dim = { score: 0.8, weight: 0.125, justification: 'ok' };
const dimensions = {
  ownership: dim, documentation: dim, contracts: dim, slos: dim,
  observability: dim, changeGovernance: dim, runbooks: dim, security: dim,
};
const getServiceScorecard = vi.fn().mockResolvedValue({
  serviceName: 'orders-api', teamName: null, domain: null, overallScore: 0.8,
  maturityLevel: 'Managed', dimensions, computedAt: '2026-01-01T00:00:00Z',
});
vi.mock('../../features/catalog/api/sourceOfTruth', () => ({
  sourceOfTruthApi: { getServiceScorecard: (...a: unknown[]) => getServiceScorecard(...a) },
}));

function renderAt(path: string) {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={[path]}>
        <ServiceScorecardPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('ServiceScorecardPage preload', () => {
  beforeEach(() => getServiceScorecard.mockClear());

  it('auto-computa o scorecard a partir de ?serviceName= sem digitação manual', async () => {
    renderAt('/services/scorecards?serviceName=orders-api');
    await waitFor(() => expect(getServiceScorecard).toHaveBeenCalledWith('orders-api', 'Production'));
  });

  it('não consulta quando não há ?serviceName=', () => {
    renderAt('/services/scorecards');
    expect(getServiceScorecard).not.toHaveBeenCalled();
  });
});
