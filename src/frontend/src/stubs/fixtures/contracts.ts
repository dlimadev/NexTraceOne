/**
 * Fixtures do módulo de contratos para o modo stub.
 *
 * Dados coerentes com os serviços do catálogo (ver fixtures/catalog.ts):
 * contratos por serviço, entidades canónicas, rulesets, resumo e saúde.
 */
import type {
  ContractListResponse,
  ContractsSummary,
  ServiceContractsResponse,
  ContractSourceOfTruth,
} from '../../types';
import type { CanonicalEntity } from '../../features/contracts/types/domain';

/** Versões de contrato do catálogo. */
export const stubContractList: ContractListResponse = {
  items: [
    { contractVersionId: 'ct-pay-v2', versionId: 'ct-pay-v2', apiAssetId: 'api-payments-v2', apiName: 'Payments API v2', name: 'Payments API v2', semVer: '2.3.0', version: '2.3.0', protocol: 'OpenApi', lifecycleState: 'Approved', isLocked: true, isSigned: true, domain: 'Billing', teamName: 'Payments', technicalOwner: 'ana.silva@nextraceone.dev', criticality: 'Critical', exposureType: 'External', ruleViolationCount: 0, overallScore: 0.92, createdAt: '2026-03-01T00:00:00.000Z' },
    { contractVersionId: 'ct-ord-v1', versionId: 'ct-ord-v1', apiAssetId: 'api-orders-v1', apiName: 'Orders API', name: 'Orders API', semVer: '1.4.2', version: '1.4.2', protocol: 'OpenApi', lifecycleState: 'Approved', isLocked: false, isSigned: true, domain: 'Commerce', teamName: 'Commerce', technicalOwner: 'joao.pereira@nextraceone.dev', criticality: 'High', exposureType: 'Internal', ruleViolationCount: 2, overallScore: 0.84, createdAt: '2026-04-10T00:00:00.000Z' },
    { contractVersionId: 'ct-inv-v1', versionId: 'ct-inv-v1', apiAssetId: 'api-inventory-gql', apiName: 'Inventory GraphQL', name: 'Inventory GraphQL', semVer: '1.0.0', version: '1.0.0', protocol: 'GraphQl', lifecycleState: 'InReview', isLocked: false, isSigned: false, domain: 'Commerce', teamName: 'Commerce', technicalOwner: 'joao.pereira@nextraceone.dev', criticality: 'High', exposureType: 'Partner', ruleViolationCount: 5, overallScore: 0.71, createdAt: '2026-06-15T00:00:00.000Z' },
    { contractVersionId: 'ct-ntf-v1', versionId: 'ct-ntf-v1', apiAssetId: 'api-notifications', apiName: 'Notifications Events', name: 'Notifications Events', semVer: '1.2.0', version: '1.2.0', protocol: 'AsyncApi', lifecycleState: 'Approved', isLocked: false, isSigned: true, domain: 'Platform', teamName: 'Platform', technicalOwner: 'maria.costa@nextraceone.dev', criticality: 'Medium', exposureType: 'Internal', ruleViolationCount: 1, overallScore: 0.88, createdAt: '2026-05-05T00:00:00.000Z' },
    { contractVersionId: 'ct-leg-v3', versionId: 'ct-leg-v3', apiAssetId: 'api-legacy-billing', apiName: 'Legacy Billing SOAP', name: 'Legacy Billing SOAP', semVer: '3.0.0', version: '3.0.0', protocol: 'Wsdl', lifecycleState: 'Deprecated', isLocked: true, isSigned: false, domain: 'Billing', teamName: 'Billing', technicalOwner: 'carlos.dias@nextraceone.dev', criticality: 'Medium', exposureType: 'Internal', ruleViolationCount: 8, overallScore: 0.52, deprecationDate: '2026-12-31T00:00:00.000Z', createdAt: '2025-01-01T00:00:00.000Z' },
  ],
  totalCount: 5,
};

/**
 * Vista Source of Truth de um contrato, derivada da lista.
 * A página deep-acede `governance.lifecycleState` e `references` — o
 * catch-all `[]` não serve, precisa de forma dedicada.
 */
