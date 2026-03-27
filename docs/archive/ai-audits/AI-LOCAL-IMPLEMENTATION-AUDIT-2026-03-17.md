# Auditoria Técnica Completa — IA Local no NexTraceOne

> **Nota:** Este documento é um relatório histórico. Referências a Grafana/Tempo/Loki no contexto do Docker Compose refletem a stack planeada inicialmente. A stack de observabilidade foi migrada para provider configurável (ClickHouse ou Elastic). Ver `docs/observability/` para a documentação atual.

**Data:** 2026-03-17  
**Escopo:** Análise rigorosa do código-fonte, configurações, dependências e fluxos reais.  
**Metodologia:** Varredura completa do repositório com inspeção de conteúdo real de cada ficheiro.

---

## 1. Resumo Executivo

**Estágio geral:** A IA local no NexTraceOne encontra-se em estágio **inicial-parcial**. Existe um módulo `aiknowledge` com arquitetura DDD/Clean Architecture bem definida, dividido em 3 subdomínios (Governance, ExternalAI, Orchestration).

O subdomínio de **Governance** é o único com implementação real funcional — possui 24 de 28 handlers com lógica real, 11 repositórios, 11 configurações EF Core, endpoints REST completos e testes unitários. Os subdomínios **ExternalAI** e **Orchestration** são 100% stubs/TODO.

O frontend possui 7 páginas e 1 componente reutilizável, todos com UI funcional mas usando dados mock hardcoded.

**Fato crítico:** Não existe nenhuma dependência de SDK de IA (Semantic Kernel, LangChain, OpenAI SDK) no projeto inteiro. Não há provider local configurado. Não há vector store. Não há embedding. Não há RAG. Não há inferência local. A IA descrita na arquitetura é uma **intenção documentada**, não uma capacidade funcional.

**Percentual estimado de maturidade:** ~20-25%

| Camada | Maturidade |
|--------|-----------|
| Domain Model | ~85% (entidades e enums bem definidos) |
| Infrastructure/Persistence | ~35% (só Governance tem repos e DbContext real) |
| Application/Features | ~30% (24/44 handlers reais, mas sem LLM real) |
| API Endpoints | ~35% (Governance completo, ExternalAI e Orchestration stub) |
| Frontend | ~40% (UI funcional, dados 100% mock) |
| Integração com LLM real | 0% |
| Vector/Embedding/RAG | 0% |
| Provider local | 0% |
| Infraestrutura Docker/AI | 0% |
| Migrations de BD para IA | 0% (DbContexts não registados para migration) |

**Conclusão objetiva:** Estágio **inicial-parcial** — existe um esqueleto arquitetural bem construído com uma vertical funcional (Governance) e UI demonstrativa, mas sem nenhuma capacidade real de IA (inferência, embeddings, RAG, providers).

---

## 2. Inventário Técnico Encontrado

### 2.1 Backend — Módulo `aiknowledge`

```
src/modules/aiknowledge/
├── NexTraceOne.AIKnowledge.Domain/
│   ├── ExternalAI/
│   │   ├── Entities/ (4): ExternalAiProvider, ExternalAiPolicy, ExternalAiConsultation, KnowledgeCapture
│   │   ├── Enums/ (2): KnowledgeStatus, ConsultationStatus
│   │   ├── Events/ (2): ExternalAIQueryRequestedEvent, ExternalAIResponseReceivedEvent
│   │   ├── Ports/ (1): IExternalAIRoutingPort
│   │   └── Errors/
│   ├── Governance/
│   │   ├── Entities/ (14): AIModel, AIRoutingStrategy, AIBudget, AIAccessPolicy,
│   │   │   AIUsageEntry, AiAssistantConversation, AiMessage, AIRoutingDecision,
│   │   │   AIExecutionPlan, AIIDEClientRegistration, AIIDECapabilityPolicy,
│   │   │   AIKnowledgeSource, AIKnowledgeSourceWeight, AIEnrichmentResult
│   │   ├── Enums/ (10): ModelType, ModelStatus, AIClientType, AIUseCaseType,
│   │   │   AIRoutingPath, BudgetPeriod, KnowledgeSourceType, etc.
│   │   └── Errors/
│   └── Orchestration/
│       ├── Entities/ (4): AiContext, AiConversation, GeneratedTestArtifact, KnowledgeCaptureEntry
│       ├── Enums/ (3): ArtifactStatus, ConversationStatus, KnowledgeEntryStatus
│       └── Events/ (1): KnowledgeCandidateCreatedEvent
│
├── NexTraceOne.AIKnowledge.Application/
│   ├── ExternalAI/Features/ (8 ficheiros — todos TODO/stub)
│   ├── Governance/
│   │   ├── Abstractions/ (11 interfaces de repositório)
│   │   └── Features/ (28 handlers — 24 reais, 2 mistos, 2 mock)
│   └── Orchestration/Features/ (8 ficheiros — todos TODO/stub)
│
├── NexTraceOne.AIKnowledge.Infrastructure/
│   ├── ExternalAI/
│   │   ├── Persistence/ExternalAiDbContext.cs (sem DbSet — TODO)
│   │   └── DependencyInjection.cs (TODO — sem registos)
│   ├── Governance/
│   │   ├── Persistence/
│   │   │   ├── AiGovernanceDbContext.cs (11 DbSets — COMPLETO)
│   │   │   ├── Configurations/ (11 EF Core configs)
│   │   │   └── Repositories/ (11 repositórios — COMPLETO)
│   │   └── DependencyInjection.cs (11 repos registados + DbContext — COMPLETO)
│   └── Orchestration/
│       ├── Persistence/AiOrchestrationDbContext.cs (sem DbSet — TODO)
│       └── DependencyInjection.cs (TODO — sem registos)
│
├── NexTraceOne.AIKnowledge.API/
│   ├── ExternalAI/Endpoints/ (TODO — sem endpoints mapeados)
│   ├── Governance/Endpoints/ (27+ endpoints REST — COMPLETO)
│   └── Orchestration/Endpoints/ (TODO — sem endpoints mapeados)
│
└── NexTraceOne.AIKnowledge.Contracts/
    ├── ExternalAI/ServiceInterfaces/IExternalAiModule.cs (TODO)
    └── Orchestration/ServiceInterfaces/IAiOrchestrationModule.cs (TODO)
```

