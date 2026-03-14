-- =============================================================================
-- Script 00: Reset / Cleanup dos dados de contratos
-- Módulo: Contracts & Interoperability (Módulo 5)
-- Uso: Apenas desenvolvimento/debug — NÃO executar em produção
-- Ordem: Executar PRIMEIRO, antes dos demais scripts
-- =============================================================================

-- Limpa artefatos gerados
DELETE FROM catalog.ct_contract_artifacts WHERE contract_version_id IN (
    SELECT id FROM catalog.ct_contract_versions WHERE api_asset_id IN (
        'c5000001-0000-0000-0000-000000000001'::uuid,
        'c5000001-0000-0000-0000-000000000002'::uuid,
        'c5000001-0000-0000-0000-000000000003'::uuid,
        'c5000001-0000-0000-0000-000000000004'::uuid,
        'c5000001-0000-0000-0000-000000000005'::uuid
    )
);

-- Limpa violações de regras
DELETE FROM catalog.ct_contract_rule_violations WHERE contract_version_id IN (
    SELECT id FROM catalog.ct_contract_versions WHERE api_asset_id IN (
        'c5000001-0000-0000-0000-000000000001'::uuid,
        'c5000001-0000-0000-0000-000000000002'::uuid,
        'c5000001-0000-0000-0000-000000000003'::uuid,
        'c5000001-0000-0000-0000-000000000004'::uuid,
        'c5000001-0000-0000-0000-000000000005'::uuid
    )
);

-- Limpa diffs semânticos
DELETE FROM catalog.ct_contract_diffs WHERE api_asset_id IN (
    'c5000001-0000-0000-0000-000000000001'::uuid,
    'c5000001-0000-0000-0000-000000000002'::uuid,
    'c5000001-0000-0000-0000-000000000003'::uuid,
    'c5000001-0000-0000-0000-000000000004'::uuid,
    'c5000001-0000-0000-0000-000000000005'::uuid
);

-- Limpa versões de contrato
DELETE FROM catalog.ct_contract_versions WHERE api_asset_id IN (
    'c5000001-0000-0000-0000-000000000001'::uuid,
    'c5000001-0000-0000-0000-000000000002'::uuid,
    'c5000001-0000-0000-0000-000000000003'::uuid,
    'c5000001-0000-0000-0000-000000000004'::uuid,
    'c5000001-0000-0000-0000-000000000005'::uuid
);
