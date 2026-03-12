-- ============================================================================
-- NexTraceOne — Identity & Access — Reset de massa de teste
-- Limpa dados de teste do módulo Identity em ordem segura de dependências.
-- ATENÇÃO: Usar APENAS em ambientes de desenvolvimento/teste local.
-- ============================================================================

-- Ordem de exclusão respeita foreign keys (dependentes primeiro)
DELETE FROM "SecurityEvents" WHERE "TenantId" IN (
    SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
);

DELETE FROM "AccessReviewItems" WHERE "CampaignId" IN (
    SELECT "Id" FROM "AccessReviewCampaigns" WHERE "TenantId" IN (
        SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
    )
);

DELETE FROM "AccessReviewCampaigns" WHERE "TenantId" IN (
    SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
);

DELETE FROM "EnvironmentAccesses" WHERE "TenantId" IN (
    SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
);

DELETE FROM "Environments" WHERE "TenantId" IN (
    SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
);

DELETE FROM "Delegations" WHERE "TenantId" IN (
    SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
);

DELETE FROM "JitAccessRequests" WHERE "TenantId" IN (
    SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
);

DELETE FROM "BreakGlassRequests" WHERE "TenantId" IN (
    SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
);

DELETE FROM "Sessions" WHERE "UserId" IN (
    SELECT u."Id" FROM "Users" u
    INNER JOIN "TenantMemberships" tm ON tm."UserId" = u."Id"
    WHERE tm."TenantId" IN (
        SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
    )
);

DELETE FROM "ExternalIdentities" WHERE "UserId" IN (
    SELECT u."Id" FROM "Users" u
    INNER JOIN "TenantMemberships" tm ON tm."UserId" = u."Id"
    WHERE tm."TenantId" IN (
        SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
    )
);

DELETE FROM "TenantMemberships" WHERE "TenantId" IN (
    SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
);

DELETE FROM "SsoGroupMappings" WHERE "TenantId" IN (
    SELECT "Id" FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive')
);

-- Limpar usuários de teste (por email)
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

-- Limpar roles de teste (preservar roles do sistema se existirem)
-- Roles do sistema são criadas pelo bootstrap e não devem ser removidas

-- Limpar tenants de teste
DELETE FROM "Tenants" WHERE "Slug" IN ('acme-corp', 'globex-inc', 'initech-inactive');

-- Mensagem de confirmação
DO $$ BEGIN RAISE NOTICE 'Identity & Access test data reset completed.'; END $$;
