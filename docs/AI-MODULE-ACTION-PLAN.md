# Plano de Ação — Módulo AIKnowledge

**Data de criação:** 2026-04-13
**Baseado na análise:** [AI-ARCHITECTURE.md](./AI-ARCHITECTURE.md), [AI-GOVERNANCE.md](./AI-GOVERNANCE.md)
**Módulo:** `src/modules/aiknowledge/`
**Responsável de produto:** Platform Engineering

---

## Contexto

Este documento consolida os gaps identificados na análise do módulo AIKnowledge e define um plano de ação concreto, priorizado e rastreável para endereçá-los.

A análise identificou **19 gaps** classificados em:
- 🔴 **4 Críticos** — impacto direto na fiabilidade e segurança
- 🟠 **5 Altos** — impacto significativo no produto
- 🟡 **7 Médios** — maturidade e valor do produto
- 🔵 **3 Baixos** — polish e completude

---

## Sumário executivo dos gaps

| ID | Título | Prioridade | Área |
|----|--------|------------|------|
| G-01 | Tool calling baseado em convenção textual (não nativo) | 🔴 Crítico | Runtime / Agents |
| G-02 | 6 phantom tools — definidas no catálogo, não implementadas | 🔴 Crítico | Runtime / Tools |
| G-03 | Token quota não validada antes da inferência | 🔴 Crítico | Governance / Runtime |
| G-04 | TeamId null no agent runtime — visibilidade de equipa não enforced | 🔴 Crítico | Governance / Security |
| G-05 | Streaming SSE não usado pelo frontend | 🟠 Alto | Frontend / UX |
| G-06 | Sem RAG semântico com embeddings no grounding | 🟠 Alto | Runtime / Intelligence |
| G-07 | Provider Anthropic/Claude em falta | 🟠 Alto | Runtime / Providers |
| G-08 | Sem context window management em conversas longas | 🟠 Alto | Runtime / Chat |
| G-09 | `SendAssistantMessage` monolito funcional (49.5 KB) | 🟠 Alto | Application / Design |
| G-10 | Guardrails definidos mas sem enforcement no pipeline | 🟡 Médio | Governance / Security |
| G-11 | Sem feedback loop operacional (rating → routing adjustment) | 🟡 Médio | Governance / AIOps |
| G-12 | `PlanExecution` desconectado do agent runtime | 🟡 Médio | Orchestration |
| G-13 | Onboarding session sem fluxo completo | 🟡 Médio | UX / Onboarding |
| G-14 | Agent Marketplace sem conteúdo operacional real | 🟡 Médio | Governance / UX |
| G-15 | Sem política de retenção de dados de IA | 🟡 Médio | Compliance / LGPD |
| G-16 | IDE integration sem SDK/extensão pública | 🟡 Médio | Developer Experience |
| G-17 | `get_contract_details`, `search_incidents`, etc. — phantom tools | 🔵 Baixo | Runtime / Tools |
| G-18 | Cobertura de testes da Orchestration parcial | 🔵 Baixo | Testes |
| G-19 | Sem endpoint de usage summary por persona | 🔵 Baixo | Governance / Reports |

---

## Plano de ação detalhado

### Fase 1 — Estabilização crítica

> Objetivo: eliminar comportamentos incorretos ou silenciosamente falhos que afetam produção.
> Duração estimada: 2 sprints

---

#### P1 — Implementar phantom tools em falta [G-02, G-17]

**Gap:** O `DefaultToolDefinitionCatalog` define 9 ferramentas (incluindo `get_contract_details`, `search_incidents`, `get_token_usage_summary`). Na infraestrutura só existem 3 implementações reais como `IAgentTool`. As restantes 6 são anunciadas no system prompt dos agents mas não são executáveis — quando chamadas, `AgentToolExecutor` devolve `"Tool not registered"` e o agent fica sem resposta válida.

**Ficheiros afetados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Tools/` — criação de novos tools
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/DependencyInjection.cs` — registo

**Ações:**

1. Criar `GetContractDetailsTool.cs` — usa `ICatalogGroundingReader` para buscar detalhes de um contrato pelo ID
2. Criar `SearchIncidentsTool.cs` — usa `IIncidentGroundingReader` para pesquisar incidentes por serviço/severidade/período
3. Criar `GetTokenUsageSummaryTool.cs` — usa `IAiTokenUsageLedgerRepository` para resumo de uso por utilizador/tenant
4. Criar `SearchKnowledgeTool.cs` — usa `IKnowledgeDocumentGroundingReader` para pesquisar documentos do Knowledge Hub
5. Criar `GetRunbookTool.cs` — pesquisa runbooks relevantes via Knowledge module
6. Criar `ListContractVersionsTool.cs` — lista versões de um contrato com breaking changes
7. Registar todos os novos tools em `DependencyInjection.cs` como `IAgentTool`
8. Adicionar testes unitários para cada tool em `tests/modules/aiknowledge/`

