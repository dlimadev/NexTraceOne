/**
 * Modelo de domínio do módulo de contratos — frontend.
 *
 * Centraliza tipos que representam as entidades conceptuais do módulo.
 * Tipos que mapeiam directamente a DTOs do backend permanecem em `types/index.ts`.
 * Aqui vivem tipos semânticos e de apresentação usados pelo módulo.
 */

// ── Contract Kind ─────────────────────────────────────────────────────────────

/** Categoria de contrato suportada pelo módulo de contratos. */
export type ContractKind =
  | 'REST'
  | 'SOAP'
  | 'EVENT_API'
  | 'WORKSERVICE'
  | 'SHARED_SCHEMA'
  | 'COPYBOOK'
  | 'MQ_MESSAGE'
  | 'CICS_COMMAREA'
  | 'WEBHOOK';

/** @deprecated Use ContractKind instead. */
export type ServiceKind = ContractKind;

/** Mapeamento entre ContractKind e o valor persistido no backend. */
export const CONTRACT_KIND_MAP: Record<ContractKind, string> = {
  REST: 'RestApi',
  SOAP: 'Soap',
  EVENT_API: 'Event',
  WORKSERVICE: 'BackgroundService',
  SHARED_SCHEMA: 'SharedSchema',
  COPYBOOK: 'Copybook',
  MQ_MESSAGE: 'MqMessage',
  CICS_COMMAREA: 'CicsCommarea',
  WEBHOOK: 'Webhook',
} as const;

/** @deprecated Use CONTRACT_KIND_MAP instead. */
export const SERVICE_KIND_MAP = CONTRACT_KIND_MAP;

/** Mapeamento inverso — valor do backend para ContractKind. */
export const CONTRACT_KIND_REVERSE: Record<string, ContractKind> = {
  RestApi: 'REST',
  Soap: 'SOAP',
  Event: 'EVENT_API',
  BackgroundService: 'WORKSERVICE',
  SharedSchema: 'SHARED_SCHEMA',
  Copybook: 'COPYBOOK',
  MqMessage: 'MQ_MESSAGE',
  CicsCommarea: 'CICS_COMMAREA',
  Webhook: 'WEBHOOK',
} as const;

/** @deprecated Use CONTRACT_KIND_REVERSE instead. */
export const SERVICE_KIND_REVERSE = CONTRACT_KIND_REVERSE;

// ── Lifecycle ─────────────────────────────────────────────────────────────────

/** Estado de aprovação de uma versão de contrato ou draft. */
export type ApprovalState =
  | 'Pending'
  | 'InReview'
  | 'Approved'
  | 'Rejected'
  | 'Escalated';

/** Role de aprovação no workflow de contratos. */
export type ApprovalRole =
  | 'Author'
  | 'TechLead'
  | 'Architect'
  | 'Security'
  | 'Compliance'
  | 'ProductOwner';

/** Checklist item de aprovação. */
export interface ApprovalChecklistItem {
  role: ApprovalRole;
  state: ApprovalState;
  reviewedBy?: string;
  reviewedAt?: string;
  comment?: string;
}

// ── Operations ────────────────────────────────────────────────────────────────

/** Operação extraída de um spec (REST endpoint, SOAP operation, Event channel). */
export interface Operation {
  id: string;
  method?: string;
  path?: string;
  operationId?: string;
  summary?: string;
  description?: string;
  tags?: string[];
  deprecated?: boolean;
  requestBody?: SchemaRef;
  responses?: Record<string, SchemaRef>;
  parameters?: OperationParameter[];
}

/** Parâmetro de uma operação. */
export interface OperationParameter {
  name: string;
  in: 'query' | 'path' | 'header' | 'cookie';
  required?: boolean;
  schema?: SchemaRef;
  description?: string;
}

// ── Schemas ───────────────────────────────────────────────────────────────────

/** Referência a um schema dentro de um spec. */
export interface SchemaRef {
  name?: string;
  $ref?: string;
  type?: string;
  format?: string;
  description?: string;
  properties?: Record<string, SchemaRef>;
  items?: SchemaRef;
  required?: string[];
  enum?: string[];
}

