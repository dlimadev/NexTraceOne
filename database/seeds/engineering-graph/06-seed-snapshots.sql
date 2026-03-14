-- ============================================================================
-- NexTraceOne — Engineering Graph — Snapshots temporais de teste
-- Cria 3 snapshots do grafo representando evolução ao longo do tempo:
--   1. Baseline Q1 2026 — estado inicial estável (30 dias atrás)
--   2. Post-Release v2.1 — após major release de pagamentos (7 dias atrás)
--   3. Current State — estado actual com todos os serviços mapeados
-- Os campos NodesJson e EdgesJson contêm estado simplificado para debug.
-- Em produção, estes campos contêm o grafo serializado completo.
-- ============================================================================

INSERT INTO graph_snapshots (
    "Id", "Label", "CapturedAt", "NodesJson", "EdgesJson",
    "NodeCount", "EdgeCount", "CreatedBy"
)
VALUES
    -- ── Snapshot 1: Baseline estável antes de alterações ────────────────────
    (
        'e6000000-0000-0000-0000-000000000001',
        'Baseline Q1 2026',
        NOW() - INTERVAL '30 days',
        '[
            {"id": "e1000000-0000-0000-0000-000000000001", "type": "Service", "name": "payment-gateway"},
            {"id": "e1000000-0000-0000-0000-000000000002", "type": "Service", "name": "payment-processor"},
            {"id": "e1000000-0000-0000-0000-000000000004", "type": "Service", "name": "auth-service"},
            {"id": "e1000000-0000-0000-0000-000000000005", "type": "Service", "name": "user-management"},
            {"id": "e1000000-0000-0000-0000-000000000006", "type": "Service", "name": "order-orchestrator"},
            {"id": "e2000000-0000-0000-0000-000000000001", "type": "Api", "name": "Payments API v2.0.0"},
            {"id": "e2000000-0000-0000-0000-000000000006", "type": "Api", "name": "Auth API v2.0.0"},
            {"id": "e2000000-0000-0000-0000-000000000009", "type": "Api", "name": "Orders API v2.0.0"}
        ]',
        '[
            {"source": "e1000000-0000-0000-0000-000000000001", "target": "e2000000-0000-0000-0000-000000000001", "type": "Exposes"},
            {"source": "e1000000-0000-0000-0000-000000000004", "target": "e2000000-0000-0000-0000-000000000006", "type": "Exposes"},
            {"source": "e1000000-0000-0000-0000-000000000006", "target": "e2000000-0000-0000-0000-000000000009", "type": "Exposes"},
            {"source": "e3000000-0000-0000-0000-000000000001", "target": "e2000000-0000-0000-0000-000000000001", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000002", "target": "e2000000-0000-0000-0000-000000000001", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000003", "target": "e2000000-0000-0000-0000-000000000001", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000001", "target": "e2000000-0000-0000-0000-000000000006", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000002", "target": "e2000000-0000-0000-0000-000000000006", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000003", "target": "e2000000-0000-0000-0000-000000000006", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000001", "target": "e2000000-0000-0000-0000-000000000009", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000002", "target": "e2000000-0000-0000-0000-000000000009", "type": "Calls"},
            {"source": "e1000000-0000-0000-0000-000000000001", "target": "e2000000-0000-0000-0000-000000000006", "type": "DependsOn"},
            {"source": "e1000000-0000-0000-0000-000000000006", "target": "e2000000-0000-0000-0000-000000000006", "type": "DependsOn"},
            {"source": "e1000000-0000-0000-0000-000000000001", "target": "e1000000-0000-0000-0000-000000000002", "type": "DependsOn"},
            {"source": "Payments", "target": "e1000000-0000-0000-0000-000000000001", "type": "Contains"},
            {"source": "Payments", "target": "e1000000-0000-0000-0000-000000000002", "type": "Contains"},
            {"source": "Identity", "target": "e1000000-0000-0000-0000-000000000004", "type": "Contains"},
            {"source": "Identity", "target": "e1000000-0000-0000-0000-000000000005", "type": "Contains"},
            {"source": "Orders", "target": "e1000000-0000-0000-0000-000000000006", "type": "Contains"},
            {"source": "Orders", "target": "e1000000-0000-0000-0000-000000000007", "type": "Contains"}
        ]',
        8,
        20,
        'system-snapshot-job'
    ),

    -- ── Snapshot 2: Após release v2.1 do payment-gateway ────────────────────
    (
        'e6000000-0000-0000-0000-000000000002',
        'Post-Release v2.1',
        NOW() - INTERVAL '7 days',
        '[
            {"id": "e1000000-0000-0000-0000-000000000001", "type": "Service", "name": "payment-gateway"},
            {"id": "e1000000-0000-0000-0000-000000000002", "type": "Service", "name": "payment-processor"},
            {"id": "e1000000-0000-0000-0000-000000000003", "type": "Service", "name": "payment-reconciliation"},
            {"id": "e1000000-0000-0000-0000-000000000004", "type": "Service", "name": "auth-service"},
            {"id": "e1000000-0000-0000-0000-000000000005", "type": "Service", "name": "user-management"},
            {"id": "e1000000-0000-0000-0000-000000000006", "type": "Service", "name": "order-orchestrator"},
            {"id": "e1000000-0000-0000-0000-000000000007", "type": "Service", "name": "catalog-service"},
            {"id": "e2000000-0000-0000-0000-000000000001", "type": "Api", "name": "Payments API v2.1.0"},
            {"id": "e2000000-0000-0000-0000-000000000006", "type": "Api", "name": "Auth API v2.0.0"},
            {"id": "e2000000-0000-0000-0000-000000000009", "type": "Api", "name": "Orders API v2.0.0"}
        ]',
        '[
            {"source": "e1000000-0000-0000-0000-000000000001", "target": "e2000000-0000-0000-0000-000000000001", "type": "Exposes"},
            {"source": "e1000000-0000-0000-0000-000000000004", "target": "e2000000-0000-0000-0000-000000000006", "type": "Exposes"},
            {"source": "e1000000-0000-0000-0000-000000000006", "target": "e2000000-0000-0000-0000-000000000009", "type": "Exposes"},
            {"source": "e3000000-0000-0000-0000-000000000001", "target": "e2000000-0000-0000-0000-000000000001", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000002", "target": "e2000000-0000-0000-0000-000000000001", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000003", "target": "e2000000-0000-0000-0000-000000000001", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000006", "target": "e2000000-0000-0000-0000-000000000001", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000001", "target": "e2000000-0000-0000-0000-000000000006", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000002", "target": "e2000000-0000-0000-0000-000000000006", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000003", "target": "e2000000-0000-0000-0000-000000000006", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000001", "target": "e2000000-0000-0000-0000-000000000009", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000002", "target": "e2000000-0000-0000-0000-000000000009", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000003", "target": "e2000000-0000-0000-0000-000000000009", "type": "Calls"},
            {"source": "e1000000-0000-0000-0000-000000000001", "target": "e2000000-0000-0000-0000-000000000006", "type": "DependsOn"},
            {"source": "e1000000-0000-0000-0000-000000000006", "target": "e2000000-0000-0000-0000-000000000006", "type": "DependsOn"},
            {"source": "e1000000-0000-0000-0000-000000000001", "target": "e1000000-0000-0000-0000-000000000002", "type": "DependsOn"},
            {"source": "e1000000-0000-0000-0000-000000000001", "target": "e1000000-0000-0000-0000-000000000003", "type": "DependsOn"},
            {"source": "Payments", "target": "e1000000-0000-0000-0000-000000000001", "type": "Contains"},
            {"source": "Payments", "target": "e1000000-0000-0000-0000-000000000002", "type": "Contains"},
            {"source": "Payments", "target": "e1000000-0000-0000-0000-000000000003", "type": "Contains"},
            {"source": "Identity", "target": "e1000000-0000-0000-0000-000000000004", "type": "Contains"},
            {"source": "Identity", "target": "e1000000-0000-0000-0000-000000000005", "type": "Contains"},
            {"source": "Orders", "target": "e1000000-0000-0000-0000-000000000006", "type": "Contains"},
            {"source": "Orders", "target": "e1000000-0000-0000-0000-000000000007", "type": "Contains"},
            {"source": "e1000000-0000-0000-0000-000000000007", "target": "e2000000-0000-0000-0000-00000000000b", "type": "Exposes"}
        ]',
        10,
        25,
        'system-snapshot-job'
    ),

    -- ── Snapshot 3: Estado actual completo com todos os 8 serviços ──────────
    (
        'e6000000-0000-0000-0000-000000000003',
        'Current State',
        NOW(),
        '[
            {"id": "e1000000-0000-0000-0000-000000000001", "type": "Service", "name": "payment-gateway"},
            {"id": "e1000000-0000-0000-0000-000000000002", "type": "Service", "name": "payment-processor"},
            {"id": "e1000000-0000-0000-0000-000000000003", "type": "Service", "name": "payment-reconciliation"},
            {"id": "e1000000-0000-0000-0000-000000000004", "type": "Service", "name": "auth-service"},
            {"id": "e1000000-0000-0000-0000-000000000005", "type": "Service", "name": "user-management"},
            {"id": "e1000000-0000-0000-0000-000000000006", "type": "Service", "name": "order-orchestrator"},
            {"id": "e1000000-0000-0000-0000-000000000007", "type": "Service", "name": "catalog-service"},
            {"id": "e1000000-0000-0000-0000-000000000008", "type": "Service", "name": "notification-service"},
            {"id": "e2000000-0000-0000-0000-000000000001", "type": "Api", "name": "Payments API v2.1.0"},
            {"id": "e2000000-0000-0000-0000-000000000006", "type": "Api", "name": "Auth API v2.0.0"},
            {"id": "e2000000-0000-0000-0000-000000000009", "type": "Api", "name": "Orders API v2.0.0"},
            {"id": "e2000000-0000-0000-0000-00000000000b", "type": "Api", "name": "Products API v1.0.0"}
        ]',
        '[
            {"source": "e1000000-0000-0000-0000-000000000001", "target": "e2000000-0000-0000-0000-000000000001", "type": "Exposes"},
            {"source": "e1000000-0000-0000-0000-000000000004", "target": "e2000000-0000-0000-0000-000000000006", "type": "Exposes"},
            {"source": "e1000000-0000-0000-0000-000000000006", "target": "e2000000-0000-0000-0000-000000000009", "type": "Exposes"},
            {"source": "e1000000-0000-0000-0000-000000000007", "target": "e2000000-0000-0000-0000-00000000000b", "type": "Exposes"},
            {"source": "e1000000-0000-0000-0000-000000000008", "target": "e2000000-0000-0000-0000-00000000000c", "type": "Exposes"},
            {"source": "e3000000-0000-0000-0000-000000000001", "target": "e2000000-0000-0000-0000-000000000001", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000002", "target": "e2000000-0000-0000-0000-000000000001", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000003", "target": "e2000000-0000-0000-0000-000000000001", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000006", "target": "e2000000-0000-0000-0000-000000000001", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000001", "target": "e2000000-0000-0000-0000-000000000006", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000002", "target": "e2000000-0000-0000-0000-000000000006", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000003", "target": "e2000000-0000-0000-0000-000000000006", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000005", "target": "e2000000-0000-0000-0000-000000000006", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000001", "target": "e2000000-0000-0000-0000-000000000009", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000002", "target": "e2000000-0000-0000-0000-000000000009", "type": "Calls"},
            {"source": "e3000000-0000-0000-0000-000000000003", "target": "e2000000-0000-0000-0000-000000000009", "type": "Calls"},
            {"source": "e1000000-0000-0000-0000-000000000001", "target": "e2000000-0000-0000-0000-000000000006", "type": "DependsOn"},
            {"source": "e1000000-0000-0000-0000-000000000006", "target": "e2000000-0000-0000-0000-000000000006", "type": "DependsOn"},
            {"source": "e1000000-0000-0000-0000-000000000008", "target": "e2000000-0000-0000-0000-000000000006", "type": "DependsOn"},
            {"source": "e1000000-0000-0000-0000-000000000001", "target": "e1000000-0000-0000-0000-000000000002", "type": "DependsOn"},
            {"source": "e1000000-0000-0000-0000-000000000001", "target": "e1000000-0000-0000-0000-000000000003", "type": "DependsOn"},
            {"source": "Payments", "target": "e1000000-0000-0000-0000-000000000001", "type": "Contains"},
            {"source": "Payments", "target": "e1000000-0000-0000-0000-000000000002", "type": "Contains"},
            {"source": "Payments", "target": "e1000000-0000-0000-0000-000000000003", "type": "Contains"},
            {"source": "Identity", "target": "e1000000-0000-0000-0000-000000000004", "type": "Contains"},
            {"source": "Identity", "target": "e1000000-0000-0000-0000-000000000005", "type": "Contains"},
            {"source": "Orders", "target": "e1000000-0000-0000-0000-000000000006", "type": "Contains"},
            {"source": "Orders", "target": "e1000000-0000-0000-0000-000000000007", "type": "Contains"}
        ]',
        12,
        28,
        'system-snapshot-job'
    )
ON CONFLICT ("Id") DO NOTHING;
