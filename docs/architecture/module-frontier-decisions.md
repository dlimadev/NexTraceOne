# NexTraceOne — Module Frontier Decisions

> **Status:** APPROVED  
> **Date:** 2026-03-24  
> **Phase:** A0 + A1 — Consolidation  
> **Sources:** `docs/11-review-modular/modular-review-master.md`, module consolidated reviews, governance reports, codebase analysis

---

## 3.1 — Catalog vs Contracts

### Context

The Contracts module was created as a first-class citizen for API contract governance. However, the backend implementation currently resides inside the Catalog project (`src/modules/catalog/`), sharing `ContractsDbContext` within the `nextraceone_catalog` database. The frontend already treats Contracts as a separate feature (`src/frontend/src/features/contracts/`).

### What belongs to Catalog

- Service registry (source of truth for all services)
- API catalog and API metadata
- Dependency graph and topology visualization
- Consumer discovery
- Health record tracking
- Developer Portal sessions
- Service snapshots

### What belongs to Contracts

- Contract entity lifecycle (Draft → InReview → Published → Deprecated)
- Contract versioning and diff
- Contract schemas (OpenAPI, AsyncAPI, WSDL, etc.)
- API endpoint definitions as governed contract artifacts
- Event contracts
- SOAP service contracts
- Spectral ruleset management and validation
- Compliance scoring per contract
- Canonical entity management
- Contract governance policies
- Contract export and publication

### How Contracts references Catalog

- Contracts reference services from Catalog via `ServiceId` (strongly-typed ID).
- A contract is always associated with a service from the Catalog.
- Contracts never own or manage service metadata — they consume it.
- The relationship is **read-only from Contracts to Catalog**: Contracts can query service information but never write to Catalog entities.
- Cross-module communication should use events (outbox pattern) for eventual consistency.

### What must NEVER be in Catalog

- Contract lifecycle management
- Contract validation rules (Spectral rulesets)
- Compliance scoring
- Contract approval workflows
- Canonical entity management

### What must NEVER be in Contracts

- Service registration and metadata
- Dependency graph management
- Consumer discovery
- Health monitoring
- Developer Portal session management
- Topology visualization

### Action Required

- Extract `ContractsDbContext` and all contract-related entities, configurations, and features from the `catalog` backend project into a dedicated `contracts` backend project.
- Maintain the `ServiceId` foreign reference as a cross-module integration point.
- Fix the 3 broken frontend routes (`/contracts/governance`, `/contracts/spectral`, `/contracts/canonical`).

---

## 3.2 — Governance vs Integrations

### Context

The Integrations module currently has its backend embedded inside the Governance project (`src/modules/governance/`) as `IntegrationHubEndpointModule`. Its entities (`IntegrationConnector`, `IngestionSource`, `IntegrationExecution`) reside in `GovernanceDbContext`. The frontend already treats Integrations as a separate feature (`src/frontend/src/features/integrations/`).

### What is governance of integrations (stays in Governance)

- Policies that define which integrations are allowed or blocked
- Compliance reporting on integration usage
- Risk assessment of integration dependencies
- Executive views on integration health across the organization
- Governance packs that include integration-related controls

### What is operational execution of integrations (goes to Integrations)

- Connector management (CRUD, configuration, credentials)
- Ingestion source definitions
- Ingestion execution orchestration and monitoring
- Data freshness tracking
- Connector health monitoring
- Integration failure notifications
- Execution retry logic

### What stays in Governance

- Integration governance policies
- Compliance views that include integration data
- Risk dashboards that reference integration health
- Executive reports that aggregate integration metrics

### What goes to Integrations

- `IntegrationConnector` entity and all CRUD operations
- `IngestionSource` entity and all CRUD operations
- `IntegrationExecution` entity (or `IngestionExecution`) and tracking
- `IntegrationHubEndpointModule` and all integration-specific endpoints
- Integration-specific notification handlers (e.g., `IntegrationFailureNotificationHandler`)
- Connector configuration and credential management

### Action Required

- Extract integration entities from `GovernanceDbContext` into a new `IntegrationsDbContext`.
- Move `IntegrationHubEndpointModule` and related features/handlers from the `governance` project to a new `integrations` backend project.
- Governance retains policy and reporting views that reference integration data via events or read models.

---

## 3.3 — Governance vs Product Analytics

### Context

The Product Analytics module currently has its backend embedded inside the Governance project (`src/modules/governance/`) as `ProductAnalyticsEndpointModule`. The frontend already treats Product Analytics as a separate feature (`src/frontend/src/features/product-analytics/`).

### What is governance/reporting of conformity (stays in Governance)

- Compliance reports that measure organizational conformity
- Risk assessment and scoring
- Governance pack compliance levels
- Policy adherence tracking
- Executive compliance views
- Evidence management

### What is analytics of product usage (goes to Product Analytics)

- Product usage event tracking and collection
- Adoption metrics by module
- Usage analytics by persona
- Journey funnels (user flows through the product)
- Value tracking (business value delivered by the platform)
- Engagement metrics
- Feature adoption rates

### What stays in Governance

- Compliance dashboards and reporting
- Risk center
- Executive views (except product usage analytics)
- Policy management
- Governance packs
- Evidence and waivers

### What goes to Product Analytics

- `ProductAnalyticsEndpointModule` and all analytics-specific endpoints
- Usage event collection endpoints
- Analytics aggregation logic
- Adoption, persona usage, and journey entities
- Value tracking entities and endpoints

### Action Required

