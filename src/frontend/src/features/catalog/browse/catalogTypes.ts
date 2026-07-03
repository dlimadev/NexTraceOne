/**
 * View-model types for the catalog browse/discovery surface.
 *
 * Honest-null map — fields investigated against the live API types
 * (src/frontend/src/types/index.ts) and serviceCatalog.ts:
 *
 * ServiceNode (AssetGraph.services[]):
 *   EXISTS   → serviceAssetId, name, domain, teamName, serviceType,
 *              criticality, lifecycleStatus
 *   ABSENT   → description/capability (present in ServiceDetail/ServiceListItem,
 *              not in the graph node), exposure/visibility (only in
 *              ServiceListItem as exposureType), health (separate NodeHealthResult)
 *
 * ApiNode (AssetGraph.apis[]):
 *   EXISTS   → apiAssetId, name, routePattern, version, visibility,
 *              consumers (ConsumerEdge[])
 *   ABSENT   → protocol (no field in ApiNode or ServiceApiSummary),
 *              hasContract/contractCount (not in any graph type)
 *
 * ServiceApiSummary (ServiceDetail.apis[]):
 *   EXISTS   → apiId, name, routePattern, version, visibility,
 *              isDecommissioned, consumerCount
 *   ABSENT   → protocol
 *
 * All absent fields are marked optional (?) in the view-models below.
 */

export type Exposure = 'Public' | 'Internal' | 'Partner';
export type Lifecycle = 'Stable' | 'Beta' | 'Deprecated' | 'Unknown';

/** API/interface consumível exposta por um serviço. */
export interface ApiVM {
  id: string;
  name: string;
  routePattern?: string;
  protocol?: string;              // ex.: REST, gRPC (honest-null — ausente nos tipos de grafo)
  exposure?: Exposure;            // de visibility
  version?: string;
  hasContract: boolean;
  consumerCount?: number;
}

/** Cartão de serviço (unidade de descoberta âncora). */
export interface ServiceVM {
  id: string;
  name: string;
  description?: string;           // capability/description — honest-null (ausente em ServiceNode)
  domain?: string;
  team?: string;
  owner?: string;                 // honest-null (technicalOwner ausente em ServiceNode)
  lifecycle: Lifecycle;           // go/no-go #1
  exposure?: Exposure;            // go/no-go #2 (agregado das apis; exposureType ausente em ServiceNode)
  health?: 'Ok' | 'Warn' | 'Down';// honest-null (requer NodeHealthResult separado)
  apis: ApiVM[];
  contractCount: number;
}

export type ResultViewMode = 'services' | 'apis';
export type Density = 'comfortable' | 'compact';
export type SortKey = 'relevance' | 'name' | 'consumers' | 'recent';

export interface CatalogFilters {
  q: string;
  domains: string[];
  protocols: string[];
  exposures: Exposure[];
  lifecycles: Lifecycle[];
  hasContract: boolean | null;
  teams: string[];
}

export interface FacetCount { value: string; label: string; count: number; }
export interface FacetGroups {
  domains: FacetCount[];
  protocols: FacetCount[];
  exposures: FacetCount[];
  lifecycles: FacetCount[];
  teams: FacetCount[];
}
