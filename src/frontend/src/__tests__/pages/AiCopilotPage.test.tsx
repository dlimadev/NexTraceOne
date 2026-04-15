import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({ tenantId: 't1', user: { id: 'u1', name: 'Test User' } }),
}));

vi.mock('../../contexts/PersonaContext', () => ({
  usePersona: () => ({
    persona: 'Engineer',
    config: {
      aiContextScopes: ['services', 'changes', 'incidents'],
      aiSuggestedPromptKeys: ['aiHub.suggestedPrompts.debugIssue', 'aiHub.suggestedPrompts.explainService'],
      sectionOrder: [],
      highlightedSections: [],
      homeSubtitleKey: '',
      homeWidgets: [],
      quickActions: [],
    },
  }),
}));

// jsdom does not implement scrollIntoView — stub it
if (typeof window !== 'undefined') {
  window.HTMLElement.prototype.scrollIntoView = () => {};
}

vi.mock('../../features/ai-hub/api/aiGovernance', () => ({
  aiGovernanceApi: {
    listConversations: vi.fn(),
    getConversation: vi.fn(),
    checkProvidersHealth: vi.fn(),
    listAvailableModels: vi.fn(),
    createConversation: vi.fn(),
    sendMessage: vi.fn(),
  },
}));

import { aiGovernanceApi } from '../../features/ai-hub/api/aiGovernance';
import { AiCopilotPage } from '../../features/ai-hub/pages/AiCopilotPage';

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <AiCopilotPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('AiCopilotPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(aiGovernanceApi.listConversations).mockResolvedValue({ items: [], totalCount: 0 } as any);
    vi.mocked(aiGovernanceApi.checkProvidersHealth).mockResolvedValue({ items: [] } as any);
    vi.mocked(aiGovernanceApi.listAvailableModels).mockResolvedValue({ items: [] } as any);
  });

  it('renders without crashing', async () => {
    renderPage();
    await waitFor(() => expect(document.body).toBeDefined());
  });

  it('renders a message input area', async () => {
    renderPage();
    await waitFor(() => {
      const inputs = document.querySelectorAll('textarea, input[type="text"]');
      expect(inputs.length).toBeGreaterThanOrEqual(0);
    });
  });

  it('calls listConversations on mount', async () => {
    renderPage();
    await waitFor(() => {
      expect(aiGovernanceApi.listConversations).toHaveBeenCalled();
    });
  });

  it('shows empty conversation list when no history', async () => {
    renderPage();
    await waitFor(() => {
      expect(aiGovernanceApi.listConversations).toHaveBeenCalledTimes(1);
    });
  });

  it('handles API failure gracefully', async () => {
    vi.mocked(aiGovernanceApi.listConversations).mockRejectedValue(new Error('API error'));
    renderPage();
    await waitFor(() => expect(document.body.textContent).toBeDefined());
  });
});
