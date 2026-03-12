-- ============================================================================
-- NexTraceOne — Identity & Access — Ambientes de teste
-- Cria ambientes (Development, Pre-Production, Production) por tenant.
-- ============================================================================

-- Ambientes do ACME Corp
INSERT INTO "Environments" ("Id", "TenantId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAt")
VALUES
    ('e1000000-0000-0000-0000-000000000001', 'a1000000-0000-0000-0000-000000000001', 'Development',    'dev',      0, true, '2025-01-01T00:00:00Z'),
    ('e1000000-0000-0000-0000-000000000002', 'a1000000-0000-0000-0000-000000000001', 'Pre-Production',  'pre-prod', 1, true, '2025-01-01T00:00:00Z'),
    ('e1000000-0000-0000-0000-000000000003', 'a1000000-0000-0000-0000-000000000001', 'Production',      'production',2, true, '2025-01-01T00:00:00Z')
ON CONFLICT ("Id") DO NOTHING;

-- Ambientes do Globex Inc
INSERT INTO "Environments" ("Id", "TenantId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAt")
VALUES
    ('e2000000-0000-0000-0000-000000000001', 'a2000000-0000-0000-0000-000000000002', 'Development',    'dev',      0, true, '2025-01-15T00:00:00Z'),
    ('e2000000-0000-0000-0000-000000000002', 'a2000000-0000-0000-0000-000000000002', 'Pre-Production',  'pre-prod', 1, true, '2025-01-15T00:00:00Z'),
    ('e2000000-0000-0000-0000-000000000003', 'a2000000-0000-0000-0000-000000000002', 'Production',      'production',2, true, '2025-01-15T00:00:00Z')
ON CONFLICT ("Id") DO NOTHING;
