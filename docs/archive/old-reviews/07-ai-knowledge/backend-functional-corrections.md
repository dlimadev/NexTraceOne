# PARTE 8 — Backend Functional Corrections

> **Módulo:** AI & Knowledge (07)
> **Data:** 2026-03-25
> **Prompt:** N13

---

## 1. Inventário de endpoints

### 1.1 AiGovernanceEndpointModule (~35 endpoints)

| # | Método | Rota | Caso de uso | Estado |
|---|--------|------|-------------|--------|
| G-01 | GET | `/api/v1/ai/models` | Listar modelos | ⚠️ Funcional |
| G-02 | GET | `/api/v1/ai/models/{id}` | Obter modelo | ⚠️ Funcional |
| G-03 | POST | `/api/v1/ai/models` | Registar modelo | ⚠️ Handler existe, frontend desativou |
| G-04 | PATCH | `/api/v1/ai/models/{id}` | Atualizar modelo | ⚠️ Funcional |
| G-05 | GET | `/api/v1/ai/policies` | Listar políticas | ✅ Funcional |
| G-06 | POST | `/api/v1/ai/policies` | Criar política | ✅ Funcional |
| G-07 | PATCH | `/api/v1/ai/policies/{id}` | Atualizar política | ⚠️ Funcional |
| G-08 | GET | `/api/v1/ai/budgets` | Listar orçamentos | ⚠️ Schema OK |
| G-09 | PATCH | `/api/v1/ai/budgets/{id}` | Atualizar orçamento | ⚠️ Sem enforcement |
| G-10 | GET | `/api/v1/ai/agents` | Listar agents | ✅ Funcional |
| G-11 | GET | `/api/v1/ai/agents/context` | Agents por contexto | ⚠️ Parcial |
| G-12 | GET | `/api/v1/ai/agents/{id}` | Obter agent | ✅ Funcional |
| G-13 | POST | `/api/v1/ai/agents` | Criar agent | ✅ Funcional |
| G-14 | PATCH | `/api/v1/ai/agents/{id}` | Atualizar agent | ✅ Funcional |
| G-15 | POST | `/api/v1/ai/agents/{id}/execute` | Executar agent | 🔴 Tools NÃO executam |
| G-16 | GET | `/api/v1/ai/agents/executions/{id}` | Obter execução | ⚠️ Funcional |
| G-17 | POST | `/api/v1/ai/agents/artifacts/{id}/review` | Revisar artefacto | ⚠️ Parcial |
| G-18 | POST | `/api/v1/ai/assistant/chat` | Enviar mensagem | ⚠️ Sem streaming |
| G-19 | GET | `/api/v1/ai/assistant/conversations` | Listar conversas | ✅ Funcional |
| G-20 | POST | `/api/v1/ai/assistant/conversations` | Criar conversa | ✅ Funcional |
| G-21 | GET | `/api/v1/ai/assistant/conversations/{id}` | Obter conversa | ✅ Funcional |
| G-22 | PATCH | `/api/v1/ai/assistant/conversations/{id}` | Atualizar conversa | ✅ Funcional |
| G-23 | GET | `/api/v1/ai/assistant/messages` | Listar mensagens | ✅ Funcional |
| G-24 | GET | `/api/v1/ai/ide/clients` | Listar IDE clients | ❌ UI-only |
| G-25 | POST | `/api/v1/ai/ide/clients` | Registar IDE client | ❌ UI-only |
| G-26 | GET | `/api/v1/ai/ide/capabilities` | Capacidades IDE | ❌ UI-only |
| G-27 | GET | `/api/v1/ai/audit` | Listar audit entries | ⚠️ Parcial |
| G-28 | GET | `/api/v1/ai/routing/strategies` | Listar estratégias | ⚠️ Schema OK |
| G-29 | POST | `/api/v1/ai/routing/decision` | Decisão de routing | ⚠️ Pode ser stub |
| G-30 | POST | `/api/v1/ai/execution/plan` | Planear execução | ⚠️ Pode ser stub |
| G-31 | POST | `/api/v1/ai/enrichment/context` | Enriquecer contexto | ⚠️ Retrieval incerto |
| G-32 | GET | `/api/v1/ai/knowledge/sources` | Listar sources | ⚠️ Schema OK |
| G-33 | GET | `/api/v1/ai/knowledge/weights` | Pesos de sources | ⚠️ Schema OK |
| G-34 | GET | `/api/v1/ai/assistant/prompts` | Prompts sugeridos | ⚠️ Pode ser hardcoded |
| G-35 | GET | `/api/v1/ai/models/available` | Modelos disponíveis | ⚠️ Funcional |

### 1.2 AiRuntimeEndpointModule (~10 endpoints)

| # | Método | Rota | Estado |
|---|--------|------|--------|
| R-01 | POST | `/api/v1/ai/runtime/chat` | ⚠️ Sem streaming |
| R-02 | POST | `/api/v1/ai/runtime/activate` | ⚠️ Parcial |
| R-03 | GET | `/api/v1/ai/runtime/models` | ⚠️ Funcional |
| R-04 | GET | `/api/v1/ai/runtime/providers` | ⚠️ Funcional |
| R-05 | GET | `/api/v1/ai/runtime/providers/health` | ⚠️ Parcial |
| R-06 | GET | `/api/v1/ai/runtime/tokens/usage` | ⚠️ Parcial |
| R-07 | GET | `/api/v1/ai/runtime/tokens/policies` | ⚠️ Schema OK |
| R-08 | POST | `/api/v1/ai/runtime/search` | ❌ Provável stub |

