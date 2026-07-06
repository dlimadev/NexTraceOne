/**
 * View-model types for the contract browse/discovery surface.
 *
 * Âncora VM: CatalogItem (src/features/contracts/catalog/types.ts) — já é rico
 * com todos os campos necessários (name, semVer, domain, team, technicalOwner,
 * criticality, exposure, updatedAt, catalogServiceType, approvalState,
 * lifecycleState via ContractListItem). Não duplicar aqui.
 */

export type ContractViewMode = 'table' | 'cards';
export type ContractDensity = 'comfortable' | 'compact';
export type ContractSortKey = 'relevance' | 'name' | 'updated' | 'criticality';

export interface ContractBrowseFilters {
  q: string;
  serviceTypes: string[];
  lifecycles: string[];
  domains: string[];
  teams: string[];
  criticalities: string[];
  exposures: string[];
  approvals: string[];
}

export interface ContractFacetCount { value: string; label: string; count: number; }
export interface ContractFacetGroups {
  serviceTypes: ContractFacetCount[];
  lifecycles: ContractFacetCount[];
  domains: ContractFacetCount[];
  teams: ContractFacetCount[];
  criticalities: ContractFacetCount[];
  exposures: ContractFacetCount[];
  approvals: ContractFacetCount[];
}

export const EMPTY_CONTRACT_BROWSE_FILTERS: ContractBrowseFilters = {
  q: '', serviceTypes: [], lifecycles: [], domains: [], teams: [], criticalities: [], exposures: [], approvals: [],
};
