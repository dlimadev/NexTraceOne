import { describe, it, expect, vi, beforeEach, beforeAll } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { AssistantPanel } from '../../features/ai-hub/components/AssistantPanel';

// ── Mock scrollIntoView for JSDOM ───────────────────────────────────────

beforeAll(() => {
  Element.prototype.scrollIntoView = vi.fn();
});

// ── Mock AI API ─────────────────────────────────────────────────────────

vi.mock('../../features/ai-hub/api/aiGovernance', () => ({
  aiGovernanceApi: {
    sendMessage: vi.fn().mockRejectedValue(new Error('API not available')),
  },
}));

// ── Mock PersonaContext ─────────────────────────────────────────────────

vi.mock('../../contexts/PersonaContext', () => ({
  usePersona: vi.fn().mockReturnValue({
    persona: 'Engineer',
    config: {
      aiContextScopes: ['services', 'contracts', 'incidents'],
      aiSuggestedPromptKeys: [],
      sectionOrder: [],
      highlightedSections: [],
      homeSubtitleKey: '',
      homeWidgets: [],
      quickActions: [],
    },
  }),
}));

// ── Mock API client ─────────────────────────────────────────────────────

vi.mock('../../api/client', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    patch: vi.fn(),
  },
}));

// ── Render helper ───────────────────────────────────────────────────────

function renderPanel(props?: Partial<React.ComponentProps<typeof AssistantPanel>>) {
  return render(
    <MemoryRouter>
      <AssistantPanel
        contextType="service"
        contextId="svc-123"
        contextSummary={{ name: 'payment-service', description: 'Payment processing', status: 'Active' }}
        {...props}
      />
    </MemoryRouter>,
  );
}

// ── Tests ────────────────────────────────────────────────────────────────

