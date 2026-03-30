-- ============================================================================
-- NexTraceOne Governance Module Seed Data
-- Idempotent: uses ON CONFLICT DO NOTHING
-- Includes: Teams, Domains, Packs, PackVersions, TeamDomainLinks
-- Table/column names must match EF Core entity configurations exactly.
-- ============================================================================

-- ── TEAMS ───────────────────────────────────────────────────────────────────

-- Platform Team: responsável pela infraestrutura e serviços transversais
INSERT INTO "gov_teams" ("Id", "Name", "DisplayName", "Description", "Status", "ParentOrganizationUnit", "CreatedAt", "UpdatedAt")
VALUES (
    '10000000-0000-0000-0000-000000000001',
    'platform-squad',
    'Platform',
    'Equipa responsável pela infraestrutura e serviços transversais da plataforma.',
    'Active',
    'Engineering',
    NOW(),
    NOW()
) ON CONFLICT DO NOTHING;

-- Commerce Team: serviços de comércio eletrónico e pagamentos
INSERT INTO "gov_teams" ("Id", "Name", "DisplayName", "Description", "Status", "ParentOrganizationUnit", "CreatedAt", "UpdatedAt")
VALUES (
    '10000000-0000-0000-0000-000000000002',
    'commerce-squad',
    'Commerce',
    'Equipa responsável pelos serviços de comércio eletrónico e pagamentos.',
    'Active',
    'Product',
    NOW(),
    NOW()
) ON CONFLICT DO NOTHING;

-- Identity Team: autenticação e autorização
INSERT INTO "gov_teams" ("Id", "Name", "DisplayName", "Description", "Status", "ParentOrganizationUnit", "CreatedAt", "UpdatedAt")
VALUES (
    '10000000-0000-0000-0000-000000000003',
    'identity-squad',
    'Identity',
    'Equipa responsável pela autenticação, autorização e gestão de identidades.',
    'Active',
    'Engineering',
    NOW(),
    NOW()
) ON CONFLICT DO NOTHING;

-- Data Team: ingestão e análise de dados
INSERT INTO "gov_teams" ("Id", "Name", "DisplayName", "Description", "Status", "ParentOrganizationUnit", "CreatedAt", "UpdatedAt")
VALUES (
    '10000000-0000-0000-0000-000000000004',
    'data-squad',
    'Data & Analytics',
    'Equipa responsável pela ingestão, transformação e análise de dados operacionais.',
    'Active',
    'Data',
    NOW(),
    NOW()
) ON CONFLICT DO NOTHING;

-- Observability Team: monitorização e alertas
INSERT INTO "gov_teams" ("Id", "Name", "DisplayName", "Description", "Status", "ParentOrganizationUnit", "CreatedAt", "UpdatedAt")
VALUES (
    '10000000-0000-0000-0000-000000000005',
    'observability-squad',
    'Observability',
    'Equipa responsável pela monitorização, alertas e observabilidade da plataforma.',
    'Active',
    'Engineering',
    NOW(),
    NOW()
) ON CONFLICT DO NOTHING;

-- ── GOVERNANCE DOMAINS ──────────────────────────────────────────────────────

-- Core Services Domain
INSERT INTO "gov_domains" ("Id", "Name", "DisplayName", "Description", "Criticality", "CapabilityClassification")
VALUES (
    '20000000-0000-0000-0000-000000000001',
    'core-services',
    'Core Services',
    'Serviços core da plataforma: autenticação, autorização, gestão de utilizadores e configurações.',
    'Critical',
    'Platform'
) ON CONFLICT DO NOTHING;

-- Payments Domain
INSERT INTO "gov_domains" ("Id", "Name", "DisplayName", "Description", "Criticality", "CapabilityClassification")
VALUES (
    '20000000-0000-0000-0000-000000000002',
    'payments',
    'Payments',
    'Serviços de processamento de pagamentos, gateways e reconciliação financeira.',
    'Critical',
    'Business'
) ON CONFLICT DO NOTHING;

-- Orders Domain
INSERT INTO "gov_domains" ("Id", "Name", "DisplayName", "Description", "Criticality", "CapabilityClassification")
VALUES (
    '20000000-0000-0000-0000-000000000003',
    'orders',
    'Orders',
    'Gestão de encomendas, carrinho de compras e fulfillment.',
    'High',
    'Business'
) ON CONFLICT DO NOTHING;

