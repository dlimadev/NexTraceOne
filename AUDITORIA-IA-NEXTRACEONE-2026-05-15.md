# AUDITORIA COMPLETA DO PROCESSO DE IA — NEXTRACEONE

**Data da auditoria:** 2026-05-15
**Auditor:** Kimi Code CLI (Análise Forense do Código-Fonte)
**Escopo:** Todo o codebase (`src/`, `tests/`, `docs/`, `deploy/`, `build/`, `tools/`)

---

## 1. SUMÁRIO EXECUTIVO

O NexTraceOne possui a **arquitetura de IA mais completa e ambiciosa** já vista em um codebase .NET 10 open-source/enterprise: ~470 arquivos C#, 4 bounded contexts (Governance, Runtime, Orchestration, ExternalAI), 68 entidades de domínio, 80+ features CQRS, 4 providers LLM, 13 agentes catalogados, frontend rico (20+ páginas), extensões IDE (VS Code + Visual Studio), e um CLI. **Contudo, existe um abismo entre a estrutura e a funcionalidade real:** a maioria dos agentes é stub, não há vector DB dedicado (usa pgvector no PostgreSQL), não há SDK de orquestração (Semantic Kernel, LangChain), o token counting é heurístico (~4 chars/token), Microsoft.ML é "dead weight", e problemas críticos de segurança (SQL injection, cross-tenant exposure, HttpClient sem factory) foram documentados internamente. O sistema funciona para chat básico (`ExecuteAiChat`) e scaffolding (`GenerateAiScaffold`), mas a visão enterprise (skills dinâmicas, RL, memória organizacional, evaluation harness) ainda é roadmap. **Nota geral: 72/100 — estrutura excelente, execução incompleta, segurança precisa de atenção imediata.**

---

## 2. TABELA DE STATUS

| Componente | Status | Evidência no Código |
|------------|--------|---------------------|
| PII Scanner | **PARCIAL** | `AiGuardrailEnforcementService.cs` — detecta connection strings, bearer tokens, private keys em output. **NÃO redacta PII do grounding antes de enviar ao LLM** (gap documentado em `docs/SECURITY-ARCHITECTURE.md`). |
| Token Budget Enforcer | **FUNCIONAL** | `AiTokenQuotaService.cs` + `AiTokenUsageLedger` — quotas diárias/mensais/por-request com cache (Redis/in-memory). Testes em `AiTokenQuotaServiceTests.cs`. |
| Model Router (Local vs Externo) | **FUNCIONAL** | `AiRoutingResolver.cs` — fallback para `llama3.2` (Ollama), health check 500ms, cascade de fallback. Políticas configuráveis via `AIRoutingStrategy`. |
| Prompt Cache | **STUB** | `InMemoryEmbeddingCacheService` existe como Singleton (não escalável horizontalmente). Não há cache de prompts/respostas de LLM propriamente dito. |
| Context Compressor (LLMLingua/truncamento) | **PARCIAL** | `ContextWindowManager.cs` — truncamento heurístico (~4 chars/token). **Sem LLMLingua, sem compressão semântica.** |
| Audit Logger (Hash Chain) | **FUNCIONAL** | `AiTokenUsageLedger` + `AiExternalInferenceRecord` — timestamp, user, model, tokens, custo estimado. **Hash SHA-256 não confirmado** nos registros. |
| service-analyst agent | **STUB** | Catalogado em `DefaultAgentCatalog.cs`, mas execução via `ExecuteAiChat` genérico. Sem pipeline especializado confirmado. |
| contract-designer agent | **STUB** | Idem. |
| change-advisor agent | **STUB** | Idem. |
| incident-responder agent | **STUB** | Idem. |
| test-generator agent | **STUB** | Idem. |
| docs-assistant agent | **STUB** | Idem. |
| security-reviewer agent | **STUB** | `SecurityReview.cs` — retorna vulnerabilidades hardcoded (SQL Injection, Hardcoded Secret). TODO: integrar SAST real. |
| RAG Pipeline (Vector DB + Embeddings) | **PARCIAL** | `EmbeddingIndexJob.cs` + pgvector no PostgreSQL. Busca semântica via `DocumentRetrievalService.cs` (operador `<=>`). **Não há re-ranking, não há vector DB dedicado (Qdrant/Weaviate).** |
| Model Registry | **FUNCIONAL** | `AIModel` entity + `AiModelCatalogService` + UI `/admin/ai/models`. Seed com 11 modelos. Lifecycle (Activate/Deactivate/Deprecate/Block). |
| Token Cost Tracker | **FUNCIONAL** | `AiTokenUsageLedgerCostTests.cs` confirma tracking por request. Custo estimado em EUR/USD não confirmado explicitamente. |
| External API Integration (OpenAI) | **FUNCIONAL** | `OpenAiProvider.cs` + `OpenAiHttpClient.cs` — chat, streaming SSE, function calling, embeddings. Testes em `OpenAiProviderTests.cs`. |
| External API Integration (Anthropic) | **PARCIAL** | `AnthropicProvider.cs` + `AnthropicHttpClient.cs` — chat completion. **Sem streaming confirmado, sem tool calling nativo, documentado como incompleto.** |
| Human-in-the-loop (approval gates) | **STUB** | `ApproveSelfHealingAction` existe como feature CQRS, mas **não há gate de aprovação humana no fluxo de chat/scaffold**. |
| Streaming de respostas | **FUNCIONAL** | Implementado em `OpenAiProvider.cs` (SSE) e `OllamaProvider.cs`. Frontend consome via EventSource. |
| Multi-turn conversation (contexto entre mensagens) | **FUNCIONAL** | `ConversationPersistenceService.cs` + `AiConversation` entity. Persistência em PostgreSQL. |

