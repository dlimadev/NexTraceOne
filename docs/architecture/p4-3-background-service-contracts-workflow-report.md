# P4.3 — Background Service Contracts Workflow Implementation Report

**Data:** 2026-03-26  
**Fase:** P4.3 — Criar workflow real de Background Service Contracts no módulo Contracts  
**Estado:** CONCLUÍDO

---

## 1. Objetivo da Fase

Implementar o workflow real de Background Service Contracts no módulo Contracts do NexTraceOne.
Esta fase fecha o terceiro gap enterprise do módulo Contracts, depois de P4.1 (SOAP/WSDL) e P4.2 (AsyncAPI/Event).
O objetivo foi tornar o tipo contratual `BackgroundService` funcional com modelo próprio, persistência,
handlers e integração frontend — cobrindo jobs, workers, schedulers e processadores assíncronos internos.

---

## 2. Estado Antigo do Suporte a Background Service Contracts

| Componente | Estado anterior |
|---|---|
| `ContractType.BackgroundService` | Valor de enum declarado, sem comportamento próprio |
| `BackgroundServiceContractDetail` entity | **Inexistente** |
| `BackgroundServiceDraftMetadata` entity | **Inexistente** |
| `RegisterBackgroundServiceContract` handler | **Inexistente** |
| `CreateBackgroundServiceDraft` handler | **Inexistente** |
| `GetBackgroundServiceContractDetail` handler | **Inexistente** |
| Endpoints Background Service dedicados | **Inexistentes** |
| Representação de schedule/trigger/inputs/outputs | **Inexistente** |

**Gap central:** `ContractType.BackgroundService` era declarado mas não tinha comportamento — qualquer draft criado com este tipo seria tratado genericamente, sem captura de informação operacional relevante (schedule, trigger, side effects, inputs/outputs).

---

## 3. Entidades e Modelos Novos Introduzidos

### 3.1 `BackgroundServiceContractDetail` (Domain Entity)

**Ficheiro:** `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Entities/BackgroundServiceContractDetail.cs`

Entidade de primeiro plano para metadados de Background Service Contracts **publicados**.

| Propriedade | Tipo | Descrição |
|---|---|---|
| `ContractVersionId` | FK | Versão de contrato associada |
| `ServiceName` | string | Nome do processo (ex: "OrderExpirationJob") |
| `Category` | string | Job, Worker, Scheduler, Processor, Exporter, Notifier |
| `TriggerType` | string | Cron, Interval, EventTriggered, OnDemand, Continuous |
| `ScheduleExpression` | string? | Expressão cron/interval quando aplicável |
| `TimeoutExpression` | string? | Timeout máximo (ex: "PT30M") |
| `AllowsConcurrency` | bool | Suporte a execução paralela |
| `InputsJson` | string | JSON de inputs esperados |
| `OutputsJson` | string | JSON de outputs produzidos |
| `SideEffectsJson` | string | JSON de side effects declarados |

### 3.2 `BackgroundServiceDraftMetadata` (Domain Entity)

**Ficheiro:** `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Entities/BackgroundServiceDraftMetadata.cs`

Entidade para metadados de Background Service específicos de **drafts** em edição no Contract Studio.
Campos: ServiceName, Category, TriggerType, ScheduleExpression, InputsJson, OutputsJson, SideEffectsJson.

---

## 4. Novos Erros de Domínio

Adicionados em `ContractsErrors.cs`:

| Código | Descrição |
|---|---|
| `Contracts.BackgroundService.DetailNotFound` | Detalhe não encontrado para a versão |
| `Contracts.BackgroundService.DraftMetadataNotFound` | Metadado de draft não encontrado |
| `Contracts.BackgroundService.ServiceNameRequired` | ServiceName é obrigatório |

---

## 5. Alterações no ContractsDbContext

**Ficheiro:** `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/Persistence/ContractsDbContext.cs`

Adicionados 2 novos DbSets:

```csharp
public DbSet<BackgroundServiceContractDetail> BackgroundServiceContractDetails => Set<BackgroundServiceContractDetail>();
public DbSet<BackgroundServiceDraftMetadata> BackgroundServiceDraftMetadata => Set<BackgroundServiceDraftMetadata>();
```

**Novas tabelas:**
- `ctr_background_service_contract_details` — FK para `ctr_contract_versions`, check constraint em `trigger_type`
- `ctr_background_service_draft_metadata` — FK para `ctr_contract_drafts`

