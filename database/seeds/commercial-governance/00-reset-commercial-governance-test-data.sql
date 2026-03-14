-- ============================================================================
-- RESET: Limpa toda massa de teste do módulo CommercialGovernance (Licensing)
-- ============================================================================
-- ATENÇÃO: Usar APENAS em ambiente de desenvolvimento/debug.
-- NÃO executar em produção.
--
-- Ordem de deleção respeita as foreign keys.
-- ============================================================================

-- Limpa quotas de uso
DELETE FROM licensing_quotas WHERE 1=1;

-- Limpa capabilities
DELETE FROM licensing_capabilities WHERE 1=1;

-- Limpa ativações
DELETE FROM licensing_activations WHERE 1=1;

-- Limpa hardware bindings
DELETE FROM licensing_hardware_bindings WHERE 1=1;

-- Limpa consentimentos de telemetria
DELETE FROM licensing_telemetry_consents WHERE 1=1;

-- Limpa licenças
DELETE FROM licensing_licenses WHERE 1=1;

-- Limpa outbox do módulo licensing (se existir)
-- DELETE FROM licensing_outbox_messages WHERE 1=1;

-- Confirma limpeza
SELECT 'CommercialGovernance test data reset complete' AS status;