### 2.2 Frontend — AI Hub

```
src/frontend/src/features/ai-hub/
├── pages/
│   ├── AiAssistantPage.tsx          (UI completa com chat mock)
│   ├── ModelRegistryPage.tsx         (UI completa com modelos mock)
│   ├── AiPoliciesPage.tsx            (UI completa com políticas mock)
│   ├── TokenBudgetPage.tsx           (UI completa com budgets mock)
│   ├── AiAuditPage.tsx              (UI completa com audit mock)
│   ├── IdeIntegrationsPage.tsx       (UI completa com IDE clients mock)
│   └── AiRoutingPage.tsx            (UI completa com routing mock)
├── components/
│   └── AssistantPanel.tsx            (componente reutilizável para páginas de detalhe)
└── api/
    └── aiGovernance.ts               (23 endpoints HTTP configurados)
```

**AssistantPanel integrado em 4 páginas de detalhe:**
- `ContractDetailPage.tsx` — context type="contract"
- `ServiceDetailPage.tsx` — context type="service"
- `IncidentDetailPage.tsx` — context type="incident"
- `ChangeDetailPage.tsx` — context type="change"

### 2.3 Testes

```
tests/modules/aiknowledge/NexTraceOne.AIKnowledge.Tests/
├── Governance/
│   ├── Application/Features/ContextBundleTests.cs (11 testes — context bundles)
│   └── Domain/Entities/AiGovernanceEntityTests.cs (41+ testes — entidades governance)
├── ExternalAI/
│   └── Domain/Entities/ExternalAiConsultationTests.cs (testes de consulta)
└── Orchestration/
    └── Domain/Entities/AiConversationTests.cs, AiOrchestrationEntityTests.cs
```

**Frontend:**
- `AiAssistantPage.test.tsx` — 9 testes
- `AssistantPanel.test.tsx` — 25 testes

### 2.4 Documentação

- `docs/AI-GOVERNANCE.md` — Princípios de governança
- `docs/AI-ARCHITECTURE.md` — Arquitetura do sistema de IA
- `docs/AI-ASSISTED-OPERATIONS.md` — Casos de uso operacionais
- `docs/AI-DEVELOPER-EXPERIENCE.md` — Experiência do desenvolvedor

### 2.5 Infraestrutura e Configuração

- **Docker Compose:** Apenas telemetria (OTel, Grafana, Tempo, Loki) — **sem containers de IA**
- **appsettings.json:** **Nenhuma secção de configuração de IA**
- **Variáveis de ambiente:** **Nenhuma para providers de IA**
- **NuGet packages IA:** **ZERO** (sem Semantic Kernel, OpenAI SDK, LangChain)
- **npm packages IA:** **ZERO** (sem OpenAI JS, LangChain.js, tiktoken)

### 2.6 Registo no Host

O módulo está registado no `Program.cs` do ApiHost:
```csharp
builder.Services.AddAiGovernanceModule(builder.Configuration);
builder.Services.AddExternalAiModule(builder.Configuration);
builder.Services.AddAiOrchestrationModule(builder.Configuration);
```

**Porém:** Os DbContexts de IA **não estão incluídos** no pipeline de migrations em `WebApplicationExtensions.cs`.

---

## 3. O Que Está Realmente Implementado

### 3.1 Governance — Vertical Funcional (a mais madura)