-- Notifications Domain
INSERT INTO "gov_domains" ("Id", "Name", "DisplayName", "Description", "Criticality", "CapabilityClassification")
VALUES (
    '20000000-0000-0000-0000-000000000004',
    'notifications',
    'Notifications',
    'Serviços de notificações: email, SMS, push e webhooks.',
    'Medium',
    'Support'
) ON CONFLICT DO NOTHING;

-- Analytics Domain
INSERT INTO "gov_domains" ("Id", "Name", "DisplayName", "Description", "Criticality", "CapabilityClassification")
VALUES (
    '20000000-0000-0000-0000-000000000005',
    'analytics',
    'Analytics',
    'Ingestão, processamento e visualização de dados analíticos e operacionais.',
    'Medium',
    'Data'
) ON CONFLICT DO NOTHING;

-- ── GOVERNANCE PACKS ────────────────────────────────────────────────────────

-- API Contract Standards Pack
INSERT INTO "gov_packs" ("Id", "Name", "DisplayName", "Description", "Category", "Status", "CurrentVersion")
VALUES (
    '30000000-0000-0000-0000-000000000001',
    'api-contract-standards',
    'API Contract Standards',
    'Padrões obrigatórios para contratos de API REST: versionamento, exemplos, schemas, headers.',
    'ContractQuality',
    'Active',
    '1.0.0'
) ON CONFLICT DO NOTHING;

-- Security Baseline Pack
INSERT INTO "gov_packs" ("Id", "Name", "DisplayName", "Description", "Category", "Status", "CurrentVersion")
VALUES (
    '30000000-0000-0000-0000-000000000002',
    'security-baseline',
    'Security Baseline',
    'Requisitos mínimos de segurança: autenticação, autorização, rate limiting, input validation.',
    'Security',
    'Active',
    '1.0.0'
) ON CONFLICT DO NOTHING;

-- Observability Standards Pack
INSERT INTO "gov_packs" ("Id", "Name", "DisplayName", "Description", "Category", "Status", "CurrentVersion")
VALUES (
    '30000000-0000-0000-0000-000000000003',
    'observability-standards',
    'Observability Standards',
    'Padrões de observabilidade: métricas, logs estruturados, traces, health checks.',
    'Observability',
    'Active',
    '1.0.0'
) ON CONFLICT DO NOTHING;

-- Change Management Pack
INSERT INTO "gov_packs" ("Id", "Name", "DisplayName", "Description", "Category", "Status", "CurrentVersion")
VALUES (
    '30000000-0000-0000-0000-000000000004',
    'change-management',
    'Change Management',
    'Regras de gestão de mudanças: aprovações, blast radius, rollback plans.',
    'ChangeManagement',
    'Draft',
    NULL
) ON CONFLICT DO NOTHING;

-- ── GOVERNANCE PACK VERSIONS ────────────────────────────────────────────────

-- API Contract Standards v1.0.0
INSERT INTO "gov_pack_versions" ("Id", "PackId", "Version", "Rules", "DefaultEnforcementMode", "ChangeDescription", "CreatedBy", "PublishedAt")
VALUES (
    '40000000-0000-0000-0000-000000000001',
    '30000000-0000-0000-0000-000000000001',
    '1.0.0',
    '[
        {"ruleId": "api-must-have-version", "ruleName": "API Must Have Version", "description": "Toda API deve ter versionamento explícito no path ou header.", "category": "ContractQuality", "defaultEnforcementMode": "Blocking", "isRequired": true},
        {"ruleId": "api-must-have-examples", "ruleName": "API Must Have Examples", "description": "Toda operação deve ter pelo menos um exemplo de request e response.", "category": "ContractQuality", "defaultEnforcementMode": "Warning", "isRequired": false},
        {"ruleId": "api-must-have-description", "ruleName": "API Must Have Description", "description": "Toda operação e parâmetro deve ter descrição legível.", "category": "ContractQuality", "defaultEnforcementMode": "Warning", "isRequired": false}
    ]',
    'Blocking',
    'Versão inicial do pack de padrões de contratos API.',
    'system',
    NOW()
) ON CONFLICT DO NOTHING;

