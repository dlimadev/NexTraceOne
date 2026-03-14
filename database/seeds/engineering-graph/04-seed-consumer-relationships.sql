-- ============================================================================
-- NexTraceOne — Engineering Graph — Relações de consumo de teste
-- Cria 17 relações de consumo (ConsumerRelationship) mostrando como
-- diferentes consumidores dependem das APIs do ecossistema.
-- Inclui relações Explicit (declaradas) e Inferred (inferidas por telemetria)
-- com scores de confiança variados (0.60–1.00).
-- ============================================================================
-- Cenários cobertos:
--   • Payments API: alta dependência com 4 consumidores directos
--   • Auth API: consumida por frontends e gateway (cross-domain)
--   • Users API: consumida por portal e batch (cenário de sincronização)
--   • Orders API: consumida por frontends e gateway (domínio Orders)
--   • Processing API: dependência interna via gateway
--   • Notifications API: consumida por frontends (notificações push/email)
--   • Products API: consumida pelo portal (catálogo de produtos)
-- ============================================================================

INSERT INTO eg_consumer_relationships (
    "Id", "ConsumerAssetId", "ConsumerName", "SourceType",
    "ConfidenceScore", "FirstObservedAt", "LastObservedAt", "ApiAssetId"
)
VALUES
    -- ── Payments API (e2..001) — 4 consumidores ─────────────────────────────
    -- App móvel consome pagamentos directamente
    (
        'e4000000-0000-0000-0000-000000000001',
        'e3000000-0000-0000-0000-000000000001', 'mobile-app', 'Explicit',
        1.0000, NOW() - INTERVAL '90 days', NOW() - INTERVAL '1 day',
        'e2000000-0000-0000-0000-000000000001'
    ),
    -- Portal web consome pagamentos
    (
        'e4000000-0000-0000-0000-000000000002',
        'e3000000-0000-0000-0000-000000000002', 'web-portal', 'Explicit',
        1.0000, NOW() - INTERVAL '90 days', NOW() - INTERVAL '1 day',
        'e2000000-0000-0000-0000-000000000001'
    ),
    -- Gateway encaminha tráfego para pagamentos
    (
        'e4000000-0000-0000-0000-000000000003',
        'e3000000-0000-0000-0000-000000000003', 'api-gateway', 'Explicit',
        0.9500, NOW() - INTERVAL '60 days', NOW() - INTERVAL '2 days',
        'e2000000-0000-0000-0000-000000000001'
    ),
    -- Parceiro externo inferido por telemetria OpenTelemetry
    (
        'e4000000-0000-0000-0000-000000000004',
        'e3000000-0000-0000-0000-000000000006', 'external-partner', 'Inferred',
        0.8500, NOW() - INTERVAL '30 days', NOW() - INTERVAL '5 days',
        'e2000000-0000-0000-0000-000000000001'
    ),

    -- ── Auth API (e2..006) — 4 consumidores (cross-domain) ─────────────────
    -- App móvel autentica-se via Auth API
    (
        'e4000000-0000-0000-0000-000000000005',
        'e3000000-0000-0000-0000-000000000001', 'mobile-app', 'Explicit',
        1.0000, NOW() - INTERVAL '120 days', NOW() - INTERVAL '1 day',
        'e2000000-0000-0000-0000-000000000006'
    ),
    -- Portal web autentica-se via Auth API
    (
        'e4000000-0000-0000-0000-000000000006',
        'e3000000-0000-0000-0000-000000000002', 'web-portal', 'Explicit',
        1.0000, NOW() - INTERVAL '120 days', NOW() - INTERVAL '1 day',
        'e2000000-0000-0000-0000-000000000006'
    ),
    -- Gateway valida tokens contra Auth API
    (
        'e4000000-0000-0000-0000-000000000007',
        'e3000000-0000-0000-0000-000000000003', 'api-gateway', 'Explicit',
        0.9500, NOW() - INTERVAL '90 days', NOW() - INTERVAL '1 day',
        'e2000000-0000-0000-0000-000000000006'
    ),
    -- Agente de monitorização verifica saúde da Auth API
    (
        'e4000000-0000-0000-0000-000000000008',
        'e3000000-0000-0000-0000-000000000005', 'monitoring-agent', 'Inferred',
        0.7500, NOW() - INTERVAL '14 days', NOW() - INTERVAL '3 days',
        'e2000000-0000-0000-0000-000000000006'
    ),

    -- ── Users API (e2..008) — 2 consumidores ───────────────────────────────
    -- Portal web consulta e gere utilizadores
    (
        'e4000000-0000-0000-0000-000000000009',
        'e3000000-0000-0000-0000-000000000002', 'web-portal', 'Explicit',
        1.0000, NOW() - INTERVAL '60 days', NOW() - INTERVAL '2 days',
        'e2000000-0000-0000-0000-000000000008'
    ),
    -- Batch processor sincroniza utilizadores periodicamente
    (
        'e4000000-0000-0000-0000-00000000000a',
        'e3000000-0000-0000-0000-000000000004', 'batch-processor', 'Inferred',
        0.8000, NOW() - INTERVAL '45 days', NOW() - INTERVAL '7 days',
        'e2000000-0000-0000-0000-000000000008'
    ),

    -- ── Orders API (e2..009) — 3 consumidores ──────────────────────────────
    -- App móvel submete encomendas
    (
        'e4000000-0000-0000-0000-00000000000b',
        'e3000000-0000-0000-0000-000000000001', 'mobile-app', 'Explicit',
        1.0000, NOW() - INTERVAL '60 days', NOW() - INTERVAL '1 day',
        'e2000000-0000-0000-0000-000000000009'
    ),
    -- Portal web gere encomendas
    (
        'e4000000-0000-0000-0000-00000000000c',
        'e3000000-0000-0000-0000-000000000002', 'web-portal', 'Explicit',
        1.0000, NOW() - INTERVAL '60 days', NOW() - INTERVAL '1 day',
        'e2000000-0000-0000-0000-000000000009'
    ),
    -- Gateway encaminha tráfego de encomendas
    (
        'e4000000-0000-0000-0000-00000000000d',
        'e3000000-0000-0000-0000-000000000003', 'api-gateway', 'Explicit',
        0.9500, NOW() - INTERVAL '45 days', NOW() - INTERVAL '2 days',
        'e2000000-0000-0000-0000-000000000009'
    ),

    -- ── Processing API (e2..003) — 1 consumidor ────────────────────────────
    -- Gateway encaminha processamento de pagamentos (dependência interna)
    (
        'e4000000-0000-0000-0000-00000000000e',
        'e3000000-0000-0000-0000-000000000003', 'api-gateway', 'Inferred',
        0.8500, NOW() - INTERVAL '30 days', NOW() - INTERVAL '3 days',
        'e2000000-0000-0000-0000-000000000003'
    ),

    -- ── Notifications API (e2..00c) — 2 consumidores ───────────────────────
    -- Portal web subscreve notificações
    (
        'e4000000-0000-0000-0000-00000000000f',
        'e3000000-0000-0000-0000-000000000002', 'web-portal', 'Explicit',
        0.9000, NOW() - INTERVAL '30 days', NOW() - INTERVAL '5 days',
        'e2000000-0000-0000-0000-00000000000c'
    ),
    -- App móvel recebe push notifications (inferido por telemetria)
    (
        'e4000000-0000-0000-0000-000000000010',
        'e3000000-0000-0000-0000-000000000001', 'mobile-app', 'Inferred',
        0.7000, NOW() - INTERVAL '14 days', NOW() - INTERVAL '7 days',
        'e2000000-0000-0000-0000-00000000000c'
    ),

    -- ── Products API (e2..00b) — 1 consumidor ──────────────────────────────
    -- Portal web consulta catálogo de produtos
    (
        'e4000000-0000-0000-0000-000000000011',
        'e3000000-0000-0000-0000-000000000002', 'web-portal', 'Explicit',
        1.0000, NOW() - INTERVAL '45 days', NOW() - INTERVAL '3 days',
        'e2000000-0000-0000-0000-00000000000b'
    )
ON CONFLICT ("Id") DO NOTHING;
