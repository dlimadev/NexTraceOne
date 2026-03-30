-- ═══════════════════════════════════════════════════════════════════════════════
-- NEXTRACEONE — Seed data: AI Knowledge Module (AiGovernanceDatabase)
-- Tables: aik_models, aik_providers, aik_routing_strategies, aik_knowledge_sources,
--         aik_source_weights, aik_agents, aik_access_policies, aik_budgets
-- All INSERT statements are idempotent: ON CONFLICT DO NOTHING.
-- ═══════════════════════════════════════════════════════════════════════════════

-- ═══ PROVIDERS ═══════════════════════════════════════════════════════════════

INSERT INTO aik_providers (
  "Id", "Name", "Slug", "DisplayName", "Description",
  "ProviderType", "BaseUrl", "IsLocal", "IsExternal", "IsEnabled",
  "AuthenticationMode", "SupportedCapabilities",
  "SupportsChat", "SupportsEmbeddings", "SupportsTools", "SupportsVision", "SupportsStructuredOutput",
  "HealthStatus", "Priority", "TimeoutSeconds", "RegisteredAt",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'cc000001-0001-0000-0000-000000000001',
  'ollama-local', 'ollama', 'Ollama (Local)', 'Local AI provider via Ollama runtime.',
  'Ollama', 'http://localhost:11434', true, false, true,
  'None', 'chat,reasoning,code,embeddings',
  true, true, true, false, true,
  'Unknown', 1, 30, NOW(),
  NOW(), 'system', NOW(), 'system', false
),
(
  'cc000002-0001-0000-0000-000000000001',
  'openai', 'openai', 'OpenAI', 'OpenAI external AI provider (requires API key).',
  'OpenAI', 'https://api.openai.com', false, true, false,
  'ApiKey', 'chat,reasoning,code,vision,embeddings,tools,structured-output',
  true, true, true, true, true,
  'Unknown', 10, 30, NOW(),
  NOW(), 'system', NOW(), 'system', false
)
ON CONFLICT DO NOTHING;

-- ═══ MODELS ══════════════════════════════════════════════════════════════════

-- Fix existing model rows to match the actual Ollama models installed locally
UPDATE aik_models SET "Name" = 'qwen3.5:9b', "Slug" = 'qwen3.5-9b', "DisplayName" = 'Qwen 3.5 9B (Chat)', "ExternalModelId" = 'qwen3.5:9b'
WHERE "Id" = 'aa000001-0001-0000-0000-000000000001';

UPDATE aik_models SET "Name" = 'deepseek-r1:14b', "Slug" = 'deepseek-r1-14b', "DisplayName" = 'DeepSeek R1 14B (Analysis)',
  "ExternalModelId" = 'deepseek-r1:14b', "ModelType" = 'Analysis', "Category" = 'Reasoning',
  "Capabilities" = 'reasoning,analysis,code,chain-of-thought', "DefaultUseCases" = 'contract-generation,code-analysis,change-analysis',
  "SensitivityLevel" = 2, "ContextWindow" = 131072, "RequiresGpu" = true, "RecommendedRamGb" = 16.0
WHERE "Id" = 'aa000002-0001-0000-0000-000000000001';

UPDATE aik_models SET "Name" = 'deepseek-r1:671b', "Slug" = 'deepseek-r1-671b', "DisplayName" = 'DeepSeek R1 671B (Heavy Reasoning)',
  "ExternalModelId" = 'deepseek-r1:671b', "Capabilities" = 'reasoning,analysis,chain-of-thought,deep-analysis',
  "DefaultUseCases" = 'change-analysis,incident-explanation,blast-radius',
  "SensitivityLevel" = 3, "ContextWindow" = 131072, "RequiresGpu" = true, "RecommendedRamGb" = 128.0
WHERE "Id" = 'aa000003-0001-0000-0000-000000000001';

