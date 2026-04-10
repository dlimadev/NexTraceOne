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

-- chg_change_events — change events per tenant (was chg_change_records — phantom corrected rev.7)
ALTER TABLE chg_change_events ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON chg_change_events;
CREATE POLICY tenant_isolation ON chg_change_events
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

-- chg_releases — releases per tenant (was chg_workflows — phantom corrected rev.7)
ALTER TABLE chg_releases ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON chg_releases;
CREATE POLICY tenant_isolation ON chg_releases
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

-- ctr_contract_versions — contract versions per tenant (was ctr_api_contracts — phantom corrected rev.7)
ALTER TABLE ctr_contract_versions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_contract_versions;
CREATE POLICY tenant_isolation ON ctr_contract_versions
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

-- ── Wave E: Governance & Reliability (ops_ and cat_ prefixes) ────────────────

-- ops_reliability_healing_recommendations — Self-healing recommendations per tenant
ALTER TABLE ops_reliability_healing_recommendations ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_reliability_healing_recommendations;
CREATE POLICY tenant_isolation ON ops_reliability_healing_recommendations
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cat_schema_evolution_advices — Schema evolution advisor reports per tenant
ALTER TABLE cat_schema_evolution_advices ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cat_schema_evolution_advices;
CREATE POLICY tenant_isolation ON cat_schema_evolution_advices
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- ops_operational_playbooks — Operational playbook definitions per tenant
ALTER TABLE ops_operational_playbooks ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_operational_playbooks;
CREATE POLICY tenant_isolation ON ops_operational_playbooks
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- ops_playbook_executions — Playbook execution records per tenant
ALTER TABLE ops_playbook_executions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_playbook_executions;
CREATE POLICY tenant_isolation ON ops_playbook_executions
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- ops_resilience_reports — Chaos engineering resilience reports per tenant
ALTER TABLE ops_resilience_reports ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_resilience_reports;
CREATE POLICY tenant_isolation ON ops_resilience_reports
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- ── Wave F: Executive & FinOps (gov_ prefix) ────────────────────────────────

-- gov_team_health_snapshots — Team health dashboard snapshots per tenant
ALTER TABLE gov_team_health_snapshots ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_team_health_snapshots;
CREATE POLICY tenant_isolation ON gov_team_health_snapshots
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- gov_change_cost_impacts — FinOps cost impact per change per tenant
ALTER TABLE gov_change_cost_impacts ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_change_cost_impacts;
CREATE POLICY tenant_isolation ON gov_change_cost_impacts
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- gov_executive_briefings — Executive briefings per tenant
ALTER TABLE gov_executive_briefings ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_executive_briefings;
CREATE POLICY tenant_isolation ON gov_executive_briefings
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- gov_cost_attributions — Operational cost attribution per tenant
ALTER TABLE gov_cost_attributions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_cost_attributions;
CREATE POLICY tenant_isolation ON gov_cost_attributions
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- ── Wave G: Visualization & Marketplace (Ideas 4, 5, 9, 11, 21, 22, 23) ─────

-- cat_semantic_diff_results — Semantic diff results per tenant (Idea 5)
ALTER TABLE cat_semantic_diff_results ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cat_semantic_diff_results;
CREATE POLICY tenant_isolation ON cat_semantic_diff_results
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cat_contract_compliance_gates — Contract compliance gates per tenant (Idea 22)
ALTER TABLE cat_contract_compliance_gates ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cat_contract_compliance_gates;
CREATE POLICY tenant_isolation ON cat_contract_compliance_gates
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cat_contract_compliance_results — Compliance evaluation results per tenant (Idea 22)
ALTER TABLE cat_contract_compliance_results ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cat_contract_compliance_results;
CREATE POLICY tenant_isolation ON cat_contract_compliance_results
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- chg_blast_radius_reports — Blast radius visualization reports per tenant (Idea 4)
ALTER TABLE chg_blast_radius_reports ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON chg_blast_radius_reports;
CREATE POLICY tenant_isolation ON chg_blast_radius_reports
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- chg_promotion_gates — Smart promotion gates per tenant (Idea 9)
ALTER TABLE chg_promotion_gates ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON chg_promotion_gates;
CREATE POLICY tenant_isolation ON chg_promotion_gates
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- chg_promotion_gate_evaluations — Gate evaluation results per tenant (Idea 9)
ALTER TABLE chg_promotion_gate_evaluations ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON chg_promotion_gate_evaluations;
CREATE POLICY tenant_isolation ON chg_promotion_gate_evaluations
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cat_contract_listings — Contract marketplace listings per tenant (Idea 11)
ALTER TABLE cat_contract_listings ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cat_contract_listings;
CREATE POLICY tenant_isolation ON cat_contract_listings
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cat_contract_reviews — Contract marketplace reviews per tenant (Idea 11)
ALTER TABLE cat_contract_reviews ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cat_contract_reviews;
CREATE POLICY tenant_isolation ON cat_contract_reviews
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- cat_impact_simulations — Dependency impact simulations per tenant (Idea 21)
ALTER TABLE cat_impact_simulations ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cat_impact_simulations;
CREATE POLICY tenant_isolation ON cat_impact_simulations
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- gov_license_compliance_reports — License compliance reports per tenant (Idea 23)
ALTER TABLE gov_license_compliance_reports ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_license_compliance_reports;
CREATE POLICY tenant_isolation ON gov_license_compliance_reports
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text)
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id()::text);

