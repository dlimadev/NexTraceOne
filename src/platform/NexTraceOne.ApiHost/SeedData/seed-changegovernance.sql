-- ═══════════════════════════════════════════════════════════════════════════════
-- NEXTRACEONE — Seed data: Change Governance Module (ChangeIntelligenceDatabase)
-- DbContexts: ChangeIntelligenceDbContext (chg_ tables),
--             PromotionDbContext (chg_deployment_environments, chg_promotion_*),
--             RulesetGovernanceDbContext (chg_rulesets, chg_ruleset_bindings, chg_lint_results)
-- All INSERT statements are idempotent: ON CONFLICT DO NOTHING.
-- Enum values (stored as integers):
--   ChangeLevel: Operational=0, NonBreaking=1, Additive=2, Breaking=3
--   DeploymentStatus: Pending=0, Running=1, Succeeded=2, Failed=3, RolledBack=4
--   ChangeType: Deployment=0, ConfigurationChange=1, ContractChange=2, SchemaChange=3
--   ConfidenceStatus: NotAssessed=0, Validated=1, NeedsAttention=2, SuspectedRegression=3
--   ValidationStatus: Pending=0, InProgress=1, Passed=2, Failed=3
--   RulesetType (int): 0=SpectralOpenApi, 1=Custom (verify via domain)
--   PromotionStatus: Pending=0, InEvaluation=1, Approved=2, Rejected=3, Blocked=4, Cancelled=5
-- ═══════════════════════════════════════════════════════════════════════════════

-- ═══ RELEASES (ChangeIntelligenceDbContext) ════════════════════════════════════

INSERT INTO chg_releases (
  "Id", "ApiAssetId", "ServiceName", "Version",
  "ReleaseName",
  "Environment", "PipelineSource", "CommitSha",
  "ChangeLevel", "Status", "ChangeScore",
  "WorkItemReference", "RolledBackFromReleaseId",
  "ChangeType", "ConfidenceStatus", "ValidationStatus",
  "TeamName", "Domain", "Description",
  "HasBreakingChanges",
  "CreatedAt", tenant_id
) VALUES
(
  'c9000001-0001-0000-0000-000000000001',
  'ca010001-0001-0000-0000-000000000001',
  'payment-service', '1.2.0',
  'payment-service 1.2.0',
  'production', 'gitlab/payments-team/payment-service', 'abc123def456',
  1, 2, 0.8500,
  'PAY-1234', NULL,
  0, 1, 2,
  'payments-team', 'payments', 'Minor release: adds support for new payment gateway with zero breaking changes.',
  false,
  NOW() - INTERVAL '7 days', 'a0000000-0000-0000-0000-000000000001'
),
(
  'c9000002-0001-0000-0000-000000000001',
  'ca010002-0001-0000-0000-000000000001',
  'catalog-service', '2.0.0',
  'catalog-service 2.0.0',
  'production', 'gitlab/platform-team/catalog-service', 'def789abc012',
  3, 2, 0.6200,
  'CAT-567', NULL,
  2, 0, 2,
  'platform-team', 'platform', 'Major release: contract changes in service registration endpoint (breaking).',
  true,
  NOW() - INTERVAL '14 days', 'a0000000-0000-0000-0000-000000000001'
),
(
  'c9000003-0001-0000-0000-000000000001',
  'ca010003-0001-0000-0000-000000000001',
  'identity-service', '1.0.5',
  'identity-service 1.0.5',
  'staging', 'gitlab/platform-team/identity-service', 'fed321cba654',
  1, 0, 0.0000,
  'PLAT-89', NULL,
  0, 0, 0,
  'platform-team', 'security', 'Patch: dependency updates and security hardening.',
  false,
  NOW() - INTERVAL '1 day', 'a0000000-0000-0000-0000-000000000001'
)
ON CONFLICT DO NOTHING;

-- ═══ DEPLOYMENT ENVIRONMENTS (PromotionDbContext) ══════════════════════════════

