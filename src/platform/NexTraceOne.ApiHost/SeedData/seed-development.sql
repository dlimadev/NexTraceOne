-- ═══════════════════════════════════════════════════════════════════════════════
-- NEXTRACEONE — Seed data para ambiente de desenvolvimento
-- Insere dados mockados em todas as tabelas dos módulos para navegação no frontend.
-- Executado automaticamente em Development após migrações.
-- Idempotente: cada INSERT usa ON CONFLICT DO NOTHING para re-execução segura.
-- ═══════════════════════════════════════════════════════════════════════════════

-- ═══ IDs fixos para referências cruzadas ═══════════════════════════════════
-- Tenants
-- t1 = 'a0000000-0000-0000-0000-000000000001' (NexTrace Corp)
-- t2 = 'a0000000-0000-0000-0000-000000000002' (Acme Fintech)

-- Users
-- u1  = 'b0000000-0000-0000-0000-000000000001' (admin@nextraceone.dev - PlatformAdmin)
-- u2  = 'b0000000-0000-0000-0000-000000000002' (techlead@nextraceone.dev - TechLead)
-- u3  = 'b0000000-0000-0000-0000-000000000003' (dev@nextraceone.dev - Developer)
-- u4  = 'b0000000-0000-0000-0000-000000000004' (auditor@nextraceone.dev - Auditor)
-- u5  = 'b0000000-0000-0000-0000-000000000005' (ana.costa@nextraceone.dev - Developer)
-- u6  = 'b0000000-0000-0000-0000-000000000006' (pedro.alves@nextraceone.dev - TechLead)
-- u7  = 'b0000000-0000-0000-0000-000000000007' (lucia.ferreira@nextraceone.dev - Developer)
-- u8  = 'b0000000-0000-0000-0000-000000000008' (rafael.lima@nextraceone.dev - Developer)
-- u9  = 'b0000000-0000-0000-0000-000000000009' (camila.rocha@nextraceone.dev - PlatformAdmin)
-- u10 = 'b0000000-0000-0000-0000-000000000010' (felipe.souza@nextraceone.dev - Auditor)

-- Roles (from Identity migration seed)
-- r1 = '1e91a557-fade-46df-b248-0f5f5899c001' (PlatformAdmin)
-- r2 = '1e91a557-fade-46df-b248-0f5f5899c002' (TechLead)
-- r3 = '1e91a557-fade-46df-b248-0f5f5899c003' (Developer)
-- r5 = '1e91a557-fade-46df-b248-0f5f5899c005' (Auditor)

-- Services
-- svc1  = 'c0000000-0000-0000-0000-000000000001' (Orders Service)
-- svc2  = 'c0000000-0000-0000-0000-000000000002' (Payments Service)
-- svc3  = 'c0000000-0000-0000-0000-000000000003' (Inventory Service)
-- svc4  = 'c0000000-0000-0000-0000-000000000004' (Notifications Service)
-- svc5  = 'c0000000-0000-0000-0000-000000000005' (Users Service)
-- svc6  = 'c0000000-0000-0000-0000-000000000006' (Shipping Service)
-- svc7  = 'c0000000-0000-0000-0000-000000000007' (Analytics Service)
-- svc8  = 'c0000000-0000-0000-0000-000000000008' (Gateway Service)
-- svc9  = 'c0000000-0000-0000-0000-000000000009' (Search Service)
-- svc10 = 'c0000000-0000-0000-0000-000000000010' (Pricing Service)

-- APIs
-- api1  = 'd0000000-0000-0000-0000-000000000001' (Orders API)
-- api2  = 'd0000000-0000-0000-0000-000000000002' (Payments API)
-- api3  = 'd0000000-0000-0000-0000-000000000003' (Inventory API)
-- api4  = 'd0000000-0000-0000-0000-000000000004' (Notifications API)
-- api5  = 'd0000000-0000-0000-0000-000000000005' (Users API)
-- api6  = 'd0000000-0000-0000-0000-000000000006' (Shipping API)
-- api7  = 'd0000000-0000-0000-0000-000000000007' (Analytics API)
-- api8  = 'd0000000-0000-0000-0000-000000000008' (Gateway API)
-- api9  = 'd0000000-0000-0000-0000-000000000009' (Search API)
-- api10 = 'd0000000-0000-0000-0000-000000000010' (Pricing API)

-- ═══════════════════════════════════════════════════════════════════════════════
-- 1. IDENTITY MODULE — Tenant, Users, Memberships, Environments
-- ═══════════════════════════════════════════════════════════════════════════════

-- Tenant
INSERT INTO identity_tenants ("Id", "Name", "Slug", "IsActive", "CreatedAt")
VALUES
  ('a0000000-0000-0000-0000-000000000001', 'NexTrace Corp', 'nexttrace-corp', true, '2025-01-01T00:00:00Z'),
  ('a0000000-0000-0000-0000-000000000002', 'Acme Fintech', 'acme-fintech', true, '2025-02-01T00:00:00Z')
ON CONFLICT DO NOTHING;

-- Users (password: Admin@123 for all)
-- Hash gerado com PBKDF2-SHA256 100k iterations, formato v1.{salt}.{hash}
INSERT INTO identity_users ("Id", "Email", "first_name", "last_name", "PasswordHash", "IsActive", "LastLoginAt", "FailedLoginAttempts")
VALUES
  ('b0000000-0000-0000-0000-000000000001', 'admin@nextraceone.dev', 'Admin', 'NexTrace', 'v1.JmfnVELOwwfJunLeGbMN/g==.nkH8yRKF34TIfYNWcx84oAGJ+f2Q725ByMUk1JC7TNw=', true, '2025-06-01T10:00:00Z', 0),
  ('b0000000-0000-0000-0000-000000000002', 'techlead@nextraceone.dev', 'Maria', 'Silva', 'v1.JmfnVELOwwfJunLeGbMN/g==.nkH8yRKF34TIfYNWcx84oAGJ+f2Q725ByMUk1JC7TNw=', true, '2025-06-01T09:30:00Z', 0),
  ('b0000000-0000-0000-0000-000000000003', 'dev@nextraceone.dev', 'João', 'Santos', 'v1.JmfnVELOwwfJunLeGbMN/g==.nkH8yRKF34TIfYNWcx84oAGJ+f2Q725ByMUk1JC7TNw=', true, '2025-06-01T08:00:00Z', 0),
  ('b0000000-0000-0000-0000-000000000004', 'auditor@nextraceone.dev', 'Carlos', 'Mendes', 'v1.JmfnVELOwwfJunLeGbMN/g==.nkH8yRKF34TIfYNWcx84oAGJ+f2Q725ByMUk1JC7TNw=', true, '2025-05-28T14:00:00Z', 0),
  ('b0000000-0000-0000-0000-000000000005', 'ana.costa@nextraceone.dev', 'Ana', 'Costa', 'v1.JmfnVELOwwfJunLeGbMN/g==.nkH8yRKF34TIfYNWcx84oAGJ+f2Q725ByMUk1JC7TNw=', true, '2025-06-02T11:00:00Z', 0),
  ('b0000000-0000-0000-0000-000000000006', 'pedro.alves@nextraceone.dev', 'Pedro', 'Alves', 'v1.JmfnVELOwwfJunLeGbMN/g==.nkH8yRKF34TIfYNWcx84oAGJ+f2Q725ByMUk1JC7TNw=', true, '2025-06-02T09:15:00Z', 0),
  ('b0000000-0000-0000-0000-000000000007', 'lucia.ferreira@nextraceone.dev', 'Lúcia', 'Ferreira', 'v1.JmfnVELOwwfJunLeGbMN/g==.nkH8yRKF34TIfYNWcx84oAGJ+f2Q725ByMUk1JC7TNw=', true, '2025-06-01T16:45:00Z', 0),
  ('b0000000-0000-0000-0000-000000000008', 'rafael.lima@nextraceone.dev', 'Rafael', 'Lima', 'v1.JmfnVELOwwfJunLeGbMN/g==.nkH8yRKF34TIfYNWcx84oAGJ+f2Q725ByMUk1JC7TNw=', true, '2025-05-30T10:20:00Z', 1),
  ('b0000000-0000-0000-0000-000000000009', 'camila.rocha@nextraceone.dev', 'Camila', 'Rocha', 'v1.JmfnVELOwwfJunLeGbMN/g==.nkH8yRKF34TIfYNWcx84oAGJ+f2Q725ByMUk1JC7TNw=', true, '2025-06-02T08:00:00Z', 0),
  ('b0000000-0000-0000-0000-000000000010', 'felipe.souza@nextraceone.dev', 'Felipe', 'Souza', 'v1.JmfnVELOwwfJunLeGbMN/g==.nkH8yRKF34TIfYNWcx84oAGJ+f2Q725ByMUk1JC7TNw=', true, '2025-06-01T14:30:00Z', 0)
ON CONFLICT DO NOTHING;

-- Tenant Memberships
INSERT INTO identity_tenant_memberships ("Id", "UserId", "TenantId", "RoleId", "JoinedAt", "IsActive")
VALUES
  -- NexTrace Corp
  ('e0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000001', '1e91a557-fade-46df-b248-0f5f5899c001', '2025-01-01T00:00:00Z', true),
  ('e0000000-0000-0000-0000-000000000002', 'b0000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000001', '1e91a557-fade-46df-b248-0f5f5899c002', '2025-01-01T00:00:00Z', true),
  ('e0000000-0000-0000-0000-000000000003', 'b0000000-0000-0000-0000-000000000003', 'a0000000-0000-0000-0000-000000000001', '1e91a557-fade-46df-b248-0f5f5899c003', '2025-01-15T00:00:00Z', true),
  ('e0000000-0000-0000-0000-000000000004', 'b0000000-0000-0000-0000-000000000004', 'a0000000-0000-0000-0000-000000000001', '1e91a557-fade-46df-b248-0f5f5899c005', '2025-02-01T00:00:00Z', true),
  ('e0000000-0000-0000-0000-000000000005', 'b0000000-0000-0000-0000-000000000005', 'a0000000-0000-0000-0000-000000000001', '1e91a557-fade-46df-b248-0f5f5899c003', '2025-03-01T00:00:00Z', true),
  ('e0000000-0000-0000-0000-000000000006', 'b0000000-0000-0000-0000-000000000006', 'a0000000-0000-0000-0000-000000000001', '1e91a557-fade-46df-b248-0f5f5899c002', '2025-03-10T00:00:00Z', true),
  ('e0000000-0000-0000-0000-000000000007', 'b0000000-0000-0000-0000-000000000007', 'a0000000-0000-0000-0000-000000000001', '1e91a557-fade-46df-b248-0f5f5899c003', '2025-03-15T00:00:00Z', true),
  ('e0000000-0000-0000-0000-000000000008', 'b0000000-0000-0000-0000-000000000008', 'a0000000-0000-0000-0000-000000000001', '1e91a557-fade-46df-b248-0f5f5899c003', '2025-04-01T00:00:00Z', true),
  -- Acme Fintech
  ('e0000000-0000-0000-0000-000000000009', 'b0000000-0000-0000-0000-000000000009', 'a0000000-0000-0000-0000-000000000002', '1e91a557-fade-46df-b248-0f5f5899c001', '2025-02-01T00:00:00Z', true),
  ('e0000000-0000-0000-0000-000000000010', 'b0000000-0000-0000-0000-000000000010', 'a0000000-0000-0000-0000-000000000002', '1e91a557-fade-46df-b248-0f5f5899c005', '2025-02-15T00:00:00Z', true)
ON CONFLICT DO NOTHING;

-- Environments
INSERT INTO identity_environments ("Id", "TenantId", "Name", "Slug", "SortOrder", "IsActive", "CreatedAt")
VALUES
  -- NexTrace Corp
  ('f0000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000001', 'Development', 'dev', 1, true, '2025-01-01T00:00:00Z'),
  ('f0000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000001', 'Staging', 'staging', 2, true, '2025-01-01T00:00:00Z'),
  ('f0000000-0000-0000-0000-000000000003', 'a0000000-0000-0000-0000-000000000001', 'Production', 'prod', 3, true, '2025-01-01T00:00:00Z'),
  -- Acme Fintech
  ('f0000000-0000-0000-0000-000000000004', 'a0000000-0000-0000-0000-000000000002', 'Development', 'dev', 1, true, '2025-02-01T00:00:00Z'),
  ('f0000000-0000-0000-0000-000000000005', 'a0000000-0000-0000-0000-000000000002', 'Production', 'prod', 2, true, '2025-02-01T00:00:00Z')
ON CONFLICT DO NOTHING;

-- Environment Accesses
INSERT INTO identity_environment_accesses ("Id", "UserId", "TenantId", "EnvironmentId", "GrantedBy", "AccessLevel", "GrantedAt", "ExpiresAt", "RevokedAt", "IsActive")
VALUES
  ('fa000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000001', 'f0000000-0000-0000-0000-000000000003', 'b0000000-0000-0000-0000-000000000001', 'Admin', '2025-01-01T00:00:00Z', NULL, NULL, true),
  ('fa000000-0000-0000-0000-000000000002', 'b0000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000001', 'f0000000-0000-0000-0000-000000000003', 'b0000000-0000-0000-0000-000000000001', 'ReadWrite', '2025-01-01T00:00:00Z', NULL, NULL, true),
  ('fa000000-0000-0000-0000-000000000003', 'b0000000-0000-0000-0000-000000000003', 'a0000000-0000-0000-0000-000000000001', 'f0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000001', 'ReadWrite', '2025-01-15T00:00:00Z', NULL, NULL, true),
  ('fa000000-0000-0000-0000-000000000004', 'b0000000-0000-0000-0000-000000000003', 'a0000000-0000-0000-0000-000000000001', 'f0000000-0000-0000-0000-000000000002', 'b0000000-0000-0000-0000-000000000002', 'ReadOnly', '2025-02-01T00:00:00Z', NULL, NULL, true),
  ('fa000000-0000-0000-0000-000000000005', 'b0000000-0000-0000-0000-000000000005', 'a0000000-0000-0000-0000-000000000001', 'f0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000001', 'ReadWrite', '2025-03-01T00:00:00Z', NULL, NULL, true),
  ('fa000000-0000-0000-0000-000000000006', 'b0000000-0000-0000-0000-000000000006', 'a0000000-0000-0000-0000-000000000001', 'f0000000-0000-0000-0000-000000000003', 'b0000000-0000-0000-0000-000000000001', 'ReadWrite', '2025-03-10T00:00:00Z', NULL, NULL, true),
  ('fa000000-0000-0000-0000-000000000007', 'b0000000-0000-0000-0000-000000000007', 'a0000000-0000-0000-0000-000000000001', 'f0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000002', 'ReadWrite', '2025-03-15T00:00:00Z', NULL, NULL, true),
  ('fa000000-0000-0000-0000-000000000008', 'b0000000-0000-0000-0000-000000000008', 'a0000000-0000-0000-0000-000000000001', 'f0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000006', 'ReadWrite', '2025-04-01T00:00:00Z', '2025-12-31T23:59:59Z', NULL, true)
