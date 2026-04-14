# Plano de Execução v2 — Módulo AIKnowledge

**Data de criação:** 2026-04-14  
**Baseado na análise:** Análise Detalhada do Módulo de IA — NexTraceOne (2026-04-14)  
**Módulo:** `src/modules/aiknowledge/`  
**Referência anterior:** [AI-MODULE-ACTION-PLAN.md](./AI-MODULE-ACTION-PLAN.md) — P1–P18 concluídos em 2026-04-13  
**Responsável de produto:** Platform Engineering

---

## Contexto

Este documento consolida a análise de segunda geração do módulo AIKnowledge, realizada após a conclusão dos 18 itens do plano anterior (P1–P18). A análise identificou gaps remanescentes, riscos operacionais e novas capacidades que diferenciam o NexTraceOne no mercado enterprise.

O módulo tem uma **arquitectura de referência enterprise** — bem segmentada, auditável, governada e com fronteiras claras de bounded context. Os gaps identificados são **pontuais e cirúrgicos**, não arquitecturais. As novas capacidades propostas reforçam directamente os pilares de Change Intelligence, Contract Governance e Operational Reliability.

---

## Estado actual — O que está bem construído

Antes de mapear os gaps, é importante reconhecer o que está sólido:

| Área | Estado |
|------|--------|
| Domain model (27 entidades fortemente tipadas e auditáveis) | ✅ Concluído |
| Providers de IA: Ollama, OpenAI, Anthropic/Claude | ✅ Concluído |
| Native function calling (OpenAI + Anthropic) | ✅ Concluído |
| 9 agent tools implementadas e registadas | ✅ Concluído |
| Grounding cross-module: Catalog, ChangeIntelligence, Incidents, KnowledgeHub | ✅ Concluído |
| RAG com embeddings (cosine similarity em memória + EmbeddingIndexJob) | ✅ Concluído |
| Guardrail enforcement no pipeline (input + output) | ✅ Concluído |
| Context window management com sliding window | ✅ Concluído |
| Streaming SSE no frontend (AiAssistantPage) | ✅ Concluído |
| Pré-validação de quota em SendAssistantMessage | ✅ Concluído |
| AiDataRetentionJob (LGPD/GDPR) | ✅ Concluído |
| FeedbackThresholdJob (feedback loop com alerta) | ✅ Concluído |
| Fluxo de onboarding assistido por IA | ✅ Concluído |
| PlanExecution integrado no SendAssistantMessage | ✅ Concluído |
| IDE Extension VS Code scaffold (tools/ide-extensions/vscode/) | ✅ Concluído |
| Agent Marketplace com ratings, execution count, tags | ✅ Concluído |
| Frontend AIHub completo: Assistant, Agents, ModelRegistry, Audit, Policies, TokenBudget, Routing, IdeIntegrations | ✅ Concluído |

---

## Gaps identificados na análise v2

### Sumário executivo

| ID | Título | Prioridade | Área |
|----|--------|------------|------|
| C-01 | Quota não pré-validada em AiAgentRuntimeService | 🔴 Crítico | Governance / Runtime |
| C-02 | Persona universal "Engineer" como fallback de segurança | 🔴 Crítico | Identity / Security |
| C-03 | Contract Grounding Reader em falta | 🔴 Crítico | Grounding / Contracts |
| A-01 | RAG pseudo-semântico — sem pgvector real | 🟠 Alto | Infrastructure / RAG |
| A-02 | Sem provider fallback automático via health checks | 🟠 Alto | Runtime / Resilience |
| A-03 | AiRoutingResolver retorna modelo vazio como fallback | 🟠 Alto | Routing / Reliability |
| A-04 | AIExecutionPlan sem FK para AiAgentExecution | 🟠 Alto | Domain / Auditability |
| M-01 | Guardrails com strings livres em vez de enums | 🟡 Médio | Domain / Correctness |
| M-02 | Feedback loop → routing adjustment efectivo em falta | 🟡 Médio | Governance / AIOps |
| M-03 | Embedding cache sem LRU correcto | 🟡 Médio | Infrastructure / Memory |
| M-04 | Sem contrato estável de erros de IA no frontend | 🟡 Médio | Frontend / DX |
| M-05 | Explicabilidade activa das respostas em falta | 🟡 Médio | UX / Trust |
| B-01 | DefaultModelCatalog e seeds hardcoded | 🔵 Baixo | Configuration |
| B-02 | IDE extension sem end-to-end de capability policies | 🔵 Baixo | Developer Experience |

---

## Novas capacidades propostas

| ID | Capacidade | Pilar reforçado | Valor |
|----|-----------|-----------------|-------|
| N-01 | AI Diff para Contratos | Contract Governance | ⭐⭐⭐ Alto |
| N-02 | Incident War Room Agent | Operational Reliability | ⭐⭐⭐ Alto |
| N-03 | AI Release Gate Advisor | Change Intelligence | ⭐⭐⭐ Alto |
| N-04 | Semantic Catalog Search | Service Governance / Source of Truth | ⭐⭐ Médio |
| N-05 | AI Budget Alert Proactivo | FinOps contextual | ⭐⭐ Médio |
| N-06 | Prompt Template Marketplace | AI Governance / Knowledge | ⭐ Baixo |

