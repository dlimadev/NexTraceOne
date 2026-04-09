-- ═══════════════════════════════════════════════════════════════════════════════
-- NexTraceOne — PostgreSQL Row-Level Security (RLS) Policies
--
-- Architecture:
--   All modules share a single PostgreSQL database (nextraceone).
--   The application sets `app.current_tenant_id` via TenantRlsInterceptor
--   before every SQL command using `set_config('app.current_tenant_id', ..., false)`.
--   These RLS policies enforce tenant isolation as a defence-in-depth layer,
--   complementing the application-side tenant middleware and MediatR pipeline.
--
-- When to apply:
--   Run AFTER all EF Core migrations have been applied (tables must exist).
--   Can be re-run safely (all statements are idempotent or use CREATE OR REPLACE).
--
-- Bypass rules:
--   - When `app.current_tenant_id` is NULL or empty (background workers, system seeding):
--     all rows are visible — this is intentional to allow maintenance operations.
--   - Superuser and the owner role (nextraceone application user) must NOT have
--     FORCE ROW LEVEL SECURITY; remove FORCE if the application runs as table owner.
--
-- Usage:
--   psql -U postgres -d nextraceone -f infra/postgres/apply-rls.sql
--   Or from bash scripts/db/apply-migrations.sh after migrations run.
--
-- Updated: 2026-04-04
-- ═══════════════════════════════════════════════════════════════════════════════

-- ── Helper: safe tenant ID lookup ────────────────────────────────────────────
-- Returns NULL when app.current_tenant_id is unset or empty.
-- This is used by every RLS policy expression for consistency.
CREATE OR REPLACE FUNCTION get_current_tenant_id()
RETURNS uuid
LANGUAGE plpgsql
STABLE
SECURITY INVOKER
AS $$
DECLARE
    v_raw text := current_setting('app.current_tenant_id', true);
BEGIN
    IF v_raw IS NULL OR v_raw = '' THEN
        RETURN NULL;
    END IF;
    RETURN v_raw::uuid;
END;
$$;

COMMENT ON FUNCTION get_current_tenant_id() IS
    'Returns the current tenant UUID from the session GUC app.current_tenant_id, '
    'or NULL when the context has not been set (background workers, system seeding).';

-- ── RLS helper macro (policy USING expression) ───────────────────────────────
-- Policy allows a row when:
--   a) The session has no tenant context (background worker / system operation), OR
--   b) The row's tenant_id matches the current session tenant.
-- This single expression is used for both USING (SELECT/UPDATE/DELETE) and
-- WITH CHECK (INSERT/UPDATE) to guarantee read=write symmetry.

-- ── Identity & Access module (iam_ / env_ prefix) ────────────────────────────

-- iam_tenant_memberships — tenant-scoped user memberships
ALTER TABLE iam_tenant_memberships ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON iam_tenant_memberships;
CREATE POLICY tenant_isolation ON iam_tenant_memberships
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- iam_user_role_assignments — role assignments per tenant
ALTER TABLE iam_user_role_assignments ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON iam_user_role_assignments;
CREATE POLICY tenant_isolation ON iam_user_role_assignments
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- iam_delegations — delegated access per tenant
ALTER TABLE iam_delegations ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON iam_delegations;
CREATE POLICY tenant_isolation ON iam_delegations
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- iam_security_events — security audit log per tenant
ALTER TABLE iam_security_events ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON iam_security_events;
CREATE POLICY tenant_isolation ON iam_security_events
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- iam_break_glass_requests — emergency access per tenant
ALTER TABLE iam_break_glass_requests ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON iam_break_glass_requests;
CREATE POLICY tenant_isolation ON iam_break_glass_requests
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- iam_jit_access_requests — just-in-time access per tenant
ALTER TABLE iam_jit_access_requests ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON iam_jit_access_requests;
CREATE POLICY tenant_isolation ON iam_jit_access_requests
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- iam_access_review_campaigns — access review campaigns per tenant
ALTER TABLE iam_access_review_campaigns ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON iam_access_review_campaigns;
CREATE POLICY tenant_isolation ON iam_access_review_campaigns
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- iam_access_review_items — items inside review campaigns
ALTER TABLE iam_access_review_items ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON iam_access_review_items;
CREATE POLICY tenant_isolation ON iam_access_review_items
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- iam_sso_group_mappings — SSO group mappings per tenant
ALTER TABLE iam_sso_group_mappings ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON iam_sso_group_mappings;
CREATE POLICY tenant_isolation ON iam_sso_group_mappings
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- iam_sessions — user sessions per tenant
ALTER TABLE iam_sessions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON iam_sessions;
CREATE POLICY tenant_isolation ON iam_sessions
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- env_environments — environments per tenant
ALTER TABLE env_environments ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON env_environments;
CREATE POLICY tenant_isolation ON env_environments
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- env_environment_accesses — environment-level access grants per tenant
ALTER TABLE env_environment_accesses ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON env_environment_accesses;
CREATE POLICY tenant_isolation ON env_environment_accesses
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ── Audit & Compliance module (aud_ prefix) ───────────────────────────────────

