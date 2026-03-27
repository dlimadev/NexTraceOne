# NexTraceOne — Execution Prompts Sequence

> **Source of Truth:** `docs/audit-forensic-2026-03/` (16 forensic audit reports, March 2026)
>
> **Project Verdict:** `STRATEGIC_BUT_INCOMPLETE`
>
> **Principle:** Close core flows and structural gaps before expanding surface.

---

## Gate Map

| Gate | Theme | Unlocked When |
|------|-------|---------------|
| **Gate 1** | Core flows stop being fake | Incidents real + AI Assistant real |
| **Gate 2** | Cross-module integration exists | Priority interfaces implemented + outbox active |
| **Gate 3** | Governance/FinOps/Knowledge stop being simulated | Real data consumption + migrations generated |
| **Gate 4** | Quality and operations hardened | E2E blocks PRs + configs per environment corrected |
| **Gate 5** | Cleanup and consolidation | Historical docs archived + replaced mocks removed |

---

## Phase 00 — Documentation Consolidation

### P00.1 — Correct documentation contradictions against audit

**Scope:** Update IMPLEMENTATION-STATUS.md and ROADMAP.md to match audit findings.

**What to do:**
1. In `IMPLEMENTATION-STATUS.md`: Update Incidents from "SIM (InMemoryIncidentStore)" to "PARTIAL — EfIncidentStore real (678 lines), IncidentDbContext with migration, correlation dynamic 0%"
2. In `ROADMAP.md`: Update Flow 3 (Incidents) from "in progress / connected" to "0% functional — correlation engine missing, frontend mock"
3. In `ROADMAP.md`: Correct E2E test count from "13 new" to "8 specs confirmed"
4. Remove any references treating Commercial Governance as active (removed PR-17)

**Depends on:** Nothing
**Unblocks:** All subsequent prompts (establishes truthful baseline)
**Blocked until done:** P00.2, P00.3

---

### P00.2 — Create CURRENT-STATE.md per module pointing to REBASELINE

**Scope:** Create one `CURRENT-STATE.md` per major module area, replacing the need to browse 100+ docs in `docs/11-review-modular/`.

**What to do:**
1. Create `docs/current-state/catalog-current-state.md` — summarize: 91.7% real, 3 DbContexts, 84 features, 7 intentional stubs (Developer Portal). Source: REBASELINE.md + backend-state-report.md
2. Create `docs/current-state/change-governance-current-state.md` — summarize: 100% real, 4 DbContexts, 50+ features. IChangeIntelligenceModule = PLAN.
3. Create `docs/current-state/identity-access-current-state.md` — summarize: 100% real, 35 features, JWT+RBAC+JIT+BreakGlass+Delegations.
4. Create `docs/current-state/audit-compliance-current-state.md` — summarize: 100% real, SHA-256 chain, 2 migrations.
5. Create `docs/current-state/operational-intelligence-current-state.md` — summarize: LOW MATURITY, incidents EfIncidentStore real but correlation 0%, automation 100% mock, reliability 8 hardcoded services.
6. Create `docs/current-state/ai-knowledge-current-state.md` — summarize: MEDIUM, governance 28 features real, orchestration partial, externalAI 8 TODO stubs, assistant hardcoded.
7. Create `docs/current-state/governance-current-state.md` — summarize: 100% MOCK by design, GovernanceDbContext exists, 74 handlers IsSimulated.
8. Create `docs/current-state/integrations-current-state.md` — summarize: STUB/INCOMPLETE, no real connector E2E, ingestion metadata-only.
9. Create `docs/current-state/knowledge-current-state.md` — summarize: KnowledgeDbContext no migrations, entities created (P10.1-P10.3), endpoints functional.
10. Create `docs/current-state/finops-current-state.md` — summarize: 100% MOCK, CostIntelligenceDbContext exists, ICostIntelligenceModule = PLAN.

**Depends on:** P00.1
**Unblocks:** P06.1, P06.5 (documentation cleanup needs current state as replacement)
**Blocked until done:** P06.1

---

### P00.3 — Align CORE-FLOW-GAPS.md to audit findings

**Scope:** Ensure CORE-FLOW-GAPS.md accurately reflects the 4 central value flows.

**What to do:**
1. Confirm Flow 1 (Service Catalog & Contracts): 75% functional — catalog real, DeveloperPortal 7 stubs awaiting IContractsModule
2. Confirm Flow 2 (Change Intelligence): 95% functional — most mature module, IChangeIntelligenceModule = PLAN
3. Update Flow 3 (Incident Correlation): 0% functional — EfIncidentStore exists but correlation dynamic absent, frontend mock, runbooks hardcoded
4. Update Flow 4 (AI Assistant): assistant returns hardcoded, governance governs nothing functional
5. Add cross-reference to audit reports as evidence source

**Depends on:** P00.1
**Unblocks:** Clear prioritization reference for Phase 01
**Blocked until done:** Nothing directly

---

## Phase 01 — Critical Core Flow Fixes (Gate 1)

### P01.1 — Dynamic incident↔change correlation engine (backend)

**Scope:** Implement correlation logic that dynamically links incidents to changes based on timestamps, affected services, and environments.

**What to do:**
1. In `OperationalIntelligence.Application`, create a `CorrelateIncidentWithChanges` handler
2. Query IncidentDbContext for incident details (service, timestamp, environment)
3. Query ChangeIntelligenceDbContext (via direct DbContext read or cross-module query) for changes in the same service within a configurable time window before the incident
4. Return correlation results with confidence scoring (exact service match > dependency match > time proximity)
5. Persist correlation links in IncidentDbContext (create `IncidentChangeCorrelation` entity if needed, or use existing IncidentNote/metadata)
6. Create endpoint: `POST /api/v1/incidents/{id}/correlate` — triggers dynamic correlation
7. Create endpoint: `GET /api/v1/incidents/{id}/correlated-changes` — returns correlated changes
8. Add unit tests for correlation logic (time-window matching, confidence scoring)

**Depends on:** Nothing (IncidentDbContext and ChangeIntelligenceDbContext both exist with migrations)
**Unblocks:** P01.3 (frontend needs real correlation API), P04.6 (deploy correlation)
**Blocked until done:** P01.3

**Files likely affected:**
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/`
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Domain/`
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/`
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.API/`

---

### P01.2 — Real incidents endpoints and persistence (backend)

**Scope:** Ensure incidents API returns real persisted data instead of seed/mock data for all critical flows.

**What to do:**
1. Verify that `EfIncidentStore` (678 lines) is the active store for all incident endpoints
2. Verify `InMemoryIncidentStore` is not referenced anywhere active — if found, remove reference
3. Ensure `GetMitigationHistory` returns real persisted MitigationRecord data instead of hardcoded
4. Ensure `CreateMitigationWorkflow` persists MitigationRecord via EfIncidentStore/IncidentDbContext
5. Ensure `RecordMitigationValidation` persists validation data instead of discarding
6. Add integration test verifying incident CRUD + mitigation persistence round-trip

