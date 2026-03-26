# P4.2 — Event Contracts / AsyncAPI Workflow Implementation Report

**Data:** 2026-03-26  
**Fase:** P4.2 — Criar workflow real de Event Contracts / AsyncAPI no módulo Contracts  
**Estado:** CONCLUÍDO

---

## 1. Objetivo da Fase

Implementar o workflow real de Event Contracts / AsyncAPI no módulo Contracts do NexTraceOne.
O objetivo foi transformar o protocolo `AsyncApi` de valor nominal (enum sem comportamento) em entidade
de primeiro plano, com modelo específico, persistência, handlers e integração frontend real —
paralelo ao que foi feito para SOAP/WSDL em P4.1.

---

## 2. Estado Antigo do Suporte a AsyncAPI

| Componente | Estado anterior |
|---|---|
| `ContractProtocol.AsyncApi` | Valor de enum declarado, sem comportamento próprio |
| `ContractType.Event` | Valor de enum declarado, sem entidade específica |
| `AsyncApiSpecParser` | Existia — extrai channels/operações (não usado em workflow) |
| `AsyncApiDiffCalculator` | Existia — calcula diffs semânticos (não integrado ao workflow) |
| `VisualEventBuilder.tsx` | Existia no frontend — builder visual sem endpoint dedicado |
| `EventContractDetail` entity | **Inexistente** |
| `EventDraftMetadata` entity | **Inexistente** |
| `AsyncApiMetadataExtractor` service | **Inexistente** |
| `ImportAsyncApiContract` handler | **Inexistente** |
| `CreateEventDraft` handler | **Inexistente** |
| `GetEventContractDetail` handler | **Inexistente** |
| Endpoints AsyncAPI dedicados | **Inexistentes** |

**Gap central:** O protocolo `AsyncApi` era declarado mas não tinha comportamento — qualquer spec AsyncAPI importada era tratada genericamente, sem extração de channels, mensagens, servidores ou versão AsyncAPI.

---

## 3. Entidades e Modelos Novos Introduzidos

### 3.1 `EventContractDetail` (Domain Entity)

**Ficheiro:** `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Entities/EventContractDetail.cs`

Entidade de primeiro plano para metadados AsyncAPI de versões de contrato **publicadas**.

| Propriedade | Tipo | Descrição |
|---|---|---|
| `ContractVersionId` | FK | Versão de contrato AsyncAPI associada |
| `Title` | string | Título do serviço event-driven (`info.title`) |
| `AsyncApiVersion` | string | Versão do protocolo AsyncAPI (ex: "2.6.0", "3.0.0") |
| `DefaultContentType` | string | Content type padrão das mensagens |
| `ChannelsJson` | string | JSON `{"channelName": ["PUBLISH", "SUBSCRIBE"], ...}` |
| `MessagesJson` | string | JSON `{"messageName": ["field1", "field2"], ...}` |
| `ServersJson` | string | JSON `{"serverName": "url", ...}` |

### 3.2 `EventDraftMetadata` (Domain Entity)

**Ficheiro:** `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Entities/EventDraftMetadata.cs`

Entidade para metadados AsyncAPI específicos de **drafts** em edição no Contract Studio.

| Propriedade | Tipo | Descrição |
|---|---|---|
| `ContractDraftId` | FK | Draft de contrato de evento associado |
| `Title` | string | Título do serviço |
| `AsyncApiVersion` | string | Versão AsyncAPI |
| `DefaultContentType` | string | Content type padrão |
| `ChannelsJson` | string | JSON de channels definidos pelo editor visual |
| `MessagesJson` | string | JSON de mensagens/schemas definidos |

### 3.3 `AsyncApiMetadataExtractor` (Domain Service)

**Ficheiro:** `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Services/AsyncApiMetadataExtractor.cs`

Novo serviço de domínio que complementa o `AsyncApiSpecParser` e `AsyncApiDiffCalculator` existentes.
Extrai metadados estruturados de uma spec AsyncAPI para popular `EventContractDetail`:

- Título (`info.title`)
- Versão AsyncAPI (`asyncapi` field)
- Default content type (`defaultContentType`)
- Channels com operações (via `AsyncApiSpecParser.ExtractChannelsAndOperations()`)
- Mensagens/schemas (`components.messages`)
- Servidores/brokers (`servers`)

---

## 4. Alterações no ContractsDbContext

**Ficheiro:** `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/Persistence/ContractsDbContext.cs`

Adicionados 2 novos DbSets:

```csharp
/// <summary>Detalhes AsyncAPI específicos de versões de contrato publicadas (Protocol = AsyncApi).</summary>
public DbSet<EventContractDetail> EventContractDetails => Set<EventContractDetail>();

/// <summary>Metadados AsyncAPI específicos de drafts de contrato em edição (ContractType = Event).</summary>
public DbSet<EventDraftMetadata> EventDraftMetadata => Set<EventDraftMetadata>();
```

**Novas tabelas:**
- `ctr_event_contract_details` — EventContractDetail com FK para `ctr_contract_versions`
- `ctr_event_draft_metadata` — EventDraftMetadata com FK para `ctr_contract_drafts`

**Configurações EF Core:**
- `EventContractDetailConfiguration.cs` — índice único por `ContractVersionId`, colunas text para JSON
- `EventDraftMetadataConfiguration.cs` — índice único por `ContractDraftId`, colunas text para JSON

---

## 5. Novos Erros de Domínio

Adicionados em `ContractsErrors.cs`:

| Código | Descrição |
|---|---|
| `Contracts.Event.DetailNotFound` | Detalhe AsyncAPI não encontrado para a versão |
| `Contracts.Event.DraftMetadataNotFound` | Metadado AsyncAPI de draft não encontrado |
| `Contracts.Event.InvalidAsyncApiContent` | Conteúdo não é AsyncAPI JSON válido |

---

## 6. Handlers/Commands/Queries Criados

### 6.1 `ImportAsyncApiContract` (Command + Handler)

**Ficheiro:** `Application/Contracts/Features/ImportAsyncApiContract/ImportAsyncApiContract.cs`  
**Endpoint:** `POST /api/v1/contracts/asyncapi/import`  
**Permissão:** `contracts:write`

Workflow em 4 passos:
1. Verifica unicidade da versão (`apiAssetId + semVer`)
2. Cria `ContractVersion` com `Protocol=AsyncApi`
3. Extrai metadados via `AsyncApiMetadataExtractor.Extract()`
4. Persiste `EventContractDetail` com channels, mensagens, servidores extraídos

**Validação específica:** JSON deve conter campo `"asyncapi"` para ser considerado válido.

### 6.2 `CreateEventDraft` (Command + Handler)

**Ficheiro:** `Application/Contracts/Features/CreateEventDraft/CreateEventDraft.cs`  
**Endpoint:** `POST /api/v1/contracts/drafts/event`  
**Permissão:** `contracts:write`

Cria draft de evento com metadados AsyncAPI:
1. Cria `ContractDraft` com `ContractType=Event` e `Protocol=AsyncApi`
2. Cria `EventDraftMetadata` com título, versão AsyncAPI, content type e channels/mensagens

### 6.3 `GetEventContractDetail` (Query + Handler)

**Ficheiro:** `Application/Contracts/Features/GetEventContractDetail/GetEventContractDetail.cs`  
**Endpoint:** `GET /api/v1/contracts/{contractVersionId}/event-detail`  
**Permissão:** `contracts:read`

Consulta os metadados AsyncAPI específicos de uma versão de contrato publicada.
Retorna erro `Contracts.Event.DetailNotFound` quando não encontrado.

---

## 7. Novas Abstrações (Repository Interfaces)

| Interface | Ficheiro |
|---|---|
| `IEventContractDetailRepository` | `Application/Contracts/Abstractions/IEventContractDetailRepository.cs` |
| `IEventDraftMetadataRepository` | `Application/Contracts/Abstractions/IEventDraftMetadataRepository.cs` |

**Implementações:**
| Repositório | Ficheiro |
|---|---|
| `EventContractDetailRepository` | `Infrastructure/Contracts/Persistence/Repositories/EventContractDetailRepository.cs` |
| `EventDraftMetadataRepository` | `Infrastructure/Contracts/Persistence/Repositories/EventDraftMetadataRepository.cs` |

---

## 8. Novos Endpoints REST

| Método | Rota | Handler | Permissão |
|---|---|---|---|
| `POST` | `/api/v1/contracts/asyncapi/import` | `ImportAsyncApiContract` | `contracts:write` |
| `POST` | `/api/v1/contracts/drafts/event` | `CreateEventDraft` | `contracts:write` |
| `GET` | `/api/v1/contracts/{id}/event-detail` | `GetEventContractDetail` | `contracts:read` |

**Módulo:** `EventContractEndpointModule.cs` — auto-descoberto via assembly scanning.

---

## 9. Impacto no Frontend/Studio

### 9.1 Novos tipos TypeScript (`src/frontend/src/types/index.ts`)

```typescript
interface EventContractDetail        // Detalhe AsyncAPI de versão publicada
interface AsyncApiImportResponse     // Resposta do endpoint de importação AsyncAPI
interface EventDraftCreateResponse   // Resposta da criação de draft de evento
```

### 9.2 Novas funções de API