-- aud_audit_events — immutable audit trail; Payload encrypted via AES-256-GCM
ALTER TABLE aud_audit_events ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aud_audit_events;
CREATE POLICY tenant_isolation ON aud_audit_events
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aud_campaigns — audit campaigns per tenant
ALTER TABLE aud_campaigns ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aud_campaigns;
CREATE POLICY tenant_isolation ON aud_campaigns
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aud_compliance_policies — compliance policies per tenant
ALTER TABLE aud_compliance_policies ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aud_compliance_policies;
CREATE POLICY tenant_isolation ON aud_compliance_policies
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aud_compliance_results — compliance check results per tenant
ALTER TABLE aud_compliance_results ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aud_compliance_results;
CREATE POLICY tenant_isolation ON aud_compliance_results
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ── Governance module (gov_ prefix) ───────────────────────────────────────────

-- gov_domains — business domains per tenant
ALTER TABLE gov_domains ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_domains;
CREATE POLICY tenant_isolation ON gov_domains
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- gov_teams — teams per tenant
ALTER TABLE gov_teams ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_teams;
CREATE POLICY tenant_isolation ON gov_teams
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- gov_waivers — governance waivers per tenant
ALTER TABLE gov_waivers ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_waivers;
CREATE POLICY tenant_isolation ON gov_waivers
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- gov_packs — governance packs per tenant
ALTER TABLE gov_packs ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_packs;
CREATE POLICY tenant_isolation ON gov_packs
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- gov_evidence_packages — evidence packages per tenant
ALTER TABLE gov_evidence_packages ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_evidence_packages;
CREATE POLICY tenant_isolation ON gov_evidence_packages
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- gov_delegated_administrations — delegated admin grants per tenant
ALTER TABLE gov_delegated_administrations ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_delegated_administrations;
CREATE POLICY tenant_isolation ON gov_delegated_administrations
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- gov_custom_dashboards — custom dashboards per tenant
ALTER TABLE gov_custom_dashboards ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_custom_dashboards;
CREATE POLICY tenant_isolation ON gov_custom_dashboards
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- gov_technical_debt_items — technical debt items per tenant
ALTER TABLE gov_technical_debt_items ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_technical_debt_items;
CREATE POLICY tenant_isolation ON gov_technical_debt_items
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ── Change Governance module (chg_ prefix) ────────────────────────────────────

-- chg_change_records — change records per tenant
ALTER TABLE chg_change_records ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON chg_change_records;
CREATE POLICY tenant_isolation ON chg_change_records
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- chg_promotion_requests — promotion requests per tenant
ALTER TABLE chg_promotion_requests ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON chg_promotion_requests;
CREATE POLICY tenant_isolation ON chg_promotion_requests
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- chg_rulesets — change governance rulesets per tenant
ALTER TABLE chg_rulesets ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON chg_rulesets;
CREATE POLICY tenant_isolation ON chg_rulesets
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- chg_workflows — change approval workflows per tenant
ALTER TABLE chg_workflows ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON chg_workflows;
CREATE POLICY tenant_isolation ON chg_workflows
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ── Operational Intelligence module (ops_ prefix) ────────────────────────────

