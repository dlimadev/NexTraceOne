import * as React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { GovernanceGatesPage } from '../../features/governance/pages/GovernanceGatesPage';

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <GovernanceGatesPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('GovernanceGatesPage', () => {
  it('renders page title', () => {
    renderPage();
    expect(screen.getByText('Governance Gates')).toBeDefined();
  });

  it('renders without crashing', () => {
    const { container } = renderPage();
    expect(container).toBeDefined();
  });
});
