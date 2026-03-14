-- ============================================================================
-- SEED: Quotas de uso para o módulo CommercialGovernance
-- ============================================================================
-- Cria quotas de uso com consumo variado para testar:
-- - Thresholds de aviso (>80%)
-- - Limites próximos do hard limit
-- - Quotas normais
-- - Quotas zeradas
--
-- EnforcementLevel: 0=NeverBreak, 1=Soft, 2=Hard, 3=Warn
-- ATENÇÃO: Usar APENAS em ambiente de desenvolvimento/debug.
-- ============================================================================

-- Licença 1: Banco Nacional — Enterprise (limites altos, uso moderado)
INSERT INTO licensing_quotas ("Id", "LicenseId", "MetricCode", "Limit", "CurrentUsage", "EnforcementLevel", "LastUpdatedAt") VALUES
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001', 'api.count', 500, 120, 1, NOW()),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001', 'environment.count', 20, 8, 1, NOW()),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001', 'user.count', 100, 45, 1, NOW())
ON CONFLICT DO NOTHING;

-- Licença 2: Seguros Confiança — Professional (limites médios, uso alto - perto do threshold)
INSERT INTO licensing_quotas ("Id", "LicenseId", "MetricCode", "Limit", "CurrentUsage", "EnforcementLevel", "LastUpdatedAt") VALUES
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000002', 'api.count', 100, 85, 2, NOW()),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000002', 'environment.count', 5, 4, 2, NOW()),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000002', 'user.count', 25, 22, 1, NOW())
ON CONFLICT DO NOTHING;

-- Licença 3: Ministério da Defesa — On-Premise Enterprise
INSERT INTO licensing_quotas ("Id", "LicenseId", "MetricCode", "Limit", "CurrentUsage", "EnforcementLevel", "LastUpdatedAt") VALUES
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000003', 'api.count', 500, 200, 1, NOW()),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000003', 'environment.count', 20, 3, 1, NOW()),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000003', 'user.count', 100, 30, 1, NOW())
ON CONFLICT DO NOTHING;

-- Licença 4: TelecomBR — Self-Hosted Professional
INSERT INTO licensing_quotas ("Id", "LicenseId", "MetricCode", "Limit", "CurrentUsage", "EnforcementLevel", "LastUpdatedAt") VALUES
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000004', 'api.count', 100, 50, 2, NOW()),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000004', 'environment.count', 5, 3, 2, NOW()),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000004', 'user.count', 25, 10, 1, NOW())
ON CONFLICT DO NOTHING;

-- Licença 5: Startup Trial (limites baixos do trial)
INSERT INTO licensing_quotas ("Id", "LicenseId", "MetricCode", "Limit", "CurrentUsage", "EnforcementLevel", "LastUpdatedAt") VALUES
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000005', 'api.count', 25, 15, 2, NOW()),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000005', 'environment.count', 2, 1, 2, NOW()),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000005', 'user.count', 5, 3, 2, NOW())
ON CONFLICT DO NOTHING;

-- Licença 10: Community (limites mínimos)
INSERT INTO licensing_quotas ("Id", "LicenseId", "MetricCode", "Limit", "CurrentUsage", "EnforcementLevel", "LastUpdatedAt") VALUES
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000010', 'api.count', 10, 8, 2, NOW()),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000010', 'environment.count', 1, 1, 2, NOW()),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000010', 'user.count', 3, 2, 2, NOW())
ON CONFLICT DO NOTHING;

SELECT count(*) AS quotas_seeded FROM licensing_quotas;
