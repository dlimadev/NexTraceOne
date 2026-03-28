-- ═══════════════════════════════════════════════════════════════════════════════
-- NEXTRACEONE — Seed data: Incidents Module (IncidentDatabase)
-- Tables: ops_incidents, ops_runbooks
-- Replicates the 3 development runbooks and sample incidents for local development.
-- All INSERT statements are idempotent: ON CONFLICT DO NOTHING.
-- ═══════════════════════════════════════════════════════════════════════════════

-- ═══ RUNBOOKS ════════════════════════════════════════════════════════════════

INSERT INTO ops_runbooks (
  "Id", "Title", "Description",
  "LinkedService", "LinkedIncidentType",
  "StepsJson", "PrerequisitesJson", "PostNotes",
  "MaintainedBy", "PublishedAt", "LastReviewedAt",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'bb000001-0001-0000-0000-000000000001',
  'Payment Gateway Rollback Procedure',
  'Step-by-step guide for rolling back the payment-service deployment to a known stable version.',
  'payment-service', 'ServiceDegradation',
  '[{"stepOrder":1,"title":"Confirm rollback target version","description":"Identify the last known stable version from deployment history.","isOptional":false},{"stepOrder":2,"title":"Notify affected teams","description":"Send notification to downstream consumers before rollback.","isOptional":false},{"stepOrder":3,"title":"Trigger rollback pipeline","description":"Use the CI/CD one-click rollback to deploy the target version.","isOptional":false},{"stepOrder":4,"title":"Validate deployment health","description":"Check health endpoints and error rates post-deployment.","isOptional":false},{"stepOrder":5,"title":"Monitor for 30 minutes","description":"Observe error rate and payment success metrics for stability.","isOptional":false},{"stepOrder":6,"title":"Update incident status","description":"Mark the incident as mitigated and document the outcome.","isOptional":true}]',
  '["CI/CD pipeline access for payment-service","Previous stable version identified","Downstream teams notified"]',
  'After rollback, monitor error rate and payment success rate for at least 30 minutes. If metrics do not return to baseline, escalate to payments-lead.',
  'platform-team@nextraceone.io',
  '2024-01-15T09:00:00+00:00', '2024-05-20T14:30:00+00:00',
  NOW(), 'system', NOW(), 'system', false
),
(
  'bb000002-0001-0000-0000-000000000001',
  'Catalog Sync Manual Recovery',
  'Steps for manually recovering catalog synchronization when the external provider is unavailable.',
  'catalog-service', 'DependencyFailure',
  '[{"stepOrder":1,"title":"Check vendor status page","description":"Verify the current status of the external catalog provider.","isOptional":false},{"stepOrder":2,"title":"Attempt manual sync request","description":"Send a manual sync request to test connectivity.","isOptional":false},{"stepOrder":3,"title":"Enable fallback mode","description":"Activate the manual sync fallback configuration.","isOptional":false},{"stepOrder":4,"title":"Verify catalog data freshness","description":"Confirm catalog data is within acceptable freshness threshold.","isOptional":false}]',
  '["Access to catalog-service configuration","Manual sync endpoint credentials"]',
  'Monitor catalog data freshness and sync error rate. Disable fallback mode once vendor connectivity is restored.',
  'platform-team@nextraceone.io',
  '2024-02-10T11:00:00+00:00', NULL,
  NOW(), 'system', NOW(), 'system', false
),
(
  'bb000003-0001-0000-0000-000000000001',
  'Generic Service Restart Procedure',
  'Standard procedure for performing a controlled restart of a service with minimal impact.',
  NULL, NULL,
  '[{"stepOrder":1,"title":"Notify dependent teams","description":"Alert teams that depend on this service about the planned restart.","isOptional":true},{"stepOrder":2,"title":"Drain active connections","description":"Gracefully drain active connections before restart.","isOptional":false},{"stepOrder":3,"title":"Trigger controlled restart","description":"Initiate the restart via orchestrator or deployment tool.","isOptional":false},{"stepOrder":4,"title":"Verify service health","description":"Confirm the service is healthy post-restart.","isOptional":false}]',
  '["Orchestrator or deployment tool access","Service health endpoint available"]',
  'Monitor service health and downstream error rates for 15 minutes post-restart.',
  'sre-team@nextraceone.io',
  '2024-03-01T08:00:00+00:00', '2024-04-10T16:00:00+00:00',
  NOW(), 'system', NOW(), 'system', false
)
ON CONFLICT ("Id") DO NOTHING;