ON CONFLICT DO NOTHING;

-- Security Events
INSERT INTO identity_security_events ("Id", "TenantId", "UserId", "SessionId", "EventType", "Description", "RiskScore", "IpAddress", "UserAgent", "MetadataJson", "OccurredAt", "IsReviewed", "ReviewedAt", "ReviewedBy")
VALUES
  ('fb000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000001', NULL, 'LoginSuccess', 'Successful local login from known IP', 5, '192.168.1.100', 'Mozilla/5.0 Chrome/125.0', '{"location":"São Paulo","method":"Local"}', '2025-06-01T10:00:00Z', true, '2025-06-01T12:00:00Z', 'b0000000-0000-0000-0000-000000000001'),
  ('fb000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000008', NULL, 'LoginFailed', 'Failed login attempt — incorrect password', 35, '10.0.0.55', 'Mozilla/5.0 Firefox/127.0', '{"attempts":1,"location":"Rio de Janeiro"}', '2025-05-30T10:18:00Z', false, NULL, NULL),
  ('fb000000-0000-0000-0000-000000000003', 'a0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000003', NULL, 'UnusualLocation', 'Login from new geographic location detected', 60, '203.45.67.89', 'Mozilla/5.0 Safari/17.5', '{"location":"Tokyo, Japan","previousLocations":["São Paulo","Curitiba"]}', '2025-06-01T03:15:00Z', false, NULL, NULL),
  ('fb000000-0000-0000-0000-000000000004', 'a0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000002', NULL, 'RapidApproval', 'Workflow approved in less than 10 seconds', 45, '192.168.1.102', 'Mozilla/5.0 Chrome/125.0', '{"workflowId":"51000000-0000-0000-0000-000000000002","approvalTimeSeconds":7}', '2025-05-20T17:45:05Z', true, '2025-05-21T09:00:00Z', 'b0000000-0000-0000-0000-000000000001'),
  ('fb000000-0000-0000-0000-000000000005', 'a0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000005', NULL, 'OffHoursAccess', 'Access attempt outside normal business hours', 25, '192.168.1.110', 'Mozilla/5.0 Chrome/125.0', '{"accessTime":"03:45","normalRange":"08:00-20:00"}', '2025-06-02T03:45:00Z', false, NULL, NULL),
  ('fb000000-0000-0000-0000-000000000006', 'a0000000-0000-0000-0000-000000000001', NULL, NULL, 'BruteForceAttempt', 'Multiple failed login attempts from single IP in 5 minutes', 80, '185.220.101.34', 'python-requests/2.31.0', '{"targetEmails":["admin@nextraceone.dev","root@nextraceone.dev"],"attempts":15,"windowMinutes":5}', '2025-05-25T14:22:00Z', true, '2025-05-25T14:30:00Z', 'b0000000-0000-0000-0000-000000000001'),
  ('fb000000-0000-0000-0000-000000000007', 'a0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000006', NULL, 'MultipleSessions', 'User has 4 concurrent active sessions', 30, '192.168.1.106', 'Mozilla/5.0 Chrome/125.0', '{"activeSessions":4,"maxExpected":2}', '2025-06-02T09:15:00Z', false, NULL, NULL),
  ('fb000000-0000-0000-0000-000000000008', 'a0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000007', NULL, 'FirstAccessResource', 'First time accessing Production environment', 20, '192.168.1.107', 'Mozilla/5.0 Chrome/125.0', '{"resource":"Production","previousAccess":"never"}', '2025-06-01T16:45:00Z', false, NULL, NULL)
ON CONFLICT DO NOTHING;

-- ═══════════════════════════════════════════════════════════════════════════════
-- 2. LICENSING MODULE — License, Capabilities, Quotas
-- ═══════════════════════════════════════════════════════════════════════════════

INSERT INTO licensing_licenses ("Id", "LicenseKey", "CustomerName", "IssuedAt", "ExpiresAt", "MaxActivations", "IsActive", "Type", "Edition", "GracePeriodDays", "TrialConverted", "TrialExtensionCount")
VALUES ('10000000-0000-0000-0000-000000000001', 'NXTRC-ENT-2025-DEMO-KEY1', 'NexTrace Corp', '2025-01-01T00:00:00Z', '2026-12-31T23:59:59Z', 10, true, 2, 2, 30, false, 0)
ON CONFLICT DO NOTHING;

