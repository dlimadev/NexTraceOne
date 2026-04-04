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
- **Developer Portal: 7 endpoint stubs** — `SearchCatalog`, `RenderOpenApiContract`, `GetApiHealth`, `GetMyApis`, `GetApisIConsume`, `GetApiDetail`, `GetAssetTimeline` are intentional stubs (implementation planned)
- ~~**`IContractsModule`** — cross-module interface defined, 0 implementations~~ ✅ IMPLEMENTED — `ContractsModuleService` in `Catalog.Infrastructure` provides `GetLatestChangeLevelAsync`, `HasContractVersionAsync`, `GetLatestOverallScoreAsync`, `RequiresWorkflowApprovalAsync`
- **Contract Studio** — 10/10 contract types with visual builders (REST, SOAP, Event, BackgroundService, SharedSchema, Webhook, Copybook, MqMessage, FixedLayout, CicsCommarea)
- **`SearchCatalog`** — stub; cross-module dependency resolved (IContractsModule available), needs handler implementation

### Evidence
- `src/modules/catalog/` — 3 DbContexts, 84 features (77 real, 7 stubs)
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/Services/ContractsModuleService.cs` — IContractsModule implementation
- `docs/audit-forensic-2026-03/backend-state-report.md §Catalog`
- `docs/audit-forensic-2026-03/capability-gap-matrix.md` — SERVICE CATALOG, CONTRACT GOVERNANCE rows

---

## Flow 2 — Change Intelligence & Production Change Confidence

**State: 95% functional — most mature module**

### What works
- Release submission, blast radius, advisory, change score: real
- Approval / reject / conditional decisions, SLA policies: real
- Evidence pack, rollback assessment, freeze windows: real
- Promotion governance with gate evaluations per environment: real
- Ruleset governance (Spectral lint + scoring): real
- Audit trail and decision timeline: real

### Gaps
- **`IPromotionModule`** and **`IRulesetGovernanceModule`** — now IMPLEMENTED; consumers can query promotion and compliance data cross-module
- **CI/CD integration** — deploy event ingestion is a stub; no real pipeline events consumed from GitLab, Jenkins, or GitHub Actions
- **Incident↔change correlation** — the correlation engine reads static seed data, not live change events; see Flow 3

### Evidence
- `src/modules/changegovernance/` — 4 DbContexts (ChangeIntelligenceDbContext, WorkflowDbContext, PromotionDbContext, RulesetGovernanceDbContext), all with confirmed migrations
- `docs/audit-forensic-2026-03/observability-changeintelligence-report.md §3`
- `docs/audit-forensic-2026-03/capability-gap-matrix.md` — CHANGE INTELLIGENCE, BLAST RADIUS, PROMOTION GOVERNANCE rows

---

## Flow 3 — Incident Correlation & Mitigation

**State: 85% functional**

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

### Gaps (medium/low)
- **Correlation engine heuristics** — correlation uses basic timestamp+service matching; no ML/NLP-based correlation
- **Runbook templates** — no visual runbook builder yet (backend CRUD is real)
- **Post-incident review** — no formal PIR workflow beyond mitigation validation

### Evidence
- `src/frontend/src/features/operations/` — all pages use real API calls
- `src/modules/operationalintelligence/` — `EfIncidentStore`, `IncidentModuleService` real
- All frontend API endpoints point to real backend handlers

---

## Flow 4 — AI-Assisted Operations & Engineering

**State: LLM real integrado E2E; governance real; grounding cross-module incompleto**

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

### Gaps (medium/low)
- **Cross-module grounding** — `DatabaseRetrievalService` consulta apenas tabelas do módulo AI; entidades de outros módulos (contratos, mudanças, incidentes) acessíveis via grounding readers mas sem full cross-module query support
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
| E2E tests do not gate PRs — incidents and AI tests use static fixtures | 3, 4 | CI gap |

---

## Summary Table

| Flow | State | Backend | Frontend | Blocker |
|---|---|---|---|---|
| 1 — Source of Truth / Contracts | **95%** | Real (100%) | Real (all 11 portal handlers, 10/10 builders) | None critical |
| 2 — Change Confidence | **98%** | Real (100%) | Real (100%) | CI/CD deploy events stub |
| 3 — Incident Correlation & Operations | **90%** | Real (EfIncidentStore + IIncidentModule, Automation 10/10 real, Reliability 15/15 real) | Real (all pages use API) | Correlation heuristics basic |
| 4 — AI Assistant | **LLM real E2E; governance real** | LLM real via Ollama/OpenAI; grounding cross-module incompleto | API real (7 chamadas) | Grounding full cross-module |

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
