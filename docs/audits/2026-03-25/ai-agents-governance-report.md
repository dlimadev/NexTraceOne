# Relatório de IA, Agentes e Governança de IA — NexTraceOne

**Data:** 25 de março de 2026

---

## 1. Objectivo

Auditar o estado real do módulo de IA: governance, agentes especializados, providers, orchestration, external AI, contexto operacional, governança de políticas e alinhamento com a visão oficial do produto.

---

## 2. Arquitectura Geral do Módulo AIKnowledge

O módulo está organizado em 3 subdomains:

```
NexTraceOne.AIKnowledge/
├── Domain/
│   ├── Governance/    (14 entidades — REAL)
│   ├── ExternalAI/    (4 entidades — STUB)
│   └── Orchestration/ (4 entidades — PARTIAL)
├── Application/
│   ├── Governance/    (28 handlers: 24 reais, 2 mock, 2 mistos)
│   ├── ExternalAI/    (8 features: 1 com lógica, 7 TODO)
│   └── Orchestration/ (8 features: 0 implementados, todos TODO)
├── Infrastructure/
│   ├── Governance/    (19 DbSets, 11 repos, DI completo — REAL)
│   ├── ExternalAI/    (0 DbSets, sem repos, DI TODO)
│   ├── Orchestration/ (4 DbSets sem config, sem repos)
│   └── Runtime/       (OllamaProvider, OpenAiProvider — REAL)
├── API/               (27+ endpoints Governance; outros TODO)
└── Tests/             (50+ testes unitários)
```

**Maturidade estimada:** ~50-55% do módulo completo

---

## 3. Subdomain Governance — Estado: REAL (95%)

### 3.1 Entidades de Domínio (14)

