-- ═══════════════════════════════════════════════════════════════════════════════
-- NEXTRACEONE — Seed data: Identity & Environment Module (IdentityDatabase)
-- Tabelas: iam_tenants, iam_users, iam_tenant_memberships,
--          env_environments, env_environment_accesses, iam_security_events
--
-- Combinação de seed_production.sql + seed_development.sql do diretório db/seed/
-- Replicado aqui para o mecanismo de bootstrap automático em Development.
--
-- Credenciais:
--   admin@nextraceone.io     → Admin@2026!     (PlatformAdmin)
--   techlead@nextraceone.io  → TechLead@2026!  (TechLead)
--   dev@nextraceone.io       → Dev@2026!       (Developer)
--   viewer@nextraceone.io    → Viewer@2026!    (Viewer)
--   auditor@nextraceone.io   → Auditor@2026!   (Auditor)
--   secreview@nextraceone.io → SecReview@2026! (SecurityReview)
--   approval@nextraceone.io  → Approval@2026!  (ApprovalOnly)
--
-- Pré-requisitos: migrations EF Core aplicadas (iam_roles seeded by HasData).
-- Idempotente: ON CONFLICT DO NOTHING.
-- ═══════════════════════════════════════════════════════════════════════════════

-- ═══ TENANT ══════════════════════════════════════════════════════════════════

INSERT INTO iam_tenants (
    "Id", "Name", "Slug", "IsActive", "CreatedAt", "UpdatedAt"
)
VALUES (
    'a0000000-0000-0000-0000-000000000001',
    'NexTraceOne',
    'nextraceone',
    true,
    NOW(),
    NOW()
)
ON CONFLICT DO NOTHING;

-- ═══ USERS ═══════════════════════════════════════════════════════════════════
-- PBKDF2/SHA256, 100 000 iterações — formato v1.{base64_salt}.{base64_hash}

INSERT INTO iam_users (
    "Id", "Email", first_name, last_name, "PasswordHash",
    "IsActive", "FailedLoginAttempts", "MfaEnabled", "FederationProvider", "ExternalId"
)
VALUES
    (
        'b0000000-0000-0000-0000-000000000001',
        'admin@nextraceone.io',
        'Platform', 'Admin',
        'v1.ppSv9+z4OYt5/ckM0rsEvQ==.VbpxYtiSfi6e4K22pofst54ZLYgPB3GyBPI7Tj1DIUk=',
        true, 0, false, NULL, NULL
    ),
    (
        'b0000000-0000-0000-0000-000000000002',
        'techlead@nextraceone.io',
        'Ana', 'Silva',
        'v1.0b46DSzRMt94/4yLnPUYaw==.KAWR5yo5D3blTBYQjhs7ujRDYV553nzSPdkNzLRpmZw=',
        true, 0, false, NULL, NULL
    ),
    (
        'b0000000-0000-0000-0000-000000000003',
        'dev@nextraceone.io',
        'Bruno', 'Costa',
        'v1.cBmEmnRiLd/BIAOmoOH7BA==.XLmgYFh4M5sIOoWlHkk9Larp4e9nUCLo4WvdtuL4XGY=',
        true, 0, false, NULL, NULL
    ),
    (
        'b0000000-0000-0000-0000-000000000004',
        'viewer@nextraceone.io',
        'Carla', 'Mendes',
        'v1.gGI8arVvEQhYACsvoDZ2yQ==.c+nPZBrQ2GTf+AndKRakJ4hrzSj4iZB93wY3z9BvFX8=',
        true, 0, false, NULL, NULL
    ),
    (
        'b0000000-0000-0000-0000-000000000005',
        'auditor@nextraceone.io',
        'Daniel', 'Ferreira',
        'v1.cidbqZdl+wrJiuUprvaOJA==.dCNhQiRcqWCEzX4EY1yfqPI/RgcGbv4E2SketMZyDME=',
        true, 0, false, NULL, NULL
    ),
    (
        'b0000000-0000-0000-0000-000000000006',
        'secreview@nextraceone.io',
        'Elena', 'Rocha',
        'v1.GaHQSxY9K0ogtqHzKvp7Tg==.ePf5tCuwHGaRGNS/F4i8vtVZXQlVSvEjfK1TPs4eRlg=',
        true, 0, false, NULL, NULL
    ),
    (
        'b0000000-0000-0000-0000-000000000007',
        'approval@nextraceone.io',
        'Fabio', 'Lima',
        'v1.fy4scsVL1c0gE/b6V0QxaQ==.OzvaWSHf9DUC+kroOllidnHAWPgilsa80CqHwvvkedM=',
        true, 0, false, NULL, NULL
    )
