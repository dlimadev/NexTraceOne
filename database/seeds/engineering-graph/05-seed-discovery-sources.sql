-- ============================================================================
-- NexTraceOne — Engineering Graph — Fontes de descoberta de teste
-- Cria fontes de descoberta (DiscoverySource) para as APIs, demonstrando
-- a proveniência dos dados no grafo. Cada fonte indica como a API foi
-- registrada ou detectada, com score de confiança correspondente.
-- ============================================================================
-- Tipos de fonte:
--   Manual          → registo humano directo (confiança 1.0)
--   OpenTelemetry   → inferência por traces distribuídos (confiança 0.85–0.95)
--   CatalogImport   → importação automatizada de catálogo (confiança 0.90)
-- ============================================================================

INSERT INTO eg_discovery_sources (
    "Id", "SourceType", "ExternalReference", "DiscoveredAt", "ConfidenceScore", "ApiAssetId"
)
VALUES
    -- ── Payments API — registada manualmente + confirmada por telemetria ────
    (
        'e5000000-0000-0000-0000-000000000001',
        'Manual',
        'API-REG-2024-001',
        NOW() - INTERVAL '180 days',
        1.0000,
        'e2000000-0000-0000-0000-000000000001'
    ),
    (
        'e5000000-0000-0000-0000-000000000002',
        'OpenTelemetry',
        'otel-trace-payments-abc123',
        NOW() - INTERVAL '90 days',
        0.9200,
        'e2000000-0000-0000-0000-000000000001'
    ),

    -- ── Refunds API — registada manualmente ─────────────────────────────────
    (
        'e5000000-0000-0000-0000-000000000003',
        'Manual',
        'API-REG-2024-002',
        NOW() - INTERVAL '150 days',
        1.0000,
        'e2000000-0000-0000-0000-000000000002'
    ),

    -- ── Processing API — detectada por telemetria ───────────────────────────
    (
        'e5000000-0000-0000-0000-000000000004',
        'OpenTelemetry',
        'otel-trace-processing-def456',
        NOW() - INTERVAL '60 days',
        0.8800,
        'e2000000-0000-0000-0000-000000000003'
    ),

    -- ── Settlements API — importada de catálogo externo ─────────────────────
    (
        'e5000000-0000-0000-0000-000000000005',
        'CatalogImport',
        'catalog-import-batch-2024-03',
        NOW() - INTERVAL '90 days',
        0.9000,
        'e2000000-0000-0000-0000-000000000004'
    ),

    -- ── Reconciliation API — importada de catálogo externo ──────────────────
    (
        'e5000000-0000-0000-0000-000000000006',
        'CatalogImport',
        'catalog-import-batch-2024-03',
        NOW() - INTERVAL '90 days',
        0.9000,
        'e2000000-0000-0000-0000-000000000005'
    ),

    -- ── Auth API — registada manualmente ────────────────────────────────────
    (
        'e5000000-0000-0000-0000-000000000007',
        'Manual',
        'API-REG-2024-003',
        NOW() - INTERVAL '200 days',
        1.0000,
        'e2000000-0000-0000-0000-000000000006'
    ),

    -- ── Token API — registada manualmente ───────────────────────────────────
    (
        'e5000000-0000-0000-0000-000000000008',
        'Manual',
        'API-REG-2024-004',
        NOW() - INTERVAL '200 days',
        1.0000,
        'e2000000-0000-0000-0000-000000000007'
    ),

    -- ── Users API — detectada por telemetria ────────────────────────────────
    (
        'e5000000-0000-0000-0000-000000000009',
        'OpenTelemetry',
        'otel-trace-users-ghi789',
        NOW() - INTERVAL '45 days',
        0.8500,
        'e2000000-0000-0000-0000-000000000008'
    ),

    -- ── Orders API — registada manualmente ──────────────────────────────────
    (
        'e5000000-0000-0000-0000-00000000000a',
        'Manual',
        'API-REG-2024-005',
        NOW() - INTERVAL '120 days',
        1.0000,
        'e2000000-0000-0000-0000-000000000009'
    ),

    -- ── Checkout API — detectada por telemetria ─────────────────────────────
    (
        'e5000000-0000-0000-0000-00000000000b',
        'OpenTelemetry',
        'otel-trace-checkout-jkl012',
        NOW() - INTERVAL '30 days',
        0.9500,
        'e2000000-0000-0000-0000-00000000000a'
    ),

    -- ── Products API — importada de catálogo externo ────────────────────────
    (
        'e5000000-0000-0000-0000-00000000000c',
        'CatalogImport',
        'catalog-import-batch-2024-05',
        NOW() - INTERVAL '60 days',
        0.9000,
        'e2000000-0000-0000-0000-00000000000b'
    )
ON CONFLICT ("Id") DO NOTHING;
