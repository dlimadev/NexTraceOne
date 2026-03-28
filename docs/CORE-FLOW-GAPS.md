# Core Flow Gaps — NexTraceOne
**Last updated: March 2026 — aligned to forensic audit findings**

This document is the canonical reference for the real operational state of each of the four central value flows. It is the first place to check before working on any module that touches a core flow.

---

## Flow 1 — Source of Truth / Contract Governance

**State: 75% functional**

### What works
- Service cataloguing with ownership graph: real (`CatalogGraphDbContext`)
- REST, SOAP, Kafka/event, background service contracts: real (`ContractsDbContext`)
- Versioning, semantic diff, compatibility evaluation: real
- Contract scoring, signing, publication workflow: real
- Global search (`/api/v1/source-of-truth/global-search`): real

### Gaps
- **Developer Portal: 7 endpoint stubs** — `SearchCatalog`, `RenderOpenApiContract`, `GetApiHealth`, `GetMyApis`, `GetApisIConsume`, `GetApiDetail`, `GetAssetTimeline` are intentional stubs awaiting `IContractsModule` implementation
- **`IContractsModule`** — cross-module interface defined, 0 implementations; blocks Developer Portal and AI grounding from reading contracts dynamically
- **Contract Studio UX** — backend complete; frontend needs polish
- **`SearchCatalog`** — stub; cross-module dependency not yet resolved

### Evidence
- `src/modules/catalog/` — 3 DbContexts, 84 features (77 real, 7 stubs)
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
- **`IChangeIntelligenceModule`** — cross-module interface = PLAN; other modules (Governance, AI) cannot query change data dynamically
- **`IPromotionModule`** and **`IRulesetGovernanceModule`** — also PLAN; inter-module promotion data unavailable
- **CI/CD integration** — deploy event ingestion is a stub; no real pipeline events consumed from GitLab, Jenkins, or GitHub Actions
- **Incident↔change correlation** — the correlation engine reads static seed data, not live change events; see Flow 3

### Evidence
- `src/modules/changegovernance/` — 4 DbContexts (ChangeIntelligenceDbContext, WorkflowDbContext, PromotionDbContext, RulesetGovernanceDbContext), all with confirmed migrations
- `docs/audit-forensic-2026-03/observability-changeintelligence-report.md §3`
- `docs/audit-forensic-2026-03/capability-gap-matrix.md` — CHANGE INTELLIGENCE, BLAST RADIUS, PROMOTION GOVERNANCE rows

---

## Flow 3 — Incident Correlation & Mitigation

**State: 0% functional**

### What works (infrastructure only)
- `IncidentDbContext` with 5 DbSets: IncidentRecord, IncidentNote, RunbookRecord, MitigationRecord — real, with confirmed migration
- `EfIncidentStore` (678 lines): real persistence layer
- Static seed data SQL (`IncidentSeedData.cs`): present

### Gaps (all functional paths are mock)
- **Frontend not connected** — `IncidentsPage.tsx` uses `mockIncidents` hardcoded inline; comment: *"Dados simulados — em produção, virão da API /api/v1/incidents"*
- **`IncidentDetailPage.tsx`** — static data; no real API call
- **Dynamic incident↔change correlation = 0%** — correlation is based on static JSON seed data, not a live engine linking incidents to changes via service + timestamp
- **Runbooks hardcoded** — 3 runbooks hardcoded in handler code; `RunbookRecord` entity exists and is unused
- **`CreateMitigationWorkflow`** — handler exists but does not persist the mitigation record; data is discarded
- **`GetMitigationHistory`** — returns fixed hardcoded data, not database records
- **`RecordMitigationValidation`** — discards data; post-change verification is not persisted
- **Mitigation and runbook UI** — stub visuals; no real API calls

### Evidence
- `src/frontend/src/features/operations/` — all pages mock
- `src/modules/operationalintelligence/` — `EfIncidentStore` real; correlation engine absent
- `docs/audit-forensic-2026-03/final-project-state-assessment.md §3 (Fluxo 3)`
- `docs/audit-forensic-2026-03/observability-changeintelligence-report.md §4`

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
| 8 cross-module interfaces (of which 6 still PLAN: `IContractsModule`, `IChangeIntelligenceModule`, `IPromotionModule`, `IRulesetGovernanceModule`, `ICostIntelligenceModule`, `IRuntimeIntelligenceModule`); `IAiOrchestrationModule` and `IExternalAiModule` now IMPLEMENTED | 1, 2, 3, 4 | PARTIAL |
| Outbox processed only for `IdentityDbContext` — 23 other DbContexts produce unprocessed domain events | All | PARTIAL |
| E2E tests do not gate PRs — incidents and AI tests use static fixtures | 3, 4 | CI gap |

---

## Summary Table

| Flow | State | Backend | Frontend | Blocker |
|---|---|---|---|---|
| 1 — Source of Truth / Contracts | **75%** | Real (91.7%) | Real (7 stubs) | `IContractsModule` PLAN |
| 2 — Change Confidence | **95%** | Real (100%) | Real (100%) | `IChangeIntelligenceModule` PLAN |
| 3 — Incident Correlation | **0%** | Infra only | Mock inline | Correlation engine absent |
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