export function buildContractSot(contractVersionId: string): ContractSourceOfTruth {
  const item =
    stubContractList.items.find(
      (c) => c.versionId === contractVersionId || c.contractVersionId === contractVersionId,
    ) ?? stubContractList.items[0];

  return {
    apiAssetId: item.apiAssetId,
    semVer: item.semVer ?? item.version ?? '1.0.0',
    protocol: item.protocol,
    format: item.protocol === 'OpenApi' ? 'YAML' : item.protocol === 'Wsdl' ? 'XML' : 'JSON',
    importedFrom: `git://nextraceone/${item.apiAssetId}`,
    artifactCount: 3,
    diffCount: 2,
    violationCount: item.ruleViolationCount ?? 0,
    governance: {
      lifecycleState: item.lifecycleState,
      isLocked: item.isLocked,
      isSigned: item.isSigned,
      deprecationDate: item.deprecationDate,
    },
    references: [
      { referenceId: 'ref-spec', title: 'Especificação do contrato', description: 'Definição versionada do contrato.', assetType: 'Specification', referenceType: 'Specification', url: `https://docs.nextraceone.dev/${item.apiAssetId}` },
      { referenceId: 'ref-changelog', title: 'Changelog', description: 'Histórico de alterações do contrato.', assetType: 'Documentation', referenceType: 'Documentation', url: `https://docs.nextraceone.dev/${item.apiAssetId}/changelog` },
    ],
  };
}

/** Resumo agregado dos contratos. */
export const stubContractsSummary: ContractsSummary = {
  totalCount: 5,
  totalVersions: 5,
  distinctContracts: 5,
  byProtocol: [
    { protocol: 'OpenApi', count: 2 },
    { protocol: 'GraphQl', count: 1 },
    { protocol: 'AsyncApi', count: 1 },
    { protocol: 'Wsdl', count: 1 },
  ],
  approvedCount: 3,
  lockedCount: 2,
  draftCount: 0,
  inReviewCount: 1,
  deprecatedCount: 1,
};

/** Contratos por serviço (aba "Contratos Vinculados" do detalhe). */
export const stubContractsByService: Record<string, ServiceContractsResponse> = {
  'svc-payments-api': { items: [{ contractVersionId: 'ct-pay-v2', version: '2.3.0', semVer: '2.3.0', apiName: 'Payments API v2', protocol: 'OpenApi', lifecycleState: 'Approved', isLocked: true }], contracts: [{ contractVersionId: 'ct-pay-v2', version: '2.3.0', semVer: '2.3.0', apiName: 'Payments API v2', protocol: 'OpenApi', lifecycleState: 'Approved', isLocked: true }], totalCount: 1 },
  'svc-orders-api': { items: [{ contractVersionId: 'ct-ord-v1', version: '1.4.2', semVer: '1.4.2', apiName: 'Orders API', protocol: 'OpenApi', lifecycleState: 'Approved', isLocked: false }], contracts: [{ contractVersionId: 'ct-ord-v1', version: '1.4.2', semVer: '1.4.2', apiName: 'Orders API', protocol: 'OpenApi', lifecycleState: 'Approved', isLocked: false }], totalCount: 1 },
};

