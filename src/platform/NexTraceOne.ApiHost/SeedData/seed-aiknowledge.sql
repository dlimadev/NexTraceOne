-- ============================================================================
-- NexTraceOne AI Knowledge Seed Data
-- Idempotent: uses ON CONFLICT DO NOTHING
-- Includes: Providers, Models, Access Policies, Sources, Token Quota Policies
-- Table/column names must match EF Core entity configurations exactly.
-- ============================================================================

-- ── AI PROVIDERS ────────────────────────────────────────────────────────────

-- Provider: Ollama (local, default)
INSERT INTO "AiProviders" ("Id", "Name", "Slug", "DisplayName", "ProviderType", "BaseUrl", "IsLocal", "IsExternal", "IsEnabled", "AuthenticationMode", "SupportedCapabilities", "SupportsChat", "SupportsEmbeddings", "SupportsTools", "SupportsVision", "SupportsStructuredOutput", "HealthStatus", "Priority", "TimeoutSeconds", "Description", "RegisteredAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES (
    'c0000000-0000-0000-0000-000000000001',
    'ollama',
    'ollama',
    'Ollama (Local)',
    'ollama',
    'http://localhost:11434',
    true,
    false,
    true,
    'None',
    'chat,reasoning,embeddings,streaming',
    true,
    true,
    false,
    false,
    false,
    'Unknown',
    1,
    30,
    'Local AI provider using Ollama. Hosts models like DeepSeek, Llama, Mistral.',
    NOW(),
    NOW(), 'system', NOW(), 'system',
    false
) ON CONFLICT DO NOTHING;

-- Provider: OpenAI (external, future)
INSERT INTO "AiProviders" ("Id", "Name", "Slug", "DisplayName", "ProviderType", "BaseUrl", "IsLocal", "IsExternal", "IsEnabled", "AuthenticationMode", "SupportedCapabilities", "SupportsChat", "SupportsEmbeddings", "SupportsTools", "SupportsVision", "SupportsStructuredOutput", "HealthStatus", "Priority", "TimeoutSeconds", "Description", "RegisteredAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES (
    'c0000000-0000-0000-0000-000000000002',
    'openai',
    'openai',
    'OpenAI',
    'openai',
    'https://api.openai.com/v1',
    false,
    true,
    false,
    'ApiKey',
    'chat,reasoning,embeddings,vision,tool-calling,streaming,structured-output',
    true,
    true,
    true,
    true,
    true,
    'Unknown',
    10,
    60,
    'OpenAI external provider (GPT-4, GPT-4o). Requires API key and token quota.',
    NOW(),
    NOW(), 'system', NOW(), 'system',
    false
) ON CONFLICT DO NOTHING;

-- Provider: Azure OpenAI (external, future)
INSERT INTO "AiProviders" ("Id", "Name", "Slug", "DisplayName", "ProviderType", "BaseUrl", "IsLocal", "IsExternal", "IsEnabled", "AuthenticationMode", "SupportedCapabilities", "SupportsChat", "SupportsEmbeddings", "SupportsTools", "SupportsVision", "SupportsStructuredOutput", "HealthStatus", "Priority", "TimeoutSeconds", "Description", "RegisteredAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES (
    'c0000000-0000-0000-0000-000000000003',
    'azure-openai',
    'azure-openai',
    'Azure OpenAI',
    'azure-openai',
    '',
    false,
    true,
    false,
    'ManagedIdentity',
    'chat,reasoning,embeddings,vision,tool-calling,streaming',
    true,
    true,
    true,
    true,
    false,
    'Unknown',
    11,
    60,
    'Azure OpenAI Service. Requires Azure endpoint and API key.',
    NOW(),
    NOW(), 'system', NOW(), 'system',
    false
) ON CONFLICT DO NOTHING;

-- Provider: Gemini (external, future)
INSERT INTO "AiProviders" ("Id", "Name", "Slug", "DisplayName", "ProviderType", "BaseUrl", "IsLocal", "IsExternal", "IsEnabled", "AuthenticationMode", "SupportedCapabilities", "SupportsChat", "SupportsEmbeddings", "SupportsTools", "SupportsVision", "SupportsStructuredOutput", "HealthStatus", "Priority", "TimeoutSeconds", "Description", "RegisteredAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES (
    'c0000000-0000-0000-0000-000000000004',
    'gemini',
    'gemini',
    'Google Gemini',
    'gemini',
    'https://generativelanguage.googleapis.com/v1',
    false,
    true,
    false,
    'ApiKey',
    'chat,reasoning,embeddings,vision,streaming',
    true,
    true,
    false,
    true,
    false,
    'Unknown',
    12,
    60,
    'Google Gemini provider. Requires Google API key.',
    NOW(),
    NOW(), 'system', NOW(), 'system',
    false
) ON CONFLICT DO NOTHING;

-- ── AI MODELS ───────────────────────────────────────────────────────────────
-- Note: ModelType and Status are stored as strings (HasConversion<string>())

