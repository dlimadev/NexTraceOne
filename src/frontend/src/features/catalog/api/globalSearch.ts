import client from '../../../api/client';

export interface SearchResultItem {
  entityId: string;
  entityType: string;
  title: string;
  subtitle: string | null;
  owner: string | null;
  status: string | null;
  route: string;
  relevanceScore: number;
}

export interface GlobalSearchResponse {
  items: SearchResultItem[];
  facetCounts: Record<string, number>;
  totalResults: number;
}

export interface GlobalSearchParams {
  q: string;
  scope?: string;
  persona?: string;
  maxResults?: number;
}

/** Cliente de API para pesquisa global unificada do NexTraceOne. */
export const globalSearchApi = {
  /** Pesquisa global por serviços, contratos, runbooks e documentação. */
  search: (params: GlobalSearchParams) =>
    client.get<GlobalSearchResponse>('/source-of-truth/global-search', { params }).then((r) => r.data),
};
