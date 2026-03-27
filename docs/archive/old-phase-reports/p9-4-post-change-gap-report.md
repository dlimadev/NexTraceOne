# P9.4 — Post-Change Gap Report

## What Was Resolved

### Tool Infrastructure
- ✅ **IToolRegistry** — Centralized tool catalog with name-based resolution
- ✅ **IToolExecutor** — Execution with timing, logging, and exception handling
- ✅ **IToolPermissionValidator** — AllowedTools CSV validation against registry
- ✅ **IAgentTool** — Individual tool contract for implementations
- ✅ **InMemoryToolRegistry** — Singleton, case-insensitive, category-aware
- ✅ **AgentToolExecutor** — Scoped executor with structured logging
- ✅ **AllowedToolsPermissionValidator** — CSV parsing with whitespace handling

### Real Tools (3 of 3 target)
- ✅ **list_services** — Service catalog query tool (service_catalog category)
- ✅ **get_service_health** — Service health status tool (operational_intelligence category)
- ✅ **list_recent_changes** — Recent changes/deploys tool (change_intelligence category)

### AiAgentRuntimeService
- ✅ **AllowedTools enforcement** — Agent's AllowedTools CSV controls which tools can execute
- ✅ **Tool call detection** — `[TOOL_CALL: name({args})]` convention parsed from model output
- ✅ **Tool execution loop** — Up to 5 iterations per execution, with re-inference after tool results
- ✅ **System prompt enrichment** — Tool descriptions injected when tools are available
- ✅ **Execution steps audit** — Tool results serialized in AiAgentExecution.Steps JSON
- ✅ **ToolExecutionSummary** — Returned in AgentExecutionResult for API consumers

### Error Catalog
- ✅ **ToolNotAllowedForAgent** — Forbidden error for unauthorized tool access
- ✅ **ToolNotFound** — NotFound error for unregistered tools
- ✅ **ToolExecutionFailed** — Business error for tool execution failures

### Tests
- ✅ **26 new tests** — Registry, executor, permission validator, and real tool tests
- ✅ **436 total tests pass** — 0 regressions

---

## What Still Remains Pending

### Tool Data Realism
1. **Cross-module integration ports** — The 3 tools execute real code but don't yet query other modules' databases. They need integration ports to ServiceCatalog, OperationalIntelligence, and ChangeGovernance modules.
2. **Real data queries** — Tools should query actual PostgreSQL data via cross-module repositories or read-models.

### Provider-Native Tool Calling
3. **OpenAI function calling** — The current tool call convention uses text patterns (`[TOOL_CALL: ...]`). Native function calling via OpenAI's `tools` parameter would be more reliable.
4. **Ollama tool support** — Ollama's tool/function calling API support varies by model. Native integration would improve reliability.

### Advanced Tool Features
5. **Tool sandboxing** — No execution sandbox; tools run in the application process.
6. **Per-tool rate limiting** — No rate limiting per tool execution.
7. **Tool result caching** — No caching of tool results across iterations.
8. **Tool chaining** — Tools can't call other tools directly; only the agent loop mediates.

### Seed Data
9. **System agents AllowedTools** — All official system agents have `AllowedTools = ''`. They need to be updated in seed data to declare which tools they can use.

---

## What Is Explicitly for P9.5 and Beyond

### P9.5 — Cross-Module Tool Integration
- Wire ListServicesInfoTool to ServiceCatalog read-model or integration port
- Wire GetServiceHealthTool to OperationalIntelligence RuntimeSignals
- Wire ListRecentChangesTool to ChangeGovernance releases/deployments
- Update system agent seeds with AllowedTools values

### Future Phases
- Native function calling (OpenAI tools API, Ollama tool_calls)
- Tool sandboxing and resource limits
- Per-tool permission policies (beyond AllowedTools CSV)
- Tool result rendering in UI (structured output display)
- Tool execution analytics and cost tracking
- Custom tool registration (user-defined tools)
- RAG tools (vector search, document retrieval)
- Knowledge Hub tools

---

## Residual Limitations

1. **Text-based tool calling** — Uses `[TOOL_CALL: ...]` convention instead of provider-native function calling. Works with all LLMs but depends on the model following the instruction in the system prompt.
2. **Tool data is structural** — Tools execute and return structured responses but don't yet query real cross-module data. This is by design for P9.4; real data integration is P9.5.
3. **System agents have no tools enabled** — Seed data still has `AllowedTools = ''` for all official agents. Tools must be explicitly enabled per agent.
4. **No streaming + tools** — Tool execution currently only works with one-shot `CompleteAsync`, not with `CompleteStreamingAsync`.