**Critérios de aceite:**
- Todos os 9 tools do `DefaultToolDefinitionCatalog` têm implementação `IAgentTool` correspondente
- `InMemoryToolRegistry` resolve todos os tools pelo nome
- Testes cobrem execução com input válido, input inválido e falha de dependência

---

#### P2 — Pré-validação de quota antes de inferência [G-03]

**Gap:** O `AiTokenQuotaService.ValidateQuotaAsync` existe mas não é chamado antes da inferência em `SendAssistantMessage` nem em `ExecuteAiChat`. A quota só é registada após consumo. Utilizadores podem ultrapassar limites antes de serem bloqueados.

**Ficheiros afetados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Features/SendAssistantMessage/SendAssistantMessage.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Runtime/Features/ExecuteAiChat/ExecuteAiChat.cs`

**Ações:**

1. Injetar `IAiTokenQuotaService` em `SendAssistantMessage.Handler`
2. Antes da chamada ao provider, chamar `ValidateQuotaAsync` com estimativa de tokens (comprimento da mensagem + contexto / 4)
3. Se quota excedida, retornar `Error.Business("AIKnowledge.QuotaExceeded", ...)` com detalhe da política violada
4. Repetir o mesmo padrão em `ExecuteAiChat.Handler`
5. Adicionar testes: quota permitida, quota de request excedida, quota diária excedida, quota mensal excedida
6. Adicionar i18n keys para mensagem de erro de quota no frontend

**Critérios de aceite:**
- Utilizador que excede quota recebe erro `QuotaExceeded` antes de consumir tokens
- Resposta inclui `policyName` e `limitType` para o frontend mostrar mensagem contextual
- Testes cobrem os 4 cenários de quota

---

#### P3 — Corrigir TeamId no agent runtime [G-04]

**Gap:** `AiAgentRuntimeService.ExecuteAsync()` passa `teamId: null` para `agent.IsAccessibleBy()` com comentário `"TeamId não disponível ainda"`. Agents com visibilidade `Team` são acessíveis a qualquer utilizador autenticado.

**Ficheiros afetados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Services/AiAgentRuntimeService.cs`
- `src/building-blocks/NexTraceOne.BuildingBlocks.Application/Abstractions/ICurrentUser.cs` (verificar se TeamId já existe)

**Ações:**

1. Verificar se `ICurrentUser` expõe `TeamId` ou `TeamIds`; se não, adicionar `IReadOnlyList<string> TeamIds` à interface e implementação
2. Em `AiAgentRuntimeService.ExecuteAsync`, obter o primeiro `TeamId` do utilizador atual
3. Passar o `teamId` real para `agent.IsAccessibleBy(currentUser.Id, teamId: currentUser.TeamIds.FirstOrDefault())`
4. Para agents com múltiplos teams, verificar se o utilizador pertence a qualquer um dos teams permitidos
5. Adicionar testes: utilizador com team correto tem acesso, utilizador sem team não tem acesso a agent de team

**Critérios de aceite:**
- Agent com `AgentVisibility.Team` só é executável por membros da equipa correta
- `AgentAccessDenied` é retornado para utilizadores fora da equipa
- Testes cobrem os cenários de acesso

---

#### P4 — Context window management [G-08]

**Gap:** Em conversas longas, o histórico de mensagens pode ultrapassar o context window do modelo. Não existe mecanismo de sliding window nem contagem de tokens antes do envio. Isto causa erros silenciosos ou truncagens inesperadas.

