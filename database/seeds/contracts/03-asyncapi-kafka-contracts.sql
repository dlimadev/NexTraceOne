-- =============================================================================
-- Script 03: Seed de contratos AsyncAPI / Kafka
-- Módulo: Contracts & Interoperability (Módulo 5)
-- Uso: Apenas desenvolvimento/debug — NÃO executar em produção
-- Pré-requisito: Executar 00-reset.sql, 01 e 02 antes
-- =============================================================================

-- Eventos de Utilizadores (AsyncAPI 2.6) — versão 1.0.0 (Draft)
-- Cenário: contrato event-driven com dois canais (user/signedup e user/updated)
INSERT INTO catalog.ct_contract_versions (
    id, api_asset_id, sem_ver, spec_content, format, protocol, lifecycle_state,
    imported_from, is_locked, created_at, created_by, updated_at, updated_by, is_deleted
) VALUES (
    'c5030001-0000-0000-0000-000000000001'::uuid,
    'c5000001-0000-0000-0000-000000000004'::uuid,
    '1.0.0',
    '{"asyncapi":"2.6.0","info":{"title":"User Events","version":"1.0.0","description":"Event streaming de utilizadores para Kafka"},"servers":{"production":{"url":"kafka://broker.internal:9092","protocol":"kafka"}},"channels":{"user/signedup":{"publish":{"operationId":"publishUserSignedUp","summary":"Publica evento de registo de utilizador","message":{"payload":{"type":"object","properties":{"userId":{"type":"string"},"email":{"type":"string","format":"email"},"timestamp":{"type":"string","format":"date-time"}},"required":["userId","email","timestamp"]}}}},"user/updated":{"publish":{"operationId":"publishUserUpdated","summary":"Publica evento de atualização de utilizador","message":{"payload":{"type":"object","properties":{"userId":{"type":"string"},"changedFields":{"type":"array","items":{"type":"string"}},"timestamp":{"type":"string","format":"date-time"}},"required":["userId","timestamp"]}}}}},"components":{"schemas":{"UserSignedUpEvent":{"type":"object","properties":{"userId":{"type":"string"},"email":{"type":"string"},"timestamp":{"type":"string"}}},"UserUpdatedEvent":{"type":"object","properties":{"userId":{"type":"string"},"changedFields":{"type":"array"},"timestamp":{"type":"string"}}}}}}',
    'json', 'AsyncApi', 'Draft',
    'schema-registry', false,
    NOW() - INTERVAL '40 days', 'seed-script', NOW() - INTERVAL '40 days', 'seed-script', false
);

-- Eventos de Utilizadores (AsyncAPI 2.6) — versão 1.1.0 (Approved, mudança aditiva)
-- Cenário: novo canal user/deleted adicionado — mudança aditiva
INSERT INTO catalog.ct_contract_versions (
    id, api_asset_id, sem_ver, spec_content, format, protocol, lifecycle_state,
    imported_from, is_locked, created_at, created_by, updated_at, updated_by, is_deleted
) VALUES (
    'c5030001-0000-0000-0000-000000000002'::uuid,
    'c5000001-0000-0000-0000-000000000004'::uuid,
    '1.1.0',
    '{"asyncapi":"2.6.0","info":{"title":"User Events","version":"1.1.0","description":"Event streaming de utilizadores para Kafka"},"servers":{"production":{"url":"kafka://broker.internal:9092","protocol":"kafka"}},"channels":{"user/signedup":{"publish":{"operationId":"publishUserSignedUp","summary":"Publica evento de registo de utilizador","message":{"payload":{"type":"object","properties":{"userId":{"type":"string"},"email":{"type":"string","format":"email"},"timestamp":{"type":"string","format":"date-time"}},"required":["userId","email","timestamp"]}}}},"user/updated":{"publish":{"operationId":"publishUserUpdated","summary":"Publica evento de atualização de utilizador","message":{"payload":{"type":"object","properties":{"userId":{"type":"string"},"changedFields":{"type":"array","items":{"type":"string"}},"timestamp":{"type":"string","format":"date-time"}},"required":["userId","timestamp"]}}}},"user/deleted":{"publish":{"operationId":"publishUserDeleted","summary":"Publica evento de remoção de utilizador","message":{"payload":{"type":"object","properties":{"userId":{"type":"string"},"reason":{"type":"string"},"timestamp":{"type":"string","format":"date-time"}},"required":["userId","timestamp"]}}}}},"components":{"schemas":{"UserSignedUpEvent":{"type":"object"},"UserUpdatedEvent":{"type":"object"},"UserDeletedEvent":{"type":"object","properties":{"userId":{"type":"string"},"reason":{"type":"string"},"timestamp":{"type":"string"}}}}}}',
    'json', 'AsyncApi', 'Approved',
    'schema-registry', false,
    NOW() - INTERVAL '25 days', 'seed-script', NOW() - INTERVAL '25 days', 'seed-script', false
);

