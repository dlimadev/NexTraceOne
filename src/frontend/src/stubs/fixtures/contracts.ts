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
  ContractVersionDetail,
  ContractRuleViolation,
  ContractVersion,
  ContractIntegrityResult,
  ContractDeploymentsResponse,
  ContractSubscribersResponse,
} from '../../types';
import type { CanonicalEntity, ValidationSummary, ContractScorecard } from '../../features/contracts/types/domain';
import type { ParsePreviewResponse } from '../../features/contracts/hooks/useSpecPreview';

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

/** Conteúdo de spec por protocolo (alimenta o editor e o live preview). */
const specByProtocol: Record<string, { format: string; content: (name: string, ver: string) => string }> = {
  OpenApi: {
    format: 'yaml',
    content: (name, ver) =>
      `openapi: 3.0.3\ninfo:\n  title: ${name}\n  version: ${ver}\npaths:\n  /payments:\n    post:\n      summary: Create payment\n      responses:\n        '201':\n          description: Created\n`,
  },
  GraphQl: {
    format: 'graphql',
    content: () => `type Query {\n  inventory(sku: ID!): InventoryItem\n}\n\ntype InventoryItem {\n  sku: ID!\n  available: Int!\n}\n`,
  },
  AsyncApi: {
    format: 'yaml',
    content: (name, ver) =>
      `asyncapi: 2.6.0\ninfo:\n  title: ${name}\n  version: ${ver}\nchannels:\n  payments.settled:\n    subscribe:\n      message:\n        payload:\n          type: object\n`,
  },
  Wsdl: {
    format: 'xml',
    content: (name) =>
      `<?xml version="1.0"?>\n<definitions name="${name}" xmlns="http://schemas.xmlsoap.org/wsdl/">\n  <service name="LegacyBilling"/>\n</definitions>\n`,
  },
};

/**
 * Detalhe completo de uma versão de contrato (aba workspace `/contracts/:id`).
 * Fornece `specContent` (senão `useSpecPreview` faz `.trim()` de undefined) e
 * todos os campos deep-acedidos por `toStudioContract`.
 */
export function buildContractDetail(contractVersionId: string): ContractVersionDetail {
  const item =
    stubContractList.items.find(
      (c) => c.versionId === contractVersionId || c.contractVersionId === contractVersionId,
    ) ?? stubContractList.items[0];

  const name = item.name ?? item.apiName ?? item.apiAssetId;
  const semVer = item.semVer ?? item.version ?? '1.0.0';
  const spec = specByProtocol[item.protocol] ?? specByProtocol.OpenApi;

  return {
    id: contractVersionId,
    apiAssetId: item.apiAssetId,
    apiName: name,
    semVer,
    specContent: spec.content(name, semVer),
    format: spec.format,
    protocol: item.protocol,
    lifecycleState: item.lifecycleState,
    isLocked: item.isLocked ?? false,
    signedBy: item.isSigned ? item.technicalOwner : undefined,
    signedAt: item.isSigned ? '2026-03-05T00:00:00.000Z' : undefined,
    importedFrom: `git://nextraceone/${item.apiAssetId}`,
    deprecationDate: item.deprecationDate,
    createdAt: item.createdAt ?? '2026-03-01T00:00:00.000Z',
    routePattern: '/api/v2/payments',
    visibility: item.exposureType,
    domain: item.domain,
    teamName: item.teamName,
    technicalOwner: item.technicalOwner,
    criticality: item.criticality,
    consumers: [
      { id: 'cons-orders', name: 'Orders API', kind: 'Service', environment: 'Production', externalReference: 'svc-orders-api', confidenceScore: 0.95, lastObservedAt: '2026-06-28T00:00:00.000Z' },
      { id: 'cons-inventory', name: 'Inventory GraphQL', kind: 'Service', environment: 'Production', externalReference: 'svc-inventory-graphql', confidenceScore: 0.82, lastObservedAt: '2026-06-20T00:00:00.000Z' },
    ],
  } as ContractVersionDetail;
}

/** Catálogo de violações de regras (partilhado por violações e validação). */
const violationCatalogue: ContractRuleViolation[] = [
  { id: 'v-http', ruleName: 'no-http', severity: 'Error', message: 'Endpoints devem usar HTTPS.', path: '$.servers[0].url', suggestedFix: 'Usar https://' },
  { id: 'v-auth', ruleName: 'require-auth', severity: 'Error', message: 'Falta esquema de autenticação.', path: '$.components.securitySchemes' },
  { id: 'v-desc', ruleName: 'operation-description', severity: 'Warning', message: 'Operação sem descrição.', path: '$.paths./payments.post' },
  { id: 'v-tag', ruleName: 'tag-description', severity: 'Info', message: 'Tag sem descrição.', path: '$.tags[0]' },
];

