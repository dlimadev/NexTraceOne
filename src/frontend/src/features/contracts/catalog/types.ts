/**
 * Tipos do catálogo de contratos — view models com dados reais do backend.
 *
 * CatalogItem estende ContractListItem com campos adicionais derivados
 * no frontend para a UI (approvalState, serviceType mapeado, etc.).
 * Todos os campos de domínio (domain, team, owner, criticality) vêm do backend.
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
  | 'GraphQl'
  | 'SharedSchema';

// ── Catalog Item (view model with derived fields) ─────────────────────────────

/**
 * Item do catálogo de contratos para exibição na UI.
 * Campos core (domain, team, owner, etc.) vêm do backend.
 * Campos derivados (approvalState) são calculados no frontend a partir do lifecycle.
 */
export interface CatalogItem extends ContractListItem {
  name: string;
  semVer: string;
  domain: string;
  team: string;
  technicalOwner: string;
  criticality: string;
  exposure: string;
  updatedAt: string;
  catalogServiceType: CatalogServiceType;
  approvalState: ApprovalState;
}

/**
 * Converte ContractListItem em CatalogItem, derivando campos da UI.
 */
export function toCatalogItem(item: ContractListItem): CatalogItem {
  const name = item.name ?? item.apiName ?? item.apiAssetId;
  const semVer = item.semVer ?? item.version ?? '0.0.0';
  const team = item.team ?? item.teamName ?? '';
  const exposure = item.exposure ?? item.exposureType ?? '';
  const updatedAt = item.updatedAt ?? item.createdAt ?? new Date(0).toISOString();
  const serviceType = item.serviceType ?? item.protocol;

  return {
    ...item,
    name,
    semVer,
    domain: item.domain ?? '',
    team,
    technicalOwner: item.technicalOwner ?? '',
    criticality: item.criticality ?? '',
    exposure,
    updatedAt,
    catalogServiceType: mapServiceType(serviceType, item.protocol),
    approvalState: deriveApprovalState(item.lifecycleState),
  };
}

/**
 * Mapeia o serviceType do backend para o tipo visual do catálogo.
 */
function mapServiceType(serviceType: string, protocol: string): CatalogServiceType {
  // Primeiro tenta pelo serviceType do backend
  switch (serviceType) {
    case 'RestApi': return 'RestApi';
    case 'Soap': return 'Soap';
    case 'KafkaProducer': return 'KafkaProducer';
    case 'KafkaConsumer': return 'KafkaConsumer';
    case 'BackgroundService': return 'BackgroundService';
    case 'GraphQl': return 'GraphQl';
  }
  // Fallback pelo protocol se serviceType não reconhecido
  switch (protocol) {
    case 'OpenApi':
    case 'Swagger': return 'RestApi';
    case 'Wsdl': return 'Soap';
    case 'AsyncApi': return 'Event';
    case 'Protobuf': return 'BackgroundService';
    case 'GraphQl': return 'GraphQl';
    default: return 'RestApi';
  }
}

/**
 * Deriva o estado de aprovação a partir do estado do ciclo de vida.
 */
function deriveApprovalState(lifecycleState: string): ApprovalState {
  switch (lifecycleState) {
    case 'Draft': return 'Pending';
    case 'InReview': return 'InReview';
    case 'Approved':
    case 'Locked':
    case 'Deprecated':
    case 'Sunset':
    case 'Retired':
      return 'Approved';
    default: return 'Pending';
  }
}

// ── Filters ───────────────────────────────────────────────────────────────────

export interface CatalogFilters {
  search: string;
  serviceType: string;
  domain: string;
  owner: string;
  team: string;
  lifecycle: string;
  approvalState: string;
  risk: string;
  exposure: string;
  protocol: string;
}

export const EMPTY_FILTERS: CatalogFilters = {
  search: '',
  serviceType: '',
  domain: '',
  owner: '',
  team: '',
  lifecycle: '',
  approvalState: '',
  risk: '',
  exposure: '',
  protocol: '',
};

// ── Sort ──────────────────────────────────────────────────────────────────────

export type SortField =
  | 'name'
  | 'serviceType'
  | 'semVer'
  | 'criticality'
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

/**
 * Extrai opções dinâmicas de filtro a partir dos itens do catálogo.
 */
export function extractFilterOptions(items: CatalogItem[]): {
  domains: string[];
  teams: string[];
  owners: string[];
  serviceTypes: CatalogServiceType[];
  exposures: string[];
} {
  const domains = [...new Set(items.map(i => i.domain).filter(Boolean))].sort();
  const teams = [...new Set(items.map(i => i.team).filter(Boolean))].sort();
  const owners = [...new Set(items.map(i => i.technicalOwner).filter(Boolean))].sort();
  const serviceTypes = [...new Set(items.map(i => i.catalogServiceType))] as CatalogServiceType[];
  const exposures = [...new Set(items.map(i => i.exposure).filter(Boolean))].sort();

  return { domains, teams, owners, serviceTypes, exposures };
}