**Depends on:** Nothing (EfIncidentStore exists)
**Unblocks:** P01.3 (frontend needs working API), P01.5 (mitigation workflow)
**Blocked until done:** P01.3

**Files likely affected:**
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Features/`
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/Persistence/`

---

### P01.3 — Connect incidents frontend to real API (frontend)

**Scope:** Replace `mockIncidents` in `IncidentsPage.tsx` with real API calls.

**What to do:**
1. In `IncidentsPage.tsx`: Remove `mockIncidents` hardcoded array
2. Create/update API client to call `GET /api/v1/incidents` (list), `GET /api/v1/incidents/{id}` (detail)
3. Add call to `GET /api/v1/incidents/{id}/correlated-changes` for correlation display
4. Use TanStack Query for data fetching with proper loading/error/empty states
5. Ensure i18n keys are used for all new UI text (loading messages, empty states, error messages)
6. Update `incidents.spec.ts` E2E to validate against real API fixtures instead of mock fixtures

**Depends on:** P01.1 (correlation API must exist), P01.2 (incidents API must return real data)
**Unblocks:** Gate 1 partial (incidents flow functional)
**Blocked until done:** Gate 1 cannot close without this

**Files likely affected:**
- `src/frontend/src/features/operations/pages/IncidentsPage.tsx`
- `src/frontend/src/features/operations/api/` (incident API client)
- `tests/e2e/incidents.spec.ts`

---

### P01.4 — Real runbooks via RunbookRecord persistence (backend)

**Scope:** Replace 3 hardcoded runbooks with RunbookRecord persistence.

**What to do:**
1. Locate where runbooks are currently hardcoded (3 entries in code)
2. Create/verify `CreateRunbook` handler that persists via RunbookRecord entity in IncidentDbContext
3. Create/verify `GetRunbooks` and `GetRunbookById` handlers reading from RunbookRecord
4. Create endpoints if not existing: `POST /api/v1/runbooks`, `GET /api/v1/runbooks`, `GET /api/v1/runbooks/{id}`
5. Create seed data using RunbookRecord persistence instead of hardcoded arrays
6. Add unit tests for runbook CRUD

**Depends on:** Nothing (RunbookRecord and IncidentDbContext exist)
**Unblocks:** P01.5 (mitigation can reference real runbooks), P03.6 (knowledge + runbooks)
**Blocked until done:** P01.5 (partially)

**Files likely affected:**
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Features/`
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.API/`

---

### P01.5 — Mitigation workflow persistence (backend)

**Scope:** Ensure CreateMitigationWorkflow persists data and MitigationRecord lifecycle works end-to-end.

**What to do:**
1. Fix `CreateMitigationWorkflow` handler to persist MitigationRecord in IncidentDbContext
2. Fix `RecordMitigationValidation` to persist validation results instead of discarding
3. Ensure `GetMitigationHistory` reads from persisted MitigationRecords
4. Link MitigationRecord to RunbookRecord when applicable
5. Add unit tests for mitigation workflow lifecycle (create → validate → history)

**Depends on:** P01.4 (runbook persistence should exist for linking)
**Unblocks:** Complete incidents backend flow
**Blocked until done:** Nothing directly

**Files likely affected:**
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/Features/`

---

### P01.6 — Connect AI Assistant to real LLM provider (backend)

**Scope:** Make `SendAssistantMessage` invoke a real LLM instead of returning hardcoded responses.

**What to do:**
1. In `SendAssistantMessage` handler: Replace hardcoded response generation with call to `IChatCompletionProvider` (Ollama or OpenAI depending on configuration)
2. Use `IExternalAIRoutingPort` to resolve preferred provider (already configured: Ollama default, OpenAI optional)
3. Ensure system prompt includes product context from `AiAgentRuntimeService.BuildSystemPrompt`
4. Ensure grounding context is passed (P9.5 already wired DocumentRetrievalService + DatabaseRetrievalService + TelemetryRetrievalService)
5. Handle provider errors gracefully (timeout, unavailable) with fallback error message
6. Add unit tests mocking IChatCompletionProvider to verify handler flow

**Depends on:** Nothing (P9.3 streaming, P9.4 tool execution, P9.5 grounding already implemented)
**Unblocks:** P01.7 (conversation persistence), P01.9 (grounding quality), Gate 1 partial
**Blocked until done:** P01.8 (frontend cannot show real conversations without real responses)

**Files likely affected:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Features/SendAssistantMessage/`

---

### P01.7 — AI conversation persistence and audit (backend)

**Scope:** Ensure AI conversations are persisted in AiOrchestrationDbContext and audited.

**What to do:**
1. Ensure `SendAssistantMessage` persists each conversation turn (user message + assistant response) via `IAiOrchestrationConversationRepository`
2. Create/verify `GetConversation` handler that loads persisted conversation history
3. Create/verify `ListConversations` handler for conversation list
4. Ensure token usage per conversation is tracked (for budget governance)
5. Ensure audit trail is created for each AI interaction (via AiGovernanceDbContext audit)
6. Add unit tests for conversation lifecycle (create → add turns → list → retrieve)

**Depends on:** P01.6 (real responses must exist to persist)
**Unblocks:** P01.8 (frontend can show real conversations)
**Blocked until done:** P01.8

**Files likely affected:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Orchestration/`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/`

---

### P01.8 — Connect AI Assistant frontend to real API (frontend)

**Scope:** Replace `mockConversations` in `AiAssistantPage.tsx` with real API calls.

**What to do:**
1. In `AiAssistantPage.tsx`: Remove `mockConversations` hardcoded data
2. Create/update API client for `POST /api/v1/ai/chat` (send message), `GET /api/v1/ai/conversations` (list), `GET /api/v1/ai/conversations/{id}` (get conversation)
3. Implement SSE consumption for `POST /api/v1/ai/chat/stream` (P9.3 streaming endpoint exists)
4. Use TanStack Query for conversation data fetching
5. Handle error states (LLM unavailable, rate limited, no budget) with i18n messages
6. Ensure loading states during LLM response generation

**Depends on:** P01.6 (real LLM), P01.7 (conversation persistence)
**Unblocks:** Gate 1 complete (AI assistant flow functional)
**Blocked until done:** Gate 1 cannot close without this

**Files likely affected:**
- `src/frontend/src/features/ai-hub/pages/AiAssistantPage.tsx`
- `src/frontend/src/features/ai-hub/api/`

---

### P01.9 — Implement essential ExternalAI handlers (backend)

**Scope:** Implement the 8 TODO stub handlers in ExternalAI area.