-- Eventos de Utilizadores (AsyncAPI 2.6) — versão 2.0.0 (InReview, breaking change)
-- Cenário: campo obrigatório adicionado ao canal user/signedup — breaking change
INSERT INTO catalog.ct_contract_versions (
    id, api_asset_id, sem_ver, spec_content, format, protocol, lifecycle_state,
    imported_from, is_locked, created_at, created_by, updated_at, updated_by, is_deleted
) VALUES (
    'c5030001-0000-0000-0000-000000000003'::uuid,
    'c5000001-0000-0000-0000-000000000004'::uuid,
    '2.0.0',
    '{"asyncapi":"2.6.0","info":{"title":"User Events","version":"2.0.0","description":"Event streaming de utilizadores v2"},"servers":{"production":{"url":"kafka://broker.internal:9092","protocol":"kafka"}},"channels":{"user/signedup":{"publish":{"operationId":"publishUserSignedUp","summary":"Publica evento de registo de utilizador v2","message":{"payload":{"type":"object","properties":{"userId":{"type":"string"},"email":{"type":"string","format":"email"},"fullName":{"type":"string"},"tenantId":{"type":"string"},"timestamp":{"type":"string","format":"date-time"}},"required":["userId","email","fullName","tenantId","timestamp"]}}}},"user/updated":{"publish":{"operationId":"publishUserUpdated","message":{"payload":{"type":"object","properties":{"userId":{"type":"string"},"changedFields":{"type":"array","items":{"type":"string"}},"timestamp":{"type":"string","format":"date-time"}},"required":["userId","timestamp"]}}}},"user/deleted":{"publish":{"operationId":"publishUserDeleted","message":{"payload":{"type":"object","properties":{"userId":{"type":"string"},"reason":{"type":"string"},"timestamp":{"type":"string","format":"date-time"}},"required":["userId","timestamp"]}}}}},"components":{"schemas":{"UserSignedUpEvent":{"type":"object"},"UserUpdatedEvent":{"type":"object"},"UserDeletedEvent":{"type":"object"}}}}',
    'json', 'AsyncApi', 'InReview',
    'schema-registry', false,
    NOW() - INTERVAL '3 days', 'seed-script', NOW() - INTERVAL '3 days', 'seed-script', false
);

-- Eventos de Pedidos (AsyncAPI 2.6) — versão 1.0.0 (Locked)
-- Cenário: contrato Kafka do domínio de pedidos, locked para produção
INSERT INTO catalog.ct_contract_versions (
    id, api_asset_id, sem_ver, spec_content, format, protocol, lifecycle_state,
    imported_from, is_locked, locked_at, locked_by,
    created_at, created_by, updated_at, updated_by, is_deleted
) VALUES (
    'c5030001-0000-0000-0000-000000000004'::uuid,
    'c5000001-0000-0000-0000-000000000005'::uuid,
    '1.0.0',
    '{"asyncapi":"2.6.0","info":{"title":"Order Events","version":"1.0.0","description":"Eventos de ciclo de vida de pedidos"},"servers":{"production":{"url":"kafka://broker.internal:9092","protocol":"kafka"}},"channels":{"order/created":{"publish":{"operationId":"publishOrderCreated","message":{"payload":{"type":"object","properties":{"orderId":{"type":"string"},"customerId":{"type":"string"},"amount":{"type":"number"},"currency":{"type":"string"},"timestamp":{"type":"string","format":"date-time"}},"required":["orderId","customerId","amount","timestamp"]}}}},"order/completed":{"subscribe":{"operationId":"onOrderCompleted","message":{"payload":{"type":"object","properties":{"orderId":{"type":"string"},"completedAt":{"type":"string","format":"date-time"}},"required":["orderId","completedAt"]}}}}},"components":{"schemas":{"OrderCreatedEvent":{"type":"object"},"OrderCompletedEvent":{"type":"object"}}}}',
    'json', 'AsyncApi', 'Locked',
    'ci-cd-pipeline', true, NOW() - INTERVAL '7 days', 'admin',
    NOW() - INTERVAL '30 days', 'seed-script', NOW() - INTERVAL '7 days', 'seed-script', false
);