-- Security Baseline v1.0.0
INSERT INTO "gov_pack_versions" ("Id", "PackId", "Version", "Rules", "DefaultEnforcementMode", "ChangeDescription", "CreatedBy", "PublishedAt")
VALUES (
    '40000000-0000-0000-0000-000000000002',
    '30000000-0000-0000-0000-000000000002',
    '1.0.0',
    '[
        {"ruleId": "must-require-auth", "ruleName": "Must Require Authentication", "description": "Toda API pública deve requerer autenticação.", "category": "Security", "defaultEnforcementMode": "Blocking", "isRequired": true},
        {"ruleId": "must-have-rate-limit", "ruleName": "Must Have Rate Limiting", "description": "Toda API deve ter rate limiting configurado.", "category": "Security", "defaultEnforcementMode": "Warning", "isRequired": false},
        {"ruleId": "no-sensitive-in-query", "ruleName": "No Sensitive Data in Query", "description": "Dados sensíveis não podem ser passados em query strings.", "category": "Security", "defaultEnforcementMode": "Blocking", "isRequired": true}
    ]',
    'Blocking',
    'Versão inicial do pack de segurança baseline.',
    'system',
    NOW()
) ON CONFLICT DO NOTHING;

-- Observability Standards v1.0.0
INSERT INTO "gov_pack_versions" ("Id", "PackId", "Version", "Rules", "DefaultEnforcementMode", "ChangeDescription", "CreatedBy", "PublishedAt")
VALUES (
    '40000000-0000-0000-0000-000000000003',
    '30000000-0000-0000-0000-000000000003',
    '1.0.0',
    '[
        {"ruleId": "must-have-health-check", "ruleName": "Must Have Health Check", "description": "Todo serviço deve expor endpoint /health.", "category": "Observability", "defaultEnforcementMode": "Blocking", "isRequired": true},
        {"ruleId": "must-log-structured", "ruleName": "Must Use Structured Logging", "description": "Logs devem ser estruturados em JSON com campos padrão.", "category": "Observability", "defaultEnforcementMode": "Warning", "isRequired": false},
        {"ruleId": "must-propagate-trace", "ruleName": "Must Propagate Trace Context", "description": "Serviços devem propagar W3C Trace Context.", "category": "Observability", "defaultEnforcementMode": "Warning", "isRequired": false}
    ]',
    'Warning',
    'Versão inicial do pack de padrões de observabilidade.',
    'system',
    NOW()
) ON CONFLICT DO NOTHING;

-- ── TEAM DOMAIN LINKS ───────────────────────────────────────────────────────

-- Platform Team owns Core Services
INSERT INTO "gov_team_domain_links" ("Id", "TeamId", "DomainId", "OwnershipType", "LinkedAt")
VALUES (
    '50000000-0000-0000-0000-000000000001',
    '10000000-0000-0000-0000-000000000001',
    '20000000-0000-0000-0000-000000000001',
    'Primary',
    NOW()
) ON CONFLICT DO NOTHING;

-- Identity Team shares Core Services
INSERT INTO "gov_team_domain_links" ("Id", "TeamId", "DomainId", "OwnershipType", "LinkedAt")
VALUES (
    '50000000-0000-0000-0000-000000000002',
    '10000000-0000-0000-0000-000000000003',
    '20000000-0000-0000-0000-000000000001',
    'Shared',
    NOW()
) ON CONFLICT DO NOTHING;

-- Commerce Team owns Payments
INSERT INTO "gov_team_domain_links" ("Id", "TeamId", "DomainId", "OwnershipType", "LinkedAt")
VALUES (
    '50000000-0000-0000-0000-000000000003',
    '10000000-0000-0000-0000-000000000002',
    '20000000-0000-0000-0000-000000000002',
    'Primary',
    NOW()
) ON CONFLICT DO NOTHING;

-- Commerce Team owns Orders
INSERT INTO "gov_team_domain_links" ("Id", "TeamId", "DomainId", "OwnershipType", "LinkedAt")
VALUES (
    '50000000-0000-0000-0000-000000000004',
    '10000000-0000-0000-0000-000000000002',
    '20000000-0000-0000-0000-000000000003',
    'Primary',
    NOW()
) ON CONFLICT DO NOTHING;

-- Platform Team owns Notifications
INSERT INTO "gov_team_domain_links" ("Id", "TeamId", "DomainId", "OwnershipType", "LinkedAt")
VALUES (
    '50000000-0000-0000-0000-000000000005',
    '10000000-0000-0000-0000-000000000001',
    '20000000-0000-0000-0000-000000000004',
    'Primary',
    NOW()
) ON CONFLICT DO NOTHING;

-- Data Team owns Analytics
INSERT INTO "gov_team_domain_links" ("Id", "TeamId", "DomainId", "OwnershipType", "LinkedAt")
VALUES (
    '50000000-0000-0000-0000-000000000006',
    '10000000-0000-0000-0000-000000000004',
    '20000000-0000-0000-0000-000000000005',
    'Primary',
    NOW()
) ON CONFLICT DO NOTHING;

