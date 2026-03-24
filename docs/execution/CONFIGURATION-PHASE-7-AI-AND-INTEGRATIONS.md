# Configuration Phase 7 — AI & Integrations

## Objective

Phase 7 extends the NexTraceOne configuration platform to cover the AI governance and integration management domains.

This phase transforms hardcoded AI rules and integration parameters into **administrable, auditable, multi-tenant configuration** — making AI providers, models, budgets, prompts, retrieval settings, connectors, schedules, retries, and failure policies governable through the product's own configuration UI.

## Scope Delivered

### 55 New Configuration Definitions (sortOrder 6000–6670)

| Block | Prefix | Count | Description |
|-------|--------|-------|-------------|
| A — Providers & Models | `ai.providers.*`, `ai.models.*`, `ai.usage.*` | 8 | Enabled providers/models, default by capability, fallback order, allow/deny external, blocked environments, internal-only capabilities |
| B — Budgets & Quotas | `ai.budget.*`, `ai.quota.*` | 7 | Token budgets by user/team/tenant, quota by capability, usage limits by environment, exceed policy, warning thresholds |
| C — Prompts & Retrieval | `ai.retention.*`, `ai.audit.*`, `ai.prompts.*`, `ai.retrieval.*`, `ai.defaults.*` | 14 | Conversation/artifact retention, audit level/logging, base prompts, tenant override, top-k, temperature, max tokens, similarity threshold, source allow/deny lists, context by environment |
| D — Connectors & Schedules | `integrations.connectors.*`, `integrations.schedule.*`, `integrations.retry.*`, `integrations.timeout.*`, `integrations.execution.*` | 10 | Enabled connectors, enablement by environment, schedules, retries, exponential backoff, timeouts by connector, max concurrent executions |
| E — Filters & Sync | `integrations.sync.*`, `integrations.import.*`, `integrations.export.*`, `integrations.freshness.*` | 8 | Sync filter/mapping policies, import/export policies, overwrite behavior, pre-validation, staleness thresholds by connector |
| F — Failure & Governance | `integrations.failure.*`, `integrations.owner.*`, `integrations.governance.*` | 8 | Failure notifications/severity/escalation, auto-disable, auth reaction, fallback owner, blocked operations in production |

### Backend

- 55 definitions added to `ConfigurationDefinitionSeeder`
- All definitions use `Functional` category
- System-only, non-inheritable for security-critical settings (AI blocked environments, tenant prompt override, integration operations blocked in production)
- JSON validation for complex structures
- Enum validation for select fields (exceed policy, audit level, overwrite behavior)
- Range validation for numeric fields (retention days, top-k, temperature, max tokens, retry attempts, backoff, timeout, staleness)

### Frontend

- `AiIntegrationsConfigurationPage` at `/platform/configuration/ai-integrations`
- 6 section tabs: Providers & Models, Budgets & Quotas, Prompts & Retrieval, Connectors & Schedules, Filters & Sync, Failure & Governance
- Effective settings explorer with scope selection, search, effective value display, audit history
- Full i18n in 4 locales (en, pt-BR, pt-PT, es)
- 13 frontend tests

### Testing

- 37 backend unit tests validating definitions, keys, categories, prefixes, sort orders, scope rules, defaults, validations
- 13 frontend tests validating page rendering, section navigation, loading/error/empty states
- Total: 251 backend + 13 frontend tests for configuration module

## Impact on Next Phases

Phase 8 can now focus on **UX administrative avançada, import/export, rollback e governança da própria capability de parametrização**, as all AI and integration rules are externalized into the configuration platform.

The effective settings explorer fully covers all 7 phases of parameterized domains.