-- ops_incidents — incidents per tenant
ALTER TABLE ops_incidents ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_incidents;
CREATE POLICY tenant_isolation ON ops_incidents
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ops_incident_narratives — AI-generated incident narratives per tenant
ALTER TABLE ops_incident_narratives ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_incident_narratives;
CREATE POLICY tenant_isolation ON ops_incident_narratives
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ops_runbooks — operational runbooks per tenant
ALTER TABLE ops_runbooks ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_runbooks;
CREATE POLICY tenant_isolation ON ops_runbooks
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ops_slo_definitions — SLO definitions per tenant
ALTER TABLE ops_slo_definitions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_slo_definitions;
CREATE POLICY tenant_isolation ON ops_slo_definitions
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ops_sla_definitions — SLA definitions per tenant
ALTER TABLE ops_sla_definitions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_sla_definitions;
CREATE POLICY tenant_isolation ON ops_sla_definitions
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ops_cost_records — cost records per tenant
ALTER TABLE ops_cost_records ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_cost_records;
CREATE POLICY tenant_isolation ON ops_cost_records
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ops_automation_workflows — automation workflows per tenant
ALTER TABLE ops_automation_workflows ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_automation_workflows;
CREATE POLICY tenant_isolation ON ops_automation_workflows
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ── Service Catalog module (cat_ prefix) ──────────────────────────────────────

-- cat_discovered_services — discovered services per tenant
ALTER TABLE cat_discovered_services ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cat_discovered_services;
CREATE POLICY tenant_isolation ON cat_discovered_services
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ── Contracts module (ctr_ prefix) ────────────────────────────────────────────

-- ctr_api_contracts — API contract definitions per tenant
ALTER TABLE ctr_api_contracts ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_api_contracts;
CREATE POLICY tenant_isolation ON ctr_api_contracts
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ── Knowledge module (knw_ prefix) ────────────────────────────────────────────

-- knw_documents — knowledge documents per tenant
ALTER TABLE knw_documents ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON knw_documents;
CREATE POLICY tenant_isolation ON knw_documents
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- knw_operational_notes — operational notes per tenant
ALTER TABLE knw_operational_notes ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON knw_operational_notes;
CREATE POLICY tenant_isolation ON knw_operational_notes
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ── Notifications module (ntf_ prefix) ────────────────────────────────────────

-- ntf_notifications — notifications per tenant
ALTER TABLE ntf_notifications ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ntf_notifications;
CREATE POLICY tenant_isolation ON ntf_notifications
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ntf_preferences — notification preferences per tenant/user
ALTER TABLE ntf_preferences ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ntf_preferences;
CREATE POLICY tenant_isolation ON ntf_preferences
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ── AI & Knowledge module (aik_ prefix) ───────────────────────────────────────

-- aik_conversations — AI conversation history per tenant
ALTER TABLE aik_conversations ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_conversations;
CREATE POLICY tenant_isolation ON aik_conversations
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_knowledge_captures — captured knowledge per tenant
ALTER TABLE aik_knowledge_captures ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_knowledge_captures;
CREATE POLICY tenant_isolation ON aik_knowledge_captures
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_usage_entries — AI token usage per tenant (FinOps basis)
ALTER TABLE aik_usage_entries ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_usage_entries;
CREATE POLICY tenant_isolation ON aik_usage_entries
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_agents — AI agents per tenant
ALTER TABLE aik_agents ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_agents;
CREATE POLICY tenant_isolation ON aik_agents
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_policies — AI governance policies per tenant
ALTER TABLE aik_policies ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_policies;
CREATE POLICY tenant_isolation ON aik_policies
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_gov_feedbacks — AI feedback loop entries per tenant
ALTER TABLE aik_gov_feedbacks ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_gov_feedbacks;
CREATE POLICY tenant_isolation ON aik_gov_feedbacks
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ── Product Analytics module (pan_ prefix) ────────────────────────────────────

-- pan_analytics_events — analytics events per tenant
ALTER TABLE pan_analytics_events ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON pan_analytics_events;
CREATE POLICY tenant_isolation ON pan_analytics_events
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ── Integrations module (int_ prefix) ─────────────────────────────────────────

