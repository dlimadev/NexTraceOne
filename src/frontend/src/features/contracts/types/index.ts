/**
 * Barrel export dos tipos do módulo de contratos.
 * Re-exporta tipos de domínio do módulo e tipos do backend que são relevantes.
 */

// ── Tipos de domínio do módulo ────────────────────────────────────────────────
export type {
  ServiceKind,
  ApprovalState,
  ApprovalRole,
  ApprovalChecklistItem,
  Operation,
  OperationParameter,
  SchemaRef,
  SharedSchema,
  SecurityProfile,
  GlossaryTerm,
  UseCase,
  InteractionExample,
  ConsumerRelationship,
  ProducerRelationship,
  DependencyRelation,
  PolicyCheckResult,
  AuditEntry,
  VersionDiff,
  VersionDiffChange,
  AuthoringMode,
  SpecFormat,
  ValidationSeverity,
  ValidationSource,
  ValidationIssue,
  ValidationSummary,
  SpectralExecutionMode,
  SpectralEnforcementBehavior,
  SpectralRulesetOrigin,
  SpectralRuleset,
  CanonicalEntityState,
  CanonicalEntity,
  CanonicalUsageReference,
} from './domain';

export { SERVICE_KIND_MAP, SERVICE_KIND_REVERSE } from './domain';

// ── Tipos de workspace ────────────────────────────────────────────────────────
export type {
  WorkspaceSectionId,
  WorkspaceSectionGroup,
  WorkspaceSectionDef,
} from './workspace';

// ── Re-exports de tipos do backend (types/index.ts) ──────────────────────────
// Centralizados aqui para que o módulo importe apenas de './types'
export type {
  ContractProtocol,
  ContractLifecycleState,
  ContractType,
  ContractVersion,
  ContractVersionDetail,
  ContractProvenance,
  ContractClassification,
  ContractRuleViolation,
  ContractArtifact,
  ContractIntegrityResult,
  ContractSearchResult,
  ContractSyncItem,
  ContractSyncResponse,
  ContractListItem,
  ContractListResponse,
  ContractProtocolCount,
  ContractsSummary,
  ServiceContractItem,
  ServiceContractsResponse,
  ContractDraft,
  ContractDraftExample,
  ContractReviewEntry,
  DraftListResponse,
  DraftStatus,
  ReviewDecision,
  SemanticDiff,
  ChangeEntry,
  ContractDiffResult,
  SignatureVerificationResult,
  ServiceListItem,
  SoapContractDetail,
  WsdlImportResponse,
  SoapDraftCreateResponse,
} from '../../../types';