### 1.3 AiOrchestrationEndpointModule (~11 endpoints)

| # | Método | Rota | Estado |
|---|--------|------|--------|
| O-01 | POST | `/api/v1/ai/orchestration/test-scenarios` | ⚠️ Parcial |
| O-02 | POST | `/api/v1/ai/orchestration/robot-framework` | ⚠️ Parcial |
| O-03 | POST | `/api/v1/ai/orchestration/catalog-question` | ⚠️ Parcial |
| O-04 | POST | `/api/v1/ai/orchestration/classify-change` | ⚠️ Parcial |
| O-05 | POST | `/api/v1/ai/orchestration/compare-environments` | ⚠️ Parcial |
| O-06 | POST | `/api/v1/ai/orchestration/analyze-nonprod` | ⚠️ Parcial |
| O-07 | POST | `/api/v1/ai/orchestration/validate-knowledge` | ⚠️ Parcial |
| O-08 | GET | `/api/v1/ai/orchestration/history` | ⚠️ Funcional |
| O-09 | POST | `/api/v1/ai/orchestration/promotion-readiness` | ⚠️ Parcial |
| O-10 | POST | `/api/v1/ai/orchestration/semantic-version` | ⚠️ Parcial |
| O-11 | POST | `/api/v1/ai/orchestration/release-summary` | ⚠️ Parcial |

### 1.4 ExternalAiEndpointModule (~8 endpoints)

| # | Método | Rota | Estado |
|---|--------|------|--------|
| E-01 | POST | `/api/v1/ai/external/query/simple` | ⚠️ Parcial |
| E-02 | POST | `/api/v1/ai/external/query/advanced` | ⚠️ Parcial |
| E-03 | POST | `/api/v1/ai/external/capture` | ⚠️ Parcial |
| E-04 | POST | `/api/v1/ai/external/policy` | ⚠️ Schema OK |
| E-05 | GET | `/api/v1/ai/external/knowledge` | ⚠️ Schema OK |
| E-06 | POST | `/api/v1/ai/external/knowledge/{id}/approve` | ⚠️ Schema OK |
| E-07 | POST | `/api/v1/ai/external/knowledge/{id}/reuse` | ⚠️ Schema OK |
| E-08 | GET | `/api/v1/ai/external/usage` | ⚠️ Parcial |

---

## 2. Endpoints mortos ou inúteis

| # | Endpoint | Motivo |
|---|----------|--------|
| G-24..G-26 | IDE clients/capabilities | ❌ Sem extensões reais de IDE — cosmético |
| R-08 | Runtime search | ❌ Provável stub sem backend real |
| G-29 | Routing decision | ⚠️ Possivelmente cosmético |
| G-30 | Execution plan | ⚠️ Possivelmente cosmético |

---

## 3. Backlog de correções backend

| # | Item | Prioridade | Esforço | Ficheiro/Área |
|---|------|------------|---------|---------------|
| B-01 | Implementar streaming SSE no endpoint de chat | P1 | 16h | `AiRuntimeEndpointModule`, `ExecuteAiChat` |
| B-02 | Implementar framework de tool calling para agents | P1 | 24h | `ExecuteAgent`, novo `IToolDispatcher` |
| B-03 | Validar e corrigir retrieval services | P1 | 16h | `EnrichContext`, services de retrieval |
| B-04 | Implementar enforcement de quota de tokens | P1 | 8h | `AiTokenQuotaPolicy`, middleware/handler |
| B-05 | Implementar enforcement de orçamento | P1 | 8h | `AIBudget`, middleware/handler |
| B-06 | Completar health check de providers | P2 | 4h | `CheckAiProvidersHealth` |
| B-07 | Adicionar campos ausentes a AiAgent | P2 | 4h | `AiAgent.cs` (MaxTokens, Timeout, RequiresApproval) |
| B-08 | Remover endpoints IDE (ou marcar experimental) | P2 | 2h | `AiIdeEndpointModule` |
| B-09 | Validar features de Orchestration ponta a ponta | P2 | 16h | `Application/Orchestration/Features/` |
| B-10 | Adicionar domain events em ações críticas | P2 | 8h | Todos os handlers de write |
| B-11 | Garantir token usage registration em todos os fluxos | P2 | 4h | `ExecuteAiChat`, `ExecuteAgent`, `QueryExternalAI` |
| B-12 | Consolidar AiSource vs AIKnowledgeSource | P2 | 4h | Domain entities |
| B-13 | Remover AiConversation duplicada de Orchestration | P2 | 2h | `AiOrchestrationDbContext` |
| B-14 | Validar External AI query end-to-end | P2 | 8h | `QueryExternalAISimple/Advanced` |
| B-15 | Adicionar testes unitários para handlers críticos | P2 | 16h | Novo `Tests/` project |
| B-16 | Adicionar rate limiting por utilizador | P3 | 8h | Middleware |
| B-17 | Implementar human-in-the-loop para agents | P3 | 12h | `ExecuteAgent`, workflow |
| B-18 | Implementar search real (SearchData, SearchDocuments) | P3 | 16h | Runtime features |
| B-19 | Validar integração cross-module (Catalog, Changes, Ops) | P3 | 12h | Orchestration handlers |
| B-20 | Publicar integration events para Audit & Compliance | P2 | 8h | Domain event publishers |

**Total estimado:** ~194h (~24 dias de trabalho)
