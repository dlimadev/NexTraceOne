/**
 * Mock enrichment para o studio de contratos.
 *
 * Enquanto o backend não fornece todos os campos (domain, owner, compliance, etc.),
 * esta camada enriquece ContractVersionDetail → StudioContract com dados
 * determinísticos baseados num hash do ID, garantindo consistência entre renders.
 *
 * Pronto para substituição real: basta popular StudioContract directamente
 * a partir do backend e remover esta camada.
 */
import type { ContractVersionDetail } from '../../../types';
import type { ApprovalState } from '../types/domain';
import type {
  StudioContract,
  StudioRisk,
  StudioActivityItem,
  StudioRelationship,
  StudioDependency,
} from './studioTypes';

// ── Deterministic hash ────────────────────────────────────────────────────────

function simpleHash(str: string): number {
  let hash = 0;
  for (let i = 0; i < str.length; i++) {
    hash = ((hash << 5) - hash + str.charCodeAt(i)) | 0;
  }
  return Math.abs(hash);
}

function pick<T>(arr: readonly T[], hash: number): T {
  return arr[hash % arr.length];
}

// ── Mock pools ────────────────────────────────────────────────────────────────

const MOCK_DOMAINS = [
  'Payments', 'Identity', 'Logistics', 'Billing',
  'Notifications', 'Analytics', 'Inventory', 'Orders',
] as const;