- Extract analytics entities from `GovernanceDbContext` into a new `ProductAnalyticsDbContext`.
- Move `ProductAnalyticsEndpointModule` and related features from the `governance` project to a new `productanalytics` backend project.
- Product Analytics is a **REQUIRED** ClickHouse consumer for high-volume event streams.
- Governance retains compliance and risk reporting that does not overlap with product usage analytics.

---

## 3.4 — Identity & Access / Configuration / Change Governance / Operations vs Environment Management

### Context

Environment Management is defined as an independent bounded context in the official module list, but it currently has no dedicated backend project. Its entities (`Environment`, `EnvironmentPolicy`, `EnvironmentProfile`) are embedded in the Identity & Access module (`IdentityDbContext`). Frontend pages (EnvironmentsPage) are in the `identity-access` feature folder.

### What Environment Management owns

- Environment entity lifecycle (create, update, archive, delete)
- Environment policies (who can access, deploy, or promote to which environment)
- Environment profiles (Dev, Staging, Production, etc.)
- Criticality levels and `IsPrimaryProduction` flag
- Drift detection and environment consistency checks
- Environment health and status
- Environment-scoped configuration overrides
- Promotion path definitions between environments

### What stays in Identity & Access

- User authentication and session management
- Tenant management (multi-tenancy isolation)
- Role and permission management
- Security events and audit
- JIT access, break glass, and delegation (these are identity concerns, even if environment-scoped)
- API key management

### What stays in Configuration

- Configuration definitions and entries (transversal store)
- Configuration inherits environment context from Environment Management
- Configuration does not own environment lifecycle — it consumes `EnvironmentId` from Environment Management

### What Change Governance only consumes

- Change Governance reads environment data to validate promotions and freeze windows
- Change Governance does not create or manage environments
- Change Governance references `EnvironmentId` from Environment Management for blast radius and promotion validation

### What Operational Intelligence only consumes

- Operational Intelligence reads environment context for incident scoping and reliability scoring
- Operational Intelligence does not create or manage environments
- Operational Intelligence references `EnvironmentId` for environment-specific metrics and dashboards

### Action Required

- Extract environment-related entities from `IdentityDbContext` into a new `EnvironmentDbContext`.
- Create a dedicated `environmentmanagement` backend project (or `environments`) with Domain/Application/Infrastructure layers.
- Move environment-specific frontend pages from `identity-access` to a new `environment-management` feature folder.
- Other modules reference environments via `EnvironmentId` (strongly-typed) and consume environment data through events or read-only queries.

---

## 3.5 — AI & Knowledge Internal Subdomains

### Context

AI & Knowledge is a single module with three internal subdomains: AI Core, Agents, and Knowledge. The module currently has the lowest backend maturity (25%) with a significant perception gap due to higher frontend maturity (70%).

### Subdomain: AI Core

**Responsibility:**

- AI model registry (model definitions, capabilities, constraints)
- AI provider management (OpenAI, Azure, local models)
- AI access policies (who can use which models, for what purpose)
- Token quota and budget management
- Token usage ledger and tracking
- AI routing decisions (which model to use for which request)
- Model performance tracking

**Key entities:** AIModel, AiProvider, AIAccessPolicy, AiTokenQuotaPolicy, AiTokenUsageLedger, AiRoutingDecision

### Subdomain: Agents

**Responsibility:**

- Agent definitions (purpose, configuration, allowed tools)
- Agent orchestration sessions (conversation state, context)
- Agent execution tracking (invocations, results, errors)
- Tool registration and binding (tools declared but not yet runtime-connected)
- Assistant panel integration (used by Change Governance and others)

**Key entities:** AiAgent, AiAgentExecution, AiOrchestrationSession, AiMessage

### Subdomain: Knowledge

**Responsibility:**

- Knowledge capture (lessons learned, operational notes, decisions)
- Knowledge retrieval (RAG-based or contextual search)
- Operational context provision to AI queries
- Knowledge sources management
- IDE extensions management (for governed AI access from IDEs)

**Key entities:** KnowledgeCaptureEntry, AiExternalInferenceRecord, IdeExtension

### Why these subdomains do not justify independent modules now

1. **Shared persistence** — All three subdomains share the same database (`nextraceone_ai`) and their DbContexts are tightly coupled with shared entity references.
2. **Low maturity** — At 25% backend maturity, splitting would create three even weaker modules with higher coordination overhead.
3. **Shared lifecycle** — AI Core, Agents, and Knowledge evolve together — provider changes affect agent capabilities which affect knowledge retrieval.
4. **Shared security model** — AI access policies govern all three subdomains uniformly.
5. **Bounded context coherence** — The AI & Knowledge module has a coherent bounded context around "AI-assisted operations" that benefits from unified governance.

### When to reconsider

- If Agents becomes a platform-level runtime with its own deployment lifecycle.
- If Knowledge becomes a standalone knowledge management product.
- If the module exceeds 300+ entities with conflicting change frequencies across subdomains.

---

## Summary of Frontier Decisions

| Frontier | Decision | Action |
|----------|----------|--------|
| Catalog vs Contracts | **Separate modules**, Contracts references Catalog via ServiceId | Extract Contracts backend from Catalog project |
| Governance vs Integrations | **Separate modules**, Governance retains policy views | Extract Integrations backend from Governance project |
| Governance vs Product Analytics | **Separate modules**, Governance retains compliance reporting | Extract Product Analytics backend from Governance project |
| Identity vs Environment Management | **Separate bounded contexts**, Identity retains auth/session | Extract Environment entities from Identity project |
| AI & Knowledge subdomains | **Keep unified**, 3 internal subdomains (AI Core, Agents, Knowledge) | No structural change now; improve backend maturity |
