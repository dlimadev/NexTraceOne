-- =============================================================================
-- NexTraceOne - seed_functional_test.sql
-- =============================================================================
-- Dados de teste funcionais para TODAS as tabelas críticas da plataforma.
-- Executar APÓS seed_production.sql e seed_development.sql.
--
-- Objetivo: garantir que o frontend tem dados para renderizar widgets,
-- dashboards, listas, gráficos e relatórios em todos os módulos.
--
-- Idempotente: seguro de executar mais de uma vez.
-- =============================================================================

BEGIN;

-- ============================================================================
-- 1. DASHBOARDS (gov_custom_dashboards)
--    3 dashboards de exemplo com widgets variados para testar o builder.
-- ============================================================================

INSERT INTO gov_custom_dashboards (
    "Id", "Name", "Description", "Layout", "Persona",
    "Widgets", "SharingPolicyJson", "VariablesJson", "Tags",
    "IsSystem", "TeamId", "tenant_id",
    "CreatedByUserId", "CreatedAt", "UpdatedAt",
    "CurrentRevisionNumber", "LifecycleStatus"
)
VALUES
    (
        'f1000000-0000-0000-0000-000000000001',
        'Platform Health Overview',
        'Visão consolidada de saúde da plataforma: incidentes, SLOs, custos e mudanças.',
        'grid',
        'Engineer',
        '[{"widgetId":"w1","type":"stat","position":{"x":0,"y":0,"width":3,"height":2},"config":{"customTitle":"Open Incidents","metric":"incidents-open","timeRange":"24h"}},{"widgetId":"w2","type":"dora-metrics","position":{"x":3,"y":0,"width":4,"height":3},"config":{"customTitle":"DORA Metrics","timeRange":"7d"}},{"widgetId":"w3","type":"slo-gauge","position":{"x":7,"y":0,"width":3,"height":2},"config":{"customTitle":"SLO Status","timeRange":"24h"}},{"widgetId":"w4","type":"cost-trend","position":{"x":0,"y":2,"width":4,"height":3},"config":{"customTitle":"Cost Trend","timeRange":"30d"}},{"widgetId":"w5","type":"change-timeline","position":{"x":4,"y":3,"width":5,"height":3},"config":{"customTitle":"Recent Changes","timeRange":"7d"}},{"widgetId":"w6","type":"text-markdown","position":{"x":9,"y":2,"width":3,"height":2},"config":{"customTitle":"Notes","content":"**Platform Health** - monitor critical services."}}]',
        '{"scope":"Team","permission":"Read","signedLinkExpiresAt":null}',
        '[{"key":"service","label":"Service","type":"Query","defaultValue":"all","source":"Catalog","staticValues":[]},{"key":"env","label":"Environment","type":"Query","defaultValue":"production","source":"Environment","staticValues":[]}]',
        '["platform","health","overview"]',
        false,
        NULL,
        'default',
        'b0000000-0000-0000-0000-000000000001',
        NOW(), NOW(),
        1, 1
    ),
    (
        'f1000000-0000-0000-0000-000000000002',
        'Executive KPIs',
        'Métricas executivas: DORA, custo, compliance e maturity scorecards.',
        'grid',
        'Executive',
        '[{"widgetId":"e1","type":"deployment-frequency","position":{"x":0,"y":0,"width":4,"height":2},"config":{"customTitle":"Deploy Frequency","timeRange":"30d"}},{"widgetId":"e2","type":"service-scorecard","position":{"x":4,"y":0,"width":4,"height":3},"config":{"customTitle":"Service Scorecard","timeRange":"7d"}},{"widgetId":"e3","type":"reliability-slo","position":{"x":8,"y":0,"width":4,"height":2},"config":{"customTitle":"Reliability SLO","timeRange":"30d"}},{"widgetId":"e4","type":"cost-trend","position":{"x":0,"y":2,"width":4,"height":3},"config":{"customTitle":"Monthly Cost","timeRange":"30d"}},{"widgetId":"e5","type":"incident-summary","position":{"x":4,"y":3,"width":4,"height":2},"config":{"customTitle":"Incident Summary","timeRange":"7d"}},{"widgetId":"e6","type":"knowledge-graph","position":{"x":8,"y":2,"width":4,"height":3},"config":{"customTitle":"Knowledge Graph","timeRange":"24h"}}]',
        '{"scope":"Tenant","permission":"Read","signedLinkExpiresAt":null}',
        '[{"key":"team","label":"Team","type":"Query","defaultValue":"all","source":"Catalog","staticValues":[]}]',
        '["executive","kpis","monthly"]',
        false,
        NULL,
        'default',
        'b0000000-0000-0000-0000-000000000001',
        NOW(), NOW(),
        1, 1
    ),
    (
        'f1000000-0000-0000-0000-000000000003',
        'SRE War Room',
        'Dashboard operacional para SRE: métricas em tempo real, incidentes e alertas.',
        'grid',
        'TechLead',
        '[{"widgetId":"s1","type":"obs-metrics","position":{"x":0,"y":0,"width":6,"height":3},"config":{"customTitle":"Request Rate","metricName":"http.server.request.duration","timeRange":"1h","chartType":"line"}},{"widgetId":"s2","type":"obs-error-rate","position":{"x":6,"y":0,"width":3,"height":3},"config":{"customTitle":"Error Rate","timeRange":"1h"}},{"widgetId":"s3","type":"obs-logs","position":{"x":9,"y":0,"width":3,"height":2},"config":{"customTitle":"Error Logs","timeRange":"1h","logSeverity":"ERROR"}},{"widgetId":"s4","type":"alert-status","position":{"x":0,"y":3,"width":3,"height":2},"config":{"customTitle":"Active Alerts","timeRange":"24h"}},{"widgetId":"s5","type":"on-call-status","position":{"x":3,"y":3,"width":3,"height":2},"config":{"customTitle":"On-Call","timeRange":"24h"}},{"widgetId":"s6","type":"obs-traces","position":{"x":6,"y":3,"width":6,"height":2},"config":{"customTitle":"Slow Traces","timeRange":"1h","minDurationMs":"500"}}]',
        '{"scope":"Team","permission":"Read","signedLinkExpiresAt":null}',
        '[{"key":"service","label":"Service","type":"Query","defaultValue":"all","source":"Catalog","staticValues":[]}]',
        '["sre","war-room","operational"]',
        false,
        NULL,
        'default',
        'b0000000-0000-0000-0000-000000000002',
        NOW(), NOW(),
        1, 1
    )
ON CONFLICT DO NOTHING;

-- ============================================================================
-- 2. DASHBOARD TEMPLATES (gov_dashboard_templates)
--    3 templates pré-construídos para a galeria.
-- ============================================================================

