-- ═══════════════════════════════════════════════════════════════════════════════
-- NEXTRACEONE — Seed data: AI Knowledge Module (AiGovernanceDatabase)
-- Tables: aik_models, aik_providers, aik_routing_strategies, aik_knowledge_sources,
--         aik_source_weights, aik_agents, aik_access_policies, aik_budgets
-- All INSERT statements are idempotent: ON CONFLICT DO NOTHING.
-- ═══════════════════════════════════════════════════════════════════════════════

-- ═══ PROVIDERS ═══════════════════════════════════════════════════════════════

INSERT INTO aik_providers (
  "Id", "Name", "DisplayName", "Description",
  "BaseUrl", "ApiVersion", "IsEnabled", "Priority",
  "HealthStatus", "HealthCheckedAt",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'cc000001-0001-0000-0000-000000000001',
  'ollama-local', 'Ollama (Local)', 'Local AI provider via Ollama runtime.',
  'http://localhost:11434', 'v1', true, 1,
  'Unknown', NULL,
  NOW(), 'system', NOW(), 'system', false
),
(
  'cc000002-0001-0000-0000-000000000001',
  'openai', 'OpenAI', 'OpenAI external AI provider (requires API key).',
  'https://api.openai.com', 'v1', false, 10,
  'Unknown', NULL,
  NOW(), 'system', NOW(), 'system', false
)
ON CONFLICT DO NOTHING;

-- ═══ MODELS ══════════════════════════════════════════════════════════════════

INSERT INTO aik_models (
  "Id", "Name", "DisplayName", "Description",
  "Provider", "ModelType", "Status",
  "IsInternal", "MaxTokens", "ContextWindow",
  "Capabilities", "Metadata",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'aa000001-0001-0000-0000-000000000001',
  'qwen2.5:7b', 'Qwen 2.5 7B (Chat)', 'General-purpose chat model via Ollama. Balanced performance for most operational queries.',
  'ollama-local', 'Chat', 'Active',
  true, 8192, 32768,
  'chat,reasoning,multilingual', '{}',
  NOW(), 'system', NOW(), 'system', false
),
(
  'aa000002-0001-0000-0000-000000000001',
  'qwen2.5-coder:7b', 'Qwen 2.5 Coder 7B (Code)', 'Code-optimised model via Ollama. Used for contract generation and code analysis.',
  'ollama-local', 'CodeGeneration', 'Active',
  true, 8192, 32768,
  'code,generation,analysis', '{}',
  NOW(), 'system', NOW(), 'system', false
),
(
  'aa000003-0001-0000-0000-000000000001',
  'deepseek-r1:8b', 'DeepSeek R1 8B (Analysis)', 'Reasoning-optimised model via Ollama. Used for change analysis and incident explanation.',
  'ollama-local', 'Analysis', 'Active',
  true, 8192, 65536,
  'reasoning,analysis,chain-of-thought', '{}',
  NOW(), 'system', NOW(), 'system', false
),
(
  'aa000004-0001-0000-0000-000000000001',
  'llama3.2:3b', 'Llama 3.2 3B (Completion)', 'Lightweight completion model via Ollama. Used for executive summaries and concise responses.',
  'ollama-local', 'Completion', 'Active',
  true, 4096, 8192,
  'completion,summarisation,concise', '{}',
  NOW(), 'system', NOW(), 'system', false
),
(
  'aa000005-0001-0000-0000-000000000001',
  'gpt-4o-mini', 'GPT-4o Mini (External)', 'OpenAI external model — requires API key and explicit policy enablement.',
  'openai', 'Chat', 'Inactive',
  false, 16384, 128000,
  'chat,reasoning,multilingual,code', '{}',
  NOW(), 'system', NOW(), 'system', false
)
ON CONFLICT DO NOTHING;

-- ═══ ACCESS POLICIES ═════════════════════════════════════════════════════════

