-- ============================================================================
-- NexTraceOne — Engineering Graph — Registros de saúde de nós de teste
-- Cria registros de saúde (NodeHealthRecord) para demonstrar o overlay de
-- Health no grafo. Cada registro contém score, status, factores contribuintes
-- e sistema de origem, permitindo explicar visualmente o estado de cada nó.
-- ============================================================================
-- Cenários de saúde cobertos:
--   • Serviços saudáveis com score > 0.90 (maioria)
--   • Serviço degradado: payment-reconciliation (score 0.65, erro rate alto)
--   • Consumidor desconhecido: monitoring-agent (score 0.00, sem dados)
--   • APIs com métricas operacionais variadas
-- ============================================================================

INSERT INTO node_health_records (
    "Id", "NodeId", "NodeType", "OverlayMode", "Status",
    "Score", "FactorsJson", "CalculatedAt", "SourceSystem"
)
VALUES
    -- ── Serviços — Overlay Health ───────────────────────────────────────────

    -- payment-gateway: saudável, baixa latência e alto throughput
    (
        'e7000000-0000-0000-0000-000000000001',
        'e1000000-0000-0000-0000-000000000001',
        'Service', 'Health', 'Healthy',
        0.9500,
        '{"errorRate": 0.01, "avgLatencyMs": 45, "requestsPerMin": 1200, "p99LatencyMs": 120}',
        NOW() - INTERVAL '1 hour',
        'prometheus-exporter'
    ),

    -- payment-processor: saudável, processamento estável
    (
        'e7000000-0000-0000-0000-000000000002',
        'e1000000-0000-0000-0000-000000000002',
        'Service', 'Health', 'Healthy',
        0.9200,
        '{"errorRate": 0.02, "avgLatencyMs": 85, "requestsPerMin": 800, "p99LatencyMs": 250}',
        NOW() - INTERVAL '1 hour',
        'prometheus-exporter'
    ),

    -- payment-reconciliation: DEGRADADO — taxa de erros elevada e latência alta
    (
        'e7000000-0000-0000-0000-000000000003',
        'e1000000-0000-0000-0000-000000000003',
        'Service', 'Health', 'Degraded',
        0.6500,
        '{"errorRate": 0.12, "avgLatencyMs": 450, "requestsPerMin": 150, "p99LatencyMs": 2100, "note": "Batch reconciliation failing intermittently"}',
        NOW() - INTERVAL '30 minutes',
        'prometheus-exporter'
    ),

    -- auth-service: muito saudável, latência ultra-baixa
    (
        'e7000000-0000-0000-0000-000000000004',
        'e1000000-0000-0000-0000-000000000004',
        'Service', 'Health', 'Healthy',
        0.9800,
        '{"errorRate": 0.001, "avgLatencyMs": 12, "requestsPerMin": 5000, "p99LatencyMs": 35}',
        NOW() - INTERVAL '1 hour',
        'prometheus-exporter'
    ),

    -- user-management: saudável
    (
        'e7000000-0000-0000-0000-000000000005',
        'e1000000-0000-0000-0000-000000000005',
        'Service', 'Health', 'Healthy',
        0.9100,
        '{"errorRate": 0.03, "avgLatencyMs": 65, "requestsPerMin": 400, "p99LatencyMs": 180}',
        NOW() - INTERVAL '1 hour',
        'prometheus-exporter'
    ),

    -- order-orchestrator: saudável
    (
        'e7000000-0000-0000-0000-000000000006',
        'e1000000-0000-0000-0000-000000000006',
        'Service', 'Health', 'Healthy',
        0.9300,
        '{"errorRate": 0.015, "avgLatencyMs": 55, "requestsPerMin": 900, "p99LatencyMs": 150}',
        NOW() - INTERVAL '1 hour',
        'prometheus-exporter'
    ),

    -- catalog-service: saudável, read-heavy com cache eficiente
    (
        'e7000000-0000-0000-0000-000000000007',
        'e1000000-0000-0000-0000-000000000007',
        'Service', 'Health', 'Healthy',
        0.9600,
        '{"errorRate": 0.005, "avgLatencyMs": 20, "requestsPerMin": 2000, "cacheHitRate": 0.92}',
        NOW() - INTERVAL '1 hour',
        'prometheus-exporter'
    ),

    -- notification-service: saudável, throughput moderado
    (
        'e7000000-0000-0000-0000-000000000008',
        'e1000000-0000-0000-0000-000000000008',
        'Service', 'Health', 'Healthy',
        0.9000,
        '{"errorRate": 0.04, "avgLatencyMs": 95, "requestsPerMin": 300, "deliveryRate": 0.97}',
        NOW() - INTERVAL '1 hour',
        'prometheus-exporter'
    ),

    -- ── APIs representativas — Overlay Health ───────────────────────────────

    -- Payments API: saudável
    (
        'e7000000-0000-0000-0000-000000000009',
        'e2000000-0000-0000-0000-000000000001',
        'Api', 'Health', 'Healthy',
        0.9400,
        '{"errorRate": 0.01, "avgLatencyMs": 48, "requestsPerMin": 1150, "openApiCompliance": 0.98}',
        NOW() - INTERVAL '1 hour',
        'prometheus-exporter'
    ),

    -- Auth API: muito saudável
    (
        'e7000000-0000-0000-0000-00000000000a',
        'e2000000-0000-0000-0000-000000000006',
        'Api', 'Health', 'Healthy',
        0.9700,
        '{"errorRate": 0.002, "avgLatencyMs": 15, "requestsPerMin": 4800, "openApiCompliance": 1.00}',
        NOW() - INTERVAL '1 hour',
        'prometheus-exporter'
    ),

    -- Orders API: saudável
    (
        'e7000000-0000-0000-0000-00000000000b',
        'e2000000-0000-0000-0000-000000000009',
        'Api', 'Health', 'Healthy',
        0.9200,
        '{"errorRate": 0.02, "avgLatencyMs": 60, "requestsPerMin": 850, "openApiCompliance": 0.95}',
        NOW() - INTERVAL '1 hour',
        'prometheus-exporter'
    ),

    -- ── Consumidor — monitoring-agent: DESCONHECIDO (sem dados suficientes) ─
    (
        'e7000000-0000-0000-0000-00000000000c',
        'e3000000-0000-0000-0000-000000000005',
        'Service', 'Health', 'Unknown',
        0.0000,
        '{"note": "No telemetry data available for this consumer"}',
        NOW() - INTERVAL '2 hours',
        'health-check-aggregator'
    )
ON CONFLICT ("Id") DO NOTHING;