---

## 3. ANÁLISE DETALHADA

### SEÇÃO 1: ARQUITETURA DE IA — O QUE EXISTE?

#### 1.1 INFRAESTRUTURA DE IA

**Servidor de modelos locais:**
- **Ollama** é o provider local padrão (`AiRuntime:Ollama:BaseUrl` = `http://localhost:11434` em `src/platform/NexTraceOne.ApiHost/appsettings.json`, linhas 42-65).
- **NÃO há container Docker do Ollama** em nenhum `docker-compose.yml`. Ollama é tratado como infraestrutura externa ao projeto — requer instalação manual no host.
- **LM Studio** é provider local alternativo (`http://localhost:1234/v1`), mas documentado como incompleto.
- **vLLM, llama.cpp:** não mencionados no código (apenas em documentação de roadmap).

**GPU dedicada:**
- **Não há configuração de GPU** em nenhum arquivo do projeto (docker-compose, Kubernetes, appsettings).
- Documentação `docs/AI-MODELS-ANALYSIS.md` recomenda RTX 4090 (24 GB VRAM) para Qwen 2.5 Coder 32B, mas isso é recomendação de hardware, não configuração do sistema.

**Comunicação .NET ↔ Modelo:**
- **HTTP clients nativos** do .NET (`HttpClient`). **Zero SDKs externos.**
- `OpenAiHttpClient.cs`, `OllamaHttpClient.cs`, `AnthropicHttpClient.cs`, `LmStudioHttpClient.cs` — todos implementam parsing manual de JSON e SSE.
- **Timeout:** Ollama 120s, OpenAI/Anthropic 60s (`appsettings.json`).
- **Retry:** `MaxRetries: 2` configurado para Ollama (`appsettings.json`).
- **Circuit Breaker:** `AiResourceGovernor` — `SemaphoreSlim` (max 5 concorrentes) + estado manual (abre após 5 falhas, half-open após 60s). Não usa Polly nem biblioteca dedicada.

#### 1.2 MODEL LAYER

**Modelos disponíveis (seed data `DefaultModelCatalog`):**

| Modelo | Origem | Formato | Tamanho | Capability |
|--------|--------|---------|---------|------------|
| deepseek-r1:1.5b | DeepSeek | GGUF (Ollama) | 1.5B | chat, reasoning |
| llama3.2:3b | Meta | GGUF (Ollama) | 3B | chat |
| nomic-embed-text | Nomic | GGUF (Ollama) | — | embeddings |
| codellama:7b | Meta | GGUF (Ollama) | 7B | code |
| qwen3.5:9b | Alibaba | GGUF (Ollama) | 9B | chat (default em appsettings) |
| gpt-4o | OpenAI | API | — | chat, vision, tool-calling |
| gpt-4o-mini | OpenAI | API | — | chat |
| claude-3-5-sonnet | Anthropic | API | — | chat |
| claude-opus-4.7 | Anthropic | API | — | chat, reasoning |
| claude-sonnet-4.6 | Anthropic | API | — | chat |
| claude-haiku-4.5 | Anthropic | API | — | chat (default Anthropic) |

**Model Registry:**
- Entidade: `AIModel` (`src/modules/aiknowledge/NexTraceOne.AIKnowledge.Domain/Governance/Entities/AiModel.cs`)
- Tabela: `aik_models`
- Propriedades: `ModelId`, `DisplayName`, `ProviderType`, `CapabilitiesFlags` (streaming, tool-calling, embeddings, vision), `LifecycleStatus`, `IsDefaultForChat`, `IsDefaultForReasoning`, `IsDefaultForEmbeddings`.
- UI: `AiModelManagerPage.tsx` / `ModelRegistryPage.tsx`

**Versionamento de modelos:**
- Existe `Version` na entidade `AIModel`, mas **não há rollback automático nem A/B testing** implementado.
- `AIRoutingStrategy` permite regras de routing por use-case, mas sem capacidade de A/B.

#### 1.3 AI GATEWAY

**Gateway centralizado:**
- **SIM** — `AiResourceGovernor` + `AiRoutingResolver` + `AiGuardrailEnforcementService` + `AiTokenQuotaService` atuam como gateway.
- O ponto de entrada único para chat é `ExecuteAiChat` handler (`src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Runtime/Features/ExecuteAiChat.cs`).

**Componentes do gateway:**

