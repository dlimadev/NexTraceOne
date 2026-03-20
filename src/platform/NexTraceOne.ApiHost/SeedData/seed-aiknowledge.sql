-- ============================================================================
-- NexTraceOne AI Knowledge Seed Data
-- Idempotent: uses ON CONFLICT DO NOTHING
-- Includes: Providers, Models, Access Policies, Sources, Token Quota Policies
-- Table/column names must match EF Core entity configurations exactly.
-- ============================================================================

-- ── AI PROVIDERS ────────────────────────────────────────────────────────────

-- Provider: Ollama (local, default)
INSERT INTO "AiProviders" ("Id", "Name", "DisplayName", "ProviderType", "BaseUrl", "IsLocal", "IsExternal", "IsEnabled", "SupportedCapabilities", "Priority", "Description", "RegisteredAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES (
    'c0000000-0000-0000-0000-000000000001',
    'ollama',
    'Ollama (Local)',
    'ollama',
    'http://localhost:11434',
    true,
    false,
    true,
    'chat,reasoning,embeddings,streaming',
    1,
    'Local AI provider using Ollama. Hosts models like DeepSeek, Llama, Mistral.',
    NOW(),
    NOW(), 'system', NOW(), 'system',
    false
) ON CONFLICT DO NOTHING;

-- Provider: OpenAI (external, future)
INSERT INTO "AiProviders" ("Id", "Name", "DisplayName", "ProviderType", "BaseUrl", "IsLocal", "IsExternal", "IsEnabled", "SupportedCapabilities", "Priority", "Description", "RegisteredAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES (
    'c0000000-0000-0000-0000-000000000002',
    'openai',
    'OpenAI',
    'openai',
    'https://api.openai.com/v1',
    false,
    true,
    false,
    'chat,reasoning,embeddings,vision,tool-calling,streaming,structured-output',
    10,
    'OpenAI external provider (GPT-4, GPT-4o). Requires API key and token quota.',
    NOW(),
    NOW(), 'system', NOW(), 'system',
    false
) ON CONFLICT DO NOTHING;

-- Provider: Azure OpenAI (external, future)
INSERT INTO "AiProviders" ("Id", "Name", "DisplayName", "ProviderType", "BaseUrl", "IsLocal", "IsExternal", "IsEnabled", "SupportedCapabilities", "Priority", "Description", "RegisteredAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES (
    'c0000000-0000-0000-0000-000000000003',
    'azure-openai',
    'Azure OpenAI',
    'azure-openai',
    '',
    false,
    true,
    false,
    'chat,reasoning,embeddings,vision,tool-calling,streaming',
    11,
    'Azure OpenAI Service. Requires Azure endpoint and API key.',
    NOW(),
    NOW(), 'system', NOW(), 'system',
    false
) ON CONFLICT DO NOTHING;

-- Provider: Gemini (external, future)
INSERT INTO "AiProviders" ("Id", "Name", "DisplayName", "ProviderType", "BaseUrl", "IsLocal", "IsExternal", "IsEnabled", "SupportedCapabilities", "Priority", "Description", "RegisteredAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES (
    'c0000000-0000-0000-0000-000000000004',
    'gemini',
    'Google Gemini',
    'gemini',
    'https://generativelanguage.googleapis.com/v1',
    false,
    true,
    false,
    'chat,reasoning,embeddings,vision,streaming',
    12,
    'Google Gemini provider. Requires Google API key.',
    NOW(),
    NOW(), 'system', NOW(), 'system',
    false
) ON CONFLICT DO NOTHING;

-- ── AI MODELS ───────────────────────────────────────────────────────────────
-- Note: ModelType and Status are stored as strings (HasConversion<string>())

-- Default AI Model: DeepSeek R1 (1.5B) via Ollama
INSERT INTO ai_gov_models ("Id", "Name", "DisplayName", "Provider", "ModelType", "IsInternal", "IsExternal", "Status", "Capabilities", "DefaultUseCases", "SensitivityLevel", "RegisteredAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES (
    'a0000000-0000-0000-0000-000000000001',
    'deepseek-r1:1.5b',
    'DeepSeek R1 1.5B',
    'ollama',
    'Chat',
    true,
    false,
    'Active',
    'chat,reasoning,completion',
    'general-chat,code-review,analysis',
    1,
    NOW(),
    NOW(), 'system', NOW(), 'system',
    false
) ON CONFLICT DO NOTHING;

-- Embedding model: Nomic Embed Text via Ollama
INSERT INTO ai_gov_models ("Id", "Name", "DisplayName", "Provider", "ModelType", "IsInternal", "IsExternal", "Status", "Capabilities", "DefaultUseCases", "SensitivityLevel", "RegisteredAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES (
    'a0000000-0000-0000-0000-000000000002',
    'nomic-embed-text',
    'Nomic Embed Text',
    'ollama',
    'Embedding',
    true,
    false,
    'Inactive',
    'embeddings',
    'semantic-search,rag',
    1,
    NOW(),
    NOW(), 'system', NOW(), 'system',
    false
) ON CONFLICT DO NOTHING;

-- Future external model placeholder: GPT-4o via OpenAI (disabled)
INSERT INTO ai_gov_models ("Id", "Name", "DisplayName", "Provider", "ModelType", "IsInternal", "IsExternal", "Status", "Capabilities", "DefaultUseCases", "SensitivityLevel", "RegisteredAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES (
    'a0000000-0000-0000-0000-000000000003',
    'gpt-4o',
    'GPT-4o (OpenAI)',
    'openai',
    'Chat',
    false,
    true,
    'Inactive',
    'chat,reasoning,vision,tool-calling,streaming,structured-output',
    'premium-analysis,complex-reasoning,code-generation',
    3,
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
