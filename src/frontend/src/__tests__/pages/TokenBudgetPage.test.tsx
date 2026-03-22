import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { TokenBudgetPage } from '../../features/ai-hub/pages/TokenBudgetPage';

vi.mock('../../features/ai-hub/api/aiGovernance', () => ({
  aiGovernanceApi: {
    listBudgets: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));

import { aiGovernanceApi } from '../../features/ai-hub/api/aiGovernance';

const mockBudgets = {
  items: [
    {
      budgetId: 'bud-001',
      name: 'Engineering Monthly',
      scope: 'Team',
      scopeValue: 'platform-team',
      period: 'Monthly',
      maxTokens: 1000000,
      maxRequests: 5000,
      currentTokensUsed: 350000,
      currentRequestCount: 1200,
      isActive: true,
      isQuotaExceeded: false,
    },
    {
      budgetId: 'bud-002',
      name: 'Intern Weekly',
      scope: 'Role',
      scopeValue: 'Intern',
      period: 'Weekly',
      maxTokens: 100000,
      maxRequests: 500,
      currentTokensUsed: 105000,
      currentRequestCount: 510,
      isActive: true,
      isQuotaExceeded: true,
    },
  ],
};

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/ai/budgets']}>
        <Routes>
          <Route path="/ai/budgets" element={<TokenBudgetPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('TokenBudgetPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state while fetching budgets', () => {
    vi.mocked(aiGovernanceApi.listBudgets).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Engineering Monthly')).not.toBeInTheDocument();
  });

  it('renders budget list after loading', async () => {
    vi.mocked(aiGovernanceApi.listBudgets).mockResolvedValue(mockBudgets);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Engineering Monthly')).toBeInTheDocument();
    });
    expect(screen.getByText('Intern Weekly')).toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(aiGovernanceApi.listBudgets).mockRejectedValue(new Error('Server error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/error/i)).toBeInTheDocument();
    });
  });

  it('shows empty state when no budgets found', async () => {
    vi.mocked(aiGovernanceApi.listBudgets).mockResolvedValue({ items: [] });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/no.*budget/i)).toBeInTheDocument();
    });
  });

  it('displays quota exceeded badge for exceeded budgets', async () => {
    vi.mocked(aiGovernanceApi.listBudgets).mockResolvedValue(mockBudgets);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Intern Weekly')).toBeInTheDocument();
    });
    expect(screen.getByText(/quota.*exceeded/i)).toBeInTheDocument();
  });

  it('displays budget scope and period information', async () => {
    vi.mocked(aiGovernanceApi.listBudgets).mockResolvedValue(mockBudgets);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Engineering Monthly')).toBeInTheDocument();
    });
    expect(screen.getByText(/Team: platform-team · Monthly/)).toBeInTheDocument();
    expect(screen.getByText(/Role: Intern · Weekly/)).toBeInTheDocument();
  });

  it('displays token usage progress', async () => {
    vi.mocked(aiGovernanceApi.listBudgets).mockResolvedValue(mockBudgets);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Engineering Monthly')).toBeInTheDocument();
    });
    // Token usage: 350,000 / 1,000,000 (35%)
    expect(screen.getByText(/350,000/)).toBeInTheDocument();
    expect(screen.getByText(/1,000,000/)).toBeInTheDocument();
  });
});
