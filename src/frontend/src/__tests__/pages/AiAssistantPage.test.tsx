import * as React from 'react';
import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Route, Routes } from 'react-router-dom';
import { renderWithProviders } from '../test-utils';
import { AiAssistantPage } from '../../features/ai-hub/pages/AiAssistantPage';

beforeAll(() => {
  Element.prototype.scrollIntoView = vi.fn();
});

const {
  listConversationsMock,
  createConversationMock,
  getConversationMock,
  sendMessageMock,
  checkProvidersHealthMock,
} = vi.hoisted(() => ({
  listConversationsMock: vi.fn(),
  createConversationMock: vi.fn(),
  getConversationMock: vi.fn(),
  sendMessageMock: vi.fn(),
  checkProvidersHealthMock: vi.fn(),
}));

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
    getConversation: getConversationMock,
    listMessages: vi.fn(),
    listSuggestedPrompts: vi.fn().mockResolvedValue({ items: [] }),
    listAvailableModels: vi.fn().mockResolvedValue({ items: [] }),
    listAgents: vi.fn().mockResolvedValue({ items: [] }),
    checkProvidersHealth: checkProvidersHealthMock,
    chat: vi.fn().mockRejectedValue(new Error('API not available')),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));

function renderPage(initialEntry = '/ai/assistant') {
  return renderWithProviders(
    <Routes>
      <Route path="/ai/assistant" element={<AiAssistantPage />} />
    </Routes>,
    { routerProps: { initialEntries: [initialEntry] } },
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
    getConversationMock.mockResolvedValue({
      conversationId: 'test-conv-1',
      title: 'New Conversation',
      persona: 'Engineer',
      clientType: 'Web',
      defaultContextScope: 'services,contracts',
      lastModelUsed: null,
      createdBy: 'user-1',
      messageCount: 0,
      tags: '',
      isActive: true,
      lastMessageAt: null,
      messages: [],
    });
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
    getConversationMock.mockResolvedValue({
      conversationId: 'conv-persisted',
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
      messages: [{
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
    });

    renderPage('/ai/assistant?conversation=conv-persisted');

    await waitFor(() => expect(getConversationMock).toHaveBeenCalledWith('conv-persisted', { messagePageSize: 100 }));
    expect(await screen.findByText('Persisted assistant answer')).toBeVisible();
  });

  it('reopens the persisted conversation after send by reloading the backend conversation detail', async () => {
    const user = userEvent.setup();

    listConversationsMock.mockResolvedValue({
      items: [{
        id: 'test-conv-1',
        title: 'Persisted conversation',
        persona: 'Engineer',
        clientType: 'Web',
        defaultContextScope: 'services',
        lastModelUsed: 'deepseek-r1:1.5b',
        createdBy: 'user-1',
        messageCount: 0,
        tags: '',
        isActive: true,
        lastMessageAt: null,
      }],
      totalCount: 1,
    });
    getConversationMock.mockResolvedValue({
      conversationId: 'test-conv-1',
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
      messages: [
        {
          messageId: 'msg-user',
          conversationId: 'test-conv-1',
          role: 'user',
          content: 'Investigate payment latency',
          modelName: null,
          provider: null,
          isInternalModel: false,
          promptTokens: 0,
          completionTokens: 0,
          appliedPolicyName: null,
          groundingSources: [],
          contextReferences: [],
          correlationId: 'corr-user',
          timestamp: '2026-03-20T10:00:00Z',
          responseState: 'Completed',
          isDegraded: false,
          degradedReason: null,
        },
        {
          messageId: 'msg-assistant',
          conversationId: 'test-conv-1',
          role: 'assistant',
          content: 'Persisted assistant answer',
          modelName: 'deepseek-r1:1.5b',
          provider: 'ollama',
          isInternalModel: true,
          promptTokens: 12,
          completionTokens: 16,
          appliedPolicyName: null,
          groundingSources: ['Service Catalog'],
          contextReferences: ['service:payments'],
          correlationId: 'corr-assistant',
          timestamp: '2026-03-20T10:00:01Z',
          responseState: 'Completed',
          isDegraded: false,
          degradedReason: null,
        },
      ],
    });
    sendMessageMock.mockResolvedValue({
      conversationId: 'test-conv-1',
      messageId: 'msg-assistant',
      userMessageId: 'msg-user',
    });

    renderPage('/ai/assistant?conversation=test-conv-1');

    const input = await screen.findByRole('textbox');
    await user.type(input, 'Investigate payment latency');
    await user.click(screen.getByRole('button', { name: /send/i }));

    await waitFor(() => expect(sendMessageMock).toHaveBeenCalled());
    await waitFor(() => expect(getConversationMock).toHaveBeenLastCalledWith('test-conv-1', { messagePageSize: 100 }));
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
    listConversationsMock.mockResolvedValue({
      items: [{
        id: 'test-conv-1',
        title: 'New Conversation',
        persona: 'Engineer',
        clientType: 'Web',
        defaultContextScope: 'services,contracts',
        lastModelUsed: null,
        createdBy: 'user-1',
        messageCount: 0,
        tags: '',
        isActive: true,
        lastMessageAt: null,
      }],
      totalCount: 1,
    });
    getConversationMock.mockResolvedValue({
      conversationId: 'test-conv-1',
      title: 'New Conversation',
      persona: 'Engineer',
      clientType: 'Web',
      defaultContextScope: 'services,contracts',
      lastModelUsed: null,
      createdBy: 'user-1',
      messageCount: 0,
      tags: '',
      isActive: true,
      lastMessageAt: null,
      messages: [],
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