-- int_connectors — integration connectors per tenant
ALTER TABLE int_connectors ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON int_connectors;
CREATE POLICY tenant_isolation ON int_connectors
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- int_ingestion_sources — ingestion sources per tenant
ALTER TABLE int_ingestion_sources ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON int_ingestion_sources;
CREATE POLICY tenant_isolation ON int_ingestion_sources
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- int_webhook_subscriptions — webhook subscriptions per tenant
ALTER TABLE int_webhook_subscriptions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON int_webhook_subscriptions;
CREATE POLICY tenant_isolation ON int_webhook_subscriptions
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ── Configuration module (cfg_ prefix) ───────────────────────────────────────
-- Note: cfg_entries, cfg_definitions, cfg_modules use scope-based isolation
-- (not tenant_id column), so RLS is not applicable to those tables.

-- cfg_user_watches — user watch lists per tenant
ALTER TABLE cfg_user_watches ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cfg_user_watches;
CREATE POLICY tenant_isolation ON cfg_user_watches
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cfg_user_alert_rules — user alert rules per tenant
ALTER TABLE cfg_user_alert_rules ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cfg_user_alert_rules;
CREATE POLICY tenant_isolation ON cfg_user_alert_rules
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cfg_entity_tags — entity tags per tenant
ALTER TABLE cfg_entity_tags ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cfg_entity_tags;
CREATE POLICY tenant_isolation ON cfg_entity_tags
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cfg_service_custom_fields — custom fields per tenant
ALTER TABLE cfg_service_custom_fields ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cfg_service_custom_fields;
CREATE POLICY tenant_isolation ON cfg_service_custom_fields
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cfg_taxonomy_categories — taxonomy categories per tenant
ALTER TABLE cfg_taxonomy_categories ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cfg_taxonomy_categories;
CREATE POLICY tenant_isolation ON cfg_taxonomy_categories
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cfg_taxonomy_values — taxonomy values per tenant
ALTER TABLE cfg_taxonomy_values ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cfg_taxonomy_values;
CREATE POLICY tenant_isolation ON cfg_taxonomy_values
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cfg_automation_rules — automation rules per tenant
ALTER TABLE cfg_automation_rules ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cfg_automation_rules;
CREATE POLICY tenant_isolation ON cfg_automation_rules
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cfg_change_checklists — change checklists per tenant
ALTER TABLE cfg_change_checklists ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cfg_change_checklists;
CREATE POLICY tenant_isolation ON cfg_change_checklists
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cfg_contract_templates — contract templates per tenant
ALTER TABLE cfg_contract_templates ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cfg_contract_templates;
CREATE POLICY tenant_isolation ON cfg_contract_templates
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cfg_scheduled_reports — scheduled reports per tenant
ALTER TABLE cfg_scheduled_reports ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cfg_scheduled_reports;
CREATE POLICY tenant_isolation ON cfg_scheduled_reports
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cfg_saved_prompts — saved prompts per tenant
ALTER TABLE cfg_saved_prompts ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cfg_saved_prompts;
CREATE POLICY tenant_isolation ON cfg_saved_prompts
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cfg_webhook_templates — webhook templates per tenant
ALTER TABLE cfg_webhook_templates ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cfg_webhook_templates;
CREATE POLICY tenant_isolation ON cfg_webhook_templates
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cfg_user_saved_views — user saved views per tenant
ALTER TABLE cfg_user_saved_views ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cfg_user_saved_views;
CREATE POLICY tenant_isolation ON cfg_user_saved_views
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cfg_user_bookmarks — user bookmarks per tenant
ALTER TABLE cfg_user_bookmarks ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cfg_user_bookmarks;
CREATE POLICY tenant_isolation ON cfg_user_bookmarks
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- ── Operational Intelligence: Runtime ────────────────────────────────────────

