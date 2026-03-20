import React from 'react';
import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AiAssistantPage } from '../../features/ai-hub/pages/AiAssistantPage';

beforeAll(() => {
  Element.prototype.scrollIntoView = vi.fn();
});

const listConversationsMock = vi.fn();
const createConversationMock = vi.fn();
const listMessagesMock = vi.fn();
const sendMessageMock = vi.fn();
const checkProvidersHealthMock = vi.fn();

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
    sendMessage: sendMessageMock,
    listConversations: listConversationsMock,
    createConversation: createConversationMock,
    listMessages: listMessagesMock,
    listSuggestedPrompts: vi.fn().mockResolvedValue({ items: [] }),
    checkProvidersHealth: checkProvidersHealthMock,
    chat: vi.fn().mockRejectedValue(new Error('API not available')),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));

function renderPage(initialEntry = '/ai/assistant') {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialEntry]}>
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
    listConversationsMock.mockResolvedValue({ items: [], totalCount: 0 });
    createConversationMock.mockResolvedValue({
      conversationId: 'test-conv-1',
      title: 'New Conversation',
      persona: 'Engineer',
      clientType: 'Web',
      defaultContextScope: 'services,contracts',
      isActive: true,
    });
    listMessagesMock.mockResolvedValue({ items: [], totalCount: 0 });
    sendMessageMock.mockRejectedValue(new Error('API not available'));
    checkProvidersHealthMock.mockResolvedValue({ allHealthy: true, items: [] });
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

  it('restores the selected conversation from the URL and loads persisted messages', async () => {
    listConversationsMock.mockResolvedValue({
      items: [{
        id: 'conv-persisted',
        title: 'Persisted conversation',
        persona: 'Engineer',
        clientType: 'Web',
        defaultContextScope: 'services',
        lastModelUsed: 'deepseek-r1:1.5b',
        createdBy: 'user-1',
        messageCount: 2,
        tags: '',
        isActive: true,
        lastMessageAt: '2026-03-20T10:00:00Z',
      }],
      totalCount: 1,
    });
    listMessagesMock.mockResolvedValue({
      items: [{
        messageId: 'msg-1',
        conversationId: 'conv-persisted',
        role: 'assistant',
        content: 'Persisted assistant answer',
        modelName: 'deepseek-r1:1.5b',
        provider: 'ollama',
        isInternalModel: true,
        promptTokens: 10,
        completionTokens: 15,
        appliedPolicyName: null,
        groundingSources: [],
        contextReferences: [],
        correlationId: 'corr-1',
        timestamp: '2026-03-20T10:00:00Z',
        responseState: 'Completed',
        isDegraded: false,
        degradedReason: null,
      }],
      totalCount: 1,
    });

    renderPage('/ai/assistant?conversation=conv-persisted');

    await waitFor(() => expect(listMessagesMock).toHaveBeenCalledWith('conv-persisted', { pageSize: 100 }));
    expect(await screen.findByText('Persisted assistant answer')).toBeVisible();
  });

  it('shows an explicit error state and keeps the typed message when sending fails', async () => {
    const user = userEvent.setup();
    createConversationMock.mockResolvedValue({
      conversationId: 'test-conv-1',
      title: 'New Conversation',
      persona: 'Engineer',
      clientType: 'Web',
      defaultContextScope: 'services,contracts',
      isActive: true,
    });

    renderPage();

    await user.click(screen.getByRole('button', { name: /new conversation/i }));

    const input = screen.getByRole('textbox');
    await user.type(input, 'Investigate payment latency');
    await user.click(screen.getByRole('button', { name: /send/i }));

    expect(await screen.findByText(/no fake assistant response was added/i)).toBeVisible();
    expect(screen.getByRole('textbox')).toHaveValue('Investigate payment latency');
    expect(screen.queryByText(/provider unavailable. no silent mock was used/i)).not.toBeInTheDocument();
  });
});