| Componente | Estado | Evidência |
|-----------|--------|-----------|
| 14 entidades de domínio | ✅ Implementado | AIModel, AIAccessPolicy, AIBudget, etc. com factory methods, validação, lifecycle |
| 10 enums de domínio | ✅ Implementado | ModelType, ModelStatus, AIClientType, AIUseCaseType, AIRoutingPath, etc. |
| 11 interfaces de repositório | ✅ Implementado | IAiModelRepository, IAiAccessPolicyRepository, etc. |
| 11 implementações de repositório | ✅ Implementado | LINQ com filtragem, paginação, async/await |
| 11 configurações EF Core | ✅ Implementado | AiGovernanceDbContext com 11 DbSets |
| DI Registration | ✅ Implementado | 11 repositórios + DbContext registados |
| 24 handlers MediatR reais | ✅ Implementado | CQRS com validação, repositórios, lógica de domínio |
| 27+ endpoints REST | ✅ Implementado | Permission-based, MediatR, error localization |
| Testes unitários de domínio | ✅ Implementado | 41+ testes para entidades Governance |
| Testes de context bundle | ✅ Implementado | 11 testes para serialização/desserialização |

### 3.2 Handler SendAssistantMessage — O mais completo (644 linhas)

- ✅ Resolução/criação de conversação
- ✅ Persistência de mensagem do utilizador
- ✅ Classificação de use case por heurística de keywords
- ✅ Seleção de estratégia de routing inteligente
- ✅ Grounding de knowledge sources com ponderação
- ✅ Desserialização de context bundle (JSON) com fallback
- ✅ Dois modos de geração de resposta (stub vs grounded)
- ✅ Registo de auditoria (usage entry)
- ✅ Metadados completos (tokens, routing, confidence, caveats)

**Nota (criticidade ALTA):** A "resposta da IA" é gerada por lógica heurística interna (templates de texto), **não** por invocação de um LLM real.

### 3.3 Frontend — UI Funcional com Dados Mock

- ✅ 7 páginas completas no AI Hub com filtros, pesquisa, estatísticas
- ✅ AssistantPanel reutilizável integrado em 4 páginas de detalhe
- ✅ 23 endpoints HTTP configurados no service API
- ✅ 30+ chaves i18n dedicadas à IA
- ✅ 5 rotas no sidebar com permissões
- ✅ 34 testes frontend (9 + 25)

---

## 4. O Que Está Parcialmente Implementado

### 4.1 SendAssistantMessage — Governança sem LLM

**O que existe:** Handler completo com routing, grounding, auditoria, context bundles.  
**O que falta:** A resposta é gerada por templates internos. Não há invocação de modelo de linguagem. Para ser funcional, precisa de:
- Integração com SDK de IA (Semantic Kernel, OpenAI SDK)
- Adaptador para provider (local ou externo)
- Pipeline de prompt construction
- Chamada real de completions API

### 4.2 EnrichContext — Contexto Simulado

**O que existe:** Classificação de use case, consulta a knowledge sources via repositório.  
**O que falta:** O contexto retornado é sintético/simulado. Comentário no código: *"Stub: retorna contexto simulado — integração real com fontes em evolução futura."*

### 4.3 GetIdeCapabilities — Defaults Hardcoded

**O que existe:** Consulta real a `IAiIdeCapabilityPolicyRepository` para políticas existentes.  
**O que falta:** Se não encontrar política, retorna defaults hardcoded em vez de erro ou criação automática.

### 4.4 ExternalAiDbContext e AiOrchestrationDbContext

**O que existe:** Classes herdando de `NexTraceDbContextBase` com RLS, audit e outbox.  
**O que falta:** Nenhum `DbSet<T>` declarado — marcados com `// TODO: Adicionar DbSet<T>`.

### 4.5 Frontend — API Service vs Dados Mock

**O que existe:** `aiGovernance.ts` com 23 endpoints HTTP configurados.  
**O que falta:** Todas as 7 páginas usam dados mock hardcoded (`mockModels`, `mockPolicies`, `mockBudgets`, etc.) em vez de chamadas à API real.

---

## 5. O Que É Apenas Stub, Placeholder ou Intenção

### 5.1 ExternalAI Features — 8 ficheiros TODO

| Feature | Ficheiro | Conteúdo |
|---------|---------|----------|
| QueryExternalAISimple | `ExternalAI/Features/QueryExternalAISimple/QueryExternalAISimple.cs` | 5 comentários TODO |
| QueryExternalAIAdvanced | `ExternalAI/Features/QueryExternalAIAdvanced/QueryExternalAIAdvanced.cs` | 5 comentários TODO |
| CaptureExternalAIResponse | `ExternalAI/Features/CaptureExternalAIResponse/CaptureExternalAIResponse.cs` | 5 comentários TODO |
| ConfigureExternalAIPolicy | `ExternalAI/Features/ConfigureExternalAIPolicy/ConfigureExternalAIPolicy.cs` | 5 comentários TODO |
| ApproveKnowledgeCapture | `ExternalAI/Features/ApproveKnowledgeCapture/ApproveKnowledgeCapture.cs` | 5 comentários TODO |
| ReuseKnowledgeCapture | `ExternalAI/Features/ReuseKnowledgeCapture/ReuseKnowledgeCapture.cs` | 5 comentários TODO |
| GetExternalAIUsage | `ExternalAI/Features/GetExternalAIUsage/GetExternalAIUsage.cs` | 5 comentários TODO |
| ListKnowledgeCaptures | `ExternalAI/Features/ListKnowledgeCaptures/ListKnowledgeCaptures.cs` | 5 comentários TODO |