/** Violações efetivas de um contrato, limitadas pelo ruleViolationCount. */
function violationsFor(contractVersionId: string): ContractRuleViolation[] {
  const item = stubContractList.items.find(
    (c) => c.versionId === contractVersionId || c.contractVersionId === contractVersionId,
  );
  const count = item?.ruleViolationCount ?? 0;
  return violationCatalogue.slice(0, Math.min(count, violationCatalogue.length));
}

/** Violações de regras de um contrato (envelope `{ contractVersionId, violations }`). */
export function buildContractViolations(contractVersionId: string): {
  contractVersionId: string;
  violations: ContractRuleViolation[];
} {
  return { contractVersionId, violations: violationsFor(contractVersionId) };
}

/**
 * Resumo de validação (aba Governança → ValidationSection).
 * `sources` é deep-acedido (`.length`/`.join`) — o catch-all `[]` não serve.
 */
export function buildValidationSummary(contractVersionId: string): ValidationSummary {
  const violations = violationsFor(contractVersionId);
  const errorCount = violations.filter((v) => v.severity === 'Error').length;
  const warningCount = violations.filter((v) => v.severity === 'Warning').length;
  const infoCount = violations.filter((v) => v.severity === 'Info').length;

  return {
    totalIssues: violations.length,
    errorCount,
    warningCount,
    infoCount,
    hintCount: 0,
    blockedCount: 0,
    isPublishReady: errorCount === 0,
    isReviewReady: errorCount === 0,
    sources: ['Spectral', 'Internal Checks', 'Canonical Adherence'],
    validatedAt: '2026-06-20T00:00:00.000Z',
    fingerprint: `sha256:${contractVersionId}-a1b2c3d4`,
    overallStatus: errorCount > 0 ? 'Invalid' : 'Valid',
  };
}

/**
 * Scorecard técnico de um contrato (Governança → Scorecard).
 * Scores em escala 0–1 (a secção faz `Math.round(score * 100)`).
 */
export function buildContractScorecard(contractVersionId: string): ContractScorecard {
  const item = stubContractList.items.find(
    (c) => c.versionId === contractVersionId || c.contractVersionId === contractVersionId,
  );
  const overall = item?.overallScore ?? 0.75;

  return {
    scorecardId: `sc-${contractVersionId}`,
    contractVersionId,
    qualityScore: overall,
    completenessScore: Math.min(1, overall + 0.05),
    compatibilityScore: Math.max(0, overall - 0.08),
    riskScore: Math.max(0, 1 - overall),
    overallScore: overall,
    operationCount: 12,
    schemaCount: 8,
    hasSecurityDefinitions: overall >= 0.6,
    hasExamples: overall >= 0.8,
    hasDescriptions: overall >= 0.5,
    qualityJustification: 'Nomenclatura consistente e estrutura bem definida.',
    completenessJustification: 'A maioria das operações tem descrições e exemplos.',
    compatibilityJustification: 'Sem alterações breaking face à versão anterior.',
    riskJustification: overall >= 0.8 ? 'Risco baixo — contrato maduro.' : 'Risco moderado — rever antes de promover.',
  };
}

/**
 * Histórico de versões de um contrato por apiAssetId (Versionamento/Changelog).
 * O cliente lê `r.data.versions` — envelope obrigatório.
 */
export function buildContractHistory(apiAssetId: string): {
  apiAssetId: string;
  versions: ContractVersion[];
} {
  const item = stubContractList.items.find((c) => c.apiAssetId === apiAssetId);
  if (!item) return { apiAssetId, versions: [] };

  const detail = buildContractDetail(item.versionId ?? item.contractVersionId ?? '');
  const current: ContractVersion = {
    id: detail.id,
    apiAssetId,
    version: detail.semVer,
    content: detail.specContent,
    format: detail.format,
    protocol: detail.protocol,
    lifecycleState: detail.lifecycleState,
    isLocked: detail.isLocked,
    signedBy: detail.signedBy,
    signedAt: detail.signedAt,
    isAiGenerated: false,
    createdAt: detail.createdAt,
  };
  const previous: ContractVersion = {
    ...current,
    id: `${detail.id}-prev`,
    version: '1.0.0',
    lifecycleState: 'Deprecated',
    isLocked: true,
    createdAt: '2025-06-01T00:00:00.000Z',
  };
  return { apiAssetId, versions: [current, previous] };
}

/** Resultado de integridade estrutural (ComplianceSection → validateIntegrity). */
export function buildContractIntegrity(contractVersionId: string): ContractIntegrityResult {
  const violations = violationsFor(contractVersionId);
  return {
    isValid: violations.length === 0,
    errors: [],
    warnings: violations.filter((v) => v.severity === 'Warning').map((v) => v.message),
    pathCount: 4,
    endpointCount: 6,
    schemaVersion: '3.0.3',
  };
}

