# P9.3 — AI Streaming Runtime Report

## Objective

Implement real streaming support in the AI providers (Ollama and OpenAI) and the chat runtime, removing the hardcoded `Stream=false` pattern and enabling incremental response delivery to the frontend.

---

## Previous State (Before P9.3)

| Component | Status |
|---|---|
| **OllamaProvider.CompleteAsync** | ✅ Real one-shot completion with `Stream = false` hardcoded |
| **OpenAiProvider.CompleteAsync** | ✅ Real one-shot completion (no stream field) |
| **OllamaHttpClient.ChatAsync** | ✅ Reads full JSON response body |
| **OpenAiHttpClient.ChatAsync** | ✅ Reads full JSON response body |
| **IChatCompletionProvider** | ⚠️ Only `CompleteAsync` (one-shot) — no streaming method |
| **Streaming endpoint** | ❌ Absent — only `POST /api/v1/ai/chat` (one-shot) |
| **Streaming DTOs** | ❌ No `ChatStreamChunk`, no OpenAI stream chunk DTO |
| **ExecuteAiChat handler** | ✅ Real one-shot execution with conversation+usage tracking |

**Key gap:** Streaming was explicitly flagged as "ABSENT — Stream=false hardcoded" in the remediation roadmap and module assessment reports.

---

## Changes Implemented

### 1. New Streaming Interface — `IChatCompletionProvider`

**File:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Runtime/Abstractions/IChatCompletionProvider.cs`

Added to the existing `IChatCompletionProvider` interface:
- `CompleteStreamingAsync(ChatCompletionRequest, CancellationToken)` — returns `IAsyncEnumerable<ChatStreamChunk>`
- `SupportsStreaming` — default interface member returning `true`

New record:
- `ChatStreamChunk(Content, IsComplete, ModelId, ProviderId, PromptTokens, CompletionTokens, ErrorMessage?)` — represents one incremental fragment from the AI provider

### 2. Streaming in OllamaHttpClient

**File:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Providers/Ollama/OllamaHttpClient.cs`

Added `ChatStreamAsync(OllamaChatRequest, CancellationToken)`:
- Sends request with `Stream = true`
- Uses `HttpCompletionOption.ResponseHeadersRead` for immediate header-level response
- Reads NDJSON (newline-delimited JSON) line by line from the response stream
- Deserializes each line to `OllamaChatResponse`
- Yields chunks until `done = true` signals completion

### 3. Streaming in OpenAiHttpClient

**File:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Providers/OpenAI/OpenAiHttpClient.cs`

Added `ChatStreamAsync(OpenAiChatRequest, CancellationToken)`:
- Creates `OpenAiStreamChatRequest` with `stream = true`
- Uses `HttpCompletionOption.ResponseHeadersRead` for immediate streaming
- Reads SSE (Server-Sent Events) lines from the response stream
- Parses `data: {json}` lines, skipping empty lines
- Terminates on `data: [DONE]`

New DTOs:
- `OpenAiStreamChatRequest` — includes `stream` field
- `OpenAiStreamChunk` — SSE chunk with choices array
- `OpenAiStreamChoice` — choice within streaming chunk
- `OpenAiStreamDelta` — delta content from streaming chunk

### 4. OllamaProvider Streaming

**File:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Providers/Ollama/OllamaProvider.cs`