**Ficheiro:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Governance/Persistence/AiGovernanceDbContext.cs`

| Entidade | Propósito | Estado |
|----------|-----------|--------|
| `AIModel` | Registry de modelos (40+ props: capabilities, license, compliance) | REAL |
| `AiProvider` | Provedores (Ollama, OpenAI, Azure, Gemini) | REAL |
| `AIAccessPolicy` | Políticas de acesso por escopo (user/group/role/persona/team) | REAL |
| `AIBudget` | Orçamentos de tokens por período | REAL |
| `AiAssistantConversation` | Conversações com threads | REAL |
| `AiMessage` | Mensagens com metadados (model, tokens, groundingSources) | REAL |
| `AIUsageEntry` | Audit trail com 18 campos | REAL |
| `AIRoutingStrategy` | Lógica de selecção de modelo | REAL |
| `AIRoutingDecision` | Registo de decisão de roteamento | REAL |
| `AiAgent` | Agentes especializados com system prompts | REAL |
| `AiAgentExecution` | Execução de agente (status, tokens, custo, duração) | REAL |
| `AiAgentArtifact` | Artefactos gerados (OpenAPI, WSDL, Avro, Test scenarios) | REAL |
| `AIKnowledgeSource` | Fontes de grounding configuráveis | REAL |
| `AIIDEClientRegistration`, `AIIDECapabilityPolicy` | Integração com IDEs | REAL |
| `AiTokenQuotaPolicy`, `AiTokenUsageLedger` | Controlo de quotas | REAL |

### 3.2 Handlers de Application (24/28 reais)

**Reais:**
- `CreateAgentCommand`, `UpdateAgentCommand`, `GetAgentQuery`, `ListAgentsQuery`
- `ExecuteAgentCommand` → chama `AiAgentRuntimeService` real
- `CreateConversationCommand`, `GetConversationQuery`, `ListConversationsQuery`
- `CreatePolicyCommand`, `ListPoliciesQuery`, `UpdatePolicyCommand`
- `RegisterModelCommand`, `UpdateModelCommand`, `GetModelQuery`, `ListModelsQuery`
- `ListBudgetsQuery`, `UpdateBudgetCommand`
- `ReviewArtifactCommand` (human-in-the-loop)
- `ListAuditEntriesQuery`
- `GetRoutingDecisionQuery`, `ListRoutingStrategiesQuery`
- `EnrichContextCommand`
- `GetIdeCapabilitiesQuery`, `RegisterIdeClientCommand`, `ListIdeClientsQuery`
- `PlanExecutionCommand`, `GetAgentExecutionQuery`
- `ListKnowledgeSourcesQuery`

**Mock (2):**
- `ListKnowledgeSourceWeightsQuery` — retorna 11 pesos hardcoded
- `ListSuggestedPromptsQuery` — retorna 21 prompts hardcoded

### 3.3 Runtime Providers — REAIS

**OllamaProvider** (`185 linhas`):
```csharp
// Verificado — HTTP client real
public async Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken ct)
{
    var httpRequest = new OllamaRequest { Model = resolvedModel, Messages = request.Messages, Stream = false };
    var response = await _client.PostAsync("/api/chat", httpRequest, ct);
    // ... mapeamento real de resposta
}
```

**OpenAiProvider** (`132 linhas`):
```csharp
// Verificado — HTTP client real
public async Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken ct)
{
    var httpRequest = new OpenAiRequest { Model = resolvedModel, Messages = request.Messages, Stream = false };
    var response = await _client.PostAsync("/v1/chat/completions", httpRequest, ct);
    // ... mapeamento real de resposta
}
```

**⚠️ Stream=false hardcoded em ambos os providers — streaming não implementado**

### 3.4 AiAgentRuntimeService — REAL (12 steps)

```
1. Resolve agent
2. Validate agent active
3. Validate user access (AIAccessPolicy)
4. Resolve model (override or preferred)
5. Validate model allowed
6. Resolve provider
7. Start execution record
8. Build system prompt
9. Execute real inference
10. Complete execution record
11. Increment execution count
12. Generate artifacts
```

### 3.5 ExecuteAiChat Handler — REAL

```
1. Resolve model
2. Get chat provider
3. Resolve or create conversation
4. Save user message
5. Build chat request with history
6. Execute real inference (await chatProvider.CompleteAsync())
7. Save assistant message with metadata
8. Record usage audit entry
9. Return response
```

---

## 4. Agentes Especializados (10)

| Agente | Domínio | Estado |
|--------|---------|--------|
| `ServiceHealthAnalyzer` | Operational Reliability | REAL (executa) |
| `ChangeImpactEvaluator` | Change Intelligence | REAL |
| `APIContractDraftGenerator` | Contract Governance | REAL |
| `IncidentCorrelator` | Operations | REAL |
| `RunbookGenerator` | Operations | REAL |
| `BlastRadiusEstimator` | Change Intelligence | REAL |
| `CodeReviewAssistant` | Developer Acceleration | REAL |
| `SemanticVersionAdvisor` | Contract Governance | REAL |
| `DependencyImpactMapper` | Service Governance | REAL |
| `ComplianceAuditor` | Governance | REAL |

**⚠️ Tools execution:** `AiAgent.AllowedTools` campo existe mas `ExecuteTools()` não wired no `AiAgentRuntimeService`

---

## 5. Subdomain ExternalAI — Estado: STUB

**Ficheiro:** `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/ExternalAI/Persistence/ExternalAiDbContext.cs`

```csharp
// DbContext com 0 DbSets — TODO
// Evidência directa do ficheiro
```

| Feature | Estado |
|---------|--------|
| `QueryExternalAISimple` | Lógica de handler existe, sem endpoint |
| `QueryExternalAIAdvanced` | TODO |
| `CaptureExternalAIResponse` | TODO |
| `ConfigureExternalAIPolicy` | TODO |
| `ApproveKnowledgeCapture` | TODO |
| `ReuseKnowledgeCapture` | TODO |
| `GetExternalAIUsage` | TODO |
| `ListKnowledgeCaptures` | TODO |

**`QueryExternalAISimple`** tem lógica: chama `IExternalAIRoutingPort.RouteQueryAsync()` com fallback detection. Mas não tem endpoint e o port não está wired.

**Repositórios:** 0 — DI.cs com comentários TODO

---

## 6. Subdomain Orchestration — Estado: STUB

Todas as 8 features são TODO:

| Feature | Estado |
|---------|--------|
| `GenerateRobotFrameworkDraft` | TODO |
| `SuggestSemanticVersionWithAI` | TODO |
| `ValidateKnowledgeCapture` | TODO |
| `GenerateTestScenarios` | TODO |
| `ClassifyChangeWithAI` | TODO |
| `GetAiConversationHistory` | TODO |
| `SummarizeReleaseForApproval` | TODO |
| `AskCatalogQuestion` | TODO |

`AiOrchestrationDbContext`: 4 DbSets definidos mas sem configurações EF Core — dados não seriam persistidos correctamente.

---

## 7. Frontend AI Hub — Estado: PARTIAL

**Ficheiro:** `src/frontend/src/features/ai-hub/`

**Páginas (12) com integração real:**
- `AiAssistantPage` — chat com `aiGovernanceApi.sendMessage()`, histórico real
- `AiAgentsPage` — catalog real via `listAgents()`
- `ModelRegistryPage` — lista real via `listModels()`
- `AiPoliciesPage` — `listPolicies()` real
- `TokenBudgetPage` — `listBudgets()` real
- `AiAuditPage` — `listAuditEntries()` real
- `IdeIntegrationsPage` — `listIdeClients()` real
- `AiRoutingPage` — `listRoutingStrategies()` real

**⚠️ AssistantPanel.tsx — MOCK em contextos de detalhe:**

`AssistantPanel` é reutilizado em 4 páginas de detalhe (Service, Contract, Change, Incident). Contém função `buildGroundedContent()` que gera resposta mock baseada em heurísticas de keywords em vez de chamar o endpoint real de chat.

**Evidência:** `src/frontend/src/features/ai-hub/components/AssistantPanel.tsx`

---

## 8. Conformidade com a Visão Oficial

| Requisito (.github/copilot-instructions.md) | Estado | Evidência |
|---------------------------------------------|--------|-----------|
| IA interna como padrão | ✅ CUMPRIDO | Ollama default; OpenAI opcional |
| IA externa governada | ✅ CUMPRIDO | AIAccessPolicy.InternalOnly, AllowExternalAI |
| Model registry | ✅ CUMPRIDO | AIModel com 40+ props |
| Políticas por user/group/role/persona/team | ✅ CUMPRIDO | AIAccessPolicy.Scope enum |
| Quotas e orçamentos de tokens | ✅ CUMPRIDO | AiTokenQuotaPolicy, AIBudget |
| Auditoria completa | ✅ CUMPRIDO | AIUsageEntry 18 campos, AiExternalInferenceRecord |
| Agentes especializados por domínio | ✅ CUMPRIDO | 10 agentes com system prompts específicos |
| Sem chat genérico sem governança | ⚠️ PARCIAL | Governance existe; AssistantPanel tem mock |
| Persona-aware AI UX | ⚠️ PARCIAL | PersonaContext tem aiScope; chat igual para todos |
| Human-in-the-loop | ⚠️ PARCIAL | ReviewStatus existe; workflow formal ausente |
| IDE Extensions reais | ❌ AUSENTE | DB e UI existem; sem extensões VS Code/Visual Studio |
| Streaming de respostas | ❌ AUSENTE | Stream=false hardcoded |
| Tools/Function calling | ❌ AUSENTE | Campo declarado; executor não implementado |
| RAG/Retrieval real | ❌ INCERTO | Interfaces registadas; implementação não verificada |

---

## 9. Dependências Técnicas de IA

| Tecnologia | Estado |
|-----------|--------|
| Semantic Kernel | ❌ NÃO USADO |
| OpenAI .NET SDK | ❌ NÃO USADO |
| LangChain | ❌ NÃO USADO |
| Azure.AI | ❌ NÃO USADO |
| OllamaHttpClient (custom) | ✅ REAL |
| OpenAiHttpClient (custom) | ✅ REAL |
| Vector Database | ❌ AUSENTE |
| Embedding Service | ❌ AUSENTE |
| Ollama Docker container | ❌ NÃO no docker-compose |

**Nota:** O produto usa HTTP clients customizados em vez de SDKs oficiais. Funcional para inferência básica mas sem suporte a features avançadas (function calling, streaming, embeddings).

---

## 10. Contradições na Documentação de IA

| Fonte | Afirmação | Realidade do Código |
|-------|-----------|---------------------|
| `AI-LOCAL-IMPLEMENTATION-AUDIT.md` (março 2026) | "ZERO dependências de SDK de IA" | Parcialmente errado — HTTP clients customizados existem |
| `AI-LOCAL-IMPLEMENTATION-AUDIT.md` (março 2026) | "20-25% maturidade" | ~50-55% para Governance; correcto para ExternalAI/Orchestration |
| Auditoria AI Agents (julho 2025) | "75-80% maturidade" | Incorrecta — cálculo inclui todo o módulo; só Governance é 75%+ |
| Auditoria AI Agents (julho 2025) | "COSMÉTICO apenas para tools" | Correcto — tools não executam |
| Auditoria AI Agents (julho 2025) | "Streaming não implementado" | Correcto |
| Auditoria AI Agents (julho 2025) | "DbContexts não incluídos no pipeline" | INCORRECTO — já incluídos no WebApplicationExtensions |

**Conclusão:** Relatório de março 2026 é mais preciso que relatório de julho 2025 para maturidade global. O de julho 2025 sobrestima porque foca apenas no Governance subdomain.

---

## 11. Recomendações

| Prioridade | Acção |
|-----------|-------|
| P0 | Remover mock response generator de AssistantPanel.tsx |
| P1 | Completar ExternalAiDbContext (DbSets, configurações, repositórios) |
| P1 | Wiring de IExternalAIRoutingPort no DI |
| P1 | Implementar streaming nas AI providers (Ollama e OpenAI) |
| P1 | Wiring de tool execution no AiAgentRuntimeService |
| P2 | Completar 7 features do ExternalAI domain |
| P2 | Iniciar Orchestration domain (GenerateTestScenarios, SummarizeReleaseForApproval prioritários) |
| P2 | Verificar e completar implementações RAG (DocumentRetrievalService, etc.) |
| P2 | Criar extensão IDE real (VS Code prioritário) |
| P2 | Implementar persona-specific AI UX (system prompt por persona) |
| P3 | Migrar para Semantic Kernel para capacidades avançadas |
| P3 | Adicionar vector database (pgvector ou Qdrant) |
| P3 | Formalizar workflow de aprovação para AiAgentArtifact |
| P3 | Consolidar documentação de maturidade de IA num único relatório actualizado |