INSERT INTO licensing_capabilities ("Id", "LicenseId", "Code", "Name", "IsEnabled")
VALUES
  ('11000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'workflow.engine', 'Workflow Engine', true),
  ('11000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001', 'blast.radius', 'Blast Radius Analysis', true),
  ('11000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000001', 'audit.trail', 'Audit Trail', true),
  ('11000000-0000-0000-0000-000000000004', '10000000-0000-0000-0000-000000000001', 'contract.diff', 'Contract Diff', true),
  ('11000000-0000-0000-0000-000000000005', '10000000-0000-0000-0000-000000000001', 'promotion.gates', 'Promotion Gates', true),
  ('11000000-0000-0000-0000-000000000006', '10000000-0000-0000-0000-000000000001', 'developer.portal', 'Developer Portal', true)
ON CONFLICT DO NOTHING;

INSERT INTO licensing_activations ("Id", "LicenseId", "HardwareFingerprint", "ActivatedBy", "ActivatedAt", "IsActive")
VALUES ('12000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'dev-machine-001', 'admin@nextraceone.dev', '2025-01-02T10:00:00Z', true)
ON CONFLICT DO NOTHING;

-- ═══════════════════════════════════════════════════════════════════════════════
-- 3. ENGINEERING GRAPH MODULE — Services, APIs, Consumer Relationships
-- ═══════════════════════════════════════════════════════════════════════════════

-- Services
INSERT INTO eg_service_assets ("Id", "Name", "Domain", "TeamName")
VALUES
  ('c0000000-0000-0000-0000-000000000001', 'Orders Service', 'Commerce', 'Team Alpha'),
  ('c0000000-0000-0000-0000-000000000002', 'Payments Service', 'Finance', 'Team Beta'),
  ('c0000000-0000-0000-0000-000000000003', 'Inventory Service', 'Logistics', 'Team Gamma'),
  ('c0000000-0000-0000-0000-000000000004', 'Notifications Service', 'Platform', 'Team Delta'),
  ('c0000000-0000-0000-0000-000000000005', 'Users Service', 'Identity', 'Team Alpha'),
  ('c0000000-0000-0000-0000-000000000006', 'Shipping Service', 'Logistics', 'Team Gamma'),
  ('c0000000-0000-0000-0000-000000000007', 'Analytics Service', 'Platform', 'Team Delta'),
  ('c0000000-0000-0000-0000-000000000008', 'Gateway Service', 'Platform', 'Team Alpha'),
  ('c0000000-0000-0000-0000-000000000009', 'Search Service', 'Platform', 'Team Beta'),
  ('c0000000-0000-0000-0000-000000000010', 'Pricing Service', 'Commerce', 'Team Beta')
ON CONFLICT DO NOTHING;

-- APIs
INSERT INTO eg_api_assets ("Id", "Name", "RoutePattern", "Version", "Visibility", "OwnerServiceId")
VALUES
  ('d0000000-0000-0000-0000-000000000001', 'Orders API', '/api/v1/orders', 'v1.3.0', 'Public', 'c0000000-0000-0000-0000-000000000001'),
  ('d0000000-0000-0000-0000-000000000002', 'Payments API', '/api/v1/payments', 'v2.1.0', 'Public', 'c0000000-0000-0000-0000-000000000002'),
  ('d0000000-0000-0000-0000-000000000003', 'Inventory API', '/api/v1/inventory', 'v1.0.0', 'Internal', 'c0000000-0000-0000-0000-000000000003'),
  ('d0000000-0000-0000-0000-000000000004', 'Notifications API', '/api/v1/notifications', 'v1.2.0', 'Internal', 'c0000000-0000-0000-0000-000000000004'),
  ('d0000000-0000-0000-0000-000000000005', 'Users API', '/api/v1/users', 'v1.1.0', 'Public', 'c0000000-0000-0000-0000-000000000005'),
  ('d0000000-0000-0000-0000-000000000006', 'Shipping API', '/api/v1/shipping', 'v1.0.0', 'Public', 'c0000000-0000-0000-0000-000000000006'),
  ('d0000000-0000-0000-0000-000000000007', 'Analytics API', '/api/v1/analytics', 'v1.0.0', 'Internal', 'c0000000-0000-0000-0000-000000000007'),
  ('d0000000-0000-0000-0000-000000000008', 'Gateway API', '/api/v1/gateway', 'v2.0.0', 'Public', 'c0000000-0000-0000-0000-000000000008'),
  ('d0000000-0000-0000-0000-000000000009', 'Search API', '/api/v1/search', 'v1.2.0', 'Public', 'c0000000-0000-0000-0000-000000000009'),
  ('d0000000-0000-0000-0000-000000000010', 'Pricing API', '/api/v1/pricing', 'v1.1.0', 'Internal', 'c0000000-0000-0000-0000-000000000010')
ON CONFLICT DO NOTHING;

-- Consumer Assets
INSERT INTO eg_consumer_assets ("Id", "Name", "Kind", "Environment")
VALUES
  ('c1000000-0000-0000-0000-000000000001', 'Payments Service', 'Service', 'Production'),
  ('c1000000-0000-0000-0000-000000000002', 'Inventory Service', 'Service', 'Production'),
  ('c1000000-0000-0000-0000-000000000003', 'Notifications Service', 'Service', 'Production'),
  ('c1000000-0000-0000-0000-000000000004', 'Orders Service', 'Service', 'Production'),
  ('c1000000-0000-0000-0000-000000000005', 'Mobile App', 'Client', 'Production'),
  ('c1000000-0000-0000-0000-000000000006', 'Shipping Service', 'Service', 'Production'),
  ('c1000000-0000-0000-0000-000000000007', 'Analytics Service', 'Service', 'Production'),
  ('c1000000-0000-0000-0000-000000000008', 'Gateway Service', 'Service', 'Production'),
  ('c1000000-0000-0000-0000-000000000009', 'Search Service', 'Service', 'Production'),
  ('c1000000-0000-0000-0000-000000000010', 'Pricing Service', 'Service', 'Production'),
  ('c1000000-0000-0000-0000-000000000011', 'Admin Dashboard', 'Client', 'Production'),
  ('c1000000-0000-0000-0000-000000000012', 'Partner Portal', 'Client', 'Staging')
ON CONFLICT DO NOTHING;

-- Consumer Relationships
INSERT INTO eg_consumer_relationships ("Id", "ApiAssetId", "ConsumerAssetId", "ConsumerName", "SourceType", "ConfidenceScore", "FirstObservedAt", "LastObservedAt")
VALUES
  ('c2000000-0000-0000-0000-000000000001', 'd0000000-0000-0000-0000-000000000001', 'c1000000-0000-0000-0000-000000000001', 'Payments Service', 'TrafficAnalysis', 0.9500, '2025-03-01T00:00:00Z', '2025-06-01T12:00:00Z'),
  ('c2000000-0000-0000-0000-000000000002', 'd0000000-0000-0000-0000-000000000001', 'c1000000-0000-0000-0000-000000000002', 'Inventory Service', 'TrafficAnalysis', 0.8800, '2025-03-15T00:00:00Z', '2025-06-01T10:00:00Z'),
  ('c2000000-0000-0000-0000-000000000003', 'd0000000-0000-0000-0000-000000000001', 'c1000000-0000-0000-0000-000000000005', 'Mobile App', 'ContractImport', 0.9900, '2025-02-01T00:00:00Z', '2025-06-01T14:00:00Z'),
  ('c2000000-0000-0000-0000-000000000004', 'd0000000-0000-0000-0000-000000000002', 'c1000000-0000-0000-0000-000000000004', 'Orders Service', 'TrafficAnalysis', 0.9200, '2025-04-01T00:00:00Z', '2025-06-01T11:00:00Z'),
  ('c2000000-0000-0000-0000-000000000005', 'd0000000-0000-0000-0000-000000000002', 'c1000000-0000-0000-0000-000000000003', 'Notifications Service', 'TrafficAnalysis', 0.7500, '2025-04-15T00:00:00Z', '2025-06-01T09:00:00Z'),
  ('c2000000-0000-0000-0000-000000000006', 'd0000000-0000-0000-0000-000000000003', 'c1000000-0000-0000-0000-000000000004', 'Orders Service', 'ContractImport', 0.9800, '2025-03-10T00:00:00Z', '2025-06-01T13:00:00Z'),
  ('c2000000-0000-0000-0000-000000000007', 'd0000000-0000-0000-0000-000000000004', 'c1000000-0000-0000-0000-000000000004', 'Orders Service', 'TrafficAnalysis', 0.8200, '2025-05-01T00:00:00Z', '2025-06-01T08:00:00Z'),
  ('c2000000-0000-0000-0000-000000000008', 'd0000000-0000-0000-0000-000000000001', 'c1000000-0000-0000-0000-000000000006', 'Shipping Service', 'TrafficAnalysis', 0.9100, '2025-04-01T00:00:00Z', '2025-06-02T09:00:00Z'),
  ('c2000000-0000-0000-0000-000000000009', 'd0000000-0000-0000-0000-000000000001', 'c1000000-0000-0000-0000-000000000011', 'Admin Dashboard', 'ContractImport', 0.9700, '2025-02-15T00:00:00Z', '2025-06-02T10:00:00Z'),
  ('c2000000-0000-0000-0000-000000000010', 'd0000000-0000-0000-0000-000000000002', 'c1000000-0000-0000-0000-000000000005', 'Mobile App', 'TrafficAnalysis', 0.8900, '2025-04-10T00:00:00Z', '2025-06-02T07:30:00Z'),
  ('c2000000-0000-0000-0000-000000000011', 'd0000000-0000-0000-0000-000000000006', 'c1000000-0000-0000-0000-000000000004', 'Orders Service', 'ContractImport', 0.9600, '2025-04-15T00:00:00Z', '2025-06-02T11:00:00Z'),
  ('c2000000-0000-0000-0000-000000000012', 'd0000000-0000-0000-0000-000000000006', 'c1000000-0000-0000-0000-000000000005', 'Mobile App', 'TrafficAnalysis', 0.8500, '2025-05-01T00:00:00Z', '2025-06-02T08:00:00Z'),
  ('c2000000-0000-0000-0000-000000000013', 'd0000000-0000-0000-0000-000000000010', 'c1000000-0000-0000-0000-000000000004', 'Orders Service', 'TrafficAnalysis', 0.9300, '2025-05-10T00:00:00Z', '2025-06-02T12:00:00Z'),
  ('c2000000-0000-0000-0000-000000000014', 'd0000000-0000-0000-0000-000000000010', 'c1000000-0000-0000-0000-000000000008', 'Gateway Service', 'TrafficAnalysis', 0.8700, '2025-05-15T00:00:00Z', '2025-06-02T13:00:00Z'),
  ('c2000000-0000-0000-0000-000000000015', 'd0000000-0000-0000-0000-000000000009', 'c1000000-0000-0000-0000-000000000005', 'Mobile App', 'ContractImport', 0.9400, '2025-05-20T00:00:00Z', '2025-06-02T14:00:00Z'),
  ('c2000000-0000-0000-0000-000000000016', 'd0000000-0000-0000-0000-000000000009', 'c1000000-0000-0000-0000-000000000011', 'Admin Dashboard', 'TrafficAnalysis', 0.8100, '2025-05-25T00:00:00Z', '2025-06-02T15:00:00Z'),
  ('c2000000-0000-0000-0000-000000000017', 'd0000000-0000-0000-0000-000000000007', 'c1000000-0000-0000-0000-000000000011', 'Admin Dashboard', 'ContractImport', 0.9900, '2025-03-01T00:00:00Z', '2025-06-02T16:00:00Z'),
  ('c2000000-0000-0000-0000-000000000018', 'd0000000-0000-0000-0000-000000000005', 'c1000000-0000-0000-0000-000000000008', 'Gateway Service', 'TrafficAnalysis', 0.9000, '2025-03-15T00:00:00Z', '2025-06-02T09:30:00Z')
ON CONFLICT DO NOTHING;

-- ═══════════════════════════════════════════════════════════════════════════════
-- 4. CONTRACTS MODULE — Contract Versions with OpenAPI specs
-- ═══════════════════════════════════════════════════════════════════════════════

INSERT INTO ct_contract_versions ("Id", "ApiAssetId", "SemVer", "SpecContent", "Format", "Protocol", "LifecycleState", "ImportedFrom", "IsLocked", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  -- Orders API (api1) — v1.2.0 → v1.3.0
  ('20000000-0000-0000-0000-000000000001', 'd0000000-0000-0000-0000-000000000001', '1.2.0', '{"openapi":"3.0.0","info":{"title":"Orders API","version":"1.2.0"},"paths":{"/orders":{"get":{"summary":"List orders"}}}}', 'json', 'OpenApi', 'Approved', 'upload', true, '2025-03-01T10:00:00Z', 'admin@nextraceone.dev', '2025-03-01T10:00:00Z', 'admin@nextraceone.dev', false),
  ('20000000-0000-0000-0000-000000000002', 'd0000000-0000-0000-0000-000000000001', '1.3.0', '{"openapi":"3.0.0","info":{"title":"Orders API","version":"1.3.0"},"paths":{"/orders":{"get":{"summary":"List orders"},"post":{"summary":"Create order"}},"/orders/{id}":{"get":{"summary":"Get order"}}}}', 'json', 'OpenApi', 'Approved', 'upload', false, '2025-05-15T14:00:00Z', 'dev@nextraceone.dev', '2025-05-15T14:00:00Z', 'dev@nextraceone.dev', false),
  -- Payments API (api2) — v2.0.0 → v2.1.0
  ('20000000-0000-0000-0000-000000000003', 'd0000000-0000-0000-0000-000000000002', '2.0.0', '{"openapi":"3.0.0","info":{"title":"Payments API","version":"2.0.0"},"paths":{"/payments":{"post":{"summary":"Process payment"}},"/payments/{id}":{"get":{"summary":"Get payment"}}}}', 'json', 'OpenApi', 'Approved', 'upload', true, '2025-04-01T09:00:00Z', 'dev@nextraceone.dev', '2025-04-01T09:00:00Z', 'dev@nextraceone.dev', false),
  ('20000000-0000-0000-0000-000000000004', 'd0000000-0000-0000-0000-000000000002', '2.1.0', '{"openapi":"3.0.0","info":{"title":"Payments API","version":"2.1.0"},"paths":{"/payments":{"post":{"summary":"Process payment"}},"/payments/{id}":{"get":{"summary":"Get payment"},"delete":{"summary":"Refund payment"}}}}', 'json', 'OpenApi', 'InReview', 'upload', false, '2025-05-20T16:00:00Z', 'dev@nextraceone.dev', '2025-05-20T16:00:00Z', 'dev@nextraceone.dev', false),
  -- Inventory API (api3) — v1.0.0
  ('20000000-0000-0000-0000-000000000005', 'd0000000-0000-0000-0000-000000000003', '1.0.0', '{"openapi":"3.0.0","info":{"title":"Inventory API","version":"1.0.0"},"paths":{"/inventory":{"get":{"summary":"List stock"}},"/inventory/{sku}":{"put":{"summary":"Update stock"}}}}', 'json', 'OpenApi', 'Approved', 'upload', true, '2025-03-20T11:00:00Z', 'admin@nextraceone.dev', '2025-03-20T11:00:00Z', 'admin@nextraceone.dev', false),
  -- Notifications API (api4) — v1.2.0 Draft
  ('20000000-0000-0000-0000-000000000006', 'd0000000-0000-0000-0000-000000000004', '1.2.0', '{"openapi":"3.0.0","info":{"title":"Notifications API","version":"1.2.0"},"paths":{"/notifications":{"post":{"summary":"Send notification"}},"/notifications/templates":{"get":{"summary":"List templates"}}}}', 'json', 'OpenApi', 'Draft', 'upload', false, '2025-06-01T08:00:00Z', 'dev@nextraceone.dev', '2025-06-01T08:00:00Z', 'dev@nextraceone.dev', false),
  -- Users API (api5) — v1.0.0 → v1.1.0
  ('20000000-0000-0000-0000-000000000007', 'd0000000-0000-0000-0000-000000000005', '1.0.0', '{"openapi":"3.0.0","info":{"title":"Users API","version":"1.0.0"},"paths":{"/users":{"get":{"summary":"List users"}},"/users/{id}":{"get":{"summary":"Get user"}}}}', 'json', 'OpenApi', 'Approved', 'upload', true, '2025-02-10T10:00:00Z', 'admin@nextraceone.dev', '2025-02-10T10:00:00Z', 'admin@nextraceone.dev', false),
  ('20000000-0000-0000-0000-000000000008', 'd0000000-0000-0000-0000-000000000005', '1.1.0', '{"openapi":"3.0.0","info":{"title":"Users API","version":"1.1.0"},"paths":{"/users":{"get":{"summary":"List users"},"post":{"summary":"Create user"}},"/users/{id}":{"get":{"summary":"Get user"},"put":{"summary":"Update user"},"delete":{"summary":"Delete user"}}}}', 'json', 'OpenApi', 'Approved', 'upload', false, '2025-05-28T11:00:00Z', 'ana.costa@nextraceone.dev', '2025-05-28T11:00:00Z', 'ana.costa@nextraceone.dev', false),
  -- Shipping API (api6) — v1.0.0
  ('20000000-0000-0000-0000-000000000009', 'd0000000-0000-0000-0000-000000000006', '1.0.0', '{"openapi":"3.0.0","info":{"title":"Shipping API","version":"1.0.0"},"paths":{"/shipments":{"get":{"summary":"List shipments"},"post":{"summary":"Create shipment"}},"/shipments/{id}/track":{"get":{"summary":"Track shipment"}}}}', 'json', 'OpenApi', 'Approved', 'upload', true, '2025-04-15T09:00:00Z', 'lucia.ferreira@nextraceone.dev', '2025-04-15T09:00:00Z', 'lucia.ferreira@nextraceone.dev', false),
  -- Analytics API (api7) — v1.0.0
  ('20000000-0000-0000-0000-000000000010', 'd0000000-0000-0000-0000-000000000007', '1.0.0', '{"openapi":"3.0.0","info":{"title":"Analytics API","version":"1.0.0"},"paths":{"/analytics/events":{"post":{"summary":"Track event"}},"/analytics/reports":{"get":{"summary":"Get reports"}},"/analytics/dashboards":{"get":{"summary":"List dashboards"}}}}', 'json', 'OpenApi', 'Approved', 'upload', true, '2025-04-20T14:00:00Z', 'rafael.lima@nextraceone.dev', '2025-04-20T14:00:00Z', 'rafael.lima@nextraceone.dev', false),
  -- Gateway API (api8) — v1.0.0 → v2.0.0 (breaking)
  ('20000000-0000-0000-0000-000000000011', 'd0000000-0000-0000-0000-000000000008', '1.0.0', '{"openapi":"3.0.0","info":{"title":"Gateway API","version":"1.0.0"},"paths":{"/gateway/routes":{"get":{"summary":"List routes"}},"/gateway/health":{"get":{"summary":"Health check"}}}}', 'json', 'OpenApi', 'Deprecated', 'upload', true, '2025-03-05T08:00:00Z', 'admin@nextraceone.dev', '2025-03-05T08:00:00Z', 'admin@nextraceone.dev', false),
  ('20000000-0000-0000-0000-000000000012', 'd0000000-0000-0000-0000-000000000008', '2.0.0', '{"openapi":"3.0.0","info":{"title":"Gateway API","version":"2.0.0"},"paths":{"/v2/gateway/routes":{"get":{"summary":"List routes v2"},"post":{"summary":"Register route"}},"/v2/gateway/health":{"get":{"summary":"Health check v2"}},"/v2/gateway/metrics":{"get":{"summary":"Gateway metrics"}}}}', 'json', 'OpenApi', 'InReview', 'upload', false, '2025-06-01T10:00:00Z', 'pedro.alves@nextraceone.dev', '2025-06-01T10:00:00Z', 'pedro.alves@nextraceone.dev', false),
  -- Search API (api9) — v1.0.0 → v1.2.0
  ('20000000-0000-0000-0000-000000000013', 'd0000000-0000-0000-0000-000000000009', '1.0.0', '{"openapi":"3.0.0","info":{"title":"Search API","version":"1.0.0"},"paths":{"/search":{"get":{"summary":"Full-text search"}}}}', 'json', 'OpenApi', 'Approved', 'upload', true, '2025-03-25T09:00:00Z', 'admin@nextraceone.dev', '2025-03-25T09:00:00Z', 'admin@nextraceone.dev', false),
  ('20000000-0000-0000-0000-000000000014', 'd0000000-0000-0000-0000-000000000009', '1.2.0', '{"openapi":"3.0.0","info":{"title":"Search API","version":"1.2.0"},"paths":{"/search":{"get":{"summary":"Full-text search"},"post":{"summary":"Advanced search"}},"/search/suggest":{"get":{"summary":"Search suggestions"}},"/search/facets":{"get":{"summary":"Search facets"}}}}', 'json', 'OpenApi', 'Approved', 'upload', false, '2025-05-25T11:00:00Z', 'ana.costa@nextraceone.dev', '2025-05-25T11:00:00Z', 'ana.costa@nextraceone.dev', false),
  -- Pricing API (api10) — v1.0.0 → v1.1.0
  ('20000000-0000-0000-0000-000000000015', 'd0000000-0000-0000-0000-000000000010', '1.0.0', '{"openapi":"3.0.0","info":{"title":"Pricing API","version":"1.0.0"},"paths":{"/pricing/rules":{"get":{"summary":"List pricing rules"}},"/pricing/calculate":{"post":{"summary":"Calculate price"}}}}', 'json', 'OpenApi', 'Approved', 'upload', true, '2025-04-10T10:00:00Z', 'dev@nextraceone.dev', '2025-04-10T10:00:00Z', 'dev@nextraceone.dev', false),
  ('20000000-0000-0000-0000-000000000016', 'd0000000-0000-0000-0000-000000000010', '1.1.0', '{"openapi":"3.0.0","info":{"title":"Pricing API","version":"1.1.0"},"paths":{"/pricing/rules":{"get":{"summary":"List pricing rules"},"post":{"summary":"Create pricing rule"}},"/pricing/calculate":{"post":{"summary":"Calculate price"}},"/pricing/history":{"get":{"summary":"Price history"}}}}', 'json', 'OpenApi', 'Draft', 'upload', false, '2025-06-02T09:00:00Z', 'rafael.lima@nextraceone.dev', '2025-06-02T09:00:00Z', 'rafael.lima@nextraceone.dev', false)
ON CONFLICT DO NOTHING;

-- Contract Diffs
INSERT INTO ct_contract_diffs ("Id", "ContractVersionId", "BaseVersionId", "TargetVersionId", "ApiAssetId", "Protocol", "ChangeLevel", "BreakingChanges", "NonBreakingChanges", "AdditiveChanges", "SuggestedSemVer", "Confidence", "ComputedAt")
VALUES
  -- Orders API v1.2.0 → v1.3.0 (Minor — additive)
  ('21000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000002', 'd0000000-0000-0000-0000-000000000001', 'OpenApi', 2, '[]', '[]', '[{"path":"/orders","changeType":"Added","isBreaking":false,"description":"Added POST /orders endpoint"},{"path":"/orders/{id}","changeType":"Added","isBreaking":false,"description":"Added GET /orders/{id} endpoint"}]', '1.3.0', 0.9500, '2025-05-15T14:30:00Z'),
  -- Payments API v2.0.0 → v2.1.0 (Minor — additive)
  ('21000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000004', '20000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000004', 'd0000000-0000-0000-0000-000000000002', 'OpenApi', 2, '[]', '[]', '[{"path":"/payments/{id}","changeType":"Added","isBreaking":false,"description":"Added DELETE /payments/{id} for refund"}]', '2.1.0', 0.9200, '2025-05-20T16:30:00Z'),
  -- Users API v1.0.0 → v1.1.0 (Minor — additive with modifications)
  ('21000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000008', '20000000-0000-0000-0000-000000000007', '20000000-0000-0000-0000-000000000008', 'd0000000-0000-0000-0000-000000000005', 'OpenApi', 2, '[]', '[{"path":"/users/{id}","changeType":"Modified","isBreaking":false,"description":"Added PUT and DELETE methods"}]', '[{"path":"/users","changeType":"Added","isBreaking":false,"description":"Added POST /users endpoint"}]', '1.1.0', 0.9400, '2025-05-28T11:30:00Z'),
  -- Gateway API v1.0.0 → v2.0.0 (Breaking — path restructure)
  ('21000000-0000-0000-0000-000000000004', '20000000-0000-0000-0000-000000000012', '20000000-0000-0000-0000-000000000011', '20000000-0000-0000-0000-000000000012', 'd0000000-0000-0000-0000-000000000008', 'OpenApi', 4, '[{"path":"/gateway/routes","changeType":"Removed","isBreaking":true,"description":"Removed /gateway/routes — replaced by /v2/gateway/routes"},{"path":"/gateway/health","changeType":"Removed","isBreaking":true,"description":"Removed /gateway/health — replaced by /v2/gateway/health"}]', '[]', '[{"path":"/v2/gateway/routes","changeType":"Added","isBreaking":false,"description":"New v2 routes endpoint with POST"},{"path":"/v2/gateway/metrics","changeType":"Added","isBreaking":false,"description":"Added metrics endpoint"}]', '2.0.0', 0.9800, '2025-06-01T10:30:00Z'),
  -- Search API v1.0.0 → v1.2.0 (Minor — additive)
  ('21000000-0000-0000-0000-000000000005', '20000000-0000-0000-0000-000000000014', '20000000-0000-0000-0000-000000000013', '20000000-0000-0000-0000-000000000014', 'd0000000-0000-0000-0000-000000000009', 'OpenApi', 2, '[]', '[]', '[{"path":"/search","changeType":"Added","isBreaking":false,"description":"Added POST /search for advanced search"},{"path":"/search/suggest","changeType":"Added","isBreaking":false,"description":"Added search suggestions"},{"path":"/search/facets","changeType":"Added","isBreaking":false,"description":"Added search facets"}]', '1.2.0', 0.9600, '2025-05-25T11:30:00Z'),
  -- Pricing API v1.0.0 → v1.1.0 (Minor — additive)
  ('21000000-0000-0000-0000-000000000006', '20000000-0000-0000-0000-000000000016', '20000000-0000-0000-0000-000000000015', '20000000-0000-0000-0000-000000000016', 'd0000000-0000-0000-0000-000000000010', 'OpenApi', 2, '[]', '[]', '[{"path":"/pricing/rules","changeType":"Added","isBreaking":false,"description":"Added POST /pricing/rules"},{"path":"/pricing/history","changeType":"Added","isBreaking":false,"description":"Added price history endpoint"}]', '1.1.0', 0.9100, '2025-06-02T09:30:00Z')
ON CONFLICT DO NOTHING;

-- ═══════════════════════════════════════════════════════════════════════════════
-- 5. CHANGE INTELLIGENCE MODULE — Releases, Blast Radius, Change Scores
-- ═══════════════════════════════════════════════════════════════════════════════

-- Releases (Status: 0=Pending, 1=Running, 2=Succeeded, 3=Failed, 4=RolledBack)
-- ChangeLevel: 0=None, 1=Patch, 2=Minor, 3=Major, 4=Breaking
INSERT INTO ci_releases ("Id", "ApiAssetId", "ServiceName", "Version", "Environment", "PipelineSource", "CommitSha", "ChangeLevel", "Status", "ChangeScore", "CreatedAt")
VALUES
  ('30000000-0000-0000-0000-000000000001', 'd0000000-0000-0000-0000-000000000001', 'Orders Service', '1.3.0', 'Production', 'https://ci.nextraceone.dev/pipelines/1001', 'abc123def456', 2, 2, 0.7200, '2025-05-15T15:00:00Z'),
  ('30000000-0000-0000-0000-000000000002', 'd0000000-0000-0000-0000-000000000002', 'Payments Service', '2.1.0', 'Staging', 'https://ci.nextraceone.dev/pipelines/1002', 'def456abc789', 2, 1, 0.6500, '2025-05-20T17:00:00Z'),
  ('30000000-0000-0000-0000-000000000003', 'd0000000-0000-0000-0000-000000000003', 'Inventory Service', '1.0.1', 'Production', 'https://ci.nextraceone.dev/pipelines/1003', '111222333444', 1, 2, 0.3000, '2025-05-10T09:00:00Z'),
  ('30000000-0000-0000-0000-000000000004', 'd0000000-0000-0000-0000-000000000004', 'Notifications Service', '1.2.0', 'Development', 'https://ci.nextraceone.dev/pipelines/1004', 'aaa111bbb222', 2, 0, 0.5500, '2025-06-01T08:30:00Z'),
  ('30000000-0000-0000-0000-000000000005', 'd0000000-0000-0000-0000-000000000001', 'Orders Service', '1.2.1', 'Production', 'https://ci.nextraceone.dev/pipelines/995', '01d111e1d222', 1, 2, 0.2000, '2025-04-20T12:00:00Z'),
  ('30000000-0000-0000-0000-000000000006', 'd0000000-0000-0000-0000-000000000005', 'Users Service', '1.1.0', 'Staging', 'https://ci.nextraceone.dev/pipelines/1005', 'ccc333ddd444', 3, 0, 0.8500, '2025-06-01T10:00:00Z'),
  ('30000000-0000-0000-0000-000000000007', 'd0000000-0000-0000-0000-000000000006', 'Shipping Service', '1.0.0', 'Production', 'https://ci.nextraceone.dev/pipelines/1006', 'eee555fff666', 2, 2, 0.4500, '2025-04-15T10:00:00Z'),
  ('30000000-0000-0000-0000-000000000008', 'd0000000-0000-0000-0000-000000000008', 'Gateway Service', '2.0.0', 'Staging', 'https://ci.nextraceone.dev/pipelines/1007', 'aab112ccf334', 4, 1, 0.9200, '2025-06-01T11:00:00Z'),
  ('30000000-0000-0000-0000-000000000009', 'd0000000-0000-0000-0000-000000000009', 'Search Service', '1.2.0', 'Production', 'https://ci.nextraceone.dev/pipelines/1008', 'bbc223dde445', 2, 2, 0.5000, '2025-05-25T12:00:00Z'),
  ('30000000-0000-0000-0000-000000000010', 'd0000000-0000-0000-0000-000000000010', 'Pricing Service', '1.1.0', 'Development', 'https://ci.nextraceone.dev/pipelines/1009', 'ccd334eef556', 2, 0, 0.4000, '2025-06-02T10:00:00Z'),
  ('30000000-0000-0000-0000-000000000011', 'd0000000-0000-0000-0000-000000000007', 'Analytics Service', '1.0.0', 'Production', 'https://ci.nextraceone.dev/pipelines/1010', 'dde445ffa667', 2, 2, 0.3500, '2025-04-20T15:00:00Z'),
  ('30000000-0000-0000-0000-000000000012', 'd0000000-0000-0000-0000-000000000001', 'Orders Service', '1.4.0', 'Development', 'https://ci.nextraceone.dev/pipelines/1011', 'eef556aab778', 2, 3, 0.6000, '2025-06-02T14:00:00Z')
ON CONFLICT DO NOTHING;

-- Blast Radius Reports
INSERT INTO ci_blast_radius_reports ("Id", "ReleaseId", "ApiAssetId", "TotalAffectedConsumers", "DirectConsumers", "TransitiveConsumers", "CalculatedAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  ('31000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', 'd0000000-0000-0000-0000-000000000001', 5, '["Payments Service","Inventory Service","Mobile App","Shipping Service","Admin Dashboard"]', '[]', '2025-05-15T15:10:00Z', '2025-05-15T15:10:00Z', 'system', '2025-05-15T15:10:00Z', 'system', false),
  ('31000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000002', 'd0000000-0000-0000-0000-000000000002', 3, '["Orders Service","Notifications Service","Mobile App"]', '[]', '2025-05-20T17:10:00Z', '2025-05-20T17:10:00Z', 'system', '2025-05-20T17:10:00Z', 'system', false),
  ('31000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000007', 'd0000000-0000-0000-0000-000000000006', 2, '["Orders Service","Mobile App"]', '[]', '2025-04-15T10:10:00Z', '2025-04-15T10:10:00Z', 'system', '2025-04-15T10:10:00Z', 'system', false),
  ('31000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000008', 'd0000000-0000-0000-0000-000000000008', 4, '["Users Service","Pricing Service"]', '["Orders Service","Mobile App"]', '2025-06-01T11:10:00Z', '2025-06-01T11:10:00Z', 'system', '2025-06-01T11:10:00Z', 'system', false),
  ('31000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-000000000009', 'd0000000-0000-0000-0000-000000000009', 2, '["Mobile App","Admin Dashboard"]', '[]', '2025-05-25T12:10:00Z', '2025-05-25T12:10:00Z', 'system', '2025-05-25T12:10:00Z', 'system', false),
  ('31000000-0000-0000-0000-000000000006', '30000000-0000-0000-0000-000000000006', 'd0000000-0000-0000-0000-000000000005', 1, '["Gateway Service"]', '[]', '2025-06-01T10:10:00Z', '2025-06-01T10:10:00Z', 'system', '2025-06-01T10:10:00Z', 'system', false)
ON CONFLICT DO NOTHING;

-- Change Scores
INSERT INTO ci_change_scores ("Id", "ReleaseId", "Score", "BreakingChangeWeight", "BlastRadiusWeight", "EnvironmentWeight", "ComputedAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  ('32000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', 0.7200, 0.0000, 0.4500, 0.2700, '2025-05-15T15:15:00Z', '2025-05-15T15:15:00Z', 'system', '2025-05-15T15:15:00Z', 'system', false),
  ('32000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000002', 0.6500, 0.0000, 0.3500, 0.3000, '2025-05-20T17:15:00Z', '2025-05-20T17:15:00Z', 'system', '2025-05-20T17:15:00Z', 'system', false),
  ('32000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000003', 0.3000, 0.0000, 0.1500, 0.1500, '2025-05-10T09:10:00Z', '2025-05-10T09:10:00Z', 'system', '2025-05-10T09:10:00Z', 'system', false),
  ('32000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000007', 0.4500, 0.0000, 0.2000, 0.2500, '2025-04-15T10:15:00Z', '2025-04-15T10:15:00Z', 'system', '2025-04-15T10:15:00Z', 'system', false),
  ('32000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-000000000008', 0.9200, 0.5000, 0.2500, 0.1700, '2025-06-01T11:15:00Z', '2025-06-01T11:15:00Z', 'system', '2025-06-01T11:15:00Z', 'system', false),
  ('32000000-0000-0000-0000-000000000006', '30000000-0000-0000-0000-000000000009', 0.5000, 0.0000, 0.2500, 0.2500, '2025-05-25T12:15:00Z', '2025-05-25T12:15:00Z', 'system', '2025-05-25T12:15:00Z', 'system', false),
  ('32000000-0000-0000-0000-000000000007', '30000000-0000-0000-0000-000000000006', 0.8500, 0.4000, 0.2000, 0.2500, '2025-06-01T10:15:00Z', '2025-06-01T10:15:00Z', 'system', '2025-06-01T10:15:00Z', 'system', false),
  ('32000000-0000-0000-0000-000000000008', '30000000-0000-0000-0000-000000000011', 0.3500, 0.0000, 0.1500, 0.2000, '2025-04-20T15:15:00Z', '2025-04-20T15:15:00Z', 'system', '2025-04-20T15:15:00Z', 'system', false)
ON CONFLICT DO NOTHING;

-- Change Events
INSERT INTO ci_change_events ("Id", "ReleaseId", "EventType", "Description", "OccurredAt", "Source", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  ('33000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', 'ContractChanged', 'New endpoints added to Orders API v1.3.0', '2025-05-15T14:30:00Z', 'ContractDiff', '2025-05-15T14:30:00Z', 'system', '2025-05-15T14:30:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000001', 'DeploymentStarted', 'Deployment to Production initiated', '2025-05-15T15:00:00Z', 'Pipeline', '2025-05-15T15:00:00Z', 'system', '2025-05-15T15:00:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000002', 'ContractChanged', 'Refund endpoint added to Payments API v2.1.0', '2025-05-20T16:30:00Z', 'ContractDiff', '2025-05-20T16:30:00Z', 'system', '2025-05-20T16:30:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000001', 'DeploymentCompleted', 'Deployment to Production completed successfully', '2025-05-15T15:45:00Z', 'Pipeline', '2025-05-15T15:45:00Z', 'system', '2025-05-15T15:45:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-000000000003', 'DeploymentStarted', 'Hotfix deployment to Production initiated', '2025-05-10T09:00:00Z', 'Pipeline', '2025-05-10T09:00:00Z', 'system', '2025-05-10T09:00:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000006', '30000000-0000-0000-0000-000000000003', 'DeploymentCompleted', 'Hotfix deployed to Production', '2025-05-10T09:20:00Z', 'Pipeline', '2025-05-10T09:20:00Z', 'system', '2025-05-10T09:20:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000007', '30000000-0000-0000-0000-000000000008', 'ContractChanged', 'Breaking changes in Gateway API v2.0.0 — path restructure', '2025-06-01T10:30:00Z', 'ContractDiff', '2025-06-01T10:30:00Z', 'system', '2025-06-01T10:30:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000008', '30000000-0000-0000-0000-000000000008', 'BlastRadiusCalculated', 'Blast radius: 4 affected consumers (2 direct, 2 transitive)', '2025-06-01T11:10:00Z', 'BlastRadiusEngine', '2025-06-01T11:10:00Z', 'system', '2025-06-01T11:10:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000009', '30000000-0000-0000-0000-000000000009', 'ContractChanged', 'Search suggestions and facets added to Search API v1.2.0', '2025-05-25T11:30:00Z', 'ContractDiff', '2025-05-25T11:30:00Z', 'system', '2025-05-25T11:30:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000010', '30000000-0000-0000-0000-000000000009', 'DeploymentCompleted', 'Search API v1.2.0 deployed to Production', '2025-05-25T12:30:00Z', 'Pipeline', '2025-05-25T12:30:00Z', 'system', '2025-05-25T12:30:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000011', '30000000-0000-0000-0000-000000000012', 'ContractChanged', 'New features for Orders API v1.4.0 in development', '2025-06-02T14:00:00Z', 'ContractDiff', '2025-06-02T14:00:00Z', 'system', '2025-06-02T14:00:00Z', 'system', false),
  ('33000000-0000-0000-0000-000000000012', '30000000-0000-0000-0000-000000000012', 'DeploymentStarted', 'Development deployment started but failed lint checks', '2025-06-02T14:10:00Z', 'Pipeline', '2025-06-02T14:10:00Z', 'system', '2025-06-02T14:10:00Z', 'system', false)
ON CONFLICT DO NOTHING;

-- ═══════════════════════════════════════════════════════════════════════════════
-- 6. RULESET GOVERNANCE MODULE — Rulesets and Bindings
-- ═══════════════════════════════════════════════════════════════════════════════

INSERT INTO rg_rulesets ("Id", "Name", "Description", "Content", "RulesetType", "IsActive", "RulesetCreatedAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  ('40000000-0000-0000-0000-000000000001', 'API Naming Convention', 'Enforces consistent naming for endpoints and schemas', '{"rules":[{"id":"naming-001","severity":"warning","message":"Endpoint path must use kebab-case"},{"id":"naming-002","severity":"error","message":"Schema names must use PascalCase"}]}', 0, true, '2025-02-01T00:00:00Z', '2025-02-01T00:00:00Z', 'admin@nextraceone.dev', '2025-02-01T00:00:00Z', 'admin@nextraceone.dev', false),
  ('40000000-0000-0000-0000-000000000002', 'Security Standards', 'Validates security schemes and authentication requirements', '{"rules":[{"id":"sec-001","severity":"error","message":"All endpoints must require authentication"},{"id":"sec-002","severity":"warning","message":"Sensitive data fields should use string format password"}]}', 0, true, '2025-02-15T00:00:00Z', '2025-02-15T00:00:00Z', 'admin@nextraceone.dev', '2025-02-15T00:00:00Z', 'admin@nextraceone.dev', false),
  ('40000000-0000-0000-0000-000000000003', 'Breaking Change Prevention', 'Prevents breaking changes in production APIs', '{"rules":[{"id":"brk-001","severity":"error","message":"Cannot remove existing endpoints"},{"id":"brk-002","severity":"error","message":"Cannot change response schema type"}]}', 0, true, '2025-03-01T00:00:00Z', '2025-03-01T00:00:00Z', 'techlead@nextraceone.dev', '2025-03-01T00:00:00Z', 'techlead@nextraceone.dev', false),
  ('40000000-0000-0000-0000-000000000004', 'Pagination Standards', 'Requires pagination parameters on collection endpoints', '{"rules":[{"id":"page-001","severity":"warning","message":"GET endpoints returning arrays must support limit/offset pagination"},{"id":"page-002","severity":"info","message":"Default page size should be between 10 and 100"}]}', 0, true, '2025-03-10T00:00:00Z', '2025-03-10T00:00:00Z', 'pedro.alves@nextraceone.dev', '2025-03-10T00:00:00Z', 'pedro.alves@nextraceone.dev', false),
  ('40000000-0000-0000-0000-000000000005', 'Error Response Format', 'Enforces consistent error response structure across APIs', '{"rules":[{"id":"err-001","severity":"error","message":"Error responses must include code, messageKey, and correlationId"},{"id":"err-002","severity":"warning","message":"4xx responses must include details array"}]}', 0, true, '2025-03-15T00:00:00Z', '2025-03-15T00:00:00Z', 'admin@nextraceone.dev', '2025-03-15T00:00:00Z', 'admin@nextraceone.dev', false),
  ('40000000-0000-0000-0000-000000000006', 'Versioning Policy', 'Requires proper API versioning in path or header', '{"rules":[{"id":"ver-001","severity":"error","message":"API path must include version prefix /api/v{n}/"},{"id":"ver-002","severity":"warning","message":"Deprecated endpoints must have sunset date header"}]}', 0, true, '2025-04-01T00:00:00Z', '2025-04-01T00:00:00Z', 'techlead@nextraceone.dev', '2025-04-01T00:00:00Z', 'techlead@nextraceone.dev', false),
  ('40000000-0000-0000-0000-000000000007', 'Rate Limiting Headers', 'Ensures rate limit headers are documented in all public APIs', '{"rules":[{"id":"rate-001","severity":"warning","message":"Public APIs must document X-RateLimit-Limit header"},{"id":"rate-002","severity":"info","message":"Consider documenting X-RateLimit-Remaining and X-RateLimit-Reset"}]}', 0, false, '2025-04-15T00:00:00Z', '2025-04-15T00:00:00Z', 'pedro.alves@nextraceone.dev', '2025-04-15T00:00:00Z', 'pedro.alves@nextraceone.dev', false)
ON CONFLICT DO NOTHING;

INSERT INTO rg_ruleset_bindings ("Id", "RulesetId", "AssetType", "BindingCreatedAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  ('41000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000001', 'ApiAsset', '2025-02-05T00:00:00Z', '2025-02-05T00:00:00Z', 'admin@nextraceone.dev', '2025-02-05T00:00:00Z', 'admin@nextraceone.dev', false),
  ('41000000-0000-0000-0000-000000000002', '40000000-0000-0000-0000-000000000002', 'ApiAsset', '2025-02-20T00:00:00Z', '2025-02-20T00:00:00Z', 'admin@nextraceone.dev', '2025-02-20T00:00:00Z', 'admin@nextraceone.dev', false),
  ('41000000-0000-0000-0000-000000000003', '40000000-0000-0000-0000-000000000003', 'ApiAsset', '2025-03-05T00:00:00Z', '2025-03-05T00:00:00Z', 'techlead@nextraceone.dev', '2025-03-05T00:00:00Z', 'techlead@nextraceone.dev', false),
  ('41000000-0000-0000-0000-000000000004', '40000000-0000-0000-0000-000000000004', 'ApiAsset', '2025-03-15T00:00:00Z', '2025-03-15T00:00:00Z', 'pedro.alves@nextraceone.dev', '2025-03-15T00:00:00Z', 'pedro.alves@nextraceone.dev', false),
  ('41000000-0000-0000-0000-000000000005', '40000000-0000-0000-0000-000000000005', 'ApiAsset', '2025-03-20T00:00:00Z', '2025-03-20T00:00:00Z', 'admin@nextraceone.dev', '2025-03-20T00:00:00Z', 'admin@nextraceone.dev', false),
  ('41000000-0000-0000-0000-000000000006', '40000000-0000-0000-0000-000000000006', 'ApiAsset', '2025-04-05T00:00:00Z', '2025-04-05T00:00:00Z', 'techlead@nextraceone.dev', '2025-04-05T00:00:00Z', 'techlead@nextraceone.dev', false),
  ('41000000-0000-0000-0000-000000000007', '40000000-0000-0000-0000-000000000003', 'ServiceAsset', '2025-03-10T00:00:00Z', '2025-03-10T00:00:00Z', 'admin@nextraceone.dev', '2025-03-10T00:00:00Z', 'admin@nextraceone.dev', false)
ON CONFLICT DO NOTHING;

-- ═══════════════════════════════════════════════════════════════════════════════
-- 7. WORKFLOW MODULE — Templates, Instances, Stages, Decisions, Evidence Packs
-- ═══════════════════════════════════════════════════════════════════════════════

-- Templates
INSERT INTO wf_workflow_templates ("Id", "Name", "Description", "ChangeType", "ApiCriticality", "TargetEnvironment", "MinimumApprovers", "IsActive", "CreatedAt")
VALUES
  ('50000000-0000-0000-0000-000000000001', 'Standard Release Workflow', 'Default workflow for minor changes to production APIs', 'Minor', 'High', 'Production', 2, true, '2025-02-01T00:00:00Z'),
  ('50000000-0000-0000-0000-000000000002', 'Fast-Track Patch Workflow', 'Streamlined workflow for patch-level fixes', 'Patch', 'Medium', 'Production', 1, true, '2025-02-15T00:00:00Z'),
  ('50000000-0000-0000-0000-000000000003', 'Breaking Change Workflow', 'Full governance for breaking changes requiring extended approval', 'Breaking', 'Critical', 'Production', 3, true, '2025-03-01T00:00:00Z'),
  ('50000000-0000-0000-0000-000000000004', 'Staging Promotion Workflow', 'Lightweight workflow for staging environment promotions', 'Minor', 'Medium', 'Staging', 1, true, '2025-03-15T00:00:00Z'),
  ('50000000-0000-0000-0000-000000000005', 'Internal API Change Workflow', 'Workflow for internal API changes with minimal oversight', 'Minor', 'Low', 'Production', 1, true, '2025-04-01T00:00:00Z')
ON CONFLICT DO NOTHING;

-- Workflow Instances
INSERT INTO wf_workflow_instances ("Id", "WorkflowTemplateId", "ReleaseId", "SubmittedBy", "Status", "CurrentStageIndex", "SubmittedAt", "CompletedAt")
VALUES
  -- Orders API v1.3.0 → Production (Approved)
  ('51000000-0000-0000-0000-000000000001', '50000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', 'dev@nextraceone.dev', 'Approved', 2, '2025-05-15T15:20:00Z', '2025-05-15T16:00:00Z'),
  -- Payments API v2.1.0 → Staging (InProgress)
  ('51000000-0000-0000-0000-000000000002', '50000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000002', 'dev@nextraceone.dev', 'InProgress', 1, '2025-05-20T17:20:00Z', NULL),
  -- Users API v1.1.0 → Staging (Pending — Breaking Change workflow)
  ('51000000-0000-0000-0000-000000000003', '50000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000006', 'ana.costa@nextraceone.dev', 'Pending', 0, '2025-06-01T10:30:00Z', NULL),
  -- Inventory hotfix v1.0.1 → Production (Approved via Fast-Track)
  ('51000000-0000-0000-0000-000000000004', '50000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000003', 'dev@nextraceone.dev', 'Approved', 1, '2025-05-10T09:05:00Z', '2025-05-10T09:25:00Z'),
  -- Gateway API v2.0.0 → Staging (InProgress — Breaking Change workflow)
  ('51000000-0000-0000-0000-000000000005', '50000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000008', 'pedro.alves@nextraceone.dev', 'InProgress', 1, '2025-06-01T11:30:00Z', NULL),
  -- Search API v1.2.0 → Production (Approved)
  ('51000000-0000-0000-0000-000000000006', '50000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000009', 'ana.costa@nextraceone.dev', 'Approved', 2, '2025-05-25T12:20:00Z', '2025-05-25T13:00:00Z'),
  -- Shipping API v1.0.0 → Production (Approved)
  ('51000000-0000-0000-0000-000000000007', '50000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-000000000007', 'lucia.ferreira@nextraceone.dev', 'Approved', 1, '2025-04-15T10:20:00Z', '2025-04-15T10:50:00Z'),
  -- Orders API v1.4.0 → Development (Rejected)
  ('51000000-0000-0000-0000-000000000008', '50000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000012', 'rafael.lima@nextraceone.dev', 'Rejected', 0, '2025-06-02T14:20:00Z', '2025-06-02T14:45:00Z')
ON CONFLICT DO NOTHING;

-- Workflow Stages
INSERT INTO wf_workflow_stages ("Id", "WorkflowInstanceId", "Name", "StageOrder", "Status", "RequiredApprovers", "CurrentApprovals", "CommentRequired", "StartedAt", "CompletedAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  -- Instance 1: Orders API v1.3.0 (Approved — both stages done)
  ('52000000-0000-0000-0000-000000000001', '51000000-0000-0000-0000-000000000001', 'Tech Lead Review', 0, 'Approved', 1, 1, true, '2025-05-15T15:20:00Z', '2025-05-15T15:35:00Z', '2025-05-15T15:20:00Z', 'system', '2025-05-15T15:35:00Z', 'system', false),
  ('52000000-0000-0000-0000-000000000002', '51000000-0000-0000-0000-000000000001', 'Security Review', 1, 'Approved', 1, 1, false, '2025-05-15T15:35:00Z', '2025-05-15T16:00:00Z', '2025-05-15T15:20:00Z', 'system', '2025-05-15T16:00:00Z', 'system', false),
  -- Instance 2: Payments API v2.1.0 (InProgress — stage 0 done, stage 1 pending)
  ('52000000-0000-0000-0000-000000000003', '51000000-0000-0000-0000-000000000002', 'Tech Lead Review', 0, 'Approved', 1, 1, true, '2025-05-20T17:20:00Z', '2025-05-20T17:45:00Z', '2025-05-20T17:20:00Z', 'system', '2025-05-20T17:45:00Z', 'system', false),
  ('52000000-0000-0000-0000-000000000004', '51000000-0000-0000-0000-000000000002', 'Security Review', 1, 'Pending', 1, 0, false, NULL, NULL, '2025-05-20T17:20:00Z', 'system', '2025-05-20T17:20:00Z', 'system', false),
  -- Instance 3: Users API v1.1.0 (Pending — all stages pending)
  ('52000000-0000-0000-0000-000000000005', '51000000-0000-0000-0000-000000000003', 'Architecture Review', 0, 'Pending', 2, 0, true, NULL, NULL, '2025-06-01T10:30:00Z', 'system', '2025-06-01T10:30:00Z', 'system', false),
  ('52000000-0000-0000-0000-000000000006', '51000000-0000-0000-0000-000000000003', 'Security Review', 1, 'Pending', 1, 0, false, NULL, NULL, '2025-06-01T10:30:00Z', 'system', '2025-06-01T10:30:00Z', 'system', false),
  ('52000000-0000-0000-0000-000000000007', '51000000-0000-0000-0000-000000000003', 'Final Approval', 2, 'Pending', 1, 0, true, NULL, NULL, '2025-06-01T10:30:00Z', 'system', '2025-06-01T10:30:00Z', 'system', false),
  -- Instance 4: Inventory hotfix (Approved — single stage via Fast-Track)
  ('52000000-0000-0000-0000-000000000008', '51000000-0000-0000-0000-000000000004', 'Quick Review', 0, 'Approved', 1, 1, false, '2025-05-10T09:05:00Z', '2025-05-10T09:25:00Z', '2025-05-10T09:05:00Z', 'system', '2025-05-10T09:25:00Z', 'system', false),
  -- Instance 5: Gateway API v2.0.0 (InProgress — stage 0 approved, stages 1-2 pending)
  ('52000000-0000-0000-0000-000000000009', '51000000-0000-0000-0000-000000000005', 'Architecture Review', 0, 'Approved', 2, 2, true, '2025-06-01T11:30:00Z', '2025-06-01T12:30:00Z', '2025-06-01T11:30:00Z', 'system', '2025-06-01T12:30:00Z', 'system', false),
  ('52000000-0000-0000-0000-000000000010', '51000000-0000-0000-0000-000000000005', 'Security Review', 1, 'Pending', 1, 0, true, NULL, NULL, '2025-06-01T11:30:00Z', 'system', '2025-06-01T11:30:00Z', 'system', false),
  ('52000000-0000-0000-0000-000000000011', '51000000-0000-0000-0000-000000000005', 'Final Approval', 2, 'Pending', 1, 0, true, NULL, NULL, '2025-06-01T11:30:00Z', 'system', '2025-06-01T11:30:00Z', 'system', false),
  -- Instance 6: Search API v1.2.0 (Approved — both stages done)
  ('52000000-0000-0000-0000-000000000012', '51000000-0000-0000-0000-000000000006', 'Tech Lead Review', 0, 'Approved', 1, 1, true, '2025-05-25T12:20:00Z', '2025-05-25T12:40:00Z', '2025-05-25T12:20:00Z', 'system', '2025-05-25T12:40:00Z', 'system', false),
  ('52000000-0000-0000-0000-000000000013', '51000000-0000-0000-0000-000000000006', 'Security Review', 1, 'Approved', 1, 1, false, '2025-05-25T12:40:00Z', '2025-05-25T13:00:00Z', '2025-05-25T12:20:00Z', 'system', '2025-05-25T13:00:00Z', 'system', false),
  -- Instance 7: Shipping API (Approved — single stage)
  ('52000000-0000-0000-0000-000000000014', '51000000-0000-0000-0000-000000000007', 'Tech Lead Review', 0, 'Approved', 1, 1, false, '2025-04-15T10:20:00Z', '2025-04-15T10:50:00Z', '2025-04-15T10:20:00Z', 'system', '2025-04-15T10:50:00Z', 'system', false),
  -- Instance 8: Orders API v1.4.0 (Rejected at first stage)
  ('52000000-0000-0000-0000-000000000015', '51000000-0000-0000-0000-000000000008', 'Tech Lead Review', 0, 'Rejected', 1, 0, true, '2025-06-02T14:20:00Z', '2025-06-02T14:45:00Z', '2025-06-02T14:20:00Z', 'system', '2025-06-02T14:45:00Z', 'system', false)
ON CONFLICT DO NOTHING;

-- Approval Decisions
INSERT INTO wf_approval_decisions ("Id", "WorkflowStageId", "WorkflowInstanceId", "DecidedBy", "Decision", "Comment", "DecidedAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  -- Instance 1: Orders API v1.3.0
  ('53000000-0000-0000-0000-000000000001', '52000000-0000-0000-0000-000000000001', '51000000-0000-0000-0000-000000000001', 'techlead@nextraceone.dev', 'Approved', 'New endpoints look well-designed. LGTM.', '2025-05-15T15:35:00Z', '2025-05-15T15:35:00Z', 'techlead@nextraceone.dev', '2025-05-15T15:35:00Z', 'techlead@nextraceone.dev', false),
  ('53000000-0000-0000-0000-000000000002', '52000000-0000-0000-0000-000000000002', '51000000-0000-0000-0000-000000000001', 'admin@nextraceone.dev', 'Approved', 'Security review passed.', '2025-05-15T16:00:00Z', '2025-05-15T16:00:00Z', 'admin@nextraceone.dev', '2025-05-15T16:00:00Z', 'admin@nextraceone.dev', false),
  -- Instance 2: Payments API v2.1.0
  ('53000000-0000-0000-0000-000000000003', '52000000-0000-0000-0000-000000000003', '51000000-0000-0000-0000-000000000002', 'techlead@nextraceone.dev', 'Approved', 'Refund endpoint approved.', '2025-05-20T17:45:00Z', '2025-05-20T17:45:00Z', 'techlead@nextraceone.dev', '2025-05-20T17:45:00Z', 'techlead@nextraceone.dev', false),
  -- Instance 4: Inventory hotfix
  ('53000000-0000-0000-0000-000000000004', '52000000-0000-0000-0000-000000000008', '51000000-0000-0000-0000-000000000004', 'techlead@nextraceone.dev', 'Approved', 'Critical hotfix approved via fast-track.', '2025-05-10T09:25:00Z', '2025-05-10T09:25:00Z', 'techlead@nextraceone.dev', '2025-05-10T09:25:00Z', 'techlead@nextraceone.dev', false),
  -- Instance 5: Gateway API v2.0.0 — Architecture Review (2 approvers)
  ('53000000-0000-0000-0000-000000000005', '52000000-0000-0000-0000-000000000009', '51000000-0000-0000-0000-000000000005', 'techlead@nextraceone.dev', 'Approved', 'Path restructure is necessary for v2 evolution.', '2025-06-01T12:00:00Z', '2025-06-01T12:00:00Z', 'techlead@nextraceone.dev', '2025-06-01T12:00:00Z', 'techlead@nextraceone.dev', false),
  ('53000000-0000-0000-0000-000000000006', '52000000-0000-0000-0000-000000000009', '51000000-0000-0000-0000-000000000005', 'pedro.alves@nextraceone.dev', 'Approved', 'Migration plan for consumers is adequate.', '2025-06-01T12:30:00Z', '2025-06-01T12:30:00Z', 'pedro.alves@nextraceone.dev', '2025-06-01T12:30:00Z', 'pedro.alves@nextraceone.dev', false),
  -- Instance 6: Search API v1.2.0
  ('53000000-0000-0000-0000-000000000007', '52000000-0000-0000-0000-000000000012', '51000000-0000-0000-0000-000000000006', 'pedro.alves@nextraceone.dev', 'Approved', 'Search improvements look solid.', '2025-05-25T12:40:00Z', '2025-05-25T12:40:00Z', 'pedro.alves@nextraceone.dev', '2025-05-25T12:40:00Z', 'pedro.alves@nextraceone.dev', false),
  ('53000000-0000-0000-0000-000000000008', '52000000-0000-0000-0000-000000000013', '51000000-0000-0000-0000-000000000006', 'admin@nextraceone.dev', 'Approved', 'No security concerns with additive changes.', '2025-05-25T13:00:00Z', '2025-05-25T13:00:00Z', 'admin@nextraceone.dev', '2025-05-25T13:00:00Z', 'admin@nextraceone.dev', false),
  -- Instance 7: Shipping API
  ('53000000-0000-0000-0000-000000000009', '52000000-0000-0000-0000-000000000014', '51000000-0000-0000-0000-000000000007', 'techlead@nextraceone.dev', 'Approved', 'Internal API approved for production.', '2025-04-15T10:50:00Z', '2025-04-15T10:50:00Z', 'techlead@nextraceone.dev', '2025-04-15T10:50:00Z', 'techlead@nextraceone.dev', false),
  -- Instance 8: Orders API v1.4.0 (Rejected)
  ('53000000-0000-0000-0000-000000000010', '52000000-0000-0000-0000-000000000015', '51000000-0000-0000-0000-000000000008', 'pedro.alves@nextraceone.dev', 'Rejected', 'Lint checks failed. Missing pagination on GET /orders/history. Fix and resubmit.', '2025-06-02T14:45:00Z', '2025-06-02T14:45:00Z', 'pedro.alves@nextraceone.dev', '2025-06-02T14:45:00Z', 'pedro.alves@nextraceone.dev', false)
ON CONFLICT DO NOTHING;

-- SLA Policies
INSERT INTO wf_sla_policies ("Id", "WorkflowTemplateId", "StageName", "MaxDurationHours", "EscalationEnabled", "EscalationTargetRole", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  ('54000000-0000-0000-0000-000000000001', '50000000-0000-0000-0000-000000000001', 'Tech Lead Review', 24, true, 'PlatformAdmin', '2025-02-01T00:00:00Z', 'admin@nextraceone.dev', '2025-02-01T00:00:00Z', 'admin@nextraceone.dev', false),
  ('54000000-0000-0000-0000-000000000002', '50000000-0000-0000-0000-000000000001', 'Security Review', 48, true, 'PlatformAdmin', '2025-02-01T00:00:00Z', 'admin@nextraceone.dev', '2025-02-01T00:00:00Z', 'admin@nextraceone.dev', false),
  ('54000000-0000-0000-0000-000000000003', '50000000-0000-0000-0000-000000000002', 'Quick Review', 4, true, 'TechLead', '2025-02-15T00:00:00Z', 'admin@nextraceone.dev', '2025-02-15T00:00:00Z', 'admin@nextraceone.dev', false),
  ('54000000-0000-0000-0000-000000000004', '50000000-0000-0000-0000-000000000003', 'Architecture Review', 72, true, 'PlatformAdmin', '2025-03-01T00:00:00Z', 'admin@nextraceone.dev', '2025-03-01T00:00:00Z', 'admin@nextraceone.dev', false),
  ('54000000-0000-0000-0000-000000000005', '50000000-0000-0000-0000-000000000003', 'Security Review', 48, true, 'PlatformAdmin', '2025-03-01T00:00:00Z', 'admin@nextraceone.dev', '2025-03-01T00:00:00Z', 'admin@nextraceone.dev', false),
  ('54000000-0000-0000-0000-000000000006', '50000000-0000-0000-0000-000000000003', 'Final Approval', 24, true, 'PlatformAdmin', '2025-03-01T00:00:00Z', 'admin@nextraceone.dev', '2025-03-01T00:00:00Z', 'admin@nextraceone.dev', false)
ON CONFLICT DO NOTHING;

-- Evidence Packs
INSERT INTO wf_evidence_packs ("Id", "WorkflowInstanceId", "ReleaseId", "ContractDiffSummary", "BlastRadiusScore", "SpectralScore", "ChangeIntelligenceScore", "ApprovalHistory", "ContractHash", "CompletenessPercentage", "GeneratedAt")
VALUES
  -- Orders API v1.3.0 (complete)
  ('55000000-0000-0000-0000-000000000001', '51000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', '2 additive changes: POST /orders, GET /orders/{id}', 0.4500, 0.9200, 0.7200, '[{"stage":"Tech Lead Review","decision":"Approved","by":"techlead@nextraceone.dev","at":"2025-05-15T15:35:00Z"},{"stage":"Security Review","decision":"Approved","by":"admin@nextraceone.dev","at":"2025-05-15T16:00:00Z"}]', 'sha256:ab12cd34ef56ab12cd34ef56ab12cd34ef56ab12cd34ef56ab12cd34ef56ab12', 100.00, '2025-05-15T16:05:00Z'),
  -- Inventory hotfix v1.0.1 (complete)
  ('55000000-0000-0000-0000-000000000002', '51000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000003', 'Patch: bug fix in stock calculation endpoint', 0.1500, 0.9800, 0.3000, '[{"stage":"Quick Review","decision":"Approved","by":"techlead@nextraceone.dev","at":"2025-05-10T09:25:00Z"}]', 'sha256:cd34ef56ab12cd34ef56ab12cd34ef56ab12cd34ef56ab12cd34ef56ab12cd34', 100.00, '2025-05-10T09:30:00Z'),
  -- Search API v1.2.0 (complete)
  ('55000000-0000-0000-0000-000000000003', '51000000-0000-0000-0000-000000000006', '30000000-0000-0000-0000-000000000009', '3 additive changes: POST /search, GET /search/suggest, GET /search/facets', 0.2500, 0.9500, 0.5000, '[{"stage":"Tech Lead Review","decision":"Approved","by":"pedro.alves@nextraceone.dev","at":"2025-05-25T12:40:00Z"},{"stage":"Security Review","decision":"Approved","by":"admin@nextraceone.dev","at":"2025-05-25T13:00:00Z"}]', 'sha256:ef56ab12cd34ef56ab12cd34ef56ab12cd34ef56ab12cd34ef56ab12cd34ef56', 100.00, '2025-05-25T13:05:00Z'),
  -- Shipping API v1.0.0 (complete)
  ('55000000-0000-0000-0000-000000000004', '51000000-0000-0000-0000-000000000007', '30000000-0000-0000-0000-000000000007', 'Initial version: 3 endpoints for shipment management', 0.2000, 0.8800, 0.4500, '[{"stage":"Tech Lead Review","decision":"Approved","by":"techlead@nextraceone.dev","at":"2025-04-15T10:50:00Z"}]', 'sha256:1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b', 100.00, '2025-04-15T10:55:00Z'),
  -- Gateway API v2.0.0 (partial — InProgress)
  ('55000000-0000-0000-0000-000000000005', '51000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-000000000008', '2 breaking changes: path restructure from /gateway/* to /v2/gateway/*; 2 additive changes', 0.6500, 0.7500, 0.9200, '[{"stage":"Architecture Review","decision":"Approved","by":"techlead@nextraceone.dev","at":"2025-06-01T12:00:00Z"},{"stage":"Architecture Review","decision":"Approved","by":"pedro.alves@nextraceone.dev","at":"2025-06-01T12:30:00Z"}]', 'sha256:2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c', 65.00, '2025-06-01T12:35:00Z')
ON CONFLICT DO NOTHING;

-- ═══════════════════════════════════════════════════════════════════════════════
-- 8. PROMOTION MODULE — Environments, Gates, Requests
-- ═══════════════════════════════════════════════════════════════════════════════

-- Deployment Environments
INSERT INTO prm_deployment_environments ("Id", "Name", "Description", "Order", "RequiresApproval", "RequiresEvidencePack", "IsActive", "CreatedAt")
VALUES
  ('60000000-0000-0000-0000-000000000001', 'Development', 'Development environment for initial testing', 1, false, false, true, '2025-01-01T00:00:00Z'),
  ('60000000-0000-0000-0000-000000000002', 'Staging', 'Pre-production environment with production-like config', 2, true, false, true, '2025-01-01T00:00:00Z'),
  ('60000000-0000-0000-0000-000000000003', 'Production', 'Live production environment', 3, true, true, true, '2025-01-01T00:00:00Z')
ON CONFLICT DO NOTHING;

-- Promotion Gates
INSERT INTO prm_promotion_gates ("Id", "DeploymentEnvironmentId", "GateName", "GateType", "IsRequired", "IsActive", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  ('61000000-0000-0000-0000-000000000001', '60000000-0000-0000-0000-000000000003', 'Workflow Approval', 'WorkflowApproval', true, true, '2025-01-01T00:00:00Z', 'admin@nextraceone.dev', '2025-01-01T00:00:00Z', 'admin@nextraceone.dev', false),
  ('61000000-0000-0000-0000-000000000002', '60000000-0000-0000-0000-000000000003', 'Contract Validation', 'ContractValidation', true, true, '2025-01-01T00:00:00Z', 'admin@nextraceone.dev', '2025-01-01T00:00:00Z', 'admin@nextraceone.dev', false),
  ('61000000-0000-0000-0000-000000000003', '60000000-0000-0000-0000-000000000003', 'Blast Radius Check', 'BlastRadius', false, true, '2025-01-01T00:00:00Z', 'admin@nextraceone.dev', '2025-01-01T00:00:00Z', 'admin@nextraceone.dev', false),
  ('61000000-0000-0000-0000-000000000004', '60000000-0000-0000-0000-000000000002', 'Workflow Approval', 'WorkflowApproval', true, true, '2025-01-01T00:00:00Z', 'admin@nextraceone.dev', '2025-01-01T00:00:00Z', 'admin@nextraceone.dev', false),
  ('61000000-0000-0000-0000-000000000005', '60000000-0000-0000-0000-000000000003', 'Evidence Pack Complete', 'EvidencePackComplete', true, true, '2025-02-01T00:00:00Z', 'admin@nextraceone.dev', '2025-02-01T00:00:00Z', 'admin@nextraceone.dev', false),
  ('61000000-0000-0000-0000-000000000006', '60000000-0000-0000-0000-000000000002', 'Contract Validation', 'ContractValidation', false, true, '2025-02-01T00:00:00Z', 'admin@nextraceone.dev', '2025-02-01T00:00:00Z', 'admin@nextraceone.dev', false)
ON CONFLICT DO NOTHING;

-- Promotion Requests
INSERT INTO prm_promotion_requests ("Id", "ReleaseId", "SourceEnvironmentId", "TargetEnvironmentId", "RequestedBy", "Status", "Justification", "RequestedAt", "CompletedAt")
VALUES
  -- Orders API v1.3.0 → Staging → Production (Approved)
  ('62000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', '60000000-0000-0000-0000-000000000002', '60000000-0000-0000-0000-000000000003', 'techlead@nextraceone.dev', 'Approved', 'Orders API v1.3.0 passed all staging tests', '2025-05-15T16:10:00Z', '2025-05-15T17:00:00Z'),
  -- Payments API v2.1.0 → Dev → Staging (Pending)
  ('62000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000002', '60000000-0000-0000-0000-000000000001', '60000000-0000-0000-0000-000000000002', 'dev@nextraceone.dev', 'Pending', 'Payments API v2.1.0 ready for staging validation', '2025-05-20T17:30:00Z', NULL),
  -- Inventory hotfix → Staging → Production (Promoted)
  ('62000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000003', '60000000-0000-0000-0000-000000000002', '60000000-0000-0000-0000-000000000003', 'techlead@nextraceone.dev', 'Promoted', 'Inventory hotfix approved for production', '2025-05-10T09:30:00Z', '2025-05-10T10:00:00Z'),
  -- Shipping API v1.0.0 → Staging → Production (Promoted)
  ('62000000-0000-0000-0000-000000000004', '30000000-0000-0000-0000-000000000007', '60000000-0000-0000-0000-000000000002', '60000000-0000-0000-0000-000000000003', 'techlead@nextraceone.dev', 'Promoted', 'Shipping API initial release passed all tests', '2025-04-15T11:00:00Z', '2025-04-15T11:30:00Z'),
  -- Gateway API v2.0.0 → Dev → Staging (Pending — awaiting workflow)
  ('62000000-0000-0000-0000-000000000005', '30000000-0000-0000-0000-000000000008', '60000000-0000-0000-0000-000000000001', '60000000-0000-0000-0000-000000000002', 'pedro.alves@nextraceone.dev', 'Pending', 'Gateway v2.0.0 with breaking changes needs staging validation', '2025-06-01T13:00:00Z', NULL),
  -- Search API v1.2.0 → Staging → Production (Promoted)
  ('62000000-0000-0000-0000-000000000006', '30000000-0000-0000-0000-000000000009', '60000000-0000-0000-0000-000000000002', '60000000-0000-0000-0000-000000000003', 'pedro.alves@nextraceone.dev', 'Promoted', 'Search API improvements validated in staging', '2025-05-25T13:10:00Z', '2025-05-25T14:00:00Z'),
  -- Orders API v1.4.0 → Dev → Staging (Rejected — lint failure)
  ('62000000-0000-0000-0000-000000000007', '30000000-0000-0000-0000-000000000012', '60000000-0000-0000-0000-000000000001', '60000000-0000-0000-0000-000000000002', 'rafael.lima@nextraceone.dev', 'Rejected', 'Lint checks failed — needs pagination on new endpoints', '2025-06-02T15:00:00Z', '2025-06-02T15:10:00Z')
ON CONFLICT DO NOTHING;

-- Gate Evaluations
INSERT INTO prm_gate_evaluations ("Id", "PromotionRequestId", "PromotionGateId", "Passed", "EvaluatedBy", "EvaluationDetails", "EvaluatedAt", "CreatedAt", "CreatedBy", "UpdatedAt", "UpdatedBy", "IsDeleted")
VALUES
  -- Orders API v1.3.0 → Production (all gates passed)
  ('63000000-0000-0000-0000-000000000001', '62000000-0000-0000-0000-000000000001', '61000000-0000-0000-0000-000000000001', true, 'system', 'Workflow instance 51000000-...-001 approved', '2025-05-15T16:15:00Z', '2025-05-15T16:15:00Z', 'system', '2025-05-15T16:15:00Z', 'system', false),
  ('63000000-0000-0000-0000-000000000002', '62000000-0000-0000-0000-000000000001', '61000000-0000-0000-0000-000000000002', true, 'system', 'Contract v1.3.0 validation passed, no breaking changes', '2025-05-15T16:16:00Z', '2025-05-15T16:16:00Z', 'system', '2025-05-15T16:16:00Z', 'system', false),
  ('63000000-0000-0000-0000-000000000003', '62000000-0000-0000-0000-000000000001', '61000000-0000-0000-0000-000000000003', true, 'system', 'Blast radius: 5 consumers, score within threshold', '2025-05-15T16:17:00Z', '2025-05-15T16:17:00Z', 'system', '2025-05-15T16:17:00Z', 'system', false),
  ('63000000-0000-0000-0000-000000000004', '62000000-0000-0000-0000-000000000001', '61000000-0000-0000-0000-000000000005', true, 'system', 'Evidence pack 100% complete', '2025-05-15T16:18:00Z', '2025-05-15T16:18:00Z', 'system', '2025-05-15T16:18:00Z', 'system', false),
  -- Inventory hotfix → Production (all gates passed)
  ('63000000-0000-0000-0000-000000000005', '62000000-0000-0000-0000-000000000003', '61000000-0000-0000-0000-000000000001', true, 'system', 'Workflow instance 51000000-...-004 approved via fast-track', '2025-05-10T09:35:00Z', '2025-05-10T09:35:00Z', 'system', '2025-05-10T09:35:00Z', 'system', false),
  ('63000000-0000-0000-0000-000000000006', '62000000-0000-0000-0000-000000000003', '61000000-0000-0000-0000-000000000002', true, 'system', 'Contract v1.0.1 validation passed, patch only', '2025-05-10T09:36:00Z', '2025-05-10T09:36:00Z', 'system', '2025-05-10T09:36:00Z', 'system', false),
  -- Shipping API → Production (all gates passed)
  ('63000000-0000-0000-0000-000000000007', '62000000-0000-0000-0000-000000000004', '61000000-0000-0000-0000-000000000001', true, 'system', 'Workflow instance 51000000-...-007 approved', '2025-04-15T11:05:00Z', '2025-04-15T11:05:00Z', 'system', '2025-04-15T11:05:00Z', 'system', false),
  ('63000000-0000-0000-0000-000000000008', '62000000-0000-0000-0000-000000000004', '61000000-0000-0000-0000-000000000002', true, 'system', 'Contract v1.0.0 validation passed', '2025-04-15T11:06:00Z', '2025-04-15T11:06:00Z', 'system', '2025-04-15T11:06:00Z', 'system', false),
  -- Search API → Production (all gates passed)
  ('63000000-0000-0000-0000-000000000009', '62000000-0000-0000-0000-000000000006', '61000000-0000-0000-0000-000000000001', true, 'system', 'Workflow instance 51000000-...-006 approved', '2025-05-25T13:15:00Z', '2025-05-25T13:15:00Z', 'system', '2025-05-25T13:15:00Z', 'system', false),
  ('63000000-0000-0000-0000-000000000010', '62000000-0000-0000-0000-000000000006', '61000000-0000-0000-0000-000000000002', true, 'system', 'Contract v1.2.0 validation passed', '2025-05-25T13:16:00Z', '2025-05-25T13:16:00Z', 'system', '2025-05-25T13:16:00Z', 'system', false),
  ('63000000-0000-0000-0000-000000000011', '62000000-0000-0000-0000-000000000006', '61000000-0000-0000-0000-000000000005', true, 'system', 'Evidence pack 100% complete', '2025-05-25T13:17:00Z', '2025-05-25T13:17:00Z', 'system', '2025-05-25T13:17:00Z', 'system', false),
  -- Orders API v1.4.0 → Staging (contract validation failed)
  ('63000000-0000-0000-0000-000000000012', '62000000-0000-0000-0000-000000000007', '61000000-0000-0000-0000-000000000004', false, 'system', 'Workflow instance 51000000-...-008 was rejected', '2025-06-02T15:05:00Z', '2025-06-02T15:05:00Z', 'system', '2025-06-02T15:05:00Z', 'system', false),
  ('63000000-0000-0000-0000-000000000013', '62000000-0000-0000-0000-000000000007', '61000000-0000-0000-0000-000000000006', false, 'system', 'Contract validation failed: missing pagination parameters', '2025-06-02T15:06:00Z', '2025-06-02T15:06:00Z', 'system', '2025-06-02T15:06:00Z', 'system', false)
ON CONFLICT DO NOTHING;

-- ═══════════════════════════════════════════════════════════════════════════════
-- 9. AUDIT MODULE — Audit Events and Chain Links
-- ═══════════════════════════════════════════════════════════════════════════════

INSERT INTO aud_audit_events ("Id", "SourceModule", "ActionType", "ResourceId", "ResourceType", "PerformedBy", "OccurredAt", "TenantId", "Payload")
VALUES
  ('70000000-0000-0000-0000-000000000001', 'Identity', 'UserCreated', 'b0000000-0000-0000-0000-000000000001', 'User', 'system', '2025-01-01T00:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"email":"admin@nextraceone.dev","role":"PlatformAdmin"}'),
  ('70000000-0000-0000-0000-000000000002', 'Identity', 'UserCreated', 'b0000000-0000-0000-0000-000000000002', 'User', 'admin@nextraceone.dev', '2025-01-01T00:01:00Z', 'a0000000-0000-0000-0000-000000000001', '{"email":"techlead@nextraceone.dev","role":"TechLead"}'),
  ('70000000-0000-0000-0000-000000000003', 'Contracts', 'ContractImported', '20000000-0000-0000-0000-000000000001', 'ContractVersion', 'admin@nextraceone.dev', '2025-03-01T10:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"apiAssetId":"d0000000-0000-0000-0000-000000000001","version":"1.2.0"}'),
  ('70000000-0000-0000-0000-000000000004', 'Contracts', 'ContractImported', '20000000-0000-0000-0000-000000000002', 'ContractVersion', 'dev@nextraceone.dev', '2025-05-15T14:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"apiAssetId":"d0000000-0000-0000-0000-000000000001","version":"1.3.0"}'),
  ('70000000-0000-0000-0000-000000000005', 'ChangeIntelligence', 'ReleaseCreated', '30000000-0000-0000-0000-000000000001', 'Release', 'dev@nextraceone.dev', '2025-05-15T15:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"service":"Orders Service","version":"1.3.0","environment":"Production"}'),
  ('70000000-0000-0000-0000-000000000006', 'Workflow', 'WorkflowApproved', '51000000-0000-0000-0000-000000000001', 'WorkflowInstance', 'techlead@nextraceone.dev', '2025-05-15T15:35:00Z', 'a0000000-0000-0000-0000-000000000001', '{"stage":"Tech Lead Review","decision":"Approved"}'),
  ('70000000-0000-0000-0000-000000000007', 'Promotion', 'PromotionApproved', '62000000-0000-0000-0000-000000000001', 'PromotionRequest', 'admin@nextraceone.dev', '2025-05-15T17:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"release":"Orders API v1.3.0","from":"Staging","to":"Production"}'),
  ('70000000-0000-0000-0000-000000000008', 'Identity', 'UserLogin', 'b0000000-0000-0000-0000-000000000001', 'User', 'admin@nextraceone.dev', '2025-06-01T10:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"ip":"127.0.0.1","method":"Local"}'),
  ('70000000-0000-0000-0000-000000000009', 'EngineeringGraph', 'ServiceRegistered', 'c0000000-0000-0000-0000-000000000001', 'ServiceAsset', 'admin@nextraceone.dev', '2025-02-01T10:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"name":"Orders Service","domain":"Commerce"}'),
  ('70000000-0000-0000-0000-000000000010', 'Licensing', 'LicenseActivated', '10000000-0000-0000-0000-000000000001', 'License', 'admin@nextraceone.dev', '2025-01-02T10:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"licenseKey":"NXTRC-ENT-2025-DEMO-KEY1","edition":"Enterprise"}'),
  -- Users u3-u8 created
  ('70000000-0000-0000-0000-000000000011', 'Identity', 'UserCreated', 'b0000000-0000-0000-0000-000000000003', 'User', 'admin@nextraceone.dev', '2025-01-15T00:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"email":"dev@nextraceone.dev","role":"Developer"}'),
  ('70000000-0000-0000-0000-000000000012', 'Identity', 'UserCreated', 'b0000000-0000-0000-0000-000000000004', 'User', 'admin@nextraceone.dev', '2025-02-01T00:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"email":"auditor@nextraceone.dev","role":"Auditor"}'),
  ('70000000-0000-0000-0000-000000000013', 'Identity', 'UserCreated', 'b0000000-0000-0000-0000-000000000005', 'User', 'admin@nextraceone.dev', '2025-03-01T00:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"email":"ana.costa@nextraceone.dev","role":"Developer"}'),
  ('70000000-0000-0000-0000-000000000014', 'Identity', 'UserCreated', 'b0000000-0000-0000-0000-000000000006', 'User', 'admin@nextraceone.dev', '2025-03-10T00:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"email":"pedro.alves@nextraceone.dev","role":"TechLead"}'),
  ('70000000-0000-0000-0000-000000000015', 'Identity', 'UserCreated', 'b0000000-0000-0000-0000-000000000007', 'User', 'admin@nextraceone.dev', '2025-03-15T00:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"email":"lucia.ferreira@nextraceone.dev","role":"Developer"}'),
  ('70000000-0000-0000-0000-000000000016', 'Identity', 'UserCreated', 'b0000000-0000-0000-0000-000000000008', 'User', 'admin@nextraceone.dev', '2025-04-01T00:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"email":"rafael.lima@nextraceone.dev","role":"Developer"}'),
  -- New services registered
  ('70000000-0000-0000-0000-000000000017', 'EngineeringGraph', 'ServiceRegistered', 'c0000000-0000-0000-0000-000000000002', 'ServiceAsset', 'admin@nextraceone.dev', '2025-02-01T10:30:00Z', 'a0000000-0000-0000-0000-000000000001', '{"name":"Payments Service","domain":"Finance"}'),
  ('70000000-0000-0000-0000-000000000018', 'EngineeringGraph', 'ServiceRegistered', 'c0000000-0000-0000-0000-000000000006', 'ServiceAsset', 'lucia.ferreira@nextraceone.dev', '2025-04-15T08:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"name":"Shipping Service","domain":"Logistics"}'),
  ('70000000-0000-0000-0000-000000000019', 'EngineeringGraph', 'ServiceRegistered', 'c0000000-0000-0000-0000-000000000008', 'ServiceAsset', 'pedro.alves@nextraceone.dev', '2025-03-05T07:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"name":"Gateway Service","domain":"Platform"}'),
  -- Contracts imported for new APIs
  ('70000000-0000-0000-0000-000000000020', 'Contracts', 'ContractImported', '20000000-0000-0000-0000-000000000009', 'ContractVersion', 'lucia.ferreira@nextraceone.dev', '2025-04-15T09:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"apiAssetId":"d0000000-0000-0000-0000-000000000006","version":"1.0.0"}'),
  ('70000000-0000-0000-0000-000000000021', 'Contracts', 'ContractImported', '20000000-0000-0000-0000-000000000012', 'ContractVersion', 'pedro.alves@nextraceone.dev', '2025-06-01T10:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"apiAssetId":"d0000000-0000-0000-0000-000000000008","version":"2.0.0"}'),
  ('70000000-0000-0000-0000-000000000022', 'Contracts', 'ContractImported', '20000000-0000-0000-0000-000000000014', 'ContractVersion', 'ana.costa@nextraceone.dev', '2025-05-25T11:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"apiAssetId":"d0000000-0000-0000-0000-000000000009","version":"1.2.0"}'),
  -- More release events
  ('70000000-0000-0000-0000-000000000023', 'ChangeIntelligence', 'ReleaseCreated', '30000000-0000-0000-0000-000000000007', 'Release', 'lucia.ferreira@nextraceone.dev', '2025-04-15T10:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"service":"Shipping Service","version":"1.0.0","environment":"Production"}'),
  ('70000000-0000-0000-0000-000000000024', 'ChangeIntelligence', 'ReleaseCreated', '30000000-0000-0000-0000-000000000008', 'Release', 'pedro.alves@nextraceone.dev', '2025-06-01T11:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"service":"Gateway Service","version":"2.0.0","environment":"Staging"}'),
  -- Workflow events
  ('70000000-0000-0000-0000-000000000025', 'Workflow', 'WorkflowSubmitted', '51000000-0000-0000-0000-000000000005', 'WorkflowInstance', 'pedro.alves@nextraceone.dev', '2025-06-01T11:30:00Z', 'a0000000-0000-0000-0000-000000000001', '{"template":"Breaking Change Workflow","release":"Gateway API v2.0.0"}'),
  ('70000000-0000-0000-0000-000000000026', 'Workflow', 'WorkflowRejected', '51000000-0000-0000-0000-000000000008', 'WorkflowInstance', 'pedro.alves@nextraceone.dev', '2025-06-02T14:45:00Z', 'a0000000-0000-0000-0000-000000000001', '{"stage":"Tech Lead Review","decision":"Rejected","reason":"Lint checks failed"}'),
  -- Promotion events
  ('70000000-0000-0000-0000-000000000027', 'Promotion', 'PromotionCompleted', '62000000-0000-0000-0000-000000000004', 'PromotionRequest', 'techlead@nextraceone.dev', '2025-04-15T11:30:00Z', 'a0000000-0000-0000-0000-000000000001', '{"release":"Shipping API v1.0.0","from":"Staging","to":"Production"}'),
  ('70000000-0000-0000-0000-000000000028', 'Promotion', 'PromotionRejected', '62000000-0000-0000-0000-000000000007', 'PromotionRequest', 'system', '2025-06-02T15:10:00Z', 'a0000000-0000-0000-0000-000000000001', '{"release":"Orders API v1.4.0","from":"Development","to":"Staging","reason":"Gate evaluation failed"}'),
  -- Logins by various users
  ('70000000-0000-0000-0000-000000000029', 'Identity', 'UserLogin', 'b0000000-0000-0000-0000-000000000002', 'User', 'techlead@nextraceone.dev', '2025-06-01T09:30:00Z', 'a0000000-0000-0000-0000-000000000001', '{"ip":"192.168.1.102","method":"Local"}'),
  ('70000000-0000-0000-0000-000000000030', 'Identity', 'UserLogin', 'b0000000-0000-0000-0000-000000000003', 'User', 'dev@nextraceone.dev', '2025-06-01T08:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"ip":"192.168.1.103","method":"Local"}'),
  -- Tenant 2 events
  ('70000000-0000-0000-0000-000000000031', 'Identity', 'UserCreated', 'b0000000-0000-0000-0000-000000000009', 'User', 'system', '2025-02-01T00:00:00Z', 'a0000000-0000-0000-0000-000000000002', '{"email":"camila.rocha@nextraceone.dev","role":"PlatformAdmin"}'),
  ('70000000-0000-0000-0000-000000000032', 'Identity', 'UserCreated', 'b0000000-0000-0000-0000-000000000010', 'User', 'camila.rocha@nextraceone.dev', '2025-02-15T00:00:00Z', 'a0000000-0000-0000-0000-000000000002', '{"email":"felipe.souza@nextraceone.dev","role":"Auditor"}'),
  -- Ruleset events
  ('70000000-0000-0000-0000-000000000033', 'RulesetGovernance', 'RulesetCreated', '40000000-0000-0000-0000-000000000004', 'Ruleset', 'pedro.alves@nextraceone.dev', '2025-03-10T00:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"name":"Pagination Standards","type":"Spectral"}'),
  ('70000000-0000-0000-0000-000000000034', 'RulesetGovernance', 'RulesetCreated', '40000000-0000-0000-0000-000000000005', 'Ruleset', 'admin@nextraceone.dev', '2025-03-15T00:00:00Z', 'a0000000-0000-0000-0000-000000000001', '{"name":"Error Response Format","type":"Spectral"}'),
  -- Security events
  ('70000000-0000-0000-0000-000000000035', 'Identity', 'SecurityEventDetected', 'fb000000-0000-0000-0000-000000000006', 'SecurityEvent', 'system', '2025-05-25T14:22:00Z', 'a0000000-0000-0000-0000-000000000001', '{"eventType":"BruteForceAttempt","riskScore":80,"ip":"185.220.101.34"}')
ON CONFLICT DO NOTHING;

-- Audit Chain Links
INSERT INTO aud_audit_chain_links ("Id", "SequenceNumber", "CurrentHash", "PreviousHash", "CreatedAt", "AuditEventId")
VALUES
  ('71000000-0000-0000-0000-000000000001', 1, 'sha256:a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2', 'sha256:0000000000000000000000000000000000000000000000000000000000000000', '2025-01-01T00:00:00Z', '70000000-0000-0000-0000-000000000001'),
  ('71000000-0000-0000-0000-000000000002', 2, 'sha256:b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3', 'sha256:a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2', '2025-01-01T00:01:00Z', '70000000-0000-0000-0000-000000000002'),
  ('71000000-0000-0000-0000-000000000003', 3, 'sha256:c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4', 'sha256:b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3', '2025-03-01T10:00:00Z', '70000000-0000-0000-0000-000000000003'),
  ('71000000-0000-0000-0000-000000000004', 4, 'sha256:d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5', 'sha256:c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4', '2025-05-15T14:00:00Z', '70000000-0000-0000-0000-000000000004'),
  ('71000000-0000-0000-0000-000000000005', 5, 'sha256:e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6', 'sha256:d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5', '2025-05-15T15:00:00Z', '70000000-0000-0000-0000-000000000005'),
  ('71000000-0000-0000-0000-000000000006', 6, 'sha256:f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1', 'sha256:e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6', '2025-05-15T15:35:00Z', '70000000-0000-0000-0000-000000000006'),
  ('71000000-0000-0000-0000-000000000007', 7, 'sha256:a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3', 'sha256:f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1', '2025-05-15T17:00:00Z', '70000000-0000-0000-0000-000000000007'),
  ('71000000-0000-0000-0000-000000000008', 8, 'sha256:b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4', 'sha256:a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3', '2025-06-01T10:00:00Z', '70000000-0000-0000-0000-000000000008'),
  ('71000000-0000-0000-0000-000000000009', 9, 'sha256:c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5', 'sha256:b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4', '2025-06-01T11:30:00Z', '70000000-0000-0000-0000-000000000025'),
  ('71000000-0000-0000-0000-000000000010', 10, 'sha256:d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6', 'sha256:c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5e6f1a2b3c4d5', '2025-06-02T14:45:00Z', '70000000-0000-0000-0000-000000000026')
ON CONFLICT DO NOTHING;

-- Retention Policies
INSERT INTO aud_retention_policies ("Id", "Name", "RetentionDays", "IsActive")
VALUES
  ('72000000-0000-0000-0000-000000000001', 'Standard Retention', 365, true),
  ('72000000-0000-0000-0000-000000000002', 'Compliance Retention', 2555, true),
  ('72000000-0000-0000-0000-000000000003', 'Short-Term Retention', 90, true)
ON CONFLICT DO NOTHING;

-- ═══════════════════════════════════════════════════════════════════════════════
-- 10. DEVELOPER PORTAL MODULE — Subscriptions, Analytics
-- ═══════════════════════════════════════════════════════════════════════════════

-- Subscriptions (Level: 0=BreakingChangesOnly, 1=AllChanges, 2=DeprecationNotices, 3=SecurityAdvisories)
-- Channel: 0=Email, 1=Webhook
INSERT INTO dp_subscriptions ("Id", "ApiAssetId", "ApiName", "SubscriberId", "SubscriberEmail", "ConsumerServiceName", "ConsumerServiceVersion", "Level", "Channel", "IsActive", "CreatedAt")
VALUES
  ('80000000-0000-0000-0000-000000000001', 'd0000000-0000-0000-0000-000000000001', 'Orders API', 'b0000000-0000-0000-0000-000000000002', 'techlead@nextraceone.dev', 'Payments Service', '2.0.0', 0, 0, true, '2025-03-01T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000002', 'd0000000-0000-0000-0000-000000000002', 'Payments API', 'b0000000-0000-0000-0000-000000000003', 'dev@nextraceone.dev', 'Orders Service', '1.3.0', 1, 0, true, '2025-04-01T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000003', 'd0000000-0000-0000-0000-000000000001', 'Orders API', 'b0000000-0000-0000-0000-000000000003', 'dev@nextraceone.dev', 'Mobile App', '3.0.0', 1, 0, true, '2025-03-15T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000004', 'd0000000-0000-0000-0000-000000000006', 'Shipping API', 'b0000000-0000-0000-0000-000000000007', 'lucia.ferreira@nextraceone.dev', 'Orders Service', '1.3.0', 1, 0, true, '2025-04-20T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000005', 'd0000000-0000-0000-0000-000000000008', 'Gateway API', 'b0000000-0000-0000-0000-000000000006', 'pedro.alves@nextraceone.dev', 'Users Service', '1.1.0', 0, 0, true, '2025-03-10T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000006', 'd0000000-0000-0000-0000-000000000009', 'Search API', 'b0000000-0000-0000-0000-000000000005', 'ana.costa@nextraceone.dev', 'Admin Dashboard', '1.0.0', 1, 0, true, '2025-05-20T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000007', 'd0000000-0000-0000-0000-000000000010', 'Pricing API', 'b0000000-0000-0000-0000-000000000008', 'rafael.lima@nextraceone.dev', 'Orders Service', '1.3.0', 1, 1, true, '2025-05-15T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000008', 'd0000000-0000-0000-0000-000000000005', 'Users API', 'b0000000-0000-0000-0000-000000000006', 'pedro.alves@nextraceone.dev', 'Gateway Service', '2.0.0', 0, 0, true, '2025-03-20T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000009', 'd0000000-0000-0000-0000-000000000003', 'Inventory API', 'b0000000-0000-0000-0000-000000000003', 'dev@nextraceone.dev', 'Orders Service', '1.3.0', 3, 0, true, '2025-03-25T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000010', 'd0000000-0000-0000-0000-000000000007', 'Analytics API', 'b0000000-0000-0000-0000-000000000001', 'admin@nextraceone.dev', 'Admin Dashboard', '1.0.0', 2, 1, true, '2025-04-25T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000011', 'd0000000-0000-0000-0000-000000000001', 'Orders API', 'b0000000-0000-0000-0000-000000000005', 'ana.costa@nextraceone.dev', 'Shipping Service', '1.0.0', 0, 0, true, '2025-05-01T00:00:00Z'),
  ('80000000-0000-0000-0000-000000000012', 'd0000000-0000-0000-0000-000000000002', 'Payments API', 'b0000000-0000-0000-0000-000000000006', 'pedro.alves@nextraceone.dev', 'Mobile App', '3.0.0', 1, 0, false, '2025-04-10T00:00:00Z')
ON CONFLICT DO NOTHING;

-- Portal Analytics Events
INSERT INTO dp_portal_analytics_events ("Id", "UserId", "EventType", "EntityId", "EntityType", "SearchQuery", "OccurredAt")
VALUES
  ('81000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000003', 'api_view', 'd0000000-0000-0000-0000-000000000001', 'ApiAsset', NULL, '2025-06-01T08:00:00Z'),
  ('81000000-0000-0000-0000-000000000002', 'b0000000-0000-0000-0000-000000000003', 'search', NULL, NULL, 'orders', '2025-06-01T07:55:00Z'),
  ('81000000-0000-0000-0000-000000000003', 'b0000000-0000-0000-0000-000000000002', 'api_view', 'd0000000-0000-0000-0000-000000000002', 'ApiAsset', NULL, '2025-05-31T14:00:00Z'),
  ('81000000-0000-0000-0000-000000000004', 'b0000000-0000-0000-0000-000000000002', 'search', NULL, NULL, 'payments', '2025-05-31T13:55:00Z'),
  ('81000000-0000-0000-0000-000000000005', 'b0000000-0000-0000-0000-000000000005', 'api_view', 'd0000000-0000-0000-0000-000000000009', 'ApiAsset', NULL, '2025-06-01T09:00:00Z'),
  ('81000000-0000-0000-0000-000000000006', 'b0000000-0000-0000-0000-000000000005', 'search', NULL, NULL, 'search', '2025-06-01T08:55:00Z'),
  ('81000000-0000-0000-0000-000000000007', 'b0000000-0000-0000-0000-000000000006', 'api_view', 'd0000000-0000-0000-0000-000000000008', 'ApiAsset', NULL, '2025-06-01T10:30:00Z'),
  ('81000000-0000-0000-0000-000000000008', 'b0000000-0000-0000-0000-000000000006', 'api_view', 'd0000000-0000-0000-0000-000000000005', 'ApiAsset', NULL, '2025-06-01T10:35:00Z'),
  ('81000000-0000-0000-0000-000000000009', 'b0000000-0000-0000-0000-000000000007', 'api_view', 'd0000000-0000-0000-0000-000000000006', 'ApiAsset', NULL, '2025-05-30T11:00:00Z'),
  ('81000000-0000-0000-0000-000000000010', 'b0000000-0000-0000-0000-000000000008', 'search', NULL, NULL, 'pricing rules', '2025-06-02T09:10:00Z'),
  ('81000000-0000-0000-0000-000000000011', 'b0000000-0000-0000-0000-000000000008', 'api_view', 'd0000000-0000-0000-0000-000000000010', 'ApiAsset', NULL, '2025-06-02T09:15:00Z'),
  ('81000000-0000-0000-0000-000000000012', 'b0000000-0000-0000-0000-000000000001', 'api_view', 'd0000000-0000-0000-0000-000000000007', 'ApiAsset', NULL, '2025-06-01T11:00:00Z'),
  ('81000000-0000-0000-0000-000000000013', 'b0000000-0000-0000-0000-000000000003', 'api_view', 'd0000000-0000-0000-0000-000000000003', 'ApiAsset', NULL, '2025-06-01T08:15:00Z'),
  ('81000000-0000-0000-0000-000000000014', 'b0000000-0000-0000-0000-000000000005', 'search', NULL, NULL, 'inventory stock', '2025-06-02T07:30:00Z'),
  ('81000000-0000-0000-0000-000000000015', 'b0000000-0000-0000-0000-000000000003', 'search', NULL, NULL, 'shipping tracking', '2025-06-02T08:00:00Z'),
  ('81000000-0000-0000-0000-000000000016', 'b0000000-0000-0000-0000-000000000001', 'search', NULL, NULL, 'gateway', '2025-06-01T10:45:00Z'),
  ('81000000-0000-0000-0000-000000000017', 'b0000000-0000-0000-0000-000000000006', 'api_view', 'd0000000-0000-0000-0000-000000000001', 'ApiAsset', NULL, '2025-06-02T09:00:00Z'),
  ('81000000-0000-0000-0000-000000000018', 'b0000000-0000-0000-0000-000000000004', 'search', NULL, NULL, 'audit trail', '2025-06-01T14:30:00Z'),
  ('81000000-0000-0000-0000-000000000019', 'b0000000-0000-0000-0000-000000000004', 'api_view', 'd0000000-0000-0000-0000-000000000004', 'ApiAsset', NULL, '2025-06-01T14:35:00Z'),
  ('81000000-0000-0000-0000-000000000020', 'b0000000-0000-0000-0000-000000000007', 'search', NULL, NULL, 'notifications templates', '2025-05-30T11:10:00Z')
ON CONFLICT DO NOTHING;
