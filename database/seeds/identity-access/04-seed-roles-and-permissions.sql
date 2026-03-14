-- ============================================================================
-- NexTraceOne — Identity & Access — Roles e Permissões
-- Cria os roles padrão do sistema e as permissões granulares por domínio.
-- A tabela de junção RolePermissions liga cada role às suas permissões.
-- IDs estáveis (r1000000-... para roles, p1000000-... para permissões)
-- garantem reprodutibilidade e idempotência via ON CONFLICT DO NOTHING.
-- ============================================================================

-- ────────────────────────────────────────────────────────────────────────────
-- Roles do sistema — cada role representa um perfil funcional enterprise
-- ────────────────────────────────────────────────────────────────────────────

INSERT INTO "Roles" ("Id", "Name", "Description", "IsSystem", "CreatedAt")
VALUES
    -- Administrador da plataforma — acesso irrestrito a todas as funcionalidades
    ('r1000000-0000-0000-0000-000000000001', 'PlatformAdmin',  'Full system access — manages tenants, users, environments and all configurations', true,  '2025-01-01T00:00:00Z'),
    -- Tech Lead — aprova workflows, gere equipa e ambientes superiores
    ('r1000000-0000-0000-0000-000000000002', 'TechLead',       'Approve workflows, manage team and upper environments',                          false, '2025-01-01T00:00:00Z'),
    -- Desenvolvedor — acesso padrão de desenvolvimento e submissão de mudanças
    ('r1000000-0000-0000-0000-000000000003', 'Developer',      'Standard development access — submit changes, read catalog and contracts',       false, '2025-01-01T00:00:00Z'),
    -- Viewer — acesso somente leitura ao catálogo, contratos e releases
    ('r1000000-0000-0000-0000-000000000004', 'Viewer',         'Read-only access to catalog, contracts and releases',                            false, '2025-01-01T00:00:00Z'),
    -- Auditor — acesso a trilha de auditoria e exportação de evidências
    ('r1000000-0000-0000-0000-000000000005', 'Auditor',        'Audit and compliance access — read audit trail and export evidence',              false, '2025-01-01T00:00:00Z'),
    -- Security Reviewer — revisões de segurança e acesso
    ('r1000000-0000-0000-0000-000000000006', 'SecurityReview', 'Security review access — access reviews, session management, risk analysis',     false, '2025-01-01T00:00:00Z'),
    -- Aprovador — permissão restrita a aprovação/rejeição de workflows
    ('r1000000-0000-0000-0000-000000000007', 'ApprovalOnly',   'Approve or reject workflow requests — no other write access',                    false, '2025-01-01T00:00:00Z')
ON CONFLICT ("Id") DO NOTHING;

-- ────────────────────────────────────────────────────────────────────────────
-- Permissões granulares — organizadas por categoria (bounded context)
-- O código segue o padrão {domínio}:{recurso}:{ação} para alinhamento com i18n
-- ────────────────────────────────────────────────────────────────────────────

