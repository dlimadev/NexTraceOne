/**
 * Mock enrichment para itens do catálogo.
 *
 * Enquanto o backend não fornece todos os campos (domain, owner, compliance, etc.),
 * esta camada enriquece ContractListItem → CatalogItem com dados determinísticos
 * baseados num hash do versionId, garantindo consistência entre renders.
 *
 * Pronto para substituição real: basta popular CatalogItem directamente a partir
 * do backend e remover esta camada.
 */
import type { ContractListItem, ApprovalState } from '../types';
import type { CatalogItem, CatalogServiceType } from './types';

// ── Deterministic hash ────────────────────────────────────────────────────────

function simpleHash(str: string): number {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    hash = ((hash << 5) - hash + str.charCodeAt(i)) | 0;
  }
  return Math.abs(hash);
}

// ── Mock pools ────────────────────────────────────────────────────────────────

const MOCK_DOMAINS = [
  'Payments', 'Identity', 'Logistics', 'Billing',
  'Notifications', 'Analytics', 'Inventory', 'Orders',
] as const;

const MOCK_PRODUCTS = [
  'Core Platform', 'Merchant Portal', 'Mobile App',
  'Admin Console', 'Data Pipeline', 'API Gateway',
] as const;

const MOCK_OWNERS = [
  'john.silva', 'maria.santos', 'carlos.mendes',
  'ana.oliveira', 'pedro.costa', 'lucia.ferreira',
] as const;

const MOCK_TEAMS = [
  'Platform', 'Payments', 'Identity', 'DevEx',
  'Data', 'Infrastructure', 'Mobile',
] as const;

const MOCK_TECHNOLOGIES = [
  'Java', 'C#', 'TypeScript', 'Go', 'Python', 'Kotlin',
] as const;

const SERVICE_TYPE_BY_PROTOCOL: Record<string, CatalogServiceType> = {
  OpenApi: 'RestApi',
  Swagger: 'RestApi',
  Wsdl: 'Soap',
  AsyncApi: 'Event',
  Protobuf: 'BackgroundService',
  GraphQl: 'RestApi',
};

const CRITICALITIES = ['Low', 'Medium', 'High', 'Critical'] as const;
const EXPOSURES = ['Internal', 'External', 'Partner'] as const;

// ── Enrichment ────────────────────────────────────────────────────────────────

export function enrichCatalogItems(items: ContractListItem[]): CatalogItem[] {
  return items.map(enrichSingle);
}

function enrichSingle(item: ContractListItem): CatalogItem {
  const h = simpleHash(item.versionId);
  const protocol = item.protocol || 'OpenApi';
  const base = SERVICE_TYPE_BY_PROTOCOL[protocol] ?? 'RestApi';

  const serviceType: CatalogServiceType =
    base === 'Event'
      ? (h % 3 === 0 ? 'KafkaProducer' : h % 3 === 1 ? 'KafkaConsumer' : 'Event')
      : base;

  return {
    ...item,
    name: formatName(item.apiAssetId),
    serviceType,
    domain: MOCK_DOMAINS[h % MOCK_DOMAINS.length],
    product: MOCK_PRODUCTS[h % MOCK_PRODUCTS.length],
    owner: MOCK_OWNERS[h % MOCK_OWNERS.length],
    team: MOCK_TEAMS[h % MOCK_TEAMS.length],
    approvalState: deriveApproval(item.lifecycleState, h),
    complianceScore: 40 + (h % 61),
    criticality: CRITICALITIES[h % CRITICALITIES.length],
    updatedAt: item.createdAt,
    hasBreakingChanges: h % 7 === 0,
    violationCount: h % 5 === 0 ? (h % 4) + 1 : 0,
    exposure: EXPOSURES[h % EXPOSURES.length],
    technology: MOCK_TECHNOLOGIES[h % MOCK_TECHNOLOGIES.length],
  };
}

function formatName(apiAssetId: string): string {
  if (!apiAssetId) return 'Unknown Contract';
  const parts = apiAssetId
    .replace(/[-_]/g, ' ')
    .split(' ')
    .filter(Boolean);
  return parts
    .map((p) => p.charAt(0).toUpperCase() + p.slice(1).toLowerCase())
    .join(' ');
}

function deriveApproval(lifecycle: string, hash: number): ApprovalState {
  switch (lifecycle) {
    case 'Draft': return 'Pending';
    case 'InReview': return hash % 3 === 0 ? 'Escalated' : 'InReview';
    case 'Approved':
    case 'Locked':
    case 'Deprecated':
    case 'Sunset':
    case 'Retired':
      return 'Approved';
    default: return 'Pending';
  }
}

// ── Filter/Sort helpers ───────────────────────────────────────────────────────

export function extractFilterOptions(items: CatalogItem[]) {
  const unique = (arr: string[]) => [...new Set(arr)].sort();
  return {
    domains: unique(items.map((i) => i.domain)),
    products: unique(items.map((i) => i.product)),
    owners: unique(items.map((i) => i.owner)),
    teams: unique(items.map((i) => i.team)),
    technologies: unique(items.map((i) => i.technology)),
  };
}
