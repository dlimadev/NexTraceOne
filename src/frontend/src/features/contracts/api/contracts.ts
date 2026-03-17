import client from '../../../api/client';
import type {
  ContractVersion,
  ContractVersionDetail,
  SignatureVerificationResult,
  SemanticDiff,
  ContractProtocol,
  ContractRuleViolation,
  ContractSearchResult,
  ContractIntegrityResult,
  ContractSyncItem,
  ContractSyncResponse,
  ContractListResponse,
  ContractsSummary,
  ServiceContractsResponse,
} from '../types';
import type {
  ValidationIssue,
  ValidationSummary,
  SpectralRuleset,
  SpectralExecutionMode,
  SpectralEnforcementBehavior,
  SpectralRulesetOrigin,
  CanonicalEntity,
  CanonicalUsageReference,
} from '../types/domain';

/**
 * Detecta o formato da especificação (json, yaml ou xml) a partir do conteúdo bruto.
 */
function detectFormat(content: string): 'json' | 'yaml' | 'xml' {
  const trimmed = (content || '').trim();
  if (!trimmed) return 'json';
  if (trimmed.startsWith('{') || trimmed.startsWith('[')) return 'json';
  if (trimmed.startsWith('<') || trimmed.startsWith('<?xml')) return 'xml';
  return 'yaml';
}

