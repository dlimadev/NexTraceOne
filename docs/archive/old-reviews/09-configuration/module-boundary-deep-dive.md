# Configuration Module — Boundary Deep Dive

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Module:** 09 — Configuration  
> **Phase:** B1 — Module Consolidation

---

## 1. Current Responsibilities

The Configuration module is the **transversal centralized configuration store** for the entire NexTraceOne platform. Its responsibilities are:

1. **Definition Management** — Maintain a registry of ~345 configuration definitions organized in 8 phases, each with key, display name, description, category, data type, allowed scopes, default value, validation rules, and UI editor hints.
2. **Value Management** — Store and retrieve concrete configuration values at multiple hierarchical scopes (System → Tenant → Environment → Role → Team → User).
3. **Scope Resolution** — Resolve the effective value for any configuration key by traversing the scope hierarchy from most specific to most general, respecting inheritance rules.
4. **Audit Trail** — Record all configuration changes with before/after values, version tracking, change reasons, and user attribution.
5. **Sensitive Value Protection** — Encrypt sensitive values at rest (AES-256-GCM) and mask them in API responses and UI.
6. **Cache Management** — Maintain an in-memory cache with version-based invalidation for resolved configuration values.
7. **Integration Events** — Publish events when configuration values change (ConfigurationValueChanged, ConfigurationValueActivated, ConfigurationValueDeactivated).

---

## 2. Current Entities

| Entity | Type | DbSet | File |
|--------|------|-------|------|
| `ConfigurationDefinition` | Aggregate Root | `Definitions` | `Domain/Entities/ConfigurationDefinition.cs` (229 lines) |
| `ConfigurationEntry` | Aggregate Root | `Entries` | `Domain/Entities/ConfigurationEntry.cs` (257 lines) |
| `ConfigurationAuditEntry` | Aggregate Root | `AuditEntries` | `Domain/Entities/ConfigurationAuditEntry.cs` (158 lines) |

**Enums:**

| Enum | Values | File |
|------|--------|------|
| `ConfigurationScope` | System(0), Tenant(1), Environment(2), Role(3), Team(4), User(5) | `Domain/Enums/ConfigurationScope.cs` |
| `ConfigurationCategory` | Bootstrap, SensitiveOperational, Functional | `Domain/Enums/ConfigurationCategory.cs` |
| `ConfigurationValueType` | String, Integer, Decimal, Boolean, Json, StringList | `Domain/Enums/ConfigurationValueType.cs` |

---

## 3. Current APIs

| Endpoint | Method | Permission | CQRS Handler |
|----------|--------|-----------|--------------|
| `/api/v1/configuration/definitions` | GET | `configuration:read` | GetDefinitions |
| `/api/v1/configuration/entries` | GET | `configuration:read` | GetEntries |
| `/api/v1/configuration/effective` | GET | `configuration:read` | GetEffectiveSettings |
| `/api/v1/configuration/{key}` | PUT | `configuration:write` | SetConfigurationValue |
| `/api/v1/configuration/{key}/override` | DELETE | `configuration:write` | RemoveOverride |
| `/api/v1/configuration/{key}/toggle` | POST | `configuration:write` | ToggleConfiguration |
| `/api/v1/configuration/{key}/audit` | GET | `configuration:read` | GetAuditHistory |

---

## 4. Current Frontend Pages

### Module-owned pages (in `src/frontend/src/features/configuration/`)

| Page | Route | Description |
|------|-------|-------------|
| `ConfigurationAdminPage` | `/platform/configuration` | Main configuration management with 3 views (definitions, entries, effective) |
| `AdvancedConfigurationConsolePage` | `/platform/configuration/advanced` | Enterprise console with 6 tabs (explorer, diff, import/export, rollback, history, health) |

### Distributed pages (owned by other feature modules, consuming Configuration APIs)

| Page | Route | Feature Module | Prefix Filter |
|------|-------|---------------|---------------|
| `NotificationConfigurationPage` | `/platform/configuration/notifications` | notifications | `notifications.*` |
| `WorkflowConfigurationPage` | `/platform/configuration/workflows` | change-governance | `workflow.*`, `promotion.*` |
| `GovernanceConfigurationPage` | `/platform/configuration/governance` | governance | `governance.*` |
| `CatalogContractsConfigurationPage` | `/platform/configuration/catalog-contracts` | catalog | `catalog.*`, `change.*` |
| `OperationsFinOpsConfigurationPage` | `/platform/configuration/operations-finops` | operational-intelligence | `incidents.*`, `operations.*`, `finops.*`, `benchmarking.*` |
| `AiIntegrationsConfigurationPage` | `/platform/configuration/ai-integrations` | ai-hub | `ai.*`, `integrations.*` |

---

## 5. Configuration Definitions by Phase