**What to do:**
1. Identify all 8 ExternalAI feature handlers marked TODO
2. Prioritize by impact: provider routing, model selection from registry, token tracking
3. Implement at minimum:
   - Provider health check handler
   - Model selection from AiGovernanceDbContext Model Registry (replace fictional `NexTrace-Internal-v1`)
   - Token usage recording per conversation
   - Provider configuration validation
4. Leave non-critical handlers (e.g., advanced multi-provider routing, provider comparison) as clearly documented stubs with `// TODO: Phase N - description`
5. Add unit tests for implemented handlers

**Depends on:** P01.6 (assistant must be functional to exercise provider routing)
**Unblocks:** P02.7 (IExternalAiModule implementation)
**Blocked until done:** P02.7

**Files likely affected:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/ExternalAI/`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/`

---

### P01.10 — Minimal grounding quality improvement (backend)

**Scope:** Improve AI grounding to retrieve real data from existing modules.

**What to do:**
1. In `DatabaseRetrievalService`: Expand beyond AIModels search — add queries to:
   - ServiceAssetRepository (Catalog) for service context
   - ChangeIntelligence entities for recent changes
   - IncidentDbContext for recent incidents (when incident context requested)
2. In `DocumentRetrievalService`: If KnowledgeDocumentRepository has data, use it for document retrieval
3. Ensure grounding failures remain silent (already implemented in P9.5)
4. Add unit tests verifying grounding data is injected into system prompt

**Depends on:** P01.6 (assistant must call grounding), P9.5 (grounding infrastructure exists)
**Unblocks:** Better AI answer quality
**Blocked until done:** Nothing directly

**Files likely affected:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Services/`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/`

---

## Phase 02 — Structural Foundations (Gate 2)

### P02.1 — Activate outbox processor for priority DbContexts

**Scope:** Enable outbox event processing beyond IdentityDbContext for the 3 highest-priority contexts.

**What to do:**
1. Examine how outbox processing works in IdentityDbContext (reference implementation)
2. Activate outbox processing for:
   - CatalogGraphDbContext (service events → cross-module notifications)
   - ChangeIntelligenceDbContext (change events → incident correlation triggers)
   - IncidentDbContext (incident events → audit, notifications)
3. Configure event consumers per context (at minimum: audit event forwarding)
4. Register processors in BackgroundWorkers/Quartz jobs
5. Add integration tests verifying outbox messages are consumed

**Depends on:** Nothing (outbox infrastructure exists in BuildingBlocks)
**Unblocks:** Cross-module event propagation, real-time correlation
**Blocked until done:** Nothing directly (but enables richer cross-module flows)

**Files likely affected:**
- `src/platform/NexTraceOne.BackgroundWorkers/`
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/`
- `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure/`
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/`

---

### P02.2 — Implement IContractsModule cross-module interface

**Scope:** Implement the IContractsModule interface so other modules can query contract data.

**What to do:**
1. Locate `IContractsModule` definition (in BuildingBlocks or Contracts project)
2. Define methods: `GetContractsByServiceAsync`, `GetContractVersionAsync`, `GetContractConsumersAsync`
3. Implement in Catalog module's infrastructure layer (reads from ContractsDbContext)
4. Register implementation in DI
5. Add unit tests for the implementation
6. Verify no circular dependencies introduced

**Depends on:** Nothing (ContractsDbContext exists with migrations and real data)
**Unblocks:** P01.10 (AI grounding can query contracts), 7 Developer Portal stubs, Governance real contract queries
**Blocked until done:** Developer Portal stubs remain stubs

