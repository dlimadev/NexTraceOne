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
    "Id", "TenantId", "Name", "DisplayName", "ConnectorType",
    "Description", "Status", "HealthStatus",
    "ConfigurationJson", "IsBuiltIn",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '40000000-0000-0000-0001-000000000001',
    '00000000-0000-0000-0000-000000000001',
    'github',
    'GitHub',
    'SourceControl',
    'GitHub source control integration for release tracking and commit correlation.',
    'Active',
    'Unknown',
    '{}',
    TRUE,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "int_connectors" (
    "Id", "TenantId", "Name", "DisplayName", "ConnectorType",
    "Description", "Status", "HealthStatus",
    "ConfigurationJson", "IsBuiltIn",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '40000000-0000-0000-0001-000000000002',
    '00000000-0000-0000-0000-000000000001',
    'jira',
    'Jira',
    'IssueTracker',
    'Jira integration for work item linking and change governance.',
    'Active',
    'Unknown',
    '{}',
    TRUE,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "int_connectors" (
    "Id", "TenantId", "Name", "DisplayName", "ConnectorType",
    "Description", "Status", "HealthStatus",
    "ConfigurationJson", "IsBuiltIn",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '40000000-0000-0000-0001-000000000003',
    '00000000-0000-0000-0000-000000000001',
    'pagerduty',
    'PagerDuty',
    'IncidentManagement',
    'PagerDuty integration for incident ingestion and on-call correlation.',
    'Active',
    'Unknown',
    '{}',
    TRUE,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "int_connectors" (
    "Id", "TenantId", "Name", "DisplayName", "ConnectorType",
    "Description", "Status", "HealthStatus",
    "ConfigurationJson", "IsBuiltIn",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '40000000-0000-0000-0001-000000000004',
    '00000000-0000-0000-0000-000000000001',
    'aws-cost-explorer',
    'AWS Cost Explorer',
    'CloudBilling',
    'AWS Cost Explorer integration for FinOps cost attribution.',
    'Active',
    'Unknown',
    '{}',
    TRUE,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

COMMIT;
