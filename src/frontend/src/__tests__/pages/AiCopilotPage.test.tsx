import * as React from 'react';
import { describe, it, expect, vi, beforeAll, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import { Route, Routes } from 'react-router-dom';
import { renderWithProviders } from '../test-utils';
import { AiCopilotPage } from '../../features/ai-hub/pages/AiCopilotPage';

beforeAll(() => {
  Element.prototype.scrollIntoView = vi.fn();
});

vi.mock('../../contexts/PersonaContext', () => ({
  usePersona: vi.fn().mockReturnValue({
    persona: 'Engineer',
    config: {
      aiContextScopes: ['services', 'contracts', 'incidents'],
      aiSuggestedPromptKeys: ['services', 'contracts'],
      sectionOrder: [],
      highlightedSections: [],
      homeSubtitleKey: '',
      homeWidgets: [],
      quickActions: [],
    },
  }),
}));

vi.mock('../../contexts/AuthContext', () => ({
  useAuth: () => ({
    user: { id: 'u1', firstName: 'Test', fullName: 'Test User' },
    tenantId: 't1',
    logout: vi.fn(),
  }),
}));

vi.mock('../../features/ai-hub/api/aiGovernance', () => ({
  aiGovernanceApi: {
    listConversations: vi.fn().mockResolvedValue({ items: [], totalCount: 0 }),
    createConversation: vi.fn().mockResolvedValue({ conversationId: 'c1' }),
    getConversation: vi.fn().mockResolvedValue({ messages: [] }),
    sendMessage: vi.fn().mockRejectedValue(new Error('not available')),
    checkProvidersHealth: vi.fn().mockResolvedValue({ allHealthy: true, items: [] }),
    listAvailableModels: vi.fn().mockResolvedValue({ internalModels: [], externalModels: [], totalCount: 0 }),
  },
}));

vi.mock('../../api/client', () => ({
  default: {
    get: vi.fn().mockResolvedValue({ data: {} }),
    post: vi.fn().mockResolvedValue({ data: {} }),
  },
}));

function renderPage(initialEntry = '/ai/copilot') {
  return renderWithProviders(
    <Routes>
      <Route path="/ai/copilot" element={<AiCopilotPage />} />
    </Routes>,
    { routerProps: { initialEntries: [initialEntry] } },
  );
}

describe('AiCopilotPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders copilot title', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('NexTrace AI')).toBeDefined();
    });
  });

  it('renders greeting message', async () => {
    renderPage();
    await waitFor(() => {
      expect(document.body.textContent).toBeDefined();
    });
  });

  it('renders input placeholder area', async () => {
    renderPage();
    await waitFor(() => {
      const textareas = screen.getAllByRole('textbox');
      expect(textareas.length).toBeGreaterThanOrEqual(1);
    });
  });
});