**Files likely affected:**
- `src/modules/catalog/NexTraceOne.Catalog.Contracts/` or equivalent
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/`
- `src/platform/NexTraceOne.ApiHost/Program.cs` (DI registration)

---

### P02.3 — Implement IChangeIntelligenceModule cross-module interface

**Scope:** Implement the IChangeIntelligenceModule interface so other modules can query change data.

**What to do:**
1. Locate `IChangeIntelligenceModule` definition
2. Define methods: `GetRecentChangesByServiceAsync`, `GetChangeByIdAsync`, `GetChangeScoreAsync`
3. Implement in ChangeGovernance module's infrastructure layer (reads from ChangeIntelligenceDbContext)
4. Register implementation in DI
5. Add unit tests
6. Verify no circular dependencies

**Depends on:** Nothing (ChangeIntelligenceDbContext exists with full migrations and real data)
**Unblocks:** P01.1 (correlation engine can query changes properly), Governance real data, AI grounding
**Blocked until done:** P03.2 (Governance real needs change queries)

**Files likely affected:**
- `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Contracts/` or equivalent
- `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure/`

---

### P02.4 — Implement ICostIntelligenceModule cross-module interface

**Scope:** Implement the ICostIntelligenceModule interface so FinOps can access real cost data.

**What to do:**
1. Locate `ICostIntelligenceModule` definition
2. Define methods: `GetCostSnapshotByServiceAsync`, `GetCostTrendAsync`, `GetCostByTeamAsync`
3. Implement in OperationalIntelligence infrastructure (reads from CostIntelligenceDbContext)
4. Register implementation in DI
5. Add unit tests

**Depends on:** P02.8b (CostIntelligenceDbContext migration must exist)
**Unblocks:** P03.3 (FinOps real data)
**Blocked until done:** P03.3

**Files likely affected:**
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Contracts/`
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/`

---

### P02.5 — Implement IRuntimeIntelligenceModule cross-module interface

**Scope:** Implement the IRuntimeIntelligenceModule interface for service reliability data.

**What to do:**
1. Locate `IRuntimeIntelligenceModule` definition
2. Define methods: `GetServiceHealthAsync`, `GetDriftDetectionAsync`, `GetReliabilityScoreAsync`
3. Implement in OperationalIntelligence infrastructure (reads from RuntimeIntelligenceDbContext)
4. Register implementation in DI
5. Add unit tests

**Depends on:** P02.8a (RuntimeIntelligenceDbContext migration must exist)
**Unblocks:** Reliability dashboard real data, AI grounding for health data
**Blocked until done:** Nothing directly

**Files likely affected:**
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Contracts/`
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Infrastructure/`

---

### P02.6 — Implement IAiOrchestrationModule cross-module interface

**Scope:** Implement the IAiOrchestrationModule interface for AI flow orchestration.

**What to do:**
1. Locate `IAiOrchestrationModule` definition
2. Define methods: `GetConversationAsync`, `GetConversationsByServiceAsync`, `GetAgentExecutionResultAsync`
3. Implement in AIKnowledge infrastructure (reads from AiOrchestrationDbContext)
4. Register implementation in DI
5. Add unit tests

**Depends on:** P01.7 (conversation persistence must work), P02.8c (AiOrchestrationDbContext migration)
**Unblocks:** Cross-module AI conversation access, audit integration
**Blocked until done:** Nothing directly

**Files likely affected:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Contracts/`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/`

---

### P02.7 — Implement IExternalAiModule cross-module interface

**Scope:** Implement the IExternalAiModule interface for external AI provider access.

**What to do:**
1. Locate `IExternalAiModule` definition
2. Define methods: `GetAvailableProvidersAsync`, `GetProviderHealthAsync`, `RouteRequestAsync`
3. Implement in AIKnowledge infrastructure (reads from ExternalAiDbContext + provider config)
4. Register implementation in DI
5. Add unit tests

**Depends on:** P01.9 (ExternalAI handlers must be implemented), P02.8d (ExternalAiDbContext migration)
**Unblocks:** Other modules can use AI providers via governed interface
**Blocked until done:** Nothing directly

**Files likely affected:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Contracts/`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/`

---

### P02.8a — Generate migration for RuntimeIntelligenceDbContext

**Scope:** Generate EF Core migration for RuntimeIntelligenceDbContext.

**What to do:**
1. Navigate to the project containing RuntimeIntelligenceDbContext
2. Run `dotnet ef migrations add InitialCreate --context RuntimeIntelligenceDbContext` (or appropriate name)
3. Verify generated migration is correct and doesn't conflict with existing schemas
4. Verify table prefix (`ops_` or module-specific) is correct
5. Test migration applies cleanly against empty database

**Depends on:** Nothing (DbContext and ModelSnapshot exist)
**Unblocks:** P02.5 (IRuntimeIntelligenceModule needs real tables)
**Blocked until done:** P02.5

---

### P02.8b — Generate migration for CostIntelligenceDbContext

**Scope:** Generate EF Core migration for CostIntelligenceDbContext.

**What to do:**
1. Run `dotnet ef migrations add InitialCreate --context CostIntelligenceDbContext`
2. Verify migration correctness and table prefix
3. Test migration applies cleanly

**Depends on:** Nothing
**Unblocks:** P02.4 (ICostIntelligenceModule needs real tables)
**Blocked until done:** P02.4

---

### P02.8c — Generate migration for AiOrchestrationDbContext

**Scope:** Generate EF Core migration for AiOrchestrationDbContext.

**What to do:**
1. Run `dotnet ef migrations add InitialCreate --context AiOrchestrationDbContext`
2. Verify migration correctness and table prefix (`aik_` per convention)
3. Test migration applies cleanly

**Depends on:** Nothing
**Unblocks:** P02.6 (IAiOrchestrationModule needs real tables)
**Blocked until done:** P02.6 (partially)

---

### P02.8d — Generate migration for ExternalAiDbContext

**Scope:** Generate EF Core migration for ExternalAiDbContext.

**What to do:**
1. Run `dotnet ef migrations add InitialCreate --context ExternalAiDbContext`
2. Verify migration correctness and table prefix
3. Test migration applies cleanly

**Depends on:** Nothing
**Unblocks:** P02.7 (IExternalAiModule needs real tables)
**Blocked until done:** P02.7 (partially)

---

### P02.8e — Generate migration for IntegrationsDbContext

**Scope:** Generate EF Core migration for IntegrationsDbContext.

**What to do:**
1. Run `dotnet ef migrations add InitialCreate --context IntegrationsDbContext`
2. Verify migration correctness and table prefix (`int_`)
3. Test migration applies cleanly

**Depends on:** Nothing
**Unblocks:** P04.4 (CI/CD connector needs real tables)
**Blocked until done:** P04.4

---

### P02.8f — Generate migration for KnowledgeDbContext

**Scope:** Generate EF Core migration for KnowledgeDbContext.

**What to do:**
1. Run `dotnet ef migrations add InitialCreate --context KnowledgeDbContext`
2. Verify migration correctness and table prefix (`knw_`)
3. Verify entities: KnowledgeDocument, OperationalNote, KnowledgeRelation
4. Test migration applies cleanly

**Depends on:** Nothing (P10.1-P10.3 created entities and DbContext)
**Unblocks:** P03.5 (Knowledge Hub real persistence), P03.6 (runbooks + knowledge)
**Blocked until done:** P03.5

---

### P02.8g — Generate migration for ProductAnalyticsDbContext

**Scope:** Generate EF Core migration for ProductAnalyticsDbContext.

**What to do:**
1. Run `dotnet ef migrations add InitialCreate --context ProductAnalyticsDbContext`
2. Verify migration correctness and table prefix (`pan_`)
3. Test migration applies cleanly

**Depends on:** Nothing
**Unblocks:** ProductAnalytics module real data
**Blocked until done:** Nothing directly (lower priority)

---

## Phase 03 — Governance, FinOps & Knowledge Real (Gate 3)

### P03.1 — Replace mock handlers for Teams/Domains in Governance

**Scope:** Replace IsSimulated handlers for Teams and Domains with real cross-module data consumption.

**What to do:**
1. Identify handlers in Governance.Application for Teams and Domains that return `IsSimulated: true`
2. Replace with calls to real data sources:
   - Teams: Query IdentityDbContext (teams exist in Identity module) via cross-module interface or direct read
   - Domains: If domains are defined in Catalog (ServiceAsset domains), query via IContractsModule or catalog read
3. Remove `IsSimulated: true` from affected responses
4. Update DemoBanner logic in frontend to not show for Teams/Domains if data is real
5. Add unit tests verifying real data return

**Depends on:** P02.2 (IContractsModule for domain data), P02.3 (IChangeIntelligenceModule for change-related governance data)
**Unblocks:** P03.2 (broader governance real)
**Blocked until done:** Nothing directly

**Files likely affected:**
- `src/modules/governance/NexTraceOne.Governance.Application/`

---

### P03.2 — Replace mock handlers in Governance with real cross-module consumption

**Scope:** Systematically replace remaining `IsSimulated: true` handlers in Governance that can be fed by real data.

**What to do:**
1. Inventory all 74 Governance handlers with `IsSimulated: true`
2. Classify each as:
   - **CAN_REPLACE_NOW:** Data exists in another module (Catalog, ChangeGovernance, Identity, Audit)
   - **NEEDS_INTERFACE:** Requires a cross-module interface not yet implemented
   - **NEEDS_OWN_PERSISTENCE:** Requires Governance-specific persistence
3. Replace CAN_REPLACE_NOW handlers first (read from real modules)
4. For NEEDS_OWN_PERSISTENCE: Add persistence layer to GovernanceDbContext if needed
5. Leave NEEDS_INTERFACE handlers with clear `// TODO: Requires IXxxModule — see P02.x`
6. Track % of handlers converted from mock to real

**Depends on:** P03.1 (Teams/Domains first), P02.2, P02.3 (cross-module interfaces)
**Unblocks:** P03.4 (IsSimulated removal)
**Blocked until done:** P03.4

**Files likely affected:**
- `src/modules/governance/NexTraceOne.Governance.Application/Features/`

---

