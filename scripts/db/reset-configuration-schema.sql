-- ============================================================================
-- RESET COMPLETO DO SCHEMA DO ConfigurationDbContext
-- Apaga todas as tabelas cfg_* e ntf_* e limpa o histórico de migrations
-- ============================================================================

-- 1. Apagar tabelas na ordem correta (dependentes primeiro, depois CASCADE para segurança)
DROP TABLE IF EXISTS cfg_audit_entries CASCADE;
DROP TABLE IF EXISTS cfg_feature_flag_entries CASCADE;
DROP TABLE IF EXISTS cfg_entries CASCADE;
DROP TABLE IF EXISTS cfg_taxonomy_values CASCADE;
DROP TABLE IF EXISTS cfg_definitions CASCADE;
DROP TABLE IF EXISTS cfg_feature_flag_definitions CASCADE;
DROP TABLE IF EXISTS ntf_deliveries CASCADE;
DROP TABLE IF EXISTS cfg_automation_rules CASCADE;
DROP TABLE IF EXISTS cfg_change_checklists CASCADE;
DROP TABLE IF EXISTS cfg_contract_compliance_policies CASCADE;
DROP TABLE IF EXISTS cfg_contract_templates CASCADE;
DROP TABLE IF EXISTS cfg_entity_tags CASCADE;
DROP TABLE IF EXISTS cfg_modules CASCADE;
DROP TABLE IF EXISTS cfg_outbox_messages CASCADE;
DROP TABLE IF EXISTS cfg_saved_prompts CASCADE;
DROP TABLE IF EXISTS cfg_scheduled_reports CASCADE;
DROP TABLE IF EXISTS cfg_service_custom_fields CASCADE;
DROP TABLE IF EXISTS cfg_taxonomy_categories CASCADE;
DROP TABLE IF EXISTS cfg_user_alert_rules CASCADE;
DROP TABLE IF EXISTS cfg_user_bookmarks CASCADE;
DROP TABLE IF EXISTS cfg_user_saved_views CASCADE;
DROP TABLE IF EXISTS cfg_user_watches CASCADE;
DROP TABLE IF EXISTS cfg_webhook_templates CASCADE;
DROP TABLE IF EXISTS ntf_channel_configurations CASCADE;
DROP TABLE IF EXISTS ntf_notifications CASCADE;
DROP TABLE IF EXISTS ntf_preferences CASCADE;
DROP TABLE IF EXISTS ntf_smtp_configurations CASCADE;
DROP TABLE IF EXISTS ntf_templates CASCADE;

-- 2. Limpar entradas do ConfigurationDbContext no histórico de migrations
-- Remove InitialCreate e AddNotificationsEntities antigas/nosas
DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%InitialCreate'
   OR "MigrationId" LIKE '%AddNotificationsEntities';

-- ============================================================================
-- INSTRUÇÕES:
-- 1. Conecta-te à base de dados:  psql -U postgres -d nextraceone
-- 2. Executa este ficheiro:       \i scripts/db/reset-configuration-schema.sql
-- 3. Reinicia a aplicação. A migration 20260604182051_InitialCreate vai criar
--    todas as 28 tabelas do ConfigurationDbContext com o schema atual.
-- ============================================================================
