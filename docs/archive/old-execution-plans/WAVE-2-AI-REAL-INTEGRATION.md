# WAVE-2: AI Real Integration

## GenerateDraftFromAi

### Before
- Generated static templates per protocol (OpenAPI YAML, AsyncAPI YAML, WSDL XML)
- No AI provider integration
- Template was identical regardless of user prompt

### After
- Integrates with `IChatCompletionProvider` via new `IAiDraftGenerator` abstraction
- Uses `IAiModelCatalogService.ResolveDefaultModelAsync("chat")` for model selection
- Constructs protocol-aware system prompts for contract generation
- Falls back to static template when AI provider is unavailable
- Logs model, provider, and token usage for audit

### Provider/Context Used
- Provider: Ollama (default) or OpenAI (when configured)
- Temperature: 0.3 (low creativity for structured output)
- MaxTokens: 4000
- System prompt includes: protocol type, title, contract structure guidance

### Architecture
```
GenerateDraftFromAi.Handler
  └─ IAiDraftGenerator (optional dependency)
       └─ AiDraftGeneratorService (Catalog.Infrastructure)
            ├─ IChatCompletionProvider (from AIKnowledge Runtime)
            └─ IAiModelCatalogService (from AIKnowledge Runtime)
```

## DocumentRetrievalService

### Before
- Returned `Array.Empty<DocumentSearchHit>()`

### After
- Searches `AIKnowledgeSource` entities via `IAiKnowledgeSourceRepository`
- Filters by name, description, and endpoint matching
- Supports source type filtering
- Returns relevance-scored hits based on position

### Limitations
- Text matching only (no semantic/vector search yet)
- Requires knowledge sources to be registered in the system
- No external document connector (Confluence, SharePoint) yet

## TelemetryRetrievalService

### Before
- Returned `Array.Empty<TelemetrySearchHit>()`

### After
- Queries real logs via `IObservabilityProvider.QueryLogsAsync()`
- Constructs `LogQueryFilter` with service name, severity, message content, and time range
- Returns trace IDs, span IDs, and log messages for AI grounding

### Limitations
- Depends on ClickHouse/Elastic being operational
- Returns empty results when observability provider is not configured (honest empty)
- No trace correlation (single log query, not distributed trace stitching)
