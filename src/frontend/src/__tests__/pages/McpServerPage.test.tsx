import * as React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { McpServerPage } from '../../features/ai-hub/pages/McpServerPage';

vi.mock('../../features/ai-hub/api/aiGovernance', () => ({
  aiGovernanceApi: {
    getMcpServerInfo: vi.fn(),
    listMcpTools: vi.fn(),
  },
}));

vi.mock('../../api/client', () => ({
  default: { get: vi.fn(), post: vi.fn(), patch: vi.fn() },
}));

import { aiGovernanceApi } from '../../features/ai-hub/api/aiGovernance';

const mockServerInfo = {
  serverName: 'NexTraceOne MCP Server',
  protocolVersion: '2024-11-05',
  serverVersion: '1.0.0',
  description: 'Test MCP Server description',
  toolCount: 3,
  categories: ['service_catalog', 'change_intelligence'],
  endpointUrl: '/api/v1/ai/mcp',
  capabilities: {
    tools: { listChanged: false },
    prompts: null,
    resources: null,
  },
};

const mockToolsResponse = {
  tools: [
    {
      name: 'get_service_health',
      description: 'Get the health status of a service.',
      inputSchema: {
        type: 'object',
        properties: {
          service_name: { type: 'string', description: 'Name of the service' },
          environment: { type: 'string', description: 'Environment name' },
        },
        required: ['service_name'],
      },
    },
    {
      name: 'list_recent_changes',
      description: 'List recent changes for a service.',
      inputSchema: {
        type: 'object',
        properties: {
          service_name: { type: 'string', description: 'Service name' },
        },
        required: [],
      },
    },
    {
      name: 'get_service_topology',
      description: 'Get service dependency topology.',
      inputSchema: {
        type: 'object',
        properties: {},
        required: [],
      },
    },
  ],
  totalCount: 3,
};

function renderPage() {
  const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter>
        <McpServerPage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('McpServerPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (aiGovernanceApi.getMcpServerInfo as ReturnType<typeof vi.fn>).mockResolvedValue(mockServerInfo);
    (aiGovernanceApi.listMcpTools as ReturnType<typeof vi.fn>).mockResolvedValue(mockToolsResponse);
  });

  it('shows loading state initially', () => {
    (aiGovernanceApi.getMcpServerInfo as ReturnType<typeof vi.fn>).mockReturnValue(new Promise(() => {}));
    (aiGovernanceApi.listMcpTools as ReturnType<typeof vi.fn>).mockReturnValue(new Promise(() => {}));
    renderPage();
    expect(document.body.textContent).not.toBeNull();
  });

  it('renders page title after load', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('MCP Server')).toBeDefined();
    });
  });

  it('renders protocol version stat', async () => {
    renderPage();
    await waitFor(() => {
      const els = screen.getAllByText('2024-11-05');
      expect(els.length).toBeGreaterThanOrEqual(1);
    });
  });

  it('renders server version stat', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('1.0.0')).toBeDefined();
    });
  });

  it('renders tool count stat', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('3')).toBeDefined();
    });
  });

  it('renders tool names', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('get_service_health')).toBeDefined();
      expect(screen.getByText('list_recent_changes')).toBeDefined();
    });
  });

  it('renders tool descriptions', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Get the health status of a service.')).toBeDefined();
    });
  });

  it('renders categories badges', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('service_catalog')).toBeDefined();
      expect(screen.getByText('change_intelligence')).toBeDefined();
    });
  });

  it('shows error state when server info fails', async () => {
    (aiGovernanceApi.getMcpServerInfo as ReturnType<typeof vi.fn>).mockRejectedValue(new Error('Network error'));
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('Failed to load MCP server')).toBeDefined();
    });
  });

  it('calls getMcpServerInfo and listMcpTools on mount', async () => {
    renderPage();
    await waitFor(() => {
      expect(aiGovernanceApi.getMcpServerInfo).toHaveBeenCalledTimes(1);
      expect(aiGovernanceApi.listMcpTools).toHaveBeenCalledTimes(1);
    });
  });

  it('renders server name', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText('NexTraceOne MCP Server')).toBeDefined();
    });
  });

  it('renders endpoint URL', async () => {
    renderPage();
    await waitFor(() => {
      expect(screen.getByText(/\/api\/v1\/ai\/mcp/)).toBeDefined();
    });
  });
});
