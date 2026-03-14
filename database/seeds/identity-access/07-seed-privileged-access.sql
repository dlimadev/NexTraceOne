-- ============================================================================
-- NexTraceOne — Identity & Access — Acessos Privilegiados
-- Cria dados de teste para os três mecanismos de acesso excepcional:
--   1. Break Glass — acesso de emergência com justificativa e post-mortem
--   2. JIT (Just-In-Time) — acesso temporário com aprovação explícita
--   3. Delegações — transferência temporária de permissões entre utilizadores
-- Cada mecanismo possui cenários em diferentes estados do ciclo de vida.
-- ============================================================================

-- ────────────────────────────────────────────────────────────────────────────
-- 1. BREAK GLASS REQUESTS
-- Cenários: activo, expirado e revogado
-- ────────────────────────────────────────────────────────────────────────────

INSERT INTO "BreakGlassRequests" ("Id", "UserId", "TenantId", "Justification", "Status", "RequestedAt", "ExpiresAt", "RevokedAt", "RevokedBy", "PostMortemNotes")
VALUES
    -- Pedido activo — dev@acme-corp.test precisa de acesso de emergência à produção
    (
        'bg100000-0000-0000-0000-000000000001',
        'u1000000-0000-0000-0000-000000000003',
        'a1000000-0000-0000-0000-000000000001',
        'Critical production incident INC-2025-0042: payment gateway timeout affecting 15% of transactions. Need direct database access to diagnose connection pool exhaustion.',
        'Active',
        '2025-03-10T14:30:00Z',
        '2025-03-10T18:30:00Z',
        NULL, NULL, NULL
    ),
    -- Pedido expirado — acesso de emergência que já caducou naturalmente
    (
        'bg100000-0000-0000-0000-000000000002',
        'u1000000-0000-0000-0000-000000000003',
        'a1000000-0000-0000-0000-000000000001',
        'Urgent hotfix for CVE-2025-1234 affecting authentication module. Required production access to verify patch deployment.',
        'Expired',
        '2025-02-20T09:00:00Z',
        '2025-02-20T13:00:00Z',
        NULL, NULL,
        'Post-mortem: CVE-2025-1234 patched successfully. Root cause was outdated JWT library. Updated dependency and added automated vulnerability scanning to CI pipeline.'
    ),
    -- Pedido revogado — admin revogou o acesso antes da expiração
    (
        'bg100000-0000-0000-0000-000000000003',
        'u1000000-0000-0000-0000-000000000002',
        'a1000000-0000-0000-0000-000000000001',
        'Performance investigation: API latency spike in pre-production environment after deployment v2.4.1.',
        'Revoked',
        '2025-03-05T16:00:00Z',
        '2025-03-05T20:00:00Z',
        '2025-03-05T17:30:00Z',
        'u1000000-0000-0000-0000-000000000001',
        'Post-mortem: Issue identified as misconfigured connection pool size (was 5, should be 50). Reverted config and confirmed latency returned to baseline. No code change needed.'
    )
ON CONFLICT ("Id") DO NOTHING;

-- ────────────────────────────────────────────────────────────────────────────
-- 2. JIT ACCESS REQUESTS
-- Cenários: pendente de aprovação, aprovado e rejeitado
-- ────────────────────────────────────────────────────────────────────────────

INSERT INTO "JitAccessRequests" ("Id", "UserId", "TenantId", "PermissionCode", "Scope", "Justification", "Status", "RequestedAt", "ApprovalDeadline", "ApprovedBy", "RejectionReason", "ExpiresAt")
VALUES
    -- Pedido pendente — developer precisa de acesso temporário ao catálogo para escrita
    (
        'jt100000-0000-0000-0000-000000000001',
        'u1000000-0000-0000-0000-000000000004',
        'a1000000-0000-0000-0000-000000000001',
        'catalog:write',
        'environment:e1000000-0000-0000-0000-000000000001',
        'Need to register new Payment Gateway API v3.0 in the development catalog for sprint 14 integration testing.',
        'Pending',
        '2025-03-10T10:00:00Z',
        '2025-03-10T14:00:00Z',
        NULL, NULL, NULL
    ),
    -- Pedido aprovado — developer recebeu acesso temporário a releases:write
    (
        'jt100000-0000-0000-0000-000000000002',
        'u1000000-0000-0000-0000-000000000003',
        'a1000000-0000-0000-0000-000000000001',
        'releases:write',
        'environment:e1000000-0000-0000-0000-000000000002',
        'Need to create hotfix release for pre-production to fix critical data mapping bug before sprint review.',
        'Approved',
        '2025-03-08T11:00:00Z',
        '2025-03-08T15:00:00Z',
        'u1000000-0000-0000-0000-000000000002',
        NULL,
        '2025-03-09T11:00:00Z'
    ),
    -- Pedido rejeitado — tentativa de acesso a sessões negada por falta de justificativa
    (
        'jt100000-0000-0000-0000-000000000003',
        'u1000000-0000-0000-0000-000000000008',
        'a2000000-0000-0000-0000-000000000002',
        'identity:sessions:revoke',
        'tenant:a2000000-0000-0000-0000-000000000002',
        'Want to revoke sessions for testing purposes.',
        'Rejected',
        '2025-03-06T09:00:00Z',
        '2025-03-06T13:00:00Z',
        NULL,
        'Insufficient justification. Session revocation requires a specific incident reference or security concern. Please resubmit with INC/SEC ticket number.',
        NULL
    )
ON CONFLICT ("Id") DO NOTHING;

-- ────────────────────────────────────────────────────────────────────────────
-- 3. DELEGAÇÕES
-- Cenários: delegação activa e delegação expirada
-- Permissões são armazenadas como array JSON de códigos de permissão
-- ────────────────────────────────────────────────────────────────────────────

INSERT INTO "Delegations" ("Id", "GrantorId", "DelegateeId", "TenantId", "Permissions", "Status", "ValidFrom", "ValidUntil", "CreatedAt", "RevokedAt")
VALUES
    -- Delegação activa — techlead delega aprovação de workflows ao dev durante férias
    (
        'dg100000-0000-0000-0000-000000000001',
        'u1000000-0000-0000-0000-000000000002',
        'u1000000-0000-0000-0000-000000000003',
        'a1000000-0000-0000-0000-000000000001',
        '["workflow:approve", "workflow:read", "releases:read"]',
        'Active',
        '2025-03-10T00:00:00Z',
        '2025-03-17T23:59:59Z',
        '2025-03-09T16:00:00Z',
        NULL
    ),
    -- Delegação expirada — admin delegou gestão de utilizadores temporariamente
    (
        'dg100000-0000-0000-0000-000000000002',
        'u1000000-0000-0000-0000-000000000001',
        'u1000000-0000-0000-0000-000000000002',
        'a1000000-0000-0000-0000-000000000001',
        '["identity:users:write", "identity:roles:assign"]',
        'Expired',
        '2025-02-01T00:00:00Z',
        '2025-02-15T23:59:59Z',
        '2025-01-31T10:00:00Z',
        NULL
    )
ON CONFLICT ("Id") DO NOTHING;