export const contractsApi = {
  importContract: (data: {
    apiAssetId: string;
    content: string;
    version: string;
    protocol?: ContractProtocol;
    format?: string;
    importedFrom?: string;
  }) =>
    client.post<{ id: string }>('/contracts', {
      apiAssetId: data.apiAssetId,
      semVer: data.version,
      specContent: data.content,
      format: data.format || detectFormat(data.content),
      importedFrom: data.importedFrom || 'upload',
      protocol: data.protocol || 'OpenApi',
    }).then((r) => r.data),

  createVersion: (data: {
    apiAssetId: string;
    content: string;
    version: string;
    protocol?: ContractProtocol;
    format?: string;
    importedFrom?: string;
  }) =>
    client.post<{ id: string }>('/contracts/versions', {
      apiAssetId: data.apiAssetId,
      semVer: data.version,
      specContent: data.content,
      format: data.format || detectFormat(data.content),
      importedFrom: data.importedFrom || 'upload',
      protocol: data.protocol,
    }).then((r) => r.data),

  computeDiff: (fromVersionId: string, toVersionId: string) =>
    client
      .post<SemanticDiff>('/contracts/diff', { baseVersionId: fromVersionId, targetVersionId: toVersionId })
      .then((r) => r.data),

  getClassification: (contractVersionId: string) =>
    client
      .get(`/contracts/${contractVersionId}/classification`)
      .then((r) => r.data),

  suggestVersion: (apiAssetId: string, changeLevel: number) =>
    client
      .get<{ suggestedVersion: string }>('/contracts/suggest-version', {
        params: { apiAssetId, changeLevel },
      })
      .then((r) => r.data),

  getHistory: (apiAssetId: string) =>
    client
      .get<{ apiAssetId: string; versions: ContractVersion[] }>(`/contracts/history/${apiAssetId}`)
      .then((r) => r.data.versions),

  getDetail: (contractVersionId: string) =>
    client
      .get<ContractVersionDetail>(`/contracts/${contractVersionId}/detail`)
      .then((r) => r.data),

  lockVersion: (contractVersionId: string, lockedBy: string) =>
    client
      .post(`/contracts/${contractVersionId}/lock`, { lockedBy })
      .then((r) => r.data),

  transitionLifecycle: (contractVersionId: string, newState: string) =>
    client
      .post(`/contracts/${contractVersionId}/lifecycle`, { contractVersionId, newState })
      .then((r) => r.data),

  deprecateVersion: (contractVersionId: string, deprecationNotice: string, sunsetDate?: string) =>
    client
      .post(`/contracts/${contractVersionId}/deprecate`, { contractVersionId, deprecationNotice, sunsetDate })
      .then((r) => r.data),

  signVersion: (contractVersionId: string) =>
    client
      .post(`/contracts/${contractVersionId}/sign`, { contractVersionId })
      .then((r) => r.data),

  verifySignature: (contractVersionId: string) =>
    client
      .get<SignatureVerificationResult>(`/contracts/${contractVersionId}/verify`)
      .then((r) => r.data),

  exportVersion: (contractVersionId: string) =>
    client.get<{ specContent: string; format: string }>(`/contracts/${contractVersionId}/export`).then((r) => r.data),

  searchContracts: (params: {
    protocol?: ContractProtocol;
    lifecycleState?: string;
    apiAssetId?: string;
    searchTerm?: string;
    page?: number;
    pageSize?: number;
  }) =>
    client.get<ContractSearchResult>('/contracts/search', { params }).then((r) => r.data),

  listRuleViolations: (contractVersionId: string) =>
    client.get<{ contractVersionId: string; violations: ContractRuleViolation[] }>(`/contracts/${contractVersionId}/violations`).then((r) => r.data.violations),

  validateIntegrity: (contractVersionId: string) =>
    client
      .get<ContractIntegrityResult>(`/contracts/${contractVersionId}/validate`)
      .then((r) => r.data),

  syncContracts: (data: {
    items: ContractSyncItem[];
    sourceSystem: string;
    correlationId?: string;
  }) =>
    client
      .post<ContractSyncResponse>('/contracts/sync', data)
      .then((r) => r.data),

  listContracts: (params?: {
    protocol?: string;
    lifecycleState?: string;
    searchTerm?: string;
    page?: number;
    pageSize?: number;
  }) =>
    client.get<ContractListResponse>('/contracts/list', { params }).then((r) => r.data),

  getContractsSummary: () =>
    client.get<ContractsSummary>('/contracts/summary').then((r) => r.data),

  listContractsByService: (serviceId: string) =>
    client.get<ServiceContractsResponse>(`/contracts/by-service/${serviceId}`).then((r) => r.data),

  // ── Validation ──────────────────────────────────────────────────────────────

  executeValidation: (contractVersionId: string) =>
    client.post<{ issues: ValidationIssue[]; summary: ValidationSummary }>(`/contracts/${contractVersionId}/validate/spectral`).then((r) => r.data),

  getValidationSummary: (contractVersionId: string) =>
    client.get<ValidationSummary>(`/contracts/${contractVersionId}/validation-summary`).then((r) => r.data),

  validateSpecContent: (data: { specContent: string; protocol: ContractProtocol; rulesetIds?: string[] }) =>
    client.post<{ issues: ValidationIssue[]; summary: ValidationSummary }>('/contracts/validate-spec', data).then((r) => r.data),

  // ── Spectral Rulesets ───────────────────────────────────────────────────────

  listSpectralRulesets: (params?: { origin?: SpectralRulesetOrigin; isActive?: boolean; domain?: string }) =>
    client.get<{ items: SpectralRuleset[]; total: number }>('/contracts/spectral/rulesets', { params }).then((r) => r.data),

  getSpectralRuleset: (rulesetId: string) =>
    client.get<SpectralRuleset>(`/contracts/spectral/rulesets/${rulesetId}`).then((r) => r.data),

  createSpectralRuleset: (data: {
    name: string;
    description: string;
    content: string;
    origin: SpectralRulesetOrigin;
    defaultExecutionMode: SpectralExecutionMode;
    enforcementBehavior: SpectralEnforcementBehavior;
    organizationId?: string;
    owner?: string;
    domain?: string;
    applicableServiceType?: string;
    applicableProtocols?: string;
    sourceUrl?: string;
  }) =>
    client.post<{ id: string }>('/contracts/spectral/rulesets', data).then((r) => r.data),

  updateSpectralRuleset: (rulesetId: string, data: Partial<SpectralRuleset>) =>
    client.put(`/contracts/spectral/rulesets/${rulesetId}`, data).then((r) => r.data),

  deleteSpectralRuleset: (rulesetId: string) =>
    client.delete(`/contracts/spectral/rulesets/${rulesetId}`).then((r) => r.data),

  toggleSpectralRuleset: (rulesetId: string, isActive: boolean) =>
    client.patch(`/contracts/spectral/rulesets/${rulesetId}/status`, { isActive }).then((r) => r.data),

  // ── Canonical Entities ──────────────────────────────────────────────────────

  listCanonicalEntities: (params?: { domain?: string; state?: string; category?: string; searchTerm?: string }) =>
    client.get<{ items: CanonicalEntity[]; total: number }>('/contracts/canonical-entities', { params }).then((r) => r.data),

  getCanonicalEntity: (entityId: string) =>
    client.get<CanonicalEntity>(`/contracts/canonical-entities/${entityId}`).then((r) => r.data),

  createCanonicalEntity: (data: {
    name: string;
    description: string;
    domain: string;
    category: string;
    owner: string;
    schemaContent: string;
    schemaFormat: string;
    criticality?: string;
    reusePolicy?: string;
    tags?: string[];
    aliases?: string[];
  }) =>
    client.post<{ id: string }>('/contracts/canonical-entities', data).then((r) => r.data),

  updateCanonicalEntity: (entityId: string, data: Partial<CanonicalEntity>) =>
    client.put(`/contracts/canonical-entities/${entityId}`, data).then((r) => r.data),

  getCanonicalEntityUsages: (entityId: string) =>
    client.get<{ usages: CanonicalUsageReference[] }>(`/contracts/canonical-entities/${entityId}/usages`).then((r) => r.data.usages),

  promoteToCanonical: (data: { sourceContractVersionId: string; schemaName: string; name: string; domain: string; category: string }) =>
    client.post<{ id: string }>('/contracts/canonical-entities/promote', data).then((r) => r.data),
};
