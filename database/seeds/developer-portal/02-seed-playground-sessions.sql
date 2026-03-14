-- ============================================================================
-- NexTraceOne — Developer Portal — Sessões do playground de teste
-- Cria 10 sessões de execução sandbox demonstrando chamadas com diferentes
-- métodos HTTP, status de resposta e tempos de execução. Simula utilização
-- realista do playground interativo por vários utilizadores.
-- ============================================================================

INSERT INTO dp_playground_sessions (
    "Id", "ApiAssetId", "ApiName", "UserId",
    "HttpMethod", "RequestPath", "RequestBody", "RequestHeaders",
    "ResponseStatusCode", "ResponseBody", "DurationMs",
    "Environment", "ExecutedAt"
)
VALUES
    -- ── Sessão 1: GET listagem de pagamentos — sucesso rápido ───────────────
    (
        'd2000000-0000-0000-0000-000000000001',
        'e2000000-0000-0000-0000-000000000001',
        'Payments API',
        'u1000000-0000-0000-0000-000000000003',
        'GET',
        '/api/v2/payments?page=1&size=20',
        NULL,
        '{"Authorization":"Bearer test-token-dev","Accept":"application/json"}',
        200,
        '{"items":[{"id":"pay-001","amount":150.00,"currency":"EUR"}],"total":1}',
        87,
        'sandbox',
        '2025-03-10T09:15:00Z'
    ),

    -- ── Sessão 2: POST criar pagamento — sucesso com body completo ──────────
    (
        'd2000000-0000-0000-0000-000000000002',
        'e2000000-0000-0000-0000-000000000001',
        'Payments API',
        'u1000000-0000-0000-0000-000000000003',
        'POST',
        '/api/v2/payments',
        '{"amount":250.00,"currency":"EUR","description":"Teste de integração","merchantId":"merch-001"}',
        '{"Authorization":"Bearer test-token-dev","Content-Type":"application/json"}',
        201,
        '{"id":"pay-002","status":"pending","createdAt":"2025-03-10T09:20:00Z"}',
        234,
        'sandbox',
        '2025-03-10T09:20:00Z'
    ),

    -- ── Sessão 3: GET pagamento por ID — recurso não encontrado ─────────────
    (
        'd2000000-0000-0000-0000-000000000003',
        'e2000000-0000-0000-0000-000000000001',
        'Payments API',
        'u1000000-0000-0000-0000-000000000003',
        'GET',
        '/api/v2/payments/pay-nonexistent',
        NULL,
        '{"Authorization":"Bearer test-token-dev","Accept":"application/json"}',
        404,
        '{"code":"payments.not_found","messageKey":"payments.not_found"}',
        45,
        'sandbox',
        '2025-03-10T09:25:00Z'
    ),

    -- ── Sessão 4: POST reembolso — validação falhou (422) ───────────────────
    (
        'd2000000-0000-0000-0000-000000000004',
        'e2000000-0000-0000-0000-000000000002',
        'Refunds API',
        'u1000000-0000-0000-0000-000000000002',
        'POST',
        '/api/v1/refunds',
        '{"paymentId":"pay-001","amount":-50.00}',
        '{"Authorization":"Bearer test-token-lead","Content-Type":"application/json"}',
        422,
        '{"code":"refunds.invalid_amount","messageKey":"refunds.amount_must_be_positive"}',
        32,
        'sandbox',
        '2025-03-10T10:00:00Z'
    ),

    -- ── Sessão 5: GET liquidações — resposta lenta simulada ─────────────────
    (
        'd2000000-0000-0000-0000-000000000005',
        'e2000000-0000-0000-0000-000000000004',
        'Settlements API',
        'u1000000-0000-0000-0000-000000000002',
        'GET',
        '/api/v1/settlements?from=2025-01-01&to=2025-03-01',
        NULL,
        '{"Authorization":"Bearer test-token-lead","Accept":"application/json"}',
        200,
        '{"items":[{"id":"stl-001","amount":12500.00},{"id":"stl-002","amount":8730.50}],"total":2}',
        1520,
        'sandbox',
        '2025-03-10T10:30:00Z'
    ),

    -- ── Sessão 6: PUT atualizar pagamento — sucesso ─────────────────────────
    (
        'd2000000-0000-0000-0000-000000000006',
        'e2000000-0000-0000-0000-000000000001',
        'Payments API',
        'u1000000-0000-0000-0000-000000000007',
        'PUT',
        '/api/v2/payments/pay-002',
        '{"description":"Pagamento atualizado via playground","metadata":{"source":"portal"}}',
        '{"Authorization":"Bearer test-token-multi","Content-Type":"application/json"}',
        200,
        '{"id":"pay-002","status":"pending","description":"Pagamento atualizado via playground"}',
        156,
        'sandbox',
        '2025-03-10T11:00:00Z'
    ),

    -- ── Sessão 7: DELETE cancelar pagamento — sem autorização (401) ─────────
    (
        'd2000000-0000-0000-0000-000000000007',
        'e2000000-0000-0000-0000-000000000001',
        'Payments API',
        'u1000000-0000-0000-0000-000000000004',
        'DELETE',
        '/api/v2/payments/pay-002',
        NULL,
        '{"Accept":"application/json"}',
        401,
        '{"code":"auth.unauthorized","messageKey":"auth.token_required"}',
        18,
        'sandbox',
        '2025-03-10T11:15:00Z'
    ),

    -- ── Sessão 8: GET reconciliação — timeout simulado (504) ────────────────
    (
        'd2000000-0000-0000-0000-000000000008',
        'e2000000-0000-0000-0000-000000000005',
        'Reconciliation API',
        'u1000000-0000-0000-0000-000000000001',
        'GET',
        '/api/v1/reconciliation/batch/2025-02',
        NULL,
        '{"Authorization":"Bearer test-token-admin","Accept":"application/json"}',
        504,
        '{"code":"gateway.timeout","messageKey":"gateway.upstream_timeout"}',
        30000,
        'sandbox',
        '2025-03-10T14:00:00Z'
    ),

    -- ── Sessão 9: PATCH estado do processamento — sucesso (200) ─────────────
    (
        'd2000000-0000-0000-0000-000000000009',
        'e2000000-0000-0000-0000-000000000003',
        'Processing API',
        'u1000000-0000-0000-0000-000000000008',
        'PATCH',
        '/api/v3/processing/proc-001/status',
        '{"status":"completed"}',
        '{"Authorization":"Bearer test-token-devonly","Content-Type":"application/json"}',
        200,
        '{"id":"proc-001","status":"completed","updatedAt":"2025-03-10T15:00:00Z"}',
        98,
        'sandbox',
        '2025-03-10T15:00:00Z'
    ),

    -- ── Sessão 10: POST pagamento — erro interno do servidor (500) ──────────
    (
        'd2000000-0000-0000-0000-00000000000a',
        'e2000000-0000-0000-0000-000000000001',
        'Payments API',
        'u1000000-0000-0000-0000-000000000003',
        'POST',
        '/api/v2/payments',
        '{"amount":999999999.99,"currency":"XXX","description":"Stress test"}',
        '{"Authorization":"Bearer test-token-dev","Content-Type":"application/json"}',
        500,
        '{"code":"internal.error","messageKey":"internal.unexpected_error","correlationId":"corr-sandbox-001"}',
        5200,
        'sandbox',
        '2025-03-10T16:30:00Z'
    )
ON CONFLICT ("Id") DO NOTHING;
