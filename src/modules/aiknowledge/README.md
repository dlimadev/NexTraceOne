# AIKnowledge Module

## Overview

The **AIKnowledge** module is the governed AI backbone of NexTraceOne. It implements the platform's AI capabilities with full governance, auditability, and policy enforcement — ensuring AI is never an ungoverned black box but a controlled, auditable, and contextually useful capability.

This module follows the NexTraceOne principle: **AI is a governed capability, not a generic feature**.

## Architecture

The module is organized into four bounded sub-modules, each with its own DbContext, migration history, and API surface:

```
AIKnowledge/
├── Governance     — Model Registry, Access Policies, Budgets, Audit, Agents, Routing, IDE
├── Runtime        — Chat execution, Provider management, Token governance, Search
├── Orchestration  — AI-assisted analysis, Change classification, Test generation
└── ExternalAI     — External AI integration, Knowledge capture, Policy enforcement
```

### Governance (ai_governance schema)

The **Governance** sub-module owns the administrative control plane:

| Entity | Table | Purpose |
|--------|-------|---------|
| `AIModel` | `ai_models` | Model Registry — registered AI models with capabilities, sensitivity, lifecycle |
| `AiAgent` | `ai_agents` | Agent catalog — specialized AI agents with system prompts and policies |
| `AIAccessPolicy` | `ai_access_policies` | Access control — who can use which models, with what limits |
| `AIBudget` | `ai_budgets` | Token budgets per user/team/role with period enforcement |
| `AIUsageEntry` | `ai_usage_entries` | Full audit trail of every AI interaction |
| `AIRoutingStrategy` | `ai_routing_strategies` | Model routing rules per context and sensitivity |
| `AIRoutingDecision` | `ai_routing_decisions` | Audit log of routing decisions made |
| `AIKnowledgeSource` | `ai_knowledge_sources` | Knowledge grounding source definitions |
| `AIKnowledgeSourceWeight` | `ai_knowledge_source_weights` | Relevance weights per source for context enrichment |
| `AiAssistantConversation` | `ai_assistant_conversations` | Persisted chat conversations |
| `AiMessage` | `ai_messages` | Individual messages within conversations |
| `AiAgentExecution` | `ai_agent_executions` | Agent execution history and status |
| `AiAgentArtifact` | `ai_agent_artifacts` | Generated artifacts from agent executions |
| `AIIDEClientRegistration` | `ai_ide_client_registrations` | Registered IDE extensions (VS Code, Visual Studio) |
| `AIIDECapabilityPolicy` | `ai_ide_capability_policies` | IDE-specific capability policies |
| `AIEnrichmentResult` | `ai_enrichment_results` | Context enrichment results |
| `AIExecutionPlan` | `ai_execution_plans` | Planned execution strategies |
| `AiExternalInferenceRecord` | `ai_external_inference_records` | External inference audit records |

### Runtime (ai_runtime schema)

The **Runtime** sub-module handles real-time AI operations:

| Entity | Table | Purpose |
|--------|-------|---------|
| `AiProvider` | `ai_providers` | Configured AI providers (Ollama, OpenAI, Anthropic) |
| `AiSource` | `ai_sources` | Data sources available for AI grounding |
| `AiTokenQuotaPolicy` | `ai_token_quota_policies` | Token quota enforcement policies |
| `AiTokenUsageLedger` | `ai_token_usage_ledger` | Real-time token consumption tracking |

### Orchestration (ai_orchestration schema)

The **Orchestration** sub-module provides AI-assisted analysis capabilities:

| Entity | Table | Purpose |
|--------|-------|---------|
| `AiConversation` | `ai_conversations` | Orchestration conversation tracking |
| `AiContext` | `ai_contexts` | Context bundles for AI analysis |
| `GeneratedTestArtifact` | `ai_generated_test_artifacts` | Generated test scenarios and Robot Framework drafts |
| `KnowledgeCaptureEntry` | `ai_knowledge_capture_entries` | Captured knowledge from AI interactions |

### ExternalAI (ai_external schema)

The **ExternalAI** sub-module manages external AI provider integration:

| Entity | Table | Purpose |
|--------|-------|---------|
| `ExternalAiProvider` | `ai_external_providers` | External AI provider configurations |
| `ExternalAiPolicy` | `ai_external_policies` | Policies controlling external AI usage |
| `ExternalAiConsultation` | `ai_external_consultations` | External AI consultation audit trail |
| `KnowledgeCapture` | `ai_knowledge_captures` | Captured knowledge for reuse |

## API Reference

