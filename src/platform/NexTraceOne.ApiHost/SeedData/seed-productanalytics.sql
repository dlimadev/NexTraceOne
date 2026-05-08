-- ============================================================================
-- NexTraceOne Product Analytics Module Seed Data
-- Idempotent: uses ON CONFLICT DO NOTHING
-- Includes: JourneyDefinitions
-- ============================================================================

BEGIN;
SET CONSTRAINTS ALL DEFERRED;

-- ── JOURNEY DEFINITIONS ─────────────────────────────────────────────────────

INSERT INTO "pan_journey_definitions" (
    "Id", "TenantId", "Name", "Description",
    "StartEventType", "CompleteEventType", "Module",
    "IsActive", "CreatedAt", "UpdatedAt"
) VALUES (
    '20000000-0000-0000-0001-000000000001',
    '00000000-0000-0000-0000-000000000001',
    'create_service',
    'Jornada de criação de um serviço no catálogo.',
    8,  -- ServiceCreated (AnalyticsEventType.ServiceCreated)
    8,
    5,  -- SourceOfTruth
    TRUE,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "pan_journey_definitions" (
    "Id", "TenantId", "Name", "Description",
    "StartEventType", "CompleteEventType", "Module",
    "IsActive", "CreatedAt", "UpdatedAt"
) VALUES (
    '20000000-0000-0000-0001-000000000002',
    '00000000-0000-0000-0000-000000000001',
    'create_contract',
    'Jornada de criação e publicação de um contrato.',
    2,  -- ContractDraftCreated
    3,  -- ContractPublished
    0,  -- ContractStudio
    TRUE,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "pan_journey_definitions" (
    "Id", "TenantId", "Name", "Description",
    "StartEventType", "CompleteEventType", "Module",
    "IsActive", "CreatedAt", "UpdatedAt"
) VALUES (
    '20000000-0000-0000-0001-000000000003',
    '00000000-0000-0000-0000-000000000001',
    'mitigate_incident',
    'Jornada de investigação e mitigação de incidente.',
    12, -- MitigationWorkflowStarted
    13, -- MitigationWorkflowCompleted
    3,  -- Incidents
    TRUE,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

COMMIT;
