-- ============================================================================
-- NexTraceOne — Identity & Access — Sessões de teste
-- Cria sessões simuladas em diferentes estados do ciclo de vida:
--   - Activas (admin com 2 dispositivos, dev com 1)
--   - Expirada (caducou naturalmente sem renovação)
--   - Revogada (terminada manualmente pelo admin ou pelo próprio utilizador)
-- Os RefreshTokenHash são hashes SHA-256 fictícios — não representam tokens reais.
-- ============================================================================

INSERT INTO "Sessions" ("Id", "UserId", "RefreshTokenHash", "IpAddress", "UserAgent", "IsActive", "CreatedAt", "ExpiresAt", "RevokedAt")
VALUES
    -- ── admin@acme-corp.test — sessão activa 1 (desktop do escritório) ──────
    (
        's1000000-0000-0000-0000-000000000001',
        'u1000000-0000-0000-0000-000000000001',
        'sha256$a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f60001',
        '10.0.1.100',
        'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/122.0.0.0',
        true,
        '2025-03-10T08:00:00Z',
        '2025-03-11T08:00:00Z',
        NULL
    ),
    -- ── admin@acme-corp.test — sessão activa 2 (telemóvel) ──────────────────
    (
        's1000000-0000-0000-0000-000000000002',
        'u1000000-0000-0000-0000-000000000001',
        'sha256$a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f60002',
        '192.168.1.50',
        'Mozilla/5.0 (iPhone; CPU iPhone OS 17_3 like Mac OS X) AppleWebKit/605.1.15 Mobile/15E148',
        true,
        '2025-03-10T09:30:00Z',
        '2025-03-11T09:30:00Z',
        NULL
    ),
    -- ── dev@acme-corp.test — sessão activa (laptop de desenvolvimento) ──────
    (
        's1000000-0000-0000-0000-000000000003',
        'u1000000-0000-0000-0000-000000000003',
        'sha256$b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f60003',
        '10.0.2.200',
        'Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 Chrome/122.0.0.0',
        true,
        '2025-03-09T14:00:00Z',
        '2025-03-10T14:00:00Z',
        NULL
    ),
    -- ── techlead@acme-corp.test — sessão expirada (não renovou o token) ─────
    (
        's1000000-0000-0000-0000-000000000004',
        'u1000000-0000-0000-0000-000000000002',
        'sha256$c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a10004',
        '10.0.1.150',
        'Mozilla/5.0 (Macintosh; Intel Mac OS X 14_3) AppleWebKit/605.1.15 Safari/17.2',
        false,
        '2025-03-08T09:00:00Z',
        '2025-03-09T09:00:00Z',
        NULL
    ),
    -- ── viewer@acme-corp.test — sessão revogada pelo admin por suspeita ─────
    (
        's1000000-0000-0000-0000-000000000005',
        'u1000000-0000-0000-0000-000000000004',
        'sha256$d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b20005',
        '203.0.113.42',
        'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Gecko/20100101 Firefox/123.0',
        false,
        '2025-03-07T10:00:00Z',
        '2025-03-08T10:00:00Z',
        '2025-03-07T11:30:00Z'
    )
ON CONFLICT ("Id") DO NOTHING;