-- ── Validation Plan rev.7 — RLS gap closure (86 additional tables) ──────────
-- Added 2026-04-10: all remaining tenant-scoped tables that were missing RLS.
-- Tables verified to have tenant_id column via DbContext entity configurations.
-- NOTE: iam_ system tables (tenants, users, roles, permissions, external_identities,
--       role_permissions, module_access_policies) intentionally excluded — they use
--       nullable TenantId for system-level defaults and tenant-specific overrides.

-- ── AIKnowledge module — additional aik_ tables ────────────────────────────

-- aik_access_policies — access policies per tenant
ALTER TABLE aik_access_policies ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_access_policies;
CREATE POLICY tenant_isolation ON aik_access_policies
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_agent_artifacts — agent artifacts per tenant
ALTER TABLE aik_agent_artifacts ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_agent_artifacts;
CREATE POLICY tenant_isolation ON aik_agent_artifacts
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_agent_executions — agent executions per tenant
ALTER TABLE aik_agent_executions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_agent_executions;
CREATE POLICY tenant_isolation ON aik_agent_executions
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_budgets — budgets per tenant
ALTER TABLE aik_budgets ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_budgets;
CREATE POLICY tenant_isolation ON aik_budgets
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_evaluations — evaluations per tenant
ALTER TABLE aik_evaluations ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_evaluations;
CREATE POLICY tenant_isolation ON aik_evaluations
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_external_inference_records — external inference records per tenant
ALTER TABLE aik_external_inference_records ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_external_inference_records;
CREATE POLICY tenant_isolation ON aik_external_inference_records
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_guardrails — guardrails per tenant
ALTER TABLE aik_guardrails ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_guardrails;
CREATE POLICY tenant_isolation ON aik_guardrails
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_ide_capability_policies — ide capability policies per tenant
ALTER TABLE aik_ide_capability_policies ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_ide_capability_policies;
CREATE POLICY tenant_isolation ON aik_ide_capability_policies
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_ide_client_registrations — ide client registrations per tenant
ALTER TABLE aik_ide_client_registrations ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_ide_client_registrations;
CREATE POLICY tenant_isolation ON aik_ide_client_registrations
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_knowledge_sources — knowledge sources per tenant
ALTER TABLE aik_knowledge_sources ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_knowledge_sources;
CREATE POLICY tenant_isolation ON aik_knowledge_sources
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_messages — messages per tenant
ALTER TABLE aik_messages ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_messages;
CREATE POLICY tenant_isolation ON aik_messages
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_models — models per tenant
ALTER TABLE aik_models ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_models;
CREATE POLICY tenant_isolation ON aik_models
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_prompt_templates — prompt templates per tenant
ALTER TABLE aik_prompt_templates ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_prompt_templates;
CREATE POLICY tenant_isolation ON aik_prompt_templates
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_providers — providers per tenant
ALTER TABLE aik_providers ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_providers;
CREATE POLICY tenant_isolation ON aik_providers
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_routing_decisions — routing decisions per tenant
ALTER TABLE aik_routing_decisions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_routing_decisions;
CREATE POLICY tenant_isolation ON aik_routing_decisions
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_routing_strategies — routing strategies per tenant
ALTER TABLE aik_routing_strategies ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_routing_strategies;
CREATE POLICY tenant_isolation ON aik_routing_strategies
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_source_weights — source weights per tenant
ALTER TABLE aik_source_weights ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_source_weights;
CREATE POLICY tenant_isolation ON aik_source_weights
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_sources — sources per tenant
ALTER TABLE aik_sources ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_sources;
CREATE POLICY tenant_isolation ON aik_sources
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_token_quota_policies — token quota policies per tenant
ALTER TABLE aik_token_quota_policies ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_token_quota_policies;
CREATE POLICY tenant_isolation ON aik_token_quota_policies
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_token_usage_ledger — token usage ledger per tenant
ALTER TABLE aik_token_usage_ledger ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_token_usage_ledger;
CREATE POLICY tenant_isolation ON aik_token_usage_ledger
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aik_tool_definitions — tool definitions per tenant
ALTER TABLE aik_tool_definitions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aik_tool_definitions;
CREATE POLICY tenant_isolation ON aik_tool_definitions
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());


