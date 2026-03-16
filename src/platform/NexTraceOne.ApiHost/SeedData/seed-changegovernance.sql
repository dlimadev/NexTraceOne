-- ═══════════════════════════════════════════════════════════════════════════════
-- NEXTRACEONE — Seed data: Change Governance Module (nextraceone_changegovernance)
-- Tabelas: ci_releases, ci_blast_radius_reports, ci_change_scores, ci_change_events,
--          rg_rulesets, rg_ruleset_bindings,
--          wf_workflow_templates, wf_workflow_instances, wf_workflow_stages,
--          wf_approval_decisions, wf_sla_policies, wf_evidence_packs,
--          prm_deployment_environments, prm_promotion_gates, prm_promotion_requests,
--          prm_gate_evaluations
-- ═══════════════════════════════════════════════════════════════════════════════

-- ═══ CHANGE INTELLIGENCE — Releases, Blast Radius, Change Scores ══════════════

-- Releases (Status: 0=Pending, 1=Running, 2=Succeeded, 3=Failed, 4=RolledBack)
-- ChangeLevel: 0=None, 1=Patch, 2=Minor, 3=Major, 4=Breaking
INSERT INTO ci_releases ("Id", "ApiAssetId", "ServiceName", "Version", "Environment", "PipelineSource", "CommitSha", "ChangeLevel", "Status", "ChangeScore", "CreatedAt", "ChangeType", "ConfidenceStatus", "ValidationStatus")
VALUES
  ('30000000-0000-0000-0000-000000000001', 'd0000000-0000-0000-0000-000000000001', 'Orders Service', '1.3.0', 'Production', 'https://ci.nextraceone.dev/pipelines/1001', 'abc123def456', 2, 2, 0.7200, '2025-05-15T15:00:00Z', 0, 0, 0),
  ('30000000-0000-0000-0000-000000000002', 'd0000000-0000-0000-0000-000000000002', 'Payments Service', '2.1.0', 'Staging', 'https://ci.nextraceone.dev/pipelines/1002', 'def456abc789', 2, 1, 0.6500, '2025-05-20T17:00:00Z', 0, 0, 0),
  ('30000000-0000-0000-0000-000000000003', 'd0000000-0000-0000-0000-000000000003', 'Inventory Service', '1.0.1', 'Production', 'https://ci.nextraceone.dev/pipelines/1003', '111222333444', 1, 2, 0.3000, '2025-05-10T09:00:00Z', 0, 0, 0),
  ('30000000-0000-0000-0000-000000000004', 'd0000000-0000-0000-0000-000000000004', 'Notifications Service', '1.2.0', 'Development', 'https://ci.nextraceone.dev/pipelines/1004', 'aaa111bbb222', 2, 0, 0.5500, '2025-06-01T08:30:00Z', 0, 0, 0),
  ('30000000-0000-0000-0000-000000000005', 'd0000000-0000-0000-0000-000000000001', 'Orders Service', '1.2.1', 'Production', 'https://ci.nextraceone.dev/pipelines/995', '01d111e1d222', 1, 2, 0.2000, '2025-04-20T12:00:00Z', 0, 0, 0),
  ('30000000-0000-0000-0000-000000000006', 'd0000000-0000-0000-0000-000000000005', 'Users Service', '1.1.0', 'Staging', 'https://ci.nextraceone.dev/pipelines/1005', 'ccc333ddd444', 3, 0, 0.8500, '2025-06-01T10:00:00Z', 0, 0, 0),
  ('30000000-0000-0000-0000-000000000007', 'd0000000-0000-0000-0000-000000000006', 'Shipping Service', '1.0.0', 'Production', 'https://ci.nextraceone.dev/pipelines/1006', 'eee555fff666', 2, 2, 0.4500, '2025-04-15T10:00:00Z', 0, 0, 0),
  ('30000000-0000-0000-0000-000000000008', 'd0000000-0000-0000-0000-000000000008', 'Gateway Service', '2.0.0', 'Staging', 'https://ci.nextraceone.dev/pipelines/1007', 'aab112ccf334', 4, 1, 0.9200, '2025-06-01T11:00:00Z', 0, 0, 0),
  ('30000000-0000-0000-0000-000000000009', 'd0000000-0000-0000-0000-000000000009', 'Search Service', '1.2.0', 'Production', 'https://ci.nextraceone.dev/pipelines/1008', 'bbc223dde445', 2, 2, 0.5000, '2025-05-25T12:00:00Z', 0, 0, 0),
  ('30000000-0000-0000-0000-000000000010', 'd0000000-0000-0000-0000-000000000010', 'Pricing Service', '1.1.0', 'Development', 'https://ci.nextraceone.dev/pipelines/1009', 'ccd334eef556', 2, 0, 0.4000, '2025-06-02T10:00:00Z', 0, 0, 0),
  ('30000000-0000-0000-0000-000000000011', 'd0000000-0000-0000-0000-000000000007', 'Analytics Service', '1.0.0', 'Production', 'https://ci.nextraceone.dev/pipelines/1010', 'dde445ffa667', 2, 2, 0.3500, '2025-04-20T15:00:00Z', 0, 0, 0),
  ('30000000-0000-0000-0000-000000000012', 'd0000000-0000-0000-0000-000000000001', 'Orders Service', '1.4.0', 'Development', 'https://ci.nextraceone.dev/pipelines/1011', 'eef556aab778', 2, 3, 0.6000, '2025-06-02T14:00:00Z', 0, 0, 0)
ON CONFLICT DO NOTHING;

