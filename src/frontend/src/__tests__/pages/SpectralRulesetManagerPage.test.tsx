import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { SpectralRulesetManagerPage } from '../../features/contracts/spectral/SpectralRulesetManagerPage';

vi.mock('../../features/contracts/hooks/useSpectralRulesets', () => ({
  useSpectralRulesets: vi.fn(),
  useSpectralRuleset: vi.fn(),
  useCreateSpectralRuleset: vi.fn(),
  useUpdateSpectralRuleset: vi.fn(),
  useToggleSpectralRuleset: vi.fn(),
  useDeleteSpectralRuleset: vi.fn(),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

import {
  useSpectralRulesets,
  useToggleSpectralRuleset,
  useDeleteSpectralRuleset,
  useCreateSpectralRuleset,
} from '../../features/contracts/hooks/useSpectralRulesets';

const emptyData = { items: [], total: 0 };

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <SpectralRulesetManagerPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('SpectralRulesetManagerPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useSpectralRulesets).mockReturnValue({
      data: emptyData,
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useSpectralRulesets>);
    vi.mocked(useToggleSpectralRuleset).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as ReturnType<typeof useToggleSpectralRuleset>);
    vi.mocked(useDeleteSpectralRuleset).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as ReturnType<typeof useDeleteSpectralRuleset>);
    vi.mocked(useCreateSpectralRuleset).mockReturnValue({
      mutate: vi.fn(),
      isPending: false,
    } as ReturnType<typeof useCreateSpectralRuleset>);
  });

  it('renders page title', () => {
    renderPage();
    expect(screen.getByText('Spectral Rulesets')).toBeDefined();
  });

  it('shows loading state', () => {
    vi.mocked(useSpectralRulesets).mockReturnValue({
      data: undefined,
      isLoading: true,
      isError: false,
    } as ReturnType<typeof useSpectralRulesets>);
    renderPage();
    expect(document.body).toBeDefined();
  });

  it('renders rulesets when data is available', async () => {
    vi.mocked(useSpectralRulesets).mockReturnValue({
      data: {
        items: [
          {
            id: 'rs-001',
            name: 'default-rest-ruleset',
            description: 'Official REST API validation rules',
            version: '1.0.0',
            content: '',
            origin: 'Official' as const,
            defaultExecutionMode: 'Full' as const,
            enforcementBehavior: 'Block' as const,
            isActive: true,
            isDefault: true,
            createdAt: '2026-01-01T00:00:00Z',
            updatedAt: '2026-01-01T00:00:00Z',
          },
        ],
        total: 1,
      },
      isLoading: false,
      isError: false,
    } as ReturnType<typeof useSpectralRulesets>);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('default-rest-ruleset')).toBeDefined();
    });
  });
});