-- ── AuditCompliance module — additional aud_ tables ────────────────────────────

-- aud_audit_chain_links — audit chain links per tenant
ALTER TABLE aud_audit_chain_links ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aud_audit_chain_links;
CREATE POLICY tenant_isolation ON aud_audit_chain_links
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- aud_retention_policies — retention policies per tenant
ALTER TABLE aud_retention_policies ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON aud_retention_policies;
CREATE POLICY tenant_isolation ON aud_retention_policies
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());


-- ── Configuration module — additional cfg_ tables ────────────────────────────

-- cfg_audit_entries — audit entries per tenant
ALTER TABLE cfg_audit_entries ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cfg_audit_entries;
CREATE POLICY tenant_isolation ON cfg_audit_entries
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- cfg_definitions — definitions per tenant
ALTER TABLE cfg_definitions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cfg_definitions;
CREATE POLICY tenant_isolation ON cfg_definitions
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- cfg_entries — entries per tenant
ALTER TABLE cfg_entries ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cfg_entries;
CREATE POLICY tenant_isolation ON cfg_entries
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- cfg_feature_flag_definitions — feature flag definitions per tenant
ALTER TABLE cfg_feature_flag_definitions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cfg_feature_flag_definitions;
CREATE POLICY tenant_isolation ON cfg_feature_flag_definitions
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- cfg_feature_flag_entries — feature flag entries per tenant
ALTER TABLE cfg_feature_flag_entries ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cfg_feature_flag_entries;
CREATE POLICY tenant_isolation ON cfg_feature_flag_entries
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- cfg_modules — modules per tenant
ALTER TABLE cfg_modules ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cfg_modules;
CREATE POLICY tenant_isolation ON cfg_modules
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());


-- ── ChangeGovernance module — additional chg_ tables ────────────────────────────

-- chg_canary_rollouts — canary rollouts per tenant
ALTER TABLE chg_canary_rollouts ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON chg_canary_rollouts;
CREATE POLICY tenant_isolation ON chg_canary_rollouts
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- chg_change_scores — change scores per tenant
ALTER TABLE chg_change_scores ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON chg_change_scores;
CREATE POLICY tenant_isolation ON chg_change_scores
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- chg_external_markers — external markers per tenant
ALTER TABLE chg_external_markers ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON chg_external_markers;
CREATE POLICY tenant_isolation ON chg_external_markers
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- chg_feature_flag_states — feature flag states per tenant
ALTER TABLE chg_feature_flag_states ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON chg_feature_flag_states;
CREATE POLICY tenant_isolation ON chg_feature_flag_states
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- chg_freeze_windows — freeze windows per tenant
ALTER TABLE chg_freeze_windows ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON chg_freeze_windows;
CREATE POLICY tenant_isolation ON chg_freeze_windows
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- chg_observation_windows — observation windows per tenant
ALTER TABLE chg_observation_windows ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON chg_observation_windows;
CREATE POLICY tenant_isolation ON chg_observation_windows
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- chg_post_release_reviews — post release reviews per tenant
ALTER TABLE chg_post_release_reviews ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON chg_post_release_reviews;
CREATE POLICY tenant_isolation ON chg_post_release_reviews
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- chg_release_baselines — release baselines per tenant
ALTER TABLE chg_release_baselines ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON chg_release_baselines;
CREATE POLICY tenant_isolation ON chg_release_baselines
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- chg_rollback_assessments — rollback assessments per tenant
ALTER TABLE chg_rollback_assessments ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON chg_rollback_assessments;
CREATE POLICY tenant_isolation ON chg_rollback_assessments
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());


