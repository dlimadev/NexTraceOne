-- ============================================================================
-- NexTraceOne — Engineering Graph: Script de Reset/Limpeza
-- ============================================================================
-- Remove toda a massa de teste do módulo Engineering Graph.
-- Executar antes de reinserir dados de teste para garantir idempotência.
-- ATENÇÃO: Este script apaga TODOS os dados do módulo. Usar apenas em
-- ambiente de desenvolvimento ou testes.
-- ============================================================================

-- Limpar tabelas na ordem correta (respeitando FK constraints)
DELETE FROM "EngineeringGraph"."NodeHealthRecords";
DELETE FROM "EngineeringGraph"."SavedGraphViews";
DELETE FROM "EngineeringGraph"."GraphSnapshots";
DELETE FROM "EngineeringGraph"."DiscoverySources";
DELETE FROM "EngineeringGraph"."ConsumerRelationships";
DELETE FROM "EngineeringGraph"."ConsumerAssets";
DELETE FROM "EngineeringGraph"."ApiAssets";
DELETE FROM "EngineeringGraph"."ServiceAssets";

-- Nota: Se o schema não usar prefixo, ajustar os nomes das tabelas conforme
-- a configuração do EngineeringGraphDbContext (ex: sem schema ou schema diferente).
