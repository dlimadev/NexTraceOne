-- ============================================================================
-- NexTraceOne — Engineering Graph — Vistas salvas de teste
-- Cria 2 vistas salvas (SavedGraphView) que demonstram configurações de
-- visualização reutilizáveis com filtros, overlay e foco persistidos.
-- Permitem testar carregamento de vistas, deep links e partilha entre equipa.
-- ============================================================================

INSERT INTO saved_graph_views (
    "Id", "Name", "Description", "OwnerId", "IsShared", "FiltersJson", "CreatedAt"
)
VALUES
    -- ── Vista 1: Visão geral do domínio Payments ────────────────────────────
    -- Foco nos 3 serviços de pagamentos com overlay de saúde activo.
    -- Partilhada com a equipa para acompanhamento diário.
    (
        'e8000000-0000-0000-0000-000000000001',
        'Payments Domain Overview',
        'Visão consolidada de todos os serviços e APIs do domínio Payments, com overlay de saúde para monitorização da equipa de pagamentos.',
        'u1000000-0000-0000-0000-000000000001',
        true,
        '{
            "domains": ["Payments"],
            "nodeTypes": ["Service", "Api"],
            "overlay": "Health",
            "layout": "hierarchical",
            "showConsumers": true,
            "showEdgeLabels": true,
            "focusNodeId": null,
            "depthLimit": 2,
            "minConfidenceScore": 0.80
        }',
        NOW() - INTERVAL '14 days'
    ),

    -- ── Vista 2: Dependências críticas cross-domain ─────────────────────────
    -- Foco em dependências que cruzam fronteiras de domínio.
    -- Visão privada do tech lead para análise de impacto de mudanças.
    (
        'e8000000-0000-0000-0000-000000000002',
        'Critical Dependencies',
        'Mapa de dependências críticas entre domínios, focado em APIs com mais de 3 consumidores e relações cross-domain. Útil para análise de blast radius antes de releases.',
        'u1000000-0000-0000-0000-000000000002',
        false,
        '{
            "domains": ["Payments", "Identity", "Orders"],
            "nodeTypes": ["Service", "Api"],
            "overlay": "Risk",
            "layout": "force-directed",
            "showConsumers": true,
            "showEdgeLabels": true,
            "focusNodeId": null,
            "depthLimit": 3,
            "minConfidenceScore": 0.60,
            "highlightCrossDomain": true,
            "minConsumerCount": 3
        }',
        NOW() - INTERVAL '7 days'
    )
ON CONFLICT ("Id") DO NOTHING;