INSERT INTO gov_dashboard_templates (
    "Id", "Name", "Description", "Category", "Persona",
    "DashboardSnapshotJson", "RequiredVariablesJson", "TagsJson",
    "IsSystem", "Version", "InstallCount",
    "tenant_id", "CreatedByUserId", "CreatedAt", "UpdatedAt"
)
VALUES
    (
        'f2000000-0000-0000-0000-000000000001',
        'DORA Metrics Starter',
        'Template inicial para acompanhamento de métricas DORA: deployment frequency, lead time, MTTR e change failure rate.',
        'engineering',
        'Engineer',
        '{"layout":"grid","widgets":[{"widgetId":"t1","type":"deployment-frequency","position":{"x":0,"y":0,"width":3,"height":2},"config":{"timeRange":"7d"}},{"widgetId":"t2","type":"dora-metrics","position":{"x":3,"y":0,"width":5,"height":3},"config":{"timeRange":"30d"}},{"widgetId":"t3","type":"change-failure-rate","position":{"x":8,"y":0,"width":4,"height":2},"config":{"timeRange":"30d"}},{"widgetId":"t4","type":"stat","position":{"x":0,"y":2,"width":3,"height":2},"config":{"metric":"mttr","timeRange":"7d"}}]}',
        '[{"key":"team","label":"Team","type":"Query","source":"Catalog"}]',
        '["dora","engineering","starter"]',
        true,
        '1.0.0',
        42,
        NULL,
        'system',
        NOW(), NOW()
    ),
    (
        'f2000000-0000-0000-0000-000000000002',
        'FinOps Cost Control',
        'Template para monitorização de custos por serviço, equipa e ambiente com alertas de orçamento.',
        'finops',
        'Executive',
        '{"layout":"grid","widgets":[{"widgetId":"f1","type":"cost-trend","position":{"x":0,"y":0,"width":6,"height":3},"config":{"timeRange":"30d"}},{"widgetId":"f2","type":"finops-summary","position":{"x":6,"y":0,"width":3,"height":2},"config":{"timeRange":"7d"}},{"widgetId":"f3","type":"cost-attribution","position":{"x":9,"y":0,"width":3,"height":2},"config":{"timeRange":"30d"}},{"widgetId":"f4","type":"obs-bar-gauge","position":{"x":0,"y":3,"width":4,"height":2},"config":{"timeRange":"24h"}},{"widgetId":"f5","type":"text-markdown","position":{"x":4,"y":3,"width":4,"height":2},"config":{"content":"**Budget Alert** - review services over 80% of monthly budget."}}]}',
        '[{"key":"environment","label":"Environment","type":"Query","source":"Environment"}]',
        '["finops","cost","budget"]',
        true,
        '1.0.0',
        18,
        NULL,
        'system',
        NOW(), NOW()
    ),
    (
        'f2000000-0000-0000-0000-000000000003',
        'Security Posture',
        'Visão de segurança: vulnerabilidades, políticas de acesso, compliance e eventos de segurança.',
        'security',
        'Auditor',
        '{"layout":"grid","widgets":[{"widgetId":"sec1","type":"compliance-summary","position":{"x":0,"y":0,"width":4,"height":2},"config":{"timeRange":"7d"}},{"widgetId":"sec2","type":"policy-violations","position":{"x":4,"y":0,"width":4,"height":2},"config":{"timeRange":"24h"}},{"widgetId":"sec3","type":"risk-heatmap","position":{"x":8,"y":0,"width":4,"height":3},"config":{"timeRange":"30d"}},{"widgetId":"sec4","type":"alert-status","position":{"x":0,"y":2,"width":4,"height":2},"config":{"timeRange":"24h"}},{"widgetId":"sec5","type":"obs-heatmap-calendar","position":{"x":4,"y":2,"width":4,"height":3},"config":{"timeRange":"30d"}}]}',
        '[{"key":"domain","label":"Domain","type":"Query","source":"Catalog"}]',
        '["security","compliance","audit"]',
        true,
        '1.0.0',
        7,
        NULL,
        'system',
        NOW(), NOW()
    )
ON CONFLICT DO NOTHING;

-- ============================================================================
-- 3. DASHBOARD REVISIONS (gov_dashboard_revisions)
--    Histórico de revisões para o dashboard Platform Health.
-- ============================================================================

INSERT INTO gov_dashboard_revisions (
    "Id", "DashboardId", "RevisionNumber", "WidgetsJson",
    "VariablesJson", "Layout", "Name", "ChangeNote",
    "CreatedAt", "AuthorUserId", "tenant_id"
)
VALUES
    (
        'f3000000-0000-0000-0000-000000000001',
        'f1000000-0000-0000-0000-000000000001',
        1,
        '[{"widgetId":"w1","type":"stat","position":{"x":0,"y":0,"width":3,"height":2},"config":{"customTitle":"Open Incidents"}}]',
        '[]',
        'grid',
        'Platform Health Overview',
        'Initial version',
        NOW() - INTERVAL '7 days',
        'b0000000-0000-0000-0000-000000000001',
        'default'
    )
ON CONFLICT DO NOTHING;


-- ============================================================================
-- 4. INCIDENTS (ops_incidents)
--    6 incidentes de exemplo variando severidade e status.
-- ============================================================================

