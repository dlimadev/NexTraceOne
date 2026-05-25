INSERT INTO ntf_notifications (
    "Id", "TenantId", "RecipientUserId", "Title", "Message",
    "Category", "Severity", "Status", "EventType",
    "SourceModule", "SourceEntityId", "SourceEntityType",
    "OccurrenceCount", "RequiresAction", "IsEscalated", "IsSuppressed",
    "CreatedAt", "ReadAt", "AcknowledgedAt", "ArchivedAt",
    "ActionUrl", "CorrelationKey", "PayloadJson",
    "EnvironmentId", "GroupId", "ExpiresAt",
    "SnoozedBy", "SnoozedUntil", "EscalatedAt",
    "AcknowledgeComment", "SuppressionReason", "LastOccurrenceAt"
)
VALUES
    ('dead0001-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000001', 'Incident Resolved: Payment Gateway', 'O incidente INC-2026-001 foi resolvido. Latencia normalizada.', 'Incident', 'Info', 'Read', 'incident.resolved', 'OperationalIntelligence', 'a1000000-0000-0000-0000-000000000001', 'Incident', 1, false, false, false, NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days' + INTERVAL '30 minutes', NULL, NULL, '/governance/incidents/a1000000-0000-0000-0000-000000000001', 'inc-001', NULL, 'c0000000-0000-0000-0000-000000000003', NULL, NULL, NULL, NULL, NULL, NULL, NOW() - INTERVAL '2 days')
ON CONFLICT DO NOTHING;