-- ops_custom_charts — user-defined custom charts per tenant
ALTER TABLE ops_custom_charts ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_custom_charts;
CREATE POLICY tenant_isolation ON ops_custom_charts
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- ops_chaos_experiments — chaos engineering experiments per tenant
ALTER TABLE ops_chaos_experiments ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_chaos_experiments;
CREATE POLICY tenant_isolation ON ops_chaos_experiments
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cat_contract_health_scores — contract health scores per tenant
ALTER TABLE cat_contract_health_scores ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cat_contract_health_scores;
CREATE POLICY tenant_isolation ON cat_contract_health_scores
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- chg_change_confidence_events — change confidence timeline events per tenant
ALTER TABLE chg_change_confidence_events ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON chg_change_confidence_events;
CREATE POLICY tenant_isolation ON chg_change_confidence_events
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- chg_release_notes — AI-generated release notes per tenant
ALTER TABLE chg_release_notes ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON chg_release_notes;
CREATE POLICY tenant_isolation ON chg_release_notes
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- ops_anomaly_narratives — AI-generated anomaly narratives per tenant
ALTER TABLE ops_anomaly_narratives ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_anomaly_narratives;
CREATE POLICY tenant_isolation ON ops_anomaly_narratives
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- ops_environment_drift_reports — Environment drift reports per tenant
ALTER TABLE ops_environment_drift_reports ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_environment_drift_reports;
CREATE POLICY tenant_isolation ON ops_environment_drift_reports
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- gov_service_maturity_assessments — Service maturity assessments per tenant
ALTER TABLE gov_service_maturity_assessments ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_service_maturity_assessments;
CREATE POLICY tenant_isolation ON gov_service_maturity_assessments
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- knw_knowledge_graph_snapshots — Knowledge graph snapshots per tenant
ALTER TABLE knw_knowledge_graph_snapshots ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON knw_knowledge_graph_snapshots;
CREATE POLICY tenant_isolation ON knw_knowledge_graph_snapshots
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- ops_reliability_incident_prediction_patterns — Incident prediction patterns per tenant
ALTER TABLE ops_reliability_incident_prediction_patterns ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_reliability_incident_prediction_patterns;
CREATE POLICY tenant_isolation ON ops_reliability_incident_prediction_patterns
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- ── Wave D: Developer Experience (cat_ and ai_ prefixes) ─────────────────────

-- cat_pipeline_executions — Contract-to-code pipeline executions per tenant
ALTER TABLE cat_pipeline_executions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cat_pipeline_executions;
CREATE POLICY tenant_isolation ON cat_pipeline_executions
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cat_contract_negotiations — Cross-team contract negotiations per tenant
ALTER TABLE cat_contract_negotiations ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cat_contract_negotiations;
CREATE POLICY tenant_isolation ON cat_contract_negotiations
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cat_negotiation_comments — Negotiation comments per tenant
ALTER TABLE cat_negotiation_comments ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cat_negotiation_comments;
CREATE POLICY tenant_isolation ON cat_negotiation_comments
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- ai_onboarding_sessions — AI-powered onboarding sessions per tenant
ALTER TABLE ai_onboarding_sessions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ai_onboarding_sessions;
CREATE POLICY tenant_isolation ON ai_onboarding_sessions
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- ai_ide_query_sessions — IDE pair programming query sessions per tenant
ALTER TABLE ai_ide_query_sessions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ai_ide_query_sessions;
CREATE POLICY tenant_isolation ON ai_ide_query_sessions
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- ════════════════════════════════════════════════════════════════════════════════
-- SUMMARY:
--   RLS enabled on 81 tables covering all major tenant-aware data domains.
--   Remaining tables (system-level: iam_tenants, iam_roles, iam_permissions,
--   system cfg definitions, aud_chain_links) intentionally excluded — they store
--   global/system data not scoped to a single tenant.
--
-- TO ADD MORE TABLES:
--   1. Verify the entity has a `tenant_id uuid NOT NULL` column.
--   2. Add: ALTER TABLE <table> ENABLE ROW LEVEL SECURITY;
--   3. Add: DROP POLICY IF EXISTS tenant_isolation ON <table>;
--   4. Add: CREATE POLICY tenant_isolation ON <table>
--              USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
--              WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());
-- ════════════════════════════════════════════════════════════════════════════════
