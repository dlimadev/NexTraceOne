-- ============================================================================
-- NexTraceOne — Developer Portal — Subscrições de notificação de teste
-- Cria 8 subscrições distribuídas por APIs, utilizadores e níveis distintos.
-- Referencia IDs de utilizadores do IdentityAccess (u1) e APIs do
-- EngineeringGraph (e2) para simular cenários cross-module realistas.
-- ============================================================================
-- Níveis (SubscriptionLevel):
--   0 = BreakingChangesOnly, 1 = AllChanges,
--   2 = DeprecationNotices,  3 = SecurityAdvisories
-- Canais (NotificationChannel):
--   0 = Email, 1 = Webhook
-- ============================================================================

INSERT INTO dp_subscriptions (
    "Id", "ApiAssetId", "ApiName", "SubscriberId", "SubscriberEmail",
    "ConsumerServiceName", "ConsumerServiceVersion",
    "Level", "Channel", "WebhookUrl", "IsActive", "CreatedAt", "LastNotifiedAt"
)
VALUES
    -- ── Subscrição 1: Developer subscreve Payments API — apenas breaking changes por e-mail
    (
        'd1000000-0000-0000-0000-000000000001',
        'e2000000-0000-0000-0000-000000000001',
        'Payments API',
        'u1000000-0000-0000-0000-000000000003',
        'dev@acme-corp.test',
        'mobile-app',
        '2.4.1',
        0, -- BreakingChangesOnly
        0, -- Email
        NULL,
        true,
        '2025-01-15T10:30:00Z',
        '2025-03-01T14:22:00Z'
    ),

    -- ── Subscrição 2: TechLead subscreve Payments API — todas as mudanças por webhook
    (
        'd1000000-0000-0000-0000-000000000002',
        'e2000000-0000-0000-0000-000000000001',
        'Payments API',
        'u1000000-0000-0000-0000-000000000002',
        'techlead@acme-corp.test',
        'web-portal',
        '3.1.0',
        1, -- AllChanges
        1, -- Webhook
        'https://hooks.acme-corp.test/payments-changes',
        true,
        '2025-01-20T09:00:00Z',
        '2025-03-10T11:45:00Z'
    ),

    -- ── Subscrição 3: Developer subscreve Refunds API — avisos de depreciação por e-mail
    (
        'd1000000-0000-0000-0000-000000000003',
        'e2000000-0000-0000-0000-000000000002',
        'Refunds API',
        'u1000000-0000-0000-0000-000000000003',
        'dev@acme-corp.test',
        'mobile-app',
        '2.4.1',
        2, -- DeprecationNotices
        0, -- Email
        NULL,
        true,
        '2025-02-01T08:15:00Z',
        NULL
    ),

    -- ── Subscrição 4: Utilizador multi-tenant subscreve Processing API — alertas de segurança por webhook
    (
        'd1000000-0000-0000-0000-000000000004',
        'e2000000-0000-0000-0000-000000000003',
        'Processing API',
        'u1000000-0000-0000-0000-000000000007',
        'multi@globex-inc.test',
        'batch-processor',
        '1.0.0',
        3, -- SecurityAdvisories
        1, -- Webhook
        'https://hooks.globex-inc.test/security-alerts',
        true,
        '2025-02-10T14:00:00Z',
        '2025-02-28T16:30:00Z'
    ),

    -- ── Subscrição 5: Admin subscreve Settlements API — todas as mudanças por e-mail
    (
        'd1000000-0000-0000-0000-000000000005',
        'e2000000-0000-0000-0000-000000000004',
        'Settlements API',
        'u1000000-0000-0000-0000-000000000001',
        'admin@acme-corp.test',
        'api-gateway',
        '5.0.2',
        1, -- AllChanges
        0, -- Email
        NULL,
        true,
        '2025-02-15T11:20:00Z',
        '2025-03-05T09:10:00Z'
    ),

    -- ── Subscrição 6: Viewer subscreve Reconciliation API — depreciação por e-mail (INATIVA)
    (
        'd1000000-0000-0000-0000-000000000006',
        'e2000000-0000-0000-0000-000000000005',
        'Reconciliation API',
        'u1000000-0000-0000-0000-000000000004',
        'viewer@acme-corp.test',
        'monitoring-agent',
        '1.2.0',
        2, -- DeprecationNotices
        0, -- Email
        NULL,
        false,
        '2025-01-05T16:45:00Z',
        '2025-01-20T10:00:00Z'
    ),

    -- ── Subscrição 7: DevOnly do Globex subscreve Payments API — breaking por webhook
    (
        'd1000000-0000-0000-0000-000000000007',
        'e2000000-0000-0000-0000-000000000001',
        'Payments API',
        'u1000000-0000-0000-0000-000000000008',
        'devonly@globex-inc.test',
        'external-partner',
        '1.0.0',
        0, -- BreakingChangesOnly
        1, -- Webhook
        'https://hooks.globex-inc.test/breaking-notifier',
        true,
        '2025-03-01T12:00:00Z',
        NULL
    ),

    -- ── Subscrição 8: Security officer subscreve Processing API — alertas de segurança por e-mail
    (
        'd1000000-0000-0000-0000-000000000008',
        'e2000000-0000-0000-0000-000000000003',
        'Processing API',
        'u1000000-0000-0000-0000-000000000005',
        'security@acme-corp.test',
        'web-portal',
        '3.1.0',
        3, -- SecurityAdvisories
        0, -- Email
        NULL,
        true,
        '2025-03-05T08:00:00Z',
        NULL
    )
ON CONFLICT ("Id") DO NOTHING;