-- Default AI Model: DeepSeek R1 (1.5B) via Ollama
INSERT INTO ai_gov_models ("Id", "Name", "Slug", "DisplayName", "Provider", "ProviderId", "ExternalModelId", "ModelType", "Category", "IsInternal", "IsExternal", "IsInstalled", "Status", "Capabilities", "DefaultUseCases", "SensitivityLevel", "IsDefaultForChat", "IsDefaultForReasoning", "IsDefaultForEmbeddings", "SupportsStreaming", "SupportsToolCalling", "SupportsEmbeddings", "SupportsVision", "SupportsStructuredOutput", "ContextWindow", "RequiresGpu", "RecommendedRamGb", "LicenseName", "LicenseUrl", "ComplianceStatus", "RegisteredAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES (
    'a0000000-0000-0000-0000-000000000001',
    'deepseek-r1:1.5b',
    'deepseek-r1-1-5b',
    'DeepSeek R1 1.5B',
    'ollama',
    'c0000000-0000-0000-0000-000000000001',
    'deepseek-r1:1.5b',
    'Chat',
    'general',
    true,
    false,
    true,
    'Active',
    'chat,reasoning,completion',
    'general-chat,code-review,analysis',
    1,
    true,
    true,
    false,
    true,
    false,
    false,
    false,
    false,
    4096,
    false,
    3.0,
    'MIT',
    'https://github.com/deepseek-ai/DeepSeek-R1/blob/main/LICENSE',
    'approved',
    NOW(),
    NOW(), 'system', NOW(), 'system',
    false
) ON CONFLICT DO NOTHING;

-- Embedding model: Nomic Embed Text via Ollama
INSERT INTO ai_gov_models ("Id", "Name", "Slug", "DisplayName", "Provider", "ProviderId", "ExternalModelId", "ModelType", "Category", "IsInternal", "IsExternal", "IsInstalled", "Status", "Capabilities", "DefaultUseCases", "SensitivityLevel", "IsDefaultForChat", "IsDefaultForReasoning", "IsDefaultForEmbeddings", "SupportsStreaming", "SupportsToolCalling", "SupportsEmbeddings", "SupportsVision", "SupportsStructuredOutput", "ContextWindow", "RequiresGpu", "RecommendedRamGb", "LicenseName", "LicenseUrl", "ComplianceStatus", "RegisteredAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES (
    'a0000000-0000-0000-0000-000000000002',
    'nomic-embed-text',
    'nomic-embed-text',
    'Nomic Embed Text',
    'ollama',
    'c0000000-0000-0000-0000-000000000001',
    'nomic-embed-text',
    'Embedding',
    'embeddings',
    true,
    false,
    false,
    'Inactive',
    'embeddings',
    'semantic-search,rag',
    1,
    false,
    false,
    true,
    false,
    false,
    true,
    false,
    false,
    8192,
    false,
    1.0,
    'Apache-2.0',
    'https://huggingface.co/nomic-ai/nomic-embed-text-v1.5',
    'approved',
    NOW(),
    NOW(), 'system', NOW(), 'system',
    false
) ON CONFLICT DO NOTHING;

-- Future external model placeholder: GPT-4o via OpenAI (disabled)
INSERT INTO ai_gov_models ("Id", "Name", "Slug", "DisplayName", "Provider", "ProviderId", "ExternalModelId", "ModelType", "Category", "IsInternal", "IsExternal", "IsInstalled", "Status", "Capabilities", "DefaultUseCases", "SensitivityLevel", "IsDefaultForChat", "IsDefaultForReasoning", "IsDefaultForEmbeddings", "SupportsStreaming", "SupportsToolCalling", "SupportsEmbeddings", "SupportsVision", "SupportsStructuredOutput", "ContextWindow", "RequiresGpu", "RecommendedRamGb", "LicenseName", "LicenseUrl", "ComplianceStatus", "RegisteredAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES (
    'a0000000-0000-0000-0000-000000000003',
    'gpt-4o',
    'gpt-4o',
    'GPT-4o (OpenAI)',
    'openai',
    'c0000000-0000-0000-0000-000000000002',
    'gpt-4o',
    'Chat',
    'general',
    false,
    true,
    false,
    'Inactive',
    'chat,reasoning,vision,tool-calling,streaming,structured-output',
    'premium-analysis,complex-reasoning,code-generation',
    3,
    false,
    false,
    false,
    true,
    true,
    false,
    true,
    true,
    128000,
    false,
    NULL,
    'Proprietary',
    'https://openai.com/policies/terms-of-use',
    'pending-review',
    NOW(),
    NOW(), 'system', NOW(), 'system',
    false
) ON CONFLICT DO NOTHING;

-- ── ACCESS POLICIES ─────────────────────────────────────────────────────────