### Governance Endpoints (`/api/v1/ai/`)

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| GET | `/models` | `ai:governance:read` | List all registered models |
| GET | `/models/{id}` | `ai:governance:read` | Get model details |
| GET | `/models/available` | `ai:assistant:read` | List models available to current user |
| POST | `/models` | `ai:governance:write` | Register a new model |
| PATCH | `/models/{id}` | `ai:governance:write` | Update model details |
| GET | `/policies` | `ai:governance:read` | List access policies |
| POST | `/policies` | `ai:governance:write` | Create access policy |
| PATCH | `/policies/{id}` | `ai:governance:write` | Update access policy |
| GET | `/budgets` | `ai:governance:read` | List token budgets |
| PATCH | `/budgets/{id}` | `ai:governance:write` | Update token budget |
| GET | `/audit` | `ai:governance:read` | List audit entries |
| GET | `/knowledge-sources` | `ai:governance:read` | List knowledge sources |
| GET | `/knowledge-sources/weights` | `ai:governance:read` | List knowledge source weights |
| GET | `/routing/strategies` | `ai:governance:read` | List routing strategies |
| POST | `/routing/decide` | `ai:assistant:write` | Get routing decision |

### Agent Endpoints (`/api/v1/ai/agents/`)

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| GET | `/` | `ai:assistant:read` | List agents |
| GET | `/by-context` | `ai:assistant:read` | List agents by context |
| GET | `/{id}` | `ai:assistant:read` | Get agent details |
| POST | `/` | `ai:governance:write` | Create agent |
| PATCH | `/{id}` | `ai:governance:write` | Update agent |
| POST | `/{id}/execute` | `ai:assistant:write` | Execute agent |
| GET | `/executions/{id}` | `ai:assistant:read` | Get execution details |
| POST | `/artifacts/{id}/review` | `ai:governance:write` | Review artifact |

### Assistant Endpoints (`/api/v1/ai/assistant/`)

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| POST | `/messages` | `ai:assistant:write` | Send message |
| GET | `/conversations` | `ai:assistant:read` | List conversations |
| POST | `/conversations` | `ai:assistant:write` | Create conversation |
| GET | `/conversations/{id}` | `ai:assistant:read` | Get conversation |
| PATCH | `/conversations/{id}` | `ai:assistant:write` | Update conversation |
| GET | `/conversations/{id}/messages` | `ai:assistant:read` | List messages |
| GET | `/suggested-prompts` | `ai:assistant:read` | Get suggested prompts |

### Seed Endpoints (`/api/v1/ai/seed/`)

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| POST | `/models` | `ai:governance:write` | Seed default models from catalog |
| POST | `/agents` | `ai:governance:write` | Seed default agents from catalog |

### Runtime Endpoints (`/api/v1/ai/`)

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| POST | `/chat` | `ai:runtime:write` | Execute AI chat |
| POST | `/chat/stream` | `ai:runtime:write` | Stream AI chat (SSE) |
| GET | `/providers` | `ai:runtime:read` | List AI providers |
| GET | `/providers/health` | `ai:runtime:read` | Check provider health |
| GET | `/sources` | `ai:runtime:read` | List AI sources |
| GET | `/models/active` | `ai:runtime:read` | List active models |
| PUT | `/models/{id}/activate` | `ai:runtime:write` | Activate a model |
| GET | `/token-policies` | `ai:runtime:read` | List token policies |
| GET | `/token-usage` | `ai:runtime:read` | Get token usage |
| POST | `/external-inferences` | `ai:runtime:write` | Record external inference |
| POST | `/search/documents` | `ai:runtime:write` | Search documents |
| POST | `/search/data` | `ai:runtime:write` | Search data |
| POST | `/search/telemetry` | `ai:runtime:write` | Search telemetry |

### IDE Endpoints (`/api/v1/ai/ide/`)

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| GET | `/capabilities` | `ai:ide:read` | Get IDE capabilities |
| GET | `/clients` | `ai:ide:read` | List IDE clients |
| POST | `/clients/register` | `ai:ide:write` | Register IDE client |
| GET | `/policies` | `ai:ide:read` | List IDE policies |
| GET | `/summary` | `ai:ide:read` | Get IDE admin summary |

### Orchestration Endpoints (`/api/v1/aiorchestration/`)

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| POST | `/catalog/ask` | `ai:runtime:write` | Ask catalog question |
| POST | `/changes/classify` | `ai:runtime:write` | Classify change with AI |
| POST | `/contracts/suggest-version` | `ai:runtime:write` | Suggest semantic version |
| POST | `/analysis/non-prod` | `ai:runtime:write` | Analyze non-prod environment |
| POST | `/analysis/compare-environments` | `ai:runtime:write` | Compare environments |
| POST | `/analysis/promotion-readiness` | `ai:runtime:write` | Assess promotion readiness |
| GET | `/conversations/history` | `ai:runtime:read` | Get conversation history |
| POST | `/knowledge/entries/{id}/validate` | `ai:runtime:write` | Validate knowledge capture |
| POST | `/generate/test-scenarios` | `ai:runtime:write` | Generate test scenarios |
| POST | `/generate/robot-framework` | `ai:runtime:write` | Generate Robot Framework draft |
| POST | `/generate/releases/{id}/approval-summary` | `ai:runtime:write` | Summarize release for approval |

### ExternalAI Endpoints (`/api/v1/externalai/`)