/** Modelo de schema partilhado entre contratos. */
export interface SharedSchema {
  id: string;
  name: string;
  namespace?: string;
  schema: SchemaRef;
  usedBy: string[];
  version?: string;
  createdAt: string;
  updatedAt?: string;
}

// ── Security ──────────────────────────────────────────────────────────────────

/** Perfil de segurança de um contrato. */
export interface SecurityProfile {
  authType: 'None' | 'ApiKey' | 'OAuth2' | 'BearerToken' | 'BasicAuth' | 'mTLS' | 'SAML';
  classification: 'Public' | 'Internal' | 'Confidential' | 'Restricted';
  scopes?: string[];
  requiresEncryption?: boolean;
  lastReviewedAt?: string;
  lastReviewedBy?: string;
  score?: number;
}

// ── Knowledge ─────────────────────────────────────────────────────────────────

/** Termo de glossário associado a um contrato. */
export interface GlossaryTerm {
  id: string;
  term: string;
  definition: string;
  aliases?: string[];
  relatedTerms?: string[];
  contractId?: string;
}

/** Caso de uso documentado para um contrato. */
export interface UseCase {
  id: string;
  title: string;
  description: string;
  actor?: string;
  preconditions?: string;
  flow?: string;
  postconditions?: string;
  contractId?: string;
}

/** Exemplo de interacção com um contrato. */
export interface InteractionExample {
  id: string;
  name: string;
  description?: string;
  request?: string;
  response?: string;
  contentFormat: string;
  tags?: string[];
  contractId?: string;
}

// ── Relationships ─────────────────────────────────────────────────────────────

/** Consumidor de um contrato. */
export interface ConsumerRelationship {
  id: string;
  consumerId: string;
  consumerName: string;
  consumerType: 'Service' | 'Application' | 'External';
  contractVersionId: string;
  registeredAt: string;
}

/** Produtor de um contrato / evento. */
export interface ProducerRelationship {
  id: string;
  producerId: string;
  producerName: string;
  contractVersionId: string;
  registeredAt: string;
}

/** Dependência entre contratos ou serviços. */
export interface DependencyRelation {
  id: string;
  sourceId: string;
  sourceName: string;
  targetId: string;
  targetName: string;
  dependencyType: 'Runtime' | 'BuildTime' | 'Optional';
  direction: 'Upstream' | 'Downstream';
}

// ── Governance ────────────────────────────────────────────────────────────────

/** Resultado de verificação de uma política. */
export interface PolicyCheckResult {
  policyId: string;
  policyName: string;
  passed: boolean;
  severity: 'Error' | 'Warning' | 'Info';
  message: string;
  path?: string;
  ruleId?: string;
}

/** Entrada de auditoria para acções sobre contratos. */
export interface AuditEntry {
  id: string;
  action: string;
  performedBy: string;
  performedAt: string;
  contractVersionId?: string;
  details?: string;
  correlationId?: string;
}

// ── Diff ──────────────────────────────────────────────────────────────────────

/** Resultado de diff entre duas versões. */
export interface VersionDiff {
  baseVersionId: string;
  targetVersionId: string;
  changeLevel: 'Breaking' | 'Additive' | 'NonBreaking';
  suggestedSemVer: string;
  changes: VersionDiffChange[];
  isBreaking: boolean;
}

/** Mudança individual no diff. */
export interface VersionDiffChange {
  path: string;
  changeType: 'Added' | 'Removed' | 'Modified';
  isBreaking: boolean;
  description: string;
}

// ── Validation & Spectral ─────────────────────────────────────────────────────

/** Severidade de um issue de validação. */
export type ValidationSeverity = 'Info' | 'Hint' | 'Warning' | 'Error' | 'Blocked';

/** Fonte que gerou o issue de validação. */
export type ValidationSource = 'spectral' | 'internal' | 'canonical' | 'schema';

/** Issue individual de validação. */
export interface ValidationIssue {
  ruleId: string;
  ruleName: string;
  severity: ValidationSeverity;
  message: string;
  path: string;
  line?: number;
  column?: number;
  source: ValidationSource;
  rulesetId?: string;
  suggestedFix?: string;
}

