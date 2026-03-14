-- =============================================================================
-- Script 01: Seed de contratos REST (OpenAPI / Swagger)
-- Módulo: Contracts & Interoperability (Módulo 5)
-- Uso: Apenas desenvolvimento/debug — NÃO executar em produção
-- Pré-requisito: Executar 00-reset.sql antes
-- =============================================================================

-- API de Usuários — versão 1.0.0 (OpenAPI 3.1, Draft)
INSERT INTO catalog.ct_contract_versions (
    id, api_asset_id, sem_ver, spec_content, format, protocol, lifecycle_state,
    imported_from, is_locked, created_at, created_by, updated_at, updated_by, is_deleted
) VALUES (
    'c5010001-0000-0000-0000-000000000001'::uuid,
    'c5000001-0000-0000-0000-000000000001'::uuid,
    '1.0.0',
    '{"openapi":"3.1.0","info":{"title":"Users API","version":"1.0.0","description":"Gestão de utilizadores"},"servers":[{"url":"https://api.example.com"}],"paths":{"/users":{"get":{"operationId":"listUsers","summary":"List users","parameters":[{"name":"page","in":"query","required":false,"schema":{"type":"integer"}}]},"post":{"operationId":"createUser","summary":"Create user"}}},"components":{"schemas":{"User":{"type":"object","properties":{"id":{"type":"string"},"name":{"type":"string"},"email":{"type":"string","format":"email"}}}},"securitySchemes":{"bearerAuth":{"type":"http","scheme":"bearer"}}}}',
    'json', 'OpenApi', 'Draft',
    'upload', false,
    NOW() - INTERVAL '30 days', 'seed-script', NOW() - INTERVAL '30 days', 'seed-script', false
);

-- API de Usuários — versão 1.1.0 (OpenAPI 3.1, Approved, mudança aditiva)
INSERT INTO catalog.ct_contract_versions (
    id, api_asset_id, sem_ver, spec_content, format, protocol, lifecycle_state,
    imported_from, is_locked, created_at, created_by, updated_at, updated_by, is_deleted
) VALUES (
    'c5010001-0000-0000-0000-000000000002'::uuid,
    'c5000001-0000-0000-0000-000000000001'::uuid,
    '1.1.0',
    '{"openapi":"3.1.0","info":{"title":"Users API","version":"1.1.0","description":"Gestão de utilizadores"},"servers":[{"url":"https://api.example.com"}],"paths":{"/users":{"get":{"operationId":"listUsers","summary":"List users","parameters":[{"name":"page","in":"query","required":false,"schema":{"type":"integer"}}]},"post":{"operationId":"createUser","summary":"Create user"}},"/users/{id}":{"get":{"operationId":"getUser","summary":"Get user by ID"},"delete":{"operationId":"deleteUser","summary":"Delete user"}}},"components":{"schemas":{"User":{"type":"object","properties":{"id":{"type":"string"},"name":{"type":"string"},"email":{"type":"string","format":"email"},"role":{"type":"string"}}}},"securitySchemes":{"bearerAuth":{"type":"http","scheme":"bearer"}}}}',
    'json', 'OpenApi', 'Approved',
    'upload', false,
    NOW() - INTERVAL '20 days', 'seed-script', NOW() - INTERVAL '20 days', 'seed-script', false
);

-- API de Usuários — versão 2.0.0 (OpenAPI 3.1, InReview, breaking change)
INSERT INTO catalog.ct_contract_versions (
    id, api_asset_id, sem_ver, spec_content, format, protocol, lifecycle_state,
    imported_from, is_locked, created_at, created_by, updated_at, updated_by, is_deleted
) VALUES (
    'c5010001-0000-0000-0000-000000000003'::uuid,
    'c5000001-0000-0000-0000-000000000001'::uuid,
    '2.0.0',
    '{"openapi":"3.1.0","info":{"title":"Users API","version":"2.0.0","description":"Gestão de utilizadores v2"},"servers":[{"url":"https://api.example.com/v2"}],"paths":{"/users":{"get":{"operationId":"listUsers","summary":"List users","parameters":[{"name":"page","in":"query","required":false,"schema":{"type":"integer"}},{"name":"filter","in":"query","required":true,"schema":{"type":"string"}}]}}},"components":{"schemas":{"User":{"type":"object","properties":{"id":{"type":"string"},"fullName":{"type":"string"},"emailAddress":{"type":"string","format":"email"}}}},"securitySchemes":{"bearerAuth":{"type":"http","scheme":"bearer"}}}}',
    'json', 'OpenApi', 'InReview',
    'upload', false,
    NOW() - INTERVAL '5 days', 'seed-script', NOW() - INTERVAL '5 days', 'seed-script', false
);

-- API de Pedidos — versão 1.0.0 (Swagger 2.0, Locked)
INSERT INTO catalog.ct_contract_versions (
    id, api_asset_id, sem_ver, spec_content, format, protocol, lifecycle_state,
    imported_from, is_locked, locked_at, locked_by,
    created_at, created_by, updated_at, updated_by, is_deleted
) VALUES (
    'c5010001-0000-0000-0000-000000000004'::uuid,
    'c5000001-0000-0000-0000-000000000002'::uuid,
    '1.0.0',
    '{"swagger":"2.0","info":{"title":"Orders API","version":"1.0.0"},"paths":{"/orders":{"get":{"operationId":"listOrders","summary":"List orders"},"post":{"operationId":"createOrder","summary":"Create order"}},"/orders/{id}":{"get":{"operationId":"getOrder","summary":"Get order"}}},"definitions":{"Order":{"type":"object","properties":{"id":{"type":"string"},"amount":{"type":"number"},"status":{"type":"string"}}}},"securityDefinitions":{"apiKey":{"type":"apiKey","name":"X-API-Key","in":"header"}}}',
    'json', 'Swagger', 'Locked',
    'ci-cd-pipeline', true, NOW() - INTERVAL '15 days', 'admin',
    NOW() - INTERVAL '25 days', 'seed-script', NOW() - INTERVAL '15 days', 'seed-script', false
);
