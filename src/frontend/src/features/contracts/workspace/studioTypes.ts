/**
 * Tipos enriquecidos para o studio de contratos.
 *
 * O backend ContractVersionDetail não fornece todos os campos necessários
 * para a experiência completa do studio (domain, owner, compliance, etc.).
 * Esta camada define o view-model completo que o studio consome.
 *
 * Pronto para substituição real: basta popular StudioContract directamente
 * a partir do backend e remover a camada de mock enrichment.
 */
import type { ApprovalState, ApprovalChecklistItem, PolicyCheckResult } from '../types/domain';

/** View-model real do workspace/portal alimentado por dados do backend e derivações honestas. */
export interface StudioContract {
  // ── Identity ──
  id: string;
  apiAssetId: string;
  technicalName: string;
  friendlyName: string;
  functionalDescription: string;
  technicalDescription: string;

  // ── Version ──
  semVer: string;
  format: string;
  protocol: string;
  specContent: string;

  // ── Lifecycle ──
  lifecycleState: string;
  isLocked: boolean;
  lockedAt?: string;
  lockedBy?: string;
  signedBy?: string;
  signedAt?: string;
  fingerprint?: string;
  algorithm?: string;

  // ── Organization ──
  serviceType: string;
  domain: string;
  capability: string;
  product: string;
  owner: string;
  team: string;
  visibility: string;
  criticality: string;
  dataClassification: string;
  tags: string[];

  // ── SLA / SLO ──
  sla: string;
  slo: string;
  externalLinks: string[];

  // ── Governance ──
  approvalState?: ApprovalState;
  complianceScore?: number | null;
  approvalChecklist: ApprovalChecklistItem[];
  policyChecks: PolicyCheckResult[];

  // ── Provenance ──
  importedFrom?: string;
  provenance?: string;
  deprecationNotice?: string;
  sunsetDate?: string;
  createdAt: string;

  // ── Risks ──
  risks: StudioRisk[];

  // ── Activity ──
  recentActivity: StudioActivityItem[];

  // ── Relationships ──
  consumers: StudioRelationship[];
  producers: StudioRelationship[];
  dependencies: StudioDependency[];
}

/** Risco identificado no contexto de um contrato. */
export interface StudioRisk {
  id: string;
  level: 'Critical' | 'High' | 'Medium' | 'Low';
  description: string;
  category: string;
}

/** Entrada de actividade recente no studio. */
export interface StudioActivityItem {
  id: string;
  action: string;
  actor: string;
  timestamp: string;
  detail?: string;
}

/** Relação consumer/producer. */
export interface StudioRelationship {
  id: string;
  name: string;
  type: string;
  registeredAt: string;
}

/** Dependência entre contratos/serviços. */
export interface StudioDependency {
  id: string;
  name: string;
  direction: 'Upstream' | 'Downstream';
  type: 'Runtime' | 'BuildTime' | 'Optional';
}
