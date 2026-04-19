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
  SoapContractDetail,
  WsdlImportResponse,
  EventContractDetail,
  AsyncApiImportResponse,
  BackgroundServiceContractDetail,
  BackgroundServiceRegisterResponse,
  ContractDeploymentsResponse,
  ContractSubscribersResponse,
} from '../types';
import type {
  ValidationIssue,
  ValidationSummary,
  SpectralRuleset,
  CanonicalEntity,
  CanonicalUsageReference,
  ContractScorecard,
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
    client.post<{
      totalIssues: number;
      errorCount: number;
      warningCount: number;
      infoCount: number;
      hintCount: number;
      isValid: boolean;
      fingerprint: string;
      sources: string[];
      issues: ValidationIssue[];
    }>('/contracts/validate-spec', data).then((r) => r.data),

  parseSpecPreview: (data: { specContent: string; protocol: string }) =>
    client.post('/contracts/parse-preview', data).then((r) => r.data),

  // ── Spectral Rulesets ───────────────────────────────────────────────────────

  listSpectralRulesets: (params?: { isActive?: boolean }) =>
    client.get<{
      rulesets: Array<{
        rulesetId: string;
        name: string;
        description: string;
        content: string;
        rulesetType: string;
        isActive: boolean;
        createdAt: string;
      }>;
      totalCount: number;
    }>('/contracts/spectral/rulesets', { params }).then((r) => ({
      items: r.data.rulesets.map((item) => ({
        id: item.rulesetId,
        name: item.name,
        description: item.description,
        content: item.content,
        rulesetType: item.rulesetType,
        isActive: item.isActive,
        isDefault: item.rulesetType === 'Default',
        createdAt: item.createdAt,
        updatedAt: item.createdAt,
        version: '',
        origin: 'Platform' as const,
        defaultExecutionMode: 'OnDemand' as const,
        enforcementBehavior: 'AdvisoryOnly' as const,
      })) as SpectralRuleset[],
      total: r.data.totalCount,
    })),

  getSpectralRuleset: (rulesetId: string) =>
    client.get<SpectralRuleset>(`/contracts/spectral/rulesets/${rulesetId}`).then((r) => r.data),

  createSpectralRuleset: (data: {
    name: string;
    description: string;
    content: string;
    rulesetType: string;
  }) =>
    client.post<{ rulesetId: string }>('/contracts/spectral/rulesets', data).then((r) => r.data),

  updateSpectralRuleset: (rulesetId: string, data: Partial<SpectralRuleset>) =>
    client.put(`/contracts/spectral/rulesets/${rulesetId}`, data).then((r) => r.data),

  deleteSpectralRuleset: (rulesetId: string) =>
    client.delete(`/contracts/spectral/rulesets/${rulesetId}`).then((r) => r.data),

  archiveSpectralRuleset: (rulesetId: string) =>
    client.put(`/contracts/spectral/rulesets/${rulesetId}/archive`).then((r) => r.data),

  activateSpectralRuleset: (rulesetId: string) =>
    client.put(`/contracts/spectral/rulesets/${rulesetId}/activate`).then((r) => r.data),

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

  // ── SOAP/WSDL workflow ─────────────────────────────────────────────

  /**
   * Importa um contrato WSDL e extrai metadados SOAP específicos.
   * Cria ContractVersion com Protocol=Wsdl e popula SoapContractDetail.
   */
  importWsdl: (data: {
    apiAssetId: string;
    semVer: string;
    wsdlContent: string;
    importedFrom: string;
    endpointUrl?: string;
    wsdlSourceUrl?: string;
    soapVersion?: '1.1' | '1.2';
  }) =>
    client.post<WsdlImportResponse>('/contracts/wsdl/import', {
      apiAssetId: data.apiAssetId,
      semVer: data.semVer,
      wsdlContent: data.wsdlContent,
      importedFrom: data.importedFrom,
      endpointUrl: data.endpointUrl,
      wsdlSourceUrl: data.wsdlSourceUrl,
      soapVersion: data.soapVersion,
    }).then((r) => r.data),

  /**
   * Obtém os detalhes SOAP/WSDL específicos de uma versão de contrato publicada.
   */
  getSoapContractDetail: (contractVersionId: string) =>
    client.get<SoapContractDetail>(`/contracts/${contractVersionId}/soap-detail`).then((r) => r.data),

  // ── Event Contracts / AsyncAPI workflow ──────────────────────────

  /**
   * Importa uma spec AsyncAPI e extrai metadados específicos de evento.
   * Cria ContractVersion com Protocol=AsyncApi e popula EventContractDetail.
   */
  importAsyncApi: (data: {
    apiAssetId: string;
    semVer: string;
    asyncApiContent: string;
    importedFrom: string;
    defaultContentType?: string;
  }) =>
    client.post<AsyncApiImportResponse>('/contracts/asyncapi/import', {
      apiAssetId: data.apiAssetId,
      semVer: data.semVer,
      asyncApiContent: data.asyncApiContent,
      importedFrom: data.importedFrom,
      defaultContentType: data.defaultContentType,
    }).then((r) => r.data),

  /**
   * Obtém os detalhes AsyncAPI específicos de uma versão de contrato de evento publicada.
   */
  getEventContractDetail: (contractVersionId: string) =>
    client.get<EventContractDetail>(`/contracts/${contractVersionId}/event-detail`).then((r) => r.data),

  // ── Background Service Contracts workflow ─────────────────────────

  /**
   * Regista um Background Service Contract com metadados específicos do processo.
   * Cria ContractVersion (ContractType=BackgroundService) + BackgroundServiceContractDetail.
   */
  registerBackgroundService: (data: {
    apiAssetId: string;
    semVer: string;
    serviceName: string;
    category: string;
    triggerType: string;
    scheduleExpression?: string;
    timeoutExpression?: string;
    allowsConcurrency?: boolean;
    inputsJson?: string;
    outputsJson?: string;
    sideEffectsJson?: string;
    specContent?: string;
  }) =>
    client.post<BackgroundServiceRegisterResponse>('/contracts/background-services/register', data)
      .then((r) => r.data),

  /**
   * Obtém os detalhes de Background Service de uma versão de contrato publicada.
   */
  getBackgroundServiceContractDetail: (contractVersionId: string) =>
    client.get<BackgroundServiceContractDetail>(`/contracts/${contractVersionId}/background-service-detail`)
      .then((r) => r.data),

  // ── Scorecard ─────────────────────────────────────────────────────

  /**
   * Gera o scorecard técnico de uma versão de contrato.
   * Retorna scores de qualidade, completude, compatibilidade e risco.
   */
  getScorecard: (contractVersionId: string) =>
    client.get<ContractScorecard>(`/contracts/${contractVersionId}/scorecard`).then((r) => r.data),

  /**
   * Lista deployments registados de uma versão de contrato por ambiente.
   * Alimenta o Change Intelligence — rastreabilidade de mudanças por ambiente.
   */
  getDeployments: (contractVersionId: string) =>
    client.get<ContractDeploymentsResponse>(`/contracts/${contractVersionId}/deployments`).then((r) => r.data),

  /**
   * Lista subscrições formais de uma API via Developer Portal.
   * Permite ao produtor ver quem subscreveu para receber notificações de mudanças.
   */
  getSubscribers: (apiAssetId: string) =>
    client.get<ContractSubscribersResponse>(`/developerportal/catalog/${apiAssetId}/consumers`).then((r) => r.data),

  // ── Phase 5 — Mock, Guidelines, CDCT, Multi-Format Export ──────────

  /** Gera configuração de mock a partir do spec de uma versão de contrato. */
  getMockConfig: (contractVersionId: string) =>
    client.get(`/contracts/versions/${contractVersionId}/mock-config`).then((r) => r.data),

  /** Avalia directrizes de design API (score 0-100 + violações). */
  evaluateDesignGuidelines: (contractVersionId: string) =>
    client.get(`/contracts/versions/${contractVersionId}/design-guidelines`).then((r) => r.data),

  /** Exporta contrato em formato específico (openapi-yaml, postman-v21, curl, etc.). */
  exportMultiFormat: (contractVersionId: string, format: string) =>
    client.get(`/contracts/versions/${contractVersionId}/export-format`, { params: { format } }).then((r) => r.data),

  /** Regista expectativa de consumidor para CDCT. */
  registerConsumerExpectation: (apiAssetId: string, data: { consumerServiceName: string; consumerDomain: string; expectedSubsetJson: string; notes?: string }) =>
    client.post(`/contracts/${apiAssetId}/consumer-expectations`, data).then((r) => r.data),

  /** Lista expectativas de consumidores registadas para um contrato. */
  getConsumerExpectations: (apiAssetId: string) =>
    client.get(`/contracts/${apiAssetId}/consumer-expectations`).then((r) => r.data),

  /** Verifica compatibilidade provider/consumer (CDCT). */
  verifyCdct: (apiAssetId: string, versionId: string) =>
    client.get(`/contracts/${apiAssetId}/versions/${versionId}/cdct-verify`).then((r) => r.data),

  // ── Phase 6 — Intelligence & Governance ───────────────────────────

  /** Dashboard de saúde dos contratos com métricas agregadas. */
  getHealthDashboard: (params?: { domain?: string; contractType?: string; page?: number; pageSize?: number }) =>
    client.get(`/contracts/health-dashboard`, { params }).then((r) => r.data),

  /** Changelog semântico enriquecido de um contrato. */
  getSemanticChangelog: (apiAssetId: string, fromVersion?: string, toVersion?: string) =>
    client.get(`/contracts/${apiAssetId}/semantic-changelog`, { params: { fromVersion, toVersion } }).then((r) => r.data),

  /** Sugere schemas de request/response a partir do contexto do endpoint. */
  suggestSchema: (method: string, path: string, domain?: string, serviceAssetId?: string) =>
    client.get(`/contracts/suggest-schema`, { params: { method, path, domain, serviceAssetId } }).then((r) => r.data),

  /** Inicia workflow de deprecação de contrato com sunset date. */
  initiateDeprecation: (apiAssetId: string, data: { sunsetDate: string; replacementApiAssetId?: string; migrationGuide?: string; reason?: string }) =>
    client.post(`/contracts/${apiAssetId}/deprecate`, data).then((r) => r.data),

  /** Progresso da migração de um contrato deprecated. */
  getDeprecationProgress: (apiAssetId: string) =>
    client.get(`/contracts/${apiAssetId}/deprecation-progress`).then((r) => r.data),

  /** Propaga impacto de mudança de entidade canónica para contratos referenciados. */
  propagateCanonicalChange: (entityId: string, newVersion: string) =>
    client.post(`/contracts/canonical/${entityId}/propagate`, { newVersion }).then((r) => r.data),

  // ── Phase 4 — Innovation ───────────────────────────────────────────

  /** Detecta desvio entre o contrato publicado e os traces OTel observados. */
  detectContractDrift: (apiAssetId: string, observedOperations: Array<{ method: string; path: string }>) =>
    client.post(`/contracts/${apiAssetId}/drift`, observedOperations).then((r) => r.data),

  /** Linha do tempo de health score do contrato por versão. */
  getContractHealthTimeline: (apiAssetId: string, maxVersions?: number) =>
    client.get(`/contracts/${apiAssetId}/health/timeline`, { params: { maxVersions } }).then((r) => r.data),

  /** Benchmark de maturidade entre equipas e domínios. */
  getServiceMaturityBenchmark: (params?: { domain?: string; teamName?: string; topN?: number }) =>
    client.get(`/services/maturity/benchmark`, { params }).then((r) => r.data),

  /** Revisão de contrato assistida por IA. */
  reviewContractDraft: (data: { tenantId: string; draftId: string; contractContent: string; contractType: string; serviceName: string; preferredProvider?: string }) =>
    client.post(`/aiorchestration/contracts/review`, data).then((r) => r.data),

  /** Análise em cascata de impacto de entidade canónica (multi-nível). */
  getCanonicalEntityImpactCascade: (entityId: string, maxDepth?: number) =>
    client.get(`/catalog/canonical-entities/${entityId}/impact/cascade`, { params: { maxDepth } }).then((r) => r.data),

  // ── Contract Migration Patch ───────────────────────────────────────

  /** Gera sugestões de código para migrar provedor e/ou consumidores quando um contrato muda de versão. */
  generateMigrationPatch: (data: {
    baseVersionId: string;
    targetVersionId: string;
    target?: 'provider' | 'consumer' | 'all';
    language?: string;
  }) =>
    client.post(`/contracts/migration-patch`, {
      baseVersionId: data.baseVersionId,
      targetVersionId: data.targetVersionId,
      target: data.target ?? 'all',
      language: data.language ?? 'C#',
    }).then((r) => r.data as MigrationPatchResult),
};