UPDATE aik_models SET "Name" = 'llama3.1:8b', "Slug" = 'llama3.1-8b', "DisplayName" = 'Llama 3.1 8B (Completion)',
  "ExternalModelId" = 'llama3.1:8b', "Capabilities" = 'completion,summarisation,concise,multilingual',
  "ContextWindow" = 131072, "RecommendedRamGb" = 8.0
WHERE "Id" = 'aa000004-0001-0000-0000-000000000001';

INSERT INTO aik_models (
  "Id", "Name", "Slug", "DisplayName",
  "Provider", "ProviderId", "ExternalModelId",
  "ModelType", "Category",
  "IsInternal", "IsExternal", "IsInstalled",
  "Status", "Capabilities", "DefaultUseCases",
  "SensitivityLevel",
  "IsDefaultForChat", "IsDefaultForReasoning", "IsDefaultForEmbeddings",
  "SupportsStreaming", "SupportsToolCalling", "SupportsEmbeddings", "SupportsVision", "SupportsStructuredOutput",
  "ContextWindow", "RequiresGpu", "RecommendedRamGb",
  "LicenseName", "LicenseUrl", "ComplianceStatus",
  "RegisteredAt",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'aa000001-0001-0000-0000-000000000001',
  'qwen3.5:9b', 'qwen3.5-9b', 'Qwen 3.5 9B (Chat)',
  'ollama-local', 'cc000001-0001-0000-0000-000000000001', 'qwen3.5:9b',
  'Chat', 'General',
  true, false, true,
  'Active', 'chat,reasoning,multilingual', 'service-lookup,incident-explanation,executive-summary',
  1,
  true, false, false,
  true, false, false, false, true,
  32768, false, 8.0,
  'Apache 2.0', 'https://www.apache.org/licenses/LICENSE-2.0', 'Approved',
  NOW(),
  NOW(), 'system', NOW(), 'system', false
),
(
  'aa000002-0001-0000-0000-000000000001',
  'deepseek-r1:14b', 'deepseek-r1-14b', 'DeepSeek R1 14B (Analysis)',
  'ollama-local', 'cc000001-0001-0000-0000-000000000001', 'deepseek-r1:14b',
  'Analysis', 'Reasoning',
  true, false, true,
  'Active', 'reasoning,analysis,code,chain-of-thought', 'contract-generation,code-analysis,change-analysis',
  2,
  false, true, false,
  true, true, false, false, true,
  131072, true, 16.0,
  'Apache 2.0', 'https://www.apache.org/licenses/LICENSE-2.0', 'Approved',
  NOW(),
  NOW(), 'system', NOW(), 'system', false
),
(
  'aa000003-0001-0000-0000-000000000001',
  'deepseek-r1:671b', 'deepseek-r1-671b', 'DeepSeek R1 671B (Heavy Reasoning)',
  'ollama-local', 'cc000001-0001-0000-0000-000000000001', 'deepseek-r1:671b',
  'Analysis', 'Reasoning',
  true, false, true,
  'Active', 'reasoning,analysis,chain-of-thought,deep-analysis', 'change-analysis,incident-explanation,blast-radius',
  3,
  false, true, false,
  true, true, false, false, true,
  131072, true, 128.0,
  'Apache 2.0', 'https://www.apache.org/licenses/LICENSE-2.0', 'Approved',
  NOW(),
  NOW(), 'system', NOW(), 'system', false
),
(
  'aa000004-0001-0000-0000-000000000001',
  'llama3.1:8b', 'llama3.1-8b', 'Llama 3.1 8B (Completion)',
  'ollama-local', 'cc000001-0001-0000-0000-000000000001', 'llama3.1:8b',
  'Completion', 'Summarization',
  true, false, true,
  'Active', 'completion,summarisation,concise,multilingual', 'executive-summary,post-change-summary',
  1,
  false, false, false,
  true, false, false, false, false,
  131072, false, 8.0,
  'Meta Llama Community License', 'https://llama.meta.com/llama3/license/', 'Approved',
  NOW(),
  NOW(), 'system', NOW(), 'system', false
),
(
  'aa000005-0001-0000-0000-000000000001',
  'gpt-4o-mini', 'gpt-4o-mini', 'GPT-4o Mini (External)',
  'openai', 'cc000002-0001-0000-0000-000000000001', 'gpt-4o-mini',
  'Chat', 'General',
  false, true, false,
  'Inactive', 'chat,reasoning,multilingual,code', 'executive-summary,contract-explanation',
  2,
  false, false, false,
  true, true, false, true, true,
  128000, true, 32.0,
  'Proprietary', 'https://openai.com/policies/terms-of-use', 'PendingReview',
  NOW(),
  NOW(), 'system', NOW(), 'system', false
)
ON CONFLICT DO NOTHING;

