# NexTraceOne — Official Database Table Prefixes

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Phase:** A0 + A1 — Consolidation

---

## Convention

All PostgreSQL tables **must** use a module-specific prefix in the format `prefix_tablename`.

- Prefixes are **lowercase**, **3 characters**, followed by an underscore.
- Table names after the prefix use **snake_case**.
- The prefix uniquely identifies the owning module.
- No two modules share a prefix.

---

## Official Prefix Registry

### `iam_` — Identity & Access

| Attribute | Value |
|-----------|-------|
| **Module** | Identity & Access |
| **Justification** | Standard abbreviation for Identity and Access Management |
| **Example tables** | `iam_users`, `iam_tenants`, `iam_roles`, `iam_permissions`, `iam_sessions`, `iam_security_events`, `iam_api_keys`, `iam_refresh_tokens`, `iam_jit_access`, `iam_break_glass`, `iam_delegations` |
| **Notes** | Foundational module. All other modules depend on identity context from these tables. |

---

### `env_` — Environment Management

| Attribute | Value |
|-----------|-------|
| **Module** | Environment Management |
| **Justification** | Direct abbreviation of "environment" |
| **Example tables** | `env_environments`, `env_environment_policies`, `env_environment_profiles`, `env_criticality_levels`, `env_drift_records` |
| **Notes** | Currently shares IdentityDbContext. Tables will be extracted when this module gains its own DbContext. |

---

### `cat_` — Service Catalog

| Attribute | Value |
|-----------|-------|
| **Module** | Service Catalog |
| **Justification** | Standard abbreviation for "catalog" |
| **Example tables** | `cat_services`, `cat_apis`, `cat_consumers`, `cat_dependencies`, `cat_health_records`, `cat_snapshots`, `cat_developer_portal_sessions` |
| **Notes** | Source of truth for service registry. Referenced by Contracts, Change Governance, and Operational Intelligence. |

---

### `ctr_` — Contracts

| Attribute | Value |
|-----------|-------|
| **Module** | Contracts |
| **Justification** | Abbreviation for "contracts" avoiding collision with `cat_` (Catalog) and `cfg_` (Configuration) |
| **Example tables** | `ctr_contracts`, `ctr_contract_versions`, `ctr_contract_schemas`, `ctr_api_endpoints`, `ctr_event_contracts`, `ctr_soap_services`, `ctr_spectral_rulesets`, `ctr_compliance_scores` |
| **Notes** | Backend currently resides in the Catalog project. Tables will be prefixed when physically separated. |

---

### `chg_` — Change Governance

| Attribute | Value |
|-----------|-------|
| **Module** | Change Governance |
| **Justification** | Abbreviation for "change" |
| **Example tables** | `chg_releases`, `chg_change_events`, `chg_blast_radius_reports`, `chg_workflow_templates`, `chg_workflow_instances`, `chg_approval_decisions`, `chg_promotion_requests`, `chg_rulesets`, `chg_freeze_windows`, `chg_rollback_assessments` |
| **Notes** | Uses 4 DbContexts currently (ChangeIntelligence, Workflow, Promotion, RulesetGovernance). Prefix is shared across all. |

---

### `ops_` — Operational Intelligence

| Attribute | Value |
|-----------|-------|
| **Module** | Operational Intelligence |
| **Justification** | Standard abbreviation for "operations" |
| **Example tables** | `ops_incident_records`, `ops_mitigation_actions`, `ops_automation_workflows`, `ops_automation_executions`, `ops_runbooks`, `ops_reliability_snapshots`, `ops_runtime_insights`, `ops_cost_reports`, `ops_health_classifications` |
| **Notes** | Uses 5 DbContexts currently (Incident, Automation, Reliability, RuntimeIntelligence, CostIntelligence). Prefix is shared across all. |

---

### `aik_` — AI & Knowledge

| Attribute | Value |
|-----------|-------|
| **Module** | AI & Knowledge |
| **Justification** | Abbreviation combining "AI" and "Knowledge" |
| **Example tables** | `aik_models`, `aik_providers`, `aik_access_policies`, `aik_token_quotas`, `aik_token_usage_ledger`, `aik_agents`, `aik_agent_executions`, `aik_orchestration_sessions`, `aik_knowledge_entries`, `aik_messages`, `aik_routing_decisions`, `aik_ide_extensions` |
| **Notes** | Covers all 3 subdomains: AI Core, Agents, and Knowledge. Uses 3 DbContexts currently. |