### P03.3 — Connect FinOps to CostIntelligence real data

**Scope:** Replace FinOps mock data with real CostIntelligence module data.

**What to do:**
1. Identify FinOps-related handlers (in Governance or OperationalIntelligence) returning `IsSimulated: true`
2. Replace with calls to `ICostIntelligenceModule` (P02.4)
3. If CostIntelligence has no real data yet, create seed data via CostSnapshot entities
4. Ensure FinOps frontend pages receive real data (remove `IsSimulated` flag from responses)
5. Ensure FinOps contextualizes cost by: service, team, environment
6. Add unit tests

**Depends on:** P02.4 (ICostIntelligenceModule implemented), P02.8b (migration generated)
**Unblocks:** FinOps product pillar functional
**Blocked until done:** Nothing directly

**Files likely affected:**
- `src/modules/governance/NexTraceOne.Governance.Application/Features/` (FinOps handlers)
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/`

---

### P03.4 — Remove IsSimulated only where real implementation exists

**Scope:** Remove `IsSimulated: true` flags only from handlers that have been converted to real data.

**What to do:**
1. After P03.1, P03.2, P03.3: Inventory which handlers now return real data
2. Remove `IsSimulated: true` from those handlers' responses
3. Update DemoBanner logic in frontend — only show for still-simulated areas
4. **DO NOT** remove IsSimulated from handlers that still return mock data
5. Document remaining simulated handlers with clear gap reference

**Depends on:** P03.2 (handlers must be real first)
**Unblocks:** Frontend shows real governance data without demo banner where applicable
**Blocked until done:** Nothing

---

### P03.5 — Activate Knowledge Hub with real persistence

**Scope:** Connect Knowledge Hub endpoints to persisted data via KnowledgeDbContext.

**What to do:**
1. Verify KnowledgeDbContext migration exists (P02.8f)
2. Ensure CreateKnowledgeDocument, CreateOperationalNote, CreateKnowledgeRelation handlers persist to database
3. Ensure Get handlers read from database
4. Add search capability via PostgreSQL FTS (aligned with P10.2 approach)
5. Add unit tests for CRUD operations with persistence

**Depends on:** P02.8f (KnowledgeDbContext migration)
**Unblocks:** P03.6 (runbooks linked to knowledge), AI grounding from knowledge
**Blocked until done:** P03.6

**Files likely affected:**
- `src/modules/knowledge/NexTraceOne.Knowledge.Application/`
- `src/modules/knowledge/NexTraceOne.Knowledge.Infrastructure/`

---

### P03.6 — Link runbooks and operational knowledge to correct storage

**Scope:** Ensure runbooks are stored via Knowledge Hub and linked to incidents via KnowledgeRelation.

**What to do:**
1. Runbooks created via P01.4 (RunbookRecord in IncidentDbContext) — these are incident-specific runbooks
2. Create/verify KnowledgeRelation linking runbooks to services and incident types
3. Ensure operational notes can reference runbooks
4. Ensure AI grounding can retrieve runbook content for incident investigation context
5. Add unit tests for runbook-knowledge linking

**Depends on:** P01.4 (runbook persistence), P03.5 (Knowledge Hub active)
**Unblocks:** Complete operational knowledge flow
**Blocked until done:** Nothing directly

**Files likely affected:**
- `src/modules/knowledge/NexTraceOne.Knowledge.Application/`
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/`

---

## Phase 04 — Integrations and Observability (Gate 4 partial)

### P04.1 — Configure OTEL endpoint per environment

**Scope:** Replace hardcoded `localhost:4317` OTEL endpoint with environment-configurable value.

**What to do:**
1. In `appsettings.json`: Replace `http://localhost:4317` with placeholder requiring explicit configuration
2. In `appsettings.Development.json`: Keep `http://localhost:4317` for local dev
3. In `.env.example`: Add `OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317` with comment for production override
4. In startup code: Read OTEL endpoint from configuration/env var
5. Verify OTEL collector config in `build/otel-collector/` references correct endpoint

**Depends on:** Nothing
**Unblocks:** P04.2 (pipeline validation needs correct endpoint)
**Blocked until done:** P04.2

**Files likely affected:**
- `src/platform/NexTraceOne.ApiHost/appsettings.json`
- `src/platform/NexTraceOne.ApiHost/appsettings.Development.json`
- `.env.example`
- Startup OTEL configuration code

---

### P04.2 — Validate OTEL → Collector → ClickHouse pipeline

**Scope:** Verify the telemetry pipeline works end-to-end in Docker Compose environment.

**What to do:**
1. Start full Docker Compose stack (PostgreSQL, ClickHouse, OTEL Collector, ApiHost)
2. Generate traces via API calls (e.g., catalog list, change create)
3. Verify traces arrive in OTEL Collector (check collector logs)
4. Verify data is written to ClickHouse tables (query ClickHouse directly)
5. Document pipeline validation results
6. Fix any pipeline breaks discovered
7. If ClickHouse destination not working: fix `otel-collector.yaml` exporter config

**Depends on:** P04.1 (OTEL endpoint configured)
**Unblocks:** Observability pillar validated
**Blocked until done:** Nothing directly (informational validation)

**Files likely affected:**
- `build/otel-collector/otel-collector.yaml`
- `build/clickhouse/` (if schema fixes needed)
- `docker-compose.yml` / `docker-compose.override.yml`

---

### P04.3 — Semantic processing for Ingestion API

**Scope:** Make the Ingestion API process payloads semantically instead of just recording metadata.

**What to do:**
1. In Ingestion.Api: Identify the 5 endpoints that record `processingStatus: "metadata_recorded"`
2. For deploy/change events: Parse payload, extract service, environment, version, timestamp
3. Persist semantic data to ChangeIntelligenceDbContext or create canonical event in IncidentDbContext
4. Update `processingStatus` to `"processed"` on success, `"processing_failed"` on error
5. Add validation for required payload fields
6. Add unit tests for payload parsing and persistence

**Depends on:** Nothing (Ingestion.Api endpoints exist)
**Unblocks:** P04.5 (canonical deploy event model), P04.6 (deploy correlation)
**Blocked until done:** P04.5

**Files likely affected:**
- `src/platform/NexTraceOne.Ingestion.Api/`

---

### P04.4 — Implement first real CI/CD connector (GitHub Actions)

**Scope:** Create the first real integration connector for GitHub Actions.

**What to do:**
1. In Integrations module: Create `GitHubActionsConnector` implementation
2. Define connector configuration: repository URL, access token, webhook secret
3. Implement webhook receiver for GitHub Actions workflow events (workflow_run, deployment)
4. Transform GitHub events to canonical deploy event format
5. Persist connector configuration via IntegrationsDbContext
6. Register connector in IntegrationHub
7. Add unit tests for event transformation

