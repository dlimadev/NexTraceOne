import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AiRoutingPage } from '../../features/ai-hub/pages/AiRoutingPage';

vi.mock('../../features/ai-hub/api/aiGovernance', () => ({
  aiGovernanceApi: {
    listRoutingStrategies: vi.fn(),
    listKnowledgeSourceWeights: vi.fn(),
  },
}));

vi.mock('../../contexts/PersonaContext', () => ({
  usePersona: vi.fn(() => ({
    persona: 'Engineer',
    config: { highlightedSections: [], sectionOrder: [] },
    setPersona: vi.fn(),
  })),
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));

import { aiGovernanceApi } from '../../features/ai-hub/api/aiGovernance';

const mockStrategies = {
  items: [
    {
      strategyId: 'strat-001',
      name: 'Internal-First Routing',
      description: 'Route to internal models by default',
      targetPersona: '*',
      targetUseCase: 'Chat',
      targetClientType: 'Web',
      preferredPath: 'InternalOnly',
      maxSensitivityLevel: 3,
      allowExternalEscalation: false,
      isActive: true,
      priority: 1,
      createdAt: '2026-03-01T00:00:00Z',
    },
    {
      strategyId: 'strat-002',
      name: 'Architect Extended',
      description: 'Allow external AI for architects',
      targetPersona: 'Architect',
      targetUseCase: '*',
      targetClientType: '*',
      preferredPath: 'InternalPreferred',
      maxSensitivityLevel: 4,
      allowExternalEscalation: true,
      isActive: true,
      priority: 2,
      createdAt: '2026-03-05T00:00:00Z',
    },
  ],
};

const mockWeights = {
  items: [
    { sourceType: 'Contracts', useCaseType: 'ContractGeneration', relevance: 'Primary', weight: 60 },
    { sourceType: 'Documentation', useCaseType: 'ContractGeneration', relevance: 'Secondary', weight: 30 },
    { sourceType: 'Telemetry', useCaseType: 'ContractGeneration', relevance: 'Supplementary', weight: 10 },
  ],
};

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/ai/routing']}>
        <Routes>
          <Route path="/ai/routing" element={<AiRoutingPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('AiRoutingPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state while fetching strategies', () => {
    vi.mocked(aiGovernanceApi.listRoutingStrategies).mockReturnValue(new Promise(() => {}));
    vi.mocked(aiGovernanceApi.listKnowledgeSourceWeights).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(screen.queryByText('Internal-First Routing')).not.toBeInTheDocument();
  });

  it('renders strategies list after loading', async () => {
    vi.mocked(aiGovernanceApi.listRoutingStrategies).mockResolvedValue(mockStrategies);
    vi.mocked(aiGovernanceApi.listKnowledgeSourceWeights).mockResolvedValue(mockWeights);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Internal-First Routing')).toBeInTheDocument();
    });
    expect(screen.getByText('Architect Extended')).toBeInTheDocument();
  });

  it('shows error state when API fails', async () => {
    vi.mocked(aiGovernanceApi.listRoutingStrategies).mockRejectedValue(new Error('Server error'));
    vi.mocked(aiGovernanceApi.listKnowledgeSourceWeights).mockRejectedValue(new Error('Server error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/error/i)).toBeInTheDocument();
    });
  });

  it('shows empty state when no strategies found', async () => {
    vi.mocked(aiGovernanceApi.listRoutingStrategies).mockResolvedValue({ items: [] });
    vi.mocked(aiGovernanceApi.listKnowledgeSourceWeights).mockResolvedValue({ items: [] });
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/no routing strategies found/i)).toBeInTheDocument();
    });
  });

  it('displays stat overview counts', async () => {
    vi.mocked(aiGovernanceApi.listRoutingStrategies).mockResolvedValue(mockStrategies);
    vi.mocked(aiGovernanceApi.listKnowledgeSourceWeights).mockResolvedValue(mockWeights);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Internal-First Routing')).toBeInTheDocument();
    });
    // 2 strategies total, 2 active, 1 with escalation, 1 use case in weights
    const twos = screen.getAllByText('2');
    expect(twos.length).toBeGreaterThanOrEqual(2);
  });

  it('displays routing path badges', async () => {
    vi.mocked(aiGovernanceApi.listRoutingStrategies).mockResolvedValue(mockStrategies);
    vi.mocked(aiGovernanceApi.listKnowledgeSourceWeights).mockResolvedValue(mockWeights);
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('InternalOnly')).toBeInTheDocument();
    });
    expect(screen.getByText('InternalPreferred')).toBeInTheDocument();
  });
});
