-- ============================================================================
-- NexTraceOne — Engineering Graph — Reset de massa de teste
-- Limpa TODOS os dados de teste do módulo Engineering Graph em ordem segura
-- de dependências (foreign keys). Elimina: vistas salvas, registros de saúde,
-- snapshots, fontes de descoberta, relações de consumo, consumidores, APIs
-- e serviços.
-- ATENÇÃO: Usar APENAS em ambientes de desenvolvimento/teste local.
-- ============================================================================

-- ── Nomes dos serviços de teste (usados como filtro em cascata) ──────────────
-- payment-gateway, payment-processor, payment-reconciliation,
-- auth-service, user-management, order-orchestrator,
-- catalog-service, notification-service

-- ── Vistas salvas (sem dependentes — seguro eliminar primeiro) ───────────────
DELETE FROM saved_graph_views WHERE "Id" IN (
    'e8000000-0000-0000-0000-000000000001',
    'e8000000-0000-0000-0000-000000000002'
);

-- ── Registros de saúde de nós (sem dependentes) ─────────────────────────────
DELETE FROM node_health_records WHERE "Id" IN (
    'e7000000-0000-0000-0000-000000000001',
    'e7000000-0000-0000-0000-000000000002',
    'e7000000-0000-0000-0000-000000000003',
    'e7000000-0000-0000-0000-000000000004',
    'e7000000-0000-0000-0000-000000000005',
    'e7000000-0000-0000-0000-000000000006',
    'e7000000-0000-0000-0000-000000000007',
    'e7000000-0000-0000-0000-000000000008',
    'e7000000-0000-0000-0000-000000000009',
    'e7000000-0000-0000-0000-00000000000a',
    'e7000000-0000-0000-0000-00000000000b',
    'e7000000-0000-0000-0000-00000000000c'
);

-- ── Snapshots do grafo (sem dependentes) ────────────────────────────────────
DELETE FROM graph_snapshots WHERE "Id" IN (
    'e6000000-0000-0000-0000-000000000001',
    'e6000000-0000-0000-0000-000000000002',
    'e6000000-0000-0000-0000-000000000003'
);

-- ── Fontes de descoberta (dependem de eg_api_assets via FK ApiAssetId) ──────
DELETE FROM eg_discovery_sources WHERE "ApiAssetId" IN (
    SELECT "Id" FROM eg_api_assets WHERE "OwnerServiceId" IN (
        SELECT "Id" FROM eg_service_assets WHERE "Name" IN (
            'payment-gateway', 'payment-processor', 'payment-reconciliation',
            'auth-service', 'user-management',
            'order-orchestrator', 'catalog-service', 'notification-service'
        )
    )
);

-- ── Relações de consumo (dependem de eg_api_assets e eg_consumer_assets) ────
DELETE FROM eg_consumer_relationships WHERE "ApiAssetId" IN (
    SELECT "Id" FROM eg_api_assets WHERE "OwnerServiceId" IN (
        SELECT "Id" FROM eg_service_assets WHERE "Name" IN (
            'payment-gateway', 'payment-processor', 'payment-reconciliation',
            'auth-service', 'user-management',
            'order-orchestrator', 'catalog-service', 'notification-service'
        )
    )
);

-- ── APIs (dependem de eg_service_assets via FK OwnerServiceId) ──────────────
DELETE FROM eg_api_assets WHERE "OwnerServiceId" IN (
    SELECT "Id" FROM eg_service_assets WHERE "Name" IN (
        'payment-gateway', 'payment-processor', 'payment-reconciliation',
        'auth-service', 'user-management',
        'order-orchestrator', 'catalog-service', 'notification-service'
    )
);

-- ── Consumidores (referenciados por eg_consumer_relationships, já eliminados) ─
DELETE FROM eg_consumer_assets WHERE "Name" IN (
    'mobile-app', 'web-portal', 'api-gateway',
    'batch-processor', 'monitoring-agent', 'external-partner'
);

-- ── Serviços (raiz da hierarquia — eliminar por último) ─────────────────────
DELETE FROM eg_service_assets WHERE "Name" IN (
    'payment-gateway', 'payment-processor', 'payment-reconciliation',
    'auth-service', 'user-management',
    'order-orchestrator', 'catalog-service', 'notification-service'
);

-- Confirmação
DO $$ BEGIN RAISE NOTICE 'Engineering Graph test data reset completed.'; END $$;
