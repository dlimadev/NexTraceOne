-- ============================================================================
-- NexTraceOne Configuration Module Seed Data
-- Idempotent: uses ON CONFLICT DO NOTHING
-- Includes: ConfigurationDefinitions, ConfigurationEntries (system defaults)
-- ============================================================================

BEGIN;
SET CONSTRAINTS ALL DEFERRED;

-- ── CONFIGURATION DEFINITIONS ────────────────────────────────────────────────

INSERT INTO "cfg_definitions" (
    "Id", "Key", "DisplayName", "Description",
    "Category", "AllowedScopes", "ValueType", "DefaultValue",
    "IsRequired", "IsSecret", "ValidationRegex",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '30000000-0000-0000-0001-000000000001',
    'analytics.enabled',
    'Analytics Enabled',
    'Enables product analytics event recording across all modules.',
    'Functional',
    ARRAY['System', 'Tenant'],
    'Boolean',
    'true',
    FALSE, FALSE, NULL,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "cfg_definitions" (
    "Id", "Key", "DisplayName", "Description",
    "Category", "AllowedScopes", "ValueType", "DefaultValue",
    "IsRequired", "IsSecret", "ValidationRegex",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '30000000-0000-0000-0001-000000000002',
    'analytics.max_range_days',
    'Analytics Max Range (Days)',
    'Maximum number of days allowed in a single analytics query window.',
    'Functional',
    ARRAY['System'],
    'Integer',
    '90',
    FALSE, FALSE, '^\d+$',
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "cfg_definitions" (
    "Id", "Key", "DisplayName", "Description",
    "Category", "AllowedScopes", "ValueType", "DefaultValue",
    "IsRequired", "IsSecret", "ValidationRegex",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '30000000-0000-0000-0001-000000000003',
    'analytics.trend_threshold_percent',
    'Analytics Trend Threshold (%)',
    'Percentage change threshold used to classify metric trends as improving or declining.',
    'Functional',
    ARRAY['System'],
    'Decimal',
    '0.1',
    FALSE, FALSE, NULL,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "cfg_definitions" (
    "Id", "Key", "DisplayName", "Description",
    "Category", "AllowedScopes", "ValueType", "DefaultValue",
    "IsRequired", "IsSecret", "ValidationRegex",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '30000000-0000-0000-0001-000000000004',
    'catalog.contract-pipeline.preview',
    'Contract Pipeline Preview',
    'Feature flag for the Contract Pipeline feature. When enabled, the pipeline is in PREVIEW state and indicated in API responses.',
    'Functional',
    ARRAY['System'],
    'Boolean',
    'true',
    FALSE, FALSE, NULL,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "cfg_definitions" (
    "Id", "Key", "DisplayName", "Description",
    "Category", "AllowedScopes", "ValueType", "DefaultValue",
    "IsRequired", "IsSecret", "ValidationRegex",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '30000000-0000-0000-0001-000000000005',
    'platform.notifications.enabled',
    'Notifications Enabled',
    'Master switch for the notifications subsystem.',
    'Functional',
    ARRAY['System', 'Tenant'],
    'Boolean',
    'true',
    FALSE, FALSE, NULL,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "cfg_definitions" (
    "Id", "Key", "DisplayName", "Description",
    "Category", "AllowedScopes", "ValueType", "DefaultValue",
    "IsRequired", "IsSecret", "ValidationRegex",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '30000000-0000-0000-0001-000000000006',
    'env.behavior.jobs.non_prod_scheduler.enabled',
    'Non-Prod Scheduler Enabled',
    'Enables automated startup/shutdown scheduling of non-production environments.',
    'Functional',
    ARRAY['System', 'Tenant'],
    'Boolean',
    'false',
    FALSE, FALSE, NULL,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

COMMIT;