INSERT INTO "Permissions" ("Id", "Code", "Description", "Category", "CreatedAt")
VALUES
    -- Identidade — gestão de utilizadores
    ('p1000000-0000-0000-0000-000000000001', 'identity:users:read',      'View user profiles and directory',         'identity',  '2025-01-01T00:00:00Z'),
    ('p1000000-0000-0000-0000-000000000002', 'identity:users:write',     'Create and update user accounts',          'identity',  '2025-01-01T00:00:00Z'),
    -- Identidade — gestão de roles
    ('p1000000-0000-0000-0000-000000000003', 'identity:roles:read',      'View roles and their permission sets',     'identity',  '2025-01-01T00:00:00Z'),
    ('p1000000-0000-0000-0000-000000000004', 'identity:roles:assign',    'Assign or revoke roles from users',        'identity',  '2025-01-01T00:00:00Z'),
    -- Identidade — gestão de sessões
    ('p1000000-0000-0000-0000-000000000005', 'identity:sessions:read',   'View active and historical sessions',      'identity',  '2025-01-01T00:00:00Z'),
    ('p1000000-0000-0000-0000-000000000006', 'identity:sessions:revoke', 'Force-revoke active user sessions',        'identity',  '2025-01-01T00:00:00Z'),
    -- Contratos — import, diff, validação
    ('p1000000-0000-0000-0000-000000000007', 'contracts:read',           'View contract versions and diffs',         'contracts', '2025-01-01T00:00:00Z'),
    ('p1000000-0000-0000-0000-000000000008', 'contracts:write',          'Import, lock and manage contract versions','contracts', '2025-01-01T00:00:00Z'),
    -- Catálogo — assets e portal
    ('p1000000-0000-0000-0000-000000000009', 'catalog:read',             'Browse API catalog and service assets',    'catalog',   '2025-01-01T00:00:00Z'),
    ('p1000000-0000-0000-0000-000000000010', 'catalog:write',            'Register and update catalog entries',      'catalog',   '2025-01-01T00:00:00Z'),
    -- Workflow — submissão e aprovação de mudanças
    ('p1000000-0000-0000-0000-000000000011', 'workflow:read',            'View workflow requests and status',        'workflow',  '2025-01-01T00:00:00Z'),
    ('p1000000-0000-0000-0000-000000000012', 'workflow:write',           'Submit and update workflow requests',      'workflow',  '2025-01-01T00:00:00Z'),
    ('p1000000-0000-0000-0000-000000000013', 'workflow:approve',         'Approve or reject workflow requests',      'workflow',  '2025-01-01T00:00:00Z'),
    -- Auditoria — trilha e exportação de evidências
    ('p1000000-0000-0000-0000-000000000014', 'audit:read',               'Search and view audit trail entries',      'audit',     '2025-01-01T00:00:00Z'),
    ('p1000000-0000-0000-0000-000000000015', 'audit:export',             'Export audit evidence as JSON or CSV',     'audit',     '2025-01-01T00:00:00Z'),
    -- Releases — gestão de releases e promoção
    ('p1000000-0000-0000-0000-000000000016', 'releases:read',            'View releases and change intelligence',    'releases',  '2025-01-01T00:00:00Z'),
    ('p1000000-0000-0000-0000-000000000017', 'releases:write',           'Create and manage releases',              'releases',  '2025-01-01T00:00:00Z')
ON CONFLICT ("Id") DO NOTHING;

-- ────────────────────────────────────────────────────────────────────────────
-- Associação Role ↔ Permissão — matriz de acesso do sistema
-- Cada role recebe apenas as permissões necessárias (least privilege)
-- ────────────────────────────────────────────────────────────────────────────

-- PlatformAdmin — acesso total a todas as permissões
INSERT INTO "RolePermissions" ("RoleId", "PermissionId")
VALUES
    ('r1000000-0000-0000-0000-000000000001', 'p1000000-0000-0000-0000-000000000001'),
    ('r1000000-0000-0000-0000-000000000001', 'p1000000-0000-0000-0000-000000000002'),
    ('r1000000-0000-0000-0000-000000000001', 'p1000000-0000-0000-0000-000000000003'),
    ('r1000000-0000-0000-0000-000000000001', 'p1000000-0000-0000-0000-000000000004'),
    ('r1000000-0000-0000-0000-000000000001', 'p1000000-0000-0000-0000-000000000005'),
    ('r1000000-0000-0000-0000-000000000001', 'p1000000-0000-0000-0000-000000000006'),
    ('r1000000-0000-0000-0000-000000000001', 'p1000000-0000-0000-0000-000000000007'),
    ('r1000000-0000-0000-0000-000000000001', 'p1000000-0000-0000-0000-000000000008'),
    ('r1000000-0000-0000-0000-000000000001', 'p1000000-0000-0000-0000-000000000009'),
    ('r1000000-0000-0000-0000-000000000001', 'p1000000-0000-0000-0000-000000000010'),
    ('r1000000-0000-0000-0000-000000000001', 'p1000000-0000-0000-0000-000000000011'),
    ('r1000000-0000-0000-0000-000000000001', 'p1000000-0000-0000-0000-000000000012'),
    ('r1000000-0000-0000-0000-000000000001', 'p1000000-0000-0000-0000-000000000013'),
    ('r1000000-0000-0000-0000-000000000001', 'p1000000-0000-0000-0000-000000000014'),
    ('r1000000-0000-0000-0000-000000000001', 'p1000000-0000-0000-0000-000000000015'),
    ('r1000000-0000-0000-0000-000000000001', 'p1000000-0000-0000-0000-000000000016'),
    ('r1000000-0000-0000-0000-000000000001', 'p1000000-0000-0000-0000-000000000017')
