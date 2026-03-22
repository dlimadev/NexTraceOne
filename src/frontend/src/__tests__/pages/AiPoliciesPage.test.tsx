import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AiPoliciesPage } from '../../features/ai-hub/pages/AiPoliciesPage';

vi.mock('../../features/ai-hub/api/aiGovernance', () => ({
  aiGovernanceApi: {
    listPolicies: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));

import { aiGovernanceApi } from '../../features/ai-hub/api/aiGovernance';

const mockPolicies = {
  items: [
    {
      policyId: 'pol-001',
      name: 'Engineering Default',
      description: 'Default policy for engineering teams',
      scope: 'Role',
      scopeValue: 'Engineer',
      allowExternalAI: false,
      internalOnly: true,
      maxTokensPerRequest: 4000,
      isActive: true,
    },
    {
      policyId: 'pol-002',
      name: 'Architect Extended',
      description: 'Extended access for architects',
      scope: 'Role',
      scopeValue: 'Architect',
      allowExternalAI: true,
      internalOnly: false,
      maxTokensPerRequest: 16000,
      isActive: true,
    },
    {
      policyId: 'pol-003',
      name: 'Disabled Legacy Policy',
      description: 'Old policy, inactive',
      scope: 'Group',
      scopeValue: 'legacy-team',
      allowExternalAI: false,
      internalOnly: true,
      maxTokensPerRequest: 2000,
      isActive: false,
    },
  ],
};

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/ai/policies']}>
        <Routes>
          <Route path="/ai/policies" element={<AiPoliciesPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('AiPoliciesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state while fetching policies', () => {
    vi.mocked(aiGovernanceApi.listPolicies).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Engineering Default')).not.toBeInTheDocument();
  });

  it('renders policies list after loading', async () => {
    vi.mocked(aiGovernanceApi.listPolicies).mockResolvedValue(mockPolicies);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Engineering Default')).toBeInTheDocument();
    });
    expect(screen.getByText('Architect Extended')).toBeInTheDocument();
    expect(screen.getByText('Disabled Legacy Policy')).toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(aiGovernanceApi.listPolicies).mockRejectedValue(new Error('Server error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/error/i)).toBeInTheDocument();
    });
  });

  it('shows empty state when no policies found', async () => {
    vi.mocked(aiGovernanceApi.listPolicies).mockResolvedValue({ items: [] });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/no.*polic/i)).toBeInTheDocument();
    });
  });

  it('displays policy scope and token information', async () => {
    vi.mocked(aiGovernanceApi.listPolicies).mockResolvedValue(mockPolicies);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Engineering Default')).toBeInTheDocument();
    });
    // Check scope is displayed
    expect(screen.getByText(/Role: Engineer/)).toBeInTheDocument();
    expect(screen.getByText(/Role: Architect/)).toBeInTheDocument();
    // Check token limits
    expect(screen.getByText(/4,000/)).toBeInTheDocument();
    expect(screen.getByText(/16,000/)).toBeInTheDocument();
  });

  it('displays active/inactive badges', async () => {
    vi.mocked(aiGovernanceApi.listPolicies).mockResolvedValue(mockPolicies);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Engineering Default')).toBeInTheDocument();
    });
    // Two active and one inactive policy
    const activeBadges = screen.getAllByText(/active/i);
    expect(activeBadges.length).toBeGreaterThanOrEqual(2);
  });
});