INSERT INTO aik_access_policies (
  "Id", "Name", "Description", "Scope",
  "AllowedPersonas", "AllowedClientTypes", "MaxTokensPerRequest",
  "IsActive",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'dd000001-0001-0000-0000-000000000001',
  'default-internal-policy', 'Default policy: internal models only for all personas.',
  'global',
  'Engineer,TechLead,Architect,Product,Executive,PlatformAdmin,Auditor',
  'WebApp,IDEExtension,API',
  8192, true,
  NOW(), 'system', NOW(), 'system', false
),
(
  'dd000002-0001-0000-0000-000000000001',
  'platform-admin-extended', 'Extended policy for platform admins: all models, higher token limit.',
  'role:platform-admin',
  'PlatformAdmin',
  'WebApp,IDEExtension,API',
  32768, true,
  NOW(), 'system', NOW(), 'system', false
)
ON CONFLICT DO NOTHING;

-- ═══ BUDGETS ═════════════════════════════════════════════════════════════════

INSERT INTO aik_budgets (
  "Id", "Name", "Description", "Scope",
  "TotalTokenLimit", "UsedTokens",
  "Period", "IsActive",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'ee000001-0001-0000-0000-000000000001',
  'default-monthly-budget', 'Default monthly token budget — all users.',
  'global',
  5000000, 0,
  'Monthly', true,
  NOW(), 'system', NOW(), 'system', false
)
ON CONFLICT DO NOTHING;

-- ═══ ROUTING STRATEGIES ══════════════════════════════════════════════════════

INSERT INTO aik_routing_strategies (
  "Id", "Name", "Description",
  "ApplicablePersonas", "ApplicableUseCases", "ApplicableClientTypes",
  "PreferredPath", "AllowExternalEscalation",
  "Priority", "IsActive",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'ff000001-0001-0000-0000-000000000001',
  'internal-only-default', 'Default strategy: route all requests through internal models only.',
  'Engineer,TechLead,Architect,Product,Executive,PlatformAdmin,Auditor',
  'General,ServiceLookup,ContractExplanation,IncidentExplanation,MitigationGuidance,ChangeAnalysis,ExecutiveSummary,RiskComplianceExplanation,FinOpsExplanation,DependencyReasoning',
  'WebApp,IDEExtension,API',
  'InternalOnly', false,
  100, true,
  NOW(), 'system', NOW(), 'system', false
),
(
  'ff000002-0001-0000-0000-000000000001',
  'code-generation-advanced', 'Route contract and code generation to code-optimised models.',
  'Engineer,TechLead,Architect',
  'ContractGeneration',
  'WebApp,IDEExtension',
  'InternalOnly', false,
  10, true,
  NOW(), 'system', NOW(), 'system', false
)
ON CONFLICT DO NOTHING;

-- ═══ KNOWLEDGE SOURCES ═══════════════════════════════════════════════════════