| Method | Path | Permission | Description |
|--------|------|------------|-------------|
| POST | `/query/simple` | `ai:runtime:write` | Simple external AI query |
| POST | `/query/advanced` | `ai:runtime:write` | Advanced external AI query |
| POST | `/knowledge/capture` | `ai:runtime:write` | Capture external AI response |
| GET | `/knowledge/captures` | `ai:runtime:read` | List knowledge captures |
| POST | `/knowledge/captures/{id}/approve` | `ai:runtime:write` | Approve knowledge capture |
| POST | `/knowledge/captures/{id}/reuse` | `ai:runtime:write` | Reuse knowledge capture |
| GET | `/knowledge/usage` | `ai:runtime:read` | Get external AI usage stats |
| POST | `/knowledge/policy` | `ai:runtime:write` | Configure external AI policy |

## Seed Data

The module includes deterministic seed data via static catalogs:

### DefaultModelCatalog

Pre-configured AI models for immediate operation:

| Model | Provider | Type | Internal | Default For |
|-------|----------|------|----------|-------------|
| `deepseek-r1:1.5b` | Ollama | Chat | ✅ | Reasoning |
| `llama3.2:3b` | Ollama | Chat | ✅ | Chat |
| `nomic-embed-text` | Ollama | Embedding | ✅ | Embeddings |
| `codellama:7b` | Ollama | CodeGeneration | ✅ | — |
| `gpt-4o` | OpenAI | Chat | ❌ | — |
| `gpt-4o-mini` | OpenAI | Chat | ❌ | — |
| `claude-3-5-sonnet` | Anthropic | Chat | ❌ | — |

### DefaultAgentCatalog

Pre-configured official agents covering core NexTraceOne domains:

| Agent | Category | Target Persona | Domain |
|-------|----------|----------------|--------|
| `service-analyst` | ServiceAnalysis | Engineer | Services & dependencies |
| `contract-designer` | ApiDesign | Engineer | REST/SOAP/Event contracts |
| `change-advisor` | ChangeIntelligence | Tech Lead | Risk & promotion readiness |
| `incident-responder` | IncidentResponse | Engineer | Root cause & mitigation |
| `test-generator` | TestGeneration | Engineer | Test scenarios & Robot Framework |
| `docs-assistant` | DocumentationAssistance | Engineer | Runbooks & knowledge articles |
| `security-reviewer` | SecurityAudit | Architect | Security & compliance review |
| `event-designer` | EventDesign | Architect | Kafka/AsyncAPI contracts |

Seed endpoints: `POST /api/v1/ai/seed/models` and `POST /api/v1/ai/seed/agents`

Both operations are **idempotent** — existing entities (matched by name, case-insensitive) are never duplicated.

## Key Enums

### ModelType
`Chat`, `Completion`, `Embedding`, `CodeGeneration`, `Analysis`

### ModelStatus
`Active`, `Inactive`, `Deprecated`, `Blocked`

### AgentCategory
`General`, `ServiceAnalysis`, `ContractGovernance`, `IncidentResponse`, `ChangeIntelligence`, `SecurityAudit`, `FinOps`, `CodeReview`, `Documentation`, `Testing`, `Compliance`, `ApiDesign`, `TestGeneration`, `EventDesign`, `DocumentationAssistance`, `SoapDesign`

### AgentOwnershipType
`System`, `Tenant`, `User`

### AgentVisibility
`Private`, `Team`, `Tenant`

### AgentPublicationStatus
`Draft`, `PendingReview`, `Active`, `Published`, `Archived`, `Blocked`

## Authorization Permissions

| Permission | Description |
|------------|-------------|
| `ai:governance:read` | Read Model Registry, policies, budgets, audit |
| `ai:governance:write` | Create/update models, policies, budgets, agents, seed data |
| `ai:assistant:read` | Read conversations, messages, available models, agents |
| `ai:assistant:write` | Send messages, create conversations, execute agents |
| `ai:runtime:read` | Read providers, sources, active models, token usage |
| `ai:runtime:write` | Execute AI chat, search, record inferences, orchestration |
| `ai:ide:read` | Read IDE capabilities, clients, policies |
| `ai:ide:write` | Register IDE clients |

## Testing

```bash
# Run all AIKnowledge tests
dotnet test tests/modules/aiknowledge/NexTraceOne.AIKnowledge.Tests/NexTraceOne.AIKnowledge.Tests.csproj

# Build the API module
dotnet build src/modules/aiknowledge/NexTraceOne.AIKnowledge.API/NexTraceOne.AIKnowledge.API.csproj
```

## Design Principles

1. **Internal AI first** — Default models are local (Ollama). External providers are optional and governed.
2. **Full audit trail** — Every AI interaction is logged with user, model, tokens, result, and policy evaluation.
3. **Policy-driven access** — Models, agents, and capabilities are controlled by scoped policies (user/group/role/persona/team).
4. **Token governance** — Budgets and quotas prevent unbounded AI usage.
5. **Knowledge grounding** — AI responses are grounded in NexTraceOne's own data: services, contracts, incidents, telemetry.
6. **IDE integration as first-class** — VS Code and Visual Studio extensions are governed with the same policies as the web UI.
7. **Agent specialization** — Each agent is specialized for a NexTraceOne domain, not a generic chatbot.
8. **Idempotent seed** — Default models and agents can be safely re-seeded without duplication.