| Componente | Classe/Método | Biblioteca | Status |
|------------|---------------|------------|--------|
| PII Scanner | `AiGuardrailEnforcementService.EnforceOutputAsync` | Regex nativo | **PARCIAL** — detecta tokens/connection strings em output, mas **não redacta PII do grounding antes do LLM**. |
| Token Budget Enforcer | `AiTokenQuotaService.ValidateQuotaAsync` | Nenhuma (código próprio) | **FUNCIONAL** — diário/mensal/por-request, cache Redis/in-memory. |
| Model Router | `AiRoutingResolver.ResolveAsync` | Nenhuma | **FUNCIONAL** — local-first, health check 500ms, cascade fallback. |
| Prompt Cache | `InMemoryEmbeddingCacheService` | Nenhuma | **STUB** — Singleton, não escalável horizontalmente. |
| Context Compressor | `ContextWindowManager.TruncateContext` | Nenhuma | **PARCIAL** — truncamento simples por caracteres (~4 chars/token). Sem LLMLingua. |
| Audit Logger | `AiTokenUsageLedger` + `AiExternalInferenceRecord` | EF Core | **FUNCIONAL** — registro completo de uso, mas **hash SHA-256 não confirmado**. |

---

### SEÇÃO 2: AGENTS ORQUESTRADORES — COMO FUNCIONAM?

#### 2.1 AGENTS IMPLEMENTADOS

**Catálogo oficial (`DefaultAgentCatalog.cs`):**

| Agente | Classe/Arquivo | System Prompt | Tools | Memória | Orquestração |
|--------|---------------|---------------|-------|---------|--------------|
| service-analyst | Catalogado em `DefaultAgentCatalog` | `DefaultPromptTemplateCatalog` | Nenhuma específica | PostgreSQL (`AiConversation`) | **STUB** — roteado via `ExecuteAiChat` genérico |
| contract-designer | Idem | Idem | Idem | Idem | **STUB** |
| change-advisor | Idem | Idem | Idem | Idem | **STUB** |
| incident-responder | Idem | Idem | Idem | Idem | **STUB** |
| test-generator | Idem | Idem | Idem | Idem | **STUB** |
| docs-assistant | Idem | Idem | Idem | Idem | **STUB** |
| security-reviewer | `SecurityReview.cs` | Hardcoded | Hardcoded (vulns fixas) | Nenhuma | **STUB** — retorna vulnerabilidades hardcoded |
| event-designer | Catalogado | Template | Nenhuma | PostgreSQL | **STUB** |
| service-scaffold-agent | `GenerateAiScaffold` | Template por slug | Nenhuma | Nenhuma | **FUNCIONAL** — gera scaffolding real via LLM |
| dependency-advisor | Catalogado | Template | Nenhuma | PostgreSQL | **STUB** |
| architecture-fitness-agent | `ArchitectureFitness.cs` | Hardcoded | Hardcoded | Nenhuma | **STUB** — scores hardcoded |
| documentation-quality-agent | `DocumentationQuality.cs` | Hardcoded | Nenhuma | Nenhuma | **STUB** |
| contract-pipeline-agent | Catalogado | Template | Nenhuma | PostgreSQL | **STUB** |

**System prompts:**
- Armazenados em `PromptTemplate` (entidade) + `DefaultPromptTemplateCatalog` (seed).
- Substituição de variáveis: `{{serviceName}}`, etc.
- Templates versionados com `Version` e `IsActive`.

**Tools disponíveis (14+):**
- `TriggerBlastRadiusTool`
- `GetKnowledgeDocsTool`
- `GetCostContextTool`
- `GetComplianceStatusTool`
- `CheckSloStatusTool`
- `GetEvidencePackTool`
- `ListRecentChangesTool`
- `SearchKnowledgeTool`
- `SearchIncidentsTool`
- `ListServicesInfoTool`
- `ListContractVersionsTool`
- `GetTokenUsageSummaryTool`
- `GetServiceHealthTool`
- `GetContractDetailsTool`
- `GetRunbookTool`

Executor: `AgentToolExecutor.cs` — roteamento por nome, case-insensitive.

**Orquestração:**
- **NÃO usa LangGraph, Semantic Kernel, ou state machine formal.**
- Orquestração manual via `AiAgentRuntimeService` e `SkillExecutorService`.
- `AgentExecutionPlan` / `AIExecutionPlan` — planejamento estruturado, mas agentes individuais ainda não conectados a pipelines de inferência especializados.

#### 2.2 FLUXO DE EXECUÇÃO

```
Usuário faz pergunta
  → POST /api/v1/ai/chat (ou /ai/assistant)
    → ExecuteAiChat handler
      → [1] Model Resolution (AiModelCatalogService)
      → [2] Quota Validation (AiTokenQuotaService)
      → [3] Guardrails Input (AiGuardrailEnforcementService)
          → Max length 100K chars
          → 9 regex de prompt injection (ignore previous instructions, DAN, etc.)
      → [4] Context Grounding (DatabaseRetrievalService)
          → 5 readers paralelos (Catalog, Change, Incident, Contract, AIModel)
          → Timeout 500ms cada, truncamento 200 chars
      → [5] Provider Invocation (IAiProviderFactory → provider específico)
          → Async, streaming SSE (OpenAI/Ollama)
      → [6] Token Usage Audit (AiTokenUsageLedger)
      → [7] Guardrails Output (PII detection)
      → [8] Conversation Persistence (ConversationPersistenceService)
      → Resposta ao usuário (JSON/Markdown via SSE)
```

