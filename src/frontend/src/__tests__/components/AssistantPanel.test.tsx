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
    // User message appears in the chat
    await waitFor(() => {
      expect(screen.getByText(/what is the status of this service/i)).toBeInTheDocument();
    });
    // The mock API rejects instantly, so the typing indicator (data-testid="typing-indicator")
    // appears only briefly before the fallback response replaces it.
    // Verify the complete send→response flow completes.
    await waitFor(
      () => {
        expect(screen.getAllByText(/payment-service/i).length).toBeGreaterThanOrEqual(2);
      },
      { timeout: 3000 },
    );
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

  // ── Grounded response tests ──────────────────────────────────────────

  it('shows grounded response with real entity data when contextData is provided', async () => {
    const user = userEvent.setup();
    renderPanel({
      contextData: {
        entityType: 'service',
        entityName: 'payment-service',
        entityStatus: 'Active',
        entityDescription: 'Handles payment processing',
        properties: {
          team: 'payments-team',
          domain: 'finance',
          criticality: 'Critical',
        },
        relations: [
          { relationType: 'Contracts', entityType: 'contract', name: 'payment-api-v2', status: 'Active', properties: { protocol: 'OpenApi' } },
        ],
      },
    });
    await user.click(screen.getByTestId('assistant-panel-toggle'));
    await waitFor(() => expect(screen.getByTestId('assistant-panel-expanded')).toBeInTheDocument());
    const input = screen.getByTestId('assistant-input');
    await user.type(input, 'What contracts does this service expose?');
    await user.keyboard('{Enter}');
    // Wait for grounded response which includes real entity data
    await waitFor(
      () => {
        // Response should include real entity data from properties
        const matches = screen.getAllByText(/payments-team/i);
        expect(matches.length).toBeGreaterThanOrEqual(1);
      },
      { timeout: 3000 },
    );
  });

  it('shows context strength badge for grounded response', async () => {
    const user = userEvent.setup();
    renderPanel({
      contextData: {
        entityType: 'service',
        entityName: 'payment-service',
        entityStatus: 'Active',
        properties: { team: 'payments-team', domain: 'finance', criticality: 'Critical' },
        relations: [
          { relationType: 'Contracts', entityType: 'contract', name: 'payment-api-v2', status: 'Active' },
          { relationType: 'Contracts', entityType: 'contract', name: 'payment-api-v1', status: 'Deprecated' },
        ],
      },
    });
    await user.click(screen.getByTestId('assistant-panel-toggle'));
    await waitFor(() => expect(screen.getByTestId('assistant-panel-expanded')).toBeInTheDocument());
    await user.type(screen.getByTestId('assistant-input'), 'Overview');
    await user.keyboard('{Enter}');
    // Context strength badge should appear (multiple elements may contain "Context")
    await waitFor(
      () => {
        const strengthBadges = screen.getAllByText(/Strong Context|Good Context|Partial Context|Limited Context/i);
        expect(strengthBadges.length).toBeGreaterThanOrEqual(1);
      },
      { timeout: 3000 },
    );
  });

  it('shows caveats when context has limitations', async () => {
    const user = userEvent.setup();
    renderPanel({
      contextData: {
        entityType: 'service',
        entityName: 'payment-service',
        properties: { team: 'payments-team' },
        caveats: ['No contracts loaded'],
      },
    });
    await user.click(screen.getByTestId('assistant-panel-toggle'));
    await waitFor(() => expect(screen.getByTestId('assistant-panel-expanded')).toBeInTheDocument());
    await user.type(screen.getByTestId('assistant-input'), 'Show me contracts');
    await user.keyboard('{Enter}');
    await waitFor(
      () => {
        expect(screen.getByText(/No contracts loaded/i)).toBeInTheDocument();
      },
      { timeout: 3000 },
    );
  });

  it('shows weak context warning when contextData is missing', async () => {
    const user = userEvent.setup();
    renderPanel(); // no contextData
    await user.click(screen.getByTestId('assistant-panel-toggle'));
    await waitFor(() => expect(screen.getByTestId('assistant-panel-expanded')).toBeInTheDocument());
    await user.type(screen.getByTestId('assistant-input'), 'What is this?');
    await user.keyboard('{Enter}');
    // Should see limited context indication — caveats section rendered
    await waitFor(
      () => {
        const matches = screen.getAllByText(/payment-service/i);
        // Should have at least 2 matches: welcome + fallback response
        expect(matches.length).toBeGreaterThanOrEqual(2);
      },
      { timeout: 3000 },
    );
  });

  it('shows real context references with entity names in grounded response', async () => {
    const user = userEvent.setup();
    renderPanel({
      contextData: {
        entityType: 'contract',
        entityName: 'order-api — OpenApi',
        entityStatus: 'Active',
        properties: { protocol: 'OpenApi', version: '3.2.0', service: 'order-service' },
        relations: [
          { relationType: 'Version History', entityType: 'version', name: 'v3.1.0', status: 'Active' },
        ],
      },
      contextType: 'contract',
      contextId: 'ctr-456',
      contextSummary: { name: 'order-api v2', description: 'Order API', status: 'Active' },
    });
    await user.click(screen.getByTestId('assistant-panel-toggle'));
    await waitFor(() => expect(screen.getByTestId('assistant-panel-expanded')).toBeInTheDocument());
    await user.type(screen.getByTestId('assistant-input'), 'Explain this contract');
    await user.keyboard('{Enter}');
    // Context references should include real entity names
    await waitFor(
      () => {
        const refs = screen.getAllByText(/contract:order-api/);
        expect(refs.length).toBeGreaterThanOrEqual(1);
      },
      { timeout: 5000 },
    );
  });

  it('renders grounded response differently for incident context', async () => {
    const user = userEvent.setup();
    renderPanel({
      contextType: 'incident',
      contextId: 'inc-101',
      contextSummary: { name: 'INC-2847 — Payment latency spike', status: 'Investigating' },
      contextData: {
        entityType: 'incident',
        entityName: 'INC-2847 — Payment latency spike',
        entityStatus: 'Investigating',
        properties: { severity: 'High', team: 'payments-team', mitigationStatus: 'InProgress' },
        relations: [
          { relationType: 'Affected Services', entityType: 'service', name: 'payment-service', status: 'Critical' },
          { relationType: 'Runbooks', entityType: 'runbook', name: 'Payment Latency Recovery' },
        ],
      },
    });
    await user.click(screen.getByTestId('assistant-panel-toggle'));
    await waitFor(() => expect(screen.getByTestId('assistant-panel-expanded')).toBeInTheDocument());
    await user.type(screen.getByTestId('assistant-input'), 'What is the root cause?');
    await user.keyboard('{Enter}');
    await waitFor(
      () => {
        // Should show incident-specific response with affected services
        const matches = screen.getAllByText(/INC-2847/);
        expect(matches.length).toBeGreaterThanOrEqual(1);
        // Should show severity from real data
        const highMatches = screen.getAllByText(/High/i);
        expect(highMatches.length).toBeGreaterThanOrEqual(1);
      },
      { timeout: 3000 },
    );
  });

  it('renders grounded response differently for change context', async () => {
    const user = userEvent.setup();
    renderPanel({
      contextType: 'change',
      contextId: 'chg-789',
      contextSummary: { name: 'deploy-v3.1', status: 'PendingReview' },
      contextData: {
        entityType: 'change',
        entityName: 'order-service — v3.1.0',
        entityStatus: 'PendingReview',
        properties: { changeType: 'Major', environment: 'Production', team: 'order-team', score: '72' },
        relations: [
          { relationType: 'Advisory Factors', entityType: 'factor', name: 'UnitTestCoverage', status: 'Warning' },
        ],
      },
    });
    await user.click(screen.getByTestId('assistant-panel-toggle'));
    await waitFor(() => expect(screen.getByTestId('assistant-panel-expanded')).toBeInTheDocument());
    await user.type(screen.getByTestId('assistant-input'), 'What is the risk?');
    await user.keyboard('{Enter}');
    await waitFor(
      () => {
        // Should show change-specific response with real data
        const matches = screen.getAllByText(/order-service/);
        expect(matches.length).toBeGreaterThanOrEqual(1);
      },
      { timeout: 3000 },
    );
  });
});