/** Deployments de uma versão de contrato (DeploymentsSection). */
export function buildContractDeployments(contractVersionId: string): ContractDeploymentsResponse {
  const item = stubContractList.items.find(
    (c) => c.versionId === contractVersionId || c.contractVersionId === contractVersionId,
  );
  if (!item) return { deployments: [] };
  return {
    deployments: [
      { deploymentId: 'dep-prod', contractVersionId, apiAssetId: item.apiAssetId, environment: 'Production', semVer: item.semVer ?? '1.0.0', status: 'Success', deployedAt: '2026-03-06T00:00:00.000Z', deployedBy: item.technicalOwner ?? 'ci@nextraceone.dev', sourceSystem: 'GitHub Actions' },
      { deploymentId: 'dep-stg', contractVersionId, apiAssetId: item.apiAssetId, environment: 'Staging', semVer: item.semVer ?? '1.0.0', status: 'Success', deployedAt: '2026-03-04T00:00:00.000Z', deployedBy: item.technicalOwner ?? 'ci@nextraceone.dev', sourceSystem: 'GitHub Actions' },
    ],
  };
}

/** Subscritores formais de uma API (ConsumersSection via Developer Portal). */
export function buildContractSubscribers(apiAssetId: string): ContractSubscribersResponse {
  return {
    consumers: [
      { subscriberId: `sub-${apiAssetId}-1`, subscriberEmail: 'team-orders@nextraceone.dev', consumerServiceName: 'Orders API', consumerServiceVersion: '1.4.2', subscriptionLevel: 'Breaking', notificationChannel: 'Email', isActive: true, subscribedAt: '2026-02-01T00:00:00.000Z' },
      { subscriberId: 'sub-2', subscriberEmail: 'team-inventory@nextraceone.dev', consumerServiceName: 'Inventory GraphQL', consumerServiceVersion: '1.0.0', subscriptionLevel: 'All', notificationChannel: 'Slack', isActive: true, subscribedAt: '2026-04-01T00:00:00.000Z' },
    ],
    totalCount: 2,
  };
}

/**
 * Live preview parseado de um spec (POST /contracts/parse-preview).
 * O catch-all `[]` faz o preview mostrar "Erro ao analisar"; esta forma
 * devolve um PreviewModel válido para renderizar operações e schemas.
 */
export function buildParsePreview(protocol: string): ParsePreviewResponse {
  return {
    isValid: true,
    errorMessage: null,
    preview: {
      protocol,
      title: 'Payments API v2',
      specVersion: '2.3.0',
      description: 'Serviço central de processamento de pagamentos e reconciliação.',
      servers: ['https://api.nextraceone.dev/v2'],
      tags: ['payments', 'billing'],
      securitySchemes: ['bearerAuth'],
      operations: [
        {
          operationId: 'createPayment',
          name: 'Create payment',
          description: 'Cria um novo pagamento.',
          method: 'POST',
          path: '/payments',
          isDeprecated: false,
          tags: ['payments'],
          inputParameters: [],
          outputFields: [
            { name: 'id', dataType: 'string', isRequired: true, format: 'uuid', isDeprecated: false },
            { name: 'status', dataType: 'string', isRequired: true, isDeprecated: false },
          ],
          requestBody: {
            contentType: 'application/json',
            isRequired: true,
            properties: [
              { name: 'amount', dataType: 'number', isRequired: true, isDeprecated: false },
              { name: 'currency', dataType: 'string', isRequired: true, isDeprecated: false },
            ],
            schemaRef: 'Payment',
          },
          responses: [
            { statusCode: '201', description: 'Created', contentType: 'application/json', properties: [], schemaRef: 'Payment' },
          ],
        },
        {
          operationId: 'getPayment',
          name: 'Get payment',
          description: 'Obtém um pagamento por id.',
          method: 'GET',
          path: '/payments/{id}',
          isDeprecated: false,
          tags: ['payments'],
          inputParameters: [
            { name: 'id', dataType: 'string', isRequired: true, format: 'uuid', isDeprecated: false },
          ],
          outputFields: [],
          requestBody: null,
          responses: [
            { statusCode: '200', description: 'OK', contentType: 'application/json', properties: [], schemaRef: 'Payment' },
          ],
        },
      ],
      schemas: [
        {
          name: 'Payment',
          dataType: 'object',
          isRequired: true,
          isDeprecated: false,
          children: [
            { name: 'amount', dataType: 'number', isRequired: true, isDeprecated: false },
            { name: 'currency', dataType: 'string', isRequired: true, isDeprecated: false },
          ],
        },
        {
          name: 'Money',
          dataType: 'object',
          isRequired: false,
          isDeprecated: false,
          children: [],
        },
      ],
      operationCount: 2,
      schemaCount: 2,
      hasSecurityDefinitions: true,
      hasExamples: true,
      hasDescriptions: true,
    },
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