| Phase | Domain | Definitions | Key Prefix | SortOrder Range |
|-------|--------|-------------|------------|-----------------|
| 0 | Foundation (instance) | ~5 | `instance.*` | 1–10 |
| 1 | Foundation (features, policies) | ~10 | `instance.*`, `policies.*` | 10–50 |
| 2 | Notifications | 38 | `notifications.*` | 150–201 |
| 3 | Workflow & Promotion | 45 | `workflow.*`, `promotion.*` | 2000–2650 |
| 4 | Governance & Compliance | 44 | `governance.*` | 3000–3540 |
| 5 | Catalog, Contracts & Change | 49 | `catalog.*`, `change.*` | 4000–4690 |
| 6 | Operations, Incidents, FinOps | 53 | `incidents.*`, `operations.*`, `finops.*`, `benchmarking.*` | 5000–5620 |
| 7 | AI & Integrations | 55 | `ai.*`, `integrations.*` | 6000–6670 |

**Total: ~345 definitions**

---

## 6. What Is Improperly Inside the Module

**Nothing significant.** The Configuration module has a clean boundary. There are no entities, endpoints, or features that belong to other modules.

The distributed configuration pages (section 4 above) are correctly placed in their respective feature modules — they consume Configuration APIs but are owned by their domain modules.

---

## 7. What Should Be in the Module but Is Elsewhere

**Nothing significant.** The module owns all its domain logic, persistence, and core APIs.

The only observation is that the `ConfigurationDefinitionSeeder` contains definitions for all domains (notifications, AI, governance, etc.). This is intentional — the Configuration module is the **source of truth** for configuration metadata. Other modules do not define their own configuration schemas; they consume what Configuration provides.

---

## 8. Relationships with Other Modules

### 8.1 Identity & Access → Configuration

| Direction | Relationship |
|-----------|-------------|
| **Dependency** | Configuration depends on Identity & Access for authentication (JWT), authorization (`configuration:read`, `configuration:write`), tenant context (RLS via `TenantRlsInterceptor`), and user context (audit trail). |
| **Nature** | Foundational — required for Configuration to function. |

### 8.2 Configuration → All Other Modules (Transversal)

| Consumer Module | Key Prefixes Consumed | Nature |
|----------------|----------------------|--------|
| Identity & Access | `instance.*`, `security.*` | Instance settings, session timeout |
| Environment Management | `environment.*` | Environment classification, criticality |
| Service Catalog | `catalog.*` | Contract requirements, validation rules |
| Contracts | `catalog.*`, `change.*` | Publication settings, contract policies |
| Change Governance | `workflow.*`, `promotion.*` | Workflow templates, approval rules |
| Operational Intelligence | `incidents.*`, `operations.*`, `finops.*`, `benchmarking.*` | Incident taxonomy, SLA settings |
| AI & Knowledge | `ai.*` | Provider settings, model selection, token budgets |
| Governance | `governance.*` | Compliance policies, scoring thresholds |
| Notifications | `notifications.*` | Channel settings, quiet hours, escalation |
| Integrations | `integrations.*` | Connector settings, retry policies |
| Audit & Compliance | (indirect) | Inherits from Configuration's audit mechanisms |
| Product Analytics | (indirect) | Event tracking configuration |

### 8.3 Environment Management

Configuration does **not** own environments. It consumes `EnvironmentId` from Environment Management as a scope reference when resolving environment-scoped configurations. The `ConfigurationScope.Environment` scope requires an `EnvironmentId` to identify which environment's overrides to apply.

### 8.4 Notifications

Configuration does **not** handle notification delivery. It stores notification-related settings (channels, quiet hours, templates) that the Notifications module reads to govern its behavior.

### 8.5 Integrations

Configuration stores integration connector settings (timeouts, retry policies, schedules) that the Integrations module reads. It does **not** manage connectors or execute integrations.

### 8.6 AI & Knowledge

Configuration stores AI governance settings (providers, models, budgets, token quotas) that AI & Knowledge reads. It does **not** interact with AI providers or orchestrate AI operations.

---

## 9. Boundary Verdict

| Aspect | Status |
|--------|--------|
| Module scope | **CLEAR** — well-defined bounded context |
| Entity ownership | **CLEAN** — all 3 entities belong to Configuration |
| API ownership | **CLEAN** — all 7 endpoints belong to Configuration |
| Frontend ownership | **CLEAN** — 2 module pages + 6 distributed pages correctly placed |
| Cross-module contamination | **NONE** — no entities or logic from other modules |
| Missing functionality | **NONE** — all configuration concerns are here |
| Transversal nature | **CONFIRMED** — consumed by all 12 other modules |

**Conclusion:** The Configuration module boundary is well-defined and requires no structural changes. The focus should be on persistence maturity, documentation, and preparation for the migration baseline.