---

## Plano de execução detalhado

### Fase 1 — Estabilização crítica

> Objetivo: eliminar riscos de segurança e fiabilidade em runtime.  
> Duração estimada: 1–2 sprints

---

#### E-C01 — Pré-validação de quota em AiAgentRuntimeService

**Gap:** `AiAgentRuntimeService.ExecuteAsync()` não injeta `IAiTokenQuotaService` nem chama `ValidateQuotaAsync` antes de invocar o provider. Execuções de agents pesados (com múltiplos tool loops até `MaxToolIterations = 5`) podem consumir tokens sem pré-verificação, ultrapassando limites de orçamento sem aviso.

**Ficheiros afectados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Services/AiAgentRuntimeService.cs`
- `tests/modules/aiknowledge/NexTraceOne.AIKnowledge.Tests/Governance/Services/AiAgentRuntimeServiceTests.cs`

**Acções:**
1. Injectar `IAiTokenQuotaService` no constructor de `AiAgentRuntimeService`
2. Após resolver o modelo (passo 4 do pipeline), chamar `ValidateQuotaAsync` com estimativa de tokens (`input.Length / 4 + SystemPromptLength / 4 + ReservedCompletionTokens`)
3. Se quota excedida, retornar `AiGovernanceErrors.QuotaExceeded(...)` com `policyName` e `limitType`
4. Adicionar testes: quota permitida, quota de request excedida, quota diária excedida

**Critérios de aceite:**
- Agent que excede quota recebe erro `QuotaExceeded` antes de consumir tokens
- Resposta inclui `policyName` e `limitType`
- Comportamento consistente com o já implementado em `SendAssistantMessage`

---

#### E-C02 — Persona explícita no JWT

**Gap:** `AIContextBuilder` infere a persona por permissões (`HasPermission("aiknowledge:write") → "Engineer"`). O fallback final retorna `"Engineer"` para qualquer utilizador sem permissões correspondentes. Utilizadores de leitura, auditores e executivos recebem contextos de IA desenhados para engineers — com mais detalhe técnico e fontes inapropriadas ao seu papel.

**Ficheiros afectados:**
- `src/building-blocks/NexTraceOne.BuildingBlocks.Application/Abstractions/ICurrentUser.cs`
- `src/building-blocks/NexTraceOne.BuildingBlocks.Infrastructure/Security/CurrentUser.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Context/AIContextBuilder.cs`

**Acções:**
1. Adicionar claim `x-nxt-persona` ao JWT emitido pelo módulo de Identity
2. Em `ICurrentUser`, expor `string? Persona` que lê o claim `x-nxt-persona`
3. Em `AIContextBuilder.InferPersona()`, priorizar `currentUser.Persona` quando disponível
4. Manter inferência por permissões como fallback (não quebrando comportamento actual)
5. Adicionar testes: persona explícita no claim, persona inferida por permissões, fallback para "Engineer"

**Critérios de aceite:**
- Utilizador com claim `x-nxt-persona: Auditor` recebe fontes e detalhe adequados ao Auditor
- Sem quebra de comportamento para utilizadores sem o novo claim
- Persona é auditada em `AIUsageEntry.PersonaUsed`

---

#### E-C03 — Contract Grounding Reader

**Gap:** O `CrossModuleGroundingReaders.cs` implementa readers para: Catalog/Services, ChangeIntelligence/Releases, Incidents, KnowledgeHub. **Não existe reader para o módulo de Contracts.** Para use cases como `ContractExplanation` e `ContractGeneration`, o grounding não carrega o contrato real — depende apenas do `ContextBundle` passado pelo frontend, que pode estar incompleto ou ausente.

**Ficheiros afectados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Runtime/Abstractions/IGroundingReaders.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Context/CrossModuleGroundingReaders.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Services/ContextGroundingService.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/DependencyInjection.cs` (para registo)

**Acções:**
1. Definir `ContractGroundingContext` record com campos: `ContractId`, `Title`, `ServiceName`, `ContractType` (REST/SOAP/Event), `Version`, `Status`, `OwnerTeam`, `EndpointCount`, `LastChangedAt`, `Summary`
2. Definir `IContractGroundingReader` em `IGroundingReaders.cs` com método `FindContractsAsync(contractId?, serviceId?, searchTerm, maxResults, ct)`
3. Implementar `ContractGroundingReader` em `CrossModuleGroundingReaders.cs` com acesso somente-leitura ao `ContractDbContext`
4. Integrar o reader em `ContextGroundingService.ResolveGroundingAsync()` para use cases `ContractExplanation` e `ContractGeneration`
5. Registar em `DependencyInjection.cs`
6. Adicionar testes de integração para o reader e para o grounding de contratos

**Critérios de aceite:**
- Perguntas sobre contratos recebem grounding com dados reais do módulo Contracts
- Use case `ContractExplanation` inclui versão, endpoints, owner team e status na resposta
- Use case `ContractGeneration` usa contrato existente como base quando disponível

