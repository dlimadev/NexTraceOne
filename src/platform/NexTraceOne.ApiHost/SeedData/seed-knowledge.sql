-- ============================================================================
-- NexTraceOne Knowledge Module Seed Data
-- Idempotent: uses ON CONFLICT DO NOTHING
-- Includes: KnowledgeDocuments (platform runbooks and guides)
-- ============================================================================

BEGIN;
SET CONSTRAINTS ALL DEFERRED;

-- ── KNOWLEDGE DOCUMENTS ──────────────────────────────────────────────────────

INSERT INTO "knw_documents" (
    "Id", "TenantId", "Title", "Summary",
    "Content", "DocumentType", "Status",
    "ServiceName", "Tags", "FreshnessScore",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '60000000-0000-0000-0001-000000000001',
    '00000000-0000-0000-0000-000000000001',
    'NexTraceOne Platform Onboarding Guide',
    'Step-by-step guide for onboarding a new service team to the NexTraceOne platform.',
    '## Getting Started\n\n1. Register your service in the Service Catalog\n2. Configure your deployment environments\n3. Apply the relevant Governance Pack\n4. Connect your CI/CD pipeline via the Integrations module\n5. Set up notification preferences\n\nFor detailed instructions, refer to the platform documentation.',
    'Runbook',
    'Published',
    NULL,
    ARRAY['onboarding', 'platform', 'getting-started'],
    1.0,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "knw_documents" (
    "Id", "TenantId", "Title", "Summary",
    "Content", "DocumentType", "Status",
    "ServiceName", "Tags", "FreshnessScore",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '60000000-0000-0000-0001-000000000002',
    '00000000-0000-0000-0000-000000000001',
    'Incident Response Runbook',
    'Standard operating procedure for investigating and mitigating platform incidents.',
    '## Incident Response SOP\n\n### Detection\n1. Alert fires via PagerDuty or monitoring dashboard\n2. Acknowledge alert within 5 minutes\n\n### Investigation\n1. Open the incident in the Change Intelligence module\n2. Review correlated metrics and traces\n3. Identify root cause using the AI Assistant\n\n### Mitigation\n1. Start the Mitigation Workflow\n2. Apply the appropriate fix or rollback\n3. Verify service health via SLO dashboard\n\n### Post-Incident\n1. Complete the post-mortem form\n2. Update this runbook if the process changed',
    'Runbook',
    'Published',
    NULL,
    ARRAY['incident', 'response', 'mitigation', 'sop'],
    1.0,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "knw_documents" (
    "Id", "TenantId", "Title", "Summary",
    "Content", "DocumentType", "Status",
    "ServiceName", "Tags", "FreshnessScore",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '60000000-0000-0000-0001-000000000003',
    '00000000-0000-0000-0000-000000000001',
    'Observability Provider Configuration',
    'How to configure Elastic or ClickHouse as the observability backend for the NexTraceOne platform.',
    '## Observability Provider Setup\n\nNexTraceOne supports two observability backends:\n- **Elasticsearch** (default): Suitable for log-centric workloads\n- **ClickHouse** (alternative): Optimised for high-cardinality analytics at scale\n\n### Configuration\nSet `Telemetry:ObservabilityProvider:Provider` in `appsettings.json` to either `Elastic` or `ClickHouse`.\n\nFor Elastic, configure `Elastic:LegacyTelemetry:Endpoint`.\nFor ClickHouse, configure `ClickHouse:LegacyTelemetry:Endpoint`.\n\nVerify the provider is reachable via `GET /api/v1/platform/observability/provider`.',
    'Guide',
    'Published',
    NULL,
    ARRAY['observability', 'elasticsearch', 'clickhouse', 'configuration'],
    1.0,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

COMMIT;
