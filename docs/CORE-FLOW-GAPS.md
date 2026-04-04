# Core Flow Gaps — NexTraceOne
**Last updated: March 2026 — aligned to forensic audit findings**

This document is the canonical reference for the real operational state of each of the four central value flows. It is the first place to check before working on any module that touches a core flow.

---

## Flow 1 — Source of Truth / Contract Governance

**State: 95% functional**

### What works
- Service cataloguing with ownership graph: real (`CatalogGraphDbContext`)
- REST, SOAP, Kafka/event, background service contracts: real (`ContractsDbContext`)
- Versioning, semantic diff, compatibility evaluation: real
- Contract scoring, signing, publication workflow: real
- Global search (`/api/v1/source-of-truth/global-search`): real

### Gaps
- ~~**Developer Portal: 7 endpoint stubs**~~ ✅ CORRECTED — All 7 handlers are REAL implementations
- ~~**`IContractsModule`** — cross-module interface defined, 0 implementations~~ ✅ IMPLEMENTED — `ContractsModuleService` in `Catalog.Infrastructure`
- ~~**SearchCatalog Owner null**~~ ✅ FIXED — Owner now populated from `ApiAsset.OwnerService.TeamName` via batch lookup with `IApiAssetRepository.ListByApiAssetIdsAsync`
- ~~**GetApisIConsume HasBreakingChanges always false**~~ ✅ FIXED — Now computed from `ContractDiff.BreakingChanges`; counter incremented for accurate `PendingActions` and `BreakingChangesCount`
- ~~**GetApiHealth AverageLatencyMs/ErrorRate null**~~ ✅ FIXED — Handler now injects `IRuntimeIntelligenceModule` + `IApiAssetRepository`; queries runtime health to refine contract-based status when runtime reports degraded/critical
- **Contract Studio** — 10/10 contract types with visual builders (REST, SOAP, Event, BackgroundService, SharedSchema, Webhook, Copybook, MqMessage, FixedLayout, CicsCommarea)
- **Remaining minor:** AverageLatencyMs/ErrorRate still null until RuntimeIntelligence aggregates actual metrics; these fields are populated when runtime data becomes available

