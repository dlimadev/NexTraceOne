# NexTraceOne — ZR-3 AI Assistant full closure

## 1. Resumo executivo

A fase `ZR-3` fechou o fluxo produtivo do `AI Assistant` core em `/ai/assistant` com backend real, persistência coerente, reabertura/reload estável e testes reais cobrindo o round-trip principal.

O fluxo validado passou a ser:

`list conversations → open conversation → send → persist user message → persist assistant message → relist → reload → reopen → continue`

Principais resultados:

- `frontend` do assistant core ligado exclusivamente ao backend real
- `send` já não recria conversa silenciosamente quando recebe `conversationId` inexistente
- `list/open/messages` validam ownership do utilizador atual
- respostas degradadas continuam permitidas, mas agora são **explícitas**, **persistidas** e **coerentes** entre `send` e `reopen`
- conteúdo persistido degradado deixa de reaparecer com prefixo técnico cru na UI/API
- `contextReferences` e `groundingSources` persistem de forma coerente para `reload/reopen`
- suporte backend adicionado para `changeId`, alinhando o contrato com o frontend
- migration adicionada ao `AiGovernanceDbContext`

## 2. Inventário inicial do módulo

### Frontend core mapeado

- `src/frontend/src/features/ai-hub/pages/AiAssistantPage.tsx`
- `src/frontend/src/features/ai-hub/api/aiGovernance.ts`
- `src/frontend/src/__tests__/pages/AiAssistantPage.test.tsx`
- `src/frontend/e2e-real/real-core-flows.spec.ts`

### Backend mapeado

- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.API/Governance/Endpoints/Endpoints/AiGovernanceEndpointModule.cs`
- `CreateConversation`
- `ListConversations`
- `GetConversation`
- `ListMessages`
- `SendAssistantMessage`
- `AiAssistantConversation`
- `AiMessage`
- `AiGovernanceRepositories`
- `AiGovernanceDbContext`
- `ExternalAiRoutingPortAdapter`

### Estado inicial observado

- `AiAssistantPage` já estava em modo real para `list/create/send`, sem `mockConversations` ativos no core route
- backend já tinha endpoints reais para `conversations/messages/chat`
- gap principal estava na **coerência pós-send** e na **segurança/scoping**

## 3. Matriz página ↔ endpoint ↔ handler ↔ persistência

| Superfície | Endpoint | Handler | Persistência | Estado inicial | Fecho aplicado |
|---|---|---|---|---|---|
| `/ai/assistant` listagem | `GET /api/v1/ai/assistant/conversations` | `ListConversations` | `AiGovernanceDbContext.Conversations` | REAL/PARCIAL | ownership endurecido; ordenação estável |
| abrir conversa | `GET /api/v1/ai/assistant/conversations/{id}` | `GetConversation` | `AiGovernanceDbContext.Conversations` + `Messages` | REAL/PARCIAL | ownership validado; conteúdo sanitizado; frontend passou a usar este endpoint no open/reopen |
| listar mensagens | `GET /api/v1/ai/assistant/conversations/{id}/messages` | `ListMessages` | `AiGovernanceDbContext.Messages` | REAL/PARCIAL | ownership validado; conteúdo sanitizado; metadados persistidos coerentes |
| criar conversa | `POST /api/v1/ai/assistant/conversations` | `CreateConversation` | `AiGovernanceDbContext.Conversations` | REAL/PARCIAL | `changeId` alinhado ao frontend |
| enviar mensagem | `POST /api/v1/ai/assistant/chat` | `SendAssistantMessage` | `AiGovernanceDbContext.Conversations` + `Messages` + `UsageEntries` | PARCIAL | sem recriação silenciosa; persistência coerente; degraded state persistido e relistável |
| runtime/provider | `ExternalAiRoutingPortAdapter` | provider routing | provider real + fallback explícito | REAL/PARCIAL | fallback mantido como degradado explícito, nunca como fluxo fake silencioso |

## 4. Fallbacks/mocks encontrados

### No core route `/ai/assistant`

- não foram encontrados `mockConversations` ativos no fluxo principal
- não foram encontrados `mockMessages` ativos no fluxo principal
- o assistant core já usava backend real para `list/create/send`

### Gaps/fallbacks técnicos encontrados

1. `send` com provider degradado persistia o conteúdo com prefixo técnico cru `[FALLBACK_PROVIDER_UNAVAILABLE]`
2. metadata persistida de respostas degradadas divergiam de `send` para `list/reopen`
3. `conversationId` inexistente no `send` criava nova conversa silenciosamente
4. `GetConversation` e `ListMessages` não aplicavam validação de ownership
5. `ListConversations` aceitava `userId` arbitrário sem hardening suficiente
6. contrato backend não aceitava `changeId`, apesar de o frontend já o prever

## 5. Correções de backend

### 5.1 Send flow fechado

Em `SendAssistantMessage`:

- `conversationId` inexistente agora retorna `NotFound`
- conversa de outro utilizador agora retorna `Forbidden`
- `changeId` passou a fazer parte do comando
- `contextReferences` agora incluem também `scope:*` quando não há IDs explícitos
- `conversation.RecordMessage(...)` foi reposicionado para reduzir divergência entre contagem e persistência
- `assistantResponse` devolvido por `send` passa a sair sanitizado para UX/API

### 5.2 Persistência degradada coerente

Em `AiMessage`:

- criado `GetDisplayContent()` para expor conteúdo limpo
- `DegradedAssistantMessage(...)` passou a persistir:
  - `completionTokens`
  - `appliedPolicyName`
  - `groundingSources`
  - `contextReferences`

Resultado: `send` e `list/reopen` passaram a devolver o mesmo conteúdo funcional e metadados essenciais.

### 5.3 Ownership/scoping

Aplicado em:

- `ListConversations`
- `GetConversation`
- `ListMessages`
- `SendAssistantMessage`

Foi adicionado `AiGovernanceErrors.ConversationAccessDenied(...)` para bloquear acesso cruzado.

### 5.4 ChangeId end-to-end

Aplicado em:

- `AiAssistantConversation`
- `CreateConversation`
- `GetConversation`
- `SendAssistantMessage`
- migration nova do `AiGovernanceDbContext`

### 5.5 Repositórios

Em `AiGovernanceRepositories`:

- `GetByIdAsync` passou a usar query direta
- ordenação de conversas estabilizada por `LastMessageAt/CreatedAt`
- ordenação de mensagens estabilizada por `Timestamp/CreatedAt`

## 6. Correções de frontend

### 6.1 Open/reopen real pelo endpoint de detalhe

`AiAssistantPage` deixou de depender apenas de `listMessages` para abrir a thread selecionada.

Agora a página usa `GET /ai/assistant/conversations/{id}` para:

- abrir conversa real
- recarregar/reabrir conversa real
- sincronizar metadata da conversa na sidebar
- recuperar histórico persistido do mesmo shape usado no backend

### 6.2 Reload/reopen consistente

Após `send`, o fluxo passou a reler a conversa persistida e não apenas depender de estado local temporário.

### 6.3 Testes frontend ajustados

`AiAssistantPage.test.tsx` foi atualizado para validar:

- restauração da conversa pelo URL
- reopen com `getConversation`
- erro explícito sem fallback fake quando `send` falha

## 7. Fluxo end-to-end validado

Fluxo comprovado com testes reais:

1. entrar em `/ai/assistant`
2. criar conversa real
3. abrir conversa real
4. enviar mensagem real
5. persistir mensagem do utilizador
6. persistir resposta do assistant
7. relistar mensagens reais
8. recarregar a página
9. reabrir a mesma conversa
10. continuar a interação sem perder histórico

## 8. Testes reais criados/ajustados

### Integration / API host

Arquivo: `tests/platform/NexTraceOne.IntegrationTests/CriticalFlows/CoreApiHostIntegrationTests.cs`

Validações adicionadas/fortalecidas:

- `AI_Should_Create_Open_Send_Persist_Relist_And_Reopen_Conversation_With_Real_Backend`
- `AI_Should_Not_Silently_Create_A_New_Conversation_When_Send_Targets_Unknown_Id`
- `AI_Should_Enforce_User_Scoped_Conversation_Access_For_List_Open_Messages_And_Send`

### API E2E real

Arquivo: `tests/platform/NexTraceOne.E2E.Tests/Flows/RealBusinessApiFlowTests.cs`

Validações adicionadas/fortalecidas:

- `AI_Should_Create_Conversation_Send_Message_And_List_Persisted_Messages`
- `AI_Should_Create_Open_Send_Persist_Relist_And_Reopen_Conversation`

### Web E2E real

Arquivo: `src/frontend/e2e-real/real-core-flows.spec.ts`

Validação fortalecida para:

- criar conversa
- enviar primeira mensagem
- reload
- reabrir mesma conversa
- enviar segunda mensagem
- novo reload
- confirmar continuidade do histórico

### Frontend unit/integration

Arquivo: `src/frontend/src/__tests__/pages/AiAssistantPage.test.tsx`

Validações:

- restore via URL
- `getConversation` no reopen
- erro explícito sem resposta fake no failure path

### Resultado efetivamente executado nesta fase

Passaram com sucesso:

- assistant integration/API host: 3 testes
- assistant API E2E real: 2 testes
- assistant page frontend: 8 testes
- `frontend typecheck`
- `frontend e2e typecheck`

## 9. Estado de schema/migrations

### DbContext afetado

- `AiGovernanceDbContext`

### Migration criada

- `20260320165610_AddAiAssistantConversationChangeId`

### Snapshot atualizado

- `AiGovernanceDbContextModelSnapshot.cs`

### Observações

- não foi identificado drift novo no schema do assistant core
- `AiOrchestrationDbContext` e `ExternalAiDbContext` não exigiram mudança estrutural para o fecho do fluxo core

## 10. Ficheiros alterados nesta fase

- `src/frontend/src/features/ai-hub/pages/AiAssistantPage.tsx`
- `src/frontend/src/__tests__/pages/AiAssistantPage.test.tsx`
- `src/frontend/e2e-real/real-core-flows.spec.ts`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Features/CreateConversation/CreateConversation.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Features/GetConversation/GetConversation.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Features/ListConversations/ListConversations.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Features/ListMessages/ListMessages.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Application/Governance/Features/SendAssistantMessage/SendAssistantMessage.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Domain/Governance/Entities/AiAssistantConversation.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Domain/Governance/Entities/AiMessage.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Domain/Governance/Errors/AiGovernanceErrors.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Governance/Persistence/Repositories/AiGovernanceRepositories.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Governance/Persistence/Migrations/20260320165610_AddAiAssistantConversationChangeId.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Governance/Persistence/Migrations/20260320165610_AddAiAssistantConversationChangeId.Designer.cs`
- `src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure/Governance/Persistence/Migrations/AiGovernanceDbContextModelSnapshot.cs`
- `tests/platform/NexTraceOne.IntegrationTests/CriticalFlows/CoreApiHostIntegrationTests.cs`
- `tests/platform/NexTraceOne.E2E.Tests/Flows/RealBusinessApiFlowTests.cs`

## 11. Gaps remanescentes

### Assistant core (`/ai/assistant`)

Nenhum gap bloqueador remanescente foi identificado no fluxo produtivo core validado nesta fase.

### Observações fora do escopo direto da ZR-3 core

- testes baseados em Docker do `AiGovernancePostgreSqlTests` não puderam ser executados neste ambiente porque o Docker local estava indisponível
- existe um teste não relacionado ao assistant em `RealBusinessApiFlowTests` que continua fora desta fase
- componentes contextuais embutidos fora do route core não foram usados como prova de prontidão do assistant core

## 12. Veredicto final do módulo

## PRONTO

O `AI Assistant` core do NexTraceOne fica classificado como `PRONTO` para o escopo ZR-3 validado nesta execução.
