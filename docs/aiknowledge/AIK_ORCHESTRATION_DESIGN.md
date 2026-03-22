# AIKnowledge Orchestration — Design (Fase 2)

## Visão Geral

O módulo `AIKnowledge.Orchestration` coordena conversas multi-turno de IA, geração de artefatos de teste, validação de conhecimento e sumarização de releases. É o ponto de entrada para fluxos de IA ligados ao ciclo de vida de serviços e mudanças.

---

## Entidades Principais

### `AiConversation`
Conversa multi-turno de IA associada a uma release e serviço.
- Campos: `ReleaseId`, `ServiceName`, `Topic`, `TurnCount`, `Status`, `StartedBy`, `StartedAt`, `LastTurnAt`, `Summary`
- Criada via `AiConversation.Start(serviceName, topic, startedBy, startedAt, releaseId?)`

### `KnowledgeCaptureEntry`
Sugestão de entrada para base de conhecimento gerada durante conversa de IA.
- Campos: `ConversationId`, `Title`, `Content`, `Source`, `Relevance`, `Status`, `SuggestedAt`
- Criada via `KnowledgeCaptureEntry.Suggest(conversationId, title, content, source, relevance, suggestedAt)`
- Status: `Suggested → Approved | Rejected`

### `GeneratedTestArtifact`
Artefato de teste gerado por IA para uma release e serviço.
- Campos: `ReleaseId`, `ServiceName`, `TestFramework`, `Content`, `Status`, `Confidence`, `GeneratedAt`
- Criada via `GeneratedTestArtifact.Generate(releaseId, serviceName, testFramework, content, confidence, generatedAt)`

---

## Features (Handlers)

### `GetAiConversationHistory` (Query)
**Responsabilidade:** Recuperar histórico real de conversas com filtros e paginação.
**Dependência:** `IAiOrchestrationConversationRepository`
**Endpoint:** `GET /api/v1/aiorchestration/conversations/history`

### `ValidateKnowledgeCapture` (Command)
**Responsabilidade:** Validar se um `KnowledgeCaptureEntry` está apto para aprovação.
**Regras:**
- Título ≥ 10 caracteres
- Conteúdo ≥ 50 caracteres
- Source não vazio
- Relevância ≥ 0.30 (issue se < 0.30, warning se < 0.50)
- Status deve ser `Suggested`
- Verificação de duplicidade de título na mesma conversa (warning)

**Dependência:** `IKnowledgeCaptureEntryRepository`
**Endpoint:** `POST /api/v1/aiorchestration/knowledge/entries/{entryId}/validate`

### `GenerateTestScenarios` (Command)
**Responsabilidade:** Gerar cenários de teste estruturados via provider de IA.
**Fontes de input:** Spec textual, descrição de mudança, resumo de contrato (pelo menos uma obrigatória)
**Dependências:** `IExternalAIRoutingPort`, `IGeneratedTestArtifactRepository`
**Persistência:** Cria `GeneratedTestArtifact` se `releaseId` fornecido e provider responde
**Endpoint:** `POST /api/v1/aiorchestration/generate/test-scenarios`

### `GenerateRobotFrameworkDraft` (Command)
**Responsabilidade:** Gerar draft Robot Framework via provider de IA.
**Fontes de input:** Spec, EndpointDescription ou ContractSummary (pelo menos uma obrigatória)
**Dependências:** `IExternalAIRoutingPort`, `IGeneratedTestArtifactRepository`
**Persistência:** Cria `GeneratedTestArtifact` (framework="robot-framework") se `releaseId` fornecido
**Endpoint:** `POST /api/v1/aiorchestration/generate/robot-framework`

### `SummarizeReleaseForApproval` (Command)
**Responsabilidade:** Gerar resumo executivo/técnico de release para apoio à aprovação.
**Dados reais usados:**
- Até 5 conversas recentes via `IAiOrchestrationConversationRepository.GetRecentByReleaseAsync`
- Até 10 artefatos recentes via `IGeneratedTestArtifactRepository.GetRecentByReleaseAsync`
**Dependências:** `IExternalAIRoutingPort`, `IAiOrchestrationConversationRepository`, `IGeneratedTestArtifactRepository`
**Endpoint:** `POST /api/v1/aiorchestration/generate/releases/{releaseId}/approval-summary`

---

## Repositórios (Abstrações Application / Implementações Infrastructure)

```
IAiOrchestrationConversationRepository
  - ListHistoryAsync(releaseId?, serviceName?, topicFilter?, status?, from?, to?, page, pageSize, ct)
  - GetRecentByReleaseAsync(releaseId, maxCount, ct)

IKnowledgeCaptureEntryRepository
  - GetByIdAsync(id, ct)
  - HasDuplicateTitleInConversationAsync(conversationId, excludeId, title, ct)

IGeneratedTestArtifactRepository
  - AddAsync(artifact, ct)
  - GetRecentByReleaseAsync(releaseId, maxCount, ct)
```

Implementações: `AiOrchestrationRepositories.cs` em `NexTraceOne.AIKnowledge.Infrastructure/Orchestration/Persistence/Repositories/`

---

## Próximos Passos (Fase 3+)

1. Implementar `AiConversation.AddTurn()` para conversas multi-turno via endpoint dedicado
2. Adicionar `AiContextEnrichmentPort` para retrieval semântico real com embeddings
3. Indexar `GeneratedTestArtifact.Content` para busca full-text no histórico
4. Webhook/notificação quando `KnowledgeCaptureEntry` fica pronto para aprovação
