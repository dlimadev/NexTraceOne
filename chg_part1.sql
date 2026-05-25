INSERT INTO chg_change_events (
    "Id", "ReleaseId", "EventType", "Description", "Source",
    "OccurredAt", "IsDeleted", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy"
)
VALUES
    ('e1000000-0000-0000-0000-000000000001', 'a4000000-0000-0000-0000-000000000001', 'CommitAssociated', 'Commit a1b2c3d4 associado ao release v2.4.1 - actualização de dependências.', 'github', NOW() - INTERVAL '2 days', false, NOW(), 'seed', NOW(), 'seed'),
    ('e1000000-0000-0000-0000-000000000002', 'a4000000-0000-0000-000000000001', 'DeploymentStarted', 'Deploy iniciado para production via pipeline GitHub Actions.', 'github-actions', NOW() - INTERVAL '2 days', false, NOW(), 'seed', NOW(), 'seed'),
    ('e1000000-0000-0000-0000-000000000003', 'a4000000-0000-0000-0000-000000000001', 'DeploymentCompleted', 'Deploy v2.4.1 concluído com sucesso em production.', 'github-actions', NOW() - INTERVAL '2 days' + INTERVAL '8 minutes', false, NOW(), 'seed', NOW(), 'seed'),
    ('e1000000-0000-0000-0000-000000000004', 'a4000000-0000-0000-0000-000000000002', 'DeploymentStarted', 'Deploy v1.8.3 iniciado para staging.', 'github-actions', NOW() - INTERVAL '3 hours', false, NOW(), 'seed', NOW(), 'seed'),
    ('e1000000-0000-0000-0000-000000000005', 'a4000000-0000-0000-0000-000000000003', 'BreakingChangeDetected', 'Breaking change detectado no endpoint /auth/token - clients devem migrar.', 'static-analysis', NOW() - INTERVAL '5 days', false, NOW(), 'seed', NOW(), 'seed'),