ON CONFLICT DO NOTHING;

-- TechLead — leitura ampla, escrita em contratos/catálogo/releases, aprovação de workflows
INSERT INTO "RolePermissions" ("RoleId", "PermissionId")
VALUES
    ('r1000000-0000-0000-0000-000000000002', 'p1000000-0000-0000-0000-000000000001'),
    ('r1000000-0000-0000-0000-000000000002', 'p1000000-0000-0000-0000-000000000003'),
    ('r1000000-0000-0000-0000-000000000002', 'p1000000-0000-0000-0000-000000000004'),
    ('r1000000-0000-0000-0000-000000000002', 'p1000000-0000-0000-0000-000000000005'),
    ('r1000000-0000-0000-0000-000000000002', 'p1000000-0000-0000-0000-000000000007'),
    ('r1000000-0000-0000-0000-000000000002', 'p1000000-0000-0000-0000-000000000008'),
    ('r1000000-0000-0000-0000-000000000002', 'p1000000-0000-0000-0000-000000000009'),
    ('r1000000-0000-0000-0000-000000000002', 'p1000000-0000-0000-0000-000000000010'),
    ('r1000000-0000-0000-0000-000000000002', 'p1000000-0000-0000-0000-000000000011'),
    ('r1000000-0000-0000-0000-000000000002', 'p1000000-0000-0000-0000-000000000012'),
    ('r1000000-0000-0000-0000-000000000002', 'p1000000-0000-0000-0000-000000000013'),
    ('r1000000-0000-0000-0000-000000000002', 'p1000000-0000-0000-0000-000000000014'),
    ('r1000000-0000-0000-0000-000000000002', 'p1000000-0000-0000-0000-000000000016'),
    ('r1000000-0000-0000-0000-000000000002', 'p1000000-0000-0000-0000-000000000017')
ON CONFLICT DO NOTHING;

-- Developer — leitura de identidade/contratos/catálogo/workflow/releases, escrita em contratos e workflows
INSERT INTO "RolePermissions" ("RoleId", "PermissionId")
VALUES
    ('r1000000-0000-0000-0000-000000000003', 'p1000000-0000-0000-0000-000000000001'),
    ('r1000000-0000-0000-0000-000000000003', 'p1000000-0000-0000-0000-000000000003'),
    ('r1000000-0000-0000-0000-000000000003', 'p1000000-0000-0000-0000-000000000007'),
    ('r1000000-0000-0000-0000-000000000003', 'p1000000-0000-0000-0000-000000000008'),
    ('r1000000-0000-0000-0000-000000000003', 'p1000000-0000-0000-0000-000000000009'),
    ('r1000000-0000-0000-0000-000000000003', 'p1000000-0000-0000-0000-000000000011'),
    ('r1000000-0000-0000-0000-000000000003', 'p1000000-0000-0000-0000-000000000012'),
    ('r1000000-0000-0000-0000-000000000003', 'p1000000-0000-0000-0000-000000000016')