**Padrão:** Cada ficheiro contém apenas uma classe estática vazia com 5 comentários TODO:
```csharp
public static class ApproveKnowledgeCapture
{
    // TODO: Implementar record Command ou Query com dados de entrada
    // TODO: Implementar AbstractValidator<Command> com FluentValidation
    // TODO: Implementar handler herdando CommandHandlerBase ou QueryHandlerBase
    // TODO: Implementar record Response com dados de saída
}
```

### 5.2 Orchestration Features — 8 ficheiros TODO

Mesmo padrão dos ExternalAI features:
- GenerateRobotFrameworkDraft
- SuggestSemanticVersionWithAI
- ValidateKnowledgeCapture
- GenerateTestScenarios
- ClassifyChangeWithAI
- GetAiConversationHistory
- SummarizeReleaseForApproval
- AskCatalogQuestion

### 5.3 Governance Stubs (2 de 28)

| Feature | Tipo | Conteúdo |
|---------|------|----------|
| ListKnowledgeSourceWeights | Mock | Retorna 11 pesos hardcoded em memória. Comentário: *"Stub: configuração in-memory — persistência em evolução futura."* |
| ListSuggestedPrompts | Mock | Retorna 21 prompts sugeridos hardcoded. Comentário: *"Nota: prompts sugeridos são definidos em memória (code-driven) nesta fase."* |

### 5.4 Interfaces de Contrato Vazias

| Interface | Ficheiro | Conteúdo |
|-----------|---------|----------|
| IExternalAiModule | `Contracts/ExternalAI/ServiceInterfaces/IExternalAiModule.cs` | `// TODO: Definir operações` |
| IAiOrchestrationModule | `Contracts/Orchestration/ServiceInterfaces/IAiOrchestrationModule.cs` | `// TODO: Definir operações` |

### 5.5 IExternalAIRoutingPort — Interface sem Implementação

```csharp
public interface IExternalAIRoutingPort
{
    Task<string> RouteQueryAsync(string context, string query, 
        string? preferredProvider = null, CancellationToken cancellationToken = default);
}
```
**Sem adaptador registado. Sem implementação. Não utilizada em nenhum handler.**

### 5.6 Endpoints de API Vazios

| Módulo | Estado |
|--------|--------|
| ExternalAI | `_ = app.MapGroup("/api/v1/externalai"); // TODO: Mapear endpoints` |
| Orchestration | `_ = app.MapGroup("/api/v1/aiorchestration"); // TODO: Mapear endpoints` |

### 5.7 Infrastructure DI Vazios

| Módulo | Estado |
|--------|--------|
| ExternalAI | `// TODO: Registrar DbContext, repositórios, adapters` |
| Orchestration | `// TODO: Registrar DbContext, repositórios, adapters` |

---

## 6. O Que Não Existe Ainda

### 6.1 Integração com LLM — Nenhuma

- ❌ **Nenhum SDK de IA instalado** (Semantic Kernel, LangChain, OpenAI SDK, Azure.AI)
- ❌ **Nenhum provider de IA configurado** (OpenAI, Azure OpenAI, Ollama, LM Studio)
- ❌ **Nenhuma chamada real a modelo de linguagem** em todo o repositório
- ❌ **Nenhum adaptador de provider** implementado
- ❌ **Nenhuma abstração de completions/chat API** funcional
- ❌ **Nenhum pipeline de prompt construction**
- ❌ **Nenhum prompt template** persistido ou gerido

### 6.2 Embedding e Vector Store — Nenhum

- ❌ **Nenhum vector database** (Chroma, Weaviate, Milvus, Qdrant, pgvector)
- ❌ **Nenhum serviço de embedding** (local ou externo)
- ❌ **Nenhuma indexação de documentos**
- ❌ **Nenhum upload/ingestion de ficheiros para IA**
- ❌ **Nenhum similarity search**
- ❌ **Nenhum RAG pipeline**
- ❌ **Nenhum reranking**

### 6.3 Agentes e Orchestração — Nenhum

- ❌ **Nenhum agent framework** (Semantic Kernel Agents, AutoGen, LangGraph)
- ❌ **Nenhum tool calling** (function calling para LLM)
- ❌ **Nenhum skill registry** funcional
- ❌ **Nenhum memory management** para conversas (beyond message persistence)
- ❌ **Nenhum workflow long-running** de IA
- ❌ **Nenhum human-in-the-loop** funcional

### 6.4 Guardrails e Segurança de IA — Nenhum