ON CONFLICT DO NOTHING;

-- ═══ TENANT MEMBERSHIPS ══════════════════════════════════════════════════════
-- Role GUIDs seeded by EF HasData in RoleConfiguration:
--   PlatformAdmin  : 1e91a557-fade-46df-b248-0f5f5899c001
--   TechLead       : 1e91a557-fade-46df-b248-0f5f5899c002
--   Developer      : 1e91a557-fade-46df-b248-0f5f5899c003
--   Viewer         : 1e91a557-fade-46df-b248-0f5f5899c004
--   Auditor        : 1e91a557-fade-46df-b248-0f5f5899c005
--   SecurityReview : 1e91a557-fade-46df-b248-0f5f5899c006
--   ApprovalOnly   : 1e91a557-fade-46df-b248-0f5f5899c007

INSERT INTO iam_tenant_memberships (
    "Id", "UserId", "TenantId", "RoleId", "JoinedAt", "IsActive"
)
VALUES
    (
        'd0000000-0000-0000-0000-000000000001',
        'b0000000-0000-0000-0000-000000000001',
        'a0000000-0000-0000-0000-000000000001',
        '1e91a557-fade-46df-b248-0f5f5899c001', -- PlatformAdmin
        NOW(), true
    ),
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

-- ═══ ENVIRONMENTS ════════════════════════════════════════════════════════════
-- Profile  : 1=Development | 2=Validation | 3=Staging | 4=Production
-- Criticality: 1=Low | 2=Medium | 3=High | 4=Critical

INSERT INTO env_environments (
    "Id", "TenantId", "Name", "Slug", "SortOrder", "IsActive",
    "Profile", "Code", "Description", "Criticality", "Region",
    "IsProductionLike", "IsPrimaryProduction", "CreatedAt", "CreatedBy", "IsDeleted"
)
VALUES
    (
        'c0000000-0000-0000-0000-000000000001',
        'a0000000-0000-0000-0000-000000000001',
        'Development', 'development', 1, true,
        1, 'DEV',
        'Ambiente de desenvolvimento. Acesso livre para engineers, sem impacto externo.',
        1, NULL, false, false, NOW(), 'seed', false
    ),
    (
        'c0000000-0000-0000-0000-000000000002',
        'a0000000-0000-0000-0000-000000000001',
        'Staging', 'staging', 2, true,
        3, 'STG',
        'Ambiente de homologação. Comportamento próximo de produção com dados sintéticos ou anonimizados.',
        3, NULL, true, false, NOW(), 'seed', false
    ),
    (
        'c0000000-0000-0000-0000-000000000003',
        'a0000000-0000-0000-0000-000000000001',
        'Production', 'production', 3, true,
        4, 'PROD',
        'Ambiente de produção. Máxima restrição, auditoria completa, acesso controlado.',
        4, NULL, true, true, NOW(), 'seed', false
    )
ON CONFLICT DO NOTHING;

-- ═══ ENVIRONMENT ACCESSES ════════════════════════════════════════════════════
-- PlatformAdmin: admin em todos os ambientes
-- TechLead: write em Development/Staging, read em Production
-- Developer: write em Development, read em Staging, none em Production

