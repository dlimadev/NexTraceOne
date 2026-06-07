-- ═══════════════════════════════════════════════════════════════════════════════
-- NEXTRACEONE — Seed data: Catalog Module (CatalogDatabase)
-- Databases: CatalogGraphDbContext (cat_ tables),
--            ContractsDbContext (ctr_ tables),
--            DeveloperPortalDbContext (cat_portal_ / cat_subscriptions / cat_saved_searches)
-- All INSERT statements are idempotent: ON CONFLICT DO NOTHING.
-- ═══════════════════════════════════════════════════════════════════════════════

-- ═══ SERVICE ASSETS (CatalogGraphDbContext) ═══════════════════════════════════

INSERT INTO "ServiceAssets" (
  "Id", "Name", "DisplayName", "Description",
  "ServiceType", "Domain", "SystemArea",
  "TeamName", "TechnicalOwner", "BusinessOwner",
  "Criticality", "LifecycleStatus", "ExposureType",
  "DocumentationUrl", "RepositoryUrl",
  "TenantId", "SearchVector",
  "GitRepository", "CiPipelineUrl", "InfrastructureProvider", "HostingPlatform",
  "RuntimeLanguage", "RuntimeVersion", "SloTarget", "DataClassification",
  "RegulatoryScope", "ChangeFrequency", "ProductOwner", "ContactChannel",
  "OnCallRotationId", "Tier", "RowVersion",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'ca000001-0001-0000-0000-000000000001',
  'payment-service', 'Payment Service',
  'Core payment processing service. Handles authorisation, capture, refund and settlement for all payment instruments.',
  0, 'payments', 'core-platform',
  'payments-team', 'techlead@nextraceone.dev', 'product@nextraceone.dev',
  3, 3, 0,
  '', '',
  'a0000000-0000-0000-0000-000000000001',
  to_tsvector('english', 'payment-service Payment Service core-platform payments'),
  '', '', '', '',
  '', '', '', '',
  '', '', '', '',
  '', 0, 1,
  NOW(), 'system', NOW(), 'system', false
),
(
  'ca000002-0001-0000-0000-000000000001',
  'catalog-service', 'Catalog Service',
  'Service catalogue and API contract governance service. Source of truth for service ownership, dependencies and API definitions.',
  0, 'platform', 'core-platform',
  'platform-team', 'techlead@nextraceone.dev', 'product@nextraceone.dev',
  2, 3, 0,
  '', '',
  'a0000000-0000-0000-0000-000000000001',
  to_tsvector('english', 'catalog-service Catalog Service core-platform platform'),
  '', '', '', '',
  '', '', '', '',
  '', '', '', '',
  '', 0, 1,
  NOW(), 'system', NOW(), 'system', false
),
(
  'ca000003-0001-0000-0000-000000000001',
  'notification-service', 'Notification Service',
  'Centralised notification delivery service supporting email, SMS and in-app channels.',
  4, 'platform', 'core-platform',
  'platform-team', 'techlead@nextraceone.dev', '',
  1, 3, 0,
  '', '',
  'a0000000-0000-0000-0000-000000000001',
  to_tsvector('english', 'notification-service Notification Service core-platform platform'),
  '', '', '', '',
  '', '', '', '',
  '', '', '', '',
  '', 0, 1,
  NOW(), 'system', NOW(), 'system', false
),
(
  'ca000004-0001-0000-0000-000000000001',
  'identity-service', 'Identity Service',
  'Authentication, authorisation and user profile management service.',
  0, 'security', 'core-platform',
  'platform-team', 'techlead@nextraceone.dev', '',
  3, 3, 0,
  '', '',
  'a0000000-0000-0000-0000-000000000001',
  to_tsvector('english', 'identity-service Identity Service core-platform security'),
  '', '', '', '',
  '', '', '', '',
  '', '', '', '',
  '', 0, 1,
  NOW(), 'system', NOW(), 'system', false
),
(
  'ca000005-0001-0000-0000-000000000001',
  'analytics-gateway', 'Analytics Gateway',
  'API gateway for product analytics events ingestion and aggregation.',
  11, 'analytics', 'data-platform',
  'data-team', 'admin@nextraceone.dev', '',
  1, 3, 0,
  '', '',
  'a0000000-0000-0000-0000-000000000001',
  to_tsvector('english', 'analytics-gateway Analytics Gateway data-platform analytics'),
  '', '', '', '',
  '', '', '', '',
  '', '', '', '',
  '', 0, 1,
  NOW(), 'system', NOW(), 'system', false
)
ON CONFLICT DO NOTHING;

-- ═══ API ASSETS (CatalogGraphDbContext) ═══════════════════════════════════════

INSERT INTO "ApiAssets" (
  "Id", "Name", "RoutePattern", "Version",
  "Visibility", "OwnerServiceId", "IsDecommissioned"
) VALUES
(
  'ca010001-0001-0000-0000-000000000001',
  'payment-service-v1', '/api/v1/payments', 'v1',
  0,
  (SELECT "Id" FROM "ServiceAssets" WHERE "Name" = 'payment-service'),
  false
),
(
  'ca010002-0001-0000-0000-000000000001',
  'catalog-service-v1', '/api/v1', 'v1',
  0,
  (SELECT "Id" FROM "ServiceAssets" WHERE "Name" = 'catalog-service'),
  false
),
(
  'ca010003-0001-0000-0000-000000000001',
  'identity-service-v1', '/api/v1/identity', 'v1',
  0,
  (SELECT "Id" FROM "ServiceAssets" WHERE "Name" = 'identity-service'),
  false
)
ON CONFLICT DO NOTHING;

