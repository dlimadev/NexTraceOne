-- ============================================================================
-- NexTraceOne — Developer Portal — Eventos de analytics de teste
-- Cria 15 eventos cobrindo todos os tipos de interação no portal:
-- pesquisas, visualizações de API, execuções no playground, geração de código,
-- subscrições, visualização de documentação e fluxos de onboarding.
-- ============================================================================
-- PortalEventType (armazenado como string no EF — ver configuração):
--   Search=0, ApiView=1, PlaygroundExecution=2, CodeGeneration=3,
--   SubscriptionCreated=4, DocumentViewed=5,
--   OnboardingStarted=6, OnboardingCompleted=7
-- ============================================================================

INSERT INTO dp_portal_analytics_events (
    "Id", "UserId", "EventType", "EntityId", "EntityType",
    "SearchQuery", "ZeroResults", "DurationMs", "Metadata", "OccurredAt"
)
VALUES
    -- ── Evento 1: Pesquisa no catálogo com resultados ───────────────────────
    (
        'd4000000-0000-0000-0000-000000000001',
        'u1000000-0000-0000-0000-000000000003',
        'Search',
        NULL,
        NULL,
        'payments',
        false,
        120,
        '{"filters":{"domain":"Payments","visibility":"Public"},"resultCount":3}',
        '2025-03-10T09:00:00Z'
    ),

    -- ── Evento 2: Pesquisa no catálogo sem resultados — útil para análise de lacunas
    (
        'd4000000-0000-0000-0000-000000000002',
        'u1000000-0000-0000-0000-000000000003',
        'Search',
        NULL,
        NULL,
        'blockchain transfer',
        true,
        95,
        '{"filters":{},"resultCount":0}',
        '2025-03-10T09:05:00Z'
    ),

    -- ── Evento 3: Visualização de detalhes da Payments API ──────────────────
    (
        'd4000000-0000-0000-0000-000000000003',
        'u1000000-0000-0000-0000-000000000003',
        'ApiView',
        'e2000000-0000-0000-0000-000000000001',
        'ApiAsset',
        NULL,
        NULL,
        NULL,
        '{"source":"search_results","position":1}',
        '2025-03-10T09:10:00Z'
    ),

    -- ── Evento 4: Execução no playground — Payments API GET ─────────────────
    (
        'd4000000-0000-0000-0000-000000000004',
        'u1000000-0000-0000-0000-000000000003',
        'PlaygroundExecution',
        'e2000000-0000-0000-0000-000000000001',
        'ApiAsset',
        NULL,
        NULL,
        87,
        '{"method":"GET","statusCode":200,"path":"/api/v2/payments"}',
        '2025-03-10T09:15:00Z'
    ),

    -- ── Evento 5: Execução no playground — Payments API POST ────────────────
    (
        'd4000000-0000-0000-0000-000000000005',
        'u1000000-0000-0000-0000-000000000003',
        'PlaygroundExecution',
        'e2000000-0000-0000-0000-000000000001',
        'ApiAsset',
        NULL,
        NULL,
        234,
        '{"method":"POST","statusCode":201,"path":"/api/v2/payments"}',
        '2025-03-10T09:20:00Z'
    ),

    -- ── Evento 6: Geração de código — SDK C# para Payments API ─────────────
    (
        'd4000000-0000-0000-0000-000000000006',
        'u1000000-0000-0000-0000-000000000003',
        'CodeGeneration',
        'e2000000-0000-0000-0000-000000000001',
        'ApiAsset',
        NULL,
        NULL,
        3200,
        '{"language":"CSharp","generationType":"SdkClient","isAiGenerated":false}',
        '2025-03-10T09:30:00Z'
    ),

    -- ── Evento 7: Criação de subscrição — Payments API ──────────────────────
    (
        'd4000000-0000-0000-0000-000000000007',
        'u1000000-0000-0000-0000-000000000003',
        'SubscriptionCreated',
        'e2000000-0000-0000-0000-000000000001',
        'ApiAsset',
        NULL,
        NULL,
        NULL,
        '{"level":"BreakingChangesOnly","channel":"Email"}',
        '2025-03-10T09:35:00Z'
    ),

    -- ── Evento 8: Visualização de documentação — Refunds API ────────────────
    (
        'd4000000-0000-0000-0000-000000000008',
        'u1000000-0000-0000-0000-000000000002',
        'DocumentViewed',
        'e2000000-0000-0000-0000-000000000002',
        'ApiAsset',
        NULL,
        NULL,
        NULL,
        '{"documentType":"openapi-spec","version":"1.0.0"}',
        '2025-03-10T10:00:00Z'
    ),

    -- ── Evento 9: Início de onboarding — novo utilizador a explorar o portal
    (
        'd4000000-0000-0000-0000-000000000009',
        'u1000000-0000-0000-0000-000000000008',
        'OnboardingStarted',
        NULL,
        NULL,
        NULL,
        NULL,
        NULL,
        '{"step":"welcome","userAgent":"Mozilla/5.0"}',
        '2025-03-10T10:30:00Z'
    ),

    -- ── Evento 10: Conclusão de onboarding — mesmo utilizador completou o fluxo
    (
        'd4000000-0000-0000-0000-00000000000a',
        'u1000000-0000-0000-0000-000000000008',
        'OnboardingCompleted',
        NULL,
        NULL,
        NULL,
        NULL,
        450000,
        '{"totalSteps":5,"completedSteps":5,"durationMinutes":7.5}',
        '2025-03-10T10:37:30Z'
    ),

    -- ── Evento 11: Pesquisa por TechLead — filtro por domínio Identity ──────
    (
        'd4000000-0000-0000-0000-00000000000b',
        'u1000000-0000-0000-0000-000000000002',
        'Search',
        NULL,
        NULL,
        'authentication oauth',
        false,
        110,
        '{"filters":{"domain":"Identity"},"resultCount":2}',
        '2025-03-10T11:00:00Z'
    ),

    -- ── Evento 12: Visualização de API — Processing API pelo utilizador multi-tenant
    (
        'd4000000-0000-0000-0000-00000000000c',
        'u1000000-0000-0000-0000-000000000007',
        'ApiView',
        'e2000000-0000-0000-0000-000000000003',
        'ApiAsset',
        NULL,
        NULL,
        NULL,
        '{"source":"direct_link","tenant":"globex-inc"}',
        '2025-03-10T11:15:00Z'
    ),

    -- ── Evento 13: Geração de código — Data Models Java por IA ──────────────
    (
        'd4000000-0000-0000-0000-00000000000d',
        'u1000000-0000-0000-0000-000000000007',
        'CodeGeneration',
        'e2000000-0000-0000-0000-000000000003',
        'ApiAsset',
        NULL,
        NULL,
        4800,
        '{"language":"Java","generationType":"DataModels","isAiGenerated":true}',
        '2025-03-10T11:20:00Z'
    ),

    -- ── Evento 14: Evento anónimo — pesquisa sem login no portal público ────
    (
        'd4000000-0000-0000-0000-00000000000e',
        NULL,
        'Search',
        NULL,
        NULL,
        'api documentation',
        false,
        200,
        '{"anonymous":true,"resultCount":5,"userAgent":"Googlebot/2.1"}',
        '2025-03-10T12:00:00Z'
    ),

    -- ── Evento 15: Visualização de documentação — Settlements API pelo Admin ─
    (
        'd4000000-0000-0000-0000-00000000000f',
        'u1000000-0000-0000-0000-000000000001',
        'DocumentViewed',
        'e2000000-0000-0000-0000-000000000004',
        'ApiAsset',
        NULL,
        NULL,
        NULL,
        '{"documentType":"changelog","version":"1.2.0"}',
        '2025-03-10T14:30:00Z'
    )
ON CONFLICT ("Id") DO NOTHING;