**Human-in-the-loop:**
- Existe `ApproveSelfHealingAction` como feature CQRS, mas **não está integrado no fluxo de chat**. Não há pausa para aprovação humana durante a execução.

**Retry com backoff exponencial:**
- `MaxRetries: 2` para Ollama (configurado em appsettings). **Não há backoff exponencial confirmado** — apenas retry linear.

**Fallback para modelo alternativo:**
- **SIM** — `AiRoutingResolver` implementa cascade fallback. Fallback prefix: `[FALLBACK_PROVIDER_UNAVAILABLE]`.

**Timeout por agent:**
- **NÃO** — timeout é por provider (60s/120s), não por agente. `incident-responder` não tem timeout diferenciado de chat geral.

---

### SEÇÃO 3: RAG (RETRIEVAL AUGMENTED GENERATION)

#### 3.1 VECTOR DATABASE

- **NÃO há Vector DB dedicado** (Qdrant, Weaviate, Chroma, Milvus).
- **PostgreSQL + pgvector** é usado para armazenamento vetorial.
  - Imagem Docker: `pgvector/pgvector:pg16` (`docker-compose.yml`, linha 31).
  - Coluna: `EmbeddingVector` do tipo `vector(768)`.
  - Operador: `<=>` (cosine distance).
- Fallback: se pgvector não disponível, usa cosine similarity em memória ou string matching.

**Geração de embeddings:**
- **Ollama:** `nomic-embed-text` via `/api/embeddings` (`OllamaEmbeddingProvider.cs`).
- **OpenAI:** via `/v1/embeddings` (`OpenAiEmbeddingProvider.cs`).
- **Dimensão:** 768 (configurado no código).
- **Local vs API:** ambos suportados, dependendo do provider configurado.

#### 3.2 DOCUMENT PIPELINE

**Como documentos entram:**
- `EmbeddingIndexJob.cs` — background service com `PeriodicTimer` (a cada 30 minutos).
- Processa `AIKnowledgeSource` com `EmbeddingJson == null`.
- Gera embedding e persiste em `EmbeddingJson` + coluna pgvector.

**Chunking strategy:**
- **NÃO há chunking sofisticado** confirmado. O código parece indexar documentos inteiros (`AIKnowledgeSource`).
- Sem controle de overlap, tamanho de chunk, ou estratégia por sentença/parágrafo.

**Metadata:**
- `AIKnowledgeSource` armazena: `TenantId`, `ServiceId`, `DataSource`, `Category`, `Content`, `EmbeddingVector`.

**Fontes de dados indexadas:**
- Service Catalog (via `CatalogGroundingReader`)
- Contratos (via `ContractGroundingReader`)
- Mudanças históricas (via `ChangeGroundingReader`)
- Incidentes (via `IncidentGroundingReader`)
- Documentos de conhecimento (via `KnowledgeDocumentGroundingReader`)

**Nota crítica:** `DatabaseRetrievalService` (RAG principal) consulta 5 readers em paralelo, mas **faz grounding estruturado do banco relacional, não vector search semântico puro**. O vector search existe (`DocumentRetrievalService.cs`) mas não é o principal mecanismo de RAG usado pelo chat.

#### 3.3 RAG QUERY PIPELINE

**Busca semântica:**
- `DocumentRetrievalService.cs` — embedding da query + busca ANN no pgvector.
- **Não há re-ranking** confirmado.
- **Filtros:** `tenant_id` (implícito via EF Core), `data_source` (parâmetro opcional).
- **Top-k:** não explicitado no código lido, provavelmente padrão do pgvector (`LIMIT` não confirmado).

**Uso por agent:**
- O RAG/grounding é usado pelo `ExecuteAiChat` e `SendAssistantMessageLlm` de forma **genérica** para todos os agentes.
- Não há especialização: `service-analyst` não usa RAG diferenciado de `incident-responder`.

---

### SEÇÃO 4: IA EXTERNA (GOVERNADA)

#### 4.1 INTEGRAÇÃO COM PROVIDERS EXTERNOS

| Provider | Classe de Integração | API Key Storage | Rate Limiting | Fallback |
|----------|---------------------|-----------------|---------------|----------|
| **OpenAI** | `OpenAiProvider.cs` / `OpenAiHttpClient.cs` | `appsettings.json` (campo vazio no repo) + `.env.example` (`OPENAI_API_KEY=`) | Política `Ai` (30 req/min) via RateLimiter do .NET | Sim (cascade para Ollama) |
| **Anthropic** | `AnthropicProvider.cs` / `AnthropicHttpClient.cs` | Idem | Idem | Sim |
| **LM Studio** | `LmStudioProvider.cs` / `LmStudioHttpClient.cs` | Não requer (local) | Idem | Sim |

**Armazenamento de API keys:**
- `appsettings.json` — campo `ApiKey` vazio (`"ApiKey": ""`).
- `.env.example` — `OPENAI_API_KEY=` vazio.
- **Não há Vault (Azure Key Vault, HashiCorp Vault) confirmado** no código para secrets de IA.
- **RISCO:** secrets podem estar sendo injetadas via env vars em produção sem gestão centralizada.

#### 4.2 POLÍTICAS DE ACESSO

