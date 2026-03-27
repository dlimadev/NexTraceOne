# P9.5 — AI Grounding/Context Assembly Report

## Objective

Close the grounding/context assembly gap in the Assistant and agents by:
1. Wiring retrieval services into the SendAssistantMessage pipeline
2. Injecting contextJson into the agent runtime system prompt
3. Removing dead stub code

---

## Previous State (Before P9.5)

| Component | Status |
|---|---|
| **SendAssistantMessage** | ✅ Accepts ContextBundle from frontend, builds grounding context string — BUT ⚠️ never calls retrieval services |
| **AiAgentRuntimeService** | ✅ Accepts `contextJson` parameter — BUT ⚠️ only stores it in execution record, never injects into system prompt |
| **DocumentRetrievalService** | ✅ Real implementation (searches KnowledgeSources) — BUT ⚠️ never called from chat pipeline |
| **DatabaseRetrievalService** | ✅ Real implementation (searches AIModels) — BUT ⚠️ never called from chat pipeline |
| **TelemetryRetrievalService** | ✅ Real implementation (queries IObservabilityProvider) — BUT ⚠️ never called from chat pipeline |
| **AiAssistantPage** | ✅ Sends contextScope toggles — connected, affects use case classification and source resolution |
| **AssistantPanel** | ✅ Sends contextBundle with rich entity data — fully wired |
| **GenerateStubResponse** | ⚠️ Dead code — bypassed by real provider calls since P9.1 |

**Key gaps:** Retrieval services existed but were disconnected from the main pipeline; agents ignored grounding context.

---

## Changes Implemented

### 1. SendAssistantMessage — Retrieval Service Integration

**File:** `Application/Governance/Features/SendAssistantMessage/SendAssistantMessage.cs`

**Constructor expanded:** Added three retrieval services as dependencies:
- `IDocumentRetrievalService documentRetrievalService`
- `IDatabaseRetrievalService databaseRetrievalService`
- `ITelemetryRetrievalService telemetryRetrievalService`

**New method:** `AugmentWithRetrievalAsync` — called after building base grounding context, before sending to provider:

1. **Document retrieval** — Always called. Searches KnowledgeSources for relevant documents matching the user's query. Results appended as `RetrievedDocuments:` section.

2. **Database retrieval** — Always called. Searches structured data (AIModels, with entity type filter by use case: Contract, Service). Results appended as `RetrievedData:` section.

3. **Telemetry retrieval** — Only called for operational use cases (`IncidentExplanation`, `MitigationGuidance`, `ChangeAnalysis`, `FinOpsExplanation`). Queries logs/traces via IObservabilityProvider. Results appended as `RetrievedTelemetry:` section.

All three retrievals use **silent failure** — exceptions are caught and logged at Debug level without interrupting the pipeline. This ensures the grounding enrichment is best-effort and never blocks the user.

**Grounding context format (augmented):**
```
Persona: Engineer
UseCase: ChangeAnalysis
ContextScope: Changes,Services
GroundingSources: Change Intelligence, Service Catalog
EntityType: change
EntityName: deploy-v2.3
...

--- Retrieved Context ---
RetrievedDocuments:
  - [Documentation] Deployment Runbook: Standard deployment procedure
RetrievedData:
  - [AIModel] GPT-4: AI Model 'gpt-4' from provider 'openai' — text-generation
RetrievedTelemetry:
  - [Error] payment-service @ 2026-03-27T14:00:00Z: Connection timeout during health check
```

### 2. AiAgentRuntimeService — Context Injection

**File:** `Application/Governance/Services/AiAgentRuntimeService.cs`

**`BuildSystemPrompt` method expanded** to accept `string? contextJson` parameter.

When `contextJson` is provided (non-null, non-empty), it is injected into the system prompt as a dedicated grounding section:

```
[Agent's SystemPrompt]

Objective: [Agent's Objective]

Expected output format: [Agent's OutputSchema]

## Grounding Context
The following operational context has been provided for this execution.
Use it to ground your response with real data. If the context is insufficient, state limitations explicitly.

[contextJson content]

## Available Tools
...
```

This means agents now receive real grounding context from callers, instead of operating with generic prompts only.

### 3. Dead Code Removal

**Removed:** `GenerateStubResponse` method — dead code since P9.1 when real provider routing was implemented. Was never called in the current pipeline but remained as a leftover.

---

## Files Changed/Created

| File | Action |
|---|---|
| `Application/Governance/Features/SendAssistantMessage/SendAssistantMessage.cs` | **Modified** — added 3 retrieval services to constructor, added AugmentWithRetrievalAsync, removed dead GenerateStubResponse |
| `Application/Governance/Services/AiAgentRuntimeService.cs` | **Modified** — BuildSystemPrompt now accepts and injects contextJson |
| `tests/.../Runtime/Services/GroundingContextAssemblyTests.cs` | **Created** — 9 tests for retrieval services and request defaults |
| `docs/architecture/p9-5-ai-grounding-context-assembly-report.md` | **Created** |
| `docs/architecture/p9-5-post-change-gap-report.md` | **Created** |

---

## Context Flow: End-to-End

### Assistant (Full-Screen — AiAssistantPage)
```
User selects context toggles [Services, Contracts, Changes]
  → contextScope: "Services,Contracts,Changes"
  → Handler classifies use case from query + scope
  → Resolves grounding sources by use case priority
  → Calls retrieval services (documents, database, telemetry) ← NEW
  → Builds grounding context string
  → Sends to real AI provider with grounding as system prompt
  → Returns response with full metadata trail
```

### Assistant (Embedded — AssistantPanel)
```
Detail page provides contextData (properties, relations, caveats)
  → contextBundle: JSON.stringify(contextData)
  → contextScope: mapped from contextType (service→Services, etc.)
  → Handler builds grounding context with entity data + scope
  → Calls retrieval services for additional context ← NEW
  → Sends to real AI provider
  → Returns grounded response with context strength assessment
```

### Agents (AiAgentRuntimeService)
```
Caller provides contextJson (optional)
  → Agent system prompt + objective + output schema
  → contextJson injected as ## Grounding Context section ← NEW
  → Tool definitions injected (P9.4)
  → Provider inference
  → Tool execution loop
  → Return result with grounding context audit trail
```

---

## Validation

- **Build:** ✅ 0 errors
- **Tests:** ✅ 445 tests pass (436 existing + 9 new) — 0 regressions
- **Retrieval services wired:** ✅ DocumentRetrievalService, DatabaseRetrievalService, TelemetryRetrievalService called in pipeline
- **Agent context injection:** ✅ contextJson now injected in system prompt
- **Silent failure:** ✅ Retrieval failures don't block the pipeline
- **Dead code removed:** ✅ GenerateStubResponse cleaned up