**Depends on:** P02.8e (IntegrationsDbContext migration)
**Unblocks:** P04.6 (deploy correlation needs real deploy events)
**Blocked until done:** Nothing directly

**Files likely affected:**
- `src/modules/integrations/NexTraceOne.Integrations.Application/`
- `src/modules/integrations/NexTraceOne.Integrations.Infrastructure/`
- `src/modules/integrations/NexTraceOne.Integrations.API/`

---

### P04.5 — Canonical deploy event model

**Scope:** Define and implement a canonical model for deploy/release events from any CI/CD source.

**What to do:**
1. Define `CanonicalDeployEvent` entity with: service, environment, version, timestamp, source (GitHub/GitLab/Jenkins/etc.), commit SHA, actor, status
2. Place in shared Contracts or OperationalIntelligence domain
3. Create mapper from GitHub Actions webhook to canonical format
4. Create mapper from Ingestion API payload to canonical format
5. Persist canonical events
6. Add unit tests for canonical event creation and mapping

**Depends on:** P04.3 (ingestion processing) or P04.4 (connector)
**Unblocks:** P04.6 (correlation uses canonical events)
**Blocked until done:** P04.6

**Files likely affected:**
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Domain/`
- `src/modules/integrations/NexTraceOne.Integrations.Application/`

---

### P04.6 — Correlate deploys with releases and changes

**Scope:** Link canonical deploy events to Change Intelligence releases and change records.

**What to do:**
1. When a canonical deploy event arrives, find matching release in ChangeIntelligenceDbContext by service + version
2. Link deploy to release/change record
3. Update change record state (e.g., "deployed to {environment}")
4. Trigger post-change verification window opening
5. If no matching change record: create alert/notification for "untracked deploy"
6. Add unit tests for deploy-change linking

**Depends on:** P04.5 (canonical deploy events), P01.1 (correlation engine), P02.3 (IChangeIntelligenceModule)
**Unblocks:** Full change lifecycle tracking (code → PR → deploy → verify → incident correlation)
**Blocked until done:** Nothing directly

**Files likely affected:**
- `src/modules/operationalintelligence/NexTraceOne.OperationalIntelligence.Application/`
- `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Application/`

---

## Phase 05 — Quality, Gates and Hardening (Gate 4)

### P05.1 — Make E2E tests a mandatory gate on PRs

**Scope:** Configure CI pipeline so E2E tests block PR merges.

**What to do:**
1. In `.github/workflows/ci.yml`: Add E2E test job as required check
2. Configure Playwright to run core E2E specs (catalog, changes, auth, incidents) on PR
3. Ensure Docker Compose services start for E2E (PostgreSQL, API)
4. Set timeout appropriate for E2E (e.g., 15 minutes)
5. Configure GitHub branch protection to require E2E job passes
6. Document E2E as required gate in CONTRIBUTING.md or equivalent

**Depends on:** P01.3 (incidents E2E must use real fixtures), P01.8 (AI E2E must use real fixtures)
**Unblocks:** Gate 4 partial (quality assurance)
**Blocked until done:** Nothing directly

**Files likely affected:**
- `.github/workflows/ci.yml`
- `.github/workflows/e2e.yml` (may merge into ci.yml)

---

### P05.2 — Add code coverage gates

**Scope:** Track and enforce minimum code coverage thresholds.

**What to do:**
1. Add coverage collection to backend tests (e.g., `coverlet` or `dotnet-coverage`)
2. Add coverage collection to frontend tests (Vitest coverage)
3. Configure coverage threshold: enforce 60% minimum for backend, 50% for frontend as initial mandatory gate (adjust upward as codebase matures)
4. Add coverage report upload to CI (e.g., Codecov or GitHub artifact)
5. Configure CI to fail if coverage drops below threshold
6. Document coverage policy

**Depends on:** Nothing
**Unblocks:** Quality confidence improvement
**Blocked until done:** Nothing directly

**Files likely affected:**
- `.github/workflows/ci.yml`
- Backend test project configurations
- `vite.config.ts` or `vitest.config.ts`

---

### P05.3 — Reduce critical CS8632 nullable warnings

**Scope:** Address the 516 CS8632 nullable reference warnings.

**What to do:**
1. Enable `<Nullable>enable</Nullable>` progressively (if not already enabled)
2. Focus on modules with public API surfaces first: Catalog, ChangeGovernance, IdentityAccess
3. Add `?` nullable annotations where appropriate
4. Add null guards where needed
5. Target: Reduce from 516 to <100 warnings in core modules
6. Do NOT change behavior — only add type annotations

**Depends on:** Nothing
**Unblocks:** Cleaner compilation, fewer false positives in analysis
**Blocked until done:** Nothing directly

**Files likely affected:**
- Multiple `.cs` files across core modules

---

### P05.4 — Stricter smoke checks in staging pipeline

**Scope:** Make post-deploy smoke checks more rigorous.

**What to do:**
1. In staging pipeline: Add checks beyond `/live` and `/ready`:
   - Check each module's status endpoint returns 200
   - Check database connectivity (PostgreSQL)
   - Check at least 1 API endpoint per core module (catalog list, changes list, identity health)
2. Make smoke check failures block deployment (not optional)
3. Add timeout per check (5 seconds max)
4. Document smoke check inventory

**Depends on:** Nothing
**Unblocks:** Deployment confidence
**Blocked until done:** Nothing directly

**Files likely affected:**
- `scripts/smoke-check.sh`
- `.github/workflows/staging.yml`

---

### P05.5 — Validate complete migrations in pipeline

**Scope:** Ensure CI validates that all DbContexts with migrations can apply cleanly.

**What to do:**
1. Add CI step that runs `dotnet ef database update` for each DbContext against a fresh database
2. Use Testcontainers or Docker PostgreSQL for migration validation
3. Fail CI if any migration fails to apply
4. Include all 15+ confirmed DbContexts + newly generated ones (P02.8a-g)
5. Document migration validation process

**Depends on:** P02.8a-g (new migrations generated)
**Unblocks:** Migration confidence for deployments
**Blocked until done:** Nothing directly

**Files likely affected:**
- `.github/workflows/ci.yml`
- `scripts/apply-migrations.sh` (may need updates for validation mode)

---

### P05.6 — Move API Keys from appsettings to secure storage

**Scope:** Migrate API Key configuration from appsettings.json to encrypted database storage.

**What to do:**
1. Create `ApiKeyStore` entity in IdentityDbContext (or ConfigurationDbContext)
2. Implement encrypted storage using existing `[EncryptedField]` infrastructure (AES-256-GCM)
3. Create admin endpoint for API Key management: `POST /api/v1/admin/api-keys`, `DELETE /api/v1/admin/api-keys/{id}`
4. Update `ApiKeyAuthenticationHandler` to read from database instead of appsettings
5. Keep appsettings as fallback for bootstrap/initial setup only
6. Add migration for new entity
7. Add unit tests

**Depends on:** Nothing
**Unblocks:** Security hardening
**Blocked until done:** Nothing directly

**Files likely affected:**
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Domain/`
- `src/modules/identityaccess/NexTraceOne.IdentityAccess.Infrastructure/`
- `src/platform/NexTraceOne.BuildingBlocks.Security/`