**Configurações EF Core:**
- `BackgroundServiceContractDetailConfiguration.cs` — check constraint `trigger_type IN ('Cron','Interval','EventTriggered','OnDemand','Continuous')`, índice único por `ContractVersionId`
- `BackgroundServiceDraftMetadataConfiguration.cs` — índice único por `ContractDraftId`

---

## 6. Handlers/Commands/Queries Criados

### 6.1 `RegisterBackgroundServiceContract` (Command + Handler)

**Ficheiro:** `Application/Contracts/Features/RegisterBackgroundServiceContract/RegisterBackgroundServiceContract.cs`  
**Endpoint:** `POST /api/v1/contracts/background-services/register`  
**Permissão:** `contracts:write`

Workflow em 3 passos:
1. Verifica unicidade da versão (`apiAssetId + semVer`)
2. Cria `ContractVersion`
3. Persiste `BackgroundServiceContractDetail` com metadados do processo

**Validação específica:** TriggerType deve ser um dos 5 valores válidos; ServiceName obrigatório.

### 6.2 `CreateBackgroundServiceDraft` (Command + Handler)

**Ficheiro:** `Application/Contracts/Features/CreateBackgroundServiceDraft/CreateBackgroundServiceDraft.cs`  
**Endpoint:** `POST /api/v1/contracts/drafts/background-service`  
**Permissão:** `contracts:write`

Cria draft com metadados do processo:
1. Cria `ContractDraft` com `ContractType=BackgroundService`
2. Cria `BackgroundServiceDraftMetadata`

### 6.3 `GetBackgroundServiceContractDetail` (Query + Handler)

**Ficheiro:** `Application/Contracts/Features/GetBackgroundServiceContractDetail/GetBackgroundServiceContractDetail.cs`  
**Endpoint:** `GET /api/v1/contracts/{contractVersionId}/background-service-detail`  
**Permissão:** `contracts:read`

---

## 7. Novas Abstrações (Repository Interfaces)

| Interface | Ficheiro |
|---|---|
| `IBackgroundServiceContractDetailRepository` | `Application/Contracts/Abstractions/IBackgroundServiceContractDetailRepository.cs` |
| `IBackgroundServiceDraftMetadataRepository` | `Application/Contracts/Abstractions/IBackgroundServiceDraftMetadataRepository.cs` |

**Implementações:**
| Repositório | Ficheiro |
|---|---|
| `BackgroundServiceContractDetailRepository` | `Infrastructure/.../Repositories/BackgroundServiceContractDetailRepository.cs` |
| `BackgroundServiceDraftMetadataRepository` | `Infrastructure/.../Repositories/BackgroundServiceDraftMetadataRepository.cs` |

---

## 8. Novos Endpoints REST

| Método | Rota | Handler | Permissão |
|---|---|---|---|
| `POST` | `/api/v1/contracts/background-services/register` | `RegisterBackgroundServiceContract` | `contracts:write` |
| `POST` | `/api/v1/contracts/drafts/background-service` | `CreateBackgroundServiceDraft` | `contracts:write` |
| `GET` | `/api/v1/contracts/{id}/background-service-detail` | `GetBackgroundServiceContractDetail` | `contracts:read` |

**Módulo:** `BackgroundServiceContractEndpointModule.cs` — auto-descoberto via assembly scanning.

---

## 9. Impacto no Frontend/Studio

### 9.1 Novos tipos TypeScript (`src/frontend/src/types/index.ts`)

```typescript
interface BackgroundServiceContractDetail     // Detalhe de background service de versão publicada
interface BackgroundServiceRegisterResponse   // Resposta do endpoint de registo
interface BackgroundServiceDraftCreateResponse // Resposta da criação de draft
```

### 9.2 Novas funções de API

```typescript
contractsApi.registerBackgroundService()                // POST /api/v1/contracts/background-services/register
contractsApi.getBackgroundServiceContractDetail()       // GET /api/v1/contracts/{id}/background-service-detail
contractStudioApi.createBackgroundServiceDraft()        // POST /api/v1/contracts/drafts/background-service
```

### 9.3 Novos hooks (`hooks/useBackgroundServiceWorkflow.ts`)

```typescript
useRegisterBackgroundService()                         // Mutation: registar background service contract
useCreateBackgroundServiceDraft()                      // Mutation: criar draft de background service
useBackgroundServiceContractDetail(versionId)          // Query: buscar detalhe de background service
```

