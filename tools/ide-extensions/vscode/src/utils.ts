export interface CatalogServiceItem {
  name: string;
  teamName?: string;
  domain?: string;
  type?: string;
  language?: string;
  status?: string;
  description?: string;
}

/**
 * Parse the catalog API response body, supporting both array and { items: [...] } shapes.
 */
export function parseCatalogResponse(body: string): CatalogServiceItem[] {
  const parsed = JSON.parse(body) as { items?: CatalogServiceItem[] } | CatalogServiceItem[];
  return Array.isArray(parsed) ? parsed : (parsed.items ?? []);
}

/**
 * Build the NexTraceOne dashboard URL for a given service.
 */
export function buildServiceDashboardUrl(serverUrl: string, serviceName: string): string {
  const base = serverUrl.replace(/\/$/, '');
  return `${base}/services/${encodeURIComponent(serviceName)}`;
}