---

### Fase 2 — Fiabilidade e escalabilidade

> Objetivo: tornar o sistema fiável em condições de carga e falha de providers.  
> Duração estimada: 2–3 sprints

---

#### E-A01 — RAG escalável com pgvector real

**Gap:** Os embeddings são armazenados como `string EmbeddingJson` (JSON serializado) nas `AIKnowledgeSources`. A comparação de similaridade é feita em memória (deserialização de JSON + cálculo de cosine em C#). Para volumes >10k documentos, esta abordagem é ineficiente e não escalável. Não existe extensão `pgvector`, não existe coluna do tipo `vector(768)`, e o retrieval semântico real com índice ANN (HNSW/IVFFlat) não está implementado.

**Ficheiros afectados:**
- Migração EF Core — nova coluna `embedding vector(768)` em `ai_knowledge_sources`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Services/DocumentRetrievalService.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Governance/Jobs/EmbeddingIndexJob.cs`
- `docs/LOCAL-SETUP.md` e `docs/DEPLOYMENT-ARCHITECTURE.md`

**Acções:**
1. Criar migração com `CREATE EXTENSION IF NOT EXISTS vector` e adicionar coluna `Embedding vector(768)` à tabela `ai_knowledge_sources`
2. Criar índice HNSW: `CREATE INDEX ON ai_knowledge_sources USING hnsw (embedding vector_cosine_ops)`
3. Atualizar `EmbeddingIndexJob` para persistir o vector real na nova coluna (em vez de só o JSON)
4. Em `DocumentRetrievalService.SearchAsync`, usar query `<=>` (cosine distance) via EF Core quando `pgvector` disponível, com fallback para string search quando a coluna for nula
5. Remover ou manter como fallback a lógica de cosine em C# — documentar a limitação
6. Documentar o requisito de `pgvector` em `LOCAL-SETUP.md` (`CREATE EXTENSION IF NOT EXISTS vector`) e `DEPLOYMENT-ARCHITECTURE.md`
7. Adicionar testes de integração para indexação e busca semântica

**Critérios de aceite:**
- Perguntas semanticamente equivalentes a documentos existentes retornam resultados relevantes mesmo sem correspondência textual exacta
- Performance de retrieval em <100ms para 10k documentos
- Fallback transparente para string search quando embedding não disponível
- Job de indexação é idempotente

---

#### E-A02 — Provider fallback automático via health checks

**Gap:** Se o provider principal falhar (ex: Ollama não acessível), o sistema cai para o `system-fallback` determinístico. Não existe lógica de failover para o próximo provider disponível. `IAiProviderHealthService` existe mas não é integrado no fluxo de routing de `SendAssistantMessage` nem em `AiAgentRuntimeService`.

**Ficheiros afectados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Services/AiRoutingResolver.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Services/AiProviderFactory.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Services/IAiRoutingResolver.cs`

**Acções:**
1. Injectar `IAiProviderHealthService` em `AiRoutingResolver`
2. No método `ResolveRoutingAsync`, verificar saúde do provider preferido com `CheckProviderAsync`
3. Se provider unhealthy, iterar pela lista de providers ordenada por prioridade até encontrar um saudável
4. Registar o failover em log estruturado: `"Provider {primary} unhealthy, routing to fallback {fallback}"`
5. Expor `UsedFallbackProvider: bool` e `FallbackReason: string?` na `RoutingResolutionResult`
6. Reflectir `UsedFallbackProvider` na resposta do frontend como aviso contextual
7. Adicionar testes: provider principal saudável, provider principal unhealthy (fallback activado), todos os providers unhealthy (degraded mode)

**Critérios de aceite:**
- Falha do Ollama local não derruba o assistente — o sistema muda automaticamente para o próximo provider configurado
- Utilizador recebe aviso visual quando resposta veio de fallback
- Health checks não bloqueiam o pipeline — timeout máximo de 500ms

---

#### E-A03 — AiRoutingResolver sem modelo vazio como fallback

**Gap:**
```csharp
// AiRoutingResolver.cs
private static (string Model, string Provider, bool IsInternal) SelectModel(...)
{
    if (routingPath == AIRoutingPath.ExternalEscalation)
        return (string.Empty, string.Empty, false);
    return (string.Empty, string.Empty, true);  // modelo vazio
}
```
Se `IAiModelCatalogService` não conseguir resolver o modelo (sem modelos registados, base de dados indisponível), o provider recebe `ModelId = ""` e a inferência falha com erro pouco descritivo para o utilizador.

**Ficheiros afectados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Services/AiRoutingResolver.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Services/AiModelCatalogService.cs`

**Acções:**
1. Definir um modelo de sistema hard-coded como fallback de último recurso (ex: `"ollama:llama3.2"`) em `AiRoutingResolver` como constante
2. Quando `resolvedModel` for null E `SelectModel` retornar string vazia, usar o fallback de sistema e logar aviso
3. Adicionar `ModelResolutionFailed: bool` e `UsedSystemFallbackModel: bool` ao log estruturado
4. Retornar `Error.Business("AIKnowledge.NoModelAvailable")` se nenhum provider conseguir inferir (em vez de string vazia silenciosa)
5. Adicionar testes: catálogo vazio, catálogo com modelo default, override de modelo válido, override de modelo inválido

**Critérios de aceite:**
- Nunca é enviado `ModelId = ""` para um provider
- Erro `NoModelAvailable` é claro e auditável
- Sistema fallback é documentado e configurável

---

#### E-A04 — AIExecutionPlan com FK para AiAgentExecution

**Gap:** A entidade `AIExecutionPlan` não tem FK para `AiAgentExecution`. O plano é gerado pela feature `PlanExecution` mas não é persistido com referência à execução que o usou. Não é possível auditar "qual plano foi usado na execução X" nem navegar do detalhe de uma execução para o plano que a gerou.

**Ficheiros afectados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Domain/Governance/Entities/AIExecutionPlan.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Domain/Governance/Entities/AiAgentExecution.cs`
- Migração EF Core para o novo campo
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Services/AiAgentRuntimeService.cs`

**Acções:**
1. Adicionar propriedade `AiAgentExecutionId? ExecutionId` à entidade `AIExecutionPlan`
2. Adicionar migração EF Core com a nova FK nullable
3. Em `AiAgentRuntimeService`, quando o plano é gerado, persistir com `ExecutionId` preenchido após criar o registo de execução
4. Expor `PlanId` como campo opcional na resposta de `ExecuteAgent` para rastreabilidade
5. Na `AgentDetailPage` (frontend), mostrar link para o plano quando disponível
6. Adicionar testes: execução com planning activo, execução sem planning, navegação plano→execução

**Critérios de aceite:**
- Cada `AIExecutionPlan` tem referência opcional à `AiAgentExecution` que o usou
- Auditores conseguem navegar do detalhe de execução para o plano gerado
- FK é nullable — execuções sem planning continuam a funcionar

---

### Fase 3 — Correctitude e qualidade de domínio

> Objetivo: eliminar ambiguidades no domínio e melhorar a experiência de configuração e uso.  
> Duração estimada: 2–3 sprints

---

#### E-M01 — Guardrails com enums fortemente tipados

**Gap:** As propriedades `Category`, `GuardType`, `PatternType`, `Severity`, `Action` da entidade `AiGuardrail` são strings livres. Um guardrail criado com `Action = "blokk"` (typo) ou `Severity = "HIGH"` (case errado) nunca é activado correctamente. O `AiGuardrailEnforcementService` faz comparações de string sem normalização.

**Ficheiros afectados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Domain/Governance/Entities/AiGuardrail.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Domain/Governance/Enums/` — novos enums
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Services/AiGuardrailEnforcementService.cs`
- Migração EF Core para conversão de colunas
- Endpoints e DTOs de criação/atualização de guardrails

**Acções:**
1. Criar enums: `GuardrailCategory`, `GuardrailType`, `GuardrailPatternType`, `GuardrailSeverity`, `GuardrailAction`
2. Migrar campos da entidade `AiGuardrail` de `string` para os novos enums
3. Criar migração EF Core com conversão de dados existentes (`"block" → GuardrailAction.Block`, etc.)
4. Atualizar `AiGuardrailEnforcementService` para usar comparações de enum em vez de strings
5. Atualizar endpoints de criação/atualização para validar valores com `EnumOutOfRange`
6. Atualizar `AiGuardrailConfiguration.cs` para mapear os novos tipos com `HasConversion`
7. Adicionar testes para cada enum e para o enforcement com guardrails de cada tipo

**Critérios de aceite:**
- Guardrails com valores incorrectos são rejeitados na criação com mensagem de validação clara
- Enforcement usa comparação de enum — sem risco de typo silencioso
- Migração converte dados existentes sem perda de informação

---

#### E-M02 — Feedback loop com routing adjustment efectivo

**Gap:** O `FeedbackThresholdJob` existe e publica o evento `ModelFeedbackThresholdExceeded`. Porém, não há implementação do consumidor que ajusta `AIRoutingStrategy.Priority` quando um modelo acumula feedback negativo. O loop de melhoria é apenas um alerta, não uma acção correctiva real.

**Ficheiros afectados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Features/` — consumer do evento
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Domain/Governance/Entities/AIRoutingStrategy.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Governance/Jobs/FeedbackThresholdJob.cs`

**Acções:**
1. Criar handler `ModelFeedbackThresholdExceededHandler` que consome o evento interno
2. O handler consulta `AIRoutingStrategy` onde `PreferredPath` inclui o modelo problemático
3. Reduz a `Priority` da strategy em 1 (empurrando-a para baixo na ordem de selecção)
4. Cria um registo de auditoria: `"Routing strategy {name} priority reduced from {old} to {new} due to negative feedback threshold"`
5. Notifica o Platform Admin via módulo de Notifications
6. Adicionar campo `AutoAdjustedAt` e `AutoAdjustmentReason` à entidade `AIRoutingStrategy` para rastreabilidade
7. Adicionar testes: threshold atingido (reduz prioridade), threshold não atingido (sem alteração), priority já em mínimo (sem alteração)

**Critérios de aceite:**
- Modelo com feedback negativo acumulado é automaticamente desprioritizado no routing
- Ajuste é auditável e reversível manualmente pelo Platform Admin
- Platform Admin é notificado com contexto (modelo, threshold, período, contagem de negativos)

---

#### E-M03 — Embedding cache com LRU correcto

**Gap:**
```csharp
// InMemoryEmbeddingCacheService.cs
var keyToRemove = _cache.Keys.FirstOrDefault();  // não-determinístico + race condition
```
A evicção do cache de embeddings não é atómica e não garante FIFO real. Em cargas altas, dois threads podem simultaneamente tentar evictar sem coordenação, podendo causar memory pressure acima do limite configurado.

**Ficheiros afectados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Governance/Services/InMemoryEmbeddingCacheService.cs`

**Acções:**
1. Substituir a implementação actual por um LRU cache baseado em `IMemoryCache` com `SlidingExpiration` de 30 minutos e `SizeLimit` configurável
2. Alternativamente, implementar LRU com `LinkedList<string> + ConcurrentDictionary` com lock para evicção atómica
3. Opção preferencial: usar `Microsoft.Extensions.Caching.Memory.MemoryCache` com `EntryOptions.Size = 1` e `SizeLimit = 500`
4. Adicionar métricas de cache: `HitCount`, `MissCount`, `EvictionCount` como contadores de diagnóstico
5. Adicionar testes: cache hit, cache miss, evicção correcta quando limite atingido, thread safety

**Critérios de aceite:**
- Evicção correcta e thread-safe quando `MaxCacheEntries` é atingido
- `HitCount` e `MissCount` disponíveis para observabilidade
- Comportamento consistente sob carga concurrent (sem exceções ou memory leaks)

---

#### E-M04 — Contrato estável de erros de IA no frontend

**Gap:** Os erros de IA (`GuardrailViolation`, `QuotaExceeded`, `AgentAccessDenied`, `NoModelAvailable`) são retornados como `Result<T>` com `Error.Business(...)`. O frontend precisa de mapear `messageKey` para mensagens i18n contextuais específicas do módulo de IA, mas essa padronização não está documentada nem aplicada de forma uniforme.

**Ficheiros afectados:**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Domain/Governance/Errors/AiGovernanceErrors.cs`
- `src/frontend/src/features/ai-hub/api/aiGovernance.ts`
- `src/frontend/src/locales/en.json`, `pt-BR.json`, `pt-PT.json`

**Acções:**
1. Auditar todos os `Error.Business(...)` em `AiGovernanceErrors.cs` e garantir que cada código tem `messageKey` correspondente
2. No frontend, criar função `mapAiError(error: ApiError): { key: string; params?: Record<string, string> }` que mapeia os códigos conhecidos para chaves i18n
3. Adicionar i18n keys para todos os erros de IA: `aiHub.errors.quotaExceeded`, `aiHub.errors.guardrailViolation`, `aiHub.errors.agentAccessDenied`, `aiHub.errors.noModelAvailable`, `aiHub.errors.providerUnavailable`
4. Usar `mapAiError` nos componentes `AiAssistantPage`, `AiAgentsPage` e outros que consomem a API de IA
5. Adicionar testes para a função `mapAiError` com todos os códigos conhecidos

**Critérios de aceite:**
- Todos os erros de IA mostram mensagem i18n contextual (não o código técnico)
- Novos erros desconhecidos mostram mensagem genérica em vez de código bruto
- Mensagens incluem contexto quando disponível (ex: `"Quota mensal excedida (política: {policyName})"`)

---

#### E-M05 — Explicabilidade activa das respostas

**Gap:** As respostas de IA incluem metadata passiva de grounding (fontes usadas, modelo, routing rationale). Mas não há mecanismo para o utilizador perguntar "porquê esta resposta?" ou "em que fontes te baseaste?" de forma interactiva. A explicabilidade é unidireccional — metadata na resposta — e não activa (drill-down por fonte, citar origem, explicar raciocínio).

**Ficheiros afectados:**
- `src/frontend/src/features/ai-hub/pages/AiAssistantPage.tsx`
- `src/frontend/src/features/ai-hub/pages/ChatMessageItem.tsx`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Features/SendAssistantMessage/SendAssistantMessage.cs`

**Acções:**
1. No frontend, adicionar botão "Ver fontes" em cada mensagem de IA (expandable panel)
2. O panel mostra: fontes de grounding (nome + tipo + relevância), modelo usado, routing rationale, se houve truncagem de contexto
3. Adicionar acção "Explicar esta resposta" que envia nova mensagem ao assistente com prompt `"Explica o teu raciocínio para a resposta anterior, indicando quais fontes usaste e porquê"`
4. No backend, incluir `ExplainabilityHints` na resposta: lista de fontes consultadas com snippet e score de relevância
5. Adicionar i18n keys: `aiHub.assistant.viewSources`, `aiHub.assistant.explainResponse`, `aiHub.assistant.groundingSources`, `aiHub.assistant.routingRationale`
6. Adicionar testes para o componente expandable de fontes

**Critérios de aceite:**
- Utilizador pode ver as fontes usadas para cada resposta sem sair da conversa
- Botão "Explicar" disponível e funcional para todas as respostas com grounding
- Sources panel respeita persona — Engineer vê detalhe técnico, Executive vê resumo

---

### Fase 4 — Novas capacidades estratégicas

> Objetivo: implementar capacidades que diferenciam o NexTraceOne no mercado.  
> Duração estimada: 4–6 sprints

---

#### E-N01 — AI Diff para Contratos

**Contexto de produto:** O NexTraceOne tem Contract Governance como pilar central. Actualmente, o diff semântico de contratos é feito de forma textual. A IA pode analisar dois snapshots de um contrato REST/SOAP/AsyncAPI e produzir análise de impacto com rigor de produto.

**Pilar reforçado:** Contract Governance, Change Intelligence

**Ficheiros afectados (novos):**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Orchestration/Features/AnalyzeContractBreakingChanges/`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Domain/Orchestration/Entities/ContractBreakingChangeAnalysis.cs`
- `src/frontend/src/features/ai-hub/pages/AiContractGeneratorPage.tsx` (adicionar tab de diff)

**Implementação:**
1. Criar feature `AnalyzeContractBreakingChanges` no bounded context Orchestration
2. Input: `ContractBefore` (JSON/YAML), `ContractAfter` (JSON/YAML), `ServiceName`, `KnownConsumers: List<string>`
3. Output: `ContractBreakingChangeAnalysis` com:
   - Lista de `BreakingChange` (tipo, campo, severidade, descrição)
   - Lista de `NonBreakingChange`
   - `SuggestedVersion` (patch/minor/major conforme SemVer)
   - `BlastRadius` por consumidor afectado
   - `CommunicationDraft` (rascunho de comunicação para equipas)
4. Grounding: `IContractGroundingReader` (E-C03) + lista de consumidores via `ICatalogGroundingReader`
5. Agent especializado `ContractDiffAgent` com `AgentCategory.ApiDesign`
6. Endpoint: `POST /api/v1/aiorchestration/contracts/analyze-diff`
7. No frontend, adicionar tab "AI Diff" na página de contrato (no módulo Contracts, não no AI Hub)

**Critérios de aceite:**
- Breaking changes são classificados correctamente para contratos REST/AsyncAPI
- `SuggestedVersion` segue SemVer (major para breaking, minor para adições, patch para fixes)
- `CommunicationDraft` é personalizável antes de envio

---

#### E-N02 — Incident War Room Agent

**Contexto de produto:** Durante um incidente activo, o engineer precisa de velocidade e contexto concentrado. O agente de war room reduz o Mean Time to Resolution ao consolidar automaticamente as informações críticas.

**Pilar reforçado:** Operational Reliability, AIOps

**Ficheiros afectados (novos):**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Tools/GetActiveIncidentTimelineTool.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Runtime/Tools/SuggestRollbackTool.cs`
- `src/frontend/src/features/operations/pages/IncidentWarRoomPage.tsx`

**Implementação:**
1. Criar agent especializado `IncidentWarRoomAgent` com `AgentCategory.IncidentResponse`
2. Ferramentas dedicadas: `GetActiveIncidentTimeline`, `GetRelatedChanges` (existe), `SuggestRollback`, `GetRunbook` (existe)
3. Tool `GetActiveIncidentTimeline`: retorna timeline de eventos do incidente (detecção, escalações, updates, mitigações)
4. Tool `SuggestRollback`: analisa mudanças nas últimas N horas para o serviço e sugere qual reverter
5. Contexto persistente durante o incidente: `AiAssistantConversation` ligada ao `IncidentId`
6. Página `IncidentWarRoomPage` no módulo Operations com:
   - Chat assistant com contexto do incidente pré-carregado
   - Linha do tempo automática do incidente
   - Sugestões de acção (rollback, escalonamento, comunicação)
   - Link directo para runbooks relevantes
7. Integração com o módulo Incidents para injectar updates no contexto durante a sessão

**Critérios de aceite:**
- Ao abrir o War Room para um incidente, o contexto já está pré-carregado (serviço, ambiente, mudanças recentes, runbooks)
- Sugestão de rollback indica exactamente qual release reverter e porquê
- Conversa do War Room fica ligada ao post-mortem do incidente

---

#### E-N03 — AI Release Gate Advisor

**Contexto de produto:** Antes de promover uma mudança para produção, o NexTraceOne tem gates de promoção. A IA pode actuar como co-aprovador inteligente, combinando múltiplas fontes de evidência num veredicto explicável.

**Pilar reforçado:** Change Intelligence, Production Change Confidence

**Ficheiros afectados (novos):**
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Orchestration/Features/AiReleaseGateAdvisor/`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Domain/Orchestration/Entities/ReleaseGateVerdict.cs`

**Implementação:**
1. Criar feature `AiReleaseGateAdvisor` no Orchestration
2. Input: `ReleaseId`, `TargetEnvironment`, `RequestedBy`
3. Pipeline de análise:
   - Blast radius calculado (existente)
   - Mudanças recentes na janela de deploy para o mesmo serviço
   - Incidentes abertos ou recentes para o serviço em ambientes anteriores
   - Tendência de feedback de qualidade pós-deploy em ambientes precedentes
   - Cobertura de testes disponível (se ligado ao pipeline)
4. Output: `ReleaseGateVerdict` (`Approve` / `Conditional` / `Block`) com:
   - `Justification` estruturada por factor
   - `Confidence` score (0.0–1.0)
   - Lista de `Conditions` para aprovação condicional
   - `RiskLevel` (Low/Medium/High/Critical)
5. Integrar no `PromotionGovernance` module como gate opcional (flag `UseAiAdvisor` na política de promoção)
6. Histórico de veredictos persistido em `ReleaseGateVerdict` para auditoria e aprendizagem
7. Endpoint: `POST /api/v1/aiorchestration/releases/{id}/gate-assessment`

**Critérios de aceite:**
- Veredicto `Block` inclui justificativa específica (não genérica)
- Gate é configurável como advisory-only ou blocking por política
- Histórico de veredictos é visível no detalhe da release e auditável

---

#### E-N04 — Semantic Catalog Search

**Contexto de produto:** Perguntas como "quais serviços de pagamento têm dependência de terceiros e estão em deprecated?" exigem hoje múltiplos filtros manuais. A busca semântica permite consultas em linguagem natural directamente no catálogo.

**Pilar reforçado:** Service Governance, Source of Truth

**Implementação:**
1. Criar feature `SemanticCatalogSearch` no Orchestration
2. Input: `NaturalLanguageQuery`, `MaxResults`, `Persona`
3. O handler usa IA para:
   - Extrair entidades da query (domínio, criticidade, lifecycle, tipo de serviço)
   - Traduzir para filtros estruturados do Catalog
   - Rankear resultados por relevância semântica
4. Response: lista de serviços com `ExplainedRelevance` por cada resultado
5. Integrar no command palette (⌘K) com modo "AI Search" (prefixo `?`)
6. Persona-aware: Engineer vê detalhe técnico, Executive vê resumo de negócio
7. Endpoint: `POST /api/v1/aiorchestration/catalog/semantic-search`

**Critérios de aceite:**
- Query "serviços críticos de pagamento sem owner" retorna resultados correctos
- Relevância é explicada por resultado
- Disponível no command palette sem sair da página actual

---

#### E-N05 — AI Budget Alert Proactivo

**Contexto de produto:** Os alertas de budget são reactivos (quando a quota é atingida). Um alerta proactivo baseado em projecção reduz o risco de interrupção inesperada do serviço de IA.

**Pilar reforçado:** FinOps contextual, AI Governance

**Implementação:**
1. Criar `AiBudgetForecastJob` (Quartz.NET, execução diária às 08:00 UTC)
2. Calcula consumo médio diário dos últimos 7 dias por tenant/utilizador/modelo
3. Projecta consumo até fim do período de budget
4. Se projecção ≥ 80% do orçamento antes do fim do período, emite evento `BudgetForecastAlertTriggered`
5. Evento aciona notificação para Platform Admin com: tenant, modelo, consumo actual, projecção, dias restantes no período
6. No `TokenBudgetPage`, adicionar:
   - Indicador "Projecção de consumo" com linha de tendência (ECharts)
   - Badge `"Em risco"` para orçamentos com projecção acima de 80%
7. Threshold de alerta configurável por orçamento (parametrizado no banco)

**Critérios de aceite:**
- Platform Admin recebe alerta antes de o orçamento ser esgotado
- Threshold é configurável (não hardcoded)
- Dashboard mostra projecção visualmente clara

---

#### E-N06 — Prompt Template Marketplace Governado

**Contexto de produto:** Os prompt templates são isolados por tenant. Um marketplace permite partilhar boas práticas de prompting entre organizações e acelerar a adopção.

**Pilar reforçado:** AI Governance, Developer Acceleration

**Implementação:**
1. Adicionar campo `IsPublic: bool` à entidade `PromptTemplate` (default: false)
2. Criar feature `GetPublicPromptTemplates` que retorna templates públicos de todos os tenants
3. Criar feature `InstallPromptTemplate` que copia um template público para o catálogo do tenant
4. Criar página `PromptTemplateMarketplacePage` no AI Hub:
   - Grid de templates com categoria, uso count e rating
   - Filtros por categoria e persona alvo
   - Preview do template antes de instalar
   - Botão "Instalar" que copia para o catálogo do tenant
5. Agents do sistema (`AgentOwnershipType.System`) têm templates sempre visíveis
6. Auditar instalações: registo em `AIUsageEntry` com acção `TemplateInstalled`

**Critérios de aceite:**
- Platform Admin pode marcar templates como públicos
- Instalação de template é auditada
- Templates instalados são independentes do original (cópias não vinculadas)

---

## Dependências entre itens

```
E-C03 (Contract Grounding Reader) ──────────> E-N01 (AI Diff para Contratos)
E-C02 (Persona no JWT) ──────────────────────> E-M04 (contrato de erros), E-M05 (explicabilidade)
E-A01 (pgvector real) ───────────────────────> E-N02 (Incident War Room Agent)
E-A02 (provider fallback) ───────────────────> E-N03 (AI Release Gate Advisor)
E-M01 (guardrails com enums) ────────────────> E-N01, E-N02, E-N03 (guardrails nas novas capacidades)
E-M02 (feedback loop activo) ────────────────> E-A02 (routing adjustment pós-fallback)
E-N01 (AI Diff para Contratos) ──────────────> E-N03 (AI Release Gate — analisa diff de contratos)
E-N02 (Incident War Room) ───────────────────> E-A04 (plano ligado à execução do agent)
```

---

## Resumo de prioridades por fase

```
FASE 1 — Estabilização crítica (sprints 1-2)
  E-C01  Quota pre-check em AiAgentRuntimeService        🔴 Crítico
  E-C02  Persona explícita no JWT                        🔴 Crítico
  E-C03  Contract Grounding Reader                       🔴 Crítico

FASE 2 — Fiabilidade e escalabilidade (sprints 3-5)
  E-A01  pgvector real (coluna vector + índice HNSW)     🟠 Alto
  E-A02  Provider fallback automático via health checks  🟠 Alto
  E-A03  AiRoutingResolver sem modelo vazio              🟠 Alto
  E-A04  AIExecutionPlan FK para AiAgentExecution        🟠 Alto

FASE 3 — Correctitude e qualidade (sprints 6-8)
  E-M01  Guardrails com enums fortemente tipados         🟡 Médio
  E-M02  Feedback loop com routing adjustment efectivo   🟡 Médio
  E-M03  Embedding cache com LRU correcto                🟡 Médio
  E-M04  Contrato estável de erros de IA no frontend     🟡 Médio
  E-M05  Explicabilidade activa das respostas            🟡 Médio

FASE 4 — Novas capacidades estratégicas (sprints 9-14)
  E-N01  AI Diff para Contratos                          ⭐⭐⭐ Alto
  E-N02  Incident War Room Agent                         ⭐⭐⭐ Alto
  E-N03  AI Release Gate Advisor                         ⭐⭐⭐ Alto
  E-N04  Semantic Catalog Search                         ⭐⭐ Médio
  E-N05  AI Budget Alert Proactivo                       ⭐⭐ Médio
  E-N06  Prompt Template Marketplace                     ⭐ Baixo

BACKLOG (sem sprint definida)
  B-01   DefaultModelCatalog configurável externamente   🔵 Baixo
  B-02   IDE extension end-to-end com capability policies 🔵 Baixo
```

---

## Critérios de qualidade transversais

Todos os itens deste plano devem cumprir os seguintes critérios, independentemente da fase:

1. **i18n obrigatório** — todo texto visível via chaves de tradução (en, pt-BR, pt-PT, es)
2. **CancellationToken** em toda operação async
3. **Testes mínimos** — sucesso, validação falha, dependência indisponível
4. **Logging estruturado** — operações relevantes com nível e campos corretos
5. **Auditoria** — acções sensíveis registadas em `AIUsageEntry` ou log de auditoria
6. **Persona awareness** — endpoints e UX reflectem o utilizador actual
7. **Segurança** — sem exposição de segredos, sem bypass de autorização no frontend
8. **Tenant isolation** — dados e políticas nunca escapam do tenant do utilizador
9. **Sealed classes** para entidades de domínio novas
10. **Result\<T\>** para falhas controladas — sem lançamento de excepções em fluxos de negócio
11. **strongly typed IDs** para todas as novas entidades

---

## Métricas de sucesso do módulo

| Métrica | Estado actual | Objectivo pós-fase 4 |
|---------|--------------|---------------------|
| Cobertura de testes AIKnowledge | ~1124 testes | >1400 testes |
| Use cases com grounding real | 4/6 tipos | 6/6 tipos (+ contracts) |
| Providers de IA suportados | 3 (Ollama, OpenAI, Anthropic) | 3 + fallback automático |
| Capacidades de agente tool | 9 tools | 9 + 2 (WarRoom) |
| Novas capacidades estratégicas | 0 | 3 (Diff, WarRoom, Gate) |
| LGPD/GDPR compliance | DataRetentionJob activo | + policy por budget |
| RAG semântico | Em memória (pseudo-semântico) | pgvector (ANN real) |

---

## Relação com o plano anterior (AI-MODULE-ACTION-PLAN.md)

Este plano é **complementar** ao anterior, não substitutivo. Os P1–P18 do plano anterior estão todos concluídos. Este plano aborda:

1. **Gaps que emergiram após a conclusão do plano anterior** (ex: E-C01 quota em agent runtime, E-A03 modelo vazio)
2. **Gaps de segunda ordem** que só se tornam visíveis com o sistema em operação (ex: E-M03 embedding cache, E-M01 guardrails com strings)
3. **Novas capacidades que só fazem sentido após ter a base estável** (ex: E-N01 AI Diff, E-N02 War Room, E-N03 Release Gate)
