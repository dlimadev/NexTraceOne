-- ═══════════════════════════════════════════════════════════════════════════════
-- NEXTRACEONE — Seed data: Licensing Module (nextraceone_licensing)
-- Tabelas: licensing_licenses, licensing_capabilities, licensing_activations
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
