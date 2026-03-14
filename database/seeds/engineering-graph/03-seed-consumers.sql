-- ============================================================================
-- NexTraceOne — Engineering Graph — Consumidores de teste
-- Cria 6 consumidores com tipos (Kind) diversos para validar cenários de
-- rastreamento de dependências no grafo: frontends, gateways, jobs,
-- serviços internos e parceiros externos.
-- ============================================================================

INSERT INTO eg_consumer_assets ("Id", "Name", "Kind", "Environment")
VALUES
    -- ── Frontends (aplicações de utilizador final) ──────────────────────────
    ('e3000000-0000-0000-0000-000000000001', 'mobile-app',        'Frontend', 'Production'),
    ('e3000000-0000-0000-0000-000000000002', 'web-portal',        'Frontend', 'Production'),

    -- ── Gateway (ponto de entrada centralizado) ─────────────────────────────
    ('e3000000-0000-0000-0000-000000000003', 'api-gateway',       'Gateway',  'Production'),

    -- ── Job batch (processamento assíncrono/agendado) ───────────────────────
    ('e3000000-0000-0000-0000-000000000004', 'batch-processor',   'Job',      'Production'),

    -- ── Serviço interno de monitorização ────────────────────────────────────
    ('e3000000-0000-0000-0000-000000000005', 'monitoring-agent',  'Service',  'Production'),

    -- ── Parceiro externo (integração B2B) ───────────────────────────────────
    ('e3000000-0000-0000-0000-000000000006', 'external-partner',  'External', 'Production')
ON CONFLICT ("Id") DO NOTHING;