-- Observability Team shares Analytics
INSERT INTO "gov_team_domain_links" ("Id", "TeamId", "DomainId", "OwnershipType", "LinkedAt")
VALUES (
    '50000000-0000-0000-0000-000000000007',
    '10000000-0000-0000-0000-000000000005',
    '20000000-0000-0000-0000-000000000005',
    'Shared',
    NOW()
) ON CONFLICT DO NOTHING;

-- ── DELEGATED ADMINISTRATIONS ───────────────────────────────────────────────

-- Admin delegation to tech lead for Platform team
INSERT INTO "gov_delegated_administrations" ("Id", "GranteeUserId", "GranteeDisplayName", "Scope", "TeamId", "DomainId", "Reason", "IsActive", "GrantedAt", "ExpiresAt", "RevokedAt")
VALUES (
    '60000000-0000-0000-0000-000000000001',
    'techlead-platform',
    'Tech Lead Platform',
    'TeamAdmin',
    '10000000-0000-0000-0000-000000000001',
    NULL,
    'Delegação de administração da equipa Platform ao tech lead.',
    true,
    NOW(),
    NULL,
    NULL
) ON CONFLICT DO NOTHING;

-- ── GOVERNANCE WAIVERS ──────────────────────────────────────────────────────

-- Sample approved waiver
INSERT INTO "gov_waivers" ("Id", "PackId", "RuleId", "Scope", "ScopeType", "Justification", "Status", "RequestedBy", "RequestedAt", "ReviewedBy", "ReviewedAt", "ExpiresAt", "EvidenceLinks")
VALUES (
    '70000000-0000-0000-0000-000000000001',
    '30000000-0000-0000-0000-000000000002',
    'must-have-rate-limit',
    'internal-batch-service',
    'Service',
    'Serviço batch interno sem exposição externa. Rate limiting não aplicável.',
    'Approved',
    'engineer-data',
    NOW() - interval '7 days',
    'admin-platform',
    NOW() - interval '5 days',
    NOW() + interval '180 days',
    '["https://wiki.internal/batch-service-architecture"]'
) ON CONFLICT DO NOTHING;

-- Sample pending waiver
INSERT INTO "gov_waivers" ("Id", "PackId", "RuleId", "Scope", "ScopeType", "Justification", "Status", "RequestedBy", "RequestedAt", "ReviewedBy", "ReviewedAt", "ExpiresAt", "EvidenceLinks")
VALUES (
    '70000000-0000-0000-0000-000000000002',
    '30000000-0000-0000-0000-000000000001',
    'api-must-have-examples',
    'legacy-integration-api',
    'Service',
    'API de integração legacy em processo de migração. Exemplos serão adicionados na v2.',
    'Pending',
    'engineer-commerce',
    NOW() - interval '2 days',
    NULL,
    NULL,
    NOW() + interval '90 days',
    '["https://jira.internal/LEGACY-123", "https://wiki.internal/migration-plan"]'
) ON CONFLICT DO NOTHING;

-- ── GOVERNANCE ROLLOUT RECORDS ──────────────────────────────────────────────

-- Sample completed rollout
INSERT INTO "gov_rollout_records" ("Id", "PackId", "VersionId", "Scope", "ScopeType", "EnforcementMode", "Status", "InitiatedBy", "InitiatedAt", "CompletedAt")
VALUES (
    '80000000-0000-0000-0000-000000000001',
    '30000000-0000-0000-0000-000000000001',
    '40000000-0000-0000-0000-000000000001',
    'core-services',
    'Domain',
    'Blocking',
    'Completed',
    'admin-platform',
    NOW() - interval '30 days',
    NOW() - interval '30 days' + interval '5 minutes'
) ON CONFLICT DO NOTHING;

-- Sample in-progress rollout
INSERT INTO "gov_rollout_records" ("Id", "PackId", "VersionId", "Scope", "ScopeType", "EnforcementMode", "Status", "InitiatedBy", "InitiatedAt", "CompletedAt")
VALUES (
    '80000000-0000-0000-0000-000000000002',
    '30000000-0000-0000-0000-000000000003',
    '40000000-0000-0000-0000-000000000003',
    'payments',
    'Domain',
    'Warning',
    'InProgress',
    'admin-platform',
    NOW() - interval '1 hour',
    NULL
) ON CONFLICT DO NOTHING;