-- ── Contracts sub-module — additional ctr_ tables ────────────────────────────

-- ctr_background_service_contract_details — background service contract details per tenant
ALTER TABLE ctr_background_service_contract_details ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_background_service_contract_details;
CREATE POLICY tenant_isolation ON ctr_background_service_contract_details
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ctr_background_service_draft_metadata — background service draft metadata per tenant
ALTER TABLE ctr_background_service_draft_metadata ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_background_service_draft_metadata;
CREATE POLICY tenant_isolation ON ctr_background_service_draft_metadata
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ctr_canonical_entities — canonical entities per tenant
ALTER TABLE ctr_canonical_entities ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_canonical_entities;
CREATE POLICY tenant_isolation ON ctr_canonical_entities
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ctr_canonical_entity_versions — canonical entity versions per tenant
ALTER TABLE ctr_canonical_entity_versions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_canonical_entity_versions;
CREATE POLICY tenant_isolation ON ctr_canonical_entity_versions
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ctr_consumer_expectations — consumer expectations per tenant
ALTER TABLE ctr_consumer_expectations ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_consumer_expectations;
CREATE POLICY tenant_isolation ON ctr_consumer_expectations
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ctr_contract_artifacts — contract artifacts per tenant
ALTER TABLE ctr_contract_artifacts ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_contract_artifacts;
CREATE POLICY tenant_isolation ON ctr_contract_artifacts
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ctr_contract_deployments — contract deployments per tenant
ALTER TABLE ctr_contract_deployments ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_contract_deployments;
CREATE POLICY tenant_isolation ON ctr_contract_deployments
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ctr_contract_diffs — contract diffs per tenant
ALTER TABLE ctr_contract_diffs ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_contract_diffs;
CREATE POLICY tenant_isolation ON ctr_contract_diffs
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ctr_contract_drafts — contract drafts per tenant
ALTER TABLE ctr_contract_drafts ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_contract_drafts;
CREATE POLICY tenant_isolation ON ctr_contract_drafts
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ctr_contract_evidence_packs — contract evidence packs per tenant
ALTER TABLE ctr_contract_evidence_packs ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_contract_evidence_packs;
CREATE POLICY tenant_isolation ON ctr_contract_evidence_packs
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ctr_contract_examples — contract examples per tenant
ALTER TABLE ctr_contract_examples ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_contract_examples;
CREATE POLICY tenant_isolation ON ctr_contract_examples
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ctr_contract_reviews — contract reviews per tenant
ALTER TABLE ctr_contract_reviews ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_contract_reviews;
CREATE POLICY tenant_isolation ON ctr_contract_reviews
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ctr_contract_rule_violations — contract rule violations per tenant
ALTER TABLE ctr_contract_rule_violations ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_contract_rule_violations;
CREATE POLICY tenant_isolation ON ctr_contract_rule_violations
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ctr_contract_scorecards — contract scorecards per tenant
ALTER TABLE ctr_contract_scorecards ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_contract_scorecards;
CREATE POLICY tenant_isolation ON ctr_contract_scorecards
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ctr_event_contract_details — event contract details per tenant
ALTER TABLE ctr_event_contract_details ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_event_contract_details;
CREATE POLICY tenant_isolation ON ctr_event_contract_details
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ctr_event_draft_metadata — event draft metadata per tenant
ALTER TABLE ctr_event_draft_metadata ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_event_draft_metadata;
CREATE POLICY tenant_isolation ON ctr_event_draft_metadata
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ctr_soap_contract_details — soap contract details per tenant
ALTER TABLE ctr_soap_contract_details ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_soap_contract_details;
CREATE POLICY tenant_isolation ON ctr_soap_contract_details
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ctr_soap_draft_metadata — soap draft metadata per tenant
ALTER TABLE ctr_soap_draft_metadata ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_soap_draft_metadata;
CREATE POLICY tenant_isolation ON ctr_soap_draft_metadata
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ctr_spectral_rulesets — spectral rulesets per tenant
ALTER TABLE ctr_spectral_rulesets ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_spectral_rulesets;
CREATE POLICY tenant_isolation ON ctr_spectral_rulesets
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ctr_contract_verifications — contract verifications per tenant
ALTER TABLE ctr_contract_verifications ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_contract_verifications;
CREATE POLICY tenant_isolation ON ctr_contract_verifications
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id());

