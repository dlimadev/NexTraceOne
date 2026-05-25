    ('e1000000-0000-0000-0000-000000000006', 'a4000000-0000-0000-0000-000000000003', 'RollbackInitiated', 'Rollback para v3.0.2 iniciado após erro 500 em production.', 'sre-oncall', NOW() - INTERVAL '5 days' + INTERVAL '20 minutes', false, NOW(), 'seed', NOW(), 'seed'),
    ('e1000000-0000-0000-0000-000000000007', 'a4000000-0000-0000-0000-000000000004', 'WorkItemAssociated', 'Work item NXT-1098 associado ao release v1.2.0.', 'jira', NOW() - INTERVAL '30 minutes', false, NOW(), 'seed', NOW(), 'seed'),
    ('e1000000-0000-0000-0000-000000000008', 'a4000000-0000-0000-0000-000000000004', 'CanaryStarted', 'Canary deployment iniciado em development (10% traffic).', 'argocd', NOW() - INTERVAL '25 minutes', false, NOW(), 'seed', NOW(), 'seed')
ON CONFLICT DO NOTHING;