-- Default access policy: allow all internal models for everyone
INSERT INTO ai_gov_access_policies ("Id", "Name", "Description", "Scope", "ScopeValue", "AllowedModelIds", "BlockedModelIds", "AllowExternalAI", "InternalOnly", "MaxTokensPerRequest", "EnvironmentRestrictions", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES (
    'b0000000-0000-0000-0000-000000000001',
    'default-internal-access',
    'Default policy: allows all internal AI models for all users',
    'role',
    '*',
    '',
    '',
    false,
    true,
    4096,
    '',
    true,
    NOW(), 'system', NOW(), 'system',
    false
) ON CONFLICT DO NOTHING;

-- Premium access policy: allow external models for admins
INSERT INTO ai_gov_access_policies ("Id", "Name", "Description", "Scope", "ScopeValue", "AllowedModelIds", "BlockedModelIds", "AllowExternalAI", "InternalOnly", "MaxTokensPerRequest", "EnvironmentRestrictions", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES (
    'b0000000-0000-0000-0000-000000000002',
    'admin-premium-access',
    'Premium policy: allows admins to use external AI models with governance',
    'role',
    'Admin',
    '',
    '',
    true,
    false,
    8192,
    '',
    true,
    NOW(), 'system', NOW(), 'system',
    false
) ON CONFLICT DO NOTHING;

-- ── AI SOURCES (grounding/retrieval) ────────────────────────────────────────
-- Note: SourceType is stored as string (HasConversion<string>())

-- Document source: API contracts and documentation
INSERT INTO "AiSources" ("Id", "Name", "DisplayName", "SourceType", "Description", "IsEnabled", "ConnectionInfo", "AccessPolicyScope", "Classification", "OwnerTeam", "HealthStatus", "RegisteredAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES (
    'd0000000-0000-0000-0000-000000000001',
    'api-contracts',
    'API Contracts & Documentation',
    'Document',
    'OpenAPI/AsyncAPI contracts, Spectral policies, glossary, changelog, and functional documentation.',
    true,
    'internal://documents/api-contracts',
    'tenant',
    'internal',
    'platform',
    'healthy',
    NOW(),
    NOW(), 'system', NOW(), 'system',
    false
) ON CONFLICT DO NOTHING;

-- Database source: NexTraceOne internal data
INSERT INTO "AiSources" ("Id", "Name", "DisplayName", "SourceType", "Description", "IsEnabled", "ConnectionInfo", "AccessPolicyScope", "Classification", "OwnerTeam", "HealthStatus", "RegisteredAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES (
    'd0000000-0000-0000-0000-000000000002',
    'nextraceone-data',
    'NexTraceOne Internal Data',
    'Database',
    'Internal data from NexTraceOne: APIs, versions, owners, governance states, canonical entities, metadata.',
    true,
    'internal://database/nextraceone',
    'tenant',
    'internal',
    'platform',
    'healthy',
    NOW(),
    NOW(), 'system', NOW(), 'system',
    false
) ON CONFLICT DO NOTHING;

-- Telemetry source: OpenTelemetry logs and traces
INSERT INTO "AiSources" ("Id", "Name", "DisplayName", "SourceType", "Description", "IsEnabled", "ConnectionInfo", "AccessPolicyScope", "Classification", "OwnerTeam", "HealthStatus", "RegisteredAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES (
    'd0000000-0000-0000-0000-000000000003',
    'otel-telemetry',
    'OpenTelemetry Logs & Traces',
    'Telemetry',
    'Structured logs, traces, spans, correlation IDs, operational failures, latency data from OpenTelemetry.',
    true,
    'internal://telemetry/opentelemetry',
    'tenant',
    'operational',
    'platform',
    'healthy',
    NOW(),
    NOW(), 'system', NOW(), 'system',
    false
) ON CONFLICT DO NOTHING;

-- ── TOKEN QUOTA POLICIES ────────────────────────────────────────────────────

-- Default token quota for external AI usage (per user)
INSERT INTO "AiTokenQuotaPolicies" ("Id", "Name", "Description", "Scope", "ScopeValue", "ProviderId", "ModelId", "MaxInputTokensPerRequest", "MaxOutputTokensPerRequest", "MaxTotalTokensPerRequest", "MaxTokensPerDay", "MaxTokensPerMonth", "MaxTokensAccumulated", "IsHardLimit", "AllowSensitiveData", "AllowKnowledgePromotion", "IsEnabled", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES (
    'e0000000-0000-0000-0000-000000000001',
    'default-external-quota',
    'Default token quota for external AI: 4K per request, 50K daily, 500K monthly',
    'user',
    '*',
    NULL,
    NULL,
    4096,
    4096,
    8192,
    50000,
    500000,
    5000000,
    true,
    false,
    false,
    true,
    NOW(), 'system', NOW(), 'system',
    false
) ON CONFLICT DO NOTHING;

-- Premium token quota for admins
INSERT INTO "AiTokenQuotaPolicies" ("Id", "Name", "Description", "Scope", "ScopeValue", "ProviderId", "ModelId", "MaxInputTokensPerRequest", "MaxOutputTokensPerRequest", "MaxTotalTokensPerRequest", "MaxTokensPerDay", "MaxTokensPerMonth", "MaxTokensAccumulated", "IsHardLimit", "AllowSensitiveData", "AllowKnowledgePromotion", "IsEnabled", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES (
    'e0000000-0000-0000-0000-000000000002',
    'admin-premium-quota',
    'Premium quota for admins: 8K per request, 200K daily, 2M monthly, can promote knowledge',
    'user',
    'Admin',
    NULL,
    NULL,
    8192,
    8192,
    16384,
    200000,
    2000000,
    20000000,
    false,
    true,
    true,
    true,
    NOW(), 'system', NOW(), 'system',
    false
) ON CONFLICT DO NOTHING;

-- ============================================================================
-- AI ASSISTANT CONVERSATIONS & MESSAGES (Demo Data)
-- ============================================================================

-- ── Conversation 1: Payment API Investigation ──────────────────────────────

INSERT INTO ai_gov_conversations (
    "Id", "Title", "Persona", "ClientType", "DefaultContextScope",
    "LastModelUsed", "CreatedBy", "MessageCount", "Tags", "IsActive",
    "LastMessageAt", "ServiceId", "ContractId", "IncidentId", "TeamId",
    "CreatedAt", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES (
    'f0000000-0000-0000-0000-000000000001',
    'Payment API latency investigation',
    'Engineer',
    'Web',
    'services,incidents,changes',
    'deepseek-r1:1.5b',
    'system',
    4,
    'troubleshooting,payment,production',
    true,
    NOW() - interval '1 hour',
    NULL, NULL, NULL, NULL,
    NOW() - interval '2 hours', NOW(), 'system', false
) ON CONFLICT DO NOTHING;

-- Conversation 1 Messages
INSERT INTO ai_gov_messages (
    "Id", "ConversationId", "Role", "Content",
    "ModelName", "Provider", "IsInternalModel",
    "PromptTokens", "CompletionTokens", "AppliedPolicyName",
    "GroundingSources", "ContextReferences", "CorrelationId",
    "Timestamp", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES (
    'e1000000-0000-0000-0000-000000000001',
    'f0000000-0000-0000-0000-000000000001',
    'assistant',
    'Welcome! I''m the NexTraceOne AI Assistant. I can help you investigate production issues, analyze contracts, correlate incidents, and provide operational insights. What would you like to explore?',
    'deepseek-r1:1.5b',
    'ollama',
    true,
    0, 42, 'default-internal-access',
    'Service Catalog,Contract Registry',
    '',
    'init-conv1',
    NOW() - interval '2 hours',
    NOW() - interval '2 hours', 'system', NOW(), 'system', false
) ON CONFLICT DO NOTHING;

INSERT INTO ai_gov_messages (
    "Id", "ConversationId", "Role", "Content",
    "ModelName", "Provider", "IsInternalModel",
    "PromptTokens", "CompletionTokens", "AppliedPolicyName",
    "GroundingSources", "ContextReferences", "CorrelationId",
    "Timestamp", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES (
    'e1000000-0000-0000-0000-000000000002',
    'f0000000-0000-0000-0000-000000000001',
    'user',
    'What issues are affecting the payment API right now?',
    NULL, NULL, false,
    0, 0, NULL,
    '', '',
    'user-msg-001',
    NOW() - interval '1 hour 55 minutes',
    NOW() - interval '1 hour 55 minutes', 'system', NOW(), 'system', false
) ON CONFLICT DO NOTHING;

INSERT INTO ai_gov_messages (
    "Id", "ConversationId", "Role", "Content",
    "ModelName", "Provider", "IsInternalModel",
    "PromptTokens", "CompletionTokens", "AppliedPolicyName",
    "GroundingSources", "ContextReferences", "CorrelationId",
    "Timestamp", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES (
    'e1000000-0000-0000-0000-000000000003',
    'f0000000-0000-0000-0000-000000000001',
    'assistant',
    'Based on the Service Catalog and Incident History, the payment-service is currently experiencing elevated latency (p99 > 2s) since 09:45 UTC. There is an active incident INC-2847 correlated with a deployment change CHG-1923 from 09:30 UTC. The blast radius includes order-service and notification-service as downstream consumers.',
    'deepseek-r1:1.5b',
    'ollama',
    true,
    156, 89, 'default-internal-access',
    'Service Catalog,Incident History,Change Intelligence',
    'service:payment-service,incident:INC-2847,change:CHG-1923',
    'resp-002',
    NOW() - interval '1 hour 54 minutes',
    NOW() - interval '1 hour 54 minutes', 'system', NOW(), 'system', false
) ON CONFLICT DO NOTHING;

INSERT INTO ai_gov_messages (
    "Id", "ConversationId", "Role", "Content",
    "ModelName", "Provider", "IsInternalModel",
    "PromptTokens", "CompletionTokens", "AppliedPolicyName",
    "GroundingSources", "ContextReferences", "CorrelationId",
    "Timestamp", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES (
    'e1000000-0000-0000-0000-000000000004',
    'f0000000-0000-0000-0000-000000000001',
    'user',
    'Is there a runbook for this type of issue?',
    NULL, NULL, false,
    0, 0, NULL,
    '', '',
    'user-msg-002',
    NOW() - interval '1 hour 50 minutes',
    NOW() - interval '1 hour 50 minutes', 'system', NOW(), 'system', false
) ON CONFLICT DO NOTHING;

-- ── Conversation 2: Contract Compatibility Check ───────────────────────────

INSERT INTO ai_gov_conversations (
    "Id", "Title", "Persona", "ClientType", "DefaultContextScope",
    "LastModelUsed", "CreatedBy", "MessageCount", "Tags", "IsActive",
    "LastMessageAt", "ServiceId", "ContractId", "IncidentId", "TeamId",
    "CreatedAt", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES (
    'f0000000-0000-0000-0000-000000000002',
    'Contract compatibility check — order-service v3',
    'Architect',
    'Web',
    'contracts,services',
    'deepseek-r1:1.5b',
    'system',
    2,
    'contracts,compatibility',
    true,
    NOW() - interval '1 day',
    NULL, NULL, NULL, NULL,
    NOW() - interval '1 day', NOW(), 'system', false
) ON CONFLICT DO NOTHING;

INSERT INTO ai_gov_messages (
    "Id", "ConversationId", "Role", "Content",
    "ModelName", "Provider", "IsInternalModel",
    "PromptTokens", "CompletionTokens", "AppliedPolicyName",
    "GroundingSources", "ContextReferences", "CorrelationId",
    "Timestamp", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES (
    'e2000000-0000-0000-0000-000000000001',
    'f0000000-0000-0000-0000-000000000002',
    'assistant',
    'Welcome! I''m ready to help you analyze contract compatibility. What would you like to check?',
    'deepseek-r1:1.5b',
    'ollama',
    true,
    0, 22, 'default-internal-access',
    'Contract Registry',
    '',
    'init-conv2',
    NOW() - interval '1 day',
    NOW() - interval '1 day', 'system', NOW(), 'system', false
) ON CONFLICT DO NOTHING;

INSERT INTO ai_gov_messages (
    "Id", "ConversationId", "Role", "Content",
    "ModelName", "Provider", "IsInternalModel",
    "PromptTokens", "CompletionTokens", "AppliedPolicyName",
    "GroundingSources", "ContextReferences", "CorrelationId",
    "Timestamp", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES (
    'e2000000-0000-0000-0000-000000000002',
    'f0000000-0000-0000-0000-000000000002',
    'user',
    'Check the compatibility of order-service v3 contract with existing consumers',
    NULL, NULL, false,
    0, 0, NULL,
    '', '',
    'user-msg-003',
    NOW() - interval '23 hours',
    NOW() - interval '23 hours', 'system', NOW(), 'system', false
) ON CONFLICT DO NOTHING;

-- ── Conversation 3: Incident Correlation (Archived) ────────────────────────

INSERT INTO ai_gov_conversations (
    "Id", "Title", "Persona", "ClientType", "DefaultContextScope",
    "LastModelUsed", "CreatedBy", "MessageCount", "Tags", "IsActive",
    "LastMessageAt", "ServiceId", "ContractId", "IncidentId", "TeamId",
    "CreatedAt", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES (
    'f0000000-0000-0000-0000-000000000003',
    'Incident correlation — notification failures',
    'TechLead',
    'Web',
    'incidents,changes',
    'deepseek-r1:1.5b',
    'system',
    3,
    'incident,correlation,resolved',
    false,
    NOW() - interval '3 days',
    NULL, NULL, NULL, NULL,
    NOW() - interval '3 days', NOW(), 'system', false
) ON CONFLICT DO NOTHING;

INSERT INTO ai_gov_messages (
    "Id", "ConversationId", "Role", "Content",
    "ModelName", "Provider", "IsInternalModel",
    "PromptTokens", "CompletionTokens", "AppliedPolicyName",
    "GroundingSources", "ContextReferences", "CorrelationId",
    "Timestamp", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES (
    'e3000000-0000-0000-0000-000000000001',
    'f0000000-0000-0000-0000-000000000003',
    'assistant',
    'Welcome! I''m ready to help you correlate incidents. What would you like to investigate?',
    'deepseek-r1:1.5b',
    'ollama',
    true,
    0, 20, 'default-internal-access',
    'Incident History,Change Intelligence',
    '',
    'init-conv3',
    NOW() - interval '3 days',
    NOW() - interval '3 days', 'system', NOW(), 'system', false
) ON CONFLICT DO NOTHING;

INSERT INTO ai_gov_messages (
    "Id", "ConversationId", "Role", "Content",
    "ModelName", "Provider", "IsInternalModel",
    "PromptTokens", "CompletionTokens", "AppliedPolicyName",
    "GroundingSources", "ContextReferences", "CorrelationId",
    "Timestamp", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES (
    'e3000000-0000-0000-0000-000000000002',
    'f0000000-0000-0000-0000-000000000003',
    'user',
    'We are seeing notification failures across multiple services. Can you help correlate these incidents?',
    NULL, NULL, false,
    0, 0, NULL,
    '', '',
    'user-msg-004',
    NOW() - interval '2 days 23 hours',
    NOW() - interval '2 days 23 hours', 'system', NOW(), 'system', false
) ON CONFLICT DO NOTHING;

INSERT INTO ai_gov_messages (
    "Id", "ConversationId", "Role", "Content",
    "ModelName", "Provider", "IsInternalModel",
    "PromptTokens", "CompletionTokens", "AppliedPolicyName",
    "GroundingSources", "ContextReferences", "CorrelationId",
    "Timestamp", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES (
    'e3000000-0000-0000-0000-000000000003',
    'f0000000-0000-0000-0000-000000000003',
    'assistant',
    'I''ve analyzed the incident timeline and identified a correlation. The notification failures started at 14:32 UTC, which coincides with deployment CHG-1845 to the messaging-gateway service. This change introduced a configuration update that affected the message queue connection pool. I recommend checking the messaging-gateway logs and considering a rollback of CHG-1845.',
    'deepseek-r1:1.5b',
    'ollama',
    true,
    178, 95, 'default-internal-access',
    'Incident History,Change Intelligence,Service Catalog',
    'incident:INC-2831,change:CHG-1845,service:messaging-gateway',
    'resp-004',
    NOW() - interval '2 days 22 hours',
    NOW() - interval '2 days 22 hours', 'system', NOW(), 'system', false
) ON CONFLICT DO NOTHING;

-- ── AI AGENTS ───────────────────────────────────────────────────────────────
-- Phase 3: Agent Runtime Foundation — includes ownership, visibility, publication status

-- Agent: Service Analyst (official)
INSERT INTO ai_gov_agents (
    "Id", "Name", "DisplayName", "Slug", "Description", "Category",
    "IsOfficial", "IsActive", "SystemPrompt", "PreferredModelId",
    "Capabilities", "TargetPersona", "Icon", "SortOrder",
    "OwnershipType", "Visibility", "PublicationStatus", "OwnerId", "OwnerTeamId",
    "AllowedModelIds", "AllowedTools", "Objective", "InputSchema", "OutputSchema",
    "AllowModelOverride", "Version", "ExecutionCount",
    "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES (
    'a1000000-0000-0000-0000-000000000001',
    'service-analyst',
    'Service Analyst',
    'service-analyst',
    'Analyzes services, dependencies, health status, and topology. Helps engineers understand service relationships and operational state.',
    'ServiceAnalysis',
    true, true,
    'You are a Service Analyst agent for NexTraceOne. You analyze services, their dependencies, health status, topology, and operational state. Provide clear, actionable insights about service relationships and reliability.',
    null,
    'chat,analysis',
    'Engineer',
    '🔍', 10,
    'System', 'Tenant', 'Published', 'system', '',
    '', '', 'Analyze services, dependencies and operational state for the NexTraceOne platform.', '', '',
    true, 1, 0,
    NOW(), 'system', NOW(), 'system', false
) ON CONFLICT DO NOTHING;

-- Agent: Contract Governance (official)
INSERT INTO ai_gov_agents (
    "Id", "Name", "DisplayName", "Slug", "Description", "Category",
    "IsOfficial", "IsActive", "SystemPrompt", "PreferredModelId",
    "Capabilities", "TargetPersona", "Icon", "SortOrder",
    "OwnershipType", "Visibility", "PublicationStatus", "OwnerId", "OwnerTeamId",
    "AllowedModelIds", "AllowedTools", "Objective", "InputSchema", "OutputSchema",
    "AllowModelOverride", "Version", "ExecutionCount",
    "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES (
    'a1000000-0000-0000-0000-000000000002',
    'contract-governance',
    'Contract Governance',
    'contract-governance',
    'Assists with API contract creation, validation, compatibility checks, and versioning. Ensures contracts follow governance policies.',
    'ContractGovernance',
    true, true,
    'You are a Contract Governance agent for NexTraceOne. You assist with API contract creation, validation, compatibility analysis, and versioning. Enforce governance policies and best practices for REST, SOAP, Kafka, and background service contracts.',
    null,
    'chat,analysis,generation',
    'Architect',
    '📋', 20,
    'System', 'Tenant', 'Published', 'system', '',
    '', '', 'Govern API contracts: validate, version, check compatibility and enforce policies.', '', '',
    true, 1, 0,
    NOW(), 'system', NOW(), 'system', false
) ON CONFLICT DO NOTHING;

-- Agent: Incident Responder (official)
INSERT INTO ai_gov_agents (
    "Id", "Name", "DisplayName", "Slug", "Description", "Category",
    "IsOfficial", "IsActive", "SystemPrompt", "PreferredModelId",
    "Capabilities", "TargetPersona", "Icon", "SortOrder",
    "OwnershipType", "Visibility", "PublicationStatus", "OwnerId", "OwnerTeamId",
    "AllowedModelIds", "AllowedTools", "Objective", "InputSchema", "OutputSchema",
    "AllowModelOverride", "Version", "ExecutionCount",
    "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES (
    'a1000000-0000-0000-0000-000000000003',
    'incident-responder',
    'Incident Responder',
    'incident-responder',
    'Investigates production incidents, correlates with recent changes, identifies probable causes, and recommends mitigation steps.',
    'IncidentResponse',
    true, true,
    'You are an Incident Responder agent for NexTraceOne. You investigate production incidents, correlate them with recent changes, identify probable root causes, and recommend mitigation steps. Be precise and action-oriented.',
    null,
    'chat,analysis',
    'Engineer',
    '🚨', 30,
    'System', 'Tenant', 'Published', 'system', '',
    '', '', 'Investigate incidents, correlate with changes, identify root cause and recommend mitigation.', '', '',
    true, 1, 0,
    NOW(), 'system', NOW(), 'system', false
) ON CONFLICT DO NOTHING;

-- Agent: Change Intelligence (official)
INSERT INTO ai_gov_agents (
    "Id", "Name", "DisplayName", "Slug", "Description", "Category",
    "IsOfficial", "IsActive", "SystemPrompt", "PreferredModelId",
    "Capabilities", "TargetPersona", "Icon", "SortOrder",
    "OwnershipType", "Visibility", "PublicationStatus", "OwnerId", "OwnerTeamId",
    "AllowedModelIds", "AllowedTools", "Objective", "InputSchema", "OutputSchema",
    "AllowModelOverride", "Version", "ExecutionCount",
    "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES (
    'a1000000-0000-0000-0000-000000000004',
    'change-intelligence',
    'Change Intelligence',
    'change-intelligence',
    'Analyzes production changes, calculates blast radius, validates change confidence, and correlates changes with incidents.',
    'ChangeIntelligence',
    true, true,
    'You are a Change Intelligence agent for NexTraceOne. You analyze production changes, calculate blast radius, validate deployment confidence, and correlate changes with incidents. Provide data-driven change risk assessments.',
    null,
    'chat,analysis',
    'TechLead',
    '🔄', 40,
    'System', 'Tenant', 'Published', 'system', '',
    '', '', 'Analyze production changes, assess blast radius and deployment confidence.', '', '',
    true, 1, 0,
    NOW(), 'system', NOW(), 'system', false
) ON CONFLICT DO NOTHING;

-- Agent: Security Auditor (official)
INSERT INTO ai_gov_agents (
    "Id", "Name", "DisplayName", "Slug", "Description", "Category",
    "IsOfficial", "IsActive", "SystemPrompt", "PreferredModelId",
    "Capabilities", "TargetPersona", "Icon", "SortOrder",
    "OwnershipType", "Visibility", "PublicationStatus", "OwnerId", "OwnerTeamId",
    "AllowedModelIds", "AllowedTools", "Objective", "InputSchema", "OutputSchema",
    "AllowModelOverride", "Version", "ExecutionCount",
    "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES (
    'a1000000-0000-0000-0000-000000000005',
    'security-auditor',
    'Security Auditor',
    'security-auditor',
    'Reviews security posture, compliance status, access policies, and audit trails. Identifies security gaps and recommends fixes.',
    'SecurityAudit',
    true, true,
    'You are a Security Auditor agent for NexTraceOne. You review security posture, compliance status, access policies, and audit trails. Identify security gaps and recommend actionable fixes following industry standards.',
    null,
    'chat,analysis',
    'Auditor',
    '🛡️', 50,
    'System', 'Tenant', 'Published', 'system', '',
    '', '', 'Review security posture, audit trails and compliance. Recommend fixes.', '', '',
    true, 1, 0,
    NOW(), 'system', NOW(), 'system', false
) ON CONFLICT DO NOTHING;

-- Agent: Documentation Assistant (official)
INSERT INTO ai_gov_agents (
    "Id", "Name", "DisplayName", "Slug", "Description", "Category",
    "IsOfficial", "IsActive", "SystemPrompt", "PreferredModelId",
    "Capabilities", "TargetPersona", "Icon", "SortOrder",
    "OwnershipType", "Visibility", "PublicationStatus", "OwnerId", "OwnerTeamId",
    "AllowedModelIds", "AllowedTools", "Objective", "InputSchema", "OutputSchema",
    "AllowModelOverride", "Version", "ExecutionCount",
    "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES (
    'a1000000-0000-0000-0000-000000000006',
    'documentation-assistant',
    'Documentation Assistant',
    'documentation-assistant',
    'Helps create and maintain operational documentation, runbooks, knowledge articles, and changelog entries.',
    'Documentation',
    true, true,
    'You are a Documentation Assistant agent for NexTraceOne. You help create and maintain operational documentation, runbooks, knowledge articles, and changelog entries. Generate clear, structured, and actionable documentation.',
    null,
    'chat,generation',
    'Engineer',
    '📝', 60,
    'System', 'Tenant', 'Published', 'system', '',
    '', '', 'Create and maintain operational documentation, runbooks and knowledge articles.', '', '',
    true, 1, 0,
    NOW(), 'system', NOW(), 'system', false
) ON CONFLICT DO NOTHING;

-- ── Phase 3: Specialized Official Agents ────────────────────────────────────

-- Agent: API Contract Author (official — generates OpenAPI drafts)
INSERT INTO ai_gov_agents (
    "Id", "Name", "DisplayName", "Slug", "Description", "Category",
    "IsOfficial", "IsActive", "SystemPrompt", "PreferredModelId",
    "Capabilities", "TargetPersona", "Icon", "SortOrder",
    "OwnershipType", "Visibility", "PublicationStatus", "OwnerId", "OwnerTeamId",
    "AllowedModelIds", "AllowedTools", "Objective", "InputSchema", "OutputSchema",
    "AllowModelOverride", "Version", "ExecutionCount",
    "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES (
    'a1000000-0000-0000-0000-000000000007',
    'api-contract-author',
    'API Contract Author',
    'api-contract-author',
    'Generates OpenAPI 3.1 contract drafts from natural language descriptions of API endpoints, resources and operations.',
    'ApiDesign',
    true, true,
    'You are the API Contract Author agent for NexTraceOne.
Your mission is to generate valid OpenAPI 3.1 YAML specifications from natural language descriptions.

Rules:
1. Always output valid OpenAPI 3.1 YAML.
2. Include info, paths, components/schemas, and example request/response bodies.
3. Use semantic HTTP methods (GET for reads, POST for creates, PUT for full updates, PATCH for partial, DELETE for removals).
4. Include proper error responses (400, 401, 403, 404, 500).
5. Use $ref for reusable schemas.
6. Include operationId for each operation.
7. Add descriptions to all endpoints, parameters and schemas.
8. Follow RESTful naming conventions (plural nouns, kebab-case paths).
9. Include pagination for list endpoints (page, pageSize query params).
10. Do NOT invent business logic — stick to the user''s description.

Output ONLY the OpenAPI YAML. No explanations before or after.',
    null,
    'generation',
    'Architect',
    '📐', 70,
    'System', 'Tenant', 'Published', 'system', '',
    '', '', 'Generate OpenAPI 3.1 YAML contract drafts from natural language descriptions.',
    '{"type":"object","properties":{"description":{"type":"string","description":"Natural language description of the API"},"resourceName":{"type":"string"},"operations":{"type":"array","items":{"type":"string"}}}}',
    '{"type":"string","format":"yaml","description":"OpenAPI 3.1 YAML specification"}',
    true, 1, 0,
    NOW(), 'system', NOW(), 'system', false
) ON CONFLICT DO NOTHING;

-- Agent: API Test Scenario Generator (official — generates test scenarios from OpenAPI)
INSERT INTO ai_gov_agents (
    "Id", "Name", "DisplayName", "Slug", "Description", "Category",
    "IsOfficial", "IsActive", "SystemPrompt", "PreferredModelId",
    "Capabilities", "TargetPersona", "Icon", "SortOrder",
    "OwnershipType", "Visibility", "PublicationStatus", "OwnerId", "OwnerTeamId",
    "AllowedModelIds", "AllowedTools", "Objective", "InputSchema", "OutputSchema",
    "AllowModelOverride", "Version", "ExecutionCount",
    "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES (
    'a1000000-0000-0000-0000-000000000008',
    'api-test-scenario',
    'API Test Scenario Generator',
    'api-test-scenario',
    'Generates comprehensive test scenarios from OpenAPI specifications, including happy path, edge cases, and error scenarios.',
    'TestGeneration',
    true, true,
    'You are the API Test Scenario Generator agent for NexTraceOne.
Your mission is to generate comprehensive test scenarios from OpenAPI specifications.

Rules:
1. Output a JSON array of test scenarios.
2. Each scenario: { "name", "description", "method", "path", "headers", "queryParams", "requestBody", "expectedStatus", "expectedResponseShape", "tags" }
3. Include: happy path, validation errors (400), auth failures (401/403), not found (404), and edge cases.
4. For list endpoints: test pagination, empty results, filtering.
5. For create endpoints: test required fields missing, invalid types, duplicate keys.
6. For update endpoints: test partial updates, full updates, optimistic concurrency.
7. For delete endpoints: test not found, already deleted, cascading effects.
8. Include boundary values for numeric and string fields.
9. Include proper Content-Type headers.
10. Do NOT include actual test code — only scenario definitions.

Output ONLY the JSON array. No explanations.',
    null,
    'generation',
    'Engineer',
    '🧪', 80,
    'System', 'Tenant', 'Published', 'system', '',
    '', '', 'Generate test scenarios (happy path, edge cases, errors) from OpenAPI specs.',
    '{"type":"object","properties":{"openApiSpec":{"type":"string","description":"OpenAPI YAML or JSON specification"},"focusEndpoints":{"type":"array","items":{"type":"string"},"description":"Optional: specific endpoints to focus on"}}}',
    '{"type":"array","items":{"type":"object","properties":{"name":{"type":"string"},"method":{"type":"string"},"path":{"type":"string"},"expectedStatus":{"type":"integer"}}}}',
    true, 1, 0,
    NOW(), 'system', NOW(), 'system', false
) ON CONFLICT DO NOTHING;

-- Agent: Kafka Schema Contract Designer (official — generates Avro/JSON schemas)
INSERT INTO ai_gov_agents (
    "Id", "Name", "DisplayName", "Slug", "Description", "Category",
    "IsOfficial", "IsActive", "SystemPrompt", "PreferredModelId",
    "Capabilities", "TargetPersona", "Icon", "SortOrder",
    "OwnershipType", "Visibility", "PublicationStatus", "OwnerId", "OwnerTeamId",
    "AllowedModelIds", "AllowedTools", "Objective", "InputSchema", "OutputSchema",
    "AllowModelOverride", "Version", "ExecutionCount",
    "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted"
)
VALUES (
    'a1000000-0000-0000-0000-000000000009',
    'kafka-schema-contract',
    'Kafka Schema Contract Designer',
    'kafka-schema-contract',
    'Designs Kafka topic schemas (Avro or JSON Schema) from event descriptions, including producer/consumer contract definitions.',
    'EventDesign',
    true, true,
    'You are the Kafka Schema Contract Designer agent for NexTraceOne.
Your mission is to design Kafka event schemas from natural language descriptions.

Rules:
1. Output valid Avro schema JSON by default (or JSON Schema if requested).
2. Include: namespace, name, doc, fields with types and docs.
3. Use logical types for dates (timestamp-millis), UUIDs (uuid), and decimals.
4. Include a metadata envelope: { "eventType", "eventVersion", "correlationId", "timestamp", "source", "payload" }.
5. Design for backward compatibility: new fields should have defaults, use UNION with null.
6. Include producer and consumer topic naming convention: {domain}.{entity}.{action}.v{version}
7. Document breaking vs non-breaking changes.
8. Use semantic field names (camelCase).
9. Include example payload.
10. Do NOT include Kafka configuration — only schema definition.

Output ONLY the Avro schema JSON (or JSON Schema if requested). No explanations.',
    null,
    'generation',
    'Architect',
    '📨', 90,
    'System', 'Tenant', 'Published', 'system', '',
    '', '', 'Design Kafka event schemas (Avro/JSON Schema) with producer/consumer contracts.',
    '{"type":"object","properties":{"eventDescription":{"type":"string","description":"Natural language description of the event"},"domain":{"type":"string"},"entity":{"type":"string"},"schemaFormat":{"type":"string","enum":["avro","jsonschema"],"default":"avro"}}}',
    '{"type":"string","format":"json","description":"Avro schema or JSON Schema definition"}',
    true, 1, 0,
    NOW(), 'system', NOW(), 'system', false
) ON CONFLICT DO NOTHING;
