-- ============================================================================
-- SEED: Licenças de teste para o módulo CommercialGovernance
-- ============================================================================
-- Cobre cenários:
-- 1. Tenant SaaS ativo (Enterprise, Professional)
-- 2. Tenant On-Premise ativo
-- 3. Tenant Self-Hosted ativo
-- 4. Tenant em Trial
-- 5. Tenant com Trial expirado
-- 6. Tenant com grace period
-- 7. Tenant com licença revogada
-- 8. Tenant com limites próximos do threshold
--
-- ATENÇÃO: Usar APENAS em ambiente de desenvolvimento/debug.
-- IDs gerados manualmente para permitir referências cruzadas entre scripts.
-- ============================================================================

-- 1. Tenant SaaS — Enterprise — Ativo, 365 dias
INSERT INTO licensing_licenses (
    "Id", "LicenseKey", "CustomerName", "IssuedAt", "ExpiresAt",
    "MaxActivations", "IsActive", "Type", "Edition", "GracePeriodDays",
    "TrialConverted", "TrialConvertedAt", "TrialExtensionCount",
    "DeploymentModel", "ActivationMode", "CommercialModel", "MeteringMode", "Status"
) VALUES (
    'a0000000-0000-0000-0000-000000000001',
    'LIC-SAAS-ENT-001',
    'Banco Nacional SA',
    NOW() - INTERVAL '30 days',
    NOW() + INTERVAL '335 days',
    10, true, 2, 2, 30,
    false, NULL, 0,
    0, 0, 1, 0, 0
) ON CONFLICT DO NOTHING;

-- 2. Tenant SaaS — Professional — Ativo, 180 dias
INSERT INTO licensing_licenses (
    "Id", "LicenseKey", "CustomerName", "IssuedAt", "ExpiresAt",
    "MaxActivations", "IsActive", "Type", "Edition", "GracePeriodDays",
    "TrialConverted", "TrialConvertedAt", "TrialExtensionCount",
    "DeploymentModel", "ActivationMode", "CommercialModel", "MeteringMode", "Status"
) VALUES (
    'a0000000-0000-0000-0000-000000000002',
    'LIC-SAAS-PRO-002',
    'Seguros Confiança Ltda',
    NOW() - INTERVAL '60 days',
    NOW() + INTERVAL '120 days',
    5, true, 1, 1, 15,
    false, NULL, 0,
    0, 0, 1, 0, 0
) ON CONFLICT DO NOTHING;

-- 3. Tenant On-Premise — Enterprise — Ativo
INSERT INTO licensing_licenses (
    "Id", "LicenseKey", "CustomerName", "IssuedAt", "ExpiresAt",
    "MaxActivations", "IsActive", "Type", "Edition", "GracePeriodDays",
    "TrialConverted", "TrialConvertedAt", "TrialExtensionCount",
    "DeploymentModel", "ActivationMode", "CommercialModel", "MeteringMode", "Status"
) VALUES (
    'a0000000-0000-0000-0000-000000000003',
    'LIC-ONPREM-ENT-003',
    'Ministério da Defesa',
    NOW() - INTERVAL '90 days',
    NOW() + INTERVAL '640 days',
    3, true, 2, 2, 30,
    false, NULL, 0,
    2, 1, 0, 2, 0
) ON CONFLICT DO NOTHING;

-- 4. Tenant Self-Hosted — Professional — Ativo
INSERT INTO licensing_licenses (
    "Id", "LicenseKey", "CustomerName", "IssuedAt", "ExpiresAt",
    "MaxActivations", "IsActive", "Type", "Edition", "GracePeriodDays",
    "TrialConverted", "TrialConvertedAt", "TrialExtensionCount",
    "DeploymentModel", "ActivationMode", "CommercialModel", "MeteringMode", "Status"
) VALUES (
    'a0000000-0000-0000-0000-000000000004',
    'LIC-SELFHOST-PRO-004',
    'TelecomBR SA',
    NOW() - INTERVAL '45 days',
    NOW() + INTERVAL '320 days',
    2, true, 1, 1, 15,
    false, NULL, 0,
    1, 2, 1, 1, 0
) ON CONFLICT DO NOTHING;

-- 5. Tenant em Trial — SaaS — 30 dias
INSERT INTO licensing_licenses (
    "Id", "LicenseKey", "CustomerName", "IssuedAt", "ExpiresAt",
    "MaxActivations", "IsActive", "Type", "Edition", "GracePeriodDays",
    "TrialConverted", "TrialConvertedAt", "TrialExtensionCount",
    "DeploymentModel", "ActivationMode", "CommercialModel", "MeteringMode", "Status"
) VALUES (
    'a0000000-0000-0000-0000-000000000005',
    'TRIAL-STARTUP-005',
    'Startup Inovadora Ltda',
    NOW() - INTERVAL '10 days',
    NOW() + INTERVAL '20 days',
    1, true, 0, 1, 7,
    false, NULL, 0,
    0, 0, 3, 0, 0
) ON CONFLICT DO NOTHING;