/** Entidades canónicas. */
export const stubCanonicalEntities: { items: CanonicalEntity[]; total: number } = {
  items: [
    { id: 'ce-payment', name: 'Payment', description: 'Modelo canónico de pagamento.', domain: 'Billing', category: 'Core', owner: 'Payments', version: '2.1.0', state: 'Published', schemaContent: '{}', schemaFormat: 'JsonSchema', aliases: ['Transaction'], tags: ['money', 'core'], criticality: 'Critical', reusePolicy: 'Encouraged', knownUsageCount: 4, createdAt: '2026-02-01T00:00:00.000Z', updatedAt: '2026-06-01T00:00:00.000Z' },
    { id: 'ce-order', name: 'Order', description: 'Modelo canónico de encomenda.', domain: 'Commerce', category: 'Core', owner: 'Commerce', version: '1.3.0', state: 'Published', schemaContent: '{}', schemaFormat: 'JsonSchema', aliases: [], tags: ['commerce'], criticality: 'High', reusePolicy: 'Encouraged', knownUsageCount: 3, createdAt: '2026-02-10T00:00:00.000Z', updatedAt: '2026-05-20T00:00:00.000Z' },
    { id: 'ce-customer', name: 'Customer', description: 'Modelo canónico de cliente.', domain: 'Identity', category: 'Core', owner: 'Platform', version: '1.0.0', state: 'Published', schemaContent: '{}', schemaFormat: 'JsonSchema', aliases: ['User'], tags: ['identity'], criticality: 'High', reusePolicy: 'Mandatory', knownUsageCount: 6, createdAt: '2026-01-15T00:00:00.000Z', updatedAt: '2026-04-01T00:00:00.000Z' },
    { id: 'ce-money', name: 'Money', description: 'Valor monetário com moeda.', domain: 'Billing', category: 'Value Object', owner: 'Payments', version: '1.0.0', state: 'Draft', schemaContent: '{}', schemaFormat: 'JsonSchema', aliases: [], tags: ['money'], criticality: 'Medium', reusePolicy: 'Encouraged', knownUsageCount: 2, createdAt: '2026-06-01T00:00:00.000Z', updatedAt: '2026-06-01T00:00:00.000Z' },
  ],
  total: 4,
};

/** Rulesets Spectral (forma bruta que o cliente mapeia via r.data.rulesets). */
export const stubRulesetsRaw = {
  rulesets: [
    { rulesetId: 'rs-naming', name: 'API Naming Conventions', description: 'Regras de nomenclatura REST (kebab-case, plurais).', content: 'rules: {}', rulesetType: 'Default', isActive: true, createdAt: '2026-02-01T00:00:00.000Z' },
    { rulesetId: 'rs-security', name: 'Security Baseline', description: 'Requer auth, HTTPS e rate limiting em todos os endpoints.', content: 'rules: {}', rulesetType: 'Custom', isActive: true, createdAt: '2026-03-01T00:00:00.000Z' },
    { rulesetId: 'rs-legacy', name: 'Legacy SOAP Checks', description: 'Verificações específicas para contratos WSDL legados.', content: 'rules: {}', rulesetType: 'Custom', isActive: false, createdAt: '2026-04-01T00:00:00.000Z' },
  ],
  totalCount: 3,
};

/** Dashboard de saúde dos contratos. */
export const stubHealthDashboard = {
  totalContractVersions: 5,
  distinctContracts: 5,
  deprecatedVersions: 1,
  filteredCount: 5,
  percentWithExamples: 60,
  percentWithCanonicalEntities: 80,
  topViolations: [
    { contractVersionId: 'ct-leg-v3', semVer: '3.0.0', violationCount: 8, topRuleIds: ['no-http', 'require-auth', 'operation-description'] },
    { contractVersionId: 'ct-inv-v1', semVer: '1.0.0', violationCount: 5, topRuleIds: ['field-naming', 'require-description'] },
    { contractVersionId: 'ct-ord-v1', semVer: '1.4.2', violationCount: 2, topRuleIds: ['tag-description'] },
  ],
  healthScore: 78,
};

/** Central de publicação (contratos publicados no Developer Portal). */
export const stubPublicationCenter = {
  items: [
    { publicationEntryId: 'pub-1', contractVersionId: 'ct-pay-v2', contractTitle: 'Payments API v2', semVer: '2.3.0', protocol: 'OpenApi', status: 'Published', visibility: 'Public', publishedAt: '2026-03-05T00:00:00.000Z', publishedBy: 'ana.silva@nextraceone.dev' },
    { publicationEntryId: 'pub-2', contractVersionId: 'ct-ntf-v1', contractTitle: 'Notifications Events', semVer: '1.2.0', protocol: 'AsyncApi', status: 'Published', visibility: 'Internal', publishedAt: '2026-05-06T00:00:00.000Z', publishedBy: 'maria.costa@nextraceone.dev' },
  ],
  totalCount: 2,
};
