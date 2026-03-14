-- =============================================================================
-- Seed: Mapeamento Plano → Feature Pack (CommercialCatalog)
-- Módulo: CommercialGovernance
-- Objetivo: Dados de teste associando planos aos seus feature packs.
-- Idempotente via ON CONFLICT DO NOTHING.
-- APENAS para desenvolvimento/debug — nunca executar em produção.
-- =============================================================================

-- SaaS Professional: API Governance + Change Intelligence
INSERT INTO plan_feature_pack_mappings (id, plan_id, feature_pack_id)
VALUES
    ('d1d2d3d4-0001-4000-8000-000000000001', 'a1b2c3d4-0001-4000-8000-000000000001', 'b1b2c3d4-0001-4000-8000-000000000001'),
    ('d1d2d3d4-0002-4000-8000-000000000002', 'a1b2c3d4-0001-4000-8000-000000000001', 'b1b2c3d4-0002-4000-8000-000000000002')
ON CONFLICT DO NOTHING;

-- SaaS Enterprise: Todos os packs
INSERT INTO plan_feature_pack_mappings (id, plan_id, feature_pack_id)
VALUES
    ('d1d2d3d4-0003-4000-8000-000000000003', 'a1b2c3d4-0002-4000-8000-000000000002', 'b1b2c3d4-0001-4000-8000-000000000001'),
    ('d1d2d3d4-0004-4000-8000-000000000004', 'a1b2c3d4-0002-4000-8000-000000000002', 'b1b2c3d4-0002-4000-8000-000000000002'),
    ('d1d2d3d4-0005-4000-8000-000000000005', 'a1b2c3d4-0002-4000-8000-000000000002', 'b1b2c3d4-0003-4000-8000-000000000003')
ON CONFLICT DO NOTHING;

-- Self-Hosted Professional: API Governance + Change Intelligence
INSERT INTO plan_feature_pack_mappings (id, plan_id, feature_pack_id)
VALUES
    ('d1d2d3d4-0006-4000-8000-000000000006', 'a1b2c3d4-0003-4000-8000-000000000003', 'b1b2c3d4-0001-4000-8000-000000000001'),
    ('d1d2d3d4-0007-4000-8000-000000000007', 'a1b2c3d4-0003-4000-8000-000000000003', 'b1b2c3d4-0002-4000-8000-000000000002')
ON CONFLICT DO NOTHING;

-- On-Premise Enterprise: Todos os packs
INSERT INTO plan_feature_pack_mappings (id, plan_id, feature_pack_id)
VALUES
    ('d1d2d3d4-0008-4000-8000-000000000008', 'a1b2c3d4-0004-4000-8000-000000000004', 'b1b2c3d4-0001-4000-8000-000000000001'),
    ('d1d2d3d4-0009-4000-8000-000000000009', 'a1b2c3d4-0004-4000-8000-000000000004', 'b1b2c3d4-0002-4000-8000-000000000002'),
    ('d1d2d3d4-0010-4000-8000-000000000010', 'a1b2c3d4-0004-4000-8000-000000000004', 'b1b2c3d4-0003-4000-8000-000000000003')
ON CONFLICT DO NOTHING;

-- Trial Starter: Apenas API Governance
INSERT INTO plan_feature_pack_mappings (id, plan_id, feature_pack_id)
VALUES
    ('d1d2d3d4-0011-4000-8000-000000000011', 'a1b2c3d4-0005-4000-8000-000000000005', 'b1b2c3d4-0001-4000-8000-000000000001')
ON CONFLICT DO NOTHING;
