import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AiAssistantPage } from '../../features/ai-hub/pages/AiAssistantPage';

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

vi.mock('../../features/ai-hub/api/aiGovernance', () => ({
  aiGovernanceApi: {
    sendMessage: vi.fn().mockRejectedValue(new Error('API not available')),
    listConversations: vi.fn(),
    createConversation: vi.fn(),
    listMessages: vi.fn(),
    listSuggestedPrompts: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/ai/assistant']}>
        <Routes>
          <Route path="/ai/assistant" element={<AiAssistantPage />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('AiAssistantPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders the AI assistant page title', () => {
    renderPage();
    expect(screen.getAllByText(/ai assistant/i).length).toBeGreaterThanOrEqual(1);
  });

  it('shows message input area', () => {
    renderPage();
    const inputs = screen.getAllByRole('textbox');
    expect(inputs.length).toBeGreaterThanOrEqual(1);
  });

  it('displays persona badge', () => {
    renderPage();
    expect(screen.getAllByText(/engineer/i).length).toBeGreaterThanOrEqual(1);
  });

  it('shows suggested prompts for new conversations', () => {
    renderPage();
    waitFor(() => {
      expect(screen.getAllByRole('button').length).toBeGreaterThanOrEqual(1);
    });
  });

  it('allows typing a message', async () => {
    const user = userEvent.setup();
    renderPage();
    const textareas = screen.getAllByRole('textbox');
    const messageInput = textareas[textareas.length - 1];
    await user.type(messageInput, 'What is the status of the payments service?');
    expect(messageInput).toHaveValue('What is the status of the payments service?');
  });
});