-- ═══ ACCESS POLICIES ═════════════════════════════════════════════════════════

INSERT INTO aik_access_policies (
  "Id", "Name", "Description", "Scope", "ScopeValue",
  "AllowedModelIds", "BlockedModelIds",
  "AllowExternalAI", "InternalOnly", "MaxTokensPerRequest",
  "EnvironmentRestrictions",
  "IsActive",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'dd000001-0001-0000-0000-000000000001',
  'default-internal-policy', 'Default policy: internal models only for all personas.',
  'persona', 'all',
  '', '',
  false, true, 8192,
  '',
  true,
  NOW(), 'system', NOW(), 'system', false
),
(
  'dd000002-0001-0000-0000-000000000001',
  'platform-admin-extended', 'Extended policy for platform admins: all models, higher token limit.',
  'role', 'platform-admin',
  '', '',
  true, false, 32768,
  '',
  true,
  NOW(), 'system', NOW(), 'system', false
)
ON CONFLICT DO NOTHING;

-- ═══ BUDGETS ═════════════════════════════════════════════════════════════════

INSERT INTO aik_budgets (
  "Id", "Name", "Scope", "ScopeValue",
  "Period", "MaxTokens", "MaxRequests",
  "CurrentTokensUsed", "CurrentRequestCount",
  "PeriodStartDate", "IsActive",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'ee000001-0001-0000-0000-000000000001',
  'default-monthly-budget', 'global', '',
  'Monthly', 5000000, 10000,
  0, 0,
  NOW(), true,
  NOW(), 'system', NOW(), 'system', false
)
ON CONFLICT DO NOTHING;

-- ═══ ROUTING STRATEGIES ══════════════════════════════════════════════════════

INSERT INTO aik_routing_strategies (
  "Id", "Name", "Description",
  "TargetPersona", "TargetUseCase", "TargetClientType",
  "PreferredPath", "MaxSensitivityLevel", "AllowExternalEscalation",
  "Priority", "IsActive", "RegisteredAt",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'ff000001-0001-0000-0000-000000000001',
  'internal-only-default', 'Default strategy: route all requests through internal models only.',
  '*', '*', '*',
  'InternalOnly', 5, false,
  100, true, NOW(),
  NOW(), 'system', NOW(), 'system', false
),
(
  'ff000002-0001-0000-0000-000000000001',
  'code-generation-advanced', 'Route contract and code generation to code-optimised models.',
  '*', 'ContractGeneration', '*',
  'InternalOnly', 3, false,
  10, true, NOW(),
  NOW(), 'system', NOW(), 'system', false
)
ON CONFLICT DO NOTHING;

-- ═══ KNOWLEDGE SOURCES ═══════════════════════════════════════════════════════

