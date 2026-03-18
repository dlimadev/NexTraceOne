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
