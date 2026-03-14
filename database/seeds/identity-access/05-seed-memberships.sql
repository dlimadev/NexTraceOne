-- ============================================================================
-- NexTraceOne — Identity & Access — Memberships (Utilizador ↔ Tenant ↔ Role)
-- Liga cada utilizador de teste ao(s) tenant(s) com o role funcional correto.
-- O utilizador multi@globex-inc.test demonstra cenário multi-tenant real:
-- pertence à ACME Corp como Developer E à Globex Inc como TechLead.
-- ============================================================================

-- ────────────────────────────────────────────────────────────────────────────
-- Referência de IDs (definidos nos scripts anteriores):
--   Tenants:  ACME Corp = a1000000-...-01, Globex = a2000000-...-02
--   Roles:    PlatformAdmin=-01, TechLead=-02, Developer=-03, Viewer=-04,
--             Auditor=-05, SecurityReview=-06, ApprovalOnly=-07
--   Users:    admin=-01, techlead=-02, dev=-03, viewer=-04, security=-05,
--             approver=-06, multi=-07, devonly=-08, oidc=-09, localfallback=-10
-- ────────────────────────────────────────────────────────────────────────────

INSERT INTO "TenantMemberships" ("Id", "UserId", "TenantId", "RoleId", "IsActive", "JoinedAt")
VALUES
    -- admin@acme-corp.test → ACME Corp como PlatformAdmin
    (
        'm1000000-0000-0000-0000-000000000001',
        'u1000000-0000-0000-0000-000000000001',
        'a1000000-0000-0000-0000-000000000001',
        'r1000000-0000-0000-0000-000000000001',
        true,
        '2025-01-01T00:00:00Z'
    ),
    -- techlead@acme-corp.test → ACME Corp como TechLead
    (
        'm1000000-0000-0000-0000-000000000002',
        'u1000000-0000-0000-0000-000000000002',
        'a1000000-0000-0000-0000-000000000001',
        'r1000000-0000-0000-0000-000000000002',
        true,
        '2025-01-02T00:00:00Z'
    ),
    -- dev@acme-corp.test → ACME Corp como Developer
    (
        'm1000000-0000-0000-0000-000000000003',
        'u1000000-0000-0000-0000-000000000003',
        'a1000000-0000-0000-0000-000000000001',
        'r1000000-0000-0000-0000-000000000003',
        true,
        '2025-01-03T00:00:00Z'
    ),
    -- viewer@acme-corp.test → ACME Corp como Viewer
    (
        'm1000000-0000-0000-0000-000000000004',
        'u1000000-0000-0000-0000-000000000004',
        'a1000000-0000-0000-0000-000000000001',
        'r1000000-0000-0000-0000-000000000004',
        true,
        '2025-01-04T00:00:00Z'
    ),
    -- security@acme-corp.test → ACME Corp como SecurityReview
    (
        'm1000000-0000-0000-0000-000000000005',
        'u1000000-0000-0000-0000-000000000005',
        'a1000000-0000-0000-0000-000000000001',
        'r1000000-0000-0000-0000-000000000006',
        true,
        '2025-01-05T00:00:00Z'
    ),
    -- approver@acme-corp.test → ACME Corp como ApprovalOnly
    (
        'm1000000-0000-0000-0000-000000000006',
        'u1000000-0000-0000-0000-000000000006',
        'a1000000-0000-0000-0000-000000000001',
        'r1000000-0000-0000-0000-000000000007',
        true,
        '2025-01-06T00:00:00Z'
    ),
    -- multi@globex-inc.test → ACME Corp como Developer (cenário multi-tenant, membership 1)
    (
        'm1000000-0000-0000-0000-000000000007',
        'u1000000-0000-0000-0000-000000000007',
        'a1000000-0000-0000-0000-000000000001',
        'r1000000-0000-0000-0000-000000000003',
        true,
        '2025-01-10T00:00:00Z'
    ),
    -- multi@globex-inc.test → Globex Inc como TechLead (cenário multi-tenant, membership 2)
    (
        'm1000000-0000-0000-0000-000000000008',
        'u1000000-0000-0000-0000-000000000007',
        'a2000000-0000-0000-0000-000000000002',
        'r1000000-0000-0000-0000-000000000002',
        true,
        '2025-01-10T00:00:00Z'
    ),
    -- devonly@globex-inc.test → Globex Inc como Developer
    (
        'm1000000-0000-0000-0000-000000000009',
        'u1000000-0000-0000-0000-000000000008',
        'a2000000-0000-0000-0000-000000000002',
        'r1000000-0000-0000-0000-000000000003',
        true,
        '2025-01-12T00:00:00Z'
    ),
    -- oidc@acme-corp.test → ACME Corp como Developer (utilizador OIDC sem senha local)
    (
        'm1000000-0000-0000-0000-000000000010',
        'u1000000-0000-0000-0000-000000000009',
        'a1000000-0000-0000-0000-000000000001',
        'r1000000-0000-0000-0000-000000000003',
        true,
        '2025-02-01T00:00:00Z'
    ),
    -- localfallback@acme-corp.test → ACME Corp como Developer (OIDC com fallback local)
    (
        'm1000000-0000-0000-0000-000000000011',
        'u1000000-0000-0000-0000-000000000010',
        'a1000000-0000-0000-0000-000000000001',
        'r1000000-0000-0000-0000-000000000003',
        true,
        '2025-02-15T00:00:00Z'
    )
ON CONFLICT ("Id") DO NOTHING;
