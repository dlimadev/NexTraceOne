import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import { WorkflowPage } from '../../features/change-governance/pages/WorkflowPage';
import type { WorkflowInstance, WorkflowTemplate, PagedList } from '../../types';

vi.mock('../../features/change-governance/api', () => ({
  workflowApi: {
    listTemplates: vi.fn(),
    listInstances: vi.fn(),
    approve: vi.fn(),
    reject: vi.fn(),
  },
}));

import { workflowApi } from '../../features/change-governance/api';

const mockTemplates: WorkflowTemplate[] = [
  {
    id: 'tpl-1',
    name: 'Standard Release',
    changeLevel: 1,
    stages: [{ id: 'stg-1', name: 'Review', order: 1, approvers: [], requiredApprovals: 1 }],
    createdAt: '2024-01-01T00:00:00Z',
  },
  {
    id: 'tpl-2',
    name: 'Breaking Change Release',
    changeLevel: 3,
    stages: [
      { id: 'stg-2', name: 'Security Review', order: 1, approvers: [], requiredApprovals: 2 },
      { id: 'stg-3', name: 'Manager Approval', order: 2, approvers: [], requiredApprovals: 1 },
    ],
    createdAt: '2024-01-01T00:00:00Z',
  },
];

const mockInstances: PagedList<WorkflowInstance> = {
  items: [
    {
      id: 'inst-1',
      releaseId: 'rel-001',
      templateId: 'tpl-1',
      status: 'InProgress',
      currentStage: 'stg-1',
      createdAt: '2024-01-15T10:00:00Z',
    },
    {
      id: 'inst-2',
      releaseId: 'rel-002',
      templateId: 'tpl-2',
      status: 'Approved',
      createdAt: '2024-01-14T09:00:00Z',
      completedAt: '2024-01-14T11:00:00Z',
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 20,
  totalPages: 1,
};

function renderWorkflow() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <WorkflowPage />
      </MemoryRouter>
    </QueryClientProvider>
  );
}

describe('WorkflowPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('exibe o título da página', () => {
    vi.mocked(workflowApi.listTemplates).mockResolvedValue([]);
    vi.mocked(workflowApi.listInstances).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0,
    });
    renderWorkflow();
    expect(screen.getByRole('heading', { name: 'Workflow & Approvals' })).toBeInTheDocument();
  });

  it('exibe templates carregados da API', async () => {
    vi.mocked(workflowApi.listTemplates).mockResolvedValue(mockTemplates);
    vi.mocked(workflowApi.listInstances).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0,
    });
    renderWorkflow();
    await waitFor(() => {
      expect(screen.getByText('Standard Release')).toBeInTheDocument();
      expect(screen.getByText('Breaking Change Release')).toBeInTheDocument();
    });
  });

  it('exibe instâncias pendentes com botões de aprovação', async () => {
    vi.mocked(workflowApi.listTemplates).mockResolvedValue([]);
    vi.mocked(workflowApi.listInstances).mockResolvedValue(mockInstances);
    renderWorkflow();
    await waitFor(() => {
      expect(screen.getByText('Pending Approvals')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /approve/i })).toBeInTheDocument();
    });
  });

  it('exibe formulário de rejeição ao clicar em Reject', async () => {
    vi.mocked(workflowApi.listTemplates).mockResolvedValue([]);
    vi.mocked(workflowApi.listInstances).mockResolvedValue(mockInstances);
    renderWorkflow();
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /reject/i })).toBeInTheDocument();
    });
    await userEvent.click(screen.getByRole('button', { name: /reject/i }));
    expect(screen.getByPlaceholderText(/reason for rejection/i)).toBeInTheDocument();
  });

  it('chama approve com instanceId e stageId corretos', async () => {
    vi.mocked(workflowApi.listTemplates).mockResolvedValue([]);
    vi.mocked(workflowApi.listInstances).mockResolvedValue(mockInstances);
    vi.mocked(workflowApi.approve).mockResolvedValue({});
    renderWorkflow();
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /approve/i })).toBeInTheDocument();
    });
    await userEvent.click(screen.getByRole('button', { name: /approve/i }));
    await waitFor(() => {
      expect(workflowApi.approve).toHaveBeenCalled();
      const [instanceId, stageId] = vi.mocked(workflowApi.approve).mock.calls[0];
      expect(instanceId).toBe('inst-1');
      expect(stageId).toBe('stg-1');
    });
  });

  it('exibe mensagem de erro quando API falha', async () => {
    vi.mocked(workflowApi.listTemplates).mockResolvedValue([]);
    vi.mocked(workflowApi.listInstances).mockRejectedValue(new Error('Network error'));
    renderWorkflow();
    await waitFor(() => {
      expect(screen.getByText(/failed to load workflow instances/i)).toBeInTheDocument();
    });
  });

  it('exibe templates padrão quando API retorna lista vazia', async () => {
    vi.mocked(workflowApi.listTemplates).mockResolvedValue([]);
    vi.mocked(workflowApi.listInstances).mockResolvedValue({
      items: [], totalCount: 0, page: 1, pageSize: 20, totalPages: 0,
    });
    renderWorkflow();
    await waitFor(() => {
      expect(screen.getByText('Standard Release')).toBeInTheDocument();
    });
  });
});
