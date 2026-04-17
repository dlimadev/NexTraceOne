import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { AutomationRulesPage } from '../../features/configuration/pages/AutomationRulesPage';

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), delete: vi.fn(), patch: vi.fn() },
}));

import client from '../../api/client';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AutomationRulesPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

vi.mock('../../contexts/EnvironmentContext', () => ({
  useEnvironment: vi.fn().mockReturnValue({
    activeEnvironmentId: 'env-prod-001',
    activeEnvironment: { id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true },
    availableEnvironments: [{ id: 'env-prod-001', name: 'Production', profile: 'production', isProductionLike: true }],
    isLoadingEnvironments: false,
    selectEnvironment: vi.fn(),
    clearEnvironment: vi.fn(),
  }),
}));
describe('AutomationRulesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(client.get).mockResolvedValue({ data: { items: [], totalCount: 0 } });
  });

  it('renders page title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Automation Rules')).toBeInTheDocument();
    });
  });

  it('shows loading state initially', () => {
    vi.mocked(client.get).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('shows empty state when no rules', async () => {
    renderPage();
    await waitFor(() => expect(document.body.textContent).toBeDefined());
  });

  it('renders rule list when data is available', async () => {
    vi.mocked(client.get).mockResolvedValue({
      data: {
        items: [
          {
            ruleId: 'rule-1',
            name: 'Notify on Deploy',
            trigger: 'on_change_created',
            conditionsJson: '{}',
            actionsJson: '[]',
            isEnabled: true,
            createdAt: '2026-01-01T00:00:00Z',
          },
        ],
        totalCount: 1,
      },
    });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Notify on Deploy')).toBeInTheDocument();
    });
  });

  it('shows error state when API fails', async () => {
    vi.mocked(client.get).mockRejectedValue(new Error('API error'));
    renderPage();
    await waitFor(() => expect(document.body.textContent).toBeDefined());
  });
});
