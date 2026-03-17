/**
 * Tipos do catálogo de contratos — view models enriquecidos.
 *
 * CatalogItem estende ContractListItem com campos que o backend ainda
 * não fornece (domain, owner, compliance, etc.). Mock enrichment
 * preenche esses campos de forma determinística até integração real.
 */
import type { ContractListItem } from '../types';
import type { ApprovalState } from '../types';

// ── Catalog Service Type ──────────────────────────────────────────────────────

export type CatalogServiceType =
  | 'RestApi'
  | 'Soap'
  | 'Event'
  | 'KafkaProducer'
  | 'KafkaConsumer'
  | 'BackgroundService'
  | 'SharedSchema';

// ── Catalog Item (enriched view model) ────────────────────────────────────────

export interface CatalogItem extends ContractListItem {
  name: string;
  serviceType: CatalogServiceType;
  domain: string;
  product: string;
  owner: string;
  team: string;
  approvalState: ApprovalState;
  complianceScore: number;
  criticality: 'Low' | 'Medium' | 'High' | 'Critical';
  updatedAt: string;
  hasBreakingChanges: boolean;
  violationCount: number;
  exposure: 'Internal' | 'External' | 'Partner';
  technology: string;
}

// ── Filters ───────────────────────────────────────────────────────────────────

export interface CatalogFilters {
  search: string;
  serviceType: string;
  domain: string;
  product: string;
  owner: string;
  team: string;
  lifecycle: string;
  approvalState: string;
  compliance: string;
  risk: string;
  exposure: string;
  technology: string;
  protocol: string;
}

export const EMPTY_FILTERS: CatalogFilters = {
  search: '',
  serviceType: '',
  domain: '',
  product: '',
  owner: '',
  team: '',
  lifecycle: '',
  approvalState: '',
  compliance: '',
  risk: '',
  exposure: '',
  technology: '',
  protocol: '',
};

// ── Sort ──────────────────────────────────────────────────────────────────────

export type SortField =
  | 'name'
  | 'serviceType'
  | 'semVer'
  | 'complianceScore'
  | 'lifecycleState'
  | 'updatedAt';

export type SortDirection = 'asc' | 'desc';

export interface SortConfig {
  field: SortField;
  direction: SortDirection;
}

// ── Helpers ───────────────────────────────────────────────────────────────────

export function activeFilterCount(filters: CatalogFilters): number {
  return Object.entries(filters).filter(
    ([key, val]) => key !== 'search' && val !== '',
  ).length;
}
