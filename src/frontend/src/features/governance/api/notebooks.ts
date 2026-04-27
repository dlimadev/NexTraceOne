import client from '../../../api/client';

// ── Types ─────────────────────────────────────────────────────────────────────

export interface NotebookCellDto {
  cellId: string;
  cellType: 'Markdown' | 'Query' | 'Widget' | 'Action' | 'Ai';
  sortOrder: number;
  content: string;
  outputJson?: string | null;
  isCollapsed: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface NotebookDetail {
  notebookId: string;
  title: string;
  description?: string | null;
  tenantId: string;
  createdByUserId: string;
  teamId?: string | null;
  persona: string;
  status: 'Draft' | 'Published' | 'Archived';
  sharingScope: string;
  currentRevisionNumber: number;
  linkedDashboardId?: string | null;
  cells: NotebookCellDto[];
  createdAt: string;
  updatedAt: string;
}

export interface NotebookSummary {
  notebookId: string;
  title: string;
  description?: string | null;
  persona: string;
  status: 'Draft' | 'Published' | 'Archived';
  sharingScope: string;
  cellCount: number;
  currentRevisionNumber: number;
  linkedDashboardId?: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface ListNotebooksResponse {
  items: NotebookSummary[];
  total: number;
  page: number;
  pageSize: number;
}

export interface CreateNotebookRequest {
  title: string;
  description?: string | null;
  tenantId: string;
  userId: string;
  persona: string;
  teamId?: string | null;
  initialCells?: Array<{ cellType: string; sortOrder: number; content: string }>;
}

export interface UpdateNotebookRequest {
  tenantId: string;
  userId: string;
  title: string;
  description?: string | null;
  teamId?: string | null;
  cells: Array<{ cellId?: string; cellType: string; sortOrder: number; content: string }>;
}

// AI Composer types
export interface ComposeAiDashboardRequest {
  prompt: string;
  tenantId: string;
  userId: string;
  persona: string;
  teamId?: string | null;
  environmentId?: string | null;
  serviceIds?: string[];
}

export interface ProposedVariableDto {
  key: string;
  label: string;
  type: string;
  defaultValue?: string | null;
}

export interface ProposedWidgetDto {
  widgetType: string;
  title?: string | null;
  serviceFilter?: string | null;
  nqlQuery?: string | null;
  gridX: number;
  gridY: number;
  gridWidth: number;
  gridHeight: number;
}

export interface AiDashboardProposal {
  isSimulated: boolean;
  simulatedNote?: string | null;
  proposedTitle: string;
  proposedLayout: string;
  proposedVariables: ProposedVariableDto[];
  proposedWidgets: ProposedWidgetDto[];
  groundingContext: string;
  generatedAt: string;
}

// ── API client ────────────────────────────────────────────────────────────────

export const notebooksApi = {
  list: (params: {
    tenantId: string;
    persona?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  }) =>
    client
      .get<ListNotebooksResponse>('/governance/notebooks', { params })
      .then((r) => r.data),

  get: (notebookId: string, tenantId: string) =>
    client
      .get<NotebookDetail>(`/governance/notebooks/${notebookId}`, { params: { tenantId } })
      .then((r) => r.data),

  create: (data: CreateNotebookRequest) =>
    client.post<{ notebookId: string; title: string; cellCount: number; status: string }>(
      '/governance/notebooks',
      data,
    ).then((r) => r.data),

  update: (notebookId: string, data: UpdateNotebookRequest) =>
    client
      .put<{ notebookId: string; currentRevisionNumber: number; cellCount: number }>(
        `/governance/notebooks/${notebookId}`,
        data,
      )
      .then((r) => r.data),

  delete: (notebookId: string, tenantId: string) =>
    client
      .delete<{ notebookId: string; deleted: boolean }>(
        `/governance/notebooks/${notebookId}`,
        { params: { tenantId } },
      )
      .then((r) => r.data),

  composeAiDashboard: (data: ComposeAiDashboardRequest) =>
    client
      .post<AiDashboardProposal>('/governance/ai/compose-dashboard', data)
      .then((r) => r.data),
};