INSERT INTO aik_knowledge_sources (
  "Id", "Name", "Description",
  "SourceType", "EndpointOrPath",
  "Priority", "IsActive",
  "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
) VALUES
(
  'bb000001-0002-0000-0000-000000000001',
  'service-catalog', 'Service Catalog — services, ownership, teams, dependencies.',
  'Service', 'internal://catalog',
  1, true,
  NOW(), 'system', NOW(), 'system', false
),
(
  'bb000002-0002-0000-0000-000000000001',
  'contract-registry', 'Contract Registry — REST, SOAP, Event and background service contracts.',
  'Contract', 'internal://contracts',
  2, true,
  NOW(), 'system', NOW(), 'system', false
),
(
  'bb000003-0002-0000-0000-000000000001',
  'change-intelligence', 'Change Intelligence — deploy events, blast radius, confidence scores.',
  'Change', 'internal://changes',
  3, true,
  NOW(), 'system', NOW(), 'system', false
),
(
  'bb000004-0002-0000-0000-000000000001',
  'incident-store', 'Incident Store — active incidents, timeline, mitigation history.',
  'Incident', 'internal://incidents',
  4, true,
  NOW(), 'system', NOW(), 'system', false
),
(
  'bb000005-0002-0000-0000-000000000001',
  'runbook-library', 'Runbook Library — operational runbooks, step-by-step guides.',
  'Runbook', 'internal://runbooks',
  5, true,
  NOW(), 'system', NOW(), 'system', false
),
(
  'bb000006-0002-0000-0000-000000000001',
  'source-of-truth', 'Source of Truth — authoritative data across services, contracts, environments.',
  'SourceOfTruth', 'internal://source-of-truth',
  6, true,
  NOW(), 'system', NOW(), 'system', false
),
(
  'bb000007-0002-0000-0000-000000000001',
  'telemetry-summary', 'Telemetry Summary — aggregated metrics, error rates, latency data.',
  'TelemetrySummary', 'internal://telemetry',
  7, true,
  NOW(), 'system', NOW(), 'system', false
),
(
  'bb000008-0002-0000-0000-000000000001',
  'knowledge-docs', 'Knowledge Documentation — internal docs, wiki, technical notes.',
  'Documentation', 'internal://knowledge',
  8, true,
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
  'ag000001-0001-0000-0000-000000000001',
  'contract-assistant', 'Contract Assistant', 'contract-assistant',
  'Assists with understanding, generating, and validating API, SOAP, and event contracts.',
  'ContractGovernance',
  'You are a contract governance assistant for NexTraceOne. Your role is to help engineers understand existing contracts, generate new contracts following NexTraceOne standards, identify breaking changes, and ensure contracts are consistent with service ownership. Always reference contract ownership, versioning, and compatibility when responding.',
  'contract-explanation,contract-generation,compatibility-analysis,schema-validation',
  'Engineer,TechLead,Architect',
  'FileText', NULL,
  true, true,
  'Platform', 'Public', 'Published',
  'system', 'platform-team',
  '', 'list_services', 'Help engineers manage and govern service contracts.',
  '{}', '{}',
  false, 1, 0, 1,
  NOW(), 'system', NOW(), 'system', false
),
(
  'ag000002-0001-0000-0000-000000000001',
  'incident-investigator', 'Incident Investigator', 'incident-investigator',
  'Correlates incidents with recent changes, blast radius, and suggests mitigation paths.',
  'IncidentResponse',
  'You are an incident investigation assistant for NexTraceOne. Your role is to help engineers investigate active incidents by correlating them with recent deployments, service changes, and blast radius. Use runbooks, telemetry summaries, and change intelligence to recommend mitigation actions.',
  'incident-correlation,change-analysis,runbook-guidance,blast-radius',
  'Engineer,TechLead',
  'AlertTriangle', NULL,
  true, true,
  'Platform', 'Public', 'Published',
  'system', 'platform-team',
  '', 'list_services,list_recent_changes,get_service_health', 'Accelerate incident resolution through change correlation and mitigation guidance.',
  '{}', '{}',
  false, 1, 0, 2,
  NOW(), 'system', NOW(), 'system', false
),
(
  'ag000003-0001-0000-0000-000000000001',
  'change-risk-analyst', 'Change Risk Analyst', 'change-risk-analyst',
  'Evaluates change risk, blast radius, and promotion readiness across environments.',
  'ChangeIntelligence',
  'You are a change intelligence assistant for NexTraceOne. Your role is to evaluate change risk, estimate blast radius, assess promotion readiness between environments, and provide confidence scoring for production deployments. Always consider service dependencies, contract compatibility, and historical incident patterns.',
  'change-analysis,blast-radius,promotion-readiness,risk-scoring',
  'TechLead,Architect,Product',
  'GitBranch', NULL,
  true, true,
  'Platform', 'Public', 'Published',
  'system', 'platform-team',
  '', 'list_services,list_recent_changes', 'Improve confidence in production changes by quantifying risk and promotion readiness.',
  '{}', '{}',
  false, 1, 0, 3,
  NOW(), 'system', NOW(), 'system', false
)
ON CONFLICT DO NOTHING;
