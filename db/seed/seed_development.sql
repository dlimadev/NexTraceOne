-- =============================================================================
-- NexTraceOne — seed_development.sql
-- =============================================================================
-- Dados de teste para ambiente de desenvolvimento local.
-- EXECUTAR APENAS EM AMBIENTES NÃO PRODUTIVOS.
--
-- Requer seed_production.sql aplicado antes (tenant, admin e ambientes devem
-- existir).
--
-- Conteúdo:
--   1. Utilizadores de teste — um por papel do sistema
--   2. Vínculos utilizadores → tenant (tenant memberships)
--   3. Equipas de governança (gov_teams)
--   4. Domínios de governança (gov_domains)
--   5. Vínculos equipa-domínio (gov_team_domain_links)
--   6. Governance Packs (gov_packs)
--   7. Versões dos Governance Packs (gov_pack_versions)
--   8. Service Assets do catálogo (cat_service_assets)
--   9. Releases de exemplo para Change Intelligence (chg_releases)
--
-- Credenciais dos utilizadores de teste:
--   admin@nextraceone.io    → Admin@2026!     (PlatformAdmin)  — criado em seed_production.sql
--   techlead@nextraceone.io → TechLead@2026!  (TechLead)
--   dev@nextraceone.io      → Dev@2026!       (Developer)
--   viewer@nextraceone.io   → Viewer@2026!    (Viewer)
--   auditor@nextraceone.io  → Auditor@2026!   (Auditor)
--   secreview@nextraceone.io→ SecReview@2026! (SecurityReview)
--   approval@nextraceone.io → Approval@2026!  (ApprovalOnly)
--
-- Idempotente: seguro de executar mais de uma vez.
-- =============================================================================

BEGIN;