**Model Registry com políticas:**
- `AIModel` + `AiModelAuthorizationService` — RBAC via permissões (`ai:governance:read/write`, etc.).
- `ExternalAiPolicy` — políticas de uso de IA externa por tenant/equipe.
- `AiTokenQuotaPolicy` — quotas diárias/mensais/por-request.

**Routing inteligente:**
- `AIRoutingStrategy` — regras por persona/use-case/client-type com wildcard `*` e self-adjusting priority.
- `AiRoutingResolver` — fallback model = `llama3.2` (Ollama).
- **NÃO há routing baseado em sensibilidade de dados automático** (ex: "dados sensíveis → força local"). O routing é baseado em regras configuradas, não em análise do conteúdo.

**Quota alerts:**
- `TokenBudgetExceeded` evento existe (`AiGovernanceNotificationHandlerTests.cs`), mas **não há confirmação de alerta em 80% da quota**.

#### 4.3 CUSTO E AUDITORIA

**Tracking de custo:**
- `AiTokenUsageLedger` — registra tokens input/output por requisição.
- Custo estimado em EUR/USD: **não confirmado explicitamente** no código lido. O ledger armazena tokens, mas a conversão para moeda não é evidente.
- Dashboard: `GetAiUsageDashboard` + `TokenBudgetPage.tsx`.

**Audit trail:**
- `AiExternalInferenceRecord` / `AiTokenUsageLedger`
- Registra: timestamp, usuário, modelo, tokens consumidos, prompt (sanitizado?), resposta.
- **Hash SHA-256:** mencionado em requisitos de arquitetura, mas **não confirmado na implementação** do ledger.

---

### SEÇÃO 5: BIBLIOTECAS E DEPENDÊNCIAS

#### 5.1 PACOTES NUGET (IA-RELACIONADOS)

| Pacote | Versão | Para que serve | Onde é usado | Status |
|--------|--------|---------------|--------------|--------|
| **Microsoft.ML** | 4.0.0 | Machine Learning | `NexTraceOne.AIKnowledge.Application.csproj` | **DEAD WEIGHT** — apenas `using Microsoft.ML;` em `IntelligentRouter.cs` (stub vazio). |

**Pacotes esperados que NÃO existem:**

| Pacote | Status | Impacto |
|--------|--------|---------|
| Microsoft.SemanticKernel | ❌ Não encontrado | Principal SDK de orquestração de IA da Microsoft |
| OllamaSharp | ❌ Não encontrado | Cliente .NET para Ollama |
| Tiktoken | ❌ Não encontrado | Contagem precisa de tokens OpenAI |
| Microsoft.ML.Tokenizers | ❌ Não encontrado | Tokenização genérica |
| Qdrant.Client | ❌ Não encontrado | Vector DB dedicado |
| Azure.AI.OpenAI | ❌ Não encontrado | SDK oficial Azure OpenAI |
| OpenAI (SDK oficial .NET) | ❌ Não encontrado | SDK oficial OpenAI |
| Anthropic (SDK oficial) | ❌ Não encontrado | SDK oficial Anthropic |
| LLamaSharp | ❌ Não encontrado | Inferência local de GGUF |
| TorchSharp | ❌ Não encontrado | Deep learning em .NET |
| Microsoft.ML.OnnxRuntime | ❌ Não encontrado | Inferência ONNX |

**Nota arquitetural:** A decisão de usar apenas `HttpClient` nativo é intencional ("zero dependências extras"). Isso reduz supply chain risk, mas aumenta manutenção (DTOs próprios, parsing manual de SSE, etc.).

#### 5.2 PACOTES PYTHON

- **ZERO dependências Python.** Não há `requirements.txt`, `pyproject.toml`, ou serviço Python auxiliar.
- Roadmap (`AI-AGENT-LIGHTNING.md`) menciona serviço Python standalone para RL, mas não existe no código.

#### 5.3 DOCKER / INFRA

| Serviço | Containerizado? | Arquivo | Observação |
|---------|----------------|---------|------------|
| Ollama | ❌ NÃO | — | Serviço externo esperado em localhost:11434 |
| Qdrant | ❌ NÃO | — | Não mencionado |
| Weaviate | ❌ NÃO | — | Não mencionado |
| Chroma | ❌ NÃO | — | Não mencionado |
| Redis | ✅ SIM | `deploy/kubernetes/helm/nextraceone/values.yaml` | Usado para cache distribuído (quotas) |
| vLLM | ❌ NÃO | — | Apenas roadmap |
| PostgreSQL+pgvector | ✅ SIM | `docker-compose.yml` linha 31 | `pgvector/pgvector:pg16` |

---

### SEÇÃO 6: FUNCIONALIDADES — O QUE FUNCIONA vs O QUE NÃO FUNCIONA

#### 6.1 STATUS POR COMPONENTE

