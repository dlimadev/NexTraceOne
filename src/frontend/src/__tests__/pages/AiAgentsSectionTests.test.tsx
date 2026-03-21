import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { AiAgentsSection } from '../../features/contracts/workspace/sections/AiAgentsSection';
import type { StudioContract } from '../../features/contracts/workspace/studioTypes';

// ── Hoisted mocks ──────────────────────────────────────────────────────────

const { listAgentsByContextMock, executeAgentMock } = vi.hoisted(() => ({
  listAgentsByContextMock: vi.fn(),
  executeAgentMock: vi.fn(),
}));

vi.mock('../../features/ai-hub/api/aiGovernance', () => ({
  aiGovernanceApi: {
    listAgentsByContext: listAgentsByContextMock,
    executeAgent: executeAgentMock,
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn() },
}));

// ── Helpers ────────────────────────────────────────────────────────────────

const makeContract = (overrides: Partial<StudioContract> = {}): StudioContract => ({
  id: 'cv-001',
  apiAssetId: 'api-payments',
  technicalName: 'payments-api',
  friendlyName: 'Payments API',
  functionalDescription: 'Handles payment transactions.',
  technicalDescription: '',
  semVer: '2.1.0',
  format: 'yaml',
  protocol: 'OpenApi',
  specContent: 'openapi: "3.1.0"',
  lifecycleState: 'Approved',
  isLocked: false,
  serviceType: 'RestApi',
  domain: 'Finance',
  capability: '',
  product: '',
  owner: 'payments-team',
  team: 'payments-team',
  visibility: 'Internal',
  criticality: 'High',
  dataClassification: 'Internal',
  tags: [],
  sla: '',
  slo: '',
  externalLinks: [],
  approvalState: undefined,
  complianceScore: null,
  approvalChecklist: [],
  policyChecks: [],
  ...overrides,
});

const makeAgent = (overrides = {}) => ({
  agentId: 'agent-001',
  name: 'api-contract-author',
  displayName: 'API Contract Author',
  slug: 'api-contract-author',
  description: 'Generates OpenAPI 3.1 YAML specifications.',
  category: 'ApiDesign',
  isOfficial: true,
  isActive: true,
  capabilities: 'generation',
  targetPersona: 'Architect',
  icon: '📐',
  preferredModelId: null,
  ...overrides,
});

const renderSection = (contract: StudioContract) =>
  render(
    <MemoryRouter>
      <AiAgentsSection contract={contract} />
    </MemoryRouter>,
  );

// ── Tests ──────────────────────────────────────────────────────────────────