---

## Phase 06 — Cleanup, Archival and Consolidation (Gate 5)

### P06.1 — Archive historical execution documentation

**Scope:** Move historical execution and review documents to archive.

**What to do:**
1. Move `docs/architecture/e14-*` through `docs/architecture/e18-*` to `docs/archive/historical-execution/`
2. Move `docs/architecture/p0-*` through `docs/architecture/p1-*` to `docs/archive/historical-security/`
3. Move `docs/architecture/n-trail-*` to `docs/archive/historical-audits/`
4. If `docs/11-review-modular/` still exists outside archive: Move to `docs/archive/historical-reviews/`
5. Update any references in DOCUMENTATION-INDEX.md
6. Preserve internal structure within archive folders

**Depends on:** P00.2 (CURRENT-STATE.md must exist as replacement)
**Unblocks:** Cleaner documentation navigation
**Blocked until done:** Nothing directly

**Files likely affected:**
- `docs/architecture/` (move files)
- `docs/archive/` (receive files)
- `docs/DOCUMENTATION-INDEX.md`

---

### P06.2 — Consolidate duplicate documentation

**Scope:** Merge overlapping documents into single authoritative versions.

**What to do:**
1. Consolidate `IMPLEMENTATION-STATUS.md` + `REBASELINE.md` into single `CURRENT-STATE.md` (or keep REBASELINE as authoritative and deprecate IMPLEMENTATION-STATUS)
2. Consolidate `ROADMAP.md` + `POST-PR16-EVOLUTION.md` + `PRODUCT-REFOUNDATION.md` into single `ROADMAP.md`
3. Consolidate duplicate ADR references (if any `adr/` and `ADR-*.md` overlap)
4. Remove superseded documents or redirect to canonical version
5. Update DOCUMENTATION-INDEX.md

**Depends on:** P00.1 (contradictions corrected first)
**Unblocks:** Single source of truth for documentation
**Blocked until done:** Nothing directly

**Files likely affected:**
- `docs/` (multiple markdown files)
- `docs/DOCUMENTATION-INDEX.md`

---

### P06.3 — Remove residues of Commercial Governance

**Scope:** Clean up any remaining references to the removed Commercial Governance module (PR-17).

**What to do:**
1. Search codebase for "Commercial Governance", "CommercialGovernance", "commercial_governance"
2. Remove/update any references found in:
   - Documentation
   - Comments in code
   - Configuration
   - Test references
3. Verify no orphaned imports or namespace references exist
4. Document the removal context in an ADR if not already done (ADR for PR-17 Commercial Governance removal)

**Depends on:** Nothing
**Unblocks:** Clean codebase
**Blocked until done:** Nothing directly

---

### P06.4 — Verify and remove InMemoryIncidentStore residue

**Scope:** Confirm InMemoryIncidentStore is not referenced anywhere active.

**What to do:**
1. Search for `InMemoryIncidentStore` across entire codebase
2. If found in active code: Remove reference, ensure EfIncidentStore is used instead
3. If found only in tests or archived: Acceptable, but add comment
4. If found in DI registration: Remove and ensure only EfIncidentStore is registered
5. Verify no other in-memory stores exist for critical data paths

**Depends on:** P01.2 (EfIncidentStore confirmed as active)
**Unblocks:** Confidence that persistence is real
**Blocked until done:** Nothing directly

---

### P06.5 — Consolidate roadmap and implementation status

**Scope:** Create a single authoritative roadmap aligned to audit findings.

**What to do:**
1. Use `REBASELINE.md` as the factual base
2. Use `prioritized-remediation-roadmap.md` as the gap/priority base
3. Create or update `ROADMAP.md` with:
   - Current state by module (% real vs mock)
   - Priority gaps ordered by Gate 1 → Gate 5
   - Clear dependency chain
   - Target timeline estimates
4. Archive old roadmap versions

**Depends on:** P00.1 (contradictions corrected), significant Phase 01-03 progress for accuracy
**Unblocks:** Clear project direction
**Blocked until done:** Nothing directly

---

### P06.6 — Cleanup replaced mocks

**Scope:** Remove mock implementations that have been superseded by real implementations.

**What to do:**
1. After Gate 1-3 completion: Inventory all `IsSimulated: true` handlers that were replaced
2. Remove dead mock code only where real implementation is confirmed working
3. Remove `mockIncidents` from frontend (if not done in P01.3)
4. Remove `mockConversations` from frontend (if not done in P01.8)
5. Remove any unused mock data generators
6. Run full test suite to confirm no regressions

**Depends on:** Gate 3 (mocks must be replaced before removal)
**Unblocks:** Gate 5 closure
**Blocked until done:** Gate 5

---

## Dependency Graph Summary

```
Phase 00 (Docs)
  P00.1 ──┬── P00.2 ──── P06.1
           └── P00.3

Phase 01 (Core Flows) ─── GATE 1
  P01.1 ────── P01.3 ─┐
  P01.2 ────── P01.3 ─┤
  P01.4 ────── P01.5  │
  P01.6 ──┬── P01.7 ──┤── P01.8 ──── GATE 1 CLOSED
           └── P01.9   │
  P01.10 (parallel)    │

Phase 02 (Foundations) ── GATE 2
  P02.1 (parallel)
  P02.2 ──── P03.1
  P02.3 ──── P03.2
  P02.4 ──── P03.3 (needs P02.8b)
  P02.5 (needs P02.8a)
  P02.6 (needs P02.8c)
  P02.7 (needs P01.9, P02.8d)
  P02.8a-g (parallel, independent)

Phase 03 (Real Data) ─── GATE 3
  P03.1 ──── P03.2 ──── P03.4
  P03.3 (needs P02.4)
  P03.5 (needs P02.8f) ── P03.6 (needs P01.4)

Phase 04 (Integrations)
  P04.1 ──── P04.2
  P04.3 ──── P04.5 ──── P04.6
  P04.4 (needs P02.8e)
  P04.5 ──── P04.6

Phase 05 (Quality) ───── GATE 4
  P05.1 (needs P01.3, P01.8)
  P05.2-P05.6 (parallel, independent)

Phase 06 (Cleanup) ───── GATE 5
  P06.1 (needs P00.2)
  P06.2 (needs P00.1)
  P06.3-P06.4 (independent)
  P06.5 (needs P00.1)
  P06.6 (needs Gate 3) ── GATE 5 CLOSED
```

---

## Execution Order (Optimal Sequence)