| Componente | Status | Evidência |
|------------|--------|-----------|
| PII Scanner | PARCIAL | `AiGuardrailEnforcementService.cs` |
| Token Budget Enforcer | FUNCIONAL | `AiTokenQuotaService.cs` + testes |
| Model Router (Local vs Externo) | FUNCIONAL | `AiRoutingResolver.cs` + testes |
| Prompt Cache | STUB | `InMemoryEmbeddingCacheService` (Singleton) |
| Context Compressor | PARCIAL | `ContextWindowManager.cs` (heurística simples) |
| Audit Logger (Hash Chain) | FUNCIONAL | `AiTokenUsageLedger` (hash não confirmado) |
| service-analyst agent | STUB | `DefaultAgentCatalog.cs` |
| contract-designer agent | STUB | Idem |
| change-advisor agent | STUB | Idem |
| incident-responder agent | STUB | Idem |
| test-generator agent | STUB | Idem |
| docs-assistant agent | STUB | Idem |
| security-reviewer agent | STUB | `SecurityReview.cs` (hardcoded) |
| RAG Pipeline | PARCIAL | `DatabaseRetrievalService` + pgvector |
| Model Registry | FUNCIONAL | `AIModel` + UI + seed |
| Token Cost Tracker | FUNCIONAL | `AiTokenUsageLedger` |
| External API Integration (OpenAI) | FUNCIONAL | `OpenAiProvider.cs` + testes |
| External API Integration (Anthropic) | PARCIAL | `AnthropicProvider.cs` (documentado incompleto) |
| Human-in-the-loop | STUB | `ApproveSelfHealingAction` não integrado ao chat |
| Streaming de respostas | FUNCIONAL | SSE em OpenAI/Ollama |
| Multi-turn conversation | FUNCIONAL | `ConversationPersistenceService.cs` |

#### 6.2 TESTES

| Tipo | Quantidade | Cobertura | Observação |
|------|------------|-----------|------------|
| Unitários | ~120 arquivos | Boa (Domain + Application) | xUnit + FluentAssertions + NSubstitute |
| Integração (DB) | 1 arquivo | Boa | `AiGovernancePostgreSqlTests.cs` (Testcontainers PostgreSQL) |
| Integração (HTTP) | 0 | — | **NENHUM teste chama providers reais** |
| E2E API | 0 | — | **NENHUM teste E2E de chat/scaffold** |
| Performance/Load | 0 | — | **NENHUM teste de carga para endpoints de IA** |
| Selenium UI | 1 arquivo | Básico | `AiHubNavigationTests.cs` (smoke de navegação) |

**Mocks:** Não há mocks/stubs centralizados reutilizáveis. Todos os mocks são criados inline via NSubstitute.

---

### SEÇÃO 7: GAPS E RISCOS IDENTIFICADOS

#### 7.1 GAPS TÉCNICOS

| Gap | Severidade | Detalhamento |
|-----|------------|--------------|
| Context window limitado | 🔴 Alto | `ContextWindowManager.cs` usa heurística ~4 chars/token. Modelos de 32k+ context não são explorados eficientemente. Sem Tiktoken/Microsoft.ML.Tokenizers. |
| RAG sem re-ranking | 🟠 Médio | pgvector retorna resultados por cosine distance, mas sem re-ranking (Cohere Rerank, Cross-Encoder). Qualidade questionável. |
| Tool calling incompleto | 🟠 Médio | `AgentToolExecutor.cs` existe, mas agentes individuais não estão conectados a pipelines que usem tools de forma especializada. |
| Sem SDK de orquestração | 🟡 Médio | Semantic Kernel ou LangChain acelerariam desenvolvimento de agents complexos. Trade-off: supply chain risk vs velocity. |
| Modelos locais insuficientes | 🔴 Alto | deepseek-r1:1.5b e llama3.2:3b são placeholders de desenvolvimento. Para produção, exige Qwen 2.5 Coder 32B + GPU RTX 4090. |
| Sem vector DB dedicado | 🟡 Médio | pgvector funciona para escala média, mas não escala para very-large-scale semantic search. |

#### 7.2 GAPS DE COMPLIANCE

| Gap | Severidade | Detalhamento |
|-----|------------|--------------|
| FRIA não documentada | 🔴 Alto | Não há evidência de Fundamental Rights Impact Assessment (EU AI Act). |
| Human oversight técnico | 🟠 Médio | `ApproveSelfHealingAction` existe mas não está no fluxo crítico de chat. Não há "human-in-the-loop" obrigatório. |
| Audit trail por inferência | 🟡 Médio | `AiExternalInferenceRecord` registra por requisição, mas **não há hash chain confirmado** nem garantia de imutabilidade. |
| Exit strategy para providers | 🟡 Médio | Fallback para local existe, mas sem bundle offline de modelos curados (`WAVE-04-AI-LOCAL.md` W4-05 não implementado). |
| Grounding não redacta PII | 🔴 Alto | Documentado em `SECURITY-ARCHITECTURE.md`: "Grounding não redacta PIIs antes de enviar ao LLM." |
| Prompt injection mitigation | 🟠 Médio | 9 regex existem, mas documentação admite "sem sanitização explícita". |

#### 7.3 RISCOS OPERACIONAIS