**Ficheiros afetados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Runtime/Features/ExecuteAiChat/ExecuteAiChat.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Features/SendAssistantMessage/SendAssistantMessage.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Domain/Governance/Entities/AIModel.cs` (adicionar `MaxContextTokens`)

**Ações:**

1. Adicionar campo `MaxContextTokens` à entidade `AIModel` (default: 4096; configurável por modelo)
2. Adicionar migração EF Core para o campo
3. Criar utilitário `ContextWindowManager` com método `TrimToFit(messages, maxTokens, reserveForCompletion)` que aplica sliding window (mantém system prompt + últimas N mensagens)
4. Integrar `ContextWindowManager` no `ExecuteAiChat` antes de chamar o provider
5. Integrar no `SendAssistantMessage` ao carregar histórico de mensagens
6. Logar quando truncagem ocorre (com aviso ao utilizador via metadata da resposta)
7. Adicionar testes para `ContextWindowManager` com conversas curtas, longas e edge cases

**Critérios de aceite:**
- Conversas com 100+ mensagens não causam erros por context overflow
- System prompt é sempre preservado na janela
- Campo `WasTruncated` na resposta sinaliza ao frontend quando o histórico foi truncado

---

### Fase 2 — Qualidade e experiência

> Objetivo: melhorar experiência do utilizador, completar providers e decomposição de código.
> Duração estimada: 2–3 sprints

---

#### P5 — Streaming SSE no frontend [G-05]

**Gap:** O backend tem `/api/v1/ai/chat/stream` com SSE via `CompleteStreamingAsync`. O frontend (`AiAssistantPage.tsx`) usa a API REST blocante — o utilizador espera pela resposta completa sem feedback visual.

**Ficheiros afetados:**
- `src/frontend/src/features/ai-hub/pages/AiAssistantPage.tsx`
- `src/frontend/src/features/ai-hub/api/aiGovernance.ts`
- `src/frontend/src/locales/en.json` (i18n keys se necessário)

**Ações:**

1. Criar função `sendMessageStreaming(message, conversationId, onChunk, onComplete, onError)` em `aiGovernance.ts` que usa `fetch` com `ReadableStream` para consumir SSE
2. No `AiAssistantPage`, detetar suporte a streaming (flag de feature ou config de provider) e escolher entre API blocante e streaming
3. Renderizar tokens incrementalmente à medida que chegam — usar estado local para acumular conteúdo parcial
4. Mostrar cursor animado durante streaming para indicar que a IA está a escrever
5. Tratar erros de rede a meio do stream com retry automático
6. Garantir que token usage e metadados chegam no evento final `[DONE]`
7. Adicionar testes para o componente com mock de SSE

**Critérios de aceite:**
- Tokens aparecem progressivamente no chat sem esperar pela resposta completa
- Cursor animado visível durante geração
- Cancelamento de stream ao navegar para outra página

---

#### P6 — Implementar provider Anthropic/Claude [G-07]

**Gap:** `DefaultModelCatalog` inclui `claude-3-5-sonnet` e `claude-3-haiku` como modelos externos. Não existe `AnthropicProvider` na infraestrutura — utilizadores com estes modelos configurados recebem `Provider 'anthropic' not found`.

**Ficheiros afetados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Providers/Anthropic/` — nova pasta
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Configuration/AnthropicOptions.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/DependencyInjection.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Configuration/` — `appsettings` config section

**Ações:**

1. Criar `AnthropicOptions.cs` com `ApiKey`, `BaseUrl` (default: `https://api.anthropic.com`), `DefaultChatModel`, `ApiVersion` (default: `2023-06-01`)
2. Criar `AnthropicHttpClient.cs` com métodos `ChatAsync(request)` e suporte a messages API (`/v1/messages`) com header `anthropic-version`
3. Criar `AnthropicProvider.cs` implementando `IAiProvider` e `IChatCompletionProvider`
4. Mapear formato Anthropic para `ChatCompletionResult` (Anthropic usa `content[0].text` em vez de `choices[0].message.content`)
5. Implementar `CompleteStreamingAsync` usando SSE da API Anthropic (`stream: true`)
6. Registar em `DependencyInjection.cs` condicional (`if AnthropicOptions.ApiKey not empty`)
7. Adicionar à documentação de configuração em `ENVIRONMENT-VARIABLES.md`
8. Adicionar testes com mock de HTTP para chat e health check

**Critérios de aceite:**
- `claude-3-5-sonnet` funciona end-to-end quando `ApiKey` configurado
- Provider é ignorado graciosamente quando `ApiKey` está vazio
- Testes cobrem sucesso, erro de API, e timeout

---

#### P7 — Decomposição de `SendAssistantMessage` [G-09]

**Gap:** O handler `SendAssistantMessage` tem 49.5 KB e combina num único ficheiro: routing strategy, context grounding, model authorization, quota check (a adicionar), inferência, fallback, persistência de conversa/mensagens, registo de auditoria e lógica de metadata.