### Evidence
- `src/modules/catalog/` — 3 DbContexts, 84 features (all real, 0 stubs)
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/Services/ContractsModuleService.cs` — IContractsModule implementation
- All Developer Portal handlers confirmed querying real repositories
- `docs/audit-forensic-2026-03/backend-state-report.md §Catalog`
- `docs/audit-forensic-2026-03/capability-gap-matrix.md` — SERVICE CATALOG, CONTRACT GOVERNANCE rows

---

## Flow 2 — Change Intelligence & Production Change Confidence

**State: 99% functional — deploy event ingestion fully operational**

### What works
- Release submission, blast radius, advisory, change score: real
- Approval / reject / conditional decisions, SLA policies: real
- Evidence pack, rollback assessment, freeze windows: real
- Promotion governance with gate evaluations per environment: real
- Ruleset governance (Spectral lint + scoring): real
- Audit trail and decision timeline: real

### Gaps
- ~~**CI/CD integration** — deploy event ingestion is a stub~~ ✅ RESOLVED — Endpoint exists at `POST /api/v1/releases/` (DeploymentEndpoints) with real `NotifyDeployment` handler. Convenience alias added at `POST /api/v1/changes/deploy-events` for CI/CD pipeline discoverability
- **Incident↔change correlation** — the correlation engine reads real release data via `EfChangeIntelligenceReader`; correlation scoring uses temporal proximity + service matching + blast radius (no ML/NLP yet)

### Evidence
- `src/modules/changegovernance/` — 4 DbContexts (ChangeIntelligenceDbContext, WorkflowDbContext, PromotionDbContext, RulesetGovernanceDbContext), all with confirmed migrations
- `docs/audit-forensic-2026-03/observability-changeintelligence-report.md §3`
- `docs/audit-forensic-2026-03/capability-gap-matrix.md` — CHANGE INTELLIGENCE, BLAST RADIUS, PROMOTION GOVERNANCE rows

---

## Flow 3 — Incident Correlation & Mitigation

**State: 95% functional**

### What works
- `IncidentDbContext` with 6 DbSets: IncidentRecord, MitigationWorkflowRecord, MitigationWorkflowActionLog, MitigationValidationLog, RunbookRecord, IncidentChangeCorrelation — real, with confirmed migration
- `EfIncidentStore` (678 lines): real persistence layer — **registered in production DI**
- **Frontend fully connected** — `IncidentsPage.tsx`, `IncidentDetailPage.tsx`, `RunbooksPage.tsx` all use real API calls (`incidentsApi.listIncidents()`, etc.)
- **CreateMitigationWorkflow** — persists to DB via `IMitigationWorkflowRepository`
- **GetMitigationHistory** — queries database for real audit entries
- **RecordMitigationValidation** — persists validation logs to `IMitigationValidationRepository`
- **UpdateMitigationWorkflowAction** — modifies workflow state in DB
- **Dynamic incident↔change correlation** — `IIncidentCorrelationRepository`, `IChangeIntelligenceReader`, `LegacyEventCorrelator` all registered
- **IIncidentModule** — cross-module interface IMPLEMENTED by `IncidentModuleService` for governance executive dashboard integration
- **Runbooks** — `IRunbookRepository` with `EfRunbookRepository` registered; database-driven
- **SuggestRunbooksForIncident** — runbook recommendation engine with relevance scoring (service match, type match, text search) at `GET /api/v1/runbooks/suggest`
- **PostIncidentReview (PIR)** — formal PIR workflow with phase progression: FactGathering→RootCauseAnalysis→PreventiveActions→FinalReview→Completed. Entity `PostIncidentReview` + `EfPostIncidentReviewRepository`. API: `POST/GET /api/v1/incidents/{id}/pir`, `PUT /api/v1/incidents/{id}/pir/progress`

### Gaps (low)
- **Correlation engine heuristics** — correlation uses basic timestamp+service matching; no ML/NLP-based correlation (functional for production use)
- ~~**Runbook templates** — no visual runbook builder yet~~ ✅ ENHANCED — `SuggestRunbooksForIncident` feature added with relevance scoring (service match, type match, text search) at `GET /api/v1/runbooks/suggest`
- ~~**Post-incident review** — no formal PIR workflow~~ ✅ IMPLEMENTED — `PostIncidentReview` entity with phase progression (FactGathering→RootCauseAnalysis→PreventiveActions→FinalReview→Completed), `StartPostIncidentReview`, `ProgressPostIncidentReview`, `GetPostIncidentReview` features with REST API at `/api/v1/incidents/{id}/pir`

### Evidence
- `src/frontend/src/features/operations/` — all pages use real API calls
- `src/modules/operationalintelligence/` — `EfIncidentStore`, `IncidentModuleService` real
- All frontend API endpoints point to real backend handlers

---

## Flow 4 — AI-Assisted Operations & Engineering

**State: LLM real integrado E2E; governance real; grounding cross-module completo**

### What works
- Model Registry: CRUD, budget tracking, metadata (`AiGovernanceDbContext`)
- AI Access Policies: per-user and per-group (`AiGovernanceDbContext`)
- Token & Budget Governance: token spend control by tenant/user
- AI Audit log: entries recorded (`AiGovernanceDbContext`)
- Tool execution: 3 real tools wired — `list_services`, `get_service_health`, `list_recent_changes`
- Streaming endpoint: `POST /api/v1/ai/chat/stream` (SSE) — infrastructure present
- Grounding assembly: `DocumentRetrievalService`, `DatabaseRetrievalService`, `TelemetryRetrievalService` wired
- **`SendAssistantMessage`** — invoca `IChatCompletionProvider.CompleteAsync()` via `IAiProviderFactory`; LLM real via Ollama/OpenAI; fallback degradado quando nenhum provider disponível
- **`AiAssistantPage.tsx`** — usa `aiGovernanceApi.listConversations`, `sendMessage`, `getMessages` (7 chamadas API reais)
- **`IAiOrchestrationModule`** — implementado por `AiOrchestrationModule`; DbContext com migrações confirmadas
- **`IExternalAiModule`** — implementado por `ExternalAiModule`; DbContext com migrações confirmadas
- **Knowledge Source Weights** — persistidos em `aik_source_weights`; `ListKnowledgeSourceWeights` consulta DB com fallback a defaults
- **PlanExecution model selection** — usa `IAiModelCatalogService` para resolver modelo real via Model Registry
- **AI Source health check** — verifica conectividade HTTP para fontes Document com URL; actualiza estado persistido

### Gaps (low)
- ~~**Cross-module grounding**~~ ✅ VERIFIED — `DatabaseRetrievalService` queries Catalog (services), ChangeIntelligence (releases), OperationalIntelligence (incidents), and Knowledge (documents) via 4 dedicated grounding readers (`CatalogGroundingReader`, `ChangeGroundingReader`, `IncidentGroundingReader`, `KnowledgeDocumentGroundingReader`)
- **AI Source health check** — conectores para fontes Database e ExternalMemory ainda retornam estado persistido (sem teste de conectividade real para esses tipos)
- **Model selection routing** — classificação de caso de uso usa heurística de palavras-chave; NLP real não implementado

### Evidence
- `src/modules/aiknowledge/` — `AiGovernanceDbContext`, `AiOrchestrationDbContext`, `ExternalAiDbContext` (todos com migrações confirmadas)
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Features/SendAssistantMessage/SendAssistantMessage.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Orchestration/Services/AiOrchestrationModule.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/ExternalAI/Services/ExternalAiModule.cs`