Added `CompleteStreamingAsync`:
- Resolves model (with error-safe fallback that doesn't use yield-in-catch)
- Creates streaming request with `Stream = true`
- Iterates over `ChatStreamAsync` chunks
- Maps each `OllamaChatResponse` chunk to `ChatStreamChunk`
- Token counts (PromptEvalCount, EvalCount) are available only in the final chunk
- Falls back to error chunk if no data received

### 5. OpenAiProvider Streaming

**File:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Providers/OpenAI/OpenAiProvider.cs`

Added `CompleteStreamingAsync`:
- Resolves model from request or options
- Maps messages to OpenAI format
- Iterates over `ChatStreamAsync` chunks
- Extracts delta content from `choices[0].delta.content`
- Detects completion via `finish_reason` ("stop" or "length")
- Usage stats available only in final chunk (when provider includes them)

### 6. Streaming Chat Endpoint

**File:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.API/Runtime/Endpoints/Endpoints/AiRuntimeEndpointModule.cs`

Added `POST /api/v1/ai/chat/stream`:
- Resolves model and provider inline (no MediatR — streaming requires direct HttpContext access)
- Builds `ChatCompletionRequest` with system prompt + user message
- Sets response headers: `Content-Type: text/event-stream`, `Cache-Control: no-cache`, `Connection: keep-alive`
- Iterates `CompleteStreamingAsync`, writing each chunk as `data: {json}\n\n`
- Ends with `data: [DONE]\n\n` to signal stream completion
- Requires `ai:runtime:write` permission, rate-limited under "ai" policy

New DTO:
- `ExecuteAiChatStreamRequest(Message, PreferredModelId?, SystemPrompt?, Temperature?, MaxTokens?)`

### 7. Existing One-Shot Flow Preserved

The existing `POST /api/v1/ai/chat` endpoint and `ExecuteAiChat` handler remain **completely unchanged**. The one-shot flow continues to work exactly as before — no regressions. `CompleteAsync` in both providers still uses `Stream = false` for one-shot calls.

---

## SSE Contract for Frontend

The streaming endpoint returns Server-Sent Events with the following format:

```
data: {"content":"Hello","isComplete":false,"modelId":"llama3","providerId":"ollama","promptTokens":0,"completionTokens":0,"error":null}

data: {"content":" world","isComplete":false,"modelId":"llama3","providerId":"ollama","promptTokens":0,"completionTokens":0,"error":null}

data: {"content":"","isComplete":true,"modelId":"llama3","providerId":"ollama","promptTokens":42,"completionTokens":8,"error":null}

data: [DONE]
```

Frontend consumption:
```typescript
const response = await fetch('/api/v1/ai/chat/stream', { method: 'POST', body: JSON.stringify(payload) });
const reader = response.body.getReader();
// Read SSE chunks progressively
```

---

## Files Changed

| File | Action |
|---|---|
| `src/.../Application/Runtime/Abstractions/IChatCompletionProvider.cs` | Modified — added `CompleteStreamingAsync`, `ChatStreamChunk`, `SupportsStreaming` |
| `src/.../Infrastructure/Runtime/Providers/Ollama/OllamaHttpClient.cs` | Modified — added `ChatStreamAsync` (NDJSON streaming) |
| `src/.../Infrastructure/Runtime/Providers/Ollama/OllamaProvider.cs` | Modified — added `CompleteStreamingAsync` |
| `src/.../Infrastructure/Runtime/Providers/OpenAI/OpenAiHttpClient.cs` | Modified — added `ChatStreamAsync` (SSE), `OpenAiStreamChatRequest`, `OpenAiStreamChunk`, `OpenAiStreamChoice`, `OpenAiStreamDelta` |
| `src/.../Infrastructure/Runtime/Providers/OpenAI/OpenAiProvider.cs` | Modified — added `CompleteStreamingAsync` |
| `src/.../API/Runtime/Endpoints/Endpoints/AiRuntimeEndpointModule.cs` | Modified — added `POST /api/v1/ai/chat/stream` endpoint, `ExecuteAiChatStreamRequest` DTO |
| `docs/architecture/p9-3-ai-streaming-runtime-report.md` | Created |
| `docs/architecture/p9-3-post-change-gap-report.md` | Created |

---

## Validation

- **Build:** ✅ 0 errors
- **Tests:** ✅ 410 AIKnowledge tests pass (0 regressions)
- **Existing one-shot chat:** ✅ Preserved — `POST /api/v1/ai/chat` unchanged
- **Streaming endpoint:** ✅ Registered — `POST /api/v1/ai/chat/stream` available