**Ficheiros afetados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Features/SendAssistantMessage/SendAssistantMessage.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Services/` — novos serviços

**Ações:**

1. Criar `IContextGroundingService` com método `BuildGroundingContextAsync(command, cancellationToken): Task<GroundingContext>` — extrai a lógica de retrieval de documentos, DB e telemetria
2. Criar `IAiRoutingResolver` com método `ResolveAsync(command, strategies, cancellationToken): Task<ResolvedModel?>` — extrai a lógica de resolução de modelo por routing strategy
3. Criar `IConversationPersistenceService` com métodos `GetOrCreateAsync` e `PersistMessagePairAsync` — extrai a lógica de criação/atualização de conversa e mensagens
4. Refatorar `SendAssistantMessage.Handler` para orquestrar estes serviços, reduzindo o handler a ~150 linhas de orquestração clara
5. Registar os novos serviços em `DependencyInjection.cs`
6. Manter todos os testes existentes a passar (sem mudança de comportamento)
7. Adicionar testes unitários para cada serviço extraído

**Critérios de aceite:**
- `SendAssistantMessage.cs` fica abaixo de 200 linhas
- Cada serviço extraído tem responsabilidade única e testável de forma independente
- Todos os testes existentes continuam a passar

---

### Fase 3 — Inteligência e governance avançada

> Objetivo: elevar a qualidade das respostas, proteger o sistema e criar loops de melhoria contínua.
> Duração estimada: 3–4 sprints

---

#### P8 — Guardrail enforcement no pipeline [G-10]

**Gap:** `AiGuardrail` tem entidade, catálogo, CRUD completo e endpoints. O pipeline de `SendAssistantMessage` e `AiAgentRuntimeService` não invoca guardrails antes de enviar inputs ao LLM.

**Ficheiros afetados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Services/` — `IAiGuardrailEnforcementService`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Features/SendAssistantMessage/SendAssistantMessage.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Services/AiAgentRuntimeService.cs`

**Ações:**

1. Criar `IAiGuardrailEnforcementService` com `EvaluateInputAsync(input, tenantId, persona, cancellationToken): Task<GuardrailEvaluationResult>`
2. Implementar verificações:
   - Comprimento máximo de input
   - Palavras/padrões bloqueados por política
   - Deteção de prompt injection (`"ignore previous instructions"`, `"system:"`, etc.)
   - Dados sensíveis (regex configuráveis: PII, tokens, passwords)
3. Criar `EvaluateOutputAsync(output, tenantId, cancellationToken)` para verificar outputs antes de retornar ao utilizador
4. Integrar no início de `SendAssistantMessage.Handler` (input) e antes de persistir a resposta (output)
5. Quando guardrail bloqueia, retornar `Error.Business("AIKnowledge.GuardrailViolation", ...)` com detalhes auditáveis
6. Registar evento de violação em `AIUsageEntry` com resultado `GuardrailBlocked`
7. Adicionar testes para cada tipo de guardrail

**Critérios de aceite:**
- Input com prompt injection é bloqueado antes de chegar ao LLM
- Output com padrões sensíveis é filtrado antes de retornar ao utilizador
- Violações são auditadas em `AIUsageEntry` e visíveis no audit log

---

#### P9 — RAG semântico com embeddings [G-06]

**Gap:** `IEmbeddingProvider` está implementado (Ollama `nomic-embed-text` + OpenAI `text-embedding-3-small`), mas nunca é chamado no pipeline de grounding. Não existe vector store. O `DocumentRetrievalService` usa apenas text search. Para um Knowledge Hub com runbooks, contratos e notas operacionais, a ausência de busca semântica limita a qualidade do grounding.

**Ficheiros afetados:**
- Schema PostgreSQL — adição de coluna `embedding vector(768)` via migração
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Services/DocumentRetrievalService.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Governance/Persistence/` — nova tabela ou coluna na entidade de knowledge source

**Ações:**

1. Habilitar extensão `pgvector` no PostgreSQL (script de migração com `CREATE EXTENSION IF NOT EXISTS vector`)
2. Adicionar coluna `Embedding vector(768)` à tabela `ai_knowledge_sources` (ou nova tabela de chunks)
3. Criar job `EmbeddingIndexJob` (Quartz.NET) que, ao criar/atualizar um `AIKnowledgeSource`, gera embedding via `IEmbeddingProvider` e persiste
4. Em `DocumentRetrievalService.SearchAsync`, fazer similarity search via `<=>` (cosine distance) quando embedding do query estiver disponível, com fallback para text search
5. Criar `IEmbeddingCacheService` para evitar recalcular embeddings de queries frequentes
6. Documentar requisito de `pgvector` em `LOCAL-SETUP.md` e `DEPLOYMENT-ARCHITECTURE.md`
7. Adicionar testes de integração para indexação e busca semântica

**Critérios de aceite:**
- Perguntas semanticamente equivalentes a documentos existentes retornam resultados relevantes
- Fallback para text search quando embedding não disponível
- Job de indexação é idempotente

---

#### P10 — Native function calling (OpenAI/Anthropic) [G-01]

**Gap:** O `AiAgentRuntimeService` usa o padrão textual `[TOOL_CALL: tool_name({"arg":"value"})]` para detetar chamadas de ferramentas. Este padrão depende de que o LLM siga a convenção e pode falhar silenciosamente. OpenAI e Anthropic têm suporte nativo a function calling com JSON estruturado garantido.

**Ficheiros afetados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Runtime/Abstractions/IChatCompletionProvider.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Providers/OpenAI/OpenAiProvider.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Providers/Anthropic/AnthropicProvider.cs` (novo)
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Services/AiAgentRuntimeService.cs`

**Ações:**

