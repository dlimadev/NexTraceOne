-- ═══════════════════════════════════════════════════════════════════════════════
-- NEXTRACEONE — Seed data: Incidents Module (nextraceone_incidents)
-- Tabelas: oi_incidents, oi_runbooks
-- Replica os 6 incidentes e 3 runbooks de desenvolvimento para paridade
-- funcional com InMemoryIncidentStore.
-- ═══════════════════════════════════════════════════════════════════════════════

-- Enum mappings (integer):
-- IncidentType: ServiceDegradation=0, DependencyFailure=1, MessagingIssue=2, OperationalRegression=3, BackgroundProcessingIssue=4, ContractImpact=5
-- IncidentSeverity: Critical=0, Major=1, Minor=2, Warning=3
-- IncidentStatus: Open=0, Investigating=1, Mitigating=2, Monitoring=3, Resolved=4, Closed=5
-- CorrelationConfidence: Low=0, Medium=1, High=2, Confirmed=3, NotAssessed=4
-- MitigationStatus: NotStarted=0, InProgress=1, Applied=2, Verified=3

-- ═══ INCIDENTS ════════════════════════════════════════════════════════════════

INSERT INTO oi_incidents (
  "Id", "ExternalRef", "Title", "Description",
  "Type", "Severity", "Status",
  "ServiceId", "ServiceName", "OwnerTeam", "ImpactedDomain", "Environment",
  "DetectedAt", "LastUpdatedAt",
  "HasCorrelation", "CorrelationConfidence", "MitigationStatus",
  "CorrelationAnalysis",
  "EvidenceTelemetrySummary", "EvidenceBusinessImpact", "EvidenceAnalysis", "EvidenceTemporalContext",
  "MitigationNarrative", "HasEscalationPath", "EscalationPath",
  "TimelineJson", "LinkedServicesJson",
  "CorrelatedChangesJson", "CorrelatedServicesJson", "CorrelatedDependenciesJson",
  "ImpactedContractsJson", "EvidenceObservationsJson",
  "MitigationActionsJson", "MitigationRecommendationsJson", "MitigationRecommendedRunbooksJson",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
-- INC-1: Payment Gateway — elevated error rate
(
  'a1b2c3d4-0001-0000-0000-000000000001',
  'INC-2026-0042',
  'Payment Gateway — elevated error rate',
  'Error rate increased to 8.2% after deployment of v2.14.0. Multiple payment flows affected.',
  0, 0, 2,
  'svc-payment-gateway', 'Payment Gateway', 'payment-squad', 'Payments', 'Production',
  NOW() - INTERVAL '3 hours', NOW() - INTERVAL '15 minutes',
  true, 2, 1,
  'Strong temporal correlation with deployment of v2.14.0 (deployed 3h ago). Error spike started 12 minutes after deployment.',
  'Error rate: 8.2% (baseline: 0.3%). P95 latency: 2.4s (baseline: 180ms). Affected endpoints: POST /payments, GET /payments/{id}.',
  'Estimated revenue impact: €12,400/hour. 847 failed transactions in last 3 hours.',
  'Root cause identified: Payment validation regression in v2.14.0. Null reference in PaymentValidator when optional discount field is missing.',
  'Error spike started at 14:12 UTC, 12 minutes after v2.14.0 deployment completed at 14:00 UTC.',
  'Rollback to v2.13.2 initiated. Recovery expected within 15 minutes.', true,
  'Payment Squad → Platform Engineering → VP Engineering',
  '[{"timestamp":"2026-03-16T11:00:00Z","description":"Incident detected — error rate threshold breached"},{"timestamp":"2026-03-16T11:30:00Z","description":"Investigation started — payment-squad notified"},{"timestamp":"2026-03-16T12:00:00Z","description":"Root cause identified — v2.14.0 introduced regression in payment validation"},{"timestamp":"2026-03-16T13:00:00Z","description":"Mitigation started — rollback initiated"},{"timestamp":"2026-03-16T13:45:00Z","description":"Rollback deployed — monitoring recovery"}]',
  '["Orders Service","Checkout Service","Refunds Service"]',
  '[{"changeId":"chg-001","version":"v2.14.0","deployedAt":"2026-03-16T10:00:00Z","confidence":"High"}]',
  '[{"serviceId":"svc-orders","serviceName":"Orders Service","impact":"High"},{"serviceId":"svc-checkout","serviceName":"Checkout Service","impact":"Medium"}]',
  '["PostgreSQL (payments-db)","Redis (session-cache)"]',
  '[{"contractId":"ctr-payments-api","contractName":"Payments API v2","impact":"Breaking"}]',
  '[{"category":"Telemetry","observation":"Error rate 8.2%","severity":"Critical"},{"category":"Business","observation":"847 failed transactions","severity":"Critical"}]',
  '[{"action":"Rollback to v2.13.2","status":"InProgress","owner":"payment-squad"},{"action":"Notify affected merchants","status":"Pending","owner":"support-team"}]',
  '[{"recommendation":"Immediate rollback to v2.13.2","confidence":"High","source":"ChangeCorrelation"},{"recommendation":"Add integration test for optional discount field","confidence":"Medium","source":"RootCauseAnalysis"}]',
  '[{"runbookId":"bb000001-0001-0000-0000-000000000001","title":"Payment Service Rollback"}]',
  NOW() - INTERVAL '3 hours', 'system', NOW() - INTERVAL '15 minutes', 'system', false
),
-- INC-2: Catalog Sync — stale data
(
  'a1b2c3d4-0002-0000-0000-000000000002',
  'INC-2026-0041',
  'Catalog Sync — stale data detected in search index',
  'Product search returning outdated prices after catalog bulk update. Elasticsearch reindex lag detected.',
  3, 1, 1,
  'svc-catalog-sync', 'Catalog Sync', 'catalog-team', 'Commerce', 'Production',
  NOW() - INTERVAL '6 hours', NOW() - INTERVAL '1 hour',
  true, 1, 0,
  'Temporal correlation with bulk catalog update job that ran 6 hours ago. Elasticsearch reindex queue backlog detected.',
  'Search index freshness: 6 hours behind. 12,450 products with stale pricing.', 'Customer-facing price discrepancies on ~2% of catalog.', 'Elasticsearch reindex consumer lag due to increased bulk update volume.', 'Lag started after scheduled bulk update at 08:00 UTC.',
  NULL, false, NULL,
  '[{"timestamp":"2026-03-16T08:00:00Z","description":"Bulk catalog update started"},{"timestamp":"2026-03-16T09:00:00Z","description":"Stale data detected in search results"},{"timestamp":"2026-03-16T13:00:00Z","description":"Investigation started — catalog-team notified"}]',
  '["Search Service","Pricing Service"]',
  '[{"changeId":"chg-002","version":"bulk-update-2026-03-16","deployedAt":"2026-03-16T08:00:00Z","confidence":"Medium"}]',
  '[{"serviceId":"svc-search","serviceName":"Search Service","impact":"Medium"}]',
  '["Elasticsearch (catalog-index)"]',
  '[]',
  '[{"category":"DataQuality","observation":"12,450 products with stale pricing","severity":"Major"}]',
  '[{"action":"Trigger manual reindex","status":"Pending","owner":"catalog-team"}]',
  '[{"recommendation":"Trigger manual Elasticsearch reindex","confidence":"High","source":"DataAnalysis"}]',
  '[]',
  NOW() - INTERVAL '6 hours', 'system', NOW() - INTERVAL '1 hour', 'system', false
),
-- INC-3: Inventory Consumer — processing failure
(
  'a1b2c3d4-0003-0000-0000-000000000003',
  'INC-2026-0040',
  'Inventory Consumer — Kafka consumer group rebalance failures',
  'Kafka consumer group for inventory updates experiencing frequent rebalances. Message processing delayed by 45 minutes.',
  2, 1, 3,
  'svc-inventory-consumer', 'Inventory Consumer', 'logistics-team', 'Logistics', 'Production',
  NOW() - INTERVAL '12 hours', NOW() - INTERVAL '2 hours',
  false, 4, 2,
  NULL,
  'Consumer lag: 45 minutes. Rebalance frequency: 3x/hour (normal: 0).', 'Inventory levels not updating in real-time. Overselling risk on high-demand items.', 'Consumer group rebalancing due to pod memory pressure.', 'Started after Kubernetes node scaling event at 02:00 UTC.',
  'Consumer group stabilized after pod memory limits increased.', false, NULL,
  '[{"timestamp":"2026-03-16T02:00:00Z","description":"Kafka consumer rebalance detected"},{"timestamp":"2026-03-16T06:00:00Z","description":"Alert triggered — consumer lag exceeded 30 minutes"},{"timestamp":"2026-03-16T10:00:00Z","description":"Pod memory limits increased"},{"timestamp":"2026-03-16T12:00:00Z","description":"Consumer group stabilized — monitoring"}]',
  '["Orders Service","Warehouse Service"]',
  '[]', '[]', '["Kafka (inventory-events)"]',
  '[]',
  '[{"category":"Infrastructure","observation":"Consumer lag 45 minutes","severity":"Major"},{"category":"Business","observation":"Overselling risk on high-demand items","severity":"Major"}]',
  '[{"action":"Increase pod memory limits","status":"Completed","owner":"logistics-team"}]',
  '[{"recommendation":"Increase consumer pod memory limits","confidence":"High","source":"InfraAnalysis"}]',
  '[]',
  NOW() - INTERVAL '12 hours', 'system', NOW() - INTERVAL '2 hours', 'system', false
),
-- INC-4: Order API — latency spike
(
  'a1b2c3d4-0004-0000-0000-000000000004',
  'INC-2026-0039',
  'Order API — P99 latency spike affecting checkout flow',
  'Order creation endpoint P99 latency increased from 200ms to 3.2s. Checkout conversion rate dropped 15%.',
  0, 0, 4,
  'svc-order-api', 'Order API', 'commerce-team', 'Commerce', 'Production',
  NOW() - INTERVAL '24 hours', NOW() - INTERVAL '4 hours',
  true, 3, 3,
  'Confirmed correlation with database connection pool exhaustion after traffic surge from marketing campaign.',
  'P99 latency: 3.2s (baseline: 200ms). Connection pool utilization: 98%.', 'Checkout conversion dropped 15%. Estimated lost revenue: €45,000.', 'Database connection pool saturated under traffic surge. Insufficient max connections configured.', 'Traffic surge started at 10:00 UTC coinciding with marketing campaign launch.',
  'Connection pool max increased from 50 to 200. Latency normalized.', true, 'Commerce Team → Database Team → CTO',
  '[{"timestamp":"2026-03-15T10:00:00Z","description":"Traffic surge detected from marketing campaign"},{"timestamp":"2026-03-15T10:30:00Z","description":"P99 latency spike detected"},{"timestamp":"2026-03-15T11:00:00Z","description":"DB connection pool exhaustion identified"},{"timestamp":"2026-03-15T12:00:00Z","description":"Connection pool max increased"},{"timestamp":"2026-03-15T14:00:00Z","description":"Latency normalized — incident resolved"}]',
  '["Checkout Service","Inventory Service"]',
  '[]',
  '[{"serviceId":"svc-checkout","serviceName":"Checkout Service","impact":"Critical"}]',
  '["PostgreSQL (orders-db)"]',
  '[]',
  '[{"category":"Performance","observation":"P99 latency 3.2s","severity":"Critical"},{"category":"Business","observation":"15% conversion drop","severity":"Critical"}]',
  '[{"action":"Increase DB connection pool","status":"Completed","owner":"commerce-team"}]',
  '[{"recommendation":"Increase connection pool max to 200","confidence":"Confirmed","source":"DBAnalysis"}]',
  '[]',
  NOW() - INTERVAL '24 hours', 'system', NOW() - INTERVAL '4 hours', 'system', false
),
-- INC-5: Notification Worker — queue backlog
(
  'a1b2c3d4-0005-0000-0000-000000000005',
  'INC-2026-0038',
  'Notification Worker — email queue backlog growing',
  'Email notification queue depth increased to 15,000. Processing rate dropped to 50/min (normal: 500/min).',
  4, 2, 0,
  'svc-notification-worker', 'Notification Worker', 'platform-team', 'Platform', 'Production',
  NOW() - INTERVAL '2 hours', NOW() - INTERVAL '30 minutes',
  false, 4, 0,
  NULL,
  'Queue depth: 15,000. Processing rate: 50/min (baseline: 500/min).', 'Order confirmation emails delayed. Customer complaints increasing.', 'SMTP provider rate limiting detected.', 'Queue growth started 2 hours ago.',
  NULL, false, NULL,
  '[{"timestamp":"2026-03-16T12:00:00Z","description":"Queue depth alert triggered"},{"timestamp":"2026-03-16T13:30:00Z","description":"SMTP rate limiting confirmed"}]',
  '["Orders Service"]',
  '[]', '[]', '["SMTP Provider (SendGrid)"]',
  '[]',
  '[{"category":"Infrastructure","observation":"Queue depth 15,000","severity":"Minor"},{"category":"Business","observation":"Order confirmation emails delayed","severity":"Minor"}]',
  '[]',
  '[{"recommendation":"Contact SMTP provider about rate limits","confidence":"Medium","source":"InfraAnalysis"}]',
  '[]',
  NOW() - INTERVAL '2 hours', 'system', NOW() - INTERVAL '30 minutes', 'system', false
),
-- INC-6: Auth Gateway — token validation intermittent failures
(
  'a1b2c3d4-0006-0000-0000-000000000006',
  'INC-2026-0037',
  'Auth Gateway — intermittent token validation failures',
  'JWT validation failing intermittently (~2% of requests). Affected users get 401 and must re-authenticate.',
  1, 2, 5,
  'svc-auth-gateway', 'Auth Gateway', 'security-team', 'Identity', 'Production',
  NOW() - INTERVAL '48 hours', NOW() - INTERVAL '24 hours',
  false, 4, 3,
  NULL,
  'Token validation failure rate: 2%. Affected users: ~340/day.', 'User friction: forced re-authentication. No data breach.', 'Clock skew between auth gateway pods causing token expiry validation edge cases.', 'Started after Kubernetes rolling update 48 hours ago.',
  'NTP sync fixed across all pods. Failure rate returned to baseline.', false, NULL,
  '[{"timestamp":"2026-03-14T14:00:00Z","description":"Token validation failures detected"},{"timestamp":"2026-03-14T16:00:00Z","description":"Clock skew identified between pods"},{"timestamp":"2026-03-15T10:00:00Z","description":"NTP sync applied"},{"timestamp":"2026-03-15T14:00:00Z","description":"Failure rate normalized — incident closed"}]',
  '[]',
  '[]', '[]', '[]',
  '[]',
  '[{"category":"Security","observation":"2% token validation failures","severity":"Minor"}]',
  '[{"action":"Fix NTP sync","status":"Completed","owner":"security-team"}]',
  '[{"recommendation":"Implement NTP sync monitoring","confidence":"High","source":"InfraAnalysis"}]',
  '[]',
  NOW() - INTERVAL '48 hours', 'system', NOW() - INTERVAL '24 hours', 'system', false
)
ON CONFLICT ("Id") DO NOTHING;

-- ═══ RUNBOOKS ═════════════════════════════════════════════════════════════════

INSERT INTO oi_runbooks (
  "Id", "Title", "Description",
  "LinkedService", "LinkedIncidentType",
  "StepsJson", "PrerequisitesJson", "PostNotes",
  "MaintainedBy", "PublishedAt", "LastReviewedAt",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'bb000001-0001-0000-0000-000000000001',
  'Payment Service Rollback',
  'Step-by-step procedure for rolling back the Payment Gateway to a previous stable version.',
  'svc-payment-gateway', 'ServiceDegradation',
  '[{"step":1,"title":"Verify current deployment","description":"Check current deployed version via kubectl get deployment payment-gateway -o jsonpath=''{.spec.template.spec.containers[0].image}''"},{"step":2,"title":"Initiate rollback","description":"Run: kubectl rollout undo deployment/payment-gateway"},{"step":3,"title":"Verify rollback","description":"Confirm previous version is running and health checks pass"},{"step":4,"title":"Monitor recovery","description":"Watch error rates and latency for 15 minutes post-rollback"}]',
  '["kubectl access to production cluster","Payment Gateway deployment permissions","Monitoring dashboard access"]',
  'After rollback, create a post-incident review ticket and notify the payment-squad channel.',
  'payment-squad', NOW() - INTERVAL '30 days', NOW() - INTERVAL '7 days',
  NOW() - INTERVAL '30 days', 'system', NOW() - INTERVAL '7 days', 'system', false
),
(
  'bb000002-0001-0000-0000-000000000001',
  'Kafka Consumer Recovery',
  'Procedure for recovering Kafka consumer groups experiencing rebalance storms or processing failures.',
  'svc-inventory-consumer', 'MessagingIssue',
  '[{"step":1,"title":"Check consumer group status","description":"Run: kafka-consumer-groups --bootstrap-server kafka:9092 --group inventory-consumers --describe"},{"step":2,"title":"Check pod health","description":"kubectl get pods -l app=inventory-consumer -o wide"},{"step":3,"title":"Restart consumers if needed","description":"kubectl rollout restart deployment/inventory-consumer"},{"step":4,"title":"Monitor lag reduction","description":"Watch consumer lag decrease to acceptable levels (<5 minutes)"}]',
  '["Kafka CLI access","kubectl access to production cluster"]',
  'If lag does not reduce within 30 minutes, escalate to platform-team.',
  'logistics-team', NOW() - INTERVAL '60 days', NOW() - INTERVAL '14 days',
  NOW() - INTERVAL '60 days', 'system', NOW() - INTERVAL '14 days', 'system', false
),
(
  'bb000003-0001-0000-0000-000000000001',
  'Database Connection Pool Tuning',
  'Procedure for diagnosing and resolving database connection pool exhaustion issues.',
  'svc-order-api', 'ServiceDegradation',
  '[{"step":1,"title":"Check connection pool metrics","description":"Query pg_stat_activity for active connections and wait events"},{"step":2,"title":"Identify connection consumers","description":"Check which services hold the most connections"},{"step":3,"title":"Increase pool size if needed","description":"Update connection pool max in service configuration and restart"},{"step":4,"title":"Verify improvement","description":"Monitor P99 latency and connection pool utilization"}]',
  '["PostgreSQL admin access","Service configuration access"]',
  'Document the new pool size and update capacity planning spreadsheet.',
  'database-team', NOW() - INTERVAL '90 days', NOW() - INTERVAL '30 days',
  NOW() - INTERVAL '90 days', 'system', NOW() - INTERVAL '30 days', 'system', false
)
ON CONFLICT ("Id") DO NOTHING;