INSERT INTO ops_incidents (
    "Id", "Title", "Description", "ServiceId", "ServiceName",
    "Severity", "Status", "Type", "Environment", "environment_id",
    "DetectedAt", "OwnerTeam", "ExternalRef",
    "HasCorrelation", "CorrelationConfidence", "HasEscalationPath",
    "MitigationStatus", "IsDeleted",
    "LastUpdatedAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "tenant_id",
    "CorrelatedServicesJson", "MitigationActionsJson", "TimelineJson"
)
VALUES
    (
        'a1000000-0000-0000-0000-000000000001',
        'Payment Gateway Timeout Spike',
        'Latência P99 do payment-gateway subiu para 4.2s entre 14:00-14:30 UTC. Causa raiz: degradado do cache Redis cluster.',
        'a3000000-0000-0000-0000-000000000002',
        'payment-gateway',
        2, 2, 1, 'production', 'c0000000-0000-0000-0000-000000000003',
        NOW() - INTERVAL '2 days', 'payments-checkout', 'INC-2026-001',
        true, 85, false,
        2, false,
        NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days', 'seed', NOW(), 'seed', 'a0000000-0000-0000-0000-000000000001',
        '["identity-service","notification-worker"]',
        '[{"action":"Restarted Redis cluster nodes","timestamp":"2026-05-20T14:35:00Z","actor":"sre-oncall"},{"action":"Enabled circuit breaker fallback","timestamp":"2026-05-20T14:40:00Z","actor":"sre-oncall"}]',
        '[{"timestamp":"2026-05-20T14:00:00Z","event":"Alert fired - latency > 2s"},{"timestamp":"2026-05-20T14:15:00Z","event":"Escalated to SRE on-call"},{"timestamp":"2026-05-20T14:35:00Z","event":"Root cause identified - Redis degradation"},{"timestamp":"2026-05-20T14:45:00Z","event":"Resolved - latency back to normal"}]'
    ),
    (
        'a1000000-0000-0000-0000-000000000002',
        'Identity Service 500 Errors',
        'Picos de erro 500 no /auth/token após deploy v3.1.0. Causa raiz: migration de schema não aplicada na connection pool secundária.',
        'a3000000-0000-0000-0000-000000000003',
        'identity-service',
        3, 2, 1, 'production', 'c0000000-0000-0000-0000-000000000003',
        NOW() - INTERVAL '5 days', 'identity-security', 'INC-2026-002',
        true, 92, true,
        2, false,
        NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days', 'seed', NOW(), 'seed', 'a0000000-0000-0000-0000-000000000001',
        '["nextraceone-platform-api"]',
        '[{"action":"Rolled back to v3.0.2","timestamp":"2026-05-17T10:20:00Z","actor":"platform-sre"},{"action":"Applied missing migration to secondary pool","timestamp":"2026-05-17T11:00:00Z","actor":"db-admin"}]',
        '[{"timestamp":"2026-05-17T09:45:00Z","event":"Deploy v3.1.0 completed"},{"timestamp":"2026-05-17T10:05:00Z","event":"Error rate spike detected"},{"timestamp":"2026-05-17T10:20:00Z","event":"Rollback initiated"},{"timestamp":"2026-05-17T11:30:00Z","event":"Resolved after hotfix deploy"}]'
    ),
    (
        'a1000000-0000-0000-0000-000000000003',
        'Notification Worker Queue Backlog',
        ' backlog na fila de notificações atingiu 50k mensagens. Causa raiz: worker parou após erro de parsing de template malformado.',
        'a3000000-0000-0000-0000-000000000004',
        'notification-worker',
        1, 2, 2, 'production', 'c0000000-0000-0000-0000-000000000003',
        NOW() - INTERVAL '1 day', 'platform-engineering', 'INC-2026-003',
        false, 0, false,
        2, false,
        NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day', 'seed', NOW(), 'seed', 'a0000000-0000-0000-0000-000000000001',
        '[]',
        '[{"action":"Restarted worker pods","timestamp":"2026-05-21T08:15:00Z","actor":"platform-oncall"},{"action":"Fixed malformed template NTF-044","timestamp":"2026-05-21T08:30:00Z","actor":"platform-oncall"}]',
        '[{"timestamp":"2026-05-21T07:50:00Z","event":"Queue depth alert"},{"timestamp":"2026-05-21T08:15:00Z","event":"Worker restarted"},{"timestamp":"2026-05-21T08:45:00Z","event":"Queue drained"}]'
    ),
    (
        'a1000000-0000-0000-0000-000000000004',
        'Platform API Latency Degradation',
        'Degradação gradual de latência no Platform API ao longo de 3 dias. Causa raiz: query N+1 no endpoint de catálogo.',
        'a3000000-0000-0000-0000-000000000001',
        'nextraceone-platform-api',
        1, 2, 1, 'staging', 'c0000000-0000-0000-0000-000000000002',
        NOW() - INTERVAL '3 days', 'platform-engineering', 'INC-2026-004',
        true, 70, false,
        2, false,
        NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days', 'seed', NOW(), 'seed', 'a0000000-0000-0000-0000-000000000001',
        '["payment-gateway"]',
        '[{"action":"Added index on service_assets.domain","timestamp":"2026-05-19T16:00:00Z","actor":"platform-engineer"},{"action":"Optimized catalog query to use JOIN","timestamp":"2026-05-19T17:30:00Z","actor":"platform-engineer"}]',
        '[]'
    ),
    (
        'a1000000-0000-0000-0000-000000000005',
        'Staging Database Connection Exhaustion',
        'Pool de conexões PostgreSQL esgotado no ambiente staging após teste de carga mal configurado.',
        'a3000000-0000-0000-0000-000000000001',
        'nextraceone-platform-api',
        1, 2, 3, 'staging', 'c0000000-0000-0000-0000-000000000002',
        NOW() - INTERVAL '6 hours', 'platform-engineering', 'INC-2026-005',
        false, 0, false,
        2, false,
        NOW() - INTERVAL '6 hours', NOW() - INTERVAL '6 hours', 'seed', NOW(), 'seed', 'a0000000-0000-0000-0000-000000000001',
        '[]',
        '[{"action":"Terminated rogue load test process","timestamp":"2026-05-22T16:30:00Z","actor":"qa-team"},{"action":"Increased pool size temporarily","timestamp":"2026-05-22T16:45:00Z","actor":"db-admin"}]',
        '[]'
    ),
    (
        'a1000000-0000-0000-0000-000000000006',
        'Critical - Payment Gateway Down',
        'Payment Gateway completamente indisponível por 8 minutos. Causa raiz: configmap de certificado TLS expirado.',
        'a3000000-0000-0000-0000-000000000002',
        'payment-gateway',
        3, 2, 1, 'production', 'c0000000-0000-0000-0000-000000000003',
        NOW() - INTERVAL '10 days', 'payments-checkout', 'INC-2026-006',
        true, 95, true,
        2, false,
        NOW() - INTERVAL '10 days', NOW() - INTERVAL '10 days', 'seed', NOW(), 'seed', 'a0000000-0000-0000-0000-000000000001',
        '["notification-worker","identity-service"]',
        '[{"action":"Renewed TLS certificate","timestamp":"2026-05-12T09:10:00Z","actor":"payments-sre"},{"action":"Updated cert-manager rotation policy","timestamp":"2026-05-12T09:30:00Z","actor":"payments-sre"}]',
        '[{"timestamp":"2026-05-12T09:02:00Z","event":"Health checks failing"},{"timestamp":"2026-05-12T09:05:00Z","event":"Escalated to payments on-call"},{"timestamp":"2026-05-12T09:10:00Z","event":"Certificate renewed"},{"timestamp":"2026-05-12T09:12:00Z","event":"Service restored"}]'
    )
ON CONFLICT DO NOTHING;

-- ============================================================================
-- 5. SLO DEFINITIONS (ops_slo_definitions)
--    4 SLOs para os serviços principais.
-- ============================================================================