1. Criar interface `IFunctionCallingChatProvider` (extends `IChatCompletionProvider`) com método `CompleteWithToolsAsync(request, tools, cancellationToken): Task<FunctionCallingResult>`
2. `FunctionCallingResult` inclui `Content`, `ToolCalls: List<NativeToolCall>`, `FinishReason`
3. Implementar em `OpenAiProvider` usando o `tools` array nativo da OpenAI API
4. Implementar em `AnthropicProvider` usando o `tools` block nativo da Anthropic API (quando disponível)
5. Em `AiAgentRuntimeService`, verificar se o provider implementa `IFunctionCallingChatProvider`:
   - Se sim: usar native function calling
   - Se não: manter o padrão textual `[TOOL_CALL:]` como fallback (compatibilidade com Ollama)
6. Manter `DetectToolCall` como fallback exclusivo para providers sem suporte nativo
7. Adicionar testes para o fluxo nativo e o fluxo de fallback

**Critérios de aceite:**
- Agents com OpenAI usam function calling nativo (100% fiável)
- Agents com Ollama continuam a funcionar com o padrão textual
- Tool calls nativas são auditadas no `AiAgentExecution.StepsJson`

---

#### P11 — Feedback loop operacional [G-11]

**Gap:** `AiFeedback` e `ListNegativeFeedback` existem. Mas não há mecanismo que, face a feedback negativo acumulado num modelo/agent, ajuste routing strategies, aumente o sensitivity level ou acione alerta ao Platform Admin.

**Ficheiros afetados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Contracts/IntegrationEvents/AiGovernanceIntegrationEvents.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Features/SubmitAiFeedback/SubmitAiFeedback.cs`
- Nova feature `EvaluateFeedbackThresholds`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Governance/` — Quartz job

**Ações:**

1. Adicionar evento `ModelFeedbackThresholdExceeded(modelId, agentId, negativeCount, period)` a `AiGovernanceIntegrationEvents`
2. Criar `FeedbackThresholdJob` (Quartz.NET, execução horária) que:
   - Agrega feedback negativo por modelo e agent nas últimas 24h
   - Quando excede threshold configurável (ex: 5 negativos consecutivos), publica o evento
3. O evento é consumido pelo `Notifications` module para alertar o Platform Admin
4. Opcionalmente, o evento pode acionar redução automática de `AIRoutingStrategy.Priority` para o modelo problemático
5. Expor métricas de feedback na view de `GetAgent` (taxa de positivos/negativos nas últimas 24h/7d)
6. Adicionar testes para o job com threshold atingido e não atingido

**Critérios de aceite:**
- Platform Admin recebe notificação quando modelo/agent acumula feedback negativo acima do threshold
- Threshold é configurável por tenant (parametrizado no banco, não em `appsettings`)
- Evento é auditável e rastreável

---

### Fase 4 — Completude e compliance

> Objetivo: fechar gaps de compliance, developer experience e cobertura de testes.
> Duração estimada: 2–3 sprints

---

#### P12 — Política de retenção de dados de IA [G-15]

**Gap:** `AIUsageEntry`, `AiAssistantConversation`, `AiMessage`, `AiTokenUsageLedger` crescem sem política de retenção. Para um produto enterprise/self-hosted com LGPD/GDPR como referência, a ausência de data lifecycle management é um gap de compliance.

**Ficheiros afetados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Governance/` — novo Quartz job
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Domain/Governance/Entities/AIAccessPolicy.cs` — campo `DataRetentionDays`
- Migração EF Core para o novo campo

**Ações:**

1. Adicionar campo `DataRetentionDays` (nullable, default null = sem expiração) à entidade `AIAccessPolicy`
2. Adicionar migração
3. Criar `AiDataRetentionJob` (Quartz.NET, execução nocturna, 02:00 UTC) que:
   - Para cada tenant com política activa com `DataRetentionDays > 0`:
     - Elimina `AiMessage` com `CreatedAt < now - RetentionDays`
     - Arquiva (soft-delete) `AiAssistantConversation` sem mensagens
     - Elimina `AIUsageEntry` acima do período configurado
     - Elimina `AiTokenUsageLedger` acima do período configurado
4. Log estruturado de cada operação de retenção com contagem de registos eliminados
5. Documentar em `SECURITY-ARCHITECTURE.md` e `docs/security/` a política de retenção
6. Adicionar testes para o job com dados dentro e fora do período de retenção

**Critérios de aceite:**
- Dados de IA são eliminados automaticamente após período configurado
- Job é idempotente e seguro para re-execução
- Operações de retenção são auditadas em log estruturado

---

#### P13 — View de usage summary por persona [G-19]

**Gap:** Não existe endpoint de usage aggregado de IA por dimensão (equipa, modelo, período). O Executive e Platform Admin precisam desta visão para governance de custos e uso.

