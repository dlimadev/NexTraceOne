-- ============================================================================
-- NexTraceOne — Identity & Access — Reset de massa de teste
-- Limpa TODOS os dados de teste do módulo Identity em ordem segura de
-- dependências (foreign keys). Inclui: eventos de segurança, sessões,
-- acessos privilegiados, acessos a ambientes, memberships, roles,
-- permissões, utilizadores, ambientes e tenants.
-- ATENÇÃO: Usar APENAS em ambientes de desenvolvimento/teste local.
-- ============================================================================

-- Ordem de exclusão respeita foreign keys (dependentes primeiro)

-- ── Eventos de segurança (sem dependentes, mas referencia sessões/utilizadores) ──
DELETE FROM "SecurityEvents" WHERE "TenantId" IN (
    SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
);

-- ── Access Reviews ──────────────────────────────────────────────────────────
DELETE FROM "AccessReviewItems" WHERE "CampaignId" IN (
    SELECT "Id" FROM "AccessReviewCampaigns" WHERE "TenantId" IN (
        SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
    )
);

DELETE FROM "AccessReviewCampaigns" WHERE "TenantId" IN (
    SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
);

-- ── Acessos a ambientes ─────────────────────────────────────────────────────
DELETE FROM "EnvironmentAccesses" WHERE "TenantId" IN (
    SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
);

-- ── Ambientes ───────────────────────────────────────────────────────────────
DELETE FROM "Environments" WHERE "TenantId" IN (
    SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
);

-- ── Acessos privilegiados (delegações, JIT, break glass) ────────────────────
DELETE FROM "Delegations" WHERE "TenantId" IN (
    SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
);

DELETE FROM "JitAccessRequests" WHERE "TenantId" IN (
    SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
);

DELETE FROM "BreakGlassRequests" WHERE "TenantId" IN (
    SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
);

-- ── Sessões ─────────────────────────────────────────────────────────────────
DELETE FROM "Sessions" WHERE "UserId" IN (
    SELECT u."Id" FROM "Users" u
    INNER JOIN "TenantMemberships" tm ON tm."UserId" = u."Id"
    WHERE tm."TenantId" IN (
        SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
    )
);

-- ── Identidades externas (OIDC/SAML) ────────────────────────────────────────
DELETE FROM "ExternalIdentities" WHERE "UserId" IN (
    SELECT u."Id" FROM "Users" u
    INNER JOIN "TenantMemberships" tm ON tm."UserId" = u."Id"
    WHERE tm."TenantId" IN (
        SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
    )
);

-- ── Memberships (utilizador ↔ tenant ↔ role) ────────────────────────────────
DELETE FROM "TenantMemberships" WHERE "TenantId" IN (
    SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
);

-- ── SSO Group Mappings ──────────────────────────────────────────────────────
DELETE FROM "SsoGroupMappings" WHERE "TenantId" IN (
    SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
);

-- ── Associações Role ↔ Permissão (limpar antes de roles e permissões) ───────
DELETE FROM "RolePermissions" WHERE "RoleId" IN (
    SELECT "Id" FROM "Roles" WHERE "Id" IN (
        'r1000000-0000-0000-0000-000000000001',
        'r1000000-0000-0000-0000-000000000002',
        'r1000000-0000-0000-0000-000000000003',
        'r1000000-0000-0000-0000-000000000004',
        'r1000000-0000-0000-0000-000000000005',
        'r1000000-0000-0000-0000-000000000006',
        'r1000000-0000-0000-0000-000000000007'
    )
);

-- ── Roles de teste (IDs estáveis do seed, não afecta roles de bootstrap) ────
DELETE FROM "Roles" WHERE "Id" IN (
    'r1000000-0000-0000-0000-000000000001',
    'r1000000-0000-0000-0000-000000000002',
    'r1000000-0000-0000-0000-000000000003',
    'r1000000-0000-0000-0000-000000000004',
    'r1000000-0000-0000-0000-000000000005',
    'r1000000-0000-0000-0000-000000000006',
    'r1000000-0000-0000-0000-000000000007'
);

-- ── Permissões de teste (IDs estáveis do seed) ──────────────────────────────
DELETE FROM "Permissions" WHERE "Id" IN (
    'p1000000-0000-0000-0000-000000000001',
    'p1000000-0000-0000-0000-000000000002',
    'p1000000-0000-0000-0000-000000000003',
    'p1000000-0000-0000-0000-000000000004',
    'p1000000-0000-0000-0000-000000000005',
    'p1000000-0000-0000-0000-000000000006',
    'p1000000-0000-0000-0000-000000000007',
    'p1000000-0000-0000-0000-000000000008',
    'p1000000-0000-0000-0000-000000000009',
    'p1000000-0000-0000-0000-000000000010',
    'p1000000-0000-0000-0000-000000000011',
    'p1000000-0000-0000-0000-000000000012',
    'p1000000-0000-0000-0000-000000000013',
    'p1000000-0000-0000-0000-000000000014',
    'p1000000-0000-0000-0000-000000000015',
    'p1000000-0000-0000-0000-000000000016',
    'p1000000-0000-0000-0000-000000000017'
);

-- ── Utilizadores de teste (por email) ───────────────────────────────────────
DELETE FROM "Users" WHERE "Email" IN (
    'admin@acme-corp.test',
    'techlead@acme-corp.test',
    'dev@acme-corp.test',
    'viewer@acme-corp.test',
    'security@acme-corp.test',
    'approver@acme-corp.test',
    'multi@globex-inc.test',
    'devonly@globex-inc.test',
    'oidc@acme-corp.test',
    'localfallback@acme-corp.test'
);

-- ── Tenants de teste ────────────────────────────────────────────────────────
DELETE FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive');

-- Mensagem de confirmação
DO $$ BEGIN RAISE NOTICE 'Identity & Access test data reset completed (includes roles, permissions, sessions, privileged access).'; END $$;
