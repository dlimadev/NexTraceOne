INSERT INTO chg_change_events (
    "Id", "ReleaseId", "EventType", "Description", "Source",
    "OccurredAt", "IsDeleted", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy"
)
VALUES
    ('e1000000-0000-0000-0000-000000000001', 'a4000000-0000-0000-0000-000000000001', 'CommitAssociated', 'Commit a1b2c3d4 associado ao release v2.4.1 - actualizacao de dependencias.', 'github', NOW() - INTERVAL '2 days', false, NOW(), 'seed', NOW(), 'seed'),
    ('e1000000-0000-0000-0000-000000000002', 'a4000000-0000-0000-0000-000000000001', 'DeploymentStarted', 'Deploy iniciado para production via pipeline GitHub Actions.', 'github-actions', NOW() - INTERVAL '2 days', false, NOW(), 'seed', NOW(), 'seed')
ON CONFLICT DO NOTHING;