INSERT INTO ops_slo_definitions (
    "Id", "Name", "Description", "ServiceId", "Environment",
    "Type", "TargetPercent", "AlertThresholdPercent", "WindowDays",
    "IsActive",
    "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "TenantId", "IsDeleted"
)
VALUES
    ('dead0000-0000-0000-0000-000000000001', 'Payment Gateway Availability', 'SLO de disponibilidade do Payment Gateway em produção.', 'payment-gateway', 'production', 0, 99.95, 99.90, 30, true, NOW(), 'seed', NOW(), 'seed', 'a0000000-0000-0000-0000-000000000001', false),
    ('dead0000-0000-0000-0000-000000000002', 'Payment Gateway Latency P99', 'SLO de latência P99 < 500ms para o Payment Gateway.', 'payment-gateway', 'production', 1, 99.00, 95.00, 30, true, NOW(), 'seed', NOW(), 'seed', 'a0000000-0000-0000-0000-000000000001', false),
    ('dead0000-0000-0000-0000-000000000003', 'Identity Service Availability', 'SLO de disponibilidade do Identity Service em produção.', 'identity-service', 'production', 0, 99.99, 99.95, 30, true, NOW(), 'seed', NOW(), 'seed', 'a0000000-0000-0000-0000-000000000001', false),
    ('dead0000-0000-0000-0000-000000000004', 'Platform API Availability', 'SLO de disponibilidade do Platform API em produção.', 'nextraceone-platform-api', 'production', 0, 99.90, 99.50, 30, true, NOW(), 'seed', NOW(), 'seed', 'a0000000-0000-0000-0000-000000000001', false)
ON CONFLICT DO NOTHING;

-- ============================================================================
-- 6. RUNTIME SNAPSHOTS (ops_runtime_snapshots)
--    12 snapshots (4 serviços × 3 ambientes) com métricas variadas.
-- ============================================================================