describe('AiAgentsSection', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state while fetching agents', () => {
    listAgentsByContextMock.mockReturnValue(new Promise(() => {}));

    renderSection(makeContract());

    expect(screen.getByText(/loading ai agents/i)).toBeInTheDocument();
  });

  it('shows error state when agents fail to load', async () => {
    listAgentsByContextMock.mockRejectedValue(new Error('Network error'));

    renderSection(makeContract());

    await waitFor(() => {
      expect(screen.getByText(/failed to load ai agents/i)).toBeInTheDocument();
    });
  });

  it('shows empty state when no agents returned', async () => {
    listAgentsByContextMock.mockResolvedValue({ items: [], totalCount: 0 });

    renderSection(makeContract());

    await waitFor(() => {
      expect(screen.getByText(/no ai agents available/i)).toBeInTheDocument();
    });
  });

  it('renders agents returned by the API', async () => {
    listAgentsByContextMock.mockResolvedValue({
      items: [makeAgent()],
      totalCount: 1,
    });

    renderSection(makeContract());

    await waitFor(() => {
      expect(screen.getByText('API Contract Author')).toBeInTheDocument();
    });

    expect(screen.getByText('Generates OpenAPI 3.1 YAML specifications.')).toBeInTheDocument();
    expect(screen.getAllByText('Official').length).toBeGreaterThan(0);
  });

  it('calls listAgentsByContext with rest-api context for OpenApi contracts', async () => {
    listAgentsByContextMock.mockResolvedValue({ items: [], totalCount: 0 });

    renderSection(makeContract({ protocol: 'OpenApi', serviceType: 'RestApi' }));

    await waitFor(() => {
      expect(listAgentsByContextMock).toHaveBeenCalledWith('rest-api');
    });
  });

  it('calls listAgentsByContext with soap context for WSDL contracts', async () => {
    listAgentsByContextMock.mockResolvedValue({ items: [], totalCount: 0 });

    renderSection(makeContract({ protocol: 'Wsdl', serviceType: 'Soap' }));

    await waitFor(() => {
      expect(listAgentsByContextMock).toHaveBeenCalledWith('soap');
    });
  });

  it('calls listAgentsByContext with kafka context for AsyncApi contracts', async () => {
    listAgentsByContextMock.mockResolvedValue({ items: [], totalCount: 0 });

    renderSection(makeContract({ protocol: 'AsyncApi', serviceType: 'KafkaProducer' }));

    await waitFor(() => {
      expect(listAgentsByContextMock).toHaveBeenCalledWith('kafka');
    });
  });

  it('shows execution panel when agent is selected', async () => {
    listAgentsByContextMock.mockResolvedValue({
      items: [makeAgent()],
      totalCount: 1,
    });

    renderSection(makeContract());

    await waitFor(() => {
      expect(screen.getByText('API Contract Author')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('API Contract Author'));

    expect(screen.getByLabelText(/describe what you need/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /run agent/i })).toBeInTheDocument();
  });

  it('disables Run Agent button when input is empty', async () => {
    listAgentsByContextMock.mockResolvedValue({
      items: [makeAgent()],
      totalCount: 1,
    });

    renderSection(makeContract());

    await waitFor(() => {
      expect(screen.getByText('API Contract Author')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('API Contract Author'));

    const runButton = screen.getByRole('button', { name: /run agent/i });
    expect(runButton).toBeDisabled();
  });

  it('renders multiple agents', async () => {
    listAgentsByContextMock.mockResolvedValue({
      items: [
        makeAgent({ agentId: 'a1', displayName: 'API Contract Author', name: 'api-contract-author' }),
        makeAgent({ agentId: 'a2', displayName: 'API Test Scenario Generator', name: 'api-test-scenario', icon: '🧪', category: 'TestGeneration' }),
      ],
      totalCount: 2,
    });

    renderSection(makeContract());

    await waitFor(() => {
      expect(screen.getByText('API Contract Author')).toBeInTheDocument();
      expect(screen.getByText('API Test Scenario Generator')).toBeInTheDocument();
    });
  });

  it('executes agent and shows result', async () => {
    listAgentsByContextMock.mockResolvedValue({
      items: [makeAgent()],
      totalCount: 1,
    });

    executeAgentMock.mockResolvedValue({
      executionId: 'exec-001',
      agentId: 'agent-001',
      agentName: 'API Contract Author',
      status: 'Completed',
      output: 'openapi: "3.1.0"\ninfo:\n  title: Payments API',
      promptTokens: 150,
      completionTokens: 300,
      durationMs: 1200,
      artifacts: [
        {
          artifactId: 'art-001',
          artifactType: 'OpenApiDraft',
          title: 'Payments API — Output v1',
          format: 'yaml',
        },
      ],
    });

    renderSection(makeContract());

    await waitFor(() => {
      expect(screen.getByText('API Contract Author')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('API Contract Author'));

    const textarea = screen.getByLabelText(/describe what you need/i);
    fireEvent.change(textarea, { target: { value: 'Generate a complete REST API spec for payments' } });

    const runButton = screen.getByRole('button', { name: /run agent/i });
    fireEvent.click(runButton);

    await waitFor(() => {
      expect(screen.getByText(/agent completed/i)).toBeInTheDocument();
    });

    expect(screen.getByText('Payments API — Output v1')).toBeInTheDocument();
    expect(screen.getByText(/openapi: "3.1.0"/)).toBeInTheDocument();
  });
});