```typescript
contractsApi.importAsyncApi()           // POST /api/v1/contracts/asyncapi/import
contractsApi.getEventContractDetail()   // GET /api/v1/contracts/{id}/event-detail
contractStudioApi.createEventDraft()    // POST /api/v1/contracts/drafts/event
```

### 9.3 Novos hooks (`hooks/useEventWorkflow.ts`)

```typescript
useAsyncApiImport()                     // Mutation: importar AsyncAPI spec
useCreateEventDraft()                   // Mutation: criar draft de evento
useEventContractDetail(versionId)       // Query: buscar detalhe de evento
```

### 9.4 `CreateServicePage.tsx` — Atualizada

Fluxo de criação de contratos Event agora:
- Usa `contractStudioApi.createEventDraft()` em vez de `createDraft()` genérico
- Mostra campos Event específicos (AsyncAPI Version, Default Content Type)
- No modo import, usa formato `json` e placeholder de AsyncAPI JSON
- Integra os metadados com o `EventDraftMetadata` persistido

---

## 10. Ficheiros Criados/Alterados

### Ficheiros CRIADOS (20)

**Backend — Domain:**
- `Entities/EventContractDetail.cs`
- `Entities/EventDraftMetadata.cs`
- `Services/AsyncApiMetadataExtractor.cs`

**Backend — Infrastructure:**
- `Configurations/EventContractDetailConfiguration.cs`
- `Configurations/EventDraftMetadataConfiguration.cs`
- `Repositories/EventContractDetailRepository.cs`
- `Repositories/EventDraftMetadataRepository.cs`

**Backend — Application:**
- `Abstractions/IEventContractDetailRepository.cs`
- `Abstractions/IEventDraftMetadataRepository.cs`
- `Features/ImportAsyncApiContract/ImportAsyncApiContract.cs`
- `Features/CreateEventDraft/CreateEventDraft.cs`
- `Features/GetEventContractDetail/GetEventContractDetail.cs`

**Backend — API:**
- `Endpoints/EventContractEndpointModule.cs`

**Frontend:**
- `hooks/useEventWorkflow.ts`

**Tests:**
- `Domain/Services/AsyncApiMetadataExtractorTests.cs`
- `Domain/Entities/EventContractDetailTests.cs`
- `Domain/Entities/EventDraftMetadataTests.cs`
- `Application/Features/ImportAsyncApiContractTests.cs`
- `Application/Features/CreateEventDraftTests.cs`
- `Application/Features/GetEventContractDetailTests.cs`

**Docs:**
- `docs/architecture/p4-2-event-contracts-asyncapi-workflow-report.md`
- `docs/architecture/p4-2-post-change-gap-report.md`

### Ficheiros ALTERADOS (10)

| Ficheiro | Alteração |
|---|---|
| `ContractsDbContext.cs` | +2 DbSets: EventContractDetails, EventDraftMetadata |
| `ContractsErrors.cs` | +3 erros AsyncAPI específicos |
| `Application/Contracts/DependencyInjection.cs` | +3 validators Event + 2 usings |
| `Infrastructure/Contracts/DependencyInjection.cs` | +2 repositórios Event |
| `src/frontend/src/types/index.ts` | +3 tipos AsyncAPI |
| `src/features/contracts/api/contracts.ts` | +2 funções: importAsyncApi, getEventContractDetail |
| `src/features/contracts/api/contractStudio.ts` | +1 função: createEventDraft |
| `src/features/contracts/types/index.ts` | +3 exports de tipos AsyncAPI |
| `src/features/contracts/hooks/index.ts` | +4 exports de hooks AsyncAPI |
| `src/features/contracts/create/CreateServicePage.tsx` | Fluxo Event-aware + campos AsyncAPI |

---

## 11. Validação

### Build
- `NexTraceOne.Catalog.Domain`: ✅ 0 errors
- `NexTraceOne.Catalog.Application`: ✅ 0 errors
- `NexTraceOne.Catalog.Infrastructure`: ✅ 0 errors
- `NexTraceOne.Catalog.API`: ✅ 0 errors
- Frontend TypeScript (`tsc --noEmit`): ✅ 0 errors

### Tests
- **Antes (P4.1):** 517 testes passavam
- **Depois (P4.2):** 558 testes passam (**+41 novos testes**)
- 0 falhas, 0 regressões

| Ficheiro de teste | Novos testes |
|---|---|
| `AsyncApiMetadataExtractorTests` | 10 |
| `EventContractDetailTests` | 8 |
| `EventDraftMetadataTests` | 7 |
| `ImportAsyncApiContractTests` | 8 |
| `CreateEventDraftTests` | 5 |
| `GetEventContractDetailTests` | 5 |
| **Total** | **43** |
