-- ============================================================================
-- NexTraceOne Notifications Module Seed Data
-- Idempotent: uses ON CONFLICT DO NOTHING
-- Includes: NotificationTemplates (system defaults)
-- ============================================================================

BEGIN;
SET CONSTRAINTS ALL DEFERRED;

-- ── NOTIFICATION TEMPLATES ───────────────────────────────────────────────────

INSERT INTO "ntf_templates" (
    "Id", "TenantId", "EventType", "Name",
    "SubjectTemplate", "BodyTemplate", "PlainTextTemplate",
    "Channel", "Locale", "IsActive", "IsBuiltIn",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '50000000-0000-0000-0001-000000000001',
    '00000000-0000-0000-0000-000000000001',
    'GovernancePackApplied',
    'governance_pack_applied',
    'Governance Pack Applied: {{PackName}}',
    'The governance pack "{{PackName}}" (version {{PackVersion}}) has been successfully applied to your domain.',
    'The governance pack "{{PackName}}" (version {{PackVersion}}) has been successfully applied to your domain.',
    'InApp',
    'en',
    TRUE, TRUE,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "ntf_templates" (
    "Id", "TenantId", "EventType", "Name",
    "SubjectTemplate", "BodyTemplate", "PlainTextTemplate",
    "Channel", "Locale", "IsActive", "IsBuiltIn",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '50000000-0000-0000-0001-000000000002',
    '00000000-0000-0000-0000-000000000001',
    'ComplianceViolationDetected',
    'compliance_violation_detected',
    'Compliance Violation: {{PolicyName}}',
    'A compliance violation has been detected for policy "{{PolicyName}}" on service {{ServiceName}}. Please review and remediate.',
    'A compliance violation has been detected for policy "{{PolicyName}}" on service {{ServiceName}}. Please review and remediate.',
    'InApp',
    'en',
    TRUE, TRUE,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "ntf_templates" (
    "Id", "TenantId", "EventType", "Name",
    "SubjectTemplate", "BodyTemplate", "PlainTextTemplate",
    "Channel", "Locale", "IsActive", "IsBuiltIn",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '50000000-0000-0000-0001-000000000003',
    '00000000-0000-0000-0000-000000000001',
    'WaiverApproved',
    'waiver_approved',
    'Waiver Approved: {{WaiverTitle}}',
    'Your governance waiver request "{{WaiverTitle}}" has been approved.',
    'Your governance waiver request "{{WaiverTitle}}" has been approved.',
    'InApp',
    'en',
    TRUE, TRUE,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "ntf_templates" (
    "Id", "TenantId", "EventType", "Name",
    "SubjectTemplate", "BodyTemplate", "PlainTextTemplate",
    "Channel", "Locale", "IsActive", "IsBuiltIn",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '50000000-0000-0000-0001-000000000004',
    '00000000-0000-0000-0000-000000000001',
    'IncidentMitigationStarted',
    'incident_mitigation_started',
    'Mitigation Started: {{IncidentTitle}}',
    'Mitigation workflow has started for incident "{{IncidentTitle}}". Estimated resolution by {{EstimatedAt}}.',
    'Mitigation workflow has started for incident "{{IncidentTitle}}". Estimated resolution by {{EstimatedAt}}.',
    'InApp',
    'en',
    TRUE, TRUE,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

COMMIT;