- ❌ **Nenhum guardrail** de input/output
- ❌ **Nenhuma validação de conteúdo** gerado
- ❌ **Nenhum filtro de dados sensíveis** (PII redaction)
- ❌ **Nenhum rate limiting** específico para IA
- ❌ **Nenhum circuit breaker** para providers
- ❌ **Nenhum fallback** entre providers

### 6.5 Observabilidade de IA — Nenhuma

- ❌ **Nenhum tracing** de chamadas LLM
- ❌ **Nenhuma métrica** de latência/tokens/custo em tempo real
- ❌ **Nenhum dashboard** de monitorização de IA
- ❌ **Nenhuma integração** com OTel para IA (apesar de OTel estar configurado para o resto)

### 6.6 Infraestrutura Local — Nenhuma

- ❌ **Nenhum container Docker** para IA (Ollama, vLLM, TGI)
- ❌ **Nenhuma configuração** de endpoint local de inferência
- ❌ **Nenhuma variável de ambiente** para modelos locais
- ❌ **Nenhum docker-compose** com serviços de IA

### 6.7 IDE Extensions — Nenhuma

- ❌ **Nenhum código** de extensão VS Code
- ❌ **Nenhum código** de extensão Visual Studio
- ❌ Apenas documentação e entidades de domínio (AIIDEClientRegistration, AIIDECapabilityPolicy)

### 6.8 Migrations de Base de Dados para IA

- ❌ **AiGovernanceDbContext** não incluído no pipeline de migrations
- ❌ **ExternalAiDbContext** não incluído no pipeline de migrations
- ❌ **AiOrchestrationDbContext** não incluído no pipeline de migrations
- ❌ **Nenhum seed data** para bases de dados de IA
- Ficheiro: `src/platform/NexTraceOne.ApiHost/WebApplicationExtensions.cs` — linhas 51-60 não incluem contextos AI

---

## 7. Fluxos Reais Disponíveis Hoje

### 7.1 Fluxo de Chat Assistente (Parcial — sem LLM)

```
[Frontend: AiAssistantPage]
  → (mock data local — não chama API)
  → Exibe conversas e mensagens hardcoded
  → Simula envio de mensagem com delay de 1200ms
  → Gera resposta mock com metadados

[Backend: POST /api/v1/aigovernance/assistant/messages]
  → SendAssistantMessage handler
  → Cria/resolve conversação
  → Persiste mensagem do utilizador
  → Classifica use case por keywords
  → Seleciona estratégia de routing
  → Consulta knowledge sources
  → Gera resposta por templates internos (SEM LLM)
  → Persiste resposta e registo de auditoria
  → Retorna Response com 17+ campos de metadados
```

**Estado:** O backend funciona end-to-end **se a base de dados estiver criada**, mas a resposta é template, não IA real. O frontend **não chama o backend** — usa mock local.

### 7.2 Fluxo do AssistantPanel em Páginas de Detalhe (Parcial)

```
[Frontend: ContractDetailPage / ServiceDetailPage / etc.]
  → Renderiza AssistantPanel com contextType e contextData
  → Panel faz chamada a aiGovernanceApi.sendMessage()
  → Se API falhar → fallback para resposta mock contextual
  → Exibe resposta com grounding sources, metadados, suggested actions
```

**Estado:** O panel **tenta chamar a API real**, mas faz fallback para mock se falhar.

### 7.3 Fluxos de CRUD de Governance (Funcionais)

Os seguintes fluxos CRUD funcionam end-to-end via API (se DB existir):
- Registo, listagem, atualização de modelos AI
- Criação, listagem de políticas de acesso
- Gestão de budgets de tokens
- Registo de IDE clients
- Listagem de estratégias de routing
- Listagem de audit entries
- Gestão de knowledge sources
- Gestão de conversações e mensagens

---

## 8. Integrações Atuais com Outros Módulos do NexTraceOne

### 8.1 Isolamento do Módulo

O módulo `aiknowledge` está **corretamente isolado**:
- Depende apenas de `BuildingBlocks` (Infrastructure, Security, Application, Core)
- Nenhum outro módulo (Catalog, ChangeGovernance, etc.) importa `NexTraceOne.AIKnowledge`
- A comunicação prevista seria via eventos de domínio ou contratos de serviço (ambos TODO)

### 8.2 Integração Frontend via AssistantPanel

O `AssistantPanel` está integrado em 4 páginas de detalhe de outros módulos:

| Página | Módulo | Contexto |
|--------|--------|----------|
| ContractDetailPage | Catalog | contract — protocolo, versão, estado, violações |
| ServiceDetailPage | Catalog | service — metadata do serviço |
| IncidentDetailPage | Operations | incident — detalhes do incidente |
| ChangeDetailPage | Changes | change — metadata de release/change |

**Nível de integração:** O panel recebe dados de contexto da página host e os envia ao backend de IA. Não há acoplamento direto entre módulos backend.

### 8.3 Integrações Não Existentes