-- ---------------------------------------------------------------------------
-- 1. Utilizadores de teste — um por papel do sistema
--    Todos com PBKDF2/SHA256 — formato v1.{base64_salt}.{base64_hash}, 100 000 iterações.
-- ---------------------------------------------------------------------------
INSERT INTO iam_users (
    "Id",
    "Email",
    first_name,
    last_name,
    "PasswordHash",
    "IsActive",
    "FailedLoginAttempts",
    "MfaEnabled",
    "FederationProvider",
    "ExternalId"
)
VALUES
    (
        'b0000000-0000-0000-0000-000000000002',
        'techlead@nextraceone.io',
        'Ana',
        'Silva',
        'v1.0b46DSzRMt94/4yLnPUYaw==.KAWR5yo5D3blTBYQjhs7ujRDYV553nzSPdkNzLRpmZw=',
        true, 0, false, NULL, NULL
    ),
    (
        'b0000000-0000-0000-0000-000000000003',
        'dev@nextraceone.io',
        'Bruno',
        'Costa',
        'v1.cBmEmnRiLd/BIAOmoOH7BA==.XLmgYFh4M5sIOoWlHkk9Larp4e9nUCLo4WvdtuL4XGY=',
        true, 0, false, NULL, NULL
    ),
    (
        'b0000000-0000-0000-0000-000000000004',
        'viewer@nextraceone.io',
        'Carla',
        'Mendes',
        'v1.gGI8arVvEQhYACsvoDZ2yQ==.c+nPZBrQ2GTf+AndKRakJ4hrzSj4iZB93wY3z9BvFX8=',
        true, 0, false, NULL, NULL
    ),
    (
        'b0000000-0000-0000-0000-000000000005',
        'auditor@nextraceone.io',
        'Daniel',
        'Ferreira',
        'v1.cidbqZdl+wrJiuUprvaOJA==.dCNhQiRcqWCEzX4EY1yfqPI/RgcGbv4E2SketMZyDME=',
        true, 0, false, NULL, NULL
    ),
    (
        'b0000000-0000-0000-0000-000000000006',
        'secreview@nextraceone.io',
        'Elena',
        'Rocha',
        'v1.GaHQSxY9K0ogtqHzKvp7Tg==.ePf5tCuwHGaRGNS/F4i8vtVZXQlVSvEjfK1TPs4eRlg=',
        true, 0, false, NULL, NULL
    ),
    (
        'b0000000-0000-0000-0000-000000000007',
        'approval@nextraceone.io',
        'Fabio',
        'Lima',
        'v1.fy4scsVL1c0gE/b6V0QxaQ==.OzvaWSHf9DUC+kroOllidnHAWPgilsa80CqHwvvkedM=',
        true, 0, false, NULL, NULL
    )
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 2. Vínculos utilizadores de teste → tenant nextraceone
--    Roles seeded by EF migrations (iam_roles.HasData):
--      TechLead     : 1e91a557-fade-46df-b248-0f5f5899c002
--      Developer    : 1e91a557-fade-46df-b248-0f5f5899c003
--      Viewer       : 1e91a557-fade-46df-b248-0f5f5899c004
--      Auditor      : 1e91a557-fade-46df-b248-0f5f5899c005
--      SecurityReview: 1e91a557-fade-46df-b248-0f5f5899c006
--      ApprovalOnly : 1e91a557-fade-46df-b248-0f5f5899c007
-- ---------------------------------------------------------------------------
INSERT INTO iam_tenant_memberships (
    "Id",
    "UserId",
    "TenantId",
    "RoleId",
    "JoinedAt",
    "IsActive"
)
VALUES
    (
        'd0000000-0000-0000-0000-000000000002',
        'b0000000-0000-0000-0000-000000000002',
        'a0000000-0000-0000-0000-000000000001',
        '1e91a557-fade-46df-b248-0f5f5899c002', -- TechLead
        NOW(), true
    ),
    (
        'd0000000-0000-0000-0000-000000000003',
        'b0000000-0000-0000-0000-000000000003',
        'a0000000-0000-0000-0000-000000000001',
        '1e91a557-fade-46df-b248-0f5f5899c003', -- Developer
        NOW(), true
    ),
    (
        'd0000000-0000-0000-0000-000000000004',
        'b0000000-0000-0000-0000-000000000004',
        'a0000000-0000-0000-0000-000000000001',
        '1e91a557-fade-46df-b248-0f5f5899c004', -- Viewer
        NOW(), true
    ),
    (
        'd0000000-0000-0000-0000-000000000005',
        'b0000000-0000-0000-0000-000000000005',
        'a0000000-0000-0000-0000-000000000001',
        '1e91a557-fade-46df-b248-0f5f5899c005', -- Auditor
        NOW(), true
    ),
    (
        'd0000000-0000-0000-0000-000000000006',
        'b0000000-0000-0000-0000-000000000006',
        'a0000000-0000-0000-0000-000000000001',
        '1e91a557-fade-46df-b248-0f5f5899c006', -- SecurityReview
        NOW(), true
    ),
    (
        'd0000000-0000-0000-0000-000000000007',
        'b0000000-0000-0000-0000-000000000007',
        'a0000000-0000-0000-0000-000000000001',
        '1e91a557-fade-46df-b248-0f5f5899c007', -- ApprovalOnly
        NOW(), true
    )
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 3. Equipas de governança
--    Status válidos: 'Active' | 'Inactive' | 'Archived'
-- ---------------------------------------------------------------------------
INSERT INTO gov_teams (
    "Id",
    "Name",
    "DisplayName",
    "Description",
    "Status",
    "ParentOrganizationUnit",
    "CreatedAt",
    "UpdatedAt"
)
VALUES
    (
        'e0000000-0000-0000-0000-000000000001',
        'platform-engineering',
        'Platform Engineering',
        'Responsável pela infraestrutura da plataforma NexTraceOne, observabilidade e tooling interno.',
        'Active',
        'Engineering',
        NOW(),
        NOW()
    ),
    (
        'e0000000-0000-0000-0000-000000000002',
        'payments-checkout',
        'Payments & Checkout',
        'Responsável pelos serviços de pagamento, checkout e integrações com providers financeiros.',
        'Active',
        'Product Engineering',
        NOW(),
        NOW()
    ),
    (
        'e0000000-0000-0000-0000-000000000003',
        'identity-security',
        'Identity & Security',
        'Responsável por autenticação, autorização, gestão de sessões e políticas de segurança.',
        'Active',
        'Engineering',
        NOW(),
        NOW()
    )
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 4. Domínios de governança
--    Criticality válidos: 'Low' | 'Medium' | 'High' | 'Critical'
-- ---------------------------------------------------------------------------
INSERT INTO gov_domains (
    "Id",
    "Name",
    "DisplayName",
    "Description",
    "Criticality",
    "CapabilityClassification",
    "CreatedAt",
    "UpdatedAt"
)
VALUES
    (
        'f0000000-0000-0000-0000-000000000001',
        'platform',
        'Platform',
        'Domínio da plataforma — serviços de suporte, observabilidade, tooling e contratos internos.',
        'High',
        'Platform Capability',
        NOW(),
        NOW()
    ),
    (
        'f0000000-0000-0000-0000-000000000002',
        'payments',
        'Payments',
        'Domínio de pagamentos — gateway, processamento de transacções e reconciliação financeira.',
        'Critical',
        'Core Business Capability',
        NOW(),
        NOW()
    ),
    (
        'f0000000-0000-0000-0000-000000000003',
        'security',
        'Security',
        'Domínio de segurança — identidade, acesso, políticas de proteção e conformidade regulatória.',
        'Critical',
        'Enabling Capability',
        NOW(),
        NOW()
    )
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 5. Vínculos equipa → domínio
--    OwnershipType: 'Primary' | 'Supporting' | 'Consuming' (conforme enumeração do domínio)
-- ---------------------------------------------------------------------------
INSERT INTO gov_team_domain_links (
    "Id",
    "TeamId",
    "DomainId",
    "OwnershipType",
    "LinkedAt"
)
VALUES
    (
        'a5000000-0000-0000-0000-000000000001',
        'e0000000-0000-0000-0000-000000000001',
        'f0000000-0000-0000-0000-000000000001',
        'Primary',
        NOW()
    ),
    (
        'a5000000-0000-0000-0000-000000000002',
        'e0000000-0000-0000-0000-000000000002',
        'f0000000-0000-0000-0000-000000000002',
        'Primary',
        NOW()
    ),
    (
        'a5000000-0000-0000-0000-000000000003',
        'e0000000-0000-0000-0000-000000000003',
        'f0000000-0000-0000-0000-000000000003',
        'Primary',
        NOW()
    )
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 6. Governance Packs
--    Status válidos: 'Draft' | 'Published' | 'Deprecated' | 'Archived'
-- ---------------------------------------------------------------------------
INSERT INTO gov_packs (
    "Id",
    "Name",
    "DisplayName",
    "Description",
    "Category",
    "Status",
    "CurrentVersion",
    "CreatedAt",
    "UpdatedAt"
)
VALUES
    (
        'a1000000-0000-0000-0000-000000000001',
        'api-contract-standards',
        'API Contract Standards',
        'Políticas e regras para definição, versionamento e publicação de contratos REST e de eventos. Garante consistência e rastreabilidade dos contratos da plataforma.',
        'ApiContracts',
        'Published',
        '1.0.0',
        NOW(),
        NOW()
    ),
    (
        'a1000000-0000-0000-0000-000000000002',
        'change-management-policy',
        'Change Management Policy',
        'Políticas de classificação de mudanças, janelas de deploy, critérios de aprovação e rollback. Reduz o risco de incidentes causados por mudanças não controladas.',
        'ChangeManagement',
        'Published',
        '1.0.0',
        NOW(),
        NOW()
    )
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 7. Versões dos Governance Packs
--    DefaultEnforcementMode: 'Advisory' | 'Enforced' | 'Blocking'
-- ---------------------------------------------------------------------------
INSERT INTO gov_pack_versions (
    "Id",
    "PackId",
    "Version",
    "Rules",
    "DefaultEnforcementMode",
    "ChangeDescription",
    "CreatedBy",
    "CreatedAt",
    "PublishedAt"
)
VALUES
    (
        'a2000000-0000-0000-0000-000000000001',
        'a1000000-0000-0000-0000-000000000001',
        '1.0.0',
        '{"rules":[{"id":"api-001","name":"OpenAPI spec obrigatorio","description":"Todo contrato REST deve ter especificacao OpenAPI 3.x valida","severity":"error"},{"id":"api-002","name":"Versao semver obrigatoria","description":"O campo version deve seguir o formato semver x.y.z","severity":"error"},{"id":"api-003","name":"Contacto de equipa obrigatorio","description":"O campo info.contact deve estar preenchido com email e nome da equipa","severity":"warning"},{"id":"api-004","name":"Descricao de endpoints obrigatoria","description":"Todos os endpoints publicados devem ter descricao preenchida","severity":"warning"}]}',
        'Advisory',
        'Versão inicial do pack de contratos API. Cobre regras de formato e rastreabilidade.',
        'seed',
        NOW(),
        NOW()
    ),
    (
        'a2000000-0000-0000-0000-000000000002',
        'a1000000-0000-0000-0000-000000000002',
        '1.0.0',
        '{"rules":[{"id":"chg-001","name":"Work item obrigatorio para mudancas major","description":"Releases de nivel Major ou Breaking devem referenciar um work item","severity":"error"},{"id":"chg-002","name":"Aprovacao TechLead para producao","description":"Deploys em producao requerem aprovacao de TechLead ou superior","severity":"error"},{"id":"chg-003","name":"Freeze window respeitada","description":"Nenhum deploy pode ser realizado durante janelas de freeze activas","severity":"error"},{"id":"chg-004","name":"Blast radius calculado antes do deploy","description":"Releases breaking devem ter blast radius calculado antes do deploy em producao","severity":"warning"}]}',
        'Enforced',
        'Versão inicial da política de gestão de mudanças. Foco em producao e mudancas breaking.',
        'seed',
        NOW(),
        NOW()
    )
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 8. Service Assets do catálogo
--    ServiceType válidos: 'RestApi' | 'GraphqlApi' | 'GrpcService' | 'KafkaProducer' |
--                         'KafkaConsumer' | 'BackgroundService' | 'LegacySystem' | 'Gateway' | 'ThirdParty'
--    Criticality válidos: 'Critical' | 'High' | 'Medium' | 'Low'
--    LifecycleStatus válidos: 'Planning' | 'Development' | 'Staging' | 'Active' | 'Deprecating' | 'Deprecated' | 'Retired'
--    ExposureType válidos: 'Internal' | 'Partner' | 'Public'
-- ---------------------------------------------------------------------------
INSERT INTO cat_service_assets (
    "Id",
    "Name",
    "DisplayName",
    "Description",
    "ServiceType",
    "Domain",
    "SystemArea",
    "TeamName",
    "TechnicalOwner",
    "BusinessOwner",
    "Criticality",
    "LifecycleStatus",
    "ExposureType",
    "DocumentationUrl",
    "RepositoryUrl"
)
VALUES
    (
        'a3000000-0000-0000-0000-000000000001',
        'nextraceone-platform-api',
        'NexTraceOne Platform API',
        'API principal da plataforma NexTraceOne. Concentra endpoints de governança, contratos, change intelligence e catálogo de serviços.',
        'RestApi',
        'platform',
        'Platform Core',
        'platform-engineering',
        'admin@nextraceone.io',
        'admin@nextraceone.io',
        'High',
        'Active',
        'Internal',
        'https://docs.nextraceone.io/platform-api',
        'https://github.com/nextraceone/platform-api'
    ),
    (
        'a3000000-0000-0000-0000-000000000002',
        'payment-gateway',
        'Payment Gateway',
        'Gateway de pagamentos responsável por processar transacções, validar métodos de pagamento e integrar com providers externos (Stripe, Adyen).',
        'RestApi',
        'payments',
        'Payments Core',
        'payments-checkout',
        'techlead@nextraceone.io',
        'techlead@nextraceone.io',
        'Critical',
        'Active',
        'Internal',
        'https://docs.nextraceone.io/payment-gateway',
        'https://github.com/nextraceone/payment-gateway'
    ),
    (
        'a3000000-0000-0000-0000-000000000003',
        'identity-service',
        'Identity Service',
        'Serviço de identidade e autenticação. Gere utilizadores, sessões, tokens JWT, MFA e políticas de acesso por tenant.',
        'RestApi',
        'security',
        'Identity Core',
        'identity-security',
        'secreview@nextraceone.io',
        'secreview@nextraceone.io',
        'Critical',
        'Active',
        'Internal',
        'https://docs.nextraceone.io/identity-service',
        'https://github.com/nextraceone/identity-service'
    ),
    (
        'a3000000-0000-0000-0000-000000000004',
        'notification-worker',
        'Notification Worker',
        'Worker de envio de notificações por email, webhook e in-app. Processamento assíncrono via outbox pattern.',
        'BackgroundService',
        'platform',
        'Notifications',
        'platform-engineering',
        'dev@nextraceone.io',
        'admin@nextraceone.io',
        'Medium',
        'Active',
        'Internal',
        'https://docs.nextraceone.io/notification-worker',
        'https://github.com/nextraceone/notification-worker'
    )
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 9. Releases de exemplo para Change Intelligence
--
--    ChangeLevel:      0=None | 1=Minor | 2=Moderate | 3=Major | 4=Breaking
--    Status:           0=Pending | 1=InProgress | 2=Completed | 3=Failed | 4=RolledBack
--    ChangeType:       0=Deployment (valor padrão assumido)
--    ConfidenceStatus: 0=Unknown
--    ValidationStatus: 0=NotValidated
--
--    tenant_id e environment_id são snake_case (HasColumnName explícito no EF).
--    Restantes colunas são PascalCase (convenção EF Core + Npgsql sem snake_case global).
-- ---------------------------------------------------------------------------
INSERT INTO chg_releases (
    "Id",
    "ApiAssetId",
    "ServiceName",
    "Version",
    "Environment",
    "PipelineSource",
    "CommitSha",
    "ChangeLevel",
    "Status",
    "ChangeScore",
    "ChangeType",
    "ConfidenceStatus",
    "ValidationStatus",
    "WorkItemReference",
    "TeamName",
    "Domain",
    "Description",
    "CreatedAt",
    tenant_id,
    environment_id
)
VALUES
    (
        -- nextraceone-platform-api v2.4.1 → Production (Completed, Moderate)
        'a4000000-0000-0000-0000-000000000001',
        'a3000000-0000-0000-0000-000000000001',
        'nextraceone-platform-api',
        '2.4.1',
        'production',
        'https://github.com/nextraceone/platform-api/actions/runs/100001',
        'a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f60001',
        2,          -- Moderate
        2,          -- Completed
        0.3000,
        0,          -- Deployment
        0,          -- Unknown
        0,          -- NotValidated
        'NXT-1042',
        'platform-engineering',
        'platform',
        'Actualização de dependências de segurança e melhoria de performance nos endpoints de catálogo. Sem breaking changes.',
        NOW() - INTERVAL '2 days',
        'a0000000-0000-0000-0000-000000000001',
        'c0000000-0000-0000-0000-000000000003'
    ),
    (
        -- payment-gateway v1.8.3 → Staging (InProgress, Minor)
        'a4000000-0000-0000-0000-000000000002',
        'a3000000-0000-0000-0000-000000000002',
        'payment-gateway',
        '1.8.3',
        'staging',
        'https://github.com/nextraceone/payment-gateway/actions/runs/200002',
        'b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a10002',
        1,          -- Minor
        1,          -- InProgress
        0.1500,
        0,          -- Deployment
        0,          -- Unknown
        0,          -- NotValidated
        'PAY-880',
        'payments-checkout',
        'payments',
        'Correcção de bug no cálculo de taxas para transacções internacionais com conversão de moeda.',
        NOW() - INTERVAL '3 hours',
        'a0000000-0000-0000-0000-000000000001',
        'c0000000-0000-0000-0000-000000000002'
    ),
    (
        -- identity-service v3.1.0 → Production (Completed, Major — alto risco)
        'a4000000-0000-0000-0000-000000000003',
        'a3000000-0000-0000-0000-000000000003',
        'identity-service',
        '3.1.0',
        'production',
        'https://github.com/nextraceone/identity-service/actions/runs/300003',
        'c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b20003',
        3,          -- Major
        2,          -- Completed
        0.7200,
        0,          -- Deployment
        0,          -- Unknown
        0,          -- NotValidated
        'SEC-312',
        'identity-security',
        'security',
        'Migração para OIDC v3 com suporte a passkeys e WebAuthn. Breaking change no endpoint /auth/token — clientes devem migrar para o novo contrato.',
        NOW() - INTERVAL '5 days',
        'a0000000-0000-0000-0000-000000000001',
        'c0000000-0000-0000-0000-000000000003'
    ),
    (
        -- notification-worker v1.2.0 → Development (Pending, Minor)
        'a4000000-0000-0000-0000-000000000004',
        'a3000000-0000-0000-0000-000000000004',
        'notification-worker',
        '1.2.0',
        'development',
        'https://github.com/nextraceone/notification-worker/actions/runs/400004',
        'd4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c30004',
        1,          -- Minor
        0,          -- Pending
        0.0800,
        0,          -- Deployment
        0,          -- Unknown
        0,          -- NotValidated
        'NXT-1098',
        'platform-engineering',
        'platform',
        'Adição de suporte a webhooks configuráveis por tenant. Processamento assíncrono via novo worker dedicado.',
        NOW() - INTERVAL '30 minutes',
        'a0000000-0000-0000-0000-000000000001',
        'c0000000-0000-0000-0000-000000000001'
    )
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 10. Acessos de ambiente para utilizadores de teste
--     AccessLevel válidos: 'read' | 'write' | 'admin' | 'none'
--     GrantedBy: admin (b0000000-0000-0000-0000-000000000001)
--
--     Política de acesso por papel:
--       TechLead    → development: admin | staging: admin | production: write
--       Developer   → development: write | staging: read  | production: read
--       Viewer      → development: read  | staging: read  | production: read
--       Auditor     → development: read  | staging: read  | production: read
--       SecReview   → development: read  | staging: read  | production: read
--       ApprovalOnly→ development: read  | staging: read  | production: read
-- ---------------------------------------------------------------------------
INSERT INTO env_environment_accesses (
    "Id",
    "UserId",
    "TenantId",
    "EnvironmentId",
    "AccessLevel",
    "GrantedAt",
    "ExpiresAt",
    "GrantedBy",
    "IsActive",
    "RevokedAt"
)
VALUES
    -- TechLead
    ('e2000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', 'admin', NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    ('e2000000-0000-0000-0000-000000000002', 'b0000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000002', 'admin', NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    ('e2000000-0000-0000-0000-000000000003', 'b0000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000003', 'write', NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    -- Developer
    ('e2000000-0000-0000-0000-000000000004', 'b0000000-0000-0000-0000-000000000003', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', 'write', NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    ('e2000000-0000-0000-0000-000000000005', 'b0000000-0000-0000-0000-000000000003', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000002', 'read',  NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    ('e2000000-0000-0000-0000-000000000006', 'b0000000-0000-0000-0000-000000000003', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000003', 'read',  NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    -- Viewer
    ('e2000000-0000-0000-0000-000000000007', 'b0000000-0000-0000-0000-000000000004', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', 'read',  NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    ('e2000000-0000-0000-0000-000000000008', 'b0000000-0000-0000-0000-000000000004', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000002', 'read',  NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    ('e2000000-0000-0000-0000-000000000009', 'b0000000-0000-0000-0000-000000000004', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000003', 'read',  NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    -- Auditor
    ('e2000000-0000-0000-0000-000000000010', 'b0000000-0000-0000-0000-000000000005', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', 'read',  NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    ('e2000000-0000-0000-0000-000000000011', 'b0000000-0000-0000-0000-000000000005', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000002', 'read',  NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    ('e2000000-0000-0000-0000-000000000012', 'b0000000-0000-0000-0000-000000000005', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000003', 'read',  NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    -- SecurityReview
    ('e2000000-0000-0000-0000-000000000013', 'b0000000-0000-0000-0000-000000000006', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', 'read',  NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    ('e2000000-0000-0000-0000-000000000014', 'b0000000-0000-0000-0000-000000000006', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000002', 'read',  NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    ('e2000000-0000-0000-0000-000000000015', 'b0000000-0000-0000-0000-000000000006', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000003', 'read',  NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    -- ApprovalOnly
    ('e2000000-0000-0000-0000-000000000016', 'b0000000-0000-0000-0000-000000000007', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', 'read',  NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    ('e2000000-0000-0000-0000-000000000017', 'b0000000-0000-0000-0000-000000000007', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000002', 'read',  NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    ('e2000000-0000-0000-0000-000000000018', 'b0000000-0000-0000-0000-000000000007', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000003', 'read',  NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL)
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 11. Configuração SMTP de desenvolvimento (ntf_smtp_configurations)
--     Aponta para MailHog (localhost:1025) — para testes de notificações por email.
--     EncryptedPassword: vazio em dev (MailHog não requer autenticação).
--     BaseUrl: URL base da plataforma para construção de links nos templates.
-- ---------------------------------------------------------------------------
INSERT INTO ntf_smtp_configurations (
    "Id",
    "TenantId",
    "Host",
    "Port",
    "UseSsl",
    "Username",
    "EncryptedPassword",
    "FromAddress",
    "FromName",
    "BaseUrl",
    "IsEnabled",
    "CreatedAt",
    "UpdatedAt"
)
VALUES
    (
        'd1000000-0000-0000-0000-000000000001',
        'a0000000-0000-0000-0000-000000000001',
        'localhost',
        1025,
        false,
        '',
        '',
        'dev@nextraceone.io',
        'NexTraceOne Dev',
        'http://localhost:5173',
        true,
        NOW(), NOW()
    )
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 12. Rulesets de governança para testes (chg_rulesets)
--     RulesetType: 0=ApiContract | 1=ChangeGovernance | 2=SecurityPolicy
--     Content: JSON com array de regras para validação.
-- ---------------------------------------------------------------------------
INSERT INTO chg_rulesets (
    "Id",
    "Name",
    "Description",
    "Content",
    "RulesetType",
    "IsActive",
    "RulesetCreatedAt",
    "CreatedAt",
    "CreatedBy",
    "UpdatedAt",
    "UpdatedBy",
    "IsDeleted"
)
VALUES
    (
        'd2000000-0000-0000-0000-000000000001',
        'API Contract Compliance',
        'Regras de conformidade para contratos REST e eventos. Valida versionamento semântico, documentação de endpoints e contacto de equipa.',
        '{"rules":[{"id":"r-api-001","name":"OpenAPI spec present","description":"Contract must have valid OpenAPI 3.x spec","severity":"error","type":"structural"},{"id":"r-api-002","name":"Semver version","description":"Version field must follow semver x.y.z format","severity":"error","type":"format"},{"id":"r-api-003","name":"Team contact filled","description":"info.contact must include name and email fields","severity":"warning","type":"metadata"},{"id":"r-api-004","name":"Endpoint descriptions","description":"All published endpoints must have non-empty descriptions","severity":"warning","type":"documentation"},{"id":"r-api-005","name":"Response schemas documented","description":"All 2xx responses must have schema definitions","severity":"info","type":"documentation"}]}',
        0,
        true,
        NOW(), NOW(), 'seed', NOW(), 'seed', false
    ),
    (
        'd2000000-0000-0000-0000-000000000002',
        'Breaking Change Governance',
        'Validações obrigatórias para releases breaking. Garante que work item, blast radius e aprovação existem antes do deploy em produção.',
        '{"rules":[{"id":"r-chg-001","name":"Work item required for breaking","description":"Breaking releases must reference a valid work item","severity":"error","type":"governance"},{"id":"r-chg-002","name":"Blast radius required","description":"Breaking releases must have blast radius calculated before production deploy","severity":"error","type":"risk"},{"id":"r-chg-003","name":"TechLead approval required","description":"Production deploys require TechLead or higher approval","severity":"error","type":"approval"},{"id":"r-chg-004","name":"No deploy during freeze","description":"Deployments are blocked during active freeze windows","severity":"error","type":"policy"},{"id":"r-chg-005","name":"Evidence pack for major","description":"Major or breaking changes must have an evidence pack attached","severity":"warning","type":"evidence"}]}',
        1,
        true,
        NOW(), NOW(), 'seed', NOW(), 'seed', false
    )
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 13. Vínculos de rulesets a assets (chg_ruleset_bindings)
--     AssetType: 'RestApi' | 'GrpcService' | 'KafkaProducer' | 'BackgroundService' | 'Any'
--     Cada binding aplica o ruleset a todos os assets do tipo especificado.
-- ---------------------------------------------------------------------------
INSERT INTO chg_ruleset_bindings (
    "Id",
    "RulesetId",
    "AssetType",
    "BindingCreatedAt",
    "CreatedAt",
    "CreatedBy",
    "UpdatedAt",
    "UpdatedBy",
    "IsDeleted"
)
VALUES
    ('d3000000-0000-0000-0000-000000000001', 'd2000000-0000-0000-0000-000000000001', 'RestApi',           NOW(), NOW(), 'seed', NOW(), 'seed', false),
    ('d3000000-0000-0000-0000-000000000002', 'd2000000-0000-0000-0000-000000000002', 'RestApi',           NOW(), NOW(), 'seed', NOW(), 'seed', false),
    ('d3000000-0000-0000-0000-000000000003', 'd2000000-0000-0000-0000-000000000001', 'BackgroundService', NOW(), NOW(), 'seed', NOW(), 'seed', false)
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 14. Provider de IA local para desenvolvimento (aik_providers)
--     Aponta para Ollama rodando localmente (http://localhost:11434).
--     AuthenticationMode: 'None' — Ollama local não requer API key.
--     HealthStatus: 'Unknown' — avaliado em runtime.
-- ---------------------------------------------------------------------------
INSERT INTO aik_providers (
    "Id",
    "Name",
    "Slug",
    "DisplayName",
    "ProviderType",
    "BaseUrl",
    "IsLocal",
    "IsExternal",
    "IsEnabled",
    "AuthenticationMode",
    "SupportedCapabilities",
    "SupportsChat",
    "SupportsEmbeddings",
    "SupportsTools",
    "SupportsVision",
    "SupportsStructuredOutput",
    "HealthStatus",
    "Priority",
    "TimeoutSeconds",
    "Description",
    "RegisteredAt",
    "CreatedAt",
    "CreatedBy",
    "UpdatedAt",
    "UpdatedBy",
    "IsDeleted"
)
VALUES
    (
        'd4000000-0000-0000-0000-000000000001',
        'Ollama Local',
        'ollama-local',
        'Ollama (Local Dev)',
        'Ollama',
        'http://localhost:11434',
        true, false, true,
        'None',
        'chat,text-generation,embeddings',
        true, true, false, false, false,
        'Unknown',
        1, 60,
        'Provider local Ollama para ambiente de desenvolvimento. Executa modelos LLM localmente sem API key. Requer Ollama instalado e activo.',
        NOW(), NOW(), 'seed', NOW(), 'seed', false
    )
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 15. Modelos de IA para desenvolvimento (aik_models)
--     ProviderId FK para aik_providers.
--     Status: 'Registered' | 'Available' | 'Unavailable' | 'Deprecated'
--     SensitivityLevel: 0=Public | 1=Internal | 2=Confidential | 3=Restricted
-- ---------------------------------------------------------------------------
INSERT INTO aik_models (
    "Id",
    "Name",
    "Slug",
    "DisplayName",
    "Provider",
    "ProviderId",
    "ExternalModelId",
    "ModelType",
    "Category",
    "IsInternal",
    "IsExternal",
    "IsInstalled",
    "Status",
    "Capabilities",
    "DefaultUseCases",
    "SensitivityLevel",
    "IsDefaultForChat",
    "IsDefaultForReasoning",
    "IsDefaultForEmbeddings",
    "SupportsStreaming",
    "SupportsToolCalling",
    "SupportsEmbeddings",
    "SupportsVision",
    "SupportsStructuredOutput",
    "ContextWindow",
    "RequiresGpu",
    "RecommendedRamGb",
    "LicenseName",
    "LicenseUrl",
    "ComplianceStatus",
    "RegisteredAt",
    "CreatedAt",
    "CreatedBy",
    "UpdatedAt",
    "UpdatedBy",
    "IsDeleted"
)
VALUES
    (
        'd5000000-0000-0000-0000-000000000001',
        'Llama 3.2 3B',
        'llama3.2-3b',
        'Llama 3.2 3B (Local Dev)',
        'Ollama',
        'd4000000-0000-0000-0000-000000000001',
        'llama3.2:3b',
        'Chat',
        'General',
        true, false, false,
        'Active',
        'chat,text-generation,analysis,summarization',
        'analysis,summarization,documentation,contract-review',
        0,
        true, false, false,
        true, false, false, false, false,
        128000,
        false,
        4.0,
        'Meta Llama Community License',
        'https://llama.meta.com/llama3/license/',
        'Approved',
        NOW(), NOW(), 'seed', NOW(), 'seed', false
    ),
    (
        'd5000000-0000-0000-0000-000000000002',
        'Nomic Embed Text',
        'nomic-embed-text',
        'Nomic Embed Text (Local Dev)',
        'Ollama',
        'd4000000-0000-0000-0000-000000000001',
        'nomic-embed-text:latest',
        'Embedding',
        'Embeddings',
        true, false, false,
        'Active',
        'embeddings,semantic-search',
        'knowledge-search,similarity,rag',
        0,
        false, false, true,
        false, false, true, false, false,
        8192,
        false,
        2.0,
        'Apache 2.0',
        'https://huggingface.co/nomic-ai/nomic-embed-text-v1',
        'Approved',
        NOW(), NOW(), 'seed', NOW(), 'seed', false
    )
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 16. Políticas de acesso a IA (aik_access_policies)
--     Scope: 'Global' | 'Role' | 'Team' | 'User' | 'Tenant'
--     ScopeValue: identificador do scope (nome do papel, equipa, etc.)
--     InternalOnly=true: proíbe uso de modelos externos (API keys externas).
--     AllowedModelIds: vazio = sem restrição de modelo (dentro do escopo).
-- ---------------------------------------------------------------------------
INSERT INTO aik_access_policies (
    "Id",
    "Name",
    "Description",
    "Scope",
    "ScopeValue",
    "AllowedModelIds",
    "BlockedModelIds",
    "AllowExternalAI",
    "InternalOnly",
    "MaxTokensPerRequest",
    "EnvironmentRestrictions",
    "IsActive",
    "CreatedAt",
    "CreatedBy",
    "UpdatedAt",
    "UpdatedBy",
    "IsDeleted"
)
VALUES
    (
        'd6000000-0000-0000-0000-000000000001',
        'Global Default Policy',
        'Política padrão global aplicada a todos os utilizadores. Apenas IA interna permitida. Sem envio de dados sensíveis para modelos externos.',
        'Global', '',
        '', '',
        false, true,
        8192, '',
        true, NOW(), 'seed', NOW(), 'seed', false
    ),
    (
        'd6000000-0000-0000-0000-000000000002',
        'Developer AI Policy',
        'Política para papel Developer — acesso expandido a geração de contratos, análise de código e cenários de teste.',
        'Role', 'Developer',
        'd5000000-0000-0000-0000-000000000001', '',
        false, true,
        16384, '',
        true, NOW(), 'seed', NOW(), 'seed', false
    ),
    (
        'd6000000-0000-0000-0000-000000000003',
        'TechLead AI Policy',
        'Política para papel TechLead — acesso pleno a análise operacional, contratos, mudanças e investigação de incidentes.',
        'Role', 'TechLead',
        '', '',
        false, true,
        32768, '',
        true, NOW(), 'seed', NOW(), 'seed', false
    ),
    (
        'd6000000-0000-0000-0000-000000000004',
        'Viewer Restricted Policy',
        'Política restrita para papéis de leitura (Viewer, Auditor, ApprovalOnly). Apenas consultas básicas, sem geração de código ou contratos.',
        'Role', 'Viewer',
        'd5000000-0000-0000-0000-000000000001', '',
        false, true,
        4096, '',
        true, NOW(), 'seed', NOW(), 'seed', false
    )
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 17. Quotas de tokens de IA (aik_token_quota_policies)
--     Scope: 'Global' | 'Role' | 'Team' | 'User'
--     MaxTokensAccumulated=0: sem limite acumulado.
--     IsHardLimit=false: aviso ao atingir limite, não bloqueio imediato.
--     ProviderId e ModelId: identificadores do provider/modelo (string externa).
-- ---------------------------------------------------------------------------
INSERT INTO aik_token_quota_policies (
    "Id",
    "Name",
    "Description",
    "Scope",
    "ScopeValue",
    "ProviderId",
    "ModelId",
    "MaxInputTokensPerRequest",
    "MaxOutputTokensPerRequest",
    "MaxTotalTokensPerRequest",
    "MaxTokensPerDay",
    "MaxTokensPerMonth",
    "MaxTokensAccumulated",
    "IsHardLimit",
    "AllowSensitiveData",
    "AllowKnowledgePromotion",
    "IsEnabled",
    "CreatedAt",
    "CreatedBy",
    "UpdatedAt",
    "UpdatedBy",
    "IsDeleted"
)
VALUES
    (
        'd7000000-0000-0000-0000-000000000001',
        'Default Token Quota',
        'Quota padrão para todos os utilizadores — limites moderados adequados para uso geral de assistência.',
        'Global', '',
        'ollama-local', '',
        8192, 4096, 12288,
        100000, 2000000, 0,
        false, false, true, true,
        NOW(), 'seed', NOW(), 'seed', false
    ),
    (
        'd7000000-0000-0000-0000-000000000002',
        'Developer Token Quota',
        'Quota expandida para papel Developer — análise extensa, geração de contratos e cenários de teste.',
        'Role', 'Developer',
        'ollama-local', '',
        16384, 8192, 24576,
        500000, 10000000, 0,
        false, false, true, true,
        NOW(), 'seed', NOW(), 'seed', false
    ),
    (
        'd7000000-0000-0000-0000-000000000003',
        'TechLead Token Quota',
        'Quota premium para papel TechLead — análise operacional completa, investigação de incidentes e gestão de mudanças.',
        'Role', 'TechLead',
        'ollama-local', '',
        32768, 16384, 49152,
        1000000, 20000000, 0,
        false, false, true, true,
        NOW(), 'seed', NOW(), 'seed', false
    )
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 18. Políticas de capacidades de IA para IDE (aik_ide_capability_policies)
--     ClientType: 'VisualStudio' | 'VSCode' | 'JetBrains' | 'Any'
--     Persona: papel do utilizador no contexto IDE.
--     AllowedCommands: comandos permitidos (CSV).
--     AllowedContextScopes: escopos de contexto permitidos (CSV).
-- ---------------------------------------------------------------------------
INSERT INTO aik_ide_capability_policies (
    "Id",
    "ClientType",
    "Persona",
    "AllowedCommands",
    "AllowedContextScopes",
    "AllowedModelIds",
    "AllowContractGeneration",
    "AllowIncidentTroubleshooting",
    "AllowExternalAI",
    "MaxTokensPerRequest",
    "IsActive",
    "CreatedAt",
    "CreatedBy",
    "UpdatedAt",
    "UpdatedBy",
    "IsDeleted"
)
VALUES
    (
        'd8000000-0000-0000-0000-000000000001',
        'VisualStudio', 'Developer',
        'explain,generate,review,contract-gen,test-gen,refactor',
        'file,selection,project,contract',
        '',
        true, false, false, 8192, true,
        NOW(), 'seed', NOW(), 'seed', false
    ),
    (
        'd8000000-0000-0000-0000-000000000002',
        'VSCode', 'Developer',
        'explain,generate,review,contract-gen,test-gen,refactor',
        'file,selection,project,contract',
        '',
        true, false, false, 8192, true,
        NOW(), 'seed', NOW(), 'seed', false
    ),
    (
        'd8000000-0000-0000-0000-000000000003',
        'VSCode', 'TechLead',
        'explain,generate,review,contract-gen,test-gen,refactor,incident-analysis,change-impact,blast-radius',
        'file,selection,project,contract,incident,change,service',
        '',
        true, true, false, 16384, true,
        NOW(), 'seed', NOW(), 'seed', false
    ),
    (
        'd8000000-0000-0000-0000-000000000004',
        'VisualStudio', 'TechLead',
        'explain,generate,review,contract-gen,test-gen,refactor,incident-analysis,change-impact,blast-radius',
        'file,selection,project,contract,incident,change,service',
        '',
        true, true, false, 16384, true,
        NOW(), 'seed', NOW(), 'seed', false
    )
ON CONFLICT DO NOTHING;

COMMIT;

-- =============================================================================
-- Resumo do que foi inserido:
--   iam_users                   : 6   (techlead, dev, viewer, auditor, secreview, approval)
--   iam_tenant_memberships      : 6   (um por utilizador de teste)
--   gov_teams                   : 3   (platform-engineering, payments-checkout, identity-security)
--   gov_domains                 : 3   (platform, payments, security)
--   gov_team_domain_links       : 3   (um por equipa)
--   gov_packs                   : 2   (api-contract-standards, change-management-policy)
--   gov_pack_versions           : 2   (v1.0.0 de cada pack)
--   cat_service_assets          : 4   (platform-api, payment-gateway, identity-service,
--                                       notification-worker)
--   chg_releases                : 4   (exemplos em diferentes estados e ambientes)
--   env_environment_accesses    : 18  (6 utilizadores × 3 ambientes, níveis por papel)
--   ntf_smtp_configurations     : 1   (dev SMTP — MailHog localhost:1025)
--   chg_rulesets                : 2   (API Contract Compliance, Breaking Change Governance)
--   chg_ruleset_bindings        : 3   (RestApi×2 + BackgroundService×1)
--   aik_providers               : 1   (Ollama Local)
--   aik_models                  : 2   (Llama 3.2 3B, Nomic Embed Text)
--   aik_access_policies         : 4   (Global, Developer, TechLead, Viewer)
--   aik_token_quota_policies    : 3   (Default, Developer, TechLead)
--   aik_ide_capability_policies : 4   (VS+VSCode × Developer+TechLead)
--
-- Nota: cfg_modules, chg_deployment_environments, chg_workflow_templates,
--       chg_sla_policies, chg_promotion_gates, ntf_channel_configurations
--       e ntf_templates são inseridos por seed_production.sql (dados de referência
--       comuns a todos os ambientes).
-- =============================================================================