-- 6. Tenant com Trial expirado
INSERT INTO licensing_licenses (
    "Id", "LicenseKey", "CustomerName", "IssuedAt", "ExpiresAt",
    "MaxActivations", "IsActive", "Type", "Edition", "GracePeriodDays",
    "TrialConverted", "TrialConvertedAt", "TrialExtensionCount",
    "DeploymentModel", "ActivationMode", "CommercialModel", "MeteringMode", "Status"
) VALUES (
    'a0000000-0000-0000-0000-000000000006',
    'TRIAL-EXPIRED-006',
    'Tech Antiga SA',
    NOW() - INTERVAL '45 days',
    NOW() - INTERVAL '15 days',
    1, true, 0, 1, 7,
    false, NULL, 0,
    0, 0, 3, 0, 2
) ON CONFLICT DO NOTHING;

-- 7. Tenant em Grace Period — SaaS
INSERT INTO licensing_licenses (
    "Id", "LicenseKey", "CustomerName", "IssuedAt", "ExpiresAt",
    "MaxActivations", "IsActive", "Type", "Edition", "GracePeriodDays",
    "TrialConverted", "TrialConvertedAt", "TrialExtensionCount",
    "DeploymentModel", "ActivationMode", "CommercialModel", "MeteringMode", "Status"
) VALUES (
    'a0000000-0000-0000-0000-000000000007',
    'LIC-GRACE-007',
    'Empresa em Renovação SA',
    NOW() - INTERVAL '370 days',
    NOW() - INTERVAL '5 days',
    5, true, 1, 1, 15,
    false, NULL, 0,
    0, 0, 1, 0, 1
) ON CONFLICT DO NOTHING;

-- 8. Tenant com licença revogada
INSERT INTO licensing_licenses (
    "Id", "LicenseKey", "CustomerName", "IssuedAt", "ExpiresAt",
    "MaxActivations", "IsActive", "Type", "Edition", "GracePeriodDays",
    "TrialConverted", "TrialConvertedAt", "TrialExtensionCount",
    "DeploymentModel", "ActivationMode", "CommercialModel", "MeteringMode", "Status"
) VALUES (
    'a0000000-0000-0000-0000-000000000008',
    'LIC-REVOKED-008',
    'Ex-Cliente Corp',
    NOW() - INTERVAL '200 days',
    NOW() + INTERVAL '165 days',
    5, false, 1, 2, 15,
    false, NULL, 0,
    0, 0, 1, 0, 4
) ON CONFLICT DO NOTHING;

-- 9. Tenant com Trial convertido — SaaS
INSERT INTO licensing_licenses (
    "Id", "LicenseKey", "CustomerName", "IssuedAt", "ExpiresAt",
    "MaxActivations", "IsActive", "Type", "Edition", "GracePeriodDays",
    "TrialConverted", "TrialConvertedAt", "TrialExtensionCount",
    "DeploymentModel", "ActivationMode", "CommercialModel", "MeteringMode", "Status"
) VALUES (
    'a0000000-0000-0000-0000-000000000009',
    'LIC-CONVERTED-009',
    'Empresa Convertida SA',
    NOW() - INTERVAL '60 days',
    NOW() + INTERVAL '305 days',
    5, true, 1, 1, 15,
    true, NOW() - INTERVAL '30 days', 0,
    0, 0, 1, 0, 0
) ON CONFLICT DO NOTHING;

-- 10. Tenant SaaS — Community (gratuito)
INSERT INTO licensing_licenses (
    "Id", "LicenseKey", "CustomerName", "IssuedAt", "ExpiresAt",
    "MaxActivations", "IsActive", "Type", "Edition", "GracePeriodDays",
    "TrialConverted", "TrialConvertedAt", "TrialExtensionCount",
    "DeploymentModel", "ActivationMode", "CommercialModel", "MeteringMode", "Status"
) VALUES (
    'a0000000-0000-0000-0000-000000000010',
    'LIC-COMMUNITY-010',
    'Projeto Open Source',
    NOW() - INTERVAL '15 days',
    NOW() + INTERVAL '350 days',
    1, true, 1, 0, 0,
    false, NULL, 0,
    0, 0, 4, 0, 0
) ON CONFLICT DO NOTHING;

SELECT count(*) AS licenses_seeded FROM licensing_licenses;