INSERT INTO chg_deployment_environments (
  "Id", "Name", "Description", "Order",
  "RequiresApproval", "RequiresEvidencePack", "IsActive",
  "CreatedAt"
) VALUES
(
  'c9010001-0001-0000-0000-000000000001',
  'development', 'Development environment for feature work and integration testing.',
  1, false, false, true,
  NOW() - INTERVAL '90 days'
),
(
  'c9010002-0001-0000-0000-000000000001',
  'staging', 'Pre-production staging environment. Mirrors production configuration.',
  2, false, false, true,
  NOW() - INTERVAL '90 days'
),
(
  'c9010003-0001-0000-0000-000000000001',
  'production', 'Production environment. Requires approval and evidence pack for all breaking changes.',
  3, true, true, true,
  NOW() - INTERVAL '90 days'
)
ON CONFLICT DO NOTHING;

-- ═══ RULESETS (RulesetGovernanceDbContext) ════════════════════════════════════

INSERT INTO chg_rulesets (
  "Id", "Name", "Description",
  "Content", "RulesetType", "IsActive", "RulesetCreatedAt",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'c9020001-0001-0000-0000-000000000001',
  'nextraceone-change-standards',
  'Default NexTraceOne change governance ruleset. Validates release metadata, change level classification and evidence completeness.',
  '{"rules":{"require-work-item-reference":{"severity":"warn","description":"Releases should reference a work item"},"breaking-change-requires-approval":{"severity":"error","description":"Breaking changes require explicit promotion approval"},"evidence-pack-for-production":{"severity":"error","description":"Production deployments require evidence pack"}}}',
  0, true, NOW() - INTERVAL '90 days',
  NOW() - INTERVAL '90 days', 'system', NOW(), 'system', false
),
(
  'c9020002-0001-0000-0000-000000000001',
  'nextraceone-security-baseline',
  'Security baseline ruleset. Validates that releases include security approval markers for critical services.',
  '{"rules":{"critical-service-security-approval":{"severity":"error","description":"Critical services require SecurityApproval marker before production"},"commit-sha-required":{"severity":"error","description":"All releases must have a commit SHA for traceability"}}}',
  0, true, NOW() - INTERVAL '60 days',
  NOW() - INTERVAL '60 days', 'system', NOW(), 'system', false
)
ON CONFLICT DO NOTHING;

-- ═══ RULESET BINDINGS (RulesetGovernanceDbContext) ════════════════════════════

INSERT INTO chg_ruleset_bindings (
  "Id", "RulesetId", "AssetType",
  "BindingCreatedAt",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'c9030001-0001-0000-0000-000000000001',
  'c9020001-0001-0000-0000-000000000001',
  'RestApi',
  NOW() - INTERVAL '90 days',
  NOW() - INTERVAL '90 days', 'system', NOW(), 'system', false
),
(
  'c9030002-0001-0000-0000-000000000001',
  'c9020002-0001-0000-0000-000000000001',
  'RestApi',
  NOW() - INTERVAL '60 days',
  NOW() - INTERVAL '60 days', 'system', NOW(), 'system', false
)
ON CONFLICT DO NOTHING;

-- ═══ LINT RESULTS (RulesetGovernanceDbContext) ════════════════════════════════

INSERT INTO chg_lint_results (
  "Id", "RulesetId", "ReleaseId", "ApiAssetId",
  "Score", "TotalFindings", "Findings", "ExecutedAt",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'c9040001-0001-0000-0000-000000000001',
  'c9020001-0001-0000-0000-000000000001',
  'c9000001-0001-0000-0000-000000000001',
  'ca010001-0001-0000-0000-000000000001',
  92.50, 1, '[]',
  NOW() - INTERVAL '7 days',
  NOW() - INTERVAL '7 days', 'system', NOW(), 'system', false
),
(
  'c9040002-0001-0000-0000-000000000001',
  'c9020001-0001-0000-0000-000000000001',
  'c9000002-0001-0000-0000-000000000001',
  'ca010002-0001-0000-0000-000000000001',
  68.00, 4, '[]',
  NOW() - INTERVAL '14 days',
  NOW() - INTERVAL '14 days', 'system', NOW(), 'system', false
)
ON CONFLICT DO NOTHING;
