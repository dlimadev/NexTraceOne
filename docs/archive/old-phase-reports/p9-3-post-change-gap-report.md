# P9.3 — Post-Change Gap Report

## What Was Resolved

### Streaming Infrastructure
- ✅ **`IChatCompletionProvider.CompleteStreamingAsync`** — New interface method for streaming, returning `IAsyncEnumerable<ChatStreamChunk>`
- ✅ **`ChatStreamChunk` record** — Typed DTO for incremental streaming fragments
- ✅ **`SupportsStreaming` property** — Default interface member allowing providers to declare streaming support

### Provider Streaming
- ✅ **OllamaProvider streaming** — Real NDJSON streaming from Ollama API (`stream=true`)
- ✅ **OpenAiProvider streaming** — Real SSE streaming from OpenAI API (`stream=true`)
- ✅ **`OllamaHttpClient.ChatStreamAsync`** — NDJSON line-by-line reader with `HttpCompletionOption.ResponseHeadersRead`
- ✅ **`OpenAiHttpClient.ChatStreamAsync`** — SSE parser with `data: [DONE]` termination
- ✅ **Stream=false hardcoded removed** — Streaming is now the default path for the streaming endpoint; one-shot remains as a separate path

### Endpoint
- ✅ **`POST /api/v1/ai/chat/stream`** — New SSE endpoint for streaming chat responses
- ✅ **SSE contract defined** — `data: {json}\n\n` format with `data: [DONE]` termination
- ✅ **Rate limiting** — Uses existing "ai" rate limiter
- ✅ **Authorization** — Requires `ai:runtime:write` permission

### Backward Compatibility
- ✅ **One-shot chat preserved** — `POST /api/v1/ai/chat` and `ExecuteAiChat` handler unchanged
- ✅ **`CompleteAsync` preserved** — Both providers still support one-shot mode
- ✅ **All 410 tests pass** — Zero regressions

---

## What Still Remains Pending

### Streaming Chat Enhancements (Not in P9.3 Scope)
1. **Conversation tracking for streaming** — The streaming endpoint does not yet persist conversations/messages/usage entries. The one-shot endpoint (`POST /api/v1/ai/chat`) handles all persistence. A future phase should add streaming-aware conversation tracking.
2. **Streaming error recovery** — If a provider fails mid-stream, the current implementation stops yielding. A retry/reconnection mechanism could be added in a future phase.
3. **Token counting accuracy** — OpenAI does not always include usage in streaming responses unless `stream_options: {"include_usage": true}` is sent. Final token counts may be 0 in the streaming endpoint.

### Frontend Integration
4. **Frontend SSE consumption** — The backend SSE contract is ready. The frontend chat UI needs to be updated to consume the `text/event-stream` response from `/api/v1/ai/chat/stream`.

### Not in Scope for P9.3
- **Tool execution** — Not addressed in this phase
- **RAG/retrieval** — Not addressed in this phase
- **Knowledge Hub** — Not addressed in this phase
- **WebSocket support** — SSE was chosen for simplicity; WebSocket can be added later if needed
- **Multiple transport protocols** — Only SSE in this phase

---

## What Is Explicitly for P9.4 and Beyond

### P9.4 — Streaming Chat Conversation Tracking
- Persist conversations and messages from streaming sessions
- Record usage entries for streaming calls
- Update `ExecuteAiChat` to optionally delegate to streaming internally

### Future Phases
- Frontend SSE consumption in AI Hub chat component
- Tool execution pipeline with streaming
- RAG/retrieval with streaming context assembly
- Knowledge Hub integration
- WebSocket support (if SSE proves insufficient)
- OpenAI `stream_options.include_usage` for accurate streaming token counts

---

## Residual Limitations

1. **No conversation persistence in streaming** — The streaming endpoint resolves model/provider and streams directly. It does not create AiAssistantConversation, AiMessage, or AIUsageEntry records. The one-shot endpoint handles all tracking.
2. **Token counts may be 0 in streaming** — OpenAI doesn't always include usage stats in streaming responses. The final chunk may report 0/0 tokens.
3. **No streaming retry on failure** — If a provider connection drops mid-stream, the endpoint terminates. No automatic retry is implemented.