-- ═══ SPECTRAL RULESETS (ContractsDbContext) ═══════════════════════════════════

INSERT INTO "ContractLintRulesets" (
  "Id", "Name", "Description", "Version",
  "Content", "Origin", "DefaultExecutionMode", "EnforcementBehavior",
  "OrganizationId", "Owner", "Domain", "ApplicableServiceType", "ApplicableProtocols",
  "IsActive", "IsDefault",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'c7000001-0001-0000-0000-000000000001',
  'nextraceone-api-standards', 'NexTraceOne default API linting ruleset. Enforces naming, versioning, security and response standards.',
  '1.0.0',
  '{"rules":{"oas3-api-servers":true,"oas3-valid-schema-example":true,"operation-operationId":true,"operation-description":"warn","operation-tags":true,"no-eval-in-markdown":true,"no-script-tags-in-markdown":true,"openapi-tags":true,"tag-description":"warn","path-params":true}}',
  'Platform', 'Full', 'WarnAndFlag',
  NULL, 'platform-team', NULL, NULL, 'OpenApi',
  true, true,
  NOW(), 'system', NOW(), 'system', false
)
ON CONFLICT DO NOTHING;

-- ═══ CANONICAL ENTITIES (ContractsDbContext) ══════════════════════════════════

INSERT INTO "CanonicalEntities" (
  "Id", "Name", "Description",
  "Domain", "Category", "Owner", "Version",
  "State", "SchemaContent", "SchemaFormat",
  "Aliases", "Tags", "Criticality", "ReusePolicy",
  "OrganizationId",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'c7100001-0001-0000-0000-000000000001',
  'PaymentRequest', 'Canonical DTO for payment authorisation requests across all payment instruments.',
  'payments', 'Request', 'payments-team', '1.0.0',
  'Published',
  '{"type":"object","required":["amount","currency","paymentMethod"],"properties":{"amount":{"type":"number","minimum":0},"currency":{"type":"string","maxLength":3},"paymentMethod":{"type":"string","enum":["card","bank_transfer","wallet"]},"reference":{"type":"string","maxLength":100}}}',
  'JsonSchema',
  '{}', '{"payments","canonical"}', 2, 'recommended',
  NULL,
  NOW(), 'system', NOW(), 'system', false
),
(
  'c7100002-0001-0000-0000-000000000001',
  'ServiceAsset', 'Canonical representation of a registered service in the NexTraceOne catalogue.',
  'platform', 'Entity', 'platform-team', '1.0.0',
  'Published',
  '{"type":"object","required":["name","domain","teamName"],"properties":{"name":{"type":"string","maxLength":200},"domain":{"type":"string","maxLength":200},"teamName":{"type":"string","maxLength":200},"serviceType":{"type":"string","enum":["RestApi","GraphqlApi","GrpcService","KafkaProducer","KafkaConsumer","BackgroundService","LegacySystem","Gateway","ThirdParty"]},"criticality":{"type":"string","enum":["Critical","High","Medium","Low"]}}}',
  'JsonSchema',
  '{}', '{"platform","catalog","canonical"}', 1, 'recommended',
  NULL,
  NOW(), 'system', NOW(), 'system', false
)
ON CONFLICT DO NOTHING;

-- ═══ CONTRACT DRAFTS (ContractsDbContext) ════════════════════════════════════

INSERT INTO "Drafts" (
  "Id", "Title", "Description",
  "ServiceId", "ContractType", "Protocol",
  "SpecContent", "Format",
  "ProposedVersion", "Status", "Author",
  "BaseContractVersionId", "IsAiGenerated", "AiGenerationPrompt",
  "LastEditedAt", "LastEditedBy",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'c7200001-0001-0000-0000-000000000001',
  'Payment Service API v1',
  'OpenAPI 3.0 contract for the Payment Service REST API covering authorisation, capture and refund operations.',
  'ca000001-0001-0000-0000-000000000001',
  'Api', 'OpenApi',
  '{"openapi":"3.0.3","info":{"title":"Payment Service API","version":"1.0.0","description":"Core payment processing API"},"paths":{"/payments":{"post":{"operationId":"authorisePayment","summary":"Authorise a payment","tags":["Payments"],"requestBody":{"required":true,"content":{"application/json":{"schema":{"$ref":"#/components/schemas/PaymentRequest"}}}},"responses":{"202":{"description":"Payment accepted for processing"},"400":{"description":"Invalid request"}}}}},"/payments/{id}/capture":{"post":{"operationId":"capturePayment","summary":"Capture an authorised payment","tags":["Payments"],"parameters":[{"name":"id","in":"path","required":true,"schema":{"type":"string","format":"uuid"}}],"responses":{"200":{"description":"Payment captured"},"404":{"description":"Payment not found"}}}}}}',
  'Json',
  '1.0.0', 'Editing', 'system',
  NULL, false, NULL,
  NULL, NULL,
  NOW(), 'system', NOW(), 'system', false
)
ON CONFLICT DO NOTHING;
