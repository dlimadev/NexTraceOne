-- ============================================================================
-- NexTraceOne — Developer Portal — Pesquisas salvas de teste
-- Cria 4 pesquisas salvas por diferentes utilizadores, com critérios e filtros
-- variados. Simula reutilização de pesquisas frequentes no catálogo de APIs.
-- ============================================================================

INSERT INTO dp_saved_searches (
    "Id", "UserId", "Name", "SearchQuery", "Filters", "CreatedAt", "LastUsedAt"
)
VALUES
    -- ── Pesquisa 1: Developer guarda pesquisa de APIs de pagamento públicas ──
    (
        'd5000000-0000-0000-0000-000000000001',
        'u1000000-0000-0000-0000-000000000003',
        'APIs de Pagamento',
        'payments',
        '{"domain":"Payments","visibility":"Public","status":"active"}',
        '2025-02-15T10:00:00Z',
        '2025-03-10T09:00:00Z'
    ),

    -- ── Pesquisa 2: TechLead guarda pesquisa de APIs internas do domínio Identity
    (
        'd5000000-0000-0000-0000-000000000002',
        'u1000000-0000-0000-0000-000000000002',
        'APIs Internas de Identidade',
        'auth oauth identity',
        '{"domain":"Identity","visibility":"Internal"}',
        '2025-02-20T14:30:00Z',
        '2025-03-10T11:00:00Z'
    ),

    -- ── Pesquisa 3: Utilizador multi-tenant guarda pesquisa por tag de versão v3+
    (
        'd5000000-0000-0000-0000-000000000003',
        'u1000000-0000-0000-0000-000000000007',
        'APIs v3 ou superior',
        'version:>=3.0.0',
        '{"minVersion":"3.0.0","tags":["stable","production-ready"]}',
        '2025-03-01T08:00:00Z',
        '2025-03-10T11:15:00Z'
    ),

    -- ── Pesquisa 4: Admin guarda pesquisa de APIs descomissionadas para auditoria
    (
        'd5000000-0000-0000-0000-000000000004',
        'u1000000-0000-0000-0000-000000000001',
        'APIs Descomissionadas',
        'decommissioned deprecated',
        '{"status":"decommissioned","includeInactive":true}',
        '2025-03-05T16:00:00Z',
        '2025-03-10T14:30:00Z'
    )
ON CONFLICT ("Id") DO NOTHING;