### Sprint 0 (Immediate — 1-2 days)
1. **P00.1** — Fix doc contradictions
2. **P00.3** — Align CORE-FLOW-GAPS

### Sprint 1 (Gate 1 — 1-2 weeks)
3. **P01.1** — Incident↔change correlation engine (backend)
4. **P01.2** — Real incidents endpoints persistence (backend)
5. **P01.4** — Real runbooks persistence (backend)
6. **P01.6** — AI Assistant → real LLM (backend)
7. **P01.5** — Mitigation workflow persistence (backend) — after P01.4
8. **P01.7** — AI conversation persistence (backend) — after P01.6
9. **P01.9** — ExternalAI essential handlers (backend)
10. **P01.10** — Grounding quality improvement (backend)
11. **P01.3** — Incidents frontend → real API — after P01.1 + P01.2
12. **P01.8** — AI Assistant frontend → real API — after P01.6 + P01.7
→ **GATE 1 CHECKPOINT**

### Sprint 2 (Gate 2 — 1-2 weeks)
13. **P02.8a** — Migration: RuntimeIntelligenceDbContext
14. **P02.8b** — Migration: CostIntelligenceDbContext
15. **P02.8c** — Migration: AiOrchestrationDbContext
16. **P02.8d** — Migration: ExternalAiDbContext
17. **P02.8e** — Migration: IntegrationsDbContext
18. **P02.8f** — Migration: KnowledgeDbContext
19. **P02.8g** — Migration: ProductAnalyticsDbContext
20. **P02.1** — Outbox processor activation (3 priority DbContexts)
21. **P02.2** — IContractsModule implementation
22. **P02.3** — IChangeIntelligenceModule implementation
23. **P02.4** — ICostIntelligenceModule implementation — after P02.8b
24. **P02.5** — IRuntimeIntelligenceModule implementation — after P02.8a
25. **P02.6** — IAiOrchestrationModule implementation — after P02.8c
26. **P02.7** — IExternalAiModule implementation — after P02.8d + P01.9
→ **GATE 2 CHECKPOINT**

### Sprint 3 (Gate 3 — 1-2 weeks)
27. **P03.1** — Replace mock Teams/Domains handlers — after P02.2
28. **P03.2** — Replace remaining mock Governance handlers — after P03.1 + P02.3
29. **P03.3** — FinOps → CostIntelligence real — after P02.4
30. **P03.5** — Knowledge Hub real persistence — after P02.8f
31. **P03.6** — Link runbooks to knowledge — after P01.4 + P03.5
32. **P03.4** — Remove IsSimulated where real exists — after P03.2
→ **GATE 3 CHECKPOINT**

### Sprint 4 (Gate 4 — 1-2 weeks)
33. **P04.1** — OTEL endpoint per environment
34. **P04.2** — Validate OTEL pipeline — after P04.1
35. **P04.3** — Ingestion API semantic processing
36. **P04.4** — GitHub Actions connector — after P02.8e
37. **P04.5** — Canonical deploy event model — after P04.3 or P04.4
38. **P04.6** — Deploy ↔ release correlation — after P04.5

39. **P05.1** — E2E as PR gate — after P01.3 + P01.8
40. **P05.2** — Code coverage gates
41. **P05.3** — Reduce CS8632 warnings
42. **P05.4** — Stricter smoke checks
43. **P05.5** — Migration validation in pipeline — after P02.8a-g
44. **P05.6** — API Keys secure storage
→ **GATE 4 CHECKPOINT**

### Sprint 5 (Gate 5 — cleanup)
45. **P00.2** — CURRENT-STATE.md per module
46. **P06.1** — Archive historical docs — after P00.2
47. **P06.2** — Consolidate duplicate docs — after P00.1
48. **P06.3** — Remove Commercial Governance residues
49. **P06.4** — Remove InMemoryIncidentStore residue — after P01.2
50. **P06.5** — Consolidate roadmap — after P00.1
51. **P06.6** — Cleanup replaced mocks — after Gate 3
→ **GATE 5 CHECKPOINT — PROJECT STABILIZED**

---

## Anti-Collision Rules

1. **P01.1 and P01.2** can run in parallel (different handler areas in OperationalIntelligence)
2. **P01.6 and P01.4** can run in parallel (different modules: AIKnowledge vs OperationalIntelligence)
3. **P02.8a-g** can ALL run in parallel (independent DbContexts)
4. **P02.2 and P02.3** can run in parallel (different modules: Catalog vs ChangeGovernance)
5. **P01.3 and P01.8** should NOT run in parallel (both touch frontend feature modules and may add overlapping i18n keys in shared namespaces like `operations` and `ai-hub`, and both modify similar TanStack Query patterns in the same frontend codebase)
6. **P03.1 and P03.2** should NOT run in parallel (same Governance module handlers)
7. **P05.1-P05.6** can mostly run in parallel (different files/concerns)
8. **P06.1-P06.6** can mostly run in parallel (documentation changes don't conflict with code cleanup)

---

## Verification Checklist per Gate

### Gate 1 Verification
- [ ] `GET /api/v1/incidents` returns real persisted data (not mock)
- [ ] `GET /api/v1/incidents/{id}/correlated-changes` returns dynamic correlations
- [ ] `POST /api/v1/ai/chat` returns real LLM response (not hardcoded)
- [ ] `GET /api/v1/ai/conversations` returns persisted conversations
- [ ] IncidentsPage.tsx has no `mockIncidents` reference
- [ ] AiAssistantPage.tsx has no `mockConversations` reference
- [ ] All existing tests pass (1,447 backend + 264 frontend)

### Gate 2 Verification
- [ ] `IContractsModule` has working implementation
- [ ] `IChangeIntelligenceModule` has working implementation
- [ ] `ICostIntelligenceModule` has working implementation
- [ ] `IRuntimeIntelligenceModule` has working implementation
- [ ] Outbox events are consumed for Catalog, ChangeGovernance, OperationalIntelligence
- [ ] All 7 new migrations apply cleanly
- [ ] All existing tests pass

### Gate 3 Verification
- [ ] At least 20+ Governance handlers return real data (not IsSimulated)
- [ ] FinOps pages show real cost data
- [ ] Knowledge Hub persists and retrieves documents
- [ ] Runbooks are persisted via RunbookRecord
- [ ] All existing tests pass

### Gate 4 Verification
- [ ] E2E tests block PR merges
- [ ] OTEL endpoint configurable per environment
- [ ] At least 1 CI/CD connector processes real events
- [ ] API Keys stored in encrypted database (not appsettings)
- [ ] All existing tests pass

### Gate 5 Verification
- [ ] Historical docs moved to archive
- [ ] No duplicate roadmap/status documents
- [ ] No Commercial Governance references in active code
- [ ] No InMemoryIncidentStore in active code
- [ ] All replaced mocks removed
- [ ] Documentation reflects actual project state
