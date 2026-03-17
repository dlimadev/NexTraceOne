-- NexTraceOne AI Knowledge Seed Data
-- Provider: Ollama (local) with DeepSeek as default model
-- Idempotent: uses ON CONFLICT DO NOTHING

-- Default AI Model: DeepSeek R1 (1.5B) via Ollama
INSERT INTO "Models" ("Id", "Name", "DisplayName", "Provider", "ModelType", "IsInternal", "IsExternal", "Status", "Capabilities", "DefaultUseCases", "SensitivityLevel", "RegisteredAt", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy", "TenantId", "IsDeleted")
VALUES (
    'a0000000-0000-0000-0000-000000000001',
    'deepseek-r1:1.5b',
    'DeepSeek R1 1.5B',
    'ollama',
    0, -- Chat
    true,
    false,
    0, -- Active
    'chat,reasoning,completion',
    'general-chat,code-review,analysis',
    1,
    NOW(),
    NOW(),
    'system',
    NOW(),
    'system',
    '00000000-0000-0000-0000-000000000001',
    false
) ON CONFLICT DO NOTHING;

-- Placeholder for future embedding model
INSERT INTO "Models" ("Id", "Name", "DisplayName", "Provider", "ModelType", "IsInternal", "IsExternal", "Status", "Capabilities", "DefaultUseCases", "SensitivityLevel", "RegisteredAt", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy", "TenantId", "IsDeleted")
VALUES (
    'a0000000-0000-0000-0000-000000000002',
    'nomic-embed-text',
    'Nomic Embed Text',
    'ollama',
    2, -- Embedding
    true,
    false,
    1, -- Inactive (not yet available)
    'embeddings',
    'semantic-search,rag',
    1,
    NOW(),
    NOW(),
    'system',
    NOW(),
    'system',
    '00000000-0000-0000-0000-000000000001',
    false
) ON CONFLICT DO NOTHING;

-- Default access policy: allow all internal models for everyone
INSERT INTO "AccessPolicies" ("Id", "Name", "Description", "Scope", "ScopeValue", "AllowedModelIds", "BlockedModelIds", "AllowExternalAI", "InternalOnly", "MaxTokensPerRequest", "EnvironmentRestrictions", "IsActive", "CreatedAt", "CreatedBy", "ModifiedAt", "ModifiedBy", "TenantId", "IsDeleted")
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
    NOW(),
    'system',
    NOW(),
    'system',
    '00000000-0000-0000-0000-000000000001',
    false
) ON CONFLICT DO NOTHING;
