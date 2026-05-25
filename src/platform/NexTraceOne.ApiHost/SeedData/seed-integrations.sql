-- ============================================================================
-- NexTraceOne Integrations Module Seed Data
-- Idempotent: uses ON CONFLICT DO NOTHING
-- Includes: IntegrationConnectors (built-in source connectors)
-- ============================================================================

BEGIN;
SET CONSTRAINTS ALL DEFERRED;

-- ── INTEGRATION CONNECTORS ───────────────────────────────────────────────────
-- Built-in connectors available to all tenants.

INSERT INTO "int_connectors" (
    "Id", "Name", "ConnectorType",
    "Description", "Provider", "Endpoint",
    "Status", "Health",
    "LastSuccessAt", "LastErrorAt", "LastErrorMessage",
    "FreshnessLagMinutes", "TotalExecutions", "SuccessfulExecutions", "FailedExecutions",
    "Environment", "AuthenticationMode", "PollingMode", "AllowedTeams",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '40000000-0000-0000-0001-000000000001',
    'github',
    'SourceControl',
    'GitHub source control integration for release tracking and commit correlation.',
    'GitHub',
    'https://api.github.com',
    'Active',
    'Unknown',
    NULL, NULL, NULL,
    0, 0, 0, 0,
    'All', 'OAuth2', 'Webhook',
    '[]',
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "int_connectors" (
    "Id", "Name", "ConnectorType",
    "Description", "Provider", "Endpoint",
    "Status", "Health",
    "LastSuccessAt", "LastErrorAt", "LastErrorMessage",
    "FreshnessLagMinutes", "TotalExecutions", "SuccessfulExecutions", "FailedExecutions",
    "Environment", "AuthenticationMode", "PollingMode", "AllowedTeams",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '40000000-0000-0000-0001-000000000002',
    'jira',
    'IssueTracker',
    'Jira integration for work item linking and change governance.',
    'Atlassian',
    'https://api.atlassian.com',
    'Active',
    'Unknown',
    NULL, NULL, NULL,
    0, 0, 0, 0,
    'All', 'OAuth2', 'Polling',
    '[]',
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "int_connectors" (
    "Id", "Name", "ConnectorType",
    "Description", "Provider", "Endpoint",
    "Status", "Health",
    "LastSuccessAt", "LastErrorAt", "LastErrorMessage",
    "FreshnessLagMinutes", "TotalExecutions", "SuccessfulExecutions", "FailedExecutions",
    "Environment", "AuthenticationMode", "PollingMode", "AllowedTeams",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '40000000-0000-0000-0001-000000000003',
    'pagerduty',
    'IncidentManagement',
    'PagerDuty integration for incident ingestion and on-call correlation.',
    'PagerDuty',
    'https://api.pagerduty.com',
    'Active',
    'Unknown',
    NULL, NULL, NULL,
    0, 0, 0, 0,
    'All', 'Token', 'Webhook',
    '[]',
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "int_connectors" (
    "Id", "Name", "ConnectorType",
    "Description", "Provider", "Endpoint",
    "Status", "Health",
    "LastSuccessAt", "LastErrorAt", "LastErrorMessage",
    "FreshnessLagMinutes", "TotalExecutions", "SuccessfulExecutions", "FailedExecutions",
    "Environment", "AuthenticationMode", "PollingMode", "AllowedTeams",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '40000000-0000-0000-0001-000000000004',
    'aws-cost-explorer',
    'CloudBilling',
    'AWS Cost Explorer integration for FinOps cost attribution.',
    'AWS',
    'https://ce.us-east-1.amazonaws.com',
    'Active',
    'Unknown',
    NULL, NULL, NULL,
    0, 0, 0, 0,
    'All', 'IAMRole', 'Polling',
    '[]',
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

COMMIT;
