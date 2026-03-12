-- ============================================================================
-- NexTraceOne — Identity & Access — Tenants de teste
-- Cria tenants fictícios para validação funcional e demonstrações.
-- ============================================================================

-- Tenant principal: empresa com múltiplos usuários e cenários completos
INSERT INTO "Tenants" ("Id", "Name", "Slug", "IsActive", "CreatedAt")
VALUES (
    'a1000000-0000-0000-0000-000000000001',
    'ACME Corporation',
    'acme-corp',
    true,
    '2025-01-01T00:00:00Z'
) ON CONFLICT ("Id") DO NOTHING;

-- Tenant secundário: empresa para cenários multi-tenant
INSERT INTO "Tenants" ("Id", "Name", "Slug", "IsActive", "CreatedAt")
VALUES (
    'a2000000-0000-0000-0000-000000000002',
    'Globex Inc',
    'globex-inc',
    true,
    '2025-01-15T00:00:00Z'
) ON CONFLICT ("Id") DO NOTHING;

-- Tenant inativo: para testar bloqueio de acesso a tenants desativados
INSERT INTO "Tenants" ("Id", "Name", "Slug", "IsActive", "CreatedAt")
VALUES (
    'a3000000-0000-0000-0000-000000000003',
    'Initech (Inactive)',
    'initech-inactive',
    false,
    '2025-02-01T00:00:00Z'
) ON CONFLICT ("Id") DO NOTHING;
