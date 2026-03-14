-- ============================================================================
-- SEED: Capabilities de licenças para o módulo CommercialGovernance
-- ============================================================================
-- Associa capabilities às licenças criadas em 01-seed-licenses.sql.
-- Capabilities variam conforme a edição:
-- - Community: catalog:read, contracts:import, audit:read
-- - Professional: + catalog:write, contracts:diff, releases:manage, workflow:basic
-- - Enterprise: + workflow:advanced, audit:export, ai:consultation
-- - Trial: todas as Professional habilitadas
--
-- ATENÇÃO: Usar APENAS em ambiente de desenvolvimento/debug.
-- ============================================================================

-- Licença 1: Banco Nacional SA — Enterprise
INSERT INTO licensing_capabilities ("Id", "LicenseId", "Code", "Name", "IsEnabled") VALUES
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001', 'catalog:read', 'Catalog Read', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001', 'catalog:write', 'Catalog Write', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001', 'contracts:import', 'Contract Import', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001', 'contracts:diff', 'Semantic Diff', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001', 'releases:manage', 'Release Management', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001', 'workflow:basic', 'Basic Workflow', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001', 'workflow:advanced', 'Advanced Workflow', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001', 'audit:read', 'Audit Trail Read', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001', 'audit:export', 'Audit Export', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001', 'ai:consultation', 'AI Consultation', true)
ON CONFLICT DO NOTHING;

-- Licença 2: Seguros Confiança — Professional
INSERT INTO licensing_capabilities ("Id", "LicenseId", "Code", "Name", "IsEnabled") VALUES
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000002', 'catalog:read', 'Catalog Read', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000002', 'catalog:write', 'Catalog Write', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000002', 'contracts:import', 'Contract Import', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000002', 'contracts:diff', 'Semantic Diff', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000002', 'releases:manage', 'Release Management', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000002', 'workflow:basic', 'Basic Workflow', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000002', 'audit:read', 'Audit Trail Read', true)
ON CONFLICT DO NOTHING;

-- Licença 3: Ministério da Defesa — On-Premise Enterprise
INSERT INTO licensing_capabilities ("Id", "LicenseId", "Code", "Name", "IsEnabled") VALUES
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000003', 'catalog:read', 'Catalog Read', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000003', 'catalog:write', 'Catalog Write', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000003', 'contracts:import', 'Contract Import', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000003', 'contracts:diff', 'Semantic Diff', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000003', 'releases:manage', 'Release Management', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000003', 'workflow:basic', 'Basic Workflow', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000003', 'workflow:advanced', 'Advanced Workflow', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000003', 'audit:read', 'Audit Trail Read', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000003', 'audit:export', 'Audit Export', true)
ON CONFLICT DO NOTHING;

-- Licença 5: Startup Trial — todas as capabilities
INSERT INTO licensing_capabilities ("Id", "LicenseId", "Code", "Name", "IsEnabled") VALUES
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000005', 'catalog:read', 'Catalog Read', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000005', 'catalog:write', 'Catalog Write', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000005', 'contracts:import', 'Contract Import', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000005', 'contracts:diff', 'Semantic Diff', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000005', 'releases:manage', 'Release Management', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000005', 'workflow:basic', 'Basic Workflow', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000005', 'audit:read', 'Audit Trail Read', true)
ON CONFLICT DO NOTHING;

-- Licença 10: Community — apenas básico
INSERT INTO licensing_capabilities ("Id", "LicenseId", "Code", "Name", "IsEnabled") VALUES
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000010', 'catalog:read', 'Catalog Read', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000010', 'contracts:import', 'Contract Import', true),
    (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000010', 'audit:read', 'Audit Trail Read', true)
ON CONFLICT DO NOTHING;

SELECT count(*) AS capabilities_seeded FROM licensing_capabilities;