INSERT INTO aik_knowledge_sources (
  "Id", "Name", "Description",
  "SourceType", "EndpointOrPath",
  "Priority", "IsActive", "RegisteredAt",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'bb000001-0002-0000-0000-000000000001',
  'service-catalog', 'Service Catalog — services, ownership, teams, dependencies.',
  'Service', 'internal://catalog',
  1, true, NOW(),
  NOW(), 'system', NOW(), 'system', false
),
(
  'bb000002-0002-0000-0000-000000000001',
  'contract-registry', 'Contract Registry — REST, SOAP, Event and background service contracts.',
  'Contract', 'internal://contracts',
  2, true, NOW(),
  NOW(), 'system', NOW(), 'system', false
),
(
  'bb000003-0002-0000-0000-000000000001',
  'change-intelligence', 'Change Intelligence — deploy events, blast radius, confidence scores.',
  'Change', 'internal://changes',
  3, true, NOW(),
  NOW(), 'system', NOW(), 'system', false
),
(
  'bb000004-0002-0000-0000-000000000001',
  'incident-store', 'Incident Store — active incidents, timeline, mitigation history.',
  'Incident', 'internal://incidents',
  4, true, NOW(),
  NOW(), 'system', NOW(), 'system', false
),
(
  'bb000005-0002-0000-0000-000000000001',
  'runbook-library', 'Runbook Library — operational runbooks, step-by-step guides.',
  'Runbook', 'internal://runbooks',
  5, true, NOW(),
  NOW(), 'system', NOW(), 'system', false
),
(
  'bb000006-0002-0000-0000-000000000001',
  'source-of-truth', 'Source of Truth — authoritative data across services, contracts, environments.',
  'SourceOfTruth', 'internal://source-of-truth',
  6, true, NOW(),
  NOW(), 'system', NOW(), 'system', false
),
(
  'bb000007-0002-0000-0000-000000000001',
  'telemetry-summary', 'Telemetry Summary — aggregated metrics, error rates, latency data.',
  'TelemetrySummary', 'internal://telemetry',
  7, true, NOW(),
  NOW(), 'system', NOW(), 'system', false
),
(
  'bb000008-0002-0000-0000-000000000001',
  'knowledge-docs', 'Knowledge Documentation — internal docs, wiki, technical notes.',
  'Documentation', 'internal://knowledge',
  8, true, NOW(),
  NOW(), 'system', NOW(), 'system', false
)
ON CONFLICT DO NOTHING;

-- ═══ KNOWLEDGE SOURCE WEIGHTS ════════════════════════════════════════════════
-- Pesos padrão de fontes por caso de uso. Configúraveis por administradores.
-- TrustLevel: 1-5 (5 = máxima confiança). Weight: 0-100 (percentual de relevância).