| Integração | Estado |
|-----------|--------|
| Catálogo de Serviços → IA | ❌ Não existe (AskCatalogQuestion é TODO) |
| Contratos OpenAPI/AsyncAPI → IA | ❌ Não existe |
| Spectral Linting → IA | ❌ Não existe |
| Entidades Canónicas → IA | ❌ Não existe |
| Documentação → IA | ❌ Não existe |
| Governance → IA | ❌ Não existe (módulos separados sem comunicação) |
| Portal → IA | ❌ Não existe |
| Studio → IA | ❌ Não existe |
| Visual Builders → IA | ❌ Não existe |

---

## 9. Problemas Arquiteturais e Técnicos Encontrados

### Criticidade: CRÍTICA

| # | Problema | Evidência | Impacto |
|---|---------|-----------|---------|
| 1 | **Nenhum SDK de IA instalado** — todo o módulo de IA opera sem capacidade real de inferência | Todos os .csproj: zero packages de IA | A funcionalidade core do módulo não existe |
| 2 | **DbContexts de IA não registados para migration** — as tabelas nunca são criadas na base de dados | `WebApplicationExtensions.cs:51-60` | O módulo não funciona em runtime real |
| 3 | **Seed data inexistente para IA** — sem dados iniciais para desenvolvimento | `DevelopmentSeedDataExtensions.cs:21-28` | Ambiente de dev incompleto |

### Criticidade: ALTA

| # | Problema | Evidência | Impacto |
|---|---------|-----------|---------|
| 4 | **16 features TODO** — ExternalAI (8) e Orchestration (8) são ficheiros vazios | `ExternalAI/Features/*/`, `Orchestration/Features/*/` | 36% dos features do módulo não existem |
| 5 | **IExternalAIRoutingPort sem implementação** — interface definida mas não implementada nem registada | `Ports/IExternalAIRoutingPort.cs` | Port pattern incompleto |
| 6 | **Frontend usa 100% mock data** — nenhuma página faz fetch real à API | `mockModels`, `mockPolicies`, `mockBudgets` em todas as páginas | UI desconectada do backend |
| 7 | **ExternalAiDbContext e AiOrchestrationDbContext vazios** — sem DbSet declarations | `ExternalAiDbContext.cs`, `AiOrchestrationDbContext.cs` | Persistência inexistente para 2 de 3 subdomínios |

### Criticidade: MÉDIA

| # | Problema | Evidência | Impacto |
|---|---------|-----------|---------|
| 8 | **ListKnowledgeSourceWeights e ListSuggestedPrompts** são handlers com dados hardcoded | Comentários: "Stub: configuração in-memory" | Dados não geridos dinamicamente |
| 9 | **EnrichContext gera contexto simulado** | Comentário: "Stub: retorna contexto simulado" | Enriquecimento de contexto não é real |
| 10 | **Interfaces de contrato inter-módulo vazias** (IExternalAiModule, IAiOrchestrationModule) | `Contracts/*/ServiceInterfaces/` | Comunicação entre módulos impossível |
| 11 | **Sem configuração de providers** em appsettings.json | appsettings*.json | Deploy impossível sem configuração manual |

### Criticidade: BAIXA

| # | Problema | Evidência | Impacto |
|---|---------|-----------|---------|
| 12 | **Respostas do SendAssistantMessage geradas por templates** em vez de LLM | Lógica de template no handler | Respostas são previsíveis e limitadas |
| 13 | **Modelos internos hardcoded** (NexTrace-Internal-v1, etc.) no PlanExecution handler | `PlanExecution.cs` | Nomes de modelos não configuráveis |

---

## 10. Avaliação do Estágio Atual

### Classificação: **Inicial-Parcial (Skeleton+)**

A implementação de IA no NexTraceOne encontra-se num estágio que supera um mero esqueleto — existe uma **vertical funcional no subdomínio Governance** com entidades, repositórios, endpoints e testes reais. Porém, falta o componente mais fundamental: **a capacidade de invocar um modelo de linguagem real**.

**Analogia técnica:** É como ter um sistema de e-commerce com catálogo de produtos, carrinho, checkout e gateway de pagamento — mas sem integração com nenhum banco para processar pagamentos.

**Decomposição:**

| Camada | Governance | ExternalAI | Orchestration |
|--------|-----------|-----------|--------------|
| Domain | ✅ 85% | ✅ 70% | ✅ 60% |
| Application | ✅ 80% | ❌ 0% | ❌ 0% |
| Infrastructure | ✅ 90% | ❌ 5% | ❌ 5% |
| API | ✅ 90% | ❌ 0% | ❌ 0% |
| Testes | ✅ 70% | ⚠️ 30% | ⚠️ 30% |
| LLM Integration | ❌ 0% | ❌ 0% | ❌ 0% |

---

## 11. Próximos Passos Recomendados

### Prioridade 1 — Fundação (pré-requisito para tudo)

