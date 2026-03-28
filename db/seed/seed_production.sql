-- =============================================================================
-- NexTraceOne — seed_production.sql
-- =============================================================================
-- Dados de referência obrigatórios para qualquer ambiente (produção, staging,
-- ou desenvolvimento limpo). Deve ser executado imediatamente após as
-- migrations EF Core serem aplicadas com sucesso.
--
-- Conteúdo:
--   1. Tenant principal
--   2. Utilizador Platform Admin
--   3. Vínculo admin → tenant → papel PlatformAdmin
--   4. Ambientes: Development, Staging, Production
--
-- Pré-requisitos:
--   - Migrations EF Core aplicadas (iam_*, env_*, gov_*, chg_* já existem)
--   - iam_roles e iam_permissions já populados via HasData EF Core
--
-- Idempotente: seguro de executar mais de uma vez.
-- =============================================================================

BEGIN;

-- ---------------------------------------------------------------------------
-- 1. Tenant principal
--    ID fixo para garantir consistência entre ambientes e scripts dependentes.
-- ---------------------------------------------------------------------------
INSERT INTO iam_tenants (
    "Id",
    "Name",
    "Slug",
    "IsActive",
    "CreatedAt",
    "UpdatedAt"
)
VALUES (
    'a0000000-0000-0000-0000-000000000001',
    'NexTraceOne',
    'nextraceone',
    true,
    NOW(),
    NOW()
)
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 2. Utilizador Platform Admin
--    Senha: Admin@2026!  (PBKDF2/SHA256 — formato v1.{base64_salt}.{base64_hash}, 100 000 iterações)
--    AVISO DE SEGURANÇA: alterar a senha imediatamente após o primeiro login
--    em qualquer ambiente não-desenvolvimento.
-- ---------------------------------------------------------------------------
INSERT INTO iam_users (
    "Id",
    "Email",
    first_name,
    last_name,
    "PasswordHash",
    "IsActive",
    "FailedLoginAttempts",
    "MfaEnabled",
    "FederationProvider",
    "ExternalId"
)
VALUES (
    'b0000000-0000-0000-0000-000000000001',
    'admin@nextraceone.io',
    'Platform',
    'Admin',
    'v1.ppSv9+z4OYt5/ckM0rsEvQ==.VbpxYtiSfi6e4K22pofst54ZLYgPB3GyBPI7Tj1DIUk=',
    true,
    0,
    false,
    NULL,
    NULL
)
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 3. Vínculo do administrador ao tenant com papel PlatformAdmin
--    Role PlatformAdmin: 1e91a557-fade-46df-b248-0f5f5899c001 (seeded by EF)
-- ---------------------------------------------------------------------------
INSERT INTO iam_tenant_memberships (
    "Id",
    "UserId",
    "TenantId",
    "RoleId",
    "JoinedAt",
    "IsActive"
)
VALUES (
    'd0000000-0000-0000-0000-000000000001',
    'b0000000-0000-0000-0000-000000000001',
    'a0000000-0000-0000-0000-000000000001',
    '1e91a557-fade-46df-b248-0f5f5899c001',
    NOW(),
    true
)
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 4. Ambientes do tenant
--
--    Profile  : 1=Development | 2=Validation | 3=Staging | 4=Production
--               5=Sandbox | 6=DisasterRecovery | 7=Training | 8=UAT | 9=PerformanceTesting
--    Criticality: 1=Low | 2=Medium | 3=High | 4=Critical
-- ---------------------------------------------------------------------------
INSERT INTO env_environments (
    "Id",
    "TenantId",
    "Name",
    "Slug",
    "SortOrder",
    "IsActive",
    "Profile",
    "Code",
    "Description",
    "Criticality",
    "Region",
    "IsProductionLike",
    "IsPrimaryProduction",
    "CreatedAt",
    "CreatedBy",
    "IsDeleted"
)
VALUES
    (
        'c0000000-0000-0000-0000-000000000001',
        'a0000000-0000-0000-0000-000000000001',
        'Development',
        'development',
        1,
        true,
        1,      -- EnvironmentProfile.Development
        'DEV',
        'Ambiente de desenvolvimento. Acesso livre para engineers, sem impacto externo.',
        1,      -- EnvironmentCriticality.Low
        NULL,
        false,
        false,
        NOW(),
        'seed',
        false
    ),
    (
        'c0000000-0000-0000-0000-000000000002',
        'a0000000-0000-0000-0000-000000000001',
        'Staging',
        'staging',
        2,
        true,
        3,      -- EnvironmentProfile.Staging
        'STG',
        'Ambiente de homologação. Comportamento próximo de produção com dados sintéticos ou anonimizados.',
        3,      -- EnvironmentCriticality.High
        NULL,
        true,
        false,
        NOW(),
        'seed',
        false
    ),
    (
        'c0000000-0000-0000-0000-000000000003',
        'a0000000-0000-0000-0000-000000000001',
        'Production',
        'production',
        3,
        true,
        4,      -- EnvironmentProfile.Production
        'PROD',
        'Ambiente de produção. Máxima restrição, auditoria completa, acesso controlado.',
        4,      -- EnvironmentCriticality.Critical
        NULL,
        true,
        true,
        NOW(),
        'seed',
        false
    )
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 5. Acessos de ambiente para o utilizador Platform Admin
--    AccessLevel válidos: 'read' | 'write' | 'admin' | 'none'
--    PlatformAdmin recebe 'admin' em todos os ambientes.
-- ---------------------------------------------------------------------------
INSERT INTO env_environment_accesses (
    "Id",
    "UserId",
    "TenantId",
    "EnvironmentId",
    "AccessLevel",
    "GrantedAt",
    "ExpiresAt",
    "GrantedBy",
    "IsActive",
    "RevokedAt"
)
VALUES
    (
        'e1000000-0000-0000-0000-000000000001',
        'b0000000-0000-0000-0000-000000000001',  -- admin
        'a0000000-0000-0000-0000-000000000001',  -- tenant nextraceone
        'c0000000-0000-0000-0000-000000000001',  -- Development
        'admin',
        NOW(), NULL,
        'b0000000-0000-0000-0000-000000000001',  -- granted by self (seed)
        true, NULL
    ),
    (
        'e1000000-0000-0000-0000-000000000002',
        'b0000000-0000-0000-0000-000000000001',  -- admin
        'a0000000-0000-0000-0000-000000000001',
        'c0000000-0000-0000-0000-000000000002',  -- Staging
        'admin',
        NOW(), NULL,
        'b0000000-0000-0000-0000-000000000001',
        true, NULL
    ),
    (
        'e1000000-0000-0000-0000-000000000003',
        'b0000000-0000-0000-0000-000000000001',  -- admin
        'a0000000-0000-0000-0000-000000000001',
        'c0000000-0000-0000-0000-000000000003',  -- Production
        'admin',
        NOW(), NULL,
        'b0000000-0000-0000-0000-000000000001',
        true, NULL
    )
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 6. Módulos da plataforma (cfg_modules)
--    Necessários para que o módulo de Configuração funcione correctamente.
--    Sem estes registos, a UI de configuração não apresenta os módulos.
-- ---------------------------------------------------------------------------
INSERT INTO cfg_modules (
    "Id",
    "Key",
    "DisplayName",
    "Description",
    "SortOrder",
    "IsActive",
    "CreatedAt",
    "UpdatedAt"
)
VALUES
    ('ca000000-0000-0000-0000-000000000001', 'foundation',     'Foundation & Identity',       'Gestão de tenants, utilizadores, papéis, permissões, sessões e integrações de identidade.',                                 1, true, NOW(), NOW()),
    ('ca000000-0000-0000-0000-000000000002', 'services',       'Service Catalog',             'Catálogo de serviços, ownership, ciclo de vida, topologia e dependências entre serviços.',                                  2, true, NOW(), NOW()),
    ('ca000000-0000-0000-0000-000000000003', 'contracts',      'Contract Governance',         'Contratos REST, SOAP, eventos e AsyncAPI — versionamento, validação, diff semântico e publicação.',                         3, true, NOW(), NOW()),
    ('ca000000-0000-0000-0000-000000000004', 'changes',        'Change Intelligence',         'Rastreamento, validação, aprovação, blast radius, promoção e confiança em mudanças e releases.',                           4, true, NOW(), NOW()),
    ('ca000000-0000-0000-0000-000000000005', 'operations',     'Operations & Incidents',      'Incidentes, runbooks, observabilidade contextualizada, mitigação operacional e consistência de serviço.',                   5, true, NOW(), NOW()),
    ('ca000000-0000-0000-0000-000000000006', 'knowledge',      'Knowledge & Documentation',   'Base de conhecimento operacional, documentação viva, notas técnicas e relações de contexto.',                               6, true, NOW(), NOW()),
    ('ca000000-0000-0000-0000-000000000007', 'ai',             'AI & Automation',             'Assistente de IA, agentes especializados, modelos, políticas de acesso e governança de uso de IA.',                         7, true, NOW(), NOW()),
    ('ca000000-0000-0000-0000-000000000008', 'governance',     'Governance & Compliance',     'Relatórios por persona, risk center, FinOps contextual, compliance e auditoria avançada.',                                  8, true, NOW(), NOW()),
    ('ca000000-0000-0000-0000-000000000009', 'configuration',  'Platform Configuration',      'Configurações da plataforma, feature flags, módulos, parâmetros operacionais e parametrização por tenant.',                 9, true, NOW(), NOW())
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 7. Ambientes de deployment para Change Intelligence (chg_deployment_environments)
--    Definem os stages do pipeline de promoção. Distintos dos env_environments
--    (que são ambientes do tenant); estes são stages de aprovação de change.
--    Order deve reflectir progressão: Dev → Staging → Production.
-- ---------------------------------------------------------------------------
INSERT INTO chg_deployment_environments (
    "Id",
    "Name",
    "Description",
    "Order",
    "RequiresApproval",
    "RequiresEvidencePack",
    "IsActive",
    "CreatedAt"
)
VALUES
    ('cb000000-0000-0000-0000-000000000001', 'Development', 'Ambiente de desenvolvimento. Deploys livres, sem aprovação formal requerida.',                                  1, false, false, true, NOW()),
    ('cb000000-0000-0000-0000-000000000002', 'Staging',     'Homologação. Requer pelo menos uma aprovação antes de promover para produção.',                                2, true,  false, true, NOW()),
    ('cb000000-0000-0000-0000-000000000003', 'Production',  'Produção. Requer aprovação formal, evidence pack e blast radius calculado antes do deploy.',                   3, true,  true,  true, NOW())
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 8. Templates de workflow de aprovação (chg_workflow_templates)
--    Definem o tipo de fluxo de aprovação por tipo de mudança e criticidade.
--    ChangeType  : 'Any' | 'Breaking' | 'Major' | 'Minor'
--    ApiCriticality: 'Any' | 'Low' | 'Medium' | 'High' | 'Critical'
-- ---------------------------------------------------------------------------
INSERT INTO chg_workflow_templates (
    "Id",
    "Name",
    "Description",
    "ChangeType",
    "ApiCriticality",
    "TargetEnvironment",
    "MinimumApprovers",
    "IsActive",
    "CreatedAt"
)
VALUES
    ('cc000000-0000-0000-0000-000000000001', 'Standard Change',   'Fluxo padrão para mudanças não críticas em produção. Requer 1 aprovador TechLead.',                    'Any',      'Medium',   'production', 1, true, NOW()),
    ('cc000000-0000-0000-0000-000000000002', 'Critical Change',   'Fluxo para mudanças breaking em serviços críticos ou de alta criticidade. Requer 2 aprovadores.',       'Breaking', 'Critical', 'production', 2, true, NOW()),
    ('cc000000-0000-0000-0000-000000000003', 'Emergency Hotfix',  'Fluxo express para hotfixes críticos de produção. Aprovação rápida com post-review obrigatório.',       'Any',      'Any',      'production', 1, true, NOW())
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 9. Políticas de SLA por stage de workflow (chg_sla_policies)
--    Definem tempo máximo por stage e escalação. WorkflowTemplateId FK para
--    chg_workflow_templates.
-- ---------------------------------------------------------------------------
INSERT INTO chg_sla_policies (
    "Id",
    "WorkflowTemplateId",
    "StageName",
    "MaxDurationHours",
    "EscalationEnabled",
    "EscalationTargetRole",
    "CreatedAt",
    "CreatedBy",
    "UpdatedAt",
    "UpdatedBy",
    "IsDeleted"
)
VALUES
    -- Standard Change: 2 stages
    ('cd000000-0000-0000-0000-000000000001', 'cc000000-0000-0000-0000-000000000001', 'Review',         24, true,  'TechLead',     NOW(), 'seed', NOW(), 'seed', false),
    ('cd000000-0000-0000-0000-000000000002', 'cc000000-0000-0000-0000-000000000001', 'Approval',       48, true,  'PlatformAdmin', NOW(), 'seed', NOW(), 'seed', false),
    -- Critical Change: 3 stages
    ('cd000000-0000-0000-0000-000000000003', 'cc000000-0000-0000-0000-000000000002', 'Security Review',24, true,  'SecurityReview',NOW(), 'seed', NOW(), 'seed', false),
    ('cd000000-0000-0000-0000-000000000004', 'cc000000-0000-0000-0000-000000000002', 'TechLead Approval',24,true, 'PlatformAdmin', NOW(), 'seed', NOW(), 'seed', false),
    ('cd000000-0000-0000-0000-000000000005', 'cc000000-0000-0000-0000-000000000002', 'Final Approval', 12, true,  'PlatformAdmin', NOW(), 'seed', NOW(), 'seed', false),
    -- Emergency Hotfix: 1 stage
    ('cd000000-0000-0000-0000-000000000006', 'cc000000-0000-0000-0000-000000000003', 'Express Approval', 4, true, 'TechLead',     NOW(), 'seed', NOW(), 'seed', false)
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 10. Gates de promoção por ambiente de deployment (chg_promotion_gates)
--     DeploymentEnvironmentId FK para chg_deployment_environments.
--     GateType válidos: 'ApprovalGate' | 'EvidencePackGate' | 'BlastRadiusGate' |
--                       'FreezeWindowGate' | 'TestCoverageGate' | 'ManualGate'
-- ---------------------------------------------------------------------------
INSERT INTO chg_promotion_gates (
    "Id",
    "DeploymentEnvironmentId",
    "GateName",
    "GateType",
    "IsRequired",
    "IsActive",
    "CreatedAt",
    "CreatedBy",
    "UpdatedAt",
    "UpdatedBy",
    "IsDeleted"
)
VALUES
    -- Staging: 1 gate
    ('ce000000-0000-0000-0000-000000000001', 'cb000000-0000-0000-0000-000000000002', 'Lead Approval',          'ApprovalGate',    true,  true, NOW(), 'seed', NOW(), 'seed', false),
    -- Production: 4 gates
    ('ce000000-0000-0000-0000-000000000002', 'cb000000-0000-0000-0000-000000000003', 'TechLead Approval',      'ApprovalGate',    true,  true, NOW(), 'seed', NOW(), 'seed', false),
    ('ce000000-0000-0000-0000-000000000003', 'cb000000-0000-0000-0000-000000000003', 'Evidence Pack',          'EvidencePackGate',true,  true, NOW(), 'seed', NOW(), 'seed', false),
    ('ce000000-0000-0000-0000-000000000004', 'cb000000-0000-0000-0000-000000000003', 'Blast Radius Assessment','BlastRadiusGate', true,  true, NOW(), 'seed', NOW(), 'seed', false),
    ('ce000000-0000-0000-0000-000000000005', 'cb000000-0000-0000-0000-000000000003', 'Freeze Window Check',    'FreezeWindowGate',true,  true, NOW(), 'seed', NOW(), 'seed', false)
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 11. Canais de notificação por tenant (ntf_channel_configurations)
--     ChannelType: 'Email' | 'InApp' | 'Webhook' | 'Teams' | 'Slack'
-- ---------------------------------------------------------------------------
INSERT INTO ntf_channel_configurations (
    "Id",
    "TenantId",
    "ChannelType",
    "DisplayName",
    "IsEnabled",
    "ConfigurationJson",
    "CreatedAt",
    "UpdatedAt"
)
VALUES
    ('cf000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000001', 'Email', 'Email Notifications', true, '{"provider":"smtp","retryAttempts":3,"retryDelaySeconds":60}',   NOW(), NOW()),
    ('cf000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000001', 'InApp', 'In-App Notifications',true, '{"maxUnread":100,"retentionDays":30,"autoMarkReadAfterDays":7}', NOW(), NOW())
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 12. Templates de notificação (ntf_templates)
--     Cobrem os eventos chave de identidade, mudanças e segurança.
--     IsBuiltIn=true: não pode ser apagado por utilizadores finais.
--     SubjectTemplate / BodyTemplate: suportam {{variavel}} para interpolação.
-- ---------------------------------------------------------------------------
INSERT INTO ntf_templates (
    "Id",
    "TenantId",
    "EventType",
    "Name",
    "SubjectTemplate",
    "BodyTemplate",
    "PlainTextTemplate",
    "Channel",
    "Locale",
    "IsActive",
    "IsBuiltIn",
    "CreatedAt",
    "UpdatedAt"
)
VALUES
    (
        'd0000000-0000-0000-0000-000000000001',
        'a0000000-0000-0000-0000-000000000001',
        'user.registered',
        'User Welcome Email',
        'Bem-vindo ao NexTraceOne, {{firstName}}!',
        '<h1>Bem-vindo, {{firstName}}!</h1><p>A sua conta foi criada com sucesso na plataforma <strong>NexTraceOne</strong>.</p><p>Email: <strong>{{email}}</strong></p><p>Para começar, aceda a <a href="{{loginUrl}}">{{loginUrl}}</a>.</p><p>Em caso de dúvida contacte o administrador da plataforma.</p>',
        'Bem-vindo, {{firstName}}! A sua conta foi criada. Email: {{email}}. Aceda em: {{loginUrl}}',
        'Email', 'pt-PT', true, true, NOW(), NOW()
    ),
    (
        'd0000000-0000-0000-0000-000000000002',
        'a0000000-0000-0000-0000-000000000001',
        'user.password-reset',
        'Password Reset Request',
        '[NexTraceOne] Redefinição de senha',
        '<h1>Redefinição de Senha</h1><p>Recebemos um pedido para redefinir a senha da conta <strong>{{email}}</strong>.</p><p>Clique no link abaixo para criar uma nova senha (válido por <strong>{{expiryMinutes}} minutos</strong>):</p><p><a href="{{resetUrl}}">Redefinir Senha</a></p><p>Se não solicitou esta alteração, ignore este email — a sua senha não será alterada.</p>',
        'Redefinição de Senha para {{email}}. Link válido por {{expiryMinutes}} min: {{resetUrl}}. Se não solicitou, ignore este email.',
        'Email', 'pt-PT', true, true, NOW(), NOW()
    ),
    (
        'd0000000-0000-0000-0000-000000000003',
        'a0000000-0000-0000-0000-000000000001',
        'change.approved',
        'Change Approved Notification',
        '[NexTraceOne] Mudança aprovada — {{serviceName}} {{version}}',
        '<h1>Mudança Aprovada ✓</h1><p>A mudança <strong>{{serviceName}} {{version}}</strong> foi aprovada para deploy em <strong>{{environment}}</strong>.</p><ul><li>Aprovado por: {{approverName}}</li><li>Work Item: {{workItemReference}}</li><li>Nível de mudança: {{changeLevel}}</li></ul>',
        'Mudança Aprovada: {{serviceName}} {{version}} aprovada para {{environment}} por {{approverName}}. WI: {{workItemReference}}',
        'Email', 'pt-PT', true, true, NOW(), NOW()
    ),
    (
        'd0000000-0000-0000-0000-000000000004',
        'a0000000-0000-0000-0000-000000000001',
        'change.rejected',
        'Change Rejected Notification',
        '[NexTraceOne] Mudança rejeitada — {{serviceName}} {{version}}',
        '<h1>Mudança Rejeitada ✗</h1><p>A mudança <strong>{{serviceName}} {{version}}</strong> foi rejeitada para deploy em <strong>{{environment}}</strong>.</p><ul><li>Rejeitado por: {{reviewerName}}</li><li>Motivo: {{rejectionReason}}</li><li>Work Item: {{workItemReference}}</li></ul>',
        'Mudança Rejeitada: {{serviceName}} {{version}} para {{environment}} por {{reviewerName}}. Motivo: {{rejectionReason}}',
        'Email', 'pt-PT', true, true, NOW(), NOW()
    ),
    (
        'd0000000-0000-0000-0000-000000000005',
        'a0000000-0000-0000-0000-000000000001',
        'user.security-alert',
        'Security Alert Notification',
        '[NexTraceOne] Alerta de segurança — {{alertType}}',
        '<h1>Alerta de Segurança</h1><p>Foi detectada uma actividade de segurança na sua conta:</p><ul><li>Tipo: {{alertType}}</li><li>IP de origem: {{sourceIp}}</li><li>Data/Hora: {{eventAt}}</li></ul><p>Se não reconhece esta actividade, contacte o administrador imediatamente.</p>',
        'Alerta de Segurança: {{alertType}} detectado. IP: {{sourceIp}} em {{eventAt}}. Se não reconhece, contacte o admin.',
        'Email', 'pt-PT', true, true, NOW(), NOW()
    ),
    (
        'd0000000-0000-0000-0000-000000000006',
        'a0000000-0000-0000-0000-000000000001',
        'change.review-required',
        'Change Review Required',
        '[NexTraceOne] Revisão necessária — {{serviceName}} {{version}}',
        '<h1>Revisão Pendente</h1><p>A mudança <strong>{{serviceName}} {{version}}</strong> para <strong>{{environment}}</strong> aguarda a sua revisão.</p><ul><li>Submetida por: {{submittedBy}}</li><li>Nível de risco: {{changeLevel}}</li><li>Prazo SLA: {{slaDeadline}}</li></ul>',
        'Revisão pendente: {{serviceName}} {{version}} para {{environment}} submetida por {{submittedBy}}. Prazo: {{slaDeadline}}',
        'Email', 'pt-PT', true, true, NOW(), NOW()
    )
ON CONFLICT DO NOTHING;

COMMIT;

-- =============================================================================
-- Resumo do que foi inserido:
--   iam_tenants                 : 1   (NexTraceOne)
--   iam_users                   : 1   (admin@nextraceone.io — PlatformAdmin)
--   iam_tenant_memberships      : 1   (admin → nextraceone → PlatformAdmin)
--   env_environments            : 3   (Development, Staging, Production)
--   env_environment_accesses    : 3   (admin com nível 'admin' em todos os ambientes)
--   cfg_modules                 : 9   (foundation, services, contracts, changes, operations,
--                                       knowledge, ai, governance, configuration)
--   chg_deployment_environments : 3   (Development, Staging, Production)
--   chg_workflow_templates      : 3   (Standard, Critical, Emergency)
--   chg_sla_policies            : 6   (2 Standard + 3 Critical + 1 Emergency)
--   chg_promotion_gates         : 5   (1 Staging + 4 Production)
--   ntf_channel_configurations  : 2   (Email, InApp)
--   ntf_templates               : 6   (welcome, password-reset, change-approved,
--                                       change-rejected, security-alert, review-required)
-- =============================================================================