-- Blast Radius Reports
INSERT INTO ci_blast_radius_reports ("Id", "ReleaseId", "ApiAssetId", "TotalAffectedConsumers", "DirectConsumers", "TransitiveConsumers", "CalculatedAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  ('31000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', 'd0000000-0000-0000-0000-000000000001', 5, '["Payments Service","Inventory Service","Mobile App","Shipping Service","Admin Dashboard"]', '[]', '2025-05-15T15:10:00Z', '2025-05-15T15:10:00Z', 'system', '2025-05-15T15:10:00Z', 'system', false),
  ('31000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000002', 'd0000000-0000-0000-0000-000000000002', 3, '["Orders Service","Notifications Service","Mobile App"]', '[]', '2025-05-20T17:10:00Z', '2025-05-20T17:10:00Z', 'system', '2025-05-20T17:10:00Z', 'system', false),
  ('31000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000007', 'd0000000-0000-0000-0000-000000000006', 2, '["Orders Service","Mobile App"]', '[]', '2025-04-15T10:10:00Z', '2025-04-15T10:10:00Z', 'system', '2025-04-15T10:10:00Z', 'system', false),
  ('31000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000008', 'd0000000-0000-0000-0000-000000000008', 4, '["Users Service","Pricing Service"]', '["Orders Service","Mobile App"]', '2025-06-01T11:10:00Z', '2025-06-01T11:10:00Z', 'system', '2025-06-01T11:10:00Z', 'system', false),
  ('31000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-000000000009', 'd0000000-0000-0000-0000-000000000009', 2, '["Mobile App","Admin Dashboard"]', '[]', '2025-05-25T12:10:00Z', '2025-05-25T12:10:00Z', 'system', '2025-05-25T12:10:00Z', 'system', false),
  ('31000000-0000-0000-0000-000000000006', '30000000-0000-0000-0000-000000000006', 'd0000000-0000-0000-0000-000000000005', 1, '["Gateway Service"]', '[]', '2025-06-01T10:10:00Z', '2025-06-01T10:10:00Z', 'system', '2025-06-01T10:10:00Z', 'system', false)
ON CONFLICT DO NOTHING;

-- Change Scores
INSERT INTO ci_change_scores ("Id", "ReleaseId", "Score", "BreakingChangeWeight", "BlastRadiusWeight", "EnvironmentWeight", "ComputedAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  ('32000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', 0.7200, 0.0000, 0.4500, 0.2700, '2025-05-15T15:15:00Z', '2025-05-15T15:15:00Z', 'system', '2025-05-15T15:15:00Z', 'system', false),
  ('32000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000002', 0.6500, 0.0000, 0.3500, 0.3000, '2025-05-20T17:15:00Z', '2025-05-20T17:15:00Z', 'system', '2025-05-20T17:15:00Z', 'system', false),
  ('32000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000003', 0.3000, 0.0000, 0.1500, 0.1500, '2025-05-10T09:10:00Z', '2025-05-10T09:10:00Z', 'system', '2025-05-10T09:10:00Z', 'system', false),
  ('32000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000007', 0.4500, 0.0000, 0.2000, 0.2500, '2025-04-15T10:15:00Z', '2025-04-15T10:15:00Z', 'system', '2025-04-15T10:15:00Z', 'system', false),
  ('32000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-000000000008', 0.9200, 0.5000, 0.2500, 0.1700, '2025-06-01T11:15:00Z', '2025-06-01T11:15:00Z', 'system', '2025-06-01T11:15:00Z', 'system', false),
  ('32000000-0000-0000-0000-000000000006', '30000000-0000-0000-0000-000000000009', 0.5000, 0.0000, 0.2500, 0.2500, '2025-05-25T12:15:00Z', '2025-05-25T12:15:00Z', 'system', '2025-05-25T12:15:00Z', 'system', false),
  ('32000000-0000-0000-0000-000000000007', '30000000-0000-0000-0000-000000000006', 0.8500, 0.4000, 0.2000, 0.2500, '2025-06-01T10:15:00Z', '2025-06-01T10:15:00Z', 'system', '2025-06-01T10:15:00Z', 'system', false),
  ('32000000-0000-0000-0000-000000000008', '30000000-0000-0000-0000-000000000011', 0.3500, 0.0000, 0.1500, 0.2000, '2025-04-20T15:15:00Z', '2025-04-20T15:15:00Z', 'system', '2025-04-20T15:15:00Z', 'system', false)
ON CONFLICT DO NOTHING;

-- Change Events
INSERT INTO ci_change_events ("Id", "ReleaseId", "EventType", "Description", "OccurredAt", "Source", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  ('33000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', 'ContractChanged', 'New endpoints added to Orders API v1.3.0', '2025-05-15T14:30:00Z', 'ContractDiff', '2025-05-15T14:30:00Z', 'system', '2025-05-15T14:30:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000001', 'DeploymentStarted', 'Deployment to Production initiated', '2025-05-15T15:00:00Z', 'Pipeline', '2025-05-15T15:00:00Z', 'system', '2025-05-15T15:00:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000002', 'ContractChanged', 'Refund endpoint added to Payments API v2.1.0', '2025-05-20T16:30:00Z', 'ContractDiff', '2025-05-20T16:30:00Z', 'system', '2025-05-20T16:30:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000001', 'DeploymentCompleted', 'Deployment to Production completed successfully', '2025-05-15T15:45:00Z', 'Pipeline', '2025-05-15T15:45:00Z', 'system', '2025-05-15T15:45:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-000000000003', 'DeploymentStarted', 'Hotfix deployment to Production initiated', '2025-05-10T09:00:00Z', 'Pipeline', '2025-05-10T09:00:00Z', 'system', '2025-05-10T09:00:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000006', '30000000-0000-0000-0000-000000000003', 'DeploymentCompleted', 'Hotfix deployed to Production', '2025-05-10T09:20:00Z', 'Pipeline', '2025-05-10T09:20:00Z', 'system', '2025-05-10T09:20:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000007', '30000000-0000-0000-0000-000000000008', 'ContractChanged', 'Breaking changes in Gateway API v2.0.0 — path restructure', '2025-06-01T10:30:00Z', 'ContractDiff', '2025-06-01T10:30:00Z', 'system', '2025-06-01T10:30:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000008', '30000000-0000-0000-0000-000000000008', 'BlastRadiusCalculated', 'Blast radius: 4 affected consumers (2 direct, 2 transitive)', '2025-06-01T11:10:00Z', 'BlastRadiusEngine', '2025-06-01T11:10:00Z', 'system', '2025-06-01T11:10:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000009', '30000000-0000-0000-0000-000000000009', 'ContractChanged', 'Search suggestions and facets added to Search API v1.2.0', '2025-05-25T11:30:00Z', 'ContractDiff', '2025-05-25T11:30:00Z', 'system', '2025-05-25T11:30:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000010', '30000000-0000-0000-0000-000000000009', 'DeploymentCompleted', 'Search API v1.2.0 deployed to Production', '2025-05-25T12:30:00Z', 'Pipeline', '2025-05-25T12:30:00Z', 'system', '2025-05-25T12:30:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000011', '30000000-0000-0000-0000-000000000012', 'ContractChanged', 'New features for Orders API v1.4.0 in development', '2025-06-02T14:00:00Z', 'ContractDiff', '2025-06-02T14:00:00Z', 'system', '2025-06-02T14:00:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000012', '30000000-0000-0000-0000-000000000012', 'DeploymentStarted', 'Development deployment started but failed lint checks', '2025-06-02T14:10:00Z', 'Pipeline', '2025-06-02T14:10:00Z', 'system', '2025-06-02T14:10:00Z', 'system', false)
ON CONFLICT DO NOTHING;

-- ═══ RULESET GOVERNANCE — Rulesets and Bindings ═══════════════════════════════

INSERT INTO rg_rulesets ("Id", "Name", "Description", "Content", "RulesetType", "IsActive", "RulesetCreatedAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  ('40000000-0000-0000-0000-000000000001', 'API Naming Convention', 'Enforces consistent naming for endpoints and schemas', '{"rules":[{"id":"naming-001","severity":"warning","message":"Endpoint path must use kebab-case"},{"id":"naming-002","severity":"error","message":"Schema names must use PascalCase"}]}', 0, true, '2025-02-01T00:00:00Z', '2025-02-01T00:00:00Z', 'admin@nextraceone.dev', '2025-02-01T00:00:00Z', 'admin@nextraceone.dev', false),
  ('40000000-0000-0000-0000-000000000002', 'Security Standards', 'Validates security schemes and authentication requirements', '{"rules":[{"id":"sec-001","severity":"error","message":"All endpoints must require authentication"},{"id":"sec-002","severity":"warning","message":"Sensitive data fields should use string format password"}]}', 0, true, '2025-02-15T00:00:00Z', '2025-02-15T00:00:00Z', 'admin@nextraceone.dev', '2025-02-15T00:00:00Z', 'admin@nextraceone.dev', false),
  ('40000000-0000-0000-0000-000000000003', 'Breaking Change Prevention', 'Prevents breaking changes in production APIs', '{"rules":[{"id":"brk-001","severity":"error","message":"Cannot remove existing endpoints"},{"id":"brk-002","severity":"error","message":"Cannot change response schema type"}]}', 0, true, '2025-03-01T00:00:00Z', '2025-03-01T00:00:00Z', 'techlead@nextraceone.dev', '2025-03-01T00:00:00Z', 'techlead@nextraceone.dev', false),
  ('40000000-0000-0000-0000-000000000004', 'Pagination Standards', 'Requires pagination parameters on collection endpoints', '{"rules":[{"id":"page-001","severity":"warning","message":"GET endpoints returning arrays must support limit/offset pagination"},{"id":"page-002","severity":"info","message":"Default page size should be between 10 and 100"}]}', 0, true, '2025-03-10T00:00:00Z', '2025-03-10T00:00:00Z', 'pedro.alves@nextraceone.dev', '2025-03-10T00:00:00Z', 'pedro.alves@nextraceone.dev', false),
  ('40000000-0000-0000-0000-000000000005', 'Error Response Format', 'Enforces consistent error response structure across APIs', '{"rules":[{"id":"err-001","severity":"error","message":"Error responses must include code, messageKey, and correlationId"},{"id":"err-002","severity":"warning","message":"4xx responses must include details array"}]}', 0, true, '2025-03-15T00:00:00Z', '2025-03-15T00:00:00Z', 'admin@nextraceone.dev', '2025-03-15T00:00:00Z', 'admin@nextraceone.dev', false),
  ('40000000-0000-0000-0000-000000000006', 'Versioning Policy', 'Requires proper API versioning in path or header', '{"rules":[{"id":"ver-001","severity":"error","message":"API path must include version prefix /api/v{n}/"},{"id":"ver-002","severity":"warning","message":"Deprecated endpoints must have sunset date header"}]}', 0, true, '2025-04-01T00:00:00Z', '2025-04-01T00:00:00Z', 'techlead@nextraceone.dev', '2025-04-01T00:00:00Z', 'techlead@nextraceone.dev', false),
  ('40000000-0000-0000-0000-000000000007', 'Rate Limiting Headers', 'Ensures rate limit headers are documented in all public APIs', '{"rules":[{"id":"rate-001","severity":"warning","message":"Public APIs must document X-RateLimit-Limit header"},{"id":"rate-002","severity":"info","message":"Consider documenting X-RateLimit-Remaining and X-RateLimit-Reset"}]}', 0, false, '2025-04-15T00:00:00Z', '2025-04-15T00:00:00Z', 'pedro.alves@nextraceone.dev', '2025-04-15T00:00:00Z', 'pedro.alves@nextraceone.dev', false)
ON CONFLICT DO NOTHING;

INSERT INTO rg_ruleset_bindings ("Id", "RulesetId", "AssetType", "BindingCreatedAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  ('41000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000001', 'ApiAsset', '2025-02-05T00:00:00Z', '2025-02-05T00:00:00Z', 'admin@nextraceone.dev', '2025-02-05T00:00:00Z', 'admin@nextraceone.dev', false),
  ('41000000-0000-0000-0000-000000000002', '40000000-0000-0000-0000-000000000002', 'ApiAsset', '2025-02-20T00:00:00Z', '2025-02-20T00:00:00Z', 'admin@nextraceone.dev', '2025-02-20T00:00:00Z', 'admin@nextraceone.dev', false),
  ('41000000-0000-0000-0000-000000000003', '40000000-0000-0000-0000-000000000003', 'ApiAsset', '2025-03-05T00:00:00Z', '2025-03-05T00:00:00Z', 'techlead@nextraceone.dev', '2025-03-05T00:00:00Z', 'techlead@nextraceone.dev', false),
  ('41000000-0000-0000-0000-000000000004', '40000000-0000-0000-0000-000000000004', 'ApiAsset', '2025-03-15T00:00:00Z', '2025-03-15T00:00:00Z', 'pedro.alves@nextraceone.dev', '2025-03-15T00:00:00Z', 'pedro.alves@nextraceone.dev', false),
  ('41000000-0000-0000-0000-000000000005', '40000000-0000-0000-0000-000000000005', 'ApiAsset', '2025-03-20T00:00:00Z', '2025-03-20T00:00:00Z', 'admin@nextraceone.dev', '2025-03-20T00:00:00Z', 'admin@nextraceone.dev', false),
  ('41000000-0000-0000-0000-000000000006', '40000000-0000-0000-0000-000000000006', 'ApiAsset', '2025-04-05T00:00:00Z', '2025-04-05T00:00:00Z', 'techlead@nextraceone.dev', '2025-04-05T00:00:00Z', 'techlead@nextraceone.dev', false),
  ('41000000-0000-0000-0000-000000000007', '40000000-0000-0000-0000-000000000003', 'ServiceAsset', '2025-03-10T00:00:00Z', '2025-03-10T00:00:00Z', 'admin@nextraceone.dev', '2025-03-10T00:00:00Z', 'admin@nextraceone.dev', false)
ON CONFLICT DO NOTHING;

-- ═══ WORKFLOW — Templates, Instances, Stages, Decisions, Evidence Packs ════════

-- Templates
INSERT INTO wf_workflow_templates ("Id", "Name", "Description", "ChangeType", "ApiCriticality", "TargetEnvironment", "MinimumApprovers", "IsActive", "CreatedAt")
VALUES
  ('50000000-0000-0000-0000-000000000001', 'Standard Release Workflow', 'Default workflow for minor changes to production APIs', 'Minor', 'High', 'Production', 2, true, '2025-02-01T00:00:00Z'),
  ('50000000-0000-0000-0000-000000000002', 'Fast-Track Patch Workflow', 'Streamlined workflow for patch-level fixes', 'Patch', 'Medium', 'Production', 1, true, '2025-02-15T00:00:00Z'),
  ('50000000-0000-0000-0000-000000000003', 'Breaking Change Workflow', 'Full governance for breaking changes requiring extended approval', 'Breaking', 'Critical', 'Production', 3, true, '2025-03-01T00:00:00Z'),
  ('50000000-0000-0000-0000-000000000004', 'Staging Promotion Workflow', 'Lightweight workflow for staging environment promotions', 'Minor', 'Medium', 'Staging', 1, true, '2025-03-15T00:00:00Z'),
  ('50000000-0000-0000-0000-000000000005', 'Internal API Change Workflow', 'Workflow for internal API changes with minimal oversight', 'Minor', 'Low', 'Production', 1, true, '2025-04-01T00:00:00Z')
ON CONFLICT DO NOTHING;

-- Workflow Instances
INSERT INTO wf_workflow_instances ("Id", "WorkflowTemplateId", "ReleaseId", "SubmittedBy", "Status", "CurrentStageIndex", "SubmittedAt", "CompletedAt")
VALUES
  -- Orders API v1.3.0 → Production (Approved)
  ('51000000-0000-0000-0000-000000000001', '50000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', 'dev@nextraceone.dev', 'Approved', 2, '2025-05-15T15:20:00Z', '2025-05-15T16:00:00Z'),
  -- Payments API v2.1.0 → Staging (InProgress)
  ('51000000-0000-0000-0000-000000000002', '50000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000002', 'dev@nextraceone.dev', 'InProgress', 1, '2025-05-20T17:20:00Z', NULL),
  -- Users API v1.1.0 → Staging (Pending — Breaking Change workflow)
  ('51000000-0000-0000-0000-000000000003', '50000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000006', 'ana.costa@nextraceone.dev', 'Pending', 0, '2025-06-01T10:30:00Z', NULL),
  -- Inventory hotfix v1.0.1 → Production (Approved via Fast-Track)
  ('51000000-0000-0000-0000-000000000004', '50000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000003', 'dev@nextraceone.dev', 'Approved', 1, '2025-05-10T09:05:00Z', '2025-05-10T09:25:00Z'),
  -- Gateway API v2.0.0 → Staging (InProgress — Breaking Change workflow)
  ('51000000-0000-0000-0000-000000000005', '50000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000008', 'pedro.alves@nextraceone.dev', 'InProgress', 1, '2025-06-01T11:30:00Z', NULL),
  -- Search API v1.2.0 → Production (Approved)
  ('51000000-0000-0000-0000-000000000006', '50000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000009', 'ana.costa@nextraceone.dev', 'Approved', 2, '2025-05-25T12:20:00Z', '2025-05-25T13:00:00Z'),
  -- Shipping API v1.0.0 → Production (Approved)
  ('51000000-0000-0000-0000-000000000007', '50000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-000000000007', 'lucia.ferreira@nextraceone.dev', 'Approved', 1, '2025-04-15T10:20:00Z', '2025-04-15T10:50:00Z'),
  -- Orders API v1.4.0 → Development (Rejected)
  ('51000000-0000-0000-0000-000000000008', '50000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000012', 'rafael.lima@nextraceone.dev', 'Rejected', 0, '2025-06-02T14:20:00Z', '2025-06-02T14:45:00Z')
ON CONFLICT DO NOTHING;

-- Workflow Stages
INSERT INTO wf_workflow_stages ("Id", "WorkflowInstanceId", "Name", "StageOrder", "Status", "RequiredApprovers", "CurrentApprovals", "CommentRequired", "StartedAt", "CompletedAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  -- Instance 1: Orders API v1.3.0 (Approved — both stages done)
  ('52000000-0000-0000-0000-000000000001', '51000000-0000-0000-0000-000000000001', 'Tech Lead Review', 0, 'Approved', 1, 1, true, '2025-05-15T15:20:00Z', '2025-05-15T15:35:00Z', '2025-05-15T15:20:00Z', 'system', '2025-05-15T15:35:00Z', 'system', false),
  ('52000000-0000-0000-0000-000000000002', '51000000-0000-0000-0000-000000000001', 'Security Review', 1, 'Approved', 1, 1, false, '2025-05-15T15:35:00Z', '2025-05-15T16:00:00Z', '2025-05-15T15:20:00Z', 'system', '2025-05-15T16:00:00Z', 'system', false),
  -- Instance 2: Payments API v2.1.0 (InProgress — stage 0 done, stage 1 pending)
  ('52000000-0000-0000-0000-000000000003', '51000000-0000-0000-0000-000000000002', 'Tech Lead Review', 0, 'Approved', 1, 1, true, '2025-05-20T17:20:00Z', '2025-05-20T17:45:00Z', '2025-05-20T17:20:00Z', 'system', '2025-05-20T17:45:00Z', 'system', false),
  ('52000000-0000-0000-0000-000000000004', '51000000-0000-0000-0000-000000000002', 'Security Review', 1, 'Pending', 1, 0, false, NULL, NULL, '2025-05-20T17:20:00Z', 'system', '2025-05-20T17:20:00Z', 'system', false),
  -- Instance 3: Users API v1.1.0 (Pending — all stages pending)
  ('52000000-0000-0000-0000-000000000005', '51000000-0000-0000-0000-000000000003', 'Architecture Review', 0, 'Pending', 2, 0, true, NULL, NULL, '2025-06-01T10:30:00Z', 'system', '2025-06-01T10:30:00Z', 'system', false),
  ('52000000-0000-0000-0000-000000000006', '51000000-0000-0000-0000-000000000003', 'Security Review', 1, 'Pending', 1, 0, false, NULL, NULL, '2025-06-01T10:30:00Z', 'system', '2025-06-01T10:30:00Z', 'system', false),
  ('52000000-0000-0000-0000-000000000007', '51000000-0000-0000-0000-000000000003', 'Final Approval', 2, 'Pending', 1, 0, true, NULL, NULL, '2025-06-01T10:30:00Z', 'system', '2025-06-01T10:30:00Z', 'system', false),
  -- Instance 4: Inventory hotfix (Approved — single stage via Fast-Track)
  ('52000000-0000-0000-0000-000000000008', '51000000-0000-0000-0000-000000000004', 'Quick Review', 0, 'Approved', 1, 1, false, '2025-05-10T09:05:00Z', '2025-05-10T09:25:00Z', '2025-05-10T09:05:00Z', 'system', '2025-05-10T09:25:00Z', 'system', false),
  -- Instance 5: Gateway API v2.0.0 (InProgress — stage 0 approved, stages 1-2 pending)
  ('52000000-0000-0000-0000-000000000009', '51000000-0000-0000-0000-000000000005', 'Architecture Review', 0, 'Approved', 2, 2, true, '2025-06-01T11:30:00Z', '2025-06-01T12:30:00Z', '2025-06-01T11:30:00Z', 'system', '2025-06-01T12:30:00Z', 'system', false),
  ('52000000-0000-0000-0000-000000000010', '51000000-0000-0000-0000-000000000005', 'Security Review', 1, 'Pending', 1, 0, true, NULL, NULL, '2025-06-01T11:30:00Z', 'system', '2025-06-01T11:30:00Z', 'system', false),
  ('52000000-0000-0000-0000-000000000011', '51000000-0000-0000-0000-000000000005', 'Final Approval', 2, 'Pending', 1, 0, true, NULL, NULL, '2025-06-01T11:30:00Z', 'system', '2025-06-01T11:30:00Z', 'system', false),
  -- Instance 6: Search API v1.2.0 (Approved — both stages done)
  ('52000000-0000-0000-0000-000000000012', '51000000-0000-0000-0000-000000000006', 'Tech Lead Review', 0, 'Approved', 1, 1, true, '2025-05-25T12:20:00Z', '2025-05-25T12:40:00Z', '2025-05-25T12:20:00Z', 'system', '2025-05-25T12:40:00Z', 'system', false),
  ('52000000-0000-0000-0000-000000000013', '51000000-0000-0000-0000-000000000006', 'Security Review', 1, 'Approved', 1, 1, false, '2025-05-25T12:40:00Z', '2025-05-25T13:00:00Z', '2025-05-25T12:20:00Z', 'system', '2025-05-25T13:00:00Z', 'system', false),
  -- Instance 7: Shipping API (Approved — single stage)
  ('52000000-0000-0000-0000-000000000014', '51000000-0000-0000-0000-000000000007', 'Tech Lead Review', 0, 'Approved', 1, 1, false, '2025-04-15T10:20:00Z', '2025-04-15T10:50:00Z', '2025-04-15T10:20:00Z', 'system', '2025-04-15T10:50:00Z', 'system', false),
  -- Instance 8: Orders API v1.4.0 (Rejected at first stage)
  ('52000000-0000-0000-0000-000000000015', '51000000-0000-0000-0000-000000000008', 'Tech Lead Review', 0, 'Rejected', 1, 0, true, '2025-06-02T14:20:00Z', '2025-06-02T14:45:00Z', '2025-06-02T14:20:00Z', 'system', '2025-06-02T14:45:00Z', 'system', false)
ON CONFLICT DO NOTHING;

-- Approval Decisions
INSERT INTO wf_approval_decisions ("Id", "WorkflowStageId", "WorkflowInstanceId", "DecidedBy", "Decision", "Comment", "DecidedAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  -- Instance 1: Orders API v1.3.0
  ('53000000-0000-0000-0000-000000000001', '52000000-0000-0000-0000-000000000001', '51000000-0000-0000-0000-000000000001', 'techlead@nextraceone.dev', 'Approved', 'New endpoints look well-designed. LGTM.', '2025-05-15T15:35:00Z', '2025-05-15T15:35:00Z', 'techlead@nextraceone.dev', '2025-05-15T15:35:00Z', 'techlead@nextraceone.dev', false),
  ('53000000-0000-0000-0000-000000000002', '52000000-0000-0000-0000-000000000002', '51000000-0000-0000-0000-000000000001', 'admin@nextraceone.dev', 'Approved', 'Security review passed.', '2025-05-15T16:00:00Z', '2025-05-15T16:00:00Z', 'admin@nextraceone.dev', '2025-05-15T16:00:00Z', 'admin@nextraceone.dev', false),
  -- Instance 2: Payments API v2.1.0
  ('53000000-0000-0000-0000-000000000003', '52000000-0000-0000-0000-000000000003', '51000000-0000-0000-0000-000000000002', 'techlead@nextraceone.dev', 'Approved', 'Refund endpoint approved.', '2025-05-20T17:45:00Z', '2025-05-20T17:45:00Z', 'techlead@nextraceone.dev', '2025-05-20T17:45:00Z', 'techlead@nextraceone.dev', false),
  -- Instance 4: Inventory hotfix
  ('53000000-0000-0000-0000-000000000004', '52000000-0000-0000-0000-000000000008', '51000000-0000-0000-0000-000000000004', 'techlead@nextraceone.dev', 'Approved', 'Critical hotfix approved via fast-track.', '2025-05-10T09:25:00Z', '2025-05-10T09:25:00Z', 'techlead@nextraceone.dev', '2025-05-10T09:25:00Z', 'techlead@nextraceone.dev', false),
  -- Instance 5: Gateway API v2.0.0 — Architecture Review (2 approvers)
  ('53000000-0000-0000-0000-000000000005', '52000000-0000-0000-0000-000000000009', '51000000-0000-0000-0000-000000000005', 'techlead@nextraceone.dev', 'Approved', 'Path restructure is necessary for v2 evolution.', '2025-06-01T12:00:00Z', '2025-06-01T12:00:00Z', 'techlead@nextraceone.dev', '2025-06-01T12:00:00Z', 'techlead@nextraceone.dev', false),
  ('53000000-0000-0000-0000-000000000006', '52000000-0000-0000-0000-000000000009', '51000000-0000-0000-0000-000000000005', 'pedro.alves@nextraceone.dev', 'Approved', 'Migration plan for consumers is adequate.', '2025-06-01T12:30:00Z', '2025-06-01T12:30:00Z', 'pedro.alves@nextraceone.dev', '2025-06-01T12:30:00Z', 'pedro.alves@nextraceone.dev', false),
  -- Instance 6: Search API v1.2.0
  ('53000000-0000-0000-0000-000000000007', '52000000-0000-0000-0000-000000000012', '51000000-0000-0000-0000-000000000006', 'pedro.alves@nextraceone.dev', 'Approved', 'Search improvements look solid.', '2025-05-25T12:40:00Z', '2025-05-25T12:40:00Z', 'pedro.alves@nextraceone.dev', '2025-05-25T12:40:00Z', 'pedro.alves@nextraceone.dev', false),
  ('53000000-0000-0000-0000-000000000008', '52000000-0000-0000-0000-000000000013', '51000000-0000-0000-0000-000000000006', 'admin@nextraceone.dev', 'Approved', 'No security concerns with additive changes.', '2025-05-25T13:00:00Z', '2025-05-25T13:00:00Z', 'admin@nextraceone.dev', '2025-05-25T13:00:00Z', 'admin@nextraceone.dev', false),
  -- Instance 7: Shipping API
  ('53000000-0000-0000-0000-000000000009', '52000000-0000-0000-0000-000000000014', '51000000-0000-0000-0000-000000000007', 'techlead@nextraceone.dev', 'Approved', 'Internal API approved for production.', '2025-04-15T10:50:00Z', '2025-04-15T10:50:00Z', 'techlead@nextraceone.dev', '2025-04-15T10:50:00Z', 'techlead@nextraceone.dev', false),
  -- Instance 8: Orders API v1.4.0 (Rejected)
  ('53000000-0000-0000-0000-000000000010', '52000000-0000-0000-0000-000000000015', '51000000-0000-0000-0000-000000000008', 'pedro.alves@nextraceone.dev', 'Rejected', 'Lint checks failed. Missing pagination on GET /orders/history. Fix and resubmit.', '2025-06-02T14:45:00Z', '2025-06-02T14:45:00Z', 'pedro.alves@nextraceone.dev', '2025-06-02T14:45:00Z', 'pedro.alves@nextraceone.dev', false)
ON CONFLICT DO NOTHING;

-- SLA Policies
INSERT INTO wf_sla_policies ("Id", "WorkflowTemplateId", "StageName", "MaxDurationHours", "EscalationEnabled", "EscalationTargetRole", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  ('54000000-0000-0000-0000-000000000001', '50000000-0000-0000-0000-000000000001', 'Tech Lead Review', 24, true, 'PlatformAdmin', '2025-02-01T00:00:00Z', 'admin@nextraceone.dev', '2025-02-01T00:00:00Z', 'admin@nextraceone.dev', false),
  ('54000000-0000-0000-0000-000000000002', '50000000-0000-0000-0000-000000000001', 'Security Review', 48, true, 'PlatformAdmin', '2025-02-01T00:00:00Z', 'admin@nextraceone.dev', '2025-02-01T00:00:00Z', 'admin@nextraceone.dev', false),
  ('54000000-0000-0000-0000-000000000003', '50000000-0000-0000-0000-000000000002', 'Quick Review', 4, true, 'TechLead', '2025-02-15T00:00:00Z', 'admin@nextraceone.dev', '2025-02-15T00:00:00Z', 'admin@nextraceone.dev', false),
  ('54000000-0000-0000-0000-000000000004', '50000000-0000-0000-0000-000000000003', 'Architecture Review', 72, true, 'PlatformAdmin', '2025-03-01T00:00:00Z', 'admin@nextraceone.dev', '2025-03-01T00:00:00Z', 'admin@nextraceone.dev', false),
  ('54000000-0000-0000-0000-000000000005', '50000000-0000-0000-0000-000000000003', 'Security Review', 48, true, 'PlatformAdmin', '2025-03-01T00:00:00Z', 'admin@nextraceone.dev', '2025-03-01T00:00:00Z', 'admin@nextraceone.dev', false),
  ('54000000-0000-0000-0000-000000000006', '50000000-0000-0000-0000-000000000003', 'Final Approval', 24, true, 'PlatformAdmin', '2025-03-01T00:00:00Z', 'admin@nextraceone.dev', '2025-03-01T00:00:00Z', 'admin@nextraceone.dev', false)
ON CONFLICT DO NOTHING;

-- Evidence Packs
INSERT INTO wf_evidence_packs ("Id", "WorkflowInstanceId", "ReleaseId", "ContractDiffSummary", "BlastRadiusScore", "SpectralScore", "ChangeIntelligenceScore", "ApprovalHistory", "ContractHash", "CompletenessPercentage", "GeneratedAt")
VALUES
  -- Orders API v1.3.0 (complete)
  ('55000000-0000-0000-0000-000000000001', '51000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', '2 additive changes: POST /orders, GET /orders/{id}', 0.4500, 0.9200, 0.7200, '[{"stage":"Tech Lead Review","decision":"Approved","by":"techlead@nextraceone.dev","at":"2025-05-15T15:35:00Z"},{"stage":"Security Review","decision":"Approved","by":"admin@nextraceone.dev","at":"2025-05-15T16:00:00Z"}]', 'sha256:ab12cd34ef56ab12cd34ef56ab12cd34ef56ab12cd34ef56ab12cd34ef56ab12', 100.00, '2025-05-15T16:05:00Z'),
  -- Inventory hotfix v1.0.1 (complete)
  ('55000000-0000-0000-0000-000000000002', '51000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000003', 'Patch: bug fix in stock calculation endpoint', 0.1500, 0.9800, 0.3000, '[{"stage":"Quick Review","decision":"Approved","by":"techlead@nextraceone.dev","at":"2025-05-10T09:25:00Z"}]', 'sha256:cd34ef56ab12cd34ef56ab12cd34ef56ab12cd34ef56ab12cd34ef56ab12cd34', 100.00, '2025-05-10T09:30:00Z'),
  -- Search API v1.2.0 (complete)
  ('55000000-0000-0000-0000-000000000003', '51000000-0000-0000-0000-000000000006', '30000000-0000-0000-0000-000000000009', '3 additive changes: POST /search, GET /search/suggest, GET /search/facets', 0.2500, 0.9500, 0.5000, '[{"stage":"Tech Lead Review","decision":"Approved","by":"pedro.alves@nextraceone.dev","at":"2025-05-25T12:40:00Z"},{"stage":"Security Review","decision":"Approved","by":"admin@nextraceone.dev","at":"2025-05-25T13:00:00Z"}]', 'sha256:ef56ab12cd34ef56ab12cd34ef56ab12cd34ef56ab12cd34ef56ab12cd34ef56', 100.00, '2025-05-25T13:05:00Z'),
  -- Shipping API v1.0.0 (complete)
  ('55000000-0000-0000-0000-000000000004', '51000000-0000-0000-0000-000000000007', '30000000-0000-0000-0000-000000000007', 'Initial version: 3 endpoints for shipment management', 0.2000, 0.8800, 0.4500, '[{"stage":"Tech Lead Review","decision":"Approved","by":"techlead@nextraceone.dev","at":"2025-04-15T10:50:00Z"}]', 'sha256:1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b', 100.00, '2025-04-15T10:55:00Z'),
  -- Gateway API v2.0.0 (partial — InProgress)
  ('55000000-0000-0000-0000-000000000005', '51000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-000000000008', '2 breaking changes: path restructure from /gateway/* to /v2/gateway/*; 2 additive changes', 0.6500, 0.7500, 0.9200, '[{"stage":"Architecture Review","decision":"Approved","by":"techlead@nextraceone.dev","at":"2025-06-01T12:00:00Z"},{"stage":"Architecture Review","decision":"Approved","by":"pedro.alves@nextraceone.dev","at":"2025-06-01T12:30:00Z"}]', 'sha256:2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c', 65.00, '2025-06-01T12:35:00Z')
ON CONFLICT DO NOTHING;

-- ═══ PROMOTION — Environments, Gates, Requests ═══════════════════════════════

-- Deployment Environments
INSERT INTO prm_deployment_environments ("Id", "Name", "Description", "Order", "RequiresApproval", "RequiresEvidencePack", "IsActive", "CreatedAt")
VALUES
  ('60000000-0000-0000-0000-000000000001', 'Development', 'Development environment for initial testing', 1, false, false, true, '2025-01-01T00:00:00Z'),
  ('60000000-0000-0000-0000-000000000002', 'Staging', 'Pre-production environment with production-like config', 2, true, false, true, '2025-01-01T00:00:00Z'),
  ('60000000-0000-0000-0000-000000000003', 'Production', 'Live production environment', 3, true, true, true, '2025-01-01T00:00:00Z')
ON CONFLICT DO NOTHING;

-- Promotion Gates
INSERT INTO prm_promotion_gates ("Id", "DeploymentEnvironmentId", "GateName", "GateType", "IsRequired", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  ('61000000-0000-0000-0000-000000000001', '60000000-0000-0000-0000-000000000003', 'Workflow Approval', 'WorkflowApproval', true, true, '2025-01-01T00:00:00Z', 'admin@nextraceone.dev', '2025-01-01T00:00:00Z', 'admin@nextraceone.dev', false),
  ('61000000-0000-0000-0000-000000000002', '60000000-0000-0000-0000-000000000003', 'Contract Validation', 'ContractValidation', true, true, '2025-01-01T00:00:00Z', 'admin@nextraceone.dev', '2025-01-01T00:00:00Z', 'admin@nextraceone.dev', false),
  ('61000000-0000-0000-0000-000000000003', '60000000-0000-0000-0000-000000000003', 'Blast Radius Check', 'BlastRadius', false, true, '2025-01-01T00:00:00Z', 'admin@nextraceone.dev', '2025-01-01T00:00:00Z', 'admin@nextraceone.dev', false),
  ('61000000-0000-0000-0000-000000000004', '60000000-0000-0000-0000-000000000002', 'Workflow Approval', 'WorkflowApproval', true, true, '2025-01-01T00:00:00Z', 'admin@nextraceone.dev', '2025-01-01T00:00:00Z', 'admin@nextraceone.dev', false),
  ('61000000-0000-0000-0000-000000000005', '60000000-0000-0000-0000-000000000003', 'Evidence Pack Complete', 'EvidencePackComplete', true, true, '2025-02-01T00:00:00Z', 'admin@nextraceone.dev', '2025-02-01T00:00:00Z', 'admin@nextraceone.dev', false),
  ('61000000-0000-0000-0000-000000000006', '60000000-0000-0000-0000-000000000002', 'Contract Validation', 'ContractValidation', false, true, '2025-02-01T00:00:00Z', 'admin@nextraceone.dev', '2025-02-01T00:00:00Z', 'admin@nextraceone.dev', false)
ON CONFLICT DO NOTHING;

-- Promotion Requests
INSERT INTO prm_promotion_requests ("Id", "ReleaseId", "SourceEnvironmentId", "TargetEnvironmentId", "RequestedBy", "Status", "Justification", "RequestedAt", "CompletedAt")
VALUES
  -- Orders API v1.3.0 → Staging → Production (Approved)
  ('62000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', '60000000-0000-0000-0000-000000000002', '60000000-0000-0000-0000-000000000003', 'techlead@nextraceone.dev', 'Approved', 'Orders API v1.3.0 passed all staging tests', '2025-05-15T16:10:00Z', '2025-05-15T17:00:00Z'),
  -- Payments API v2.1.0 → Dev → Staging (Pending)
  ('62000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000002', '60000000-0000-0000-0000-000000000001', '60000000-0000-0000-0000-000000000002', 'dev@nextraceone.dev', 'Pending', 'Payments API v2.1.0 ready for staging validation', '2025-05-20T17:30:00Z', NULL),
  -- Inventory hotfix → Staging → Production (Promoted)
  ('62000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000003', '60000000-0000-0000-0000-000000000002', '60000000-0000-0000-0000-000000000003', 'techlead@nextraceone.dev', 'Promoted', 'Inventory hotfix approved for production', '2025-05-10T09:30:00Z', '2025-05-10T10:00:00Z'),
  -- Shipping API v1.0.0 → Staging → Production (Promoted)
  ('62000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000007', '60000000-0000-0000-0000-000000000002', '60000000-0000-0000-0000-000000000003', 'techlead@nextraceone.dev', 'Promoted', 'Shipping API initial release passed all tests', '2025-04-15T11:00:00Z', '2025-04-15T11:30:00Z'),
  -- Gateway API v2.0.0 → Dev → Staging (Pending — awaiting workflow)
  ('62000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-000000000008', '60000000-0000-0000-0000-000000000001', '60000000-0000-0000-0000-000000000002', 'pedro.alves@nextraceone.dev', 'Pending', 'Gateway v2.0.0 with breaking changes needs staging validation', '2025-06-01T13:00:00Z', NULL),
  -- Search API v1.2.0 → Staging → Production (Promoted)
  ('62000000-0000-0000-0000-000000000006', '30000000-0000-0000-0000-000000000009', '60000000-0000-0000-0000-000000000002', '60000000-0000-0000-0000-000000000003', 'pedro.alves@nextraceone.dev', 'Promoted', 'Search API improvements validated in staging', '2025-05-25T13:10:00Z', '2025-05-25T14:00:00Z'),
  -- Orders API v1.4.0 → Dev → Staging (Rejected — lint failure)
  ('62000000-0000-0000-0000-000000000007', '30000000-0000-0000-0000-000000000012', '60000000-0000-0000-0000-000000000001', '60000000-0000-0000-0000-000000000002', 'rafael.lima@nextraceone.dev', 'Rejected', 'Lint checks failed — needs pagination on new endpoints', '2025-06-02T15:00:00Z', '2025-06-02T15:10:00Z')
ON CONFLICT DO NOTHING;

-- Gate Evaluations
INSERT INTO prm_gate_evaluations ("Id", "PromotionRequestId", "PromotionGateId", "Passed", "EvaluatedBy", "EvaluationDetails", "EvaluatedAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  -- Orders API v1.3.0 → Production (all gates passed)
  ('63000000-0000-0000-0000-000000000001', '62000000-0000-0000-0000-000000000001', '61000000-0000-0000-0000-000000000001', true, 'system', 'Workflow instance 51000000-...-001 approved', '2025-05-15T16:15:00Z', '2025-05-15T16:15:00Z', 'system', '2025-05-15T16:15:00Z', 'system', false),
  ('63000000-0000-0000-0000-000000000002', '62000000-0000-0000-0000-000000000001', '61000000-0000-0000-0000-000000000002', true, 'system', 'Contract v1.3.0 validation passed, no breaking changes', '2025-05-15T16:16:00Z', '2025-05-15T16:16:00Z', 'system', '2025-05-15T16:16:00Z', 'system', false),
  ('63000000-0000-0000-0000-000000000003', '62000000-0000-0000-0000-000000000001', '61000000-0000-0000-0000-000000000003', true, 'system', 'Blast radius: 5 consumers, score within threshold', '2025-05-15T16:17:00Z', '2025-05-15T16:17:00Z', 'system', '2025-05-15T16:17:00Z', 'system', false),
  ('63000000-0000-0000-0000-000000000004', '62000000-0000-0000-0000-000000000001', '61000000-0000-0000-0000-000000000005', true, 'system', 'Evidence pack 100% complete', '2025-05-15T16:18:00Z', '2025-05-15T16:18:00Z', 'system', '2025-05-15T16:18:00Z', 'system', false),
  -- Inventory hotfix → Production (all gates passed)
  ('63000000-0000-0000-0000-000000000005', '62000000-0000-0000-0000-000000000003', '61000000-0000-0000-0000-000000000001', true, 'system', 'Workflow instance 51000000-...-004 approved via fast-track', '2025-05-10T09:35:00Z', '2025-05-10T09:35:00Z', 'system', '2025-05-10T09:35:00Z', 'system', false),
  ('63000000-0000-0000-0000-000000000006', '62000000-0000-0000-0000-000000000003', '61000000-0000-0000-0000-000000000002', true, 'system', 'Contract v1.0.1 validation passed, patch only', '2025-05-10T09:36:00Z', '2025-05-10T09:36:00Z', 'system', '2025-05-10T09:36:00Z', 'system', false),
  -- Shipping API → Production (all gates passed)
  ('63000000-0000-0000-0000-000000000007', '62000000-0000-0000-0000-000000000004', '61000000-0000-0000-0000-000000000001', true, 'system', 'Workflow instance 51000000-...-007 approved', '2025-04-15T11:05:00Z', '2025-04-15T11:05:00Z', 'system', '2025-04-15T11:05:00Z', 'system', false),
  ('63000000-0000-0000-0000-000000000008', '62000000-0000-0000-0000-000000000004', '61000000-0000-0000-0000-000000000002', true, 'system', 'Contract v1.0.0 validation passed', '2025-04-15T11:06:00Z', '2025-04-15T11:06:00Z', 'system', '2025-04-15T11:06:00Z', 'system', false),
  -- Search API → Production (all gates passed)
  ('63000000-0000-0000-0000-000000000009', '62000000-0000-0000-0000-000000000006', '61000000-0000-0000-0000-000000000001', true, 'system', 'Workflow instance 51000000-...-006 approved', '2025-05-25T13:15:00Z', '2025-05-25T13:15:00Z', 'system', '2025-05-25T13:15:00Z', 'system', false),
  ('63000000-0000-0000-0000-000000000010', '62000000-0000-0000-0000-000000000006', '61000000-0000-0000-0000-000000000002', true, 'system', 'Contract v1.2.0 validation passed', '2025-05-25T13:16:00Z', '2025-05-25T13:16:00Z', 'system', '2025-05-25T13:16:00Z', 'system', false),
  ('63000000-0000-0000-0000-000000000011', '62000000-0000-0000-0000-000000000006', '61000000-0000-0000-0000-000000000005', true, 'system', 'Evidence pack 100% complete', '2025-05-25T13:17:00Z', '2025-05-25T13:17:00Z', 'system', '2025-05-25T13:17:00Z', 'system', false),
  -- Orders API v1.4.0 → Staging (contract validation failed)
  ('63000000-0000-0000-0000-000000000012', '62000000-0000-0000-0000-000000000007', '61000000-0000-0000-0000-000000000004', false, 'system', 'Workflow instance 51000000-...-008 was rejected', '2025-06-02T15:05:00Z', '2025-06-02T15:05:00Z', 'system', '2025-06-02T15:05:00Z', 'system', false),
  ('63000000-0000-0000-0000-000000000013', '62000000-0000-0000-0000-000000000007', '61000000-0000-0000-0000-000000000006', false, 'system', 'Contract validation failed: missing pagination parameters', '2025-06-02T15:06:00Z', '2025-06-02T15:06:00Z', 'system', '2025-06-02T15:06:00Z', 'system', false)
ON CONFLICT DO NOTHING;