1. **Registar DbContexts de IA no pipeline de migrations** (`WebApplicationExtensions.cs`)
2. **Criar seed data** para development (modelos, políticas, knowledge sources)
3. **Instalar SDK de IA** — recomendação: `Microsoft.SemanticKernel` (abstração de provider) + `Microsoft.Extensions.AI` (abstração oficial .NET)
4. **Configurar appsettings.json** com secção de AI providers (local e externo)

### Prioridade 2 — Provider Local Mínimo

5. **Implementar IExternalAIRoutingPort** com adaptador para Ollama (IA local)
6. **Adicionar docker-compose com Ollama** para desenvolvimento local
7. **Configurar modelo local** (Llama 3.x ou Mistral via Ollama)
8. **Substituir template response** no SendAssistantMessage por chamada real ao provider

### Prioridade 3 — Pipeline de IA Funcional

9. **Implementar prompt construction** com templates versionados
10. **Implementar pipeline de context enrichment** real (buscar dados dos módulos)
11. **Conectar frontend ao backend** — substituir mock data por chamadas API reais
12. **Implementar guardrails** básicos (max tokens, input sanitization)

### Prioridade 4 — Embedding e Knowledge

13. **Adicionar pgvector** ao PostgreSQL existente (ou vector store dedicado)
14. **Implementar embedding service** para indexação de contratos, serviços, documentação
15. **Implementar RAG pipeline** básico para grounding real
16. **Implementar ingestion de documentos**

### Prioridade 5 — ExternalAI e Orchestration Features

17. **Implementar features ExternalAI** (8 handlers TODO)
18. **Implementar features Orchestration** (8 handlers TODO)
19. **Mapear endpoints** de ExternalAI e Orchestration
20. **Completar DbContexts** com DbSets e DI registration

### Prioridade 6 — Observabilidade e Governança Avançada

21. **Integrar tracing de IA com OTel** existente
22. **Implementar circuit breaker** e fallback entre providers
23. **Implementar rate limiting** por policy
24. **Implementar auditoria completa** de prompts e respostas

---

## 12. Tabela Final de Status

