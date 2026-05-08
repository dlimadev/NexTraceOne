-- ============================================================================
-- NexTraceOne Notifications Module Seed Data
-- Idempotent: uses ON CONFLICT DO NOTHING
-- Includes: NotificationTemplates (system defaults), SmtpConfiguration (placeholder)
-- ============================================================================

BEGIN;
SET CONSTRAINTS ALL DEFERRED;

-- ── NOTIFICATION TEMPLATES ───────────────────────────────────────────────────

INSERT INTO "ntf_templates" (
    "Id", "TenantId", "Name", "DisplayName",
    "Channel", "Subject", "BodyTemplate",
    "EventType", "IsActive",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '50000000-0000-0000-0001-000000000001',
    '00000000-0000-0000-0000-000000000001',
    'governance_pack_applied',
    'Governance Pack Applied',
    'InApp',
    'Governance Pack Applied: {{PackName}}',
    'The governance pack "{{PackName}}" (version {{PackVersion}}) has been successfully applied to your domain.',
    'GovernancePackApplied',
    TRUE,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "ntf_templates" (
    "Id", "TenantId", "Name", "DisplayName",
    "Channel", "Subject", "BodyTemplate",
    "EventType", "IsActive",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '50000000-0000-0000-0001-000000000002',
    '00000000-0000-0000-0000-000000000001',
    'compliance_violation_detected',
    'Compliance Violation Detected',
    'InApp',
    'Compliance Violation: {{PolicyName}}',
    'A compliance violation has been detected for policy "{{PolicyName}}" on service {{ServiceName}}. Please review and remediate.',
    'ComplianceViolationDetected',
    TRUE,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "ntf_templates" (
    "Id", "TenantId", "Name", "DisplayName",
    "Channel", "Subject", "BodyTemplate",
    "EventType", "IsActive",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '50000000-0000-0000-0001-000000000003',
    '00000000-0000-0000-0000-000000000001',
    'waiver_approved',
    'Governance Waiver Approved',
    'InApp',
    'Waiver Approved: {{WaiverTitle}}',
    'Your governance waiver request "{{WaiverTitle}}" has been approved.',
    'WaiverApproved',
    TRUE,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

INSERT INTO "ntf_templates" (
    "Id", "TenantId", "Name", "DisplayName",
    "Channel", "Subject", "BodyTemplate",
    "EventType", "IsActive",
    "CreatedAt", "UpdatedAt"
) VALUES (
    '50000000-0000-0000-0001-000000000004',
    '00000000-0000-0000-0000-000000000001',
    'incident_mitigation_started',
    'Incident Mitigation Started',
    'InApp',
    'Mitigation Started: {{IncidentTitle}}',
    'Mitigation workflow has started for incident "{{IncidentTitle}}". Estimated resolution by {{EstimatedAt}}.',
    'IncidentMitigationStarted',
    TRUE,
    NOW(), NOW()
) ON CONFLICT DO NOTHING;

COMMIT;