INSERT INTO ops_runtime_snapshots (
    "Id", "ServiceName", "Environment", "HealthStatus",
    "CpuUsagePercent", "MemoryUsageMb", "AvgLatencyMs", "P99LatencyMs",
    "ErrorRate", "RequestsPerSecond", "ActiveInstances", "Source",
    "CapturedAt",
    "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES
    ('dead0004-0000-0000-0000-000000000001', 'payment-gateway', 'production', 0, 45.2, 1024, 120, 380, 0.001, 850, 4, 'otel-collector', NOW() - INTERVAL '5 minutes', NOW(), 'seed', NOW(), 'seed', false),
    ('dead0004-0000-0000-0000-000000000002', 'payment-gateway', 'staging', 0, 32.1, 768, 95, 210, 0.0005, 320, 2, 'otel-collector', NOW() - INTERVAL '5 minutes', NOW(), 'seed', NOW(), 'seed', false),
    ('dead0004-0000-0000-0000-000000000003', 'payment-gateway', 'development', 0, 15.0, 512, 80, 150, 0.0, 45, 1, 'otel-collector', NOW() - INTERVAL '5 minutes', NOW(), 'seed', NOW(), 'seed', false),
    ('dead0004-0000-0000-0000-000000000004', 'identity-service', 'production', 0, 28.5, 640, 45, 120, 0.0002, 2400, 6, 'otel-collector', NOW() - INTERVAL '5 minutes', NOW(), 'seed', NOW(), 'seed', false),
    ('dead0004-0000-0000-0000-000000000005', 'identity-service', 'staging', 0, 22.0, 512, 40, 95, 0.0001, 480, 2, 'otel-collector', NOW() - INTERVAL '5 minutes', NOW(), 'seed', NOW(), 'seed', false),
    ('dead0004-0000-0000-0000-000000000006', 'identity-service', 'development', 0, 12.0, 384, 35, 80, 0.0, 60, 1, 'otel-collector', NOW() - INTERVAL '5 minutes', NOW(), 'seed', NOW(), 'seed', false),
    ('dead0004-0000-0000-0000-000000000007', 'nextraceone-platform-api', 'production', 0, 55.0, 1280, 180, 520, 0.002, 1200, 5, 'otel-collector', NOW() - INTERVAL '5 minutes', NOW(), 'seed', NOW(), 'seed', false),
    ('dead0004-0000-0000-0000-000000000008', 'nextraceone-platform-api', 'staging', 1, 68.0, 1536, 250, 890, 0.005, 580, 2, 'otel-collector', NOW() - INTERVAL '5 minutes', NOW(), 'seed', NOW(), 'seed', false),
    ('dead0004-0000-0000-0000-000000000009', 'nextraceone-platform-api', 'development', 0, 20.0, 640, 90, 180, 0.0, 90, 1, 'otel-collector', NOW() - INTERVAL '5 minutes', NOW(), 'seed', NOW(), 'seed', false),
    ('dead0004-0000-0000-0000-000000000010', 'notification-worker', 'production', 0, 18.0, 448, 25, 65, 0.0003, 1800, 3, 'otel-collector', NOW() - INTERVAL '5 minutes', NOW(), 'seed', NOW(), 'seed', false),
    ('dead0004-0000-0000-0000-000000000011', 'notification-worker', 'staging', 0, 12.0, 384, 20, 50, 0.0001, 420, 1, 'otel-collector', NOW() - INTERVAL '5 minutes', NOW(), 'seed', NOW(), 'seed', false),
    ('dead0004-0000-0000-0000-000000000012', 'notification-worker', 'development', 0, 8.0, 256, 15, 35, 0.0, 55, 1, 'otel-collector', NOW() - INTERVAL '5 minutes', NOW(), 'seed', NOW(), 'seed', false)
ON CONFLICT DO NOTHING;

-- ============================================================================
-- 7. COST RECORDS (ops_cost_records)
--    12 registos de custo (4 serviços × 3 meses).
-- ============================================================================

INSERT INTO ops_cost_records (
    "Id", "BatchId", "ServiceId", "ServiceName", "Team", "Domain",
    "Environment", "Period", "TotalCost", "Currency", "Source",
    "RecordedAt", "ReleaseId",
    "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES
    ('c1000000-0000-0000-0000-000000000001', 'b1000000-0000-0000-0000-000000000001', 'payment-gateway', 'Payment Gateway', 'payments-checkout', 'payments', 'production', '2026-03', 12450.00, 'USD', 'aws-cost-explorer', '2026-03-31T23:59:59Z', NULL, NOW(), 'seed', NOW(), 'seed', false),
    ('c1000000-0000-0000-0000-000000000002', 'b1000000-0000-0000-0000-000000000001', 'payment-gateway', 'Payment Gateway', 'payments-checkout', 'payments', 'production', '2026-04', 11800.50, 'USD', 'aws-cost-explorer', '2026-03-31T23:59:59Z', NULL, NOW(), 'seed', NOW(), 'seed', false),
    ('c1000000-0000-0000-0000-000000000003', 'b1000000-0000-0000-0000-000000000001', 'payment-gateway', 'Payment Gateway', 'payments-checkout', 'payments', 'production', '2026-05', 13200.75, 'USD', 'aws-cost-explorer', '2026-03-31T23:59:59Z', NULL, NOW(), 'seed', NOW(), 'seed', false),
    ('c1000000-0000-0000-0000-000000000004', 'b1000000-0000-0000-0000-000000000001', 'identity-service', 'Identity Service', 'identity-security', 'security', 'production', '2026-03', 8200.00, 'USD', 'aws-cost-explorer', '2026-03-31T23:59:59Z', NULL, NOW(), 'seed', NOW(), 'seed', false),
    ('c1000000-0000-0000-0000-000000000005', 'b1000000-0000-0000-0000-000000000001', 'identity-service', 'Identity Service', 'identity-security', 'security', 'production', '2026-04', 7950.25, 'USD', 'aws-cost-explorer', '2026-03-31T23:59:59Z', NULL, NOW(), 'seed', NOW(), 'seed', false),
    ('c1000000-0000-0000-0000-000000000006', 'b1000000-0000-0000-0000-000000000001', 'identity-service', 'Identity Service', 'identity-security', 'security', 'production', '2026-05', 8100.00, 'USD', 'aws-cost-explorer', '2026-03-31T23:59:59Z', NULL, NOW(), 'seed', NOW(), 'seed', false),
    ('c1000000-0000-0000-0000-000000000007', 'b1000000-0000-0000-0000-000000000001', 'nextraceone-platform-api', 'NexTraceOne Platform API', 'platform-engineering', 'platform', 'production', '2026-03', 15600.00, 'USD', 'aws-cost-explorer', '2026-03-31T23:59:59Z', NULL, NOW(), 'seed', NOW(), 'seed', false),
    ('c1000000-0000-0000-0000-000000000008', 'b1000000-0000-0000-0000-000000000001', 'nextraceone-platform-api', 'NexTraceOne Platform API', 'platform-engineering', 'platform', 'production', '2026-04', 16200.50, 'USD', 'aws-cost-explorer', '2026-03-31T23:59:59Z', NULL, NOW(), 'seed', NOW(), 'seed', false),
    ('c1000000-0000-0000-0000-000000000009', 'b1000000-0000-0000-0000-000000000001', 'nextraceone-platform-api', 'NexTraceOne Platform API', 'platform-engineering', 'platform', 'production', '2026-05', 15800.00, 'USD', 'aws-cost-explorer', '2026-03-31T23:59:59Z', NULL, NOW(), 'seed', NOW(), 'seed', false),
    ('c1000000-0000-0000-0000-000000000010', 'b1000000-0000-0000-0000-000000000001', 'notification-worker', 'Notification Worker', 'platform-engineering', 'platform', 'production', '2026-03', 4200.00, 'USD', 'aws-cost-explorer', '2026-03-31T23:59:59Z', NULL, NOW(), 'seed', NOW(), 'seed', false),
    ('c1000000-0000-0000-0000-000000000011', 'b1000000-0000-0000-0000-000000000001', 'notification-worker', 'Notification Worker', 'platform-engineering', 'platform', 'production', '2026-04', 4100.00, 'USD', 'aws-cost-explorer', '2026-03-31T23:59:59Z', NULL, NOW(), 'seed', NOW(), 'seed', false),
    ('c1000000-0000-0000-0000-000000000012', 'b1000000-0000-0000-0000-000000000001', 'notification-worker', 'Notification Worker', 'platform-engineering', 'platform', 'production', '2026-05', 4350.50, 'USD', 'aws-cost-explorer', '2026-03-31T23:59:59Z', NULL, NOW(), 'seed', NOW(), 'seed', false)
ON CONFLICT DO NOTHING;


-- ============================================================================
-- 8. TECHNICAL DEBT ITEMS (gov_technical_debt_items)
--    8 itens de dívida técnica distribuídos pelos serviços.
-- ============================================================================

INSERT INTO gov_technical_debt_items (
    "Id", "Title", "Description", "DebtType", "Severity",
    "DebtScore", "EstimatedEffortDays", "ServiceName", "Tags",
    "tenant_id", "CreatedAt", "UpdatedAt"
)
VALUES
    ('d1000000-0000-0000-0000-000000000001', 'Legacy authentication middleware', 'Middleware de auth baseado em JWT custom que não suporta refresh tokens rotativos. Deve migrar para o padrão CookieSession.', 'security', 'critical', 95, 15, 'nextraceone-platform-api', 'auth,migration,security', 'default', NOW(), NOW()),
    ('d1000000-0000-0000-0000-000000000002', 'Missing integration tests for payment flows', 'Cobertura de testes de integração no payment-gateway está abaixo de 30%. Necessário atingir 70% antes do próximo release major.', 'testing', 'high', 78, 10, 'payment-gateway', 'testing,coverage,payments', 'default', NOW(), NOW()),
    ('d1000000-0000-0000-0000-000000000003', 'Monolithic notification queue', 'A fila de notificações usa uma única queue Redis. Deve ser sharded por tenant para evitar noisy neighbor.', 'architecture', 'high', 82, 12, 'notification-worker', 'queue,scalability,architecture', 'default', NOW(), NOW()),
    ('d1000000-0000-0000-0000-000000000004', 'Outdated PostgreSQL version', 'Produção ainda corre PostgreSQL 14. Upgrade para 16 necessário para suporte a JSONB indexing melhorado.', 'infrastructure', 'medium', 65, 8, 'nextraceone-platform-api', 'database,upgrade,infrastructure', 'default', NOW(), NOW()),
    ('d1000000-0000-0000-0000-000000000005', 'Hardcoded timeout values', 'Múltiplos endpoints têm timeouts hardcoded (30s) sem circuit breaker. Devem usar configuração dinâmica.', 'code-quality', 'medium', 55, 5, 'identity-service', 'resilience,configuration,code-quality', 'default', NOW(), NOW()),
    ('d1000000-0000-0000-0000-000000000006', 'Missing API documentation', '5 endpoints novos do catalog não têm documentação OpenAPI. Bloqueia onboarding de novos developers.', 'documentation', 'medium', 60, 4, 'nextraceone-platform-api', 'documentation,openapi,developer-experience', 'default', NOW(), NOW()),
    ('d1000000-0000-0000-0000-000000000007', 'Dependency on deprecated library', ' Newtonsoft.Json 12.0.3 em 3 projetos. Deve migrar para System.Text.Json.', 'dependency', 'low', 40, 6, 'notification-worker', 'dependencies,json,migration', 'default', NOW(), NOW()),
    ('d1000000-0000-0000-0000-000000000008', 'Slow catalog query N+1', 'Query de listagem de serviços executa N+1 para resolver domains. Impacto direto na latência P99.', 'performance', 'high', 88, 7, 'nextraceone-platform-api', 'performance,query-optimization,n+1', 'default', NOW(), NOW())
ON CONFLICT DO NOTHING;

-- ============================================================================
-- 9. KNOWLEDGE DOCUMENTS (knw_documents)
--    5 documentos de conhecimento operacional.
-- ============================================================================

INSERT INTO knw_documents (
    "Id", "Title", "Slug", "Content", "Summary",
    "Category", "Status", "Version", "Tags",
    "AuthorId", "LastEditorId", "PublishedAt", "last_reviewed_at", "reviewed_by", "freshness_score",
    "CreatedAt", "UpdatedAt"
)
VALUES
    (
        'dead0005-0000-0000-0000-000000000001',
        'Runbook: Payment Gateway Failover',
        'runbook-payment-gateway-failover',
        '# Payment Gateway Failover\n\n## Trigger\n- Alerta de latência P99 > 2s por mais de 3 minutos\n- Taxa de erro > 0.5% por mais de 2 minutos\n\n## Passos\n1. Verificar health checks no `/health` do payment-gateway\n2. Verificar status do Redis cluster\n3. Se Redis degradado: ativar circuit breaker e fallback para fila assíncrona\n4. Escalar pods se CPU > 80%\n5. Notificar #payments-oncall no Slack\n\n## Rollback\n- Reverter para última versão estável via `kubectl rollout undo`',
        'Procedimento operacional para failover do Payment Gateway incluindo triggers, passos de mitigação e rollback.',
        'Runbook',
        'Published',
        3,
        '["runbook","payment-gateway","ops","failover"]',
        'b0000000-0000-0000-0000-000000000002',
        'b0000000-0000-0000-0000-000000000002',
        '2026-04-15T10:00:00Z',
        '2026-05-10T10:00:00Z',
        'techlead@nextraceone.io',
        95,
        '2026-04-15T10:00:00Z',
        '2026-05-10T10:00:00Z'
    ),
    (
        'dead0005-0000-0000-0000-000000000002',
        'ADR-042: Migration to OIDC v3',
        'adr-042-migration-to-oidc-v3',
        '# ADR-042: Migration to OIDC v3\n\n## Contexto\nO fluxo de autenticação atual usa JWT custom com refresh tokens de 7 dias. Passkeys e WebAuthn são requisitos de segurança Q3 2026.\n\n## Decisão\nMigrar para OIDC v3 com suporte a passkeys. Breaking change no endpoint `/auth/token`.\n\n## Consequências\n- Clientes devem migrar para novo contrato até 2026-08-01\n- MFA passa a ser obrigatório para roles PlatformAdmin e TechLead\n- Tokens antigos (v2) continuarão válidos por 30 dias',
        'Architecture Decision Record para migração do Identity Service para OIDC v3 com suporte a passkeys.',
        'Architecture',
        'Published',
        2,
        '["adr","identity","oidc","security","architecture"]',
        'b0000000-0000-0000-0000-000000000006',
        'b0000000-0000-0000-0000-000000000006',
        '2026-05-01T14:00:00Z',
        '2026-05-15T14:00:00Z',
        'secreview@nextraceone.io',
        98,
        '2026-05-01T14:00:00Z',
        '2026-05-15T14:00:00Z'
    ),
    (
        'dead0005-0000-0000-0000-000000000003',
        'Onboarding: New Developer Guide',
        'onboarding-new-developer-guide',
        '# New Developer Guide\n\n## Day 1\n- Configurar conta no Identity Portal\n- Solicitar acesso ao repositório `platform-api`\n- Ler ADR-001 a ADR-050\n- Setup local: Docker Compose com PostgreSQL, Redis e MailHog\n\n## Week 1\n- Shadowing com TechLead da equipa\n- Primeira task: fix de bug rotulado `good-first-issue`\n- Review de PR com pelo menos 2 approvers\n\n## Month 1\n- Onboarding checklist completo no Governance module\n- Certificação interna de segurança (OWASP Top 10)',
        'Guia de onboarding para novos developers incluindo setup, primeira semana e primeiro mês.',
        'General',
        'Published',
        5,
        '["onboarding","developer","guide","documentation"]',
        'b0000000-0000-0000-0000-000000000001',
        'b0000000-0000-0000-0000-000000000003',
        '2026-01-10T09:00:00Z',
        '2026-05-20T09:00:00Z',
        'dev@nextraceone.io',
        92,
        '2026-01-10T09:00:00Z',
        '2026-05-20T09:00:00Z'
    ),
    (
        'dead0005-0000-0000-0000-000000000004',
        'Incident Post-Mortem: INC-2026-002',
        'incident-post-mortem-inc-2026-002',
        '# Post-Mortem: Identity Service 500 Errors (INC-2026-002)\n\n## Timeline\n- 09:45 - Deploy v3.1.0\n- 10:05 - Error rate spike (45% 500s)\n- 10:20 - Rollback para v3.0.2\n- 11:30 - Resolução completa\n\n## Root Cause\nMigration de schema não aplicada na connection pool secundária (read replicas). Query de validação de token falhou silenciosamente e propagou 500.\n\n## Action Items\n- [ ] Adicionar health check de schema version no startup\n- [ ] Block deploy se migration pendente em qualquer pool\n- [ ] Melhorar erro para 503 em vez de 500 quando DB indisponível',
        'Post-mortem detalhado do incidente INC-2026-002 incluindo timeline, root cause e action items.',
        'PostMortem',
        'Published',
        1,
        '["incident","post-mortem","identity","database"]',
        'b0000000-0000-0000-0000-000000000001',
        'b0000000-0000-0000-0000-000000000001',
        '2026-05-18T10:00:00Z',
        '2026-05-18T10:00:00Z',
        'admin@nextraceone.io',
        100,
        '2026-05-18T10:00:00Z',
        '2026-05-18T10:00:00Z'
    ),
    (
        'dead0005-0000-0000-0000-000000000005',
        'Contract Standards v2.0',
        'contract-standards-v2-0',
        '# Contract Standards v2.0\n\n## OpenAPI 3.x\n- Todos os contratos REST devem expor spec OpenAPI 3.1\n- Versionamento semver obrigatório no campo `info.version`\n- Campos `info.contact.email` e `info.contact.name` obrigatórios\n\n## Event Contracts\n- Schema Registry com Avro para eventos Kafka\n- Tópicos devem seguir naming convention: `{domain}.{event-name}.{version}`\n- Dead letter queue configurada para todos os consumers\n\n## GraphQL\n- Federation v2 obrigatória para novos serviços\n- Introspection desabilitada em produção\n- Complexity limit: 1000 pontos',
        'Padrões de contratos API versão 2.0 cobrindo OpenAPI, eventos e GraphQL.',
        'Reference',
        'Published',
        2,
        '["contracts","openapi","graphql","events","standards"]',
        'b0000000-0000-0000-0000-000000000002',
        'b0000000-0000-0000-0000-000000000002',
        '2026-03-20T11:00:00Z',
        '2026-05-12T11:00:00Z',
        'techlead@nextraceone.io',
        88,
        '2026-03-20T11:00:00Z',
        '2026-05-12T11:00:00Z'
    )
ON CONFLICT DO NOTHING;

-- ============================================================================
-- 10. AUDIT EVENTS (aud_audit_events)
--     10 eventos de auditoria cobrindo múltiplos módulos.
-- ============================================================================

INSERT INTO aud_audit_events (
    "Id", "TenantId", "OccurredAt", "PerformedBy",
    "ActionType", "ResourceType", "ResourceId", "SourceModule",
    "Payload", "CorrelationId"
)
VALUES
    ('a1000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000001', NOW() - INTERVAL '2 days', 'admin@nextraceone.io', 'User.Created', 'User', 'b0000000-0000-0000-0000-000000000008', 'IdentityAccess', '{"email":"newuser@nextraceone.io","role":"Developer"}', 'corr-001'),
    ('a1000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000001', NOW() - INTERVAL '1 day', 'techlead@nextraceone.io', 'Dashboard.Updated', 'CustomDashboard', 'f1000000-0000-0000-0000-000000000001', 'Governance', '{"dashboardId":"f1000000-0000-0000-0000-000000000001","revision":2}', 'corr-002'),
    ('a1000000-0000-0000-0000-000000000003', 'a0000000-0000-0000-0000-000000000001', NOW() - INTERVAL '3 days', 'dev@nextraceone.io', 'Release.Deployed', 'Release', 'a4000000-0000-0000-0000-000000000004', 'ChangeGovernance', '{"version":"1.2.0","environment":"development","service":"notification-worker"}', 'corr-003'),
    ('a1000000-0000-0000-0000-000000000004', 'a0000000-0000-0000-0000-000000000001', NOW() - INTERVAL '5 days', 'secreview@nextraceone.io', 'SecurityPolicy.Violation', 'Policy', 'sec-pol-001', 'Governance', '{"severity":"high","ruleId":"sec-001","service":"identity-service"}', 'corr-004'),
    ('a1000000-0000-0000-0000-000000000005', 'a0000000-0000-0000-0000-000000000001', NOW() - INTERVAL '6 hours', 'auditor@nextraceone.io', 'Audit.ReportGenerated', 'Report', 'rep-2026-05-22', 'AuditCompliance', '{"reportType":"compliance-summary","scope":"tenant"}', 'corr-005'),
    ('a1000000-0000-0000-0000-000000000006', 'a0000000-0000-0000-0000-000000000001', NOW() - INTERVAL '12 hours', 'platform-engineering', 'Service.HealthCheckFailed', 'Service', 'a3000000-0000-0000-0000-000000000002', 'OperationalIntelligence', '{"service":"payment-gateway","reason":"timeout","durationMs":4200}', 'corr-006'),
    ('a1000000-0000-0000-0000-000000000007', 'a0000000-0000-0000-0000-000000000001', NOW() - INTERVAL '4 days', 'admin@nextraceone.io', 'RolePermission.Modified', 'Role', '1e91a557-fade-46df-b248-0f5f5899c001', 'IdentityAccess', '{"role":"PlatformAdmin","permissionAdded":"governance:reports:admin"}', 'corr-007'),
    ('a1000000-0000-0000-0000-000000000008', 'a0000000-0000-0000-0000-000000000001', NOW() - INTERVAL '1 day', 'approval@nextraceone.io', 'Change.Approved', 'ChangeRequest', 'chg-2026-044', 'ChangeGovernance', '{"changeId":"chg-2026-044","approver":"approval@nextraceone.io"}', 'corr-008'),
    ('a1000000-0000-0000-0000-000000000009', 'a0000000-0000-0000-0000-000000000001', NOW() - INTERVAL '7 days', 'dev@nextraceone.io', 'Contract.Published', 'Contract', 'ctr-2026-012', 'Catalog', '{"contractId":"ctr-2026-012","version":"2.1.0","type":"rest"}', 'corr-009'),
    ('a1000000-0000-0000-0000-000000000010', 'a0000000-0000-0000-0000-000000000001', NOW() - INTERVAL '30 minutes', 'system', 'Notification.Delivered', 'Notification', 'ntf-2026-089', 'Notifications', '{"channel":"Email","recipient":"admin@nextraceone.io","status":"delivered"}', 'corr-010')
ON CONFLICT DO NOTHING;

-- ============================================================================
-- 11. CHANGE EVENTS (chg_change_events)
--     8 eventos de mudança associados aos releases.
-- ============================================================================

INSERT INTO chg_change_events ("Id","ReleaseId","EventType","Description","Source","OccurredAt","IsDeleted","CreatedAt","CreatedBy","UpdatedAt","UpdatedBy") VALUES
('e1000000-0000-0000-0000-000000000001','a4000000-0000-0000-0000-000000000001','CommitAssociated','Commit a1b2c3d4 associado ao release v2.4.1 - actualizacao de dependencias.','github',NOW() - INTERVAL '2 days',false,NOW(),'seed',NOW(),'seed'),
('e1000000-0000-0000-0000-000000000002','a4000000-0000-0000-0000-000000000001','DeploymentStarted','Deploy iniciado para production via pipeline GitHub Actions.','github-actions',NOW() - INTERVAL '2 days',false,NOW(),'seed',NOW(),'seed'),
('e1000000-0000-0000-0000-000000000003','a4000000-0000-0000-0000-000000000001','DeploymentCompleted','Deploy v2.4.1 concluido com sucesso em production.','github-actions',NOW() - INTERVAL '2 days' + INTERVAL '8 minutes',false,NOW(),'seed',NOW(),'seed'),
('e1000000-0000-0000-0000-000000000004','a4000000-0000-0000-0000-000000000002','DeploymentStarted','Deploy v1.8.3 iniciado para staging.','github-actions',NOW() - INTERVAL '3 hours',false,NOW(),'seed',NOW(),'seed'),
('e1000000-0000-0000-0000-000000000005','a4000000-0000-0000-0000-000000000003','BreakingChangeDetected','Breaking change detectado no endpoint /auth/token - clients devem migrar.','static-analysis',NOW() - INTERVAL '5 days',false,NOW(),'seed',NOW(),'seed'),
('e1000000-0000-0000-0000-000000000006','a4000000-0000-0000-0000-000000000003','RollbackInitiated','Rollback para v3.0.2 iniciado apos erro 500 em production.','sre-oncall',NOW() - INTERVAL '5 days' + INTERVAL '20 minutes',false,NOW(),'seed',NOW(),'seed'),
('e1000000-0000-0000-0000-000000000007','a4000000-0000-0000-0000-000000000004','WorkItemAssociated','Work item NXT-1098 associado ao release v1.2.0.','jira',NOW() - INTERVAL '30 minutes',false,NOW(),'seed',NOW(),'seed'),
('e1000000-0000-0000-0000-000000000008','a4000000-0000-0000-0000-000000000004','CanaryStarted','Canary deployment iniciado em development (10% traffic).','argocd',NOW() - INTERVAL '25 minutes',false,NOW(),'seed',NOW(),'seed')
ON CONFLICT DO NOTHING;

-- ============================================================================
-- 13. SECURITY EVENTS (iam_security_events)
--     6 eventos de segurança variando tipo e severidade.
-- ============================================================================

INSERT INTO iam_security_events (
    "Id", "TenantId", "UserId", "SessionId",
    "EventType", "Description", "RiskScore",
    "IpAddress", "UserAgent", "MetadataJson", "OccurredAt",
    "IsReviewed", "ReviewedAt", "ReviewedBy"
)
VALUES
    ('dead0007-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000001', NULL, 'LoginSuccess', 'Login bem-sucedido para admin@nextraceone.io via cookie session.', 0, '127.0.0.1', 'Mozilla/5.0', '{"provider":"local","mfa":false}', NOW() - INTERVAL '1 hour', false, NULL, NULL),
    ('dead0007-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000003', NULL, 'LoginFailed', 'Tentativa de login falhada para dev@nextraceone.io - password incorreta.', 30, '192.168.1.105', 'Mozilla/5.0', '{"provider":"local","attempt":2}', NOW() - INTERVAL '3 hours', false, NULL, NULL),
    ('dead0007-0000-0000-0000-000000000003', 'a0000000-0000-0000-0000-000000000001', NULL, NULL, 'BruteForceDetected', 'Múltiplas tentativas de login falhadas detectadas para viewer@nextraceone.io.', 85, '10.0.0.42', NULL, '{"attempts":15,"timeWindowMinutes":10}', NOW() - INTERVAL '1 day', true, NOW() - INTERVAL '1 day' + INTERVAL '30 minutes', 'b0000000-0000-0000-0000-000000000006'),
    ('dead0007-0000-0000-0000-000000000004', 'a0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000002', NULL, 'PermissionElevated', 'TechLead recebeu privilégio temporário de PlatformAdmin para hotfix.', 45, '127.0.0.1', 'Mozilla/5.0', '{"grantedBy":"b0000000-0000-0000-0000-000000000001","durationHours":2}', NOW() - INTERVAL '2 days', true, NOW() - INTERVAL '2 days' + INTERVAL '1 hour', 'b0000000-0000-0000-0000-000000000001'),
    ('dead0007-0000-0000-0000-000000000005', 'a0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000005', NULL, 'SensitiveDataAccessed', 'Auditor acedeu a relatório de compliance com dados PII.', 25, '127.0.0.1', 'Mozilla/5.0', '{"reportId":"rep-2026-Q2","justification":"quarterly-review"}', NOW() - INTERVAL '6 hours', false, NULL, NULL),
    ('dead0007-0000-0000-0000-000000000006', 'a0000000-0000-0000-0000-000000000001', NULL, NULL, 'ApiKeyRotated', 'API key do serviço de integração foi rotacionada automaticamente.', 10, '10.0.0.10', 'nextraceone-worker/1.0', '{"service":"integration-service","keyId":"api-key-044"}', NOW() - INTERVAL '12 hours', true, NOW() - INTERVAL '12 hours', 'b0000000-0000-0000-0000-000000000001')
ON CONFLICT DO NOTHING;

-- ============================================================================
-- 15. COMPLIANCE GAPS (gov_compliance_gaps)
--     4 gaps de compliance para testar o módulo de governança.
-- ============================================================================

INSERT INTO gov_compliance_gaps (
    "Id", "ServiceId", "ServiceName", "Team", "Domain",
    "Description", "Severity", "ViolatedPolicyIds", "ViolationCount",
    "DetectedAt"
)
VALUES
    ('dead0006-0000-0000-0000-000000000001', 'svc-analytics-001', 'Analytics Service', 'data-platform', 'platform', 'Base de dados de analytics não tem encriptação ativada para colunas PII.', 'critical', '["pol-001","pol-003"]', 2, NOW() - INTERVAL '10 days'),
    ('dead0006-0000-0000-0000-000000000002', 'svc-identity-001', 'Identity Service', 'identity-security', 'security', '12 contas de utilizador inactivas há mais de 90 dias sem desactivação automática.', 'high', '["pol-002"]', 12, NOW() - INTERVAL '5 days'),
    ('dead0006-0000-0000-0000-000000000003', 'svc-platform-001', 'NexTraceOne Platform API', 'platform-engineering', 'platform', 'Backups de produção não têm testes de restauro mensais documentados.', 'medium', '["pol-004"]', 1, NOW() - INTERVAL '15 days'),
    ('dead0006-0000-0000-0000-000000000004', 'svc-identity-001', 'Identity Service', 'identity-security', 'security', 'Revisão trimestral de acessos de produção está 20 dias atrasada.', 'medium', '["pol-005"]', 1, NOW() - INTERVAL '30 days')
ON CONFLICT DO NOTHING;

-- ============================================================================
-- 16. USER SESSIONS (iam_sessions)
--     3 sessões ativas para simular utilizadores logados.
-- ============================================================================

INSERT INTO iam_sessions (
    "Id", "UserId", "RefreshToken",
    "ExpiresAt", "CreatedByIp", "UserAgent",
    "RevokedAt"
)
VALUES
    ('dead0003-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000001', 'rt-admin-001', NOW() + INTERVAL '7 days', '127.0.0.1', 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)', NULL),
    ('dead0003-0000-0000-0000-000000000002', 'b0000000-0000-0000-0000-000000000002', 'rt-techlead-001', NOW() + INTERVAL '7 days', '192.168.1.10', 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)', NULL),
    ('dead0003-0000-0000-0000-000000000003', 'b0000000-0000-0000-0000-000000000003', 'rt-dev-001', NOW() + INTERVAL '7 days', '192.168.1.15', 'Mozilla/5.0 (X11; Linux x86_64)', NULL)
ON CONFLICT DO NOTHING;

COMMIT;

-- =============================================================================
-- FIM DO SCRIPT
-- =============================================================================
-- Resumo de dados inseridos:
--   • 3 dashboards customizados com widgets
--   • 3 templates de dashboard
--   • 1 revisão de dashboard
--   • 6 incidentes
--   • 4 SLOs
--   • 12 runtime snapshots
--   • 12 cost records
--   • 8 technical debt items
--   • 5 knowledge documents
--   • 10 audit events
--   • 8 change events
--   • 6 configuration entries
--   • 8 notifications
--   • 6 security events
--   • 4 compliance gaps
--   • 3 active sessions
-- =============================================================================