**Ficheiros afetados:**
- Nova feature `GetAiUsageDashboard` em `Governance/Features/`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.API/Governance/Endpoints/Endpoints/AiGovernanceEndpointModule.cs`
- `src/frontend/src/features/ai-hub/pages/TokenBudgetPage.tsx`

**Ações:**

1. Criar feature `GetAiUsageDashboard` com query `(TenantId, Period, GroupBy: model|team|user|provider, Top: int)`
2. Handler agrega `AIUsageEntry` por dimensão solicitada dentro do período
3. Response inclui: total tokens, total requests, custo estimado (se `CostPerToken` configurado no modelo), breakdown por dimensão
4. Expor via `GET /api/v1/ai/usage/dashboard?period=7d&groupBy=model`
5. No frontend, adicionar tab "Usage Dashboard" ao `TokenBudgetPage` com:
   - Gráfico de barras ECharts (tokens por modelo/provider)
   - Tabela de top utilizadores por tokens
   - Indicador de custo estimado por equipa/domínio
6. Segmentar por persona: Executive vê resumo por domínio/equipa; Platform Admin vê detalhe por utilizador e modelo
7. Adicionar testes para a feature

**Critérios de aceite:**
- Dashboard disponível para personas Executive e Platform Admin
- Dados em tempo real (sem cache > 5 minutos)
- Exportação para CSV no frontend

---

#### P14 — Cobertura de testes de Orchestration [G-18]

**Gap:** Das 18 features de Orchestration, aproximadamente 8 não têm testes dedicados.

**Ficheiros afetados:**
- `tests/modules/aiknowledge/NexTraceOne.AIKnowledge.Tests/Orchestration/Features/`

**Ações — criar testes para as features sem cobertura:**

1. `GenerateArchitectureDecisionRecordTests.cs` — testa geração de ADR com contexto válido e provider indisponível
2. `EvaluateArchitectureFitnessTests.cs` — testa avaliação com score, findings e recomendações
3. `EvaluateDocumentationQualityTests.cs` — testa análise com output estruturado
4. `RecommendTemplateForServiceTests.cs` — testa resolução de template com e sem sugestões
5. `SummarizeReleaseForApprovalTests.cs` — testa sumário com release válido e sem change data
6. `GenerateRobotFrameworkDraftTests.cs` — testa geração de draft com e sem casos de teste base
7. `GenerateTestScenariosTests.cs` (complementar) — casos edge e provider fallback

**Critérios de aceite:**
- Todas as 18 features de Orchestration têm pelo menos 3 testes: sucesso, validação falha, provider indisponível
- Cobertura de Orchestration > 85%

---

#### P15 — Fluxo de onboarding completo [G-13]

**Gap:** `StartOnboardingSession`, `GetOnboardingSession`, `OnboardingSession` existem. Mas não há endpoint que guie o utilizador por um onboarding assistido por IA no primeiro acesso.

**Ficheiros afetados:**
- Endpoints de onboarding em `AiGovernanceEndpointModule.cs`
- `src/frontend/src/features/ai-hub/pages/` — novo `AiOnboardingPage.tsx` ou modal integrado na home

**Ações:**

1. Verificar e completar endpoints de onboarding (criar se em falta): `POST /api/v1/ai/onboarding/start`, `GET /api/v1/ai/onboarding/{id}`
2. No frontend, detetar utilizador sem onboarding completo (flag em perfil ou call ao endpoint)
3. Mostrar fluxo de onboarding interativo assistido por IA:
   - Passo 1: Persona selection (Engineer / Tech Lead / Architect / etc.)
   - Passo 2: Introdução ao AI Assistant com exemplo de prompt
   - Passo 3: Introdução ao Agent Marketplace
   - Passo 4: Configuração de contexto padrão (service, team)
4. Completar `OnboardingSession` após último passo
5. Redirecionar para AI Assistant com primeira mensagem sugerida
6. Adicionar testes frontend para cada passo do fluxo

**Critérios de aceite:**
- Novo utilizador é guiado por onboarding no primeiro acesso à secção de IA
- Fluxo é dismissível e reiniciável em Settings
- Onboarding completion é registado e auditável

---

#### P16 — PlanExecution integrado no agent runtime [G-12]

**Gap:** `PlanExecution` feature e `AIExecutionPlan` entity existem mas nunca são chamados pelo `AiAgentRuntimeService`.

**Ficheiros afetados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Features/PlanExecution/PlanExecution.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Services/AiAgentRuntimeService.cs`

**Ações:**

1. Verificar implementação actual de `PlanExecution` — se é stub, completar com lógica de planning (decomposição de task em steps)
2. Integrar `PlanExecution` como passo opcional no `AiAgentRuntimeService` para agents com `Category` que beneficiam de planning (ex: `ChangeIntelligence`, `SecurityAudit`, `ApiDesign`)
3. O plano gerado é injetado no system prompt como contexto estruturado antes da execução principal
4. Persistir o plano em `AIExecutionPlan` com link para `AiAgentExecution`
5. Expor o plano na resposta de `ExecuteAgent` para explicabilidade
6. Adicionar testes para o fluxo com planning ativo e desativado

