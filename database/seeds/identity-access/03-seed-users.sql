-- ============================================================================
-- NexTraceOne — Identity & Access — Usuários de teste
-- Cria usuários fictícios cobrindo todos os perfis enterprise.
-- Senhas são hashes PBKDF2 do valor "Test@12345" (apenas para testes locais).
-- ============================================================================

-- Hash fictício para "Test@12345" (PBKDF2-HMAC-SHA256, formato do Pbkdf2PasswordHasher)
-- Em ambiente real o hash seria gerado pelo serviço; aqui usamos placeholder reconhecível.
-- O login local com essa senha só funciona se o hash corresponder ao algoritmo real.

-- Admin do ACME Corp — acesso total
INSERT INTO "Users" ("Id", "Email", "FirstName", "LastName", "PasswordHash", "IsActive", "CreatedAt", "LastLoginAt", "FailedLoginCount", "LockoutEnd")
VALUES (
    'u1000000-0000-0000-0000-000000000001',
    'admin@acme-corp.test',
    'Carlos', 'Admin',
    'PBKDF2$V1$10000$dGVzdHNhbHQ=$aGFzaGVkX3Rlc3RfcGFzc3dvcmQ=',
    true, '2025-01-01T00:00:00Z', '2025-03-10T08:00:00Z', 0, NULL
) ON CONFLICT ("Id") DO NOTHING;

-- Tech Lead do ACME Corp — aprovador de workflows
INSERT INTO "Users" ("Id", "Email", "FirstName", "LastName", "PasswordHash", "IsActive", "CreatedAt", "LastLoginAt", "FailedLoginCount", "LockoutEnd")
VALUES (
    'u1000000-0000-0000-0000-000000000002',
    'techlead@acme-corp.test',
    'Ana', 'TechLead',
    'PBKDF2$V1$10000$dGVzdHNhbHQ=$aGFzaGVkX3Rlc3RfcGFzc3dvcmQ=',
    true, '2025-01-02T00:00:00Z', '2025-03-10T09:00:00Z', 0, NULL
) ON CONFLICT ("Id") DO NOTHING;

-- Developer do ACME Corp — acesso padrão de desenvolvimento
INSERT INTO "Users" ("Id", "Email", "FirstName", "LastName", "PasswordHash", "IsActive", "CreatedAt", "LastLoginAt", "FailedLoginCount", "LockoutEnd")
VALUES (
    'u1000000-0000-0000-0000-000000000003',
    'dev@acme-corp.test',
    'Bruno', 'Developer',
    'PBKDF2$V1$10000$dGVzdHNhbHQ=$aGFzaGVkX3Rlc3RfcGFzc3dvcmQ=',
    true, '2025-01-03T00:00:00Z', '2025-03-09T14:00:00Z', 0, NULL
) ON CONFLICT ("Id") DO NOTHING;

-- Viewer do ACME Corp — somente leitura
INSERT INTO "Users" ("Id", "Email", "FirstName", "LastName", "PasswordHash", "IsActive", "CreatedAt", "LastLoginAt", "FailedLoginCount", "LockoutEnd")
VALUES (
    'u1000000-0000-0000-0000-000000000004',
    'viewer@acme-corp.test',
    'Diana', 'Viewer',
    'PBKDF2$V1$10000$dGVzdHNhbHQ=$aGFzaGVkX3Rlc3RfcGFzc3dvcmQ=',
    true, '2025-01-04T00:00:00Z', '2025-03-08T10:00:00Z', 0, NULL
) ON CONFLICT ("Id") DO NOTHING;

-- Security Reviewer — responsável por revisões de acesso
INSERT INTO "Users" ("Id", "Email", "FirstName", "LastName", "PasswordHash", "IsActive", "CreatedAt", "LastLoginAt", "FailedLoginCount", "LockoutEnd")
VALUES (
    'u1000000-0000-0000-0000-000000000005',
    'security@acme-corp.test',
    'Eduardo', 'Security',
    'PBKDF2$V1$10000$dGVzdHNhbHQ=$aGFzaGVkX3Rlc3RfcGFzc3dvcmQ=',
    true, '2025-01-05T00:00:00Z', '2025-03-07T11:00:00Z', 0, NULL
) ON CONFLICT ("Id") DO NOTHING;

-- Approver — somente aprovação de workflows
INSERT INTO "Users" ("Id", "Email", "FirstName", "LastName", "PasswordHash", "IsActive", "CreatedAt", "LastLoginAt", "FailedLoginCount", "LockoutEnd")
VALUES (
    'u1000000-0000-0000-0000-000000000006',
    'approver@acme-corp.test',
    'Fernanda', 'Approver',
    'PBKDF2$V1$10000$dGVzdHNhbHQ=$aGFzaGVkX3Rlc3RfcGFzc3dvcmQ=',
    true, '2025-01-06T00:00:00Z', NULL, 0, NULL
) ON CONFLICT ("Id") DO NOTHING;

-- Usuário multi-tenant: pertence a ACME Corp e Globex Inc
INSERT INTO "Users" ("Id", "Email", "FirstName", "LastName", "PasswordHash", "IsActive", "CreatedAt", "LastLoginAt", "FailedLoginCount", "LockoutEnd")
VALUES (
    'u1000000-0000-0000-0000-000000000007',
    'multi@globex-inc.test',
    'Gabriel', 'MultiTenant',
    'PBKDF2$V1$10000$dGVzdHNhbHQ=$aGFzaGVkX3Rlc3RfcGFzc3dvcmQ=',
    true, '2025-01-10T00:00:00Z', '2025-03-10T07:00:00Z', 0, NULL
) ON CONFLICT ("Id") DO NOTHING;

-- Usuário com acesso restrito: apenas Development
INSERT INTO "Users" ("Id", "Email", "FirstName", "LastName", "PasswordHash", "IsActive", "CreatedAt", "LastLoginAt", "FailedLoginCount", "LockoutEnd")
VALUES (
    'u1000000-0000-0000-0000-000000000008',
    'devonly@globex-inc.test',
    'Helena', 'DevOnly',
    'PBKDF2$V1$10000$dGVzdHNhbHQ=$aGFzaGVkX3Rlc3RfcGFzc3dvcmQ=',
    true, '2025-01-12T00:00:00Z', NULL, 0, NULL
) ON CONFLICT ("Id") DO NOTHING;

-- Usuário OIDC simulado (sem senha local)
INSERT INTO "Users" ("Id", "Email", "FirstName", "LastName", "PasswordHash", "IsActive", "CreatedAt", "LastLoginAt", "FailedLoginCount", "LockoutEnd")
VALUES (
    'u1000000-0000-0000-0000-000000000009',
    'oidc@acme-corp.test',
    'Igor', 'OidcUser',
    NULL,
    true, '2025-02-01T00:00:00Z', '2025-03-10T06:00:00Z', 0, NULL
) ON CONFLICT ("Id") DO NOTHING;

-- Usuário local fallback (com senha e identidade externa)
INSERT INTO "Users" ("Id", "Email", "FirstName", "LastName", "PasswordHash", "IsActive", "CreatedAt", "LastLoginAt", "FailedLoginCount", "LockoutEnd")
VALUES (
    'u1000000-0000-0000-0000-000000000010',
    'localfallback@acme-corp.test',
    'Julia', 'LocalFallback',
    'PBKDF2$V1$10000$dGVzdHNhbHQ=$aGFzaGVkX3Rlc3RfcGFzc3dvcmQ=',
    true, '2025-02-15T00:00:00Z', NULL, 0, NULL
) ON CONFLICT ("Id") DO NOTHING;