INSERT INTO aik_source_weights (
  "Id", "SourceType", "UseCaseType", "Relevance",
  "Weight", "TrustLevel", "IsActive", "ConfiguredAt",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
-- ServiceLookup
('10000001-0001-0000-0000-000000000001', 'Service', 'ServiceLookup', 'Primary', 60, 5, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000002-0001-0000-0000-000000000001', 'Contract', 'ServiceLookup', 'Secondary', 25, 4, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000003-0001-0000-0000-000000000001', 'Documentation', 'ServiceLookup', 'Supplementary', 15, 3, true, NOW(), NOW(), 'system', NOW(), 'system', false),
-- ContractExplanation
('10000004-0001-0000-0000-000000000001', 'Contract', 'ContractExplanation', 'Primary', 55, 5, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000005-0001-0000-0000-000000000001', 'Service', 'ContractExplanation', 'Secondary', 25, 4, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000006-0001-0000-0000-000000000001', 'SourceOfTruth', 'ContractExplanation', 'Secondary', 20, 4, true, NOW(), NOW(), 'system', NOW(), 'system', false),
-- ContractGeneration
('10000007-0001-0000-0000-000000000001', 'Contract', 'ContractGeneration', 'Primary', 50, 5, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000008-0001-0000-0000-000000000001', 'Service', 'ContractGeneration', 'Secondary', 25, 4, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000009-0001-0000-0000-000000000001', 'SourceOfTruth', 'ContractGeneration', 'Supplementary', 15, 4, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000010-0001-0000-0000-000000000001', 'Documentation', 'ContractGeneration', 'Supplementary', 10, 3, true, NOW(), NOW(), 'system', NOW(), 'system', false),
-- IncidentExplanation
('10000011-0001-0000-0000-000000000001', 'Incident', 'IncidentExplanation', 'Primary', 40, 5, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000012-0001-0000-0000-000000000001', 'Change', 'IncidentExplanation', 'Secondary', 25, 5, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000013-0001-0000-0000-000000000001', 'Runbook', 'IncidentExplanation', 'Secondary', 20, 4, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000014-0001-0000-0000-000000000001', 'TelemetrySummary', 'IncidentExplanation', 'Supplementary', 15, 3, true, NOW(), NOW(), 'system', NOW(), 'system', false),
-- MitigationGuidance
('10000015-0001-0000-0000-000000000001', 'Runbook', 'MitigationGuidance', 'Primary', 40, 5, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000016-0001-0000-0000-000000000001', 'Incident', 'MitigationGuidance', 'Primary', 30, 5, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000017-0001-0000-0000-000000000001', 'Service', 'MitigationGuidance', 'Secondary', 15, 4, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000018-0001-0000-0000-000000000001', 'TelemetrySummary', 'MitigationGuidance', 'Supplementary', 15, 3, true, NOW(), NOW(), 'system', NOW(), 'system', false),
-- ChangeAnalysis
('10000019-0001-0000-0000-000000000001', 'Change', 'ChangeAnalysis', 'Primary', 45, 5, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000020-0001-0000-0000-000000000001', 'Service', 'ChangeAnalysis', 'Secondary', 25, 4, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000021-0001-0000-0000-000000000001', 'Incident', 'ChangeAnalysis', 'Secondary', 20, 4, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000022-0001-0000-0000-000000000001', 'TelemetrySummary', 'ChangeAnalysis', 'Supplementary', 10, 3, true, NOW(), NOW(), 'system', NOW(), 'system', false),
-- ExecutiveSummary
('10000023-0001-0000-0000-000000000001', 'SourceOfTruth', 'ExecutiveSummary', 'Primary', 40, 5, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000024-0001-0000-0000-000000000001', 'Service', 'ExecutiveSummary', 'Secondary', 30, 4, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000025-0001-0000-0000-000000000001', 'TelemetrySummary', 'ExecutiveSummary', 'Secondary', 30, 3, true, NOW(), NOW(), 'system', NOW(), 'system', false),
-- RiskComplianceExplanation
('10000026-0001-0000-0000-000000000001', 'SourceOfTruth', 'RiskComplianceExplanation', 'Primary', 40, 5, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000027-0001-0000-0000-000000000001', 'Service', 'RiskComplianceExplanation', 'Secondary', 30, 4, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000028-0001-0000-0000-000000000001', 'Documentation', 'RiskComplianceExplanation', 'Secondary', 30, 4, true, NOW(), NOW(), 'system', NOW(), 'system', false),
-- FinOpsExplanation
('10000029-0001-0000-0000-000000000001', 'TelemetrySummary', 'FinOpsExplanation', 'Primary', 45, 4, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000030-0001-0000-0000-000000000001', 'Service', 'FinOpsExplanation', 'Secondary', 35, 4, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000031-0001-0000-0000-000000000001', 'SourceOfTruth', 'FinOpsExplanation', 'Supplementary', 20, 4, true, NOW(), NOW(), 'system', NOW(), 'system', false),
-- DependencyReasoning
('10000032-0001-0000-0000-000000000001', 'Service', 'DependencyReasoning', 'Primary', 45, 5, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000033-0001-0000-0000-000000000001', 'Contract', 'DependencyReasoning', 'Primary', 35, 5, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000034-0001-0000-0000-000000000001', 'Change', 'DependencyReasoning', 'Secondary', 20, 4, true, NOW(), NOW(), 'system', NOW(), 'system', false),
-- General
('10000035-0001-0000-0000-000000000001', 'Service', 'General', 'Primary', 35, 4, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000036-0001-0000-0000-000000000001', 'Contract', 'General', 'Secondary', 25, 4, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000037-0001-0000-0000-000000000001', 'SourceOfTruth', 'General', 'Secondary', 20, 4, true, NOW(), NOW(), 'system', NOW(), 'system', false),
('10000038-0001-0000-0000-000000000001', 'Documentation', 'General', 'Supplementary', 20, 3, true, NOW(), NOW(), 'system', NOW(), 'system', false)
ON CONFLICT DO NOTHING;

-- ═══ AGENTS ══════════════════════════════════════════════════════════════════

-- Fix existing rows that were seeded with invalid enum values
UPDATE aik_agents
SET "OwnershipType" = 'System'
WHERE "OwnershipType" = 'Platform';

UPDATE aik_agents
SET "Visibility" = 'Tenant'
WHERE "Visibility" = 'Public';

INSERT INTO aik_agents (
  "Id", "Name", "DisplayName", "Slug", "Description",
  "Category", "SystemPrompt", "Capabilities", "TargetPersona",
  "Icon", "PreferredModelId",
  "IsOfficial", "IsActive",
  "OwnershipType", "Visibility", "PublicationStatus",
  "OwnerId", "OwnerTeamId",
  "AllowedModelIds", "AllowedTools", "Objective",
  "InputSchema", "OutputSchema",
  "AllowModelOverride", "Version", "ExecutionCount", "SortOrder",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'a9000001-0001-0000-0000-000000000001',
  'contract-assistant', 'Contract Assistant', 'contract-assistant',
  'Assists with understanding, generating, and validating API, SOAP, and event contracts.',
  'ContractGovernance',
  'You are a contract governance assistant for NexTraceOne. Your role is to help engineers understand existing contracts, generate new contracts following NexTraceOne standards, identify breaking changes, and ensure contracts are consistent with service ownership. Always reference contract ownership, versioning, and compatibility when responding.',
  'contract-explanation,contract-generation,compatibility-analysis,schema-validation',
  'Engineer,TechLead,Architect',
  'FileText', NULL,
  true, true,
  'System', 'Tenant', 'Published',
  'system', 'platform-team',
  '', 'list_services', 'Help engineers manage and govern service contracts',
  '{}', '{}',
  false, 1, 0, 1,
  NOW(), 'system', NOW(), 'system', false
),
(
  'a9000002-0001-0000-0000-000000000001',
  'incident-investigator', 'Incident Investigator', 'incident-investigator',
  'Correlates incidents with recent changes, blast radius, and suggests mitigation paths.',
  'IncidentResponse',
  'You are an incident investigation assistant for NexTraceOne. Your role is to help engineers investigate active incidents by correlating them with recent deployments, service changes, and blast radius. Use runbooks, telemetry summaries, and change intelligence to recommend mitigation actions.',
  'incident-correlation,change-analysis,runbook-guidance,blast-radius',
  'Engineer,TechLead',
  'AlertTriangle', NULL,
  true, true,
  'System', 'Tenant', 'Published',
  'system', 'platform-team',
  '', 'list_services,list_recent_changes,get_service_health', 'Accelerate incident resolution through change correlation and mitigation guidance',
  '{}', '{}',
  false, 1, 0, 2,
  NOW(), 'system', NOW(), 'system', false
),
(
  'a9000003-0001-0000-0000-000000000001',
  'change-risk-analyst', 'Change Risk Analyst', 'change-risk-analyst',
  'Evaluates change risk, blast radius, and promotion readiness across environments.',
  'ChangeIntelligence',
  'You are a change intelligence assistant for NexTraceOne. Your role is to evaluate change risk, estimate blast radius, assess promotion readiness between environments, and provide confidence scoring for production deployments. Always consider service dependencies, contract compatibility, and historical incident patterns.',
  'change-analysis,blast-radius,promotion-readiness,risk-scoring',
  'TechLead,Architect,Product',
  'GitBranch', NULL,
  true, true,
  'System', 'Tenant', 'Published',
  'system', 'platform-team',
  '', 'list_services,list_recent_changes', 'Improve confidence in production changes by quantifying risk and promotion readiness',
  '{}', '{}',
  false, 1, 0, 3,
  NOW(), 'system', NOW(), 'system', false
)
ON CONFLICT DO NOTHING;