**Critérios de aceite:**
- Agents de alta complexidade produzem plano explícito antes de executar
- Plano é visível no detalhe da execução (`AgentDetailPage`)
- Planning é opcional e controlado por flag no `AiAgent`

---

#### P17 — IDE integration SDK e documentação [G-16]

**Gap:** A infraestrutura server-side de IDE integration está bem implementada (client registration, capability policies, IDE query sessions). Mas não há SDK ou extensão pública que developers possam instalar.

**Ficheiros afetados:**
- Nova pasta `tools/ide-extensions/vscode/` (scaffold mínimo de extensão)
- `docs/AI-DEVELOPER-EXPERIENCE.md`

**Ações:**

1. Criar scaffold mínimo de extensão VS Code em `tools/ide-extensions/vscode/`:
   - `package.json` com publisher, contributes (commands, configuration)
   - Comando `nextraceone.chat` que abre webview ou chama API
   - Autenticação via API key (usando `IAiIdeClientRegistration`)
   - Envio de query via `POST /api/v1/ai/ide/query` com contexto de ficheiro activo
2. Documentar protocolo de integração em `docs/AI-DEVELOPER-EXPERIENCE.md`:
   - Como registar um IDE client
   - Formato de autenticação
   - Endpoints disponíveis para IDE
   - Capability policies disponíveis
3. Adicionar à secção de README da extensão os passos de instalação e configuração
4. Não é necessário publicar no VS Code Marketplace nesta fase — scaffold + documentação é suficiente

**Critérios de aceite:**
- Developer consegue instalar e configurar a extensão VS Code seguindo a documentação
- Extensão autentica e envia queries via API de IDE integration
- Capability policies são respeitadas na extensão

---

#### P18 — Agent Marketplace com conteúdo operacional [G-14]

**Gap:** `GetAgentMarketplace` existe e `AgentMarketplacePage.tsx` existe. Mas o marketplace é uma estrutura vazia sem ratings, instalações ou métricas de uso.

**Ficheiros afetados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Orchestration/Features/GetAgentMarketplace/GetAgentMarketplace.cs`
- `src/frontend/src/features/ai-hub/pages/AgentMarketplacePage.tsx`

**Ações:**

1. Enriquecer `GetAgentMarketplace` response com: `ExecutionCount`, `AverageRating`, `PublishedAt`, `Tags`, `Capabilities`
2. Agregar `AiFeedback` por agent para calcular rating médio no marketplace
3. Adicionar campo `Tags` à entidade `AiAgent` para categorização no marketplace
4. No frontend:
   - Grid de cards com rating visual (stars), execution count e tags
   - Filtros por categoria, rating, ownership type
   - Preview de agent com system prompt e exemplos de input/output
   - Botão "Add to My Agents" para utilização rápida
5. Agents do sistema (`AgentOwnershipType.System`) aparecem sempre; agents de tenant aparecem filtrados por visibilidade
6. Adicionar testes para o endpoint e componente de marketplace

**Critérios de aceite:**
- Marketplace mostra agents com rating, usage count e tags
- Filtros funcionam e atualizam resultados em tempo real
- Developer consegue instalar um agent do marketplace num clique

---

## Resumo de prioridades e fases

```
FASE 1 — Estabilização crítica (sprints 1-2)
  P1  Implementar phantom tools em falta         [G-02, G-17]  🔴 Crítico
  P2  Pré-validação de quota antes de inferência [G-03]        🔴 Crítico
  P3  Corrigir TeamId no agent runtime           [G-04]        🔴 Crítico
  P4  Context window management                  [G-08]        🟠 Alto

FASE 2 — Qualidade e experiência (sprints 3-5)
  P5  Streaming SSE no frontend                  [G-05]        🟠 Alto
  P6  Implementar provider Anthropic/Claude       [G-07]        🟠 Alto
  P7  Decomposição de SendAssistantMessage        [G-09]        🟠 Alto

FASE 3 — Inteligência e governance avançada (sprints 6-9)
  P8  Guardrail enforcement no pipeline          [G-10]        🟡 Médio
  P9  RAG semântico com embeddings               [G-06]        🟠 Alto
  P10 Native function calling                    [G-01]        🔴 Crítico
  P11 Feedback loop operacional                  [G-11]        🟡 Médio

