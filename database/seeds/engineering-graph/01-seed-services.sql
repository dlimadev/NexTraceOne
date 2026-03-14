-- ============================================================================
-- NexTraceOne — Engineering Graph — Serviços de teste
-- Cria 8 serviços distribuídos em 3 domínios de negócio para simular um
-- ecossistema bancário/enterprise realista com equipas distintas.
-- IDs determinísticos para permitir referências cruzadas entre scripts.
-- ============================================================================
-- Domínios:
--   Payments  → payment-gateway, payment-processor, payment-reconciliation
--   Identity  → auth-service, user-management
--   Orders    → order-orchestrator, catalog-service, notification-service
-- ============================================================================

INSERT INTO eg_service_assets ("Id", "Name", "Domain", "TeamName")
VALUES
    -- ── Domínio Payments ────────────────────────────────────────────────────
    ('e1000000-0000-0000-0000-000000000001', 'payment-gateway',        'Payments', 'Pagamentos Squad'),
    ('e1000000-0000-0000-0000-000000000002', 'payment-processor',      'Payments', 'Pagamentos Squad'),
    ('e1000000-0000-0000-0000-000000000003', 'payment-reconciliation', 'Payments', 'Reconciliação Squad'),

    -- ── Domínio Identity ────────────────────────────────────────────────────
    ('e1000000-0000-0000-0000-000000000004', 'auth-service',           'Identity', 'Identity Team'),
    ('e1000000-0000-0000-0000-000000000005', 'user-management',        'Identity', 'Identity Team'),

    -- ── Domínio Orders ──────────────────────────────────────────────────────
    ('e1000000-0000-0000-0000-000000000006', 'order-orchestrator',     'Orders',   'Orders Team'),
    ('e1000000-0000-0000-0000-000000000007', 'catalog-service',        'Orders',   'Orders Team'),
    ('e1000000-0000-0000-0000-000000000008', 'notification-service',   'Orders',   'Notificações Squad')
ON CONFLICT ("Id") DO NOTHING;