### 9.4 `CreateServicePage.tsx` — Atualizada

Fluxo BackgroundService agora:
- Usa `contractStudioApi.createBackgroundServiceDraft()` em vez de `createDraft()` genérico
- Mostra campos BackgroundService específicos:
  - Service/Job Name (campo de texto)
  - Category (Job/Worker/Scheduler/Processor/Exporter/Notifier)
  - Trigger Type (OnDemand/Cron/Interval/EventTriggered/Continuous)
  - Schedule Expression (visível apenas quando Trigger = Cron ou Interval)

---

## 10. Ficheiros Criados/Alterados

### Ficheiros CRIADOS (17)

**Backend — Domain:**
- `Entities/BackgroundServiceContractDetail.cs`
- `Entities/BackgroundServiceDraftMetadata.cs`

**Backend — Infrastructure:**
- `Configurations/BackgroundServiceContractDetailConfiguration.cs`
- `Configurations/BackgroundServiceDraftMetadataConfiguration.cs`
- `Repositories/BackgroundServiceContractDetailRepository.cs`
- `Repositories/BackgroundServiceDraftMetadataRepository.cs`

**Backend — Application:**
- `Abstractions/IBackgroundServiceContractDetailRepository.cs`
- `Abstractions/IBackgroundServiceDraftMetadataRepository.cs`
- `Features/RegisterBackgroundServiceContract/RegisterBackgroundServiceContract.cs`
- `Features/CreateBackgroundServiceDraft/CreateBackgroundServiceDraft.cs`
- `Features/GetBackgroundServiceContractDetail/GetBackgroundServiceContractDetail.cs`

**Backend — API:**
- `Endpoints/BackgroundServiceContractEndpointModule.cs`

**Frontend:**
- `hooks/useBackgroundServiceWorkflow.ts`

**Tests:**
- `Domain/Entities/BackgroundServiceContractDetailTests.cs`
- `Domain/Entities/BackgroundServiceDraftMetadataTests.cs`
- `Application/Features/RegisterBackgroundServiceContractTests.cs`
- `Application/Features/CreateBackgroundServiceDraftTests.cs`
- `Application/Features/GetBackgroundServiceContractDetailTests.cs`

**Docs:**
- `docs/architecture/p4-3-background-service-contracts-workflow-report.md`
- `docs/architecture/p4-3-post-change-gap-report.md`

### Ficheiros ALTERADOS (9)

| Ficheiro | Alteração |
|---|---|
| `ContractsDbContext.cs` | +2 DbSets: BackgroundServiceContractDetails, BackgroundServiceDraftMetadata |
| `ContractsErrors.cs` | +3 erros Background Service específicos |
| `Application/Contracts/DependencyInjection.cs` | +3 validators + 2 usings |
| `Infrastructure/Contracts/DependencyInjection.cs` | +2 repositórios Background Service |
| `src/frontend/src/types/index.ts` | +3 tipos Background Service |
| `src/features/contracts/api/contracts.ts` | +2 funções: registerBackgroundService, getBackgroundServiceContractDetail |
| `src/features/contracts/api/contractStudio.ts` | +1 função: createBackgroundServiceDraft |
| `src/features/contracts/types/index.ts` | +3 exports de tipos Background Service |
| `src/features/contracts/hooks/index.ts` | +4 exports de hooks Background Service |
| `src/features/contracts/create/CreateServicePage.tsx` | BackgroundService-aware: campos Category, TriggerType, ScheduleExpression |

---

## 11. Validação

### Build
- `NexTraceOne.Catalog.API` (todos layers): ✅ 0 errors
- Frontend TypeScript (`tsc --noEmit`): ✅ 0 errors

### Tests
- **Antes (P4.2):** 558 testes passavam
- **Depois (P4.3):** 589 testes passam (**+31 novos testes**)
- 0 falhas, 0 regressões

| Ficheiro de teste | Novos testes |
|---|---|
| `BackgroundServiceContractDetailTests` | 8 |
| `BackgroundServiceDraftMetadataTests` | 6 |
| `RegisterBackgroundServiceContractTests` | 8 |
| `CreateBackgroundServiceDraftTests` | 6 |
| `GetBackgroundServiceContractDetailTests` | 5 |
| **Total** | **33** |