FASE 4 — Completude e compliance (sprints 10-12)
  P12 Política de retenção de dados de IA        [G-15]        🟡 Médio
  P13 View de usage summary por persona          [G-19]        🔵 Baixo
  P14 Cobertura de testes de Orchestration       [G-18]        🔵 Baixo
  P15 Fluxo de onboarding completo               [G-13]        🟡 Médio
  P16 PlanExecution integrado no agent runtime   [G-12]        🟡 Médio
  P17 IDE integration SDK e documentação         [G-16]        🟡 Médio
  P18 Agent Marketplace com conteúdo operacional [G-14]        🟡 Médio
```

---

## Dependências entre itens

```
P1 (phantom tools) ──────────────────────────────> P10 (native function calling) 
P2 (quota pre-check) ────────────────────────────> P7 (decomposição SendAssistantMessage)
P3 (TeamId) ─────────────────────────────────────> sem dependências externas
P4 (context window) ─────────────────────────────> P7 (decomposição SendAssistantMessage)
P6 (Anthropic provider) ─────────────────────────> P10 (native function calling)
P7 (decomposição) ───────────────────────────────> P8 (guardrails), P2 (quota)
P9 (RAG embeddings) ─────────────────────────────> pgvector instalado no PostgreSQL
P11 (feedback loop) ─────────────────────────────> Notifications module consumer
P12 (retenção) ──────────────────────────────────> P15 (onboarding), P13 (usage dashboard)
P15 (onboarding) ────────────────────────────────> P18 (marketplace)
```

---

## Critérios de qualidade transversais

Todos os itens deste plano devem cumprir os seguintes critérios, independentemente da fase:

1. **i18n obrigatório** — todo texto visível em português/inglês via chaves de tradução
2. **CancellationToken** em toda operação async
3. **Testes mínimos** — sucesso, validação falha, dependência indisponível
4. **Logging estruturado** — operações relevantes com nível e campos corretos
5. **Auditoria** — ações sensíveis registadas em `AIUsageEntry` ou log de auditoria
6. **Persona awareness** — endpoints e UX refletem o utilizador atual
7. **Segurança** — sem exposição de segredos, sem bypass de autorização no frontend
8. **Migrações coerentes** — nomes descritivos, sem breaking changes implícitos
9. **Documentação inline** — comentários XML em métodos e entidades públicas
10. **Sem breaking changes de API** — novos campos em responses são sempre nullable/optional

---

## Rastreabilidade

| Item do plano | Gap(s) | Ficheiros principais | Testes esperados |
|---------------|--------|----------------------|------------------|
| P1 | G-02, G-17 | `Runtime/Tools/*.cs` | `Runtime/Tools/*Tests.cs` |
| P2 | G-03 | `SendAssistantMessage.cs`, `ExecuteAiChat.cs` | `SendAssistantMessageTests`, `ExecuteAiChatTests` |
| P3 | G-04 | `AiAgentRuntimeService.cs` | `AiAgentRuntimeServiceTests.cs` |
| P4 | G-08 | `AIModel.cs`, `ContextWindowManager.cs` | `ContextWindowManagerTests.cs` |
| P5 | G-05 | `AiAssistantPage.tsx`, `aiGovernance.ts` | `AiAssistantPage.test.tsx` |
| P6 | G-07 | `Runtime/Providers/Anthropic/*.cs` | `AnthropicProviderTests.cs` |
| P7 | G-09 | `SendAssistantMessage.cs`, `IContextGroundingService.cs` | Tests por serviço |
| P8 | G-10 | `IAiGuardrailEnforcementService.cs` | `GuardrailEnforcementTests.cs` |
| P9 | G-06 | `DocumentRetrievalService.cs`, migração pgvector | `EmbeddingSearchTests.cs` |
| P10 | G-01 | `OpenAiProvider.cs`, `AnthropicProvider.cs` | `FunctionCallingTests.cs` |
| P11 | G-11 | `FeedbackThresholdJob.cs`, `AiGovernanceIntegrationEvents.cs` | `FeedbackThresholdJobTests.cs` |
| P12 | G-15 | `AiDataRetentionJob.cs` | `AiDataRetentionJobTests.cs` |
| P13 | G-19 | `GetAiUsageDashboard.cs`, `TokenBudgetPage.tsx` | `GetAiUsageDashboardTests.cs` |
| P14 | G-18 | `Orchestration/Features/*Tests.cs` | 8 novos ficheiros de teste |
| P15 | G-13 | Onboarding endpoints, `AiOnboardingPage.tsx` | `AiOnboardingPageTests.tsx` |
| P16 | G-12 | `PlanExecution.cs`, `AiAgentRuntimeService.cs` | `PlanExecutionIntegrationTests.cs` |
| P17 | G-16 | `tools/ide-extensions/vscode/` | Documentação + smoke test |
| P18 | G-14 | `GetAgentMarketplace.cs`, `AgentMarketplacePage.tsx` | `GetAgentMarketplaceTests.cs` |

---

*Este documento deve ser revisitado no início de cada fase para ajustar prioridades com base no estado real da implementação.*
