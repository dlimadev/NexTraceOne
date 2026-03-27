# P9.5 — Post-Change Gap Report

## What Was Resolved

### Grounding Context Assembly
- ✅ **Retrieval services wired into SendAssistantMessage** — DocumentRetrievalService, DatabaseRetrievalService, TelemetryRetrievalService now called in the main chat pipeline
- ✅ **Context augmentation** — Retrieved documents, data, and telemetry appended to grounding context before provider call
- ✅ **Use case–aware retrieval** — Telemetry retrieval only triggered for operational use cases (incident, change, mitigation, finops)
- ✅ **Silent failure** — Retrieval errors caught and logged at Debug level, never blocking the pipeline

### Agent Context Injection
- ✅ **contextJson injected into system prompt** — AiAgentRuntimeService now includes grounding context in the agent's system prompt
- ✅ **Grounding section format** — Clear `## Grounding Context` header with instructions for the model
- ✅ **Backward compatible** — Empty contextJson means no grounding section (text-only prompt preserved)

### Dead Code Cleanup
- ✅ **GenerateStubResponse removed** — Dead code since P9.1, no longer needed

### Tests
- ✅ **9 new tests** — Retrieval service error handling, empty results, request defaults
- ✅ **445 total tests pass** — 0 regressions

---

## What Still Remains Pending

### Retrieval Service Expansion
1. **DatabaseRetrievalService** — Currently only searches AIModels. Needs cross-module integration to search:
   - Services (ServiceCatalog)
   - Contracts (ContractGovernance)
   - Incidents (OperationalIntelligence)
   - Changes (ChangeGovernance)
   - Runbooks (OperationalIntelligence)

2. **DocumentRetrievalService** — Searches KnowledgeSources by name/description. Needs content-level search (document bodies, not just metadata).

3. **TelemetryRetrievalService** — Depends on IObservabilityProvider which uses ClickHouse/collector. Needs real OTel collector integration for production data.

### Frontend Context
4. **AiAssistantPage** — Full-screen assistant sends `contextScope` but no `contextBundle` (entity data). This is by design since it's a general chat, not an entity-specific panel. Future improvement: allow users to pin specific entities from the full-screen assistant.

5. **Entity ID resolution** — When the assistant page sends entity IDs (serviceId, contractId, etc.), the handler includes them in the grounding context as identifiers but doesn't query the actual entities from their modules.

### Advanced Grounding
6. **Semantic search / RAG** — No vector DB or embeddings. Retrieval is keyword-based only.
7. **Dynamic context weighting** — Source weights are static by use case, not learned.
8. **Cross-module data fusion** — No direct query of ServiceCatalog, ChangeGovernance, etc. from the AI module.

---

## What Is Explicitly for Future Phases

### Near-term (P10+)
- Cross-module integration ports for retrieval services (Service, Contract, Change, Incident read-models)
- Entity ID resolution in SendAssistantMessage (query real entities by ID)
- Conversation-scoped context accumulation (context grows across turns)
- Context pinning in full-screen assistant

### Medium-term
- Vector DB integration (pgvector or dedicated)
- Embeddings for semantic document search
- Multi-hop retrieval (retrieval → analysis → follow-up retrieval)
- Knowledge Hub dedicated module

### Long-term
- RAG pipeline with document chunking and reranking
- Provider-specific context window optimization
- Context budget management (token-aware truncation)
- Real-time context streaming

---

## Residual Limitations

1. **Keyword-based retrieval** — All three retrieval services use simple string matching, not semantic search. Effective for exact matches but may miss relevant context.
2. **AIModel-only database search** — DatabaseRetrievalService queries only AIModels, not cross-module entities.
3. **Static telemetry window** — TelemetryRetrievalService defaults to last 1 hour; no dynamic window based on context.
4. **No context deduplication** — If the frontend bundle AND retrieval services return overlapping data, both are included in the grounding context.
