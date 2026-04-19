import client from '../../../api/client';
import type {
  FinOpsSummaryResponse,
  ServiceFinOpsResponse,
  TeamFinOpsResponse,
  DomainFinOpsResponse,
  FinOpsTrendsResponse,
  EfficiencyIndicatorsResponse,
} from '../../../types';

export interface WasteSignalDetail {
  signalId: string;
  serviceId: string;
  serviceName: string;
  domain: string;
  team: string;
  type: string;
  description: string;
  pattern: string;
  estimatedWaste: number;
  severity: string;
  detectedAt: string;
  correlatedCause: string | null;
}

export interface WasteByType {
  type: string;
  count: number;
  totalWaste: number;
}

export interface WasteSignalsResponse {
  totalWaste: number;
  signalCount: number;
  signals: WasteSignalDetail[];
  byType: WasteByType[];
  generatedAt: string;
  isSimulated?: boolean;
  dataSource?: string;
}

/** Configuração operacional do FinOps (moeda, gate, aprovadores). */
export interface FinOpsConfigurationResponse {
  currency: string;
  budgetGateEnabled: boolean;
  blockOnExceed: boolean;
  requireApproval: boolean;
  approvers: string[];
  alertThresholdPct: number;
  anomalyDetectionEnabled: boolean;
  anomalyThresholds: string | null;
  comparisonWindowDays: number;
  wasteDetectionEnabled: boolean;
  wasteThresholds: string | null;
  wasteCategories: string[];
  recommendationPolicy: string | null;
  notificationPolicy: string | null;
  showbackEnabled: boolean;
  chargebackEnabled: boolean;
  resolvedAt: string;
}

/** Pedido de aprovação de override de orçamento FinOps. */
export interface FinOpsBudgetApprovalDto {
  approvalId: string;
  releaseId: string;
  serviceName: string;
  environment: string;
  actualCost: number;
  baselineCost: number;
  costDeltaPct: number;
  currency: string;
  status: 'Pending' | 'Approved' | 'Rejected';
  requestedBy: string;
  justification: string | null;
  resolvedBy: string | null;
  comment: string | null;
  requestedAt: string;
  resolvedAt: string | null;
}

export interface FinOpsBudgetApprovalsResponse {
  items: FinOpsBudgetApprovalDto[];
}

export interface CreateBudgetApprovalRequest {
  releaseId: string;
  serviceName: string;
  environment: string;
  actualCost: number;
  baselineCost: number;
  costDeltaPct: number;
  currency: string;
  requestedBy: string;
  justification?: string;
}

export interface ResolveApprovalRequest {
  approved: boolean;
  resolvedBy: string;
  comment?: string;
}

/** Payload para avaliação do gate de orçamento antes de promover uma release. */
export interface EvaluateReleaseBudgetGateRequest {
  releaseId: string;
  serviceName: string;
  environment: string;
  actualCostPerDay: number;
  baselineCostPerDay: number;
  measurementDays: number;
}

/** Resultado da avaliação do gate de orçamento. */
export interface EvaluateReleaseBudgetGateResponse {
  releaseId: string;
  serviceName: string;
  environment: string;
  actualTotalCost: number;
  baselineTotalCost: number;
  costDelta: number;
  costDeltaPct: number;
  currency: string;
  action: 'Allow' | 'Warn' | 'Block' | 'RequireApproval';
  reason: string;
  evaluatedAt: string;
}

export interface CostContextPerDayResponse {
  serviceName: string;
  environment: string;
  actualCostPerDay: number;
  baselineCostPerDay: number;
  currency: string;
}

/** Cliente de API para FinOps contextual do módulo Governance. */
export const finOpsApi = {
  getSummary: (params?: { teamId?: string; domainId?: string; serviceId?: string; range?: string }) =>
    client.get<FinOpsSummaryResponse>('/finops/summary', { params }).then((r) => r.data),

  getServiceFinOps: (serviceId: string) =>
    client.get<ServiceFinOpsResponse>(`/finops/services/${serviceId}`).then((r) => r.data),

  getTeamFinOps: (teamId: string) =>
    client.get<TeamFinOpsResponse>(`/finops/teams/${teamId}`).then((r) => r.data),

  getDomainFinOps: (domainId: string) =>
    client.get<DomainFinOpsResponse>(`/finops/domains/${domainId}`).then((r) => r.data),

  getTrends: (params?: { dimension?: string; filterId?: string }) =>
    client.get<FinOpsTrendsResponse>('/finops/trends', { params }).then((r) => r.data),

  getWasteSignals: (params?: { serviceId?: string; teamId?: string; domainId?: string }) =>
    client.get<WasteSignalsResponse>('/finops/waste', { params }).then((r) => r.data),

  getEfficiency: (params?: { serviceId?: string; teamId?: string }) =>
    client.get<EfficiencyIndicatorsResponse>('/finops/efficiency', { params }).then((r) => r.data),

  getConfiguration: () =>
    client.get<FinOpsConfigurationResponse>('/finops/configuration').then((r) => r.data),

  getBudgetApprovals: (params?: { status?: string; serviceName?: string }) =>
    client.get<FinOpsBudgetApprovalsResponse>('/finops/budget-approvals', { params }).then((r) => r.data),

  createBudgetApproval: (data: CreateBudgetApprovalRequest) =>
    client.post('/finops/budget-approvals', data).then((r) => r.data),

  resolveApproval: (approvalId: string, data: ResolveApprovalRequest) =>
    client.put(`/finops/budget-approvals/${approvalId}/resolve`, data).then((r) => r.data),

  evaluateReleaseBudgetGate: (data: EvaluateReleaseBudgetGateRequest) =>
    client.post<EvaluateReleaseBudgetGateResponse>('/finops/releases/evaluate-budget-gate', data).then((r) => r.data),

  getCostContextPerDay: (serviceName: string, environment: string) =>
    client
      .get<CostContextPerDayResponse>(`/finops/service/${encodeURIComponent(serviceName)}/cost-context`, {
        params: { environment },
      })
      .then((r) => r.data),
};
