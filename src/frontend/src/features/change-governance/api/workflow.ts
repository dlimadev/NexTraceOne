import client from '../../../api/client';
import type { WorkflowTemplate, WorkflowInstance, PagedList } from '../../../types';

// ─── Checklist Evidence Types ─────────────────────────────────────────────────

/** Item individual de checklist de release (Gap 11). */
export interface ChecklistItemInput {
  name: string;
  completed: boolean;
  notes?: string | null;
}

/** Request para registar evidência de checklist no EvidencePack (Gap 11). */
export interface RecordChecklistEvidenceRequest {
  workflowInstanceId: string;
  checklistName: string;
  items: ChecklistItemInput[];
  executedBy: string;
}

/** Resposta do registo de checklist no EvidencePack (Gap 11). */
export interface ChecklistEvidenceResponse {
  evidencePackId: string;
  workflowInstanceId: string;
  checklistName: string;
  totalItems: number;
  completedItems: number;
  completionRate: number;
  evidenceCompletenessPercentage: number;
  recordedAt: string;
}

export const workflowApi = {
  listTemplates: () =>
    client.get<WorkflowTemplate[]>('/workflow/templates').then((r) => r.data),

  getTemplate: (id: string) =>
    client.get<WorkflowTemplate>(`/workflow/templates/${id}`).then((r) => r.data),

  listInstances: (page = 1, pageSize = 20) =>
    client
      .get<PagedList<WorkflowInstance>>('/workflow/instances', { params: { page, pageSize } })
      .then((r) => r.data),

  getInstance: (id: string) =>
    client.get<WorkflowInstance>(`/workflow/instances/${id}`).then((r) => r.data),

  approve: (instanceId: string, stageId: string, comment?: string) =>
    client
      .post(`/workflow/instances/${instanceId}/stages/${stageId}/approve`, { comment })
      .then((r) => r.data),

  reject: (instanceId: string, stageId: string, reason: string) =>
    client
      .post(`/workflow/instances/${instanceId}/stages/${stageId}/reject`, { reason })
      .then((r) => r.data),

  requestChanges: (instanceId: string, stageId: string, comment: string) =>
    client
      .post(`/workflow/instances/${instanceId}/stages/${stageId}/request-changes`, { comment })
      .then((r) => r.data),

  /** Regista evidência de execução de checklist no EvidencePack (Gap 11). */
  recordChecklistEvidence: (instanceId: string, data: RecordChecklistEvidenceRequest) =>
    client
      .post<ChecklistEvidenceResponse>(`/workflow/instances/${instanceId}/evidence-pack/checklist`, {
        ...data,
        workflowInstanceId: instanceId,
      })
      .then((r) => r.data),
};
