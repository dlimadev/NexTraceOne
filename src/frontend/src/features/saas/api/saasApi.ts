import client from '../../../api/client';

// ── Types ────────────────────────────────────────────────────────────────────

export type TenantPlan = 'Trial' | 'Starter' | 'Professional' | 'Enterprise';
export type LicenseStatus = 'Active' | 'Trial' | 'Suspended' | 'Expired';
export type AgentStatus = 'Active' | 'Inactive' | 'Decommissioned';
export type AlertFiringStatus = 'Firing' | 'Resolved' | 'Silenced';

export interface TenantLicenseResponse {
  tenantId: string;
  plan: TenantPlan;
  status: LicenseStatus;
  includedHostUnits: number;
  currentHostUnits: number;
  overageHostUnits: number;
  validFrom: string;
  validUntil: string | null;
  billingCycleStart: string;
  capabilities: string[];
}

export interface ProvisionLicenseRequest {
  plan: TenantPlan;
  includedHostUnits: number;
}

export interface ProvisionLicenseResponse {
  tenantId: string;
  licenseId: string;
  plan: TenantPlan;
  licenseProvisioned: boolean;
}

export interface AgentRegistrationDto {
  id: string;
  tenantId: string;
  hostUnitId: string;
  hostname: string;
  agentVersion: string;
  cpuCores: number;
  ramGb: number;
  hostUnits: number;
  status: AgentStatus;
  lastHeartbeatAt: string | null;
  registeredAt: string;
}

export interface ListAgentRegistrationsResponse {
  items: AgentRegistrationDto[];
  totalHostUnits: number;
  activeCount: number;
}

export interface RecordHeartbeatRequest {
  hostUnitId: string;
  hostname: string;
  agentVersion: string;
  cpuCores: number;
  ramGb: number;
}

export interface RecordHeartbeatResponse {
  hostUnitId: string;
  hostUnits: number;
  status: AgentStatus;
}

export interface AlertFiringRecordDto {
  id: string;
  tenantId: string;
  alertRuleId: string;
  alertRuleName: string;
  severity: string;
  conditionSummary: string;
  serviceName: string | null;
  status: AlertFiringStatus;
  firedAt: string;
  resolvedAt: string | null;
}

export interface AlertFiringHistoryResponse {
  items: AlertFiringRecordDto[];
  firingCount: number;
  resolvedCount: number;
  silencedCount: number;
}

export interface ResolveAlertRequest {
  action: 'resolve' | 'silence';
}

export interface ProvisionTenantRequest {
  name: string;
  slug: string;
  plan: TenantPlan;
  includedHostUnits: number;
  legalName?: string;
  taxId?: string;
}

export interface ProvisionTenantResponse {
  tenantId: string;
  name: string;
  slug: string;
  licenseId: string | null;
  plan: TenantPlan;
  licenseProvisioned: boolean;
}

// ── API object ────────────────────────────────────────────────────────────────

export const saasApi = {
  getLicense: (): Promise<TenantLicenseResponse> =>
    client.get<TenantLicenseResponse>('/identity/license').then((r) => r.data),

  provisionLicense: (req: ProvisionLicenseRequest): Promise<ProvisionLicenseResponse> =>
    client.post<ProvisionLicenseResponse>('/identity/license/provision', req).then((r) => r.data),

  listAgents: (): Promise<ListAgentRegistrationsResponse> =>
    client.get<ListAgentRegistrationsResponse>('/identity/agents').then((r) => r.data),

  recordHeartbeat: (req: RecordHeartbeatRequest): Promise<RecordHeartbeatResponse> =>
    client.post<RecordHeartbeatResponse>('/identity/agents/heartbeat', req).then((r) => r.data),

  listAlerts: (params?: {
    status?: AlertFiringStatus;
    days?: number;
  }): Promise<AlertFiringHistoryResponse> =>
    client
      .get<AlertFiringHistoryResponse>('/identity/alerts/firing', { params })
      .then((r) => r.data),

  resolveAlert: (recordId: string, req: ResolveAlertRequest): Promise<void> =>
    client
      .post(`/identity/alerts/firing/${recordId}/resolve`, req)
      .then(() => undefined),

  provisionTenant: (req: ProvisionTenantRequest): Promise<ProvisionTenantResponse> =>
    client
      .post<ProvisionTenantResponse>('/identity/admin/tenants/provision', req)
      .then((r) => r.data),
};
