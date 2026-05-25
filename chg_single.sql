INSERT INTO chg_change_events (
    "Id", "ReleaseId", "EventType", "Description", "Source",
    "OccurredAt", "IsDeleted", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy"
)
VALUES
    ('e1000000-0000-0000-0000-000000000002', 'a4000000-0000-0000-0000-000000000001', 'DeploymentStarted', 'Deploy iniciado para production via pipeline GitHub Actions.', 'github-actions', NOW() - INTERVAL '2 days', false, NOW(), 'seed', NOW(), 'seed')
ON CONFLICT DO NOTHING;