| Componente | Status | Evidência | Observação |
|-----------|--------|-----------|-----------|
| **Módulo aiknowledge** | Parcial | `src/modules/aiknowledge/` (5 projectos) | Governance funcional, ExternalAI e Orchestration stub |
| **Domain Entities (Governance)** | ✅ Implementado | 14 entidades com factory methods, lifecycle, validação | Maduro e bem estruturado |
| **Domain Entities (ExternalAI)** | ✅ Implementado | 4 entidades (Provider, Policy, Consultation, KnowledgeCapture) | Entidades definidas, sem uso funcional |
| **Domain Entities (Orchestration)** | ✅ Implementado | 4 entidades (Context, Conversation, TestArtifact, KnowledgeEntry) | Entidades definidas, sem uso funcional |
| **Domain Enums** | ✅ Implementado | 15+ enums (ModelType, AIUseCaseType, AIRoutingPath, etc.) | Cobertura completa de tipos |
| **Domain Events** | ✅ Implementado | 3 eventos (ExternalAIQueryRequested, ResponseReceived, KnowledgeCandidate) | Definidos mas sem consumers |
| **Domain Ports** | Stub | IExternalAIRoutingPort — interface sem implementação | Port pattern incompleto |
| **Repository Abstractions** | ✅ Implementado | 11 interfaces em Governance/Abstractions/ | Bem definidas |
| **Repository Implementations** | ✅ Implementado | 11 repos com LINQ, filtragem, paginação | Apenas para Governance |
| **EF Core Configurations** | ✅ Implementado | 11 configs em Governance/Persistence/Configurations/ | Mapeamento completo |
| **AiGovernanceDbContext** | ✅ Implementado | 11 DbSets + IUnitOfWork | Completo |
| **ExternalAiDbContext** | Stub | Herda NexTraceDbContextBase, sem DbSets | TODO no código |
| **AiOrchestrationDbContext** | Stub | Herda NexTraceDbContextBase, sem DbSets | TODO no código |
| **DB Migrations Registration** | ❌ Não iniciado | WebApplicationExtensions.cs não inclui AI DbContexts | Tabelas nunca criadas |
| **Seed Data** | ❌ Não iniciado | DevelopmentSeedDataExtensions.cs sem AI targets | Dev data inexistente |
| **Governance Features (24/28)** | ✅ Implementado | CQRS handlers com repos, validação, lógica real | 24 reais, 2 mistos, 2 mock |
| **ExternalAI Features (0/8)** | Stub | 8 ficheiros com 5 TODO comments cada | 100% vazio |
| **Orchestration Features (0/8)** | Stub | 8 ficheiros com TODO comments | 100% vazio |
| **Governance Endpoints (27+)** | ✅ Implementado | REST endpoints com permissões, MediatR | Vertical completa |
| **ExternalAI Endpoints** | Stub | MapGroup sem endpoints mapeados | TODO no código |
| **Orchestration Endpoints** | Stub | MapGroup sem endpoints mapeados | TODO no código |
| **Governance DI** | ✅ Implementado | 11 repos + DbContext registados | Completo |
| **ExternalAI DI** | Stub | Return services vazio com TODO | Nenhum registo |
| **Orchestration DI** | Stub | Return services vazio com TODO | Nenhum registo |
| **Module Service Interfaces** | Stub | IExternalAiModule, IAiOrchestrationModule vazias | TODO no código |
| **SDK de IA (Semantic Kernel/OpenAI)** | ❌ Não iniciado | Zero packages NuGet de IA em todo o projecto | Capacidade core inexistente |
| **Provider Local (Ollama/LM Studio)** | ❌ Não iniciado | Sem container, config ou endpoint | Nenhuma infra |
| **Provider Externo (OpenAI/Azure)** | ❌ Não iniciado | Sem config, keys ou adaptador | Nenhuma infra |
| **Abstração Multi-Provider** | ❌ Não iniciado | Apenas IExternalAIRoutingPort vazio | Sem implementação |
| **Prompt Management** | ❌ Não iniciado | Sem templates, sem versionamento, sem gestão | Inexistente |
| **Prompt Templates** | ❌ Não iniciado | Templates inline no handler SendAssistantMessage | Não geridos |
| **Tool Calling / Function Calling** | ❌ Não iniciado | Nenhum mecanismo de tools para LLM | Inexistente |
| **Skill Registry** | ❌ Não iniciado | Nenhum registo de skills/capabilities para agentes | Inexistente |
| **Memory Management** | Parcial | Mensagens persistidas (AiMessage), sem semantic memory | Apenas persistência CRUD |
| **Vector Store / Embedding** | ❌ Não iniciado | Sem pgvector, Chroma, Weaviate ou similar | Inexistente |
| **RAG Pipeline** | ❌ Não iniciado | Sem retrieval, sem indexação, sem ranking | Inexistente |
| **Document Ingestion** | ❌ Não iniciado | Sem upload, parsing ou indexação de ficheiros | Inexistente |
| **Guardrails** | ❌ Não iniciado | Sem validação de input/output de IA | Inexistente |
| **Observabilidade de IA** | ❌ Não iniciado | Sem tracing/métricas de chamadas LLM (OTel existe para resto) | Inexistente |
| **Circuit Breaker / Fallback** | ❌ Não iniciado | Sem resilience patterns para providers | Inexistente |
| **Human-in-the-Loop** | ❌ Não iniciado | Sem workflow de aprovação para ações de IA | Inexistente |
| **Background Services de IA** | ❌ Não iniciado | Sem hosted services ou workers para IA | Inexistente |
| **Frontend: AI Hub (7 páginas)** | Parcial | UI funcional com dados 100% mock | Não conectado ao backend |
| **Frontend: AssistantPanel** | Parcial | Componente real com fallback mock | Tenta API, usa mock se falhar |
| **Frontend: API Service** | ✅ Implementado | 23 endpoints HTTP configurados | Pronto para conectar |
| **Frontend: i18n** | ✅ Implementado | 30+ chaves dedicadas à IA | Cobertura boa |
| **Frontend: Rotas e Sidebar** | ✅ Implementado | 5 rotas com permissões no sidebar | Navegação completa |
| **Frontend: Testes** | ✅ Implementado | 34 testes (9 page + 25 panel) | Cobertura adequada |
| **Backend: Testes** | ✅ Implementado | 41+ domain + 11 context bundle | Governance bem coberto |
| **Documentação** | ✅ Implementado | 4 docs dedicados (Governance, Architecture, DX, Operations) | Visão bem documentada |
| **Docker AI Infra** | ❌ Não iniciado | Sem container Ollama, vector DB ou embedding service | Inexistente |
| **appsettings AI Config** | ❌ Não iniciado | Sem secção de configuração de providers | Inexistente |
| **IDE Extensions** | ❌ Não iniciado | Apenas entidades de domínio (IDEClientRegistration) | Código de extensão inexistente |

---

## Conclusão Final (3 linhas)

1. **Estágio atual real:** O módulo de IA está em estágio **inicial-parcial** (~20-25% de maturidade) — existe um subdomínio Governance funcional com vertical completa (domain → repos → handlers → endpoints → testes), mas **zero capacidade real de inferência de IA** (nenhum SDK, nenhum provider, nenhum LLM).

2. **Principais blocos já existentes:** Domain model maduro (22 entidades, 15+ enums), 24 handlers MediatR reais com repositórios EF Core, 27+ endpoints REST com permissões, 7 páginas frontend com UI completa, 75+ testes (backend + frontend), e 4 documentos de arquitetura.

3. **Principal gargalo para continuar a evolução:** A **ausência total de integração com SDK de IA** (Semantic Kernel, OpenAI SDK) e de **infraestrutura de provider** (Ollama, Azure OpenAI) é o bloqueio fundamental — sem isto, o módulo é uma plataforma de governança de IA que não pode governar nenhuma IA.