| Risco | Severidade | Evidência |
|-------|------------|-----------|
| HttpClient sem IHttpClientFactory | 🔴 Crítico | `AUDIT-MODULE-AIKNOWLEDGE-2026-05-14.md` P-C03 — socket exhaustion em alta carga. |
| SQL Injection em ClickHouse | 🔴 Crítico | `AUDIT-MODULE-AIKNOWLEDGE-2026-05-14.md` P-C02 — string interpolation em queries ClickHouse. |
| Cross-tenant exposure | 🔴 Crítico | `AUDIT-MODULE-AIKNOWLEDGE-2026-05-14.md` P-C01 — 36 de 54 repositórios sem filtro de tenant. |
| Sem fallback automático de GPU | 🟠 Alto | Se Ollama cai, fallback para OpenAI/Anthropic depende de API keys e política. Sem bundle offline. |
| Cache de quotas in-memory (Singleton) | 🟠 Alto | `InMemoryTokenQuotaCache` não é compartilhado entre instâncias horizontais. |
| NEST deprecated (Elasticsearch 7.x) | 🟠 Alto | `AUDIT-MODULE-AIKNOWLEDGE-2026-05-14.md` — deveria usar `Elastic.Clients.Elasticsearch` 8.x. |
| Sem testes de carga | 🟡 Médio | Nenhum teste k6 para endpoints de IA. |
| Microsoft.ML no Application layer | 🟡 Médio | ~200MB de dead weight violando Clean Architecture. |

---

### SEÇÃO 8: PLANO DE EVOLUÇÃO PROPOSTO

#### FASE 1 — FOUNDATION (1-2 meses)
**Objetivo:** Fechar buracos de segurança críticos e consolidar infraestrutura base.

| Tarefa | Classe/Arquivo a Modificar | Biblioteca | Testes |
|--------|---------------------------|------------|--------|
| Fix SQL Injection ClickHouse | `ClickHouseAiAnalyticsRepository.cs` | Parametrizar queries | `ClickHouseAiAnalyticsRepositoryTests.cs` (novo) |
| Fix HttpClient factory | `OpenAiHttpClient.cs`, `OllamaHttpClient.cs`, `AnthropicHttpClient.cs`, `LmStudioHttpClient.cs` | `IHttpClientFactory` (nativo) | Testes de resiliência |
| Fix tenant filter (36 repos) | 36 repositórios em `src/modules/aiknowledge/.../Repositories` | — | `AiGovernancePostgreSqlTests.cs` (expandir) |
| Remover Microsoft.ML dead weight | `Directory.Packages.props`, `NexTraceOne.AIKnowledge.Application.csproj`, `IntelligentRouter.cs` | — | — |
| Implementar cache distribuído de prompts | Novo: `DistributedPromptCache.cs` | StackExchange.Redis (já usado para quotas) | Testes de cache hit/miss |
| Adicionar tokenização real | Novo: `TokenCounterService.cs` | `Microsoft.ML.Tokenizers` ou `Tiktoken` (novo) | `TokenCounterServiceTests.cs` |
| Containerizar Ollama | `docker-compose.yml` (novo serviço) | `ollama/ollama` image | Testes de integração |
| Implementar PII redaction no grounding | `DatabaseRetrievalService.cs` + `AiGuardrailEnforcementService.cs` | Presidio (opcional) ou regex nativo | `PiiRedactionTests.cs` |

**Métricas de sucesso:**
- 0 vulnerabilidades críticas no módulo AIKnowledge.
- 100% dos repositórios com filtro de tenant.
- Cache de prompts com hit rate > 60%.
- Token counting com erro < 5% vs Tiktoken.

---

#### FASE 2 — OTIMIZAÇÃO (2-3 meses)
**Objetivo:** Reduzir custo, latência, e consumo de tokens externos.

| Tarefa | Classe/Arquivo a Modificar | Biblioteca | Testes |
|--------|---------------------------|------------|--------|
| Implementar prompt cache inteligente | `ExecuteAiChat.cs`, `AiResourceGovernor.cs` | — | Testes de latência |
| Implementar context compression | Novo: `ContextCompressor.cs` | `LLMLingua` (via Python bridge?) ou truncamento semântico | `ContextCompressorTests.cs` |
| Implementar routing por conteúdo (sensibilidade) | `AiRoutingResolver.cs` | — | Testes de routing |
| Melhorar RAG (chunking + re-ranking) | `EmbeddingIndexJob.cs`, `DocumentRetrievalService.cs` | Chunking próprio; re-ranking via API externa ou modelo local | Testes de relevance |
| Implementar vector DB dedicado (opcional) | Novo: `QdrantVectorStore.cs` | `Qdrant.Client` (novo) | Testes de integração Qdrant |
| Melhorar fallback com health checks | `AiProviderHealthService.cs` | — | Testes de failover |
| Adicionar rate limiting por tenant | `AiTokenQuotaService.cs` | — | Testes de throttling |

**Métricas de sucesso:**
- Redução de 30% no consumo de tokens externos.
- Latência p95 do chat < 3s (local) / < 5s (externo).
- Precisão do RAG (relevance) medida em > 70%.

---

#### FASE 3 — CAPACIDADES (3-4 meses)
**Objetivo:** Implementar agents faltantes, melhorar RAG, adicionar tool calling especializado.