-- ctr_contract_changelogs — contract changelogs per tenant
ALTER TABLE ctr_contract_changelogs ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ctr_contract_changelogs;
CREATE POLICY tenant_isolation ON ctr_contract_changelogs
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id());

-- cfg_contract_compliance_policies — contract compliance policies per tenant
ALTER TABLE cfg_contract_compliance_policies ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON cfg_contract_compliance_policies;
CREATE POLICY tenant_isolation ON cfg_contract_compliance_policies
    USING  (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR "TenantId" = get_current_tenant_id());


-- ── Governance module — additional gov_ tables ────────────────────────────

-- gov_compliance_gaps — compliance gaps per tenant
ALTER TABLE gov_compliance_gaps ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_compliance_gaps;
CREATE POLICY tenant_isolation ON gov_compliance_gaps
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- gov_evidence_items — evidence items per tenant
ALTER TABLE gov_evidence_items ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_evidence_items;
CREATE POLICY tenant_isolation ON gov_evidence_items
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- gov_pack_versions — pack versions per tenant
ALTER TABLE gov_pack_versions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_pack_versions;
CREATE POLICY tenant_isolation ON gov_pack_versions
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- gov_policy_as_code — policy as code per tenant
ALTER TABLE gov_policy_as_code ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_policy_as_code;
CREATE POLICY tenant_isolation ON gov_policy_as_code
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- gov_rollout_records — rollout records per tenant
ALTER TABLE gov_rollout_records ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_rollout_records;
CREATE POLICY tenant_isolation ON gov_rollout_records
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- gov_security_findings — security findings per tenant
ALTER TABLE gov_security_findings ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_security_findings;
CREATE POLICY tenant_isolation ON gov_security_findings
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- gov_security_scan_results — security scan results per tenant
ALTER TABLE gov_security_scan_results ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_security_scan_results;
CREATE POLICY tenant_isolation ON gov_security_scan_results
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- gov_team_domain_links — team domain links per tenant
ALTER TABLE gov_team_domain_links ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON gov_team_domain_links;
CREATE POLICY tenant_isolation ON gov_team_domain_links
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());


-- ── Integrations module — additional int_ tables ────────────────────────────

-- int_ingestion_executions — ingestion executions per tenant
ALTER TABLE int_ingestion_executions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON int_ingestion_executions;
CREATE POLICY tenant_isolation ON int_ingestion_executions
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());


-- ── Knowledge module — additional knw_ tables ────────────────────────────

-- knw_relations — relations per tenant
ALTER TABLE knw_relations ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON knw_relations;
CREATE POLICY tenant_isolation ON knw_relations
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());


-- ── Notifications module — additional ntf_ tables ────────────────────────────

-- ntf_channel_configurations — channel configurations per tenant
ALTER TABLE ntf_channel_configurations ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ntf_channel_configurations;
CREATE POLICY tenant_isolation ON ntf_channel_configurations
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ntf_deliveries — deliveries per tenant
ALTER TABLE ntf_deliveries ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ntf_deliveries;
CREATE POLICY tenant_isolation ON ntf_deliveries
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ntf_smtp_configurations — smtp configurations per tenant
ALTER TABLE ntf_smtp_configurations ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ntf_smtp_configurations;
CREATE POLICY tenant_isolation ON ntf_smtp_configurations
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ntf_templates — templates per tenant
ALTER TABLE ntf_templates ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ntf_templates;
CREATE POLICY tenant_isolation ON ntf_templates
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());


-- ── OperationalIntelligence module — additional ops_ tables ────────────────────────────