const MOCK_CAPABILITIES = [
  'Transaction Processing', 'User Authentication', 'Order Fulfillment',
  'Real-time Notifications', 'Data Analytics', 'Inventory Management',
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

const MOCK_CRITICALITIES = ['Low', 'Medium', 'High', 'Critical'] as const;

const MOCK_VISIBILITIES = ['Public', 'Internal', 'Private'] as const;

const MOCK_DATA_CLASSIFICATIONS = ['Public', 'Internal', 'Confidential', 'Restricted'] as const;

const MOCK_TAGS_POOL = [
  'core', 'payments', 'internal', 'external', 'v2', 'stable',
  'high-throughput', 'pci', 'gdpr', 'real-time', 'batch', 'deprecated',
] as const;

const SERVICE_TYPE_BY_PROTOCOL: Record<string, string> = {
  OpenApi: 'RestApi',
  Swagger: 'RestApi',
  Wsdl: 'Soap',
  AsyncApi: 'Event',
  Protobuf: 'Event',
  GraphQl: 'RestApi',
};

const APPROVAL_STATES: ApprovalState[] = ['Pending', 'InReview', 'Approved', 'Rejected', 'Escalated'];

const APPROVAL_ROLES = [
  'Author', 'TechLead', 'Architect', 'Security', 'Compliance', 'ProductOwner',
] as const;

const RISK_CATEGORIES = ['Security', 'Compliance', 'Performance', 'Operational', 'Contract'] as const;

const RISK_DESCRIPTIONS: Record<string, string[]> = {
  Security: ['Missing authentication scheme', 'No rate limiting configured', 'PII exposed in response'],
  Compliance: ['No GDPR documentation', 'Missing data retention policy', 'Audit trail incomplete'],
  Performance: ['No SLO defined', 'Missing caching headers', 'Response payload too large'],
  Operational: ['No runbook linked', 'Missing health endpoint', 'No deprecation plan'],
  Contract: ['Breaking change detected', 'Missing schema validation', 'Outdated specification'],
};

const ACTIVITY_ACTIONS = [
  'Version created', 'Lifecycle changed', 'Specification updated',
  'Review requested', 'Approved by', 'Policy check completed',
  'Consumer registered', 'Schema modified', 'Comment added',
] as const;

const CONSUMER_NAMES = [
  'Order Service', 'Payment Gateway', 'Mobile BFF', 'Admin Dashboard',
  'Notification Worker', 'Analytics Pipeline', 'Report Generator',
] as const;

const DEPENDENCY_NAMES = [
  'User Service', 'Auth Provider', 'Message Broker', 'Cache Layer',
  'Database Cluster', 'Config Service', 'Rate Limiter',
] as const;

// ── Enrichment ────────────────────────────────────────────────────────────────

/**
 * Enriquece um ContractVersionDetail do backend num StudioContract completo.
 * Usa hash determinístico do ID para gerar dados consistentes.
 */
export function enrichToStudioContract(detail: ContractVersionDetail): StudioContract {
  const h = simpleHash(detail.id);
  const h2 = simpleHash(detail.apiAssetId);

  const friendlyName = detail.apiAssetId
    .replace(/[-_]/g, ' ')
    .replace(/\b\w/g, (c) => c.toUpperCase());

  const domain = pick(MOCK_DOMAINS, h);
  const owner = pick(MOCK_OWNERS, h2);
  const team = pick(MOCK_TEAMS, h + 1);
  const criticality = pick(MOCK_CRITICALITIES, h + 2);
  const serviceType = SERVICE_TYPE_BY_PROTOCOL[detail.protocol] ?? 'RestApi';

  const approvalState = deriveApprovalState(detail.lifecycleState);
  const complianceScore = 40 + (h % 61); // 40-100

  const tagCount = 2 + (h % 3);
  const tags: string[] = [];
  for (let i = 0; i < tagCount; i++) {
    tags.push(pick(MOCK_TAGS_POOL, h + i * 7));
  }

  return {
    // Identity
    id: detail.id,
    apiAssetId: detail.apiAssetId,
    technicalName: detail.apiAssetId,
    friendlyName,
    functionalDescription: `Manages ${domain.toLowerCase()} operations for the ${pick(MOCK_PRODUCTS, h)} product. Provides core capabilities for ${pick(MOCK_CAPABILITIES, h2).toLowerCase()}.`,
    technicalDescription: `${detail.protocol} service exposing ${serviceType} endpoints. Format: ${detail.format}. Version: ${detail.semVer}.`,

    // Version
    semVer: detail.semVer,
    format: detail.format,
    protocol: detail.protocol,
    specContent: detail.specContent,

    // Lifecycle
    lifecycleState: detail.lifecycleState,
    isLocked: detail.isLocked,
    lockedAt: detail.lockedAt,
    lockedBy: detail.lockedBy,
    signedBy: detail.signedBy,
    signedAt: detail.signedAt,
    fingerprint: detail.fingerprint,
    algorithm: detail.algorithm,

    // Organization
    serviceType,
    domain,
    capability: pick(MOCK_CAPABILITIES, h + 3),
    product: pick(MOCK_PRODUCTS, h),
    owner,
    team,
    visibility: pick(MOCK_VISIBILITIES, h + 4),
    criticality,
    dataClassification: pick(MOCK_DATA_CLASSIFICATIONS, h + 5),
    tags,

    // SLA / SLO
    sla: `${99 + (h % 2)}.${'0' + (h % 10)}%`,
    slo: `p99 < ${100 + (h % 400)}ms`,
    externalLinks: [
      `https://docs.internal/${detail.apiAssetId}`,
      `https://monitoring.internal/service/${detail.apiAssetId}`,
    ],

    // Governance
    approvalState,
    complianceScore,
    approvalChecklist: APPROVAL_ROLES.map((role, idx) => ({
      role,
      state: idx <= (h % APPROVAL_ROLES.length) ? 'Approved' as const : 'Pending' as const,
      reviewedBy: idx <= (h % APPROVAL_ROLES.length) ? pick(MOCK_OWNERS, h + idx) : undefined,
      reviewedAt: idx <= (h % APPROVAL_ROLES.length) ? detail.createdAt : undefined,
    })),
    policyChecks: generatePolicyChecks(h),

    // Provenance
    importedFrom: detail.importedFrom,
    provenance: detail.provenance?.origin,
    deprecationNotice: detail.deprecationNotice,
    sunsetDate: detail.sunsetDate,
    createdAt: detail.createdAt,

    // Risks
    risks: generateRisks(h, detail.lifecycleState),

    // Activity
    recentActivity: generateActivity(h, detail.createdAt, owner),

    // Relationships
    consumers: generateConsumers(h),
    producers: generateProducers(h2),
    dependencies: generateDependencies(h),
  };
}

// ── Internal generators ───────────────────────────────────────────────────────

function deriveApprovalState(lifecycleState: string): ApprovalState {
  switch (lifecycleState) {
    case 'Draft': return 'Pending';
    case 'InReview': return 'InReview';
    case 'Approved':
    case 'Locked': return 'Approved';
    case 'Deprecated':
    case 'Sunset':
    case 'Retired': return 'Approved';
    default: return 'Pending';
  }
}

function generatePolicyChecks(h: number): StudioContract['policyChecks'] {
  const policies = [
    { id: 'naming', name: 'Naming Convention', severity: 'Warning' as const },
    { id: 'auth', name: 'Authentication Required', severity: 'Error' as const },
    { id: 'versioning', name: 'SemVer Compliance', severity: 'Info' as const },
    { id: 'docs', name: 'Documentation Coverage', severity: 'Warning' as const },
    { id: 'deprecation', name: 'Deprecation Policy', severity: 'Info' as const },
  ];

  return policies.map((p, idx) => ({
    policyId: p.id,
    policyName: p.name,
    passed: (h + idx) % 3 !== 0,
    severity: p.severity,
    message: (h + idx) % 3 !== 0 ? '' : `${p.name} check failed`,
    path: (h + idx) % 3 !== 0 ? undefined : '/info',
  }));
}

function generateRisks(h: number, lifecycleState: string): StudioRisk[] {
  const count = lifecycleState === 'Draft' ? 2 + (h % 2) : h % 3;
  const risks: StudioRisk[] = [];

  for (let i = 0; i < count; i++) {
    const cat = pick(RISK_CATEGORIES, h + i);
    const descs = RISK_DESCRIPTIONS[cat] ?? ['Unknown risk'];
    risks.push({
      id: `risk-${i}`,
      level: pick(['Medium', 'High', 'Low', 'Critical'] as const, h + i * 3),
      description: pick(descs, h + i * 5),
      category: cat,
    });
  }

  return risks;
}

function generateActivity(h: number, baseDate: string, owner: string): StudioActivityItem[] {
  const count = 4 + (h % 4);
  const items: StudioActivityItem[] = [];
  const parsed = new Date(baseDate);
  const base = Number.isNaN(parsed.getTime()) ? Date.now() : parsed.getTime();

  for (let i = 0; i < count; i++) {
    const ts = new Date(base - i * 86400000 * (1 + (h % 3)));
    items.push({
      id: `act-${i}`,
      action: pick(ACTIVITY_ACTIONS, h + i),
      actor: pick(MOCK_OWNERS, h + i * 2),
      timestamp: ts.toISOString(),
      detail: i === 0 ? `by ${owner}` : undefined,
    });
  }

  return items;
}

function generateConsumers(h: number): StudioRelationship[] {
  const count = 1 + (h % 4);
  const items: StudioRelationship[] = [];

  for (let i = 0; i < count; i++) {
    items.push({
      id: `consumer-${i}`,
      name: pick(CONSUMER_NAMES, h + i),
      type: pick(['Service', 'Application', 'External'] as const, h + i * 2),
      registeredAt: new Date(Date.now() - i * 86400000 * 7).toISOString(),
    });
  }

  return items;
}

function generateProducers(h: number): StudioRelationship[] {
  const count = h % 3;
  const items: StudioRelationship[] = [];

  for (let i = 0; i < count; i++) {
    items.push({
      id: `producer-${i}`,
      name: pick(CONSUMER_NAMES, h + i + 10),
      type: 'Service',
      registeredAt: new Date(Date.now() - i * 86400000 * 14).toISOString(),
    });
  }

  return items;
}

function generateDependencies(h: number): StudioDependency[] {
  const count = 2 + (h % 3);
  const items: StudioDependency[] = [];

  for (let i = 0; i < count; i++) {
    items.push({
      id: `dep-${i}`,
      name: pick(DEPENDENCY_NAMES, h + i),
      direction: i % 2 === 0 ? 'Upstream' : 'Downstream',
      type: pick(['Runtime', 'BuildTime', 'Optional'] as const, h + i * 2),
    });
  }

  return items;
}
