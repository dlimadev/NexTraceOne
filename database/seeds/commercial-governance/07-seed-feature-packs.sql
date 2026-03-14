-- =============================================================================
-- Seed: Feature Packs e Items (CommercialCatalog)
-- Módulo: CommercialGovernance
-- Objetivo: Dados de teste representando pacotes de funcionalidades
--           que podem ser associados a planos comerciais.
-- Idempotente via ON CONFLICT DO NOTHING.
-- APENAS para desenvolvimento/debug — nunca executar em produção.
-- =============================================================================

-- Feature Pack: API Governance
INSERT INTO feature_packs (id, code, name, description, is_active, created_at, created_by)
VALUES
    ('b1b2c3d4-0001-4000-8000-000000000001', 'api-governance-pack', 'API Governance Pack', 'Pacote com funcionalidades de governança de APIs: catálogo, contratos, diff semântico.', true, NOW(), 'seed-script'),
    ('b1b2c3d4-0002-4000-8000-000000000002', 'change-intelligence-pack', 'Change Intelligence Pack', 'Pacote com funcionalidades de inteligência de mudança: blast radius, workflow, aprovação.', true, NOW(), 'seed-script'),
    ('b1b2c3d4-0003-4000-8000-000000000003', 'admin-operations-pack', 'Admin Operations Pack', 'Pacote com funcionalidades administrativas: auditoria, compliance, gestão avançada.', true, NOW(), 'seed-script')
ON CONFLICT DO NOTHING;

-- Feature Pack Items: API Governance Pack
INSERT INTO feature_pack_items (id, feature_pack_id, capability_code, capability_name, default_limit)
VALUES
    ('c1c2c3d4-0001-4000-8000-000000000001', 'b1b2c3d4-0001-4000-8000-000000000001', 'api_catalog', 'API Catalog', NULL),
    ('c1c2c3d4-0002-4000-8000-000000000002', 'b1b2c3d4-0001-4000-8000-000000000001', 'contract_management', 'Contract Management', 100),
    ('c1c2c3d4-0003-4000-8000-000000000003', 'b1b2c3d4-0001-4000-8000-000000000001', 'semantic_diff', 'Semantic Diff', NULL),
    ('c1c2c3d4-0004-4000-8000-000000000004', 'b1b2c3d4-0001-4000-8000-000000000001', 'developer_portal', 'Developer Portal', NULL),
    ('c1c2c3d4-0005-4000-8000-000000000005', 'b1b2c3d4-0001-4000-8000-000000000001', 'multi_protocol', 'Multi-Protocol Support', NULL)
ON CONFLICT DO NOTHING;

-- Feature Pack Items: Change Intelligence Pack
INSERT INTO feature_pack_items (id, feature_pack_id, capability_code, capability_name, default_limit)
VALUES
    ('c1c2c3d4-0006-4000-8000-000000000006', 'b1b2c3d4-0002-4000-8000-000000000002', 'blast_radius', 'Blast Radius Analysis', NULL),
    ('c1c2c3d4-0007-4000-8000-000000000007', 'b1b2c3d4-0002-4000-8000-000000000002', 'workflow_engine', 'Workflow Engine', NULL),
    ('c1c2c3d4-0008-4000-8000-000000000008', 'b1b2c3d4-0002-4000-8000-000000000002', 'change_score', 'Change Intelligence Score', NULL),
    ('c1c2c3d4-0009-4000-8000-000000000009', 'b1b2c3d4-0002-4000-8000-000000000002', 'approval_workflow', 'Approval Workflow', NULL),
    ('c1c2c3d4-0010-4000-8000-000000000010', 'b1b2c3d4-0002-4000-8000-000000000002', 'promotion_gates', 'Promotion Gates', 5)
ON CONFLICT DO NOTHING;

-- Feature Pack Items: Admin Operations Pack
INSERT INTO feature_pack_items (id, feature_pack_id, capability_code, capability_name, default_limit)
VALUES
    ('c1c2c3d4-0011-4000-8000-000000000011', 'b1b2c3d4-0003-4000-8000-000000000003', 'audit_trail', 'Audit Trail', NULL),
    ('c1c2c3d4-0012-4000-8000-000000000012', 'b1b2c3d4-0003-4000-8000-000000000003', 'evidence_pack', 'Evidence Pack Export', NULL),
    ('c1c2c3d4-0013-4000-8000-000000000013', 'b1b2c3d4-0003-4000-8000-000000000003', 'compliance_reporting', 'Compliance Reporting', NULL),
    ('c1c2c3d4-0014-4000-8000-000000000014', 'b1b2c3d4-0003-4000-8000-000000000003', 'advanced_rbac', 'Advanced RBAC', NULL)
ON CONFLICT DO NOTHING;