INSERT INTO env_environment_accesses (
    "Id", "UserId", "TenantId", "EnvironmentId", "AccessLevel",
    "GrantedAt", "ExpiresAt", "GrantedBy", "IsActive", "RevokedAt"
)
VALUES
    -- admin — todos os ambientes
    ('e1000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', 'admin', NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    ('e1000000-0000-0000-0000-000000000002', 'b0000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000002', 'admin', NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    ('e1000000-0000-0000-0000-000000000003', 'b0000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000003', 'admin', NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    -- techlead — write em dev/stg, read em prod
    ('e2000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', 'write', NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    ('e2000000-0000-0000-0000-000000000002', 'b0000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000002', 'write', NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    ('e2000000-0000-0000-0000-000000000003', 'b0000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000003', 'read', NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    -- developer — write em dev, read em stg
    ('e3000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000003', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', 'write', NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    ('e3000000-0000-0000-0000-000000000002', 'b0000000-0000-0000-0000-000000000003', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000002', 'read', NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    -- auditor — read em todos os ambientes
    ('e4000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000005', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', 'read', NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    ('e4000000-0000-0000-0000-000000000002', 'b0000000-0000-0000-0000-000000000005', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000002', 'read', NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL),
    ('e4000000-0000-0000-0000-000000000003', 'b0000000-0000-0000-0000-000000000005', 'a0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000003', 'read', NOW(), NULL, 'b0000000-0000-0000-0000-000000000001', true, NULL)
ON CONFLICT DO NOTHING;

-- ═══ SECURITY EVENTS ═════════════════════════════════════════════════════════

INSERT INTO iam_security_events (
    "Id", "TenantId", "UserId", "SessionId", "EventType", "Description",
    "RiskScore", "IpAddress", "UserAgent", "MetadataJson", "OccurredAt",
    "IsReviewed", "ReviewedAt", "ReviewedBy"
)
VALUES
    (
        'fb000000-0000-0000-0000-000000000001',
        'a0000000-0000-0000-0000-000000000001',
        'b0000000-0000-0000-0000-000000000001',
        NULL, 'LoginSuccess',
        'Successful local login from known IP',
        5, '192.168.1.100', 'Mozilla/5.0 Chrome/125.0',
        '{"location":"São Paulo","method":"Local"}',
        '2025-06-01T10:00:00Z', true, '2025-06-01T12:00:00Z',
        'b0000000-0000-0000-0000-000000000001'
    ),
    (
        'fb000000-0000-0000-0000-000000000002',
        'a0000000-0000-0000-0000-000000000001',
        'b0000000-0000-0000-0000-000000000004',
        NULL, 'LoginFailed',
        'Failed login attempt — incorrect password',
        35, '10.0.0.55', 'Mozilla/5.0 Firefox/127.0',
        '{"attempts":1,"location":"Rio de Janeiro"}',
        '2025-05-30T10:18:00Z', false, NULL, NULL
    ),
    (
        'fb000000-0000-0000-0000-000000000003',
        'a0000000-0000-0000-0000-000000000001',
        'b0000000-0000-0000-0000-000000000003',
        NULL, 'UnusualLocation',
        'Login from new geographic location detected',
        60, '203.45.67.89', 'Mozilla/5.0 Safari/17.5',
        '{"location":"Tokyo, Japan","previousLocations":["São Paulo","Curitiba"]}',
        '2025-06-01T03:15:00Z', false, NULL, NULL
    ),
    (
        'fb000000-0000-0000-0000-000000000004',
        'a0000000-0000-0000-0000-000000000001',
        NULL, NULL, 'BruteForceAttempt',
        'Multiple failed login attempts from single IP in 5 minutes',
        80, '185.220.101.34', 'python-requests/2.31.0',
        '{"targetEmails":["admin@nextraceone.io"],"attempts":15,"windowMinutes":5}',
        '2025-05-25T14:22:00Z', true, '2025-05-25T14:30:00Z',
        'b0000000-0000-0000-0000-000000000001'
    )
ON CONFLICT DO NOTHING;
