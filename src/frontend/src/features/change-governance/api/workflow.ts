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

// ─── Evidence Pack Types ───────────────────────────────────────────────────────

/** Dados completos do Evidence Pack de um workflow. */
export interface EvidencePackDto {
  evidencePackId: string;
  workflowInstanceId: string;
  releaseId: string;
  contractDiffSummary: string | null;
  blastRadiusScore: number | null;
  spectralScore: number | null;
  changeIntelligenceScore: number | null;
  approvalHistory: string | null;
  contractHash: string | null;
  completenessPercentage: number;
  generatedAt: string;
  pipelineSource: string | null;
  buildId: string | null;
  commitSha: string | null;
  ciChecksResult: string | null;
}

/** Request para gerar Evidence Pack. */
export interface GenerateEvidencePackRequest {
  workflowInstanceId: string;
  contractDiffSummary?: string | null;
  blastRadiusScore?: number | null;
  spectralScore?: number | null;
  changeIntelligenceScore?: number | null;
  pipelineSource?: string | null;
  buildId?: string | null;
  commitSha?: string | null;
  ciChecksResult?: string | null;
}

/** Request para anexar evidência CI/CD ao Evidence Pack. */
export interface AttachCiCdEvidenceRequest {
  workflowInstanceId: string;
  pipelineSource: string;
  buildId: string;
  commitSha: string;
  ciChecksResult: string;
}

/** Resposta do export PDF do Evidence Pack. */
export interface EvidencePackExportDto {
  base64Content: string;
  fileName: string;
  generatedAt: string;
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

  /** Gera o Evidence Pack de uma instância de workflow. */
  generateEvidencePack: (instanceId: string, data: Omit<GenerateEvidencePackRequest, 'workflowInstanceId'>) =>
    client
      .post<EvidencePackDto>(`/workflow/instances/${instanceId}/evidence-pack`, {
        ...data,
        workflowInstanceId: instanceId,
      })
      .then((r) => r.data),

  /** Retorna o Evidence Pack de uma instância de workflow. */
  getEvidencePack: (instanceId: string) =>
    client
      .get<EvidencePackDto>(`/workflow/instances/${instanceId}/evidence-pack`)
      .then((r) => r.data),

  /** Exporta o Evidence Pack em PDF (base64). */
  exportEvidencePackPdf: (instanceId: string) =>
    client
      .get<EvidencePackExportDto>(`/workflow/instances/${instanceId}/evidence-pack/export`)
      .then((r) => r.data),

  /** Anexa evidência de pipeline CI/CD ao Evidence Pack. */
  attachCiCdEvidence: (instanceId: string, data: Omit<AttachCiCdEvidenceRequest, 'workflowInstanceId'>) =>
    client
      .post(`/workflow/instances/${instanceId}/evidence-pack/cicd`, {
        ...data,
        workflowInstanceId: instanceId,
      })
      .then((r) => r.data),
};
