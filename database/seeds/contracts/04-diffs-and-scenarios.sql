-- =============================================================================
-- Script 04: Seed de diffs semânticos e cenários breaking/non-breaking
-- Módulo: Contracts & Interoperability (Módulo 5)
-- Uso: Apenas desenvolvimento/debug — NÃO executar em produção
-- Pré-requisito: Executar 00-reset.sql e scripts 01-03 antes
-- =============================================================================

-- ── Diffs para contratos REST (OpenAPI) ─────────────────────────────────────

-- Diff entre Users API v1.0.0 → v1.1.0 (Additive: novos paths adicionados)
INSERT INTO catalog.ct_contract_diffs (
    id, contract_version_id, base_version_id, target_version_id, api_asset_id,
    change_level, breaking_changes, non_breaking_changes, additive_changes,
    suggested_sem_ver, computed_at, is_deleted
) VALUES (
    'c5040001-0000-0000-0000-000000000001'::uuid,
    'c5010001-0000-0000-0000-000000000002'::uuid,
    'c5010001-0000-0000-0000-000000000001'::uuid,
    'c5010001-0000-0000-0000-000000000002'::uuid,
    'c5000001-0000-0000-0000-000000000001'::uuid,
    'Additive',
    '[]',
    '[]',
    '[{"ChangeType":"PathAdded","Path":"/users/{id}","Method":null,"Description":"Path ''/users/{id}'' was added.","IsBreaking":false}]',
    '1.1.0',
    NOW() - INTERVAL '20 days', false
);

-- Diff entre Users API v1.1.0 → v2.0.0 (Breaking: parâmetro obrigatório adicionado, paths removidos)
INSERT INTO catalog.ct_contract_diffs (
    id, contract_version_id, base_version_id, target_version_id, api_asset_id,
    change_level, breaking_changes, non_breaking_changes, additive_changes,
    suggested_sem_ver, computed_at, is_deleted
) VALUES (
    'c5040001-0000-0000-0000-000000000002'::uuid,
    'c5010001-0000-0000-0000-000000000003'::uuid,
    'c5010001-0000-0000-0000-000000000002'::uuid,
    'c5010001-0000-0000-0000-000000000003'::uuid,
    'c5000001-0000-0000-0000-000000000001'::uuid,
    'Breaking',
    '[{"ChangeType":"PathRemoved","Path":"/users/{id}","Method":null,"Description":"Path ''/users/{id}'' was removed.","IsBreaking":true},{"ChangeType":"ParameterRequired","Path":"/users","Method":"GET","Description":"Required parameter ''filter'' was added to ''GET /users''.","IsBreaking":true}]',
    '[]',
    '[]',
    '2.0.0',
    NOW() - INTERVAL '5 days', false
);

-- ── Diffs para contratos WSDL ───────────────────────────────────────────────

-- Diff entre PaymentService v1.0.0 → v1.1.0 (Additive: nova operação)
INSERT INTO catalog.ct_contract_diffs (
    id, contract_version_id, base_version_id, target_version_id, api_asset_id,
    change_level, breaking_changes, non_breaking_changes, additive_changes,
    suggested_sem_ver, computed_at, is_deleted
) VALUES (
    'c5040001-0000-0000-0000-000000000003'::uuid,
    'c5020001-0000-0000-0000-000000000002'::uuid,
    'c5020001-0000-0000-0000-000000000001'::uuid,
    'c5020001-0000-0000-0000-000000000002'::uuid,
    'c5000001-0000-0000-0000-000000000003'::uuid,
    'Additive',
    '[]',
    '[]',
    '[{"ChangeType":"OperationAdded","Path":"PaymentPortType","Method":"CheckPaymentStatus","Description":"Operation ''CheckPaymentStatus'' was added to ''PaymentPortType''.","IsBreaking":false}]',
    '1.1.0',
    NOW() - INTERVAL '45 days', false
);

-- Diff entre PaymentService v1.1.0 → v2.0.0 (Breaking: operação removida)
INSERT INTO catalog.ct_contract_diffs (
    id, contract_version_id, base_version_id, target_version_id, api_asset_id,
    change_level, breaking_changes, non_breaking_changes, additive_changes,
    suggested_sem_ver, computed_at, is_deleted
) VALUES (
    'c5040001-0000-0000-0000-000000000004'::uuid,
    'c5020001-0000-0000-0000-000000000003'::uuid,
    'c5020001-0000-0000-0000-000000000002'::uuid,
    'c5020001-0000-0000-0000-000000000003'::uuid,
    'c5000001-0000-0000-0000-000000000003'::uuid,
    'Breaking',
    '[{"ChangeType":"OperationRemoved","Path":"PaymentPortType","Method":"RefundPayment","Description":"Operation ''RefundPayment'' was removed from ''PaymentPortType''.","IsBreaking":true}]',
    '[]',
    '[]',
    '2.0.0',
    NOW() - INTERVAL '10 days', false
);

-- ── Diffs para contratos AsyncAPI/Kafka ─────────────────────────────────────

-- Diff entre User Events v1.0.0 → v1.1.0 (Additive: novo canal)
INSERT INTO catalog.ct_contract_diffs (
    id, contract_version_id, base_version_id, target_version_id, api_asset_id,
    change_level, breaking_changes, non_breaking_changes, additive_changes,
    suggested_sem_ver, computed_at, is_deleted
) VALUES (
    'c5040001-0000-0000-0000-000000000005'::uuid,
    'c5030001-0000-0000-0000-000000000002'::uuid,
    'c5030001-0000-0000-0000-000000000001'::uuid,
    'c5030001-0000-0000-0000-000000000002'::uuid,
    'c5000001-0000-0000-0000-000000000004'::uuid,
    'Additive',
    '[]',
    '[]',
    '[{"ChangeType":"ChannelAdded","Path":"user/deleted","Method":null,"Description":"Channel ''user/deleted'' was added.","IsBreaking":false}]',
    '1.1.0',
    NOW() - INTERVAL '25 days', false
);

-- Diff entre User Events v1.1.0 → v2.0.0 (Breaking: campos obrigatórios adicionados)
INSERT INTO catalog.ct_contract_diffs (
    id, contract_version_id, base_version_id, target_version_id, api_asset_id,
    change_level, breaking_changes, non_breaking_changes, additive_changes,
    suggested_sem_ver, computed_at, is_deleted
) VALUES (
    'c5040001-0000-0000-0000-000000000006'::uuid,
    'c5030001-0000-0000-0000-000000000003'::uuid,
    'c5030001-0000-0000-0000-000000000002'::uuid,
    'c5030001-0000-0000-0000-000000000003'::uuid,
    'c5000001-0000-0000-0000-000000000004'::uuid,
    'Breaking',
    '[{"ChangeType":"FieldRequired","Path":"user/signedup","Method":"PUBLISH","Description":"Required field ''fullName'' was added to ''PUBLISH user/signedup''.","IsBreaking":true},{"ChangeType":"FieldRequired","Path":"user/signedup","Method":"PUBLISH","Description":"Required field ''tenantId'' was added to ''PUBLISH user/signedup''.","IsBreaking":true}]',
    '[]',
    '[]',
    '2.0.0',
    NOW() - INTERVAL '3 days', false
);