describe('AssistantPanel', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders collapsed state with title and context hint', () => {
    renderPanel();
    expect(screen.getByTestId('assistant-panel-toggle')).toBeInTheDocument();
    expect(screen.getByText(/ai assistant/i)).toBeInTheDocument();
  });

  it('shows suggested prompts in collapsed state', () => {
    renderPanel();
    const prompts = screen.getAllByTestId(/^suggested-prompt-/);
    expect(prompts.length).toBeGreaterThanOrEqual(1);
    expect(prompts.length).toBeLessThanOrEqual(3);
  });

  it('expands when toggle button is clicked', async () => {
    const user = userEvent.setup();
    renderPanel();
    await user.click(screen.getByTestId('assistant-panel-toggle'));
    await waitFor(() => {
      expect(screen.getByTestId('assistant-panel-expanded')).toBeInTheDocument();
    });
  });

  it('shows welcome message with grounding sources after expanding', async () => {
    const user = userEvent.setup();
    renderPanel();
    await user.click(screen.getByTestId('assistant-panel-toggle'));
    await waitFor(() => {
      expect(screen.getAllByText(/payment-service/).length).toBeGreaterThanOrEqual(1);
      expect(screen.getAllByText(/service catalog/i).length).toBeGreaterThanOrEqual(1);
    });
  });

  it('shows input field and send button when expanded', async () => {
    const user = userEvent.setup();
    renderPanel();
    await user.click(screen.getByTestId('assistant-panel-toggle'));
    await waitFor(() => {
      expect(screen.getByTestId('assistant-input')).toBeInTheDocument();
    });
  });

  it('shows suggested prompts in chat area when expanded', async () => {
    const user = userEvent.setup();
    renderPanel();
    await user.click(screen.getByTestId('assistant-panel-toggle'));
    await waitFor(() => {
      const chatPrompts = screen.getAllByTestId(/^chat-prompt-/);
      expect(chatPrompts.length).toBeGreaterThanOrEqual(1);
    });
  });

  it('shows context-specific prompts for contract type', () => {
    renderPanel({
      contextType: 'contract',
      contextId: 'ctr-456',
      contextSummary: { name: 'order-api v2', description: 'Order API', status: 'Approved' },
    });
    expect(screen.getAllByText(/order-api v2/i).length).toBeGreaterThanOrEqual(1);
  });

  it('shows context-specific prompts for change type', () => {
    renderPanel({
      contextType: 'change',
      contextId: 'chg-789',
      contextSummary: { name: 'deploy-v3.1', description: 'Production deployment', status: 'Validated' },
    });
    expect(screen.getAllByText(/deploy-v3.1/i).length).toBeGreaterThanOrEqual(1);
  });

  it('shows context-specific prompts for incident type', () => {
    renderPanel({
      contextType: 'incident',
      contextId: 'inc-101',
      contextSummary: { name: 'INC-2847 — Payment latency spike', description: 'Latency > 2s', status: 'Investigating' },
    });
    expect(screen.getAllByText(/INC-2847/i).length).toBeGreaterThanOrEqual(1);
  });

  it('sends message and shows typing indicator', async () => {
    const user = userEvent.setup();
    renderPanel();
    await user.click(screen.getByTestId('assistant-panel-toggle'));
    await waitFor(() => {
      expect(screen.getByTestId('assistant-input')).toBeInTheDocument();
    });
    const input = screen.getByTestId('assistant-input');
    await user.type(input, 'What is the status of this service?');
    await user.keyboard('{Enter}');
    await waitFor(() => {
      expect(screen.getByText(/what is the status of this service/i)).toBeInTheDocument();
    });
    // Typing indicator should appear
    await waitFor(() => {
      expect(screen.getByTestId('typing-indicator')).toBeInTheDocument();
    });
  });

  it('shows assistant response with grounding sources after sending message', async () => {
    const user = userEvent.setup();
    renderPanel();
    await user.click(screen.getByTestId('assistant-panel-toggle'));
    await waitFor(() => {
      expect(screen.getByTestId('assistant-input')).toBeInTheDocument();
    });
    const input = screen.getByTestId('assistant-input');
    await user.type(input, 'Tell me about this service');
    await user.keyboard('{Enter}');
    // Wait for mock response (fallback after API rejection)
    await waitFor(
      () => {
        const groundedBadges = screen.getAllByText(/service catalog/i);
        expect(groundedBadges.length).toBeGreaterThanOrEqual(1);
      },
      { timeout: 3000 },
    );
  });

  it('shows metadata panel when response details button is clicked', async () => {
    const user = userEvent.setup();
    renderPanel();
    await user.click(screen.getByTestId('assistant-panel-toggle'));
    await waitFor(() => {
      expect(screen.getByTestId('assistant-input')).toBeInTheDocument();
    });
    // The welcome message has a correlationId, so the metadata toggle should exist
    const metaToggles = screen.getAllByText(/response details/i);
    expect(metaToggles.length).toBeGreaterThanOrEqual(1);
    await user.click(metaToggles[0]);
    await waitFor(() => {
      expect(screen.getByTestId('response-metadata')).toBeInTheDocument();
    });
  });

  it('collapses when close button is clicked', async () => {
    const user = userEvent.setup();
    renderPanel();
    await user.click(screen.getByTestId('assistant-panel-toggle'));
    await waitFor(() => {
      expect(screen.getByTestId('assistant-panel-expanded')).toBeInTheDocument();
    });
    await user.click(screen.getByTestId('assistant-panel-close'));
    await waitFor(() => {
      expect(screen.queryByTestId('assistant-panel-expanded')).not.toBeInTheDocument();
      expect(screen.getByTestId('assistant-panel-toggle')).toBeInTheDocument();
    });
  });

  it('shows governance notice', async () => {
    const user = userEvent.setup();
    renderPanel();
    await user.click(screen.getByTestId('assistant-panel-toggle'));
    await waitFor(() => {
      expect(screen.getByText(/governed/i)).toBeInTheDocument();
    });
  });

  it('shows suggested actions in welcome message', async () => {
    const user = userEvent.setup();
    renderPanel();
    await user.click(screen.getByTestId('assistant-panel-toggle'));
    await waitFor(() => {
      const actions = screen.getAllByTestId(/^action-/);
      expect(actions.length).toBeGreaterThanOrEqual(1);
    });
  });
});