ON CONFLICT DO NOTHING;

-- Viewer — somente leitura em todos os domínios (sem escrita, sem aprovação)
INSERT INTO "RolePermissions" ("RoleId", "PermissionId")
VALUES
    ('r1000000-0000-0000-0000-000000000004', 'p1000000-0000-0000-0000-000000000001'),
    ('r1000000-0000-0000-0000-000000000004', 'p1000000-0000-0000-0000-000000000003'),
    ('r1000000-0000-0000-0000-000000000004', 'p1000000-0000-0000-0000-000000000007'),
    ('r1000000-0000-0000-0000-000000000004', 'p1000000-0000-0000-0000-000000000009'),
    ('r1000000-0000-0000-0000-000000000004', 'p1000000-0000-0000-0000-000000000011'),
    ('r1000000-0000-0000-0000-000000000004', 'p1000000-0000-0000-0000-000000000016')
ON CONFLICT DO NOTHING;

-- Auditor — leitura geral + acesso completo a auditoria (incluindo exportação)
INSERT INTO "RolePermissions" ("RoleId", "PermissionId")
VALUES
    ('r1000000-0000-0000-0000-000000000005', 'p1000000-0000-0000-0000-000000000001'),
    ('r1000000-0000-0000-0000-000000000005', 'p1000000-0000-0000-0000-000000000003'),
    ('r1000000-0000-0000-0000-000000000005', 'p1000000-0000-0000-0000-000000000005'),
    ('r1000000-0000-0000-0000-000000000005', 'p1000000-0000-0000-0000-000000000007'),
    ('r1000000-0000-0000-0000-000000000005', 'p1000000-0000-0000-0000-000000000009'),
    ('r1000000-0000-0000-0000-000000000005', 'p1000000-0000-0000-0000-000000000011'),
    ('r1000000-0000-0000-0000-000000000005', 'p1000000-0000-0000-0000-000000000014'),
    ('r1000000-0000-0000-0000-000000000005', 'p1000000-0000-0000-0000-000000000015'),
    ('r1000000-0000-0000-0000-000000000005', 'p1000000-0000-0000-0000-000000000016')
ON CONFLICT DO NOTHING;

-- SecurityReview — sessões, utilizadores, roles, auditoria + leitura de contratos e catálogo
INSERT INTO "RolePermissions" ("RoleId", "PermissionId")
VALUES
    ('r1000000-0000-0000-0000-000000000006', 'p1000000-0000-0000-0000-000000000001'),
    ('r1000000-0000-0000-0000-000000000006', 'p1000000-0000-0000-0000-000000000003'),
    ('r1000000-0000-0000-0000-000000000006', 'p1000000-0000-0000-0000-000000000004'),
    ('r1000000-0000-0000-0000-000000000006', 'p1000000-0000-0000-0000-000000000005'),
    ('r1000000-0000-0000-0000-000000000006', 'p1000000-0000-0000-0000-000000000006'),
    ('r1000000-0000-0000-0000-000000000006', 'p1000000-0000-0000-0000-000000000007'),
    ('r1000000-0000-0000-0000-000000000006', 'p1000000-0000-0000-0000-000000000009'),
    ('r1000000-0000-0000-0000-000000000006', 'p1000000-0000-0000-0000-000000000014')
ON CONFLICT DO NOTHING;

-- ApprovalOnly — apenas leitura de workflows/releases + aprovação
INSERT INTO "RolePermissions" ("RoleId", "PermissionId")
VALUES
    ('r1000000-0000-0000-0000-000000000007', 'p1000000-0000-0000-0000-000000000011'),
    ('r1000000-0000-0000-0000-000000000007', 'p1000000-0000-0000-0000-000000000013'),
    ('r1000000-0000-0000-0000-000000000007', 'p1000000-0000-0000-0000-000000000016')
ON CONFLICT DO NOTHING;
