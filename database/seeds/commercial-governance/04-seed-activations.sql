-- ============================================================================
-- SEED: Ativações de licenças para o módulo CommercialGovernance
-- ============================================================================
-- Cria registros de ativação de hardware para licenças selecionadas.
-- Simula cenários de:
-- - Primeira ativação
-- - Múltiplas ativações (enterprise)
-- - Ativação de trial
--
-- ATENÇÃO: Usar APENAS em ambiente de desenvolvimento/debug.
-- ============================================================================

-- Ativação da licença do Banco Nacional (Enterprise, múltiplas ativações)
INSERT INTO licensing_activations ("Id", "LicenseId", "HardwareFingerprint", "ActivatedBy", "ActivatedAt", "IsActive") VALUES
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001', 'hw-banco-prod-001', 'admin@banconacional.com', NOW() - INTERVAL '25 days', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001', 'hw-banco-staging-002', 'admin@banconacional.com', NOW() - INTERVAL '20 days', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001', 'hw-banco-dr-003', 'admin@banconacional.com', NOW() - INTERVAL '15 days', true)
ON CONFLICT DO NOTHING;

-- Ativação da licença de Seguros Confiança (Professional, 1 ativação)
INSERT INTO licensing_activations ("Id", "LicenseId", "HardwareFingerprint", "ActivatedBy", "ActivatedAt", "IsActive") VALUES
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000002', 'hw-seguros-prod-001', 'ops@segurosconfianca.com', NOW() - INTERVAL '55 days', true)
ON CONFLICT DO NOTHING;

-- Ativação da licença On-Premise do Ministério
INSERT INTO licensing_activations ("Id", "LicenseId", "HardwareFingerprint", "ActivatedBy", "ActivatedAt", "IsActive") VALUES
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000003', 'hw-gov-datacenter-001', 'sysadmin@defesa.gov.br', NOW() - INTERVAL '85 days', true)
ON CONFLICT DO NOTHING;

-- Ativação da licença Self-Hosted da TelecomBR
INSERT INTO licensing_activations ("Id", "LicenseId", "HardwareFingerprint", "ActivatedBy", "ActivatedAt", "IsActive") VALUES
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000004', 'hw-telecom-k8s-001', 'devops@telecombr.com.br', NOW() - INTERVAL '40 days', true)
ON CONFLICT DO NOTHING;

-- Ativação do Trial da Startup
INSERT INTO licensing_activations ("Id", "LicenseId", "HardwareFingerprint", "ActivatedBy", "ActivatedAt", "IsActive") VALUES
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000005', 'hw-startup-local-001', 'dev@startupinov.com', NOW() - INTERVAL '8 days', true)
ON CONFLICT DO NOTHING;

-- Hardware bindings correspondentes
INSERT INTO licensing_hardware_bindings ("Id", "LicenseId", "Fingerprint", "BoundAt", "LastValidatedAt") VALUES
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001', 'hw-banco-prod-001', NOW() - INTERVAL '25 days', NOW() - INTERVAL '1 day'),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000002', 'hw-seguros-prod-001', NOW() - INTERVAL '55 days', NOW() - INTERVAL '2 days'),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000003', 'hw-gov-datacenter-001', NOW() - INTERVAL '85 days', NOW() - INTERVAL '3 days'),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000004', 'hw-telecom-k8s-001', NOW() - INTERVAL '40 days', NOW() - INTERVAL '1 day'),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000005', 'hw-startup-local-001', NOW() - INTERVAL '8 days', NOW() - INTERVAL '1 day')
ON CONFLICT DO NOTHING;

SELECT count(*) AS activations_seeded FROM licensing_activations;
