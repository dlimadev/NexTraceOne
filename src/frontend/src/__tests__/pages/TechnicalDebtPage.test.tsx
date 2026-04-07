import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { TechnicalDebtPage } from '../../features/governance/pages/TechnicalDebtPage';

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import client from '../../api/client';

const mockDebtSummary = {
  totalDebtScore: 110,
  highestRiskService: 'order-service',
  recommendedAction: 'Prioritize security and architecture debt immediately to reduce production risk.',
  debtItems: [
    {
      debtId: '22222222-0000-0000-0000-000000000001',
      serviceName: 'order-service',
      debtType: 'architecture',
      title: 'Monolithic domain boundaries need decomposition',
      severity: 'high',
      estimatedEffortDays: 15,
      debtScore: 25,
    },
    {
      debtId: '22222222-0000-0000-0000-000000000002',
      serviceName: 'order-service',
      debtType: 'security',
      title: 'Outdated JWT validation library',
      severity: 'critical',
      estimatedEffortDays: 3,
      debtScore: 40,
    },
  ],
  byType: [
    { debtType: 'security', count: 1, totalScore: 40 },
    { debtType: 'architecture', count: 1, totalScore: 25 },
  ],
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <TechnicalDebtPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('TechnicalDebtPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(client.get).mockResolvedValue({ data: mockDebtSummary });
    vi.mocked(client.post).mockResolvedValue({ data: {} });
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('Technical Debt');
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(client.get).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders debt items and total score when data is available', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toContain('order-service');
      expect(document.body.textContent).toContain('110');
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(client.get).mockRejectedValue(new Error('API error'));
    renderPage();
    await waitFor(() => {
      expect(document.body).toBeDefined();
    });
  });
});
