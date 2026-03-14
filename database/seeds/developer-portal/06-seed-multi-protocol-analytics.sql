-- ============================================================================
-- NexTraceOne — Developer Portal — Eventos de analytics multi-protocolo
-- Adiciona eventos de analytics que cobrem cenários SOAP/WSDL e eventos Kafka
-- para garantir que a massa de teste represente todo o espectro de protocolos
-- suportados pelo produto: REST, SOAP e Mensageria.
-- ============================================================================
-- Tipos de evento:
--   SearchCatalog, ViewApiDetail, ExecutePlayground,
--   GenerateCode, Subscribe, ViewContract,
--   ViewWsdl, ViewEventSchema, ViewKafkaTopic
-- ============================================================================

INSERT INTO dp_analytics_events (
    "Id", "UserId", "EventType", "EntityId", "EntityType",
    "SearchQuery", "Metadata", "OccurredAt"
)
VALUES
    -- ── Pesquisas por APIs SOAP no catálogo
    (
        'd4000000-0000-0000-0000-000000000016',
        'u1000000-0000-0000-0000-000000000003',
        'SearchCatalog',
        NULL,
        NULL,
        'SOAP payment',
        '{"protocol":"Wsdl","filters":{"type":"SOAP"}}',
        '2025-03-12T09:00:00Z'
    ),
    (
        'd4000000-0000-0000-0000-000000000017',
        'u1000000-0000-0000-0000-000000000002',
        'SearchCatalog',
        NULL,
        NULL,
        'WSDL settlement',
        '{"protocol":"Wsdl","filters":{"type":"SOAP"}}',
        '2025-03-12T09:30:00Z'
    ),

    -- ── Pesquisas por tópicos Kafka e schemas de eventos
    (
        'd4000000-0000-0000-0000-000000000018',
        'u1000000-0000-0000-0000-000000000003',
        'SearchCatalog',
        NULL,
        NULL,
        'Kafka order events',
        '{"protocol":"AsyncApi","filters":{"type":"Event","broker":"Kafka"}}',
        '2025-03-12T10:00:00Z'
    ),
    (
        'd4000000-0000-0000-0000-000000000019',
        'u1000000-0000-0000-0000-000000000007',
        'SearchCatalog',
        NULL,
        NULL,
        'schema registry payments',
        '{"protocol":"AsyncApi","filters":{"schemaRegistry":true}}',
        '2025-03-12T10:15:00Z'
    ),

    -- ── Visualização de contrato WSDL
    (
        'd4000000-0000-0000-0000-000000000020',
        'u1000000-0000-0000-0000-000000000002',
        'ViewContract',
        'e2000000-0000-0000-0000-000000000004',
        'ApiAsset',
        NULL,
        '{"protocol":"Wsdl","format":"XML","operations":["ProcessPayment","RefundPayment"]}',
        '2025-03-12T11:00:00Z'
    ),

    -- ── Visualização de schema de evento Kafka (JSON Schema)
    (
        'd4000000-0000-0000-0000-000000000021',
        'u1000000-0000-0000-0000-000000000003',
        'ViewContract',
        'e2000000-0000-0000-0000-000000000003',
        'ApiAsset',
        NULL,
        '{"protocol":"AsyncApi","format":"JSON_SCHEMA","topic":"orders.created","schemaVersion":3}',
        '2025-03-12T11:30:00Z'
    ),

    -- ── Geração de código para cliente SOAP (C#)
    (
        'd4000000-0000-0000-0000-000000000022',
        'u1000000-0000-0000-0000-000000000003',
        'GenerateCode',
        'e2000000-0000-0000-0000-000000000004',
        'ApiAsset',
        NULL,
        '{"language":"CSharp","type":"SdkClient","protocol":"Wsdl"}',
        '2025-03-12T12:00:00Z'
    ),

    -- ── Geração de código para consumer Kafka (TypeScript)
    (
        'd4000000-0000-0000-0000-000000000023',
        'u1000000-0000-0000-0000-000000000002',
        'GenerateCode',
        'e2000000-0000-0000-0000-000000000003',
        'ApiAsset',
        NULL,
        '{"language":"TypeScript","type":"SdkClient","protocol":"AsyncApi","topic":"orders.created"}',
        '2025-03-12T12:30:00Z'
    ),

    -- ── Subscrição a notificações de tópico Kafka
    (
        'd4000000-0000-0000-0000-000000000024',
        'u1000000-0000-0000-0000-000000000003',
        'Subscribe',
        'e2000000-0000-0000-0000-000000000003',
        'ApiAsset',
        NULL,
        '{"level":"AllChanges","channel":"Webhook","protocol":"AsyncApi","topic":"orders.created"}',
        '2025-03-12T13:00:00Z'
    ),

    -- ── Pesquisa de compatibilidade de schema Avro
    (
        'd4000000-0000-0000-0000-000000000025',
        'u1000000-0000-0000-0000-000000000007',
        'SearchCatalog',
        NULL,
        NULL,
        'Avro schema compatibility',
        '{"protocol":"AsyncApi","filters":{"schemaFormat":"Avro","compatibility":"BACKWARD"}}',
        '2025-03-12T14:00:00Z'
    )
ON CONFLICT ("Id") DO NOTHING;
