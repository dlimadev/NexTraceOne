-- ============================================================================
-- NexTraceOne — Identity & Access — Eventos de Segurança
-- Cria amostra representativa de eventos de auditoria de segurança cobrindo:
--   - Autenticação (sucesso e falha)
--   - Gestão de roles e sessões
--   - Acessos privilegiados (break glass, JIT)
--   - Login federado (OIDC)
--   - Alterações de credenciais
-- O campo Metadata é JSON com contexto adicional específico de cada evento.
-- O RiskScore vai de 0.0 (informativo) a 1.0 (risco crítico).
-- ============================================================================

INSERT INTO "SecurityEvents" ("Id", "TenantId", "UserId", "SessionId", "EventType", "Description", "RiskScore", "IpAddress", "UserAgent", "Metadata", "OccurredAt")
VALUES
    -- 1. Login local com sucesso — admin entra no portal ACME
    (
        'se100000-0000-0000-0000-000000000001',
        'a1000000-0000-0000-0000-000000000001',
        'u1000000-0000-0000-0000-000000000001',
        's1000000-0000-0000-0000-000000000001',
        'auth.login.success',
        'Local authentication succeeded for admin@acme-corp.test',
        0.0,
        '10.0.1.100',
        'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/122.0.0.0',
        '{"method": "local", "mfa": false}',
        '2025-03-10T08:00:00Z'
    ),

    -- 2. Login falhado — tentativa com credenciais inválidas
    (
        'se100000-0000-0000-0000-000000000002',
        'a1000000-0000-0000-0000-000000000001',
        'u1000000-0000-0000-0000-000000000003',
        NULL,
        'auth.login.failure',
        'Authentication failed for dev@acme-corp.test — invalid password (attempt 1)',
        0.3,
        '10.0.2.200',
        'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 Chrome/122.0.0.0',
        '{"method": "local", "reason": "invalid_password", "attempt": 1}',
        '2025-03-09T13:55:00Z'
    ),

    -- 3. Login local com sucesso após falha — dev entra na segunda tentativa
    (
        'se100000-0000-0000-0000-000000000003',
        'a1000000-0000-0000-0000-000000000001',
        'u1000000-0000-0000-0000-000000000003',
        's1000000-0000-0000-0000-000000000003',
        'auth.login.success',
        'Local authentication succeeded for dev@acme-corp.test',
        0.0,
        '10.0.2.200',
        'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 Chrome/122.0.0.0',
        '{"method": "local", "mfa": false, "previous_failures": 1}',
        '2025-03-09T14:00:00Z'
    ),

    -- 4. Login OIDC — utilizador federado autentica-se via provider externo
    (
        'se100000-0000-0000-0000-000000000004',
        'a1000000-0000-0000-0000-000000000001',
        'u1000000-0000-0000-0000-000000000009',
        NULL,
        'auth.oidc.login',
        'OIDC authentication succeeded for oidc@acme-corp.test via corporate IdP',
        0.0,
        '10.0.3.50',
        'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edge/122.0.0.0',
        '{"method": "oidc", "provider": "corporate-idp", "subject": "oidc-sub-12345"}',
        '2025-03-10T06:00:00Z'
    ),

    -- 5. Alteração de role — admin atribuiu SecurityReview ao security@acme-corp.test
    (
        'se100000-0000-0000-0000-000000000005',
        'a1000000-0000-0000-0000-000000000001',
        'u1000000-0000-0000-0000-000000000001',
        's1000000-0000-0000-0000-000000000001',
        'identity.role.assigned',
        'Role SecurityReview assigned to security@acme-corp.test by admin@acme-corp.test',
        0.2,
        '10.0.1.100',
        'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/122.0.0.0',
        '{"target_user": "u1000000-0000-0000-0000-000000000005", "role": "SecurityReview", "action": "assign"}',
        '2025-01-05T10:00:00Z'
    ),

    -- 6. Activação de break glass — acesso de emergência (risco alto)
    (
        'se100000-0000-0000-0000-000000000006',
        'a1000000-0000-0000-0000-000000000001',
        'u1000000-0000-0000-0000-000000000003',
        's1000000-0000-0000-0000-000000000003',
        'access.breakglass.activated',
        'Break glass access activated by dev@acme-corp.test for incident INC-2025-0042',
        0.9,
        '10.0.2.200',
        'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 Chrome/122.0.0.0',
        '{"request_id": "bg100000-0000-0000-0000-000000000001", "incident": "INC-2025-0042", "duration_hours": 4}',
        '2025-03-10T14:30:00Z'
    ),

    -- 7. Break glass revogado pelo admin — encerramento prematuro controlado
    (
        'se100000-0000-0000-0000-000000000007',
        'a1000000-0000-0000-0000-000000000001',
        'u1000000-0000-0000-0000-000000000001',
        's1000000-0000-0000-0000-000000000001',
        'access.breakglass.revoked',
        'Break glass access revoked for techlead@acme-corp.test by admin@acme-corp.test',
        0.5,
        '10.0.1.100',
        'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/122.0.0.0',
        '{"request_id": "bg100000-0000-0000-0000-000000000003", "target_user": "u1000000-0000-0000-0000-000000000002", "reason": "investigation_complete"}',
        '2025-03-05T17:30:00Z'
    ),

    -- 8. Sessão revogada — admin revogou sessão suspeita do viewer
    (
        'se100000-0000-0000-0000-000000000008',
        'a1000000-0000-0000-0000-000000000001',
        'u1000000-0000-0000-0000-000000000001',
        's1000000-0000-0000-0000-000000000001',
        'identity.session.revoked',
        'Session revoked for viewer@acme-corp.test by admin@acme-corp.test — suspicious IP origin',
        0.6,
        '10.0.1.100',
        'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/122.0.0.0',
        '{"target_session": "s1000000-0000-0000-0000-000000000005", "target_user": "u1000000-0000-0000-0000-000000000004", "reason": "suspicious_ip", "suspicious_ip": "203.0.113.42"}',
        '2025-03-07T11:30:00Z'
    ),

    -- 9. Pedido JIT rejeitado — justificativa insuficiente
    (
        'se100000-0000-0000-0000-000000000009',
        'a2000000-0000-0000-0000-000000000002',
        'u1000000-0000-0000-0000-000000000008',
        NULL,
        'access.jit.rejected',
        'JIT access request rejected for devonly@globex-inc.test — insufficient justification',
        0.2,
        '10.0.4.80',
        'Mozilla/5.0 (Macintosh; Intel Mac OS X 14_3) AppleWebKit/605.1.15 Safari/17.2',
        '{"request_id": "jt100000-0000-0000-0000-000000000003", "permission": "identity:sessions:revoke", "reason": "insufficient_justification"}',
        '2025-03-06T13:00:00Z'
    ),

    -- 10. Alteração de password — utilizador alterou a própria senha
    (
        'se100000-0000-0000-0000-000000000010',
        'a1000000-0000-0000-0000-000000000001',
        'u1000000-0000-0000-0000-000000000003',
        's1000000-0000-0000-0000-000000000003',
        'identity.password.changed',
        'Password changed by dev@acme-corp.test',
        0.1,
        '10.0.2.200',
        'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 Chrome/122.0.0.0',
        '{"self_service": true}',
        '2025-03-09T15:00:00Z'
    )
ON CONFLICT ("Id") DO NOTHING;