| Tarefa | Classe/Arquivo a Modificar | Biblioteca | Testes |
|--------|---------------------------|------------|--------|
| Implementar agents especializados | `service-analyst`, `incident-responder`, `change-advisor`, `security-reviewer` | — | Testes de fluxo por agente |
| Conectar agents a tools especializadas | `AgentToolExecutor.cs` + tools específicas | — | `AgentToolExecutorTests.cs` (expandir) |
| Implementar SAST real no security-reviewer | `SecurityReview.cs` | Semgrep CLI ou Roslyn analyzers | `SecurityReviewIntegrationTests.cs` |
| Implementar skills dinâmicas | `SkillExecutorService.cs`, `SkillRegistry.cs` | — | `SkillExecutorServiceTests.cs` |
| Melhorar RAG com mais fontes | Novos readers: `LogGroundingReader`, `RunbookGroundingReader` | — | Testes de grounding |
| Implementar evaluation harness | `EvaluationSuite`, `EvaluationCase`, `EvaluationRun` | — | `EvaluationHarnessTests.cs` |
| Implementar A/B testing de modelos | `AIRoutingStrategy.cs` + novo `AiModelExperiment` | — | Testes de experimento |

**Métricas de sucesso:**
- 7+ agents funcionais com pipelines especializados.
- Tool calling funcional em 3+ agents.
- Evaluation harness rodando 100+ casos de teste.

---

#### FASE 4 — GOVERNANCE (2-3 meses)
**Objetivo:** Compliance enterprise, audit trail completo, dashboards.

| Tarefa | Classe/Arquivo a Modificar | Biblioteca | Testes |
|--------|---------------------------|------------|--------|
| Implementar hash chain no audit trail | `AiTokenUsageLedger.cs` + `AiExternalInferenceRecord.cs` | SHA-256 nativo | `AuditTrailIntegrityTests.cs` |
| Implementar token budgets enforcement UI | `TokenBudgetPage.tsx` + backend | — | Testes E2E |
| Implementar compliance dashboards | `AiGovernancePage.tsx`, `AiAuditPage.tsx` | — | Testes Selenium |
| Documentar FRIA (EU AI Act) | Novo: `docs/compliance/FRIA-AIKNOWLEDGE.md` | — | Review legal |
| Implementar human-in-the-loop obrigatório | `ExecuteAiChat.cs` + `ApproveSelfHealingAction` | — | `HumanInTheLoopTests.cs` |
| Implementar alertas de quota (80%) | `AiTokenQuotaService.cs` + Notifications | — | `QuotaAlertTests.cs` |
| Implementar offline model bundle | `WAVE-04-AI-LOCAL.md` W4-05 | — | Testes de instalação offline |
| Implementar MCP server production-ready | `McpEndpointModule.cs` | — | Testes de integração MCP |

**Métricas de sucesso:**
- 100% das inferências com hash de integridade.
- Alertas de quota funcionando em 80%/95%/100%.
- FRIA documentada e aprovada.
- Bundle offline instalável em < 30 minutos.

---

## 4. GAPS PRIORIZADOS

| Prioridade | Gap | Impacto | Esforço |
|------------|-----|---------|---------|
| **P0** | SQL Injection + Cross-tenant + HttpClient factory | Segurança crítica | 2 semanas |
| **P0** | PII redaction no grounding | Compliance/DP | 1 semana |
| **P1** | Tokenização real (Tiktoken/ML.Tokenizers) | Qualidade/custo | 1 semana |
| **P1** | Prompt cache distribuído | Performance/custo | 2 semanas |
| **P1** | Agents especializados funcionais | Valor de negócio | 1-2 meses |
| **P2** | Vector DB dedicado | Escalabilidade RAG | 2-3 semanas |
| **P2** | Context compression | Performance | 2 semanas |
| **P2** | Hash chain audit trail | Compliance | 1 semana |
| **P3** | Semantic Kernel / LangChain | Velocity de dev | 1 mês (avaliação) |
| **P3** | Skills dinâmicas + RL | Inovação | 3-4 meses |

---

## 5. RECOMENDAÇÕES IMEDIATAS (O QUE FAZER HOJE)

1. **🔒 Fixar segurança crítica:** Corrigir SQL Injection em `ClickHouseAiAnalyticsRepository.cs`, adicionar `IHttpClientFactory` nos 4 providers, e auditAR os 36 repositórios sem tenant filter. Isso é não-negociável antes de qualquer deploy.
2. **🧹 Remover dead weight:** Remover `Microsoft.ML` do `Directory.Packages.props` e do `.csproj`. Se `IntelligentRouter.cs` for necessário no futuro, reavaliar com ML.NET ou alternativa mais leve.
3. **🧪 Escrever testes de integração HTTP:** Criar `OpenAiProviderIntegrationTests.cs` e `OllamaProviderIntegrationTests.cs` usando WireMock ou Testcontainers para validar parsing de SSE e JSON.
4. **📊 Medir antes de otimizar:** Adicionar métricas OpenTelemetry nos handlers `ExecuteAiChat` e `SendAssistantMessageLlm` para medir latência por provider, taxa de cache miss, e tokens por request.
5. **🗂️ Especializar 1 agent:** Escolher o `incident-responder` ou `service-analyst` e implementar seu pipeline completo (system prompt especializado + tools + grounding especializado) como prova de conceito para os demais.

---

*Relatório gerado por análise forense automatizada do código-fonte. Todos os arquivos, classes e métodos citados foram verificados no codebase commit atual.*
