-- =============================================================================
-- Seed: Telemetry Consent — Registros de consentimento de telemetria por licença
-- =============================================================================
-- ⚠️ APENAS para ambiente de desenvolvimento/debug.
-- NÃO executar em produção.
--
-- Pré-requisito: 01-seed-licenses.sql deve ter sido executado antes.
-- =============================================================================

-- Consentimento total — Banco Nacional SA (SaaS Enterprise)
INSERT INTO licensing_telemetry_consents (
    "Id", "LicenseId", "Status", "UpdatedAt", "UpdatedBy", "Reason",
    "AllowUsageMetrics", "AllowPerformanceData", "AllowErrorDiagnostics",
    "TenantId", "CreatedAt", "ModifiedAt"
)
SELECT
    'a0a0a0a0-0001-0001-0001-000000000001'::uuid,
    l."Id",
    1, -- Granted
    NOW(),
    'admin@banconacional.com',
    'Full consent granted during onboarding',
    true, true, true,
    l."TenantId", NOW(), NOW()
FROM licensing_licenses l WHERE l."LicenseKey" = 'LIC-SAAS-ENT-001'
ON CONFLICT DO NOTHING;

-- Consentimento parcial — Seguros Confiança (SaaS Professional)
INSERT INTO licensing_telemetry_consents (
    "Id", "LicenseId", "Status", "UpdatedAt", "UpdatedBy", "Reason",
    "AllowUsageMetrics", "AllowPerformanceData", "AllowErrorDiagnostics",
    "TenantId", "CreatedAt", "ModifiedAt"
)
SELECT
    'a0a0a0a0-0002-0002-0002-000000000002'::uuid,
    l."Id",
    3, -- Partial
    NOW(),
    'dpo@segurosconfianca.com',
    'Only aggregated metrics — no PII per DPO policy',
    true, false, false,
    l."TenantId", NOW(), NOW()
FROM licensing_licenses l WHERE l."LicenseKey" = 'LIC-SAAS-PRO-002'
ON CONFLICT DO NOTHING;

-- Consentimento negado — Ministério da Defesa (On-Premise)
INSERT INTO licensing_telemetry_consents (
    "Id", "LicenseId", "Status", "UpdatedAt", "UpdatedBy", "Reason",
    "AllowUsageMetrics", "AllowPerformanceData", "AllowErrorDiagnostics",
    "TenantId", "CreatedAt", "ModifiedAt"
)
SELECT
    'a0a0a0a0-0003-0003-0003-000000000003'::uuid,
    l."Id",
    2, -- Denied
    NOW(),
    'ciso@defesa.gov',
    'Air-gapped environment — no external data collection allowed',
    false, false, false,
    l."TenantId", NOW(), NOW()
FROM licensing_licenses l WHERE l."LicenseKey" = 'LIC-ONPREM-ENT-003'
ON CONFLICT DO NOTHING;

-- Consentimento total — TelecomBR SA (Self-Hosted)
INSERT INTO licensing_telemetry_consents (
    "Id", "LicenseId", "Status", "UpdatedAt", "UpdatedBy", "Reason",
    "AllowUsageMetrics", "AllowPerformanceData", "AllowErrorDiagnostics",
    "TenantId", "CreatedAt", "ModifiedAt"
)
SELECT
    'a0a0a0a0-0004-0004-0004-000000000004'::uuid,
    l."Id",
    1, -- Granted
    NOW(),
    'infra@telecombr.com',
    'Full consent per internal IT governance',
    true, true, true,
    l."TenantId", NOW(), NOW()
FROM licensing_licenses l WHERE l."LicenseKey" = 'LIC-SELFHOST-PRO-004'
ON CONFLICT DO NOTHING;

-- Não solicitado — Startup Inovadora (Trial ativo)
-- Sem registro = status NotRequested (tratado pelo backend)

-- Consentimento parcial — Empresa em Renovação (Grace period)
INSERT INTO licensing_telemetry_consents (
    "Id", "LicenseId", "Status", "UpdatedAt", "UpdatedBy", "Reason",
    "AllowUsageMetrics", "AllowPerformanceData", "AllowErrorDiagnostics",
    "TenantId", "CreatedAt", "ModifiedAt"
)
SELECT
    'a0a0a0a0-0007-0007-0007-000000000007'::uuid,
    l."Id",
    3, -- Partial
    NOW(),
    'admin@renovacao.com',
    'Performance and error data allowed for support',
    false, true, true,
    l."TenantId", NOW(), NOW()
FROM licensing_licenses l WHERE l."LicenseKey" = 'LIC-GRACE-007'
ON CONFLICT DO NOTHING;