-- ops_burn_rate_snapshots — burn rate snapshots per tenant
ALTER TABLE ops_burn_rate_snapshots ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_burn_rate_snapshots;
CREATE POLICY tenant_isolation ON ops_burn_rate_snapshots
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ops_drift_findings — drift findings per tenant
ALTER TABLE ops_drift_findings ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_drift_findings;
CREATE POLICY tenant_isolation ON ops_drift_findings
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ops_error_budget_snapshots — error budget snapshots per tenant
ALTER TABLE ops_error_budget_snapshots ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_error_budget_snapshots;
CREATE POLICY tenant_isolation ON ops_error_budget_snapshots
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ops_incident_change_correlations — incident change correlations per tenant
ALTER TABLE ops_incident_change_correlations ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_incident_change_correlations;
CREATE POLICY tenant_isolation ON ops_incident_change_correlations
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ops_mitigation_validations — mitigation validations per tenant
ALTER TABLE ops_mitigation_validations ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_mitigation_validations;
CREATE POLICY tenant_isolation ON ops_mitigation_validations
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ops_mitigation_workflow_actions — mitigation workflow actions per tenant
ALTER TABLE ops_mitigation_workflow_actions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_mitigation_workflow_actions;
CREATE POLICY tenant_isolation ON ops_mitigation_workflow_actions
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ops_mitigation_workflows — mitigation workflows per tenant
ALTER TABLE ops_mitigation_workflows ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_mitigation_workflows;
CREATE POLICY tenant_isolation ON ops_mitigation_workflows
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ops_observability_profiles — observability profiles per tenant
ALTER TABLE ops_observability_profiles ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_observability_profiles;
CREATE POLICY tenant_isolation ON ops_observability_profiles
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ops_post_incident_reviews — post incident reviews per tenant
ALTER TABLE ops_post_incident_reviews ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_post_incident_reviews;
CREATE POLICY tenant_isolation ON ops_post_incident_reviews
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ops_reliability_capacity_forecasts — reliability capacity forecasts per tenant
ALTER TABLE ops_reliability_capacity_forecasts ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_reliability_capacity_forecasts;
CREATE POLICY tenant_isolation ON ops_reliability_capacity_forecasts
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ops_reliability_failure_predictions — reliability failure predictions per tenant
ALTER TABLE ops_reliability_failure_predictions ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_reliability_failure_predictions;
CREATE POLICY tenant_isolation ON ops_reliability_failure_predictions
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ops_reliability_snapshots — reliability snapshots per tenant
ALTER TABLE ops_reliability_snapshots ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_reliability_snapshots;
CREATE POLICY tenant_isolation ON ops_reliability_snapshots
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ops_runtime_baselines — runtime baselines per tenant
ALTER TABLE ops_runtime_baselines ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_runtime_baselines;
CREATE POLICY tenant_isolation ON ops_runtime_baselines
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ops_runtime_snapshots — runtime snapshots per tenant
ALTER TABLE ops_runtime_snapshots ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON ops_runtime_snapshots;
CREATE POLICY tenant_isolation ON ops_runtime_snapshots
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());


-- ── Catalog/Templates sub-module — tpl_ tables ────────────────────────────

-- tpl_service_templates — service templates per tenant
ALTER TABLE tpl_service_templates ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tenant_isolation ON tpl_service_templates;
CREATE POLICY tenant_isolation ON tpl_service_templates
    USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
    WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());

-- ════════════════════════════════════════════════════════════════════════════════
-- SUMMARY:
--   RLS enabled on 186 tables covering all tenant-aware data domains.
--   Phantom policies corrected: chg_change_records → chg_change_events,
--     chg_workflows → chg_releases, ctr_api_contracts → ctr_contract_versions.
--   86 additional tables added in Validation Plan rev.7 (2026-04-10).
--   Remaining tables intentionally excluded:
--     - iam_tenants, iam_users, iam_roles, iam_permissions, iam_external_identities
--       — system/global tables not scoped to a single tenant
--     - iam_role_permissions, iam_module_access_policies
--       — use nullable TenantId for system defaults vs tenant overrides
--     - Tables without TenantId column (~71 tables) — not tenant-scoped by design
--
-- TO ADD MORE TABLES:
--   1. Verify the entity has a `tenant_id uuid NOT NULL` column.
--   2. Add: ALTER TABLE <table> ENABLE ROW LEVEL SECURITY;
--   3. Add: DROP POLICY IF EXISTS tenant_isolation ON <table>;
--   4. Add: CREATE POLICY tenant_isolation ON <table>
--              USING  (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id())
--              WITH CHECK (get_current_tenant_id() IS NULL OR tenant_id = get_current_tenant_id());
-- ════════════════════════════════════════════════════════════════════════════════
