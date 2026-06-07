-- ============================================================================
-- NUKE DATABASE — Apaga TUDO do schema public (todas as tabelas de todos os DbContexts)
-- Ambiente: DESENVOLVIMENTO APENAS
-- ============================================================================
-- Este script:
--   1. Remove TODAS as tabelas do schema public (cfg_*, iam_*, cat_*, chg_*, 
--      ntf_*, gov_*, env_*, aik_*, int_*, oi_*, etc.)
--   2. Remove a tabela __EFMigrationsHistory (se existir)
--
-- Ao reiniciar a aplicação, os 10 DbContexts vão recriar todas as tabelas do
-- zero via migrations e aplicar os seeds automáticos.
-- ============================================================================

DO $$ DECLARE
    r RECORD;
    i INT;
BEGIN
    -- Múltiplas passagens para resolver dependências circulares entre tabelas
    FOR i IN 1..3 LOOP
        FOR r IN (
            SELECT tablename 
            FROM pg_tables 
            WHERE schemaname = 'public'
            ORDER BY tablename
        ) LOOP
            BEGIN
                EXECUTE 'DROP TABLE IF EXISTS ' || quote_ident(r.tablename) || ' CASCADE';
            EXCEPTION WHEN OTHERS THEN
                -- Ignora erros de dependência; a próxima passagem resolve
                NULL;
            END;
        END LOOP;
    END LOOP;
END $$;

-- Verificação: lista tabelas restantes (deve estar vazio)
SELECT tablename AS remaining_tables
FROM pg_tables 
WHERE schemaname = 'public';
