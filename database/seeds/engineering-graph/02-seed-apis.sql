-- ============================================================================
-- NexTraceOne — Engineering Graph — APIs de teste
-- Cria 12 APIs distribuídas pelos 8 serviços, com versões semânticas,
-- rotas RESTful e visibilidade (Public/Internal) variada.
-- Cada API referencia o serviço proprietário via OwnerServiceId.
-- ============================================================================

INSERT INTO eg_api_assets ("Id", "Name", "RoutePattern", "Version", "Visibility", "OwnerServiceId", "IsDecommissioned")
VALUES
    -- ── payment-gateway (2 APIs) ────────────────────────────────────────────
    (
        'e2000000-0000-0000-0000-000000000001',
        'Payments API',
        '/api/v2/payments',
        '2.1.0',
        'Public',
        'e1000000-0000-0000-0000-000000000001',
        false
    ),
    (
        'e2000000-0000-0000-0000-000000000002',
        'Refunds API',
        '/api/v1/refunds',
        '1.0.0',
        'Public',
        'e1000000-0000-0000-0000-000000000001',
        false
    ),

    -- ── payment-processor (2 APIs) ──────────────────────────────────────────
    (
        'e2000000-0000-0000-0000-000000000003',
        'Processing API',
        '/api/v3/processing',
        '3.0.0',
        'Internal',
        'e1000000-0000-0000-0000-000000000002',
        false
    ),
    (
        'e2000000-0000-0000-0000-000000000004',
        'Settlements API',
        '/api/v1/settlements',
        '1.2.0',
        'Internal',
        'e1000000-0000-0000-0000-000000000002',
        false
    ),

    -- ── payment-reconciliation (1 API) ──────────────────────────────────────
    (
        'e2000000-0000-0000-0000-000000000005',
        'Reconciliation API',
        '/api/v1/reconciliation',
        '1.0.0',
        'Internal',
        'e1000000-0000-0000-0000-000000000003',
        false
    ),

    -- ── auth-service (2 APIs) ───────────────────────────────────────────────
    (
        'e2000000-0000-0000-0000-000000000006',
        'Auth API',
        '/api/v2/auth',
        '2.0.0',
        'Public',
        'e1000000-0000-0000-0000-000000000004',
        false
    ),
    (
        'e2000000-0000-0000-0000-000000000007',
        'Token API',
        '/api/v1/tokens',
        '1.5.0',
        'Internal',
        'e1000000-0000-0000-0000-000000000004',
        false
    ),

    -- ── user-management (1 API) ─────────────────────────────────────────────
    (
        'e2000000-0000-0000-0000-000000000008',
        'Users API',
        '/api/v1/users',
        '1.0.0',
        'Public',
        'e1000000-0000-0000-0000-000000000005',
        false
    ),

    -- ── order-orchestrator (2 APIs) ─────────────────────────────────────────
    (
        'e2000000-0000-0000-0000-000000000009',
        'Orders API',
        '/api/v2/orders',
        '2.0.0',
        'Public',
        'e1000000-0000-0000-0000-000000000006',
        false
    ),
    (
        'e2000000-0000-0000-0000-00000000000a',
        'Checkout API',
        '/api/v1/checkout',
        '1.1.0',
        'Public',
        'e1000000-0000-0000-0000-000000000006',
        false
    ),

    -- ── catalog-service (1 API) ─────────────────────────────────────────────
    (
        'e2000000-0000-0000-0000-00000000000b',
        'Products API',
        '/api/v1/products',
        '1.0.0',
        'Public',
        'e1000000-0000-0000-0000-000000000007',
        false
    ),

    -- ── notification-service (1 API) ────────────────────────────────────────
    (
        'e2000000-0000-0000-0000-00000000000c',
        'Notifications API',
        '/api/v1/notifications',
        '1.0.0',
        'Internal',
        'e1000000-0000-0000-0000-000000000008',
        false
    )
ON CONFLICT ("Id") DO NOTHING;