---

### `gov_` — Governance

| Attribute | Value |
|-----------|-------|
| **Module** | Governance |
| **Justification** | Standard abbreviation for "governance" |
| **Example tables** | `gov_teams`, `gov_domains`, `gov_policies`, `gov_governance_packs`, `gov_waivers`, `gov_compliance_reports`, `gov_risk_assessments`, `gov_finops_entries`, `gov_controls`, `gov_evidence` |
| **Notes** | Currently a catch-all module. After Integrations and Product Analytics are extracted, remaining tables are purely governance-scoped. |

---

### `cfg_` — Configuration

| Attribute | Value |
|-----------|-------|
| **Module** | Configuration |
| **Justification** | Standard abbreviation for "configuration" |
| **Example tables** | `cfg_configuration_definitions`, `cfg_configuration_entries`, `cfg_configuration_audit_entries` |
| **Notes** | Transversal module consumed by all other modules. Currently uses `EnsureCreated` — must migrate to proper migrations. |

---

### `aud_` — Audit & Compliance

| Attribute | Value |
|-----------|-------|
| **Module** | Audit & Compliance |
| **Justification** | Abbreviation for "audit" |
| **Example tables** | `aud_audit_events`, `aud_audit_chain_links`, `aud_audit_campaigns`, `aud_compliance_policies`, `aud_compliance_results`, `aud_retention_policies` |
| **Notes** | Immutable audit trail with cryptographic hash chain. Append-only pattern for audit events. |

---

### `ntf_` — Notifications

| Attribute | Value |
|-----------|-------|
| **Module** | Notifications |
| **Justification** | Abbreviation for "notifications" |
| **Example tables** | `ntf_notifications`, `ntf_notification_deliveries`, `ntf_notification_preferences` |
| **Notes** | Currently uses `EnsureCreated` — must migrate to proper migrations. Transversal module handling multi-channel delivery. |

---

### `int_` — Integrations

| Attribute | Value |
|-----------|-------|
| **Module** | Integrations |
| **Justification** | Abbreviation for "integrations" |
| **Example tables** | `int_integration_connectors`, `int_ingestion_sources`, `int_ingestion_executions` |
| **Notes** | Backend currently resides in the Governance project. Tables will be prefixed when physically separated into own DbContext. |

---

### `pan_` — Product Analytics

| Attribute | Value |
|-----------|-------|
| **Module** | Product Analytics |
| **Justification** | Abbreviation for "product analytics" (first letters of each word) |
| **Example tables** | `pan_analytics_events`, `pan_adoption_metrics`, `pan_persona_usage`, `pan_journey_funnels`, `pan_value_tracking` |
| **Notes** | Backend currently resides in the Governance project. Strong candidate for ClickHouse for analytical data. Tables will be prefixed when physically separated into own DbContext. |

---

## Summary Table

| Prefix | Module | DbContext (current) | Physical Separation Needed |
|--------|--------|--------------------|-|
| `iam_` | Identity & Access | IdentityDbContext | No |
| `env_` | Environment Management | IdentityDbContext (shared) | **Yes** |
| `cat_` | Service Catalog | CatalogGraphDbContext, DeveloperPortalDbContext | No |
| `ctr_` | Contracts | ContractsDbContext | **Yes** (extract from Catalog) |
| `chg_` | Change Governance | ChangeIntelligenceDbContext + 3 others | No |
| `ops_` | Operational Intelligence | IncidentDbContext + 4 others | No |
| `aik_` | AI & Knowledge | AiGovernanceDbContext + 2 others | No |
| `gov_` | Governance | GovernanceDbContext | No (after extractions) |
| `cfg_` | Configuration | ConfigurationDbContext | No |
| `aud_` | Audit & Compliance | AuditDbContext | No |
| `ntf_` | Notifications | NotificationsDbContext | No |
| `int_` | Integrations | GovernanceDbContext (shared) | **Yes** |
| `pan_` | Product Analytics | GovernanceDbContext (shared) | **Yes** |
