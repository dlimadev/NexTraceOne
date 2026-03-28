# AI Knowledge — Current State

**Maturity:** MEDIUM — Governance infrastructure real; Assistant E2E broken; ExternalAI all stubs
**Last verified:** March 2026 — Forensic Audit
**Source:** `docs/audit-forensic-2026-03/backend-state-report.md §AIKnowledge`, `docs/audit-forensic-2026-03/frontend-state-report.md §AIHub`

---

## DbContexts

| DbContext | Migrations | Status |
|---|---|---|
| AiGovernanceDbContext | Confirmed (with snapshot; `InitialAiGovernanceSchema` migration) | READY |
| AiOrchestrationDbContext | Snapshot only — migration not confirmed | PARTIAL |
| ExternalAiDbContext | Snapshot only — migration not confirmed | PARTIAL |

Table prefix: `aik_`

---

## Features by Area

| Area | Count | Status | Notes |
|---|---|---|---|
| AI Governance (models, policies, budgets) | 28 | PARTIAL | EF Core repos real; `SendAssistantMessage` returns hardcoded responses — no real LLM E2E |
| Model Registry | — | PARTIAL | Functional with real DbContext; some fields deferred; not connected to routing |
| AI Streaming | — | PARTIAL | `IChatCompletionProvider` with `CompleteStreamingAsync`; SSE endpoint exists; no LLM wired |
| AI Tool Execution | — | PARTIAL | `IToolRegistry`, `IToolExecutor`, `IToolPermissionValidator` implemented; 3 real tools: `list_services`, `get_service_health`, `list_recent_changes`; `MaxToolIterations=5` |
| AI Grounding / Context | — | PARTIAL | Context assembly wired (`DocumentRetrievalService`, `DatabaseRetrievalService`, `TelemetryRetrievalService`); DB retrieval only searches AIModels — no cross-module entity lookup |
| AI Orchestration | — | PARTIAL | `AiOrchestrationDbContext` + repos exist (P9.2); `IAiOrchestrationModule` = PLAN (empty interface) |
| External AI | 8 | STUB | All 8 handlers have `TODO`; `IExternalAiModule` = PLAN (empty interface); OpenAI disabled by default |

---

## Frontend Pages (12 pages — PARTIAL/MOCK)

| Page | Status |
|---|---|
| AiAssistantPage | MOCK — `mockConversations` hardcoded, not connected to real API |
| AssistantPanel | PARTIAL — API with mock fallback |
| AiAnalysisPage, ModelRegistryPage, AiPoliciesPage, TokenBudgetPage | PARTIAL — API real (backend data partial/mock) |
| AiAuditPage, AiAgentsPage, AgentDetailPage, AiRoutingPage | PARTIAL — API real |
| AiIntegrationsConfigurationPage, IdeIntegrationsPage | PARTIAL — API real |

---

## Key Gaps (Critical)

- **AI Assistant E2E broken** — `AiAssistantPage` uses `mockConversations`; `SendAssistantMessage` returns hardcoded responses
- **No real LLM integration** — Ollama configured at `localhost:11434` (`qwen3.5:9b`) but not wired end-to-end
- **ExternalAI 8 stubs** — `IExternalAiModule` empty; OpenAI disabled
- **IAiOrchestrationModule** — empty interface; orchestration not callable cross-module
- **AiOrchestrationDbContext / ExternalAiDbContext** — no confirmed deployable migrations
- **Grounding** — context assembled but no cross-module entity retrieval (not validated E2E)

---

## Cross-Module Interface Status

| Interface | Status |
|---|---|
| `IAiOrchestrationModule` | PLAN — empty interface |
| `IExternalAiModule` | PLAN — empty interface |

---

*Source: `docs/audit-forensic-2026-03/backend-state-report.md`, `docs/audit-forensic-2026-03/final-project-state-assessment.md §Fluxo 4`*