/** Resumo consolidado de validação. */
export interface ValidationSummary {
  totalIssues: number;
  errorCount: number;
  warningCount: number;
  infoCount: number;
  hintCount: number;
  blockedCount: number;
  isPublishReady: boolean;
  isReviewReady: boolean;
  sources: string[];
  validatedAt: string;
}

/** Modo de execução do linting de contratos. */
export type ContractLintExecutionMode = 'Realtime' | 'OnSave' | 'OnDemand' | 'BeforeReview' | 'BeforePublish';
/** @deprecated Use ContractLintExecutionMode */
export type SpectralExecutionMode = ContractLintExecutionMode;

/** Comportamento de enforcement do linting de contratos. */
export type ContractLintEnforcementBehavior = 'AdvisoryOnly' | 'WarningOnly' | 'BlockingOnPublish' | 'BlockingOnReview' | 'Silent';
/** @deprecated Use ContractLintEnforcementBehavior */
export type SpectralEnforcementBehavior = ContractLintEnforcementBehavior;

/** Origem de um ruleset de linting de contratos. */
export type ContractLintRulesetOrigin = 'Platform' | 'Organization' | 'Team' | 'Imported' | 'ExternalRepository';
/** @deprecated Use ContractLintRulesetOrigin */
export type SpectralRulesetOrigin = ContractLintRulesetOrigin;

/** Ruleset de linting de contratos cadastrado no sistema. */
export interface ContractLintRuleset {
  id: string;
  name: string;
  description: string;
  version: string;
  content: string;
  origin: ContractLintRulesetOrigin;
  defaultExecutionMode: ContractLintExecutionMode;
  enforcementBehavior: ContractLintEnforcementBehavior;
  organizationId?: string;
  owner?: string;
  domain?: string;
  applicableServiceType?: string;
  applicableProtocols?: string;
  isActive: boolean;
  isDefault: boolean;
  sourceUrl?: string;
  createdAt: string;
  updatedAt: string;
}
/** @deprecated Use ContractLintRuleset */
export type SpectralRuleset = ContractLintRuleset;

// ── Canonical Entities ────────────────────────────────────────────────────────

/** Estado de uma entidade Canonical. */
export type CanonicalEntityState = 'Draft' | 'Published' | 'Deprecated' | 'Retired';

/** Entidade Canonical — schema/modelo padronizado e reutilizável. */
export interface CanonicalEntity {
  id: string;
  name: string;
  description: string;
  domain: string;
  category: string;
  owner: string;
  version: string;
  state: CanonicalEntityState;
  schemaContent: string;
  schemaFormat: string;
  aliases: string[];
  tags: string[];
  criticality: string;
  reusePolicy: string;
  organizationId?: string;
  knownUsageCount: number;
  createdAt: string;
  updatedAt: string;
}

/** Referência de uso de uma entidade Canonical num contrato. */
export interface CanonicalUsageReference {
  canonicalEntityId: string;
  canonicalEntityName: string;
  contractVersionId: string;
  apiAssetId: string;
  referencePath: string;
  usageType: string;
  isConformant: boolean;
  conformanceMessage?: string;
}

// ── Scorecard ─────────────────────────────────────────────────────────────────

/** Scorecard técnico de uma versão de contrato. Gerado pelo backend. */
export interface ContractScorecard {
  scorecardId: string;
  contractVersionId: string;
  qualityScore: number;
  completenessScore: number;
  compatibilityScore: number;
  riskScore: number;
  overallScore: number;
  operationCount: number;
  schemaCount: number;
  hasSecurityDefinitions: boolean;
  hasExamples: boolean;
  hasDescriptions: boolean;
  qualityJustification: string;
  completenessJustification: string;
  compatibilityJustification: string;
  riskJustification: string;
}

// ── Authoring ─────────────────────────────────────────────────────────────────

/** Modo de autoria suportado pelo workspace. */
export type AuthoringMode = 'visual' | 'source' | 'preview';

/** Formato do spec content. */
export type SpecFormat = 'json' | 'yaml' | 'xml';
