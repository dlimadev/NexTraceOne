import client from '../../../api/client';

// ── Action Catalog ────────────────────────────────────────────────────

export interface AutomationActionItem {
  actionId: string;
  name: string;
  displayName: string;
  description: string;
  actionType: string;
  riskLevel: string;
  requiresApproval: boolean;
  allowedPersonas: string[];
  allowedEnvironments: string[];
  preconditionTypes: string[];
  hasPostValidation: boolean;
}

export interface AutomationActionsResponse {
  items: AutomationActionItem[];
}

// ── Workflows ─────────────────────────────────────────────────────────

export interface AutomationWorkflowSummary {
  workflowId: string;
  actionId: string;
  actionDisplayName: string;
  status: string;
  riskLevel: string;
  requestedBy: string;
  serviceId: string | null;
  createdAt: string;
}

export interface AutomationWorkflowsResponse {
  items: AutomationWorkflowSummary[];
  totalCount: number;
}

export interface ApproverInfo {
  approvedBy: string;
  approvedAt: string;
  approvalStatus: string;
}

export interface PreconditionItem {
  type: string;
  description: string;
  status: string;
  evaluatedAt: string | null;
}

export interface ExecutionStep {
  stepOrder: number;
  title: string;
  status: string;
  completedAt: string | null;
  completedBy: string | null;
}

export interface ValidationInfo {
  status: string;
  observedOutcome: string | null;
  validatedBy: string | null;
  validatedAt: string | null;
}

export interface WorkflowAuditEntry {
  action: string;
  performedBy: string;
  performedAt: string;
  details: string | null;
}

export interface AutomationWorkflowDetail {
  workflowId: string;
  actionId: string;
  actionDisplayName: string;
  status: string;
  riskLevel: string;
  rationale: string;
  requestedBy: string;
  approverInfo: ApproverInfo | null;
  scope: string | null;
  environment: string | null;
  serviceId: string | null;
  incidentId: string | null;
  changeId: string | null;
  preconditions: PreconditionItem[];
  executionSteps: ExecutionStep[];
  validationInfo: ValidationInfo | null;
  auditEntries: WorkflowAuditEntry[];
  createdAt: string;
  updatedAt: string;
}

// ── Audit Trail ───────────────────────────────────────────────────────

export interface AuditTrailEntry {
  entryId: string;
  workflowId: string | null;
  action: string;
  performedBy: string;
  performedAt: string;
  details: string | null;
  serviceId: string | null;
  teamId: string | null;
}

export interface AuditTrailResponse {
  entries: AuditTrailEntry[];
}

/** Cliente de API para Operational Automation. */
export const automationApi = {
  listActions: (filter?: string) =>
    client.get<AutomationActionsResponse>('/automation/actions', { params: { filter } }).then((r) => r.data),

  getAction: (actionId: string) =>
    client.get<AutomationActionItem>(`/automation/actions/${actionId}`).then((r) => r.data),

  listWorkflows: (params?: {
    serviceId?: string;
    status?: string;
    page?: number;
    pageSize?: number;
  }) =>
    client.get<AutomationWorkflowsResponse>('/automation/workflows', { params }).then((r) => r.data),

  getWorkflow: (workflowId: string) =>
    client.get<AutomationWorkflowDetail>(`/automation/workflows/${workflowId}`).then((r) => r.data),

  getAuditTrail: (params?: {
    workflowId?: string;
    serviceId?: string;
    teamId?: string;
  }) =>
    client.get<AuditTrailResponse>('/automation/audit', { params }).then((r) => r.data),
};
