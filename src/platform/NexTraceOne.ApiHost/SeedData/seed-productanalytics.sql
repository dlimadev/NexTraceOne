-- ============================================================================
-- NexTraceOne Product Analytics Module Seed Data
-- Idempotent: uses ON CONFLICT DO NOTHING
-- Includes: JourneyDefinitions
-- ============================================================================

BEGIN;
SET CONSTRAINTS ALL DEFERRED;

-- ── JOURNEY DEFINITIONS ─────────────────────────────────────────────────────

INSERT INTO "pan_journey_definitions" (
    "Id", "TenantId", "Key", "Name",
    "StepsJson", "IsActive",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '20000000-0000-0000-0001-000000000001',
    '00000000-0000-0000-0000-000000000001',
    'create_service',
    'Create Service',
    '[{"name":"ServiceCreated","eventType":8,"module":5,"order":0}]',
    TRUE,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "pan_journey_definitions" (
    "Id", "TenantId", "Key", "Name",
    "StepsJson", "IsActive",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '20000000-0000-0000-0001-000000000002',
    '00000000-0000-0000-0000-000000000001',
    'create_contract',
    'Create Contract',
    '[{"name":"ContractDraftCreated","eventType":2,"module":0,"order":0},{"name":"ContractPublished","eventType":3,"module":0,"order":1}]',
    TRUE,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "pan_journey_definitions" (
    "Id", "TenantId", "Key", "Name",
    "StepsJson", "IsActive",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '20000000-0000-0000-0001-000000000003',
    '00000000-0000-0000-0000-000000000001',
    'mitigate_incident',
    'Mitigate Incident',
    '[{"name":"MitigationWorkflowStarted","eventType":12,"module":3,"order":0},{"name":"MitigationWorkflowCompleted","eventType":13,"module":3,"order":1}]',
    TRUE,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

COMMIT;