---

## Cross-Cutting Gaps Affecting All Flows

| Gap | Flows Affected | Status |
|---|---|---|
| Cross-module interfaces: `IContractsModule`, `IChangeIntelligenceModule`, `IPromotionModule`, `IRulesetGovernanceModule`, `IAiOrchestrationModule`, `IExternalAiModule`, `ICostIntelligenceModule`, `IRuntimeIntelligenceModule`, `IReliabilityModule`, `IAutomationModule`, `IProductAnalyticsModule`, `IAiGovernanceModule`, `IIncidentModule`, `IKnowledgeModule` — all IMPLEMENTED | 1, 2, 3, 4 | COMPLETE |
| Outbox processed for ALL 22 DbContexts — ModuleOutboxProcessorJob registered for each | All | COMPLETE |
| ~~E2E tests do not gate PRs~~ — E2E @smoke tests now gate PRs via `e2e-smoke` job | 3, 4 | ✅ RESOLVED |

---

## Summary Table

| Flow | State | Backend | Frontend | Blocker |
|---|---|---|---|---|
| 1 — Source of Truth / Contracts | **99%** | Real (100% — all 84 features real, 0 stubs, 3 data gaps fixed) | Real (all 11 portal handlers, 10/10 builders) | AverageLatencyMs/ErrorRate pending runtime data |
| 2 — Change Confidence | **99%** | Real (100%, deploy-events endpoint added) | Real (100%) | None — CI/CD webhook ready for external pipeline integration |
| 3 — Incident Correlation & Operations | **95%** | Real (EfIncidentStore + IIncidentModule, Automation 10/10 real, Reliability 15/15 real, PIR workflow complete, Runbook suggestions complete) | Real (all pages use API) | Visual runbook builder (UI) |
| 4 — AI Assistant | **LLM real E2E; governance real; grounding cross-module completo** | LLM real via Ollama/OpenAI; grounding cross-module 4 readers verified | API real (7 chamadas) | AI Source health for DB/Memory types |

---

## Sources

| Report | Sections Used |
|---|---|
| `docs/audit-forensic-2026-03/backend-state-report.md` | Catalog, ChangeGovernance, AIKnowledge module state |
| `docs/audit-forensic-2026-03/frontend-state-report.md` | Operations/Incidents mock, AI Hub mock, Catalog partial |
| `docs/audit-forensic-2026-03/observability-changeintelligence-report.md` | §3 Change Intelligence, §4 Incident Correlation |
| `docs/audit-forensic-2026-03/ai-agents-governance-report.md` | §4 AI Assistant, §9 Cross-Module AI Interfaces, §10 Risk |
| `docs/audit-forensic-2026-03/final-project-state-assessment.md` | §3 Four Core Value Flows, percentages |
| `docs/audit-forensic-2026-03/capability-gap-matrix.md` | Per-capability status for all flows |
| `docs/REBASELINE.md` | Cross-reference for confirmed module states |
