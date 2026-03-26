# P4.4 — Publication Center End-to-End Implementation Report

**Data:** 2026-03-26  
**Fase:** P4.4 — Completar o Publication Center end-to-end no módulo Contracts  
**Estado:** CONCLUÍDO

---

## 1. Objetivo da Fase

Fechar o workflow real de publicação de contratos no Developer Portal (Publication Center).
A auditoria identificava a capability como INCOMPLETE: o Developer Portal existia com catálogo, subscrições, playground e codegen, mas não havia fluxo explícito e governado de publicação de contratos — a visibilidade no portal era implícita, não controlada.

Esta fase adiciona o ciclo completo: publicar versão aprovada, retirar publicação, consultar estado e listar entradas do Publication Center.

---

## 2. Estado Antigo do Publication Center

| Componente | Estado anterior |
|---|---|
| `ContractPublicationEntry` entity | **Inexistente** |
| `ContractPublicationStatus` enum | **Inexistente** |
| `PublishContractToPortal` handler | **Inexistente** |
| `WithdrawContractFromPortal` handler | **Inexistente** |
| `GetPublicationCenterEntries` handler | **Inexistente** |
| `GetContractPublicationStatus` handler | **Inexistente** |
| Endpoints Publication Center | **Inexistentes** |
| Frontend: PublicationCenterPage | **Inexistente** |
| Botões "Publish to Portal" / "Withdraw from Portal" | **Inexistentes** no workspace |

**Situação pré-P4.4:**  
`PublishDraft` existia e convertia um ContractDraft aprovado em ContractVersion (operação interna do módulo Contracts). Porém, depois dessa operação, não havia qualquer controlo de se a versão estava ou não visível no Developer Portal. A visibilidade era implícita — qualquer versão que existisse no catálogo podia ser encontrada via search, sem governança de publicação.

---

## 3. Entidades e Modelos Introduzidos

### 3.1 `ContractPublicationStatus` enum

**Ficheiro:** `NexTraceOne.Catalog.Domain/Portal/Enums/ContractPublicationEnums.cs`

| Valor | Descrição |
|---|---|
| `PendingPublication` | Submetido para publicação, ainda não visível |
| `Published` | Visível no Developer Portal |
| `Withdrawn` | Removido sem deprecação formal; pode ser republicado |
| `Deprecated` | Visível mas marcado para remoção |

### 3.2 `PublicationVisibility` enum

**Ficheiro:** `NexTraceOne.Catalog.Domain/Portal/Enums/ContractPublicationEnums.cs`

| Valor | Descrição |
|---|---|
| `Internal` | Visível apenas para membros internos |
| `External` | Visível para consumidores externos autorizados |
| `RestrictedToTeams` | Visível apenas para equipas explicitamente autorizadas |

### 3.3 `ContractPublicationEntry` entity

**Ficheiro:** `NexTraceOne.Catalog.Domain/Portal/Entities/ContractPublicationEntry.cs`

Entidade que representa a entrada de publicação de um contrato no Developer Portal.
Separa explicitamente "versão aprovada internamente" (ContractVersion) de "contrato publicado no portal" (ContractPublicationEntry).

| Propriedade | Tipo | Descrição |
|---|---|---|
| `ContractVersionId` | Guid | Referência por valor ao ContractVersion (cross-module) |
| `ApiAssetId` | Guid | Referência por valor ao ApiAsset (cross-module) |
| `ContractTitle` | string | Título desnormalizado para query eficiente |
| `SemVer` | string | Versão semântica (ex: "2.1.0") |
| `Status` | ContractPublicationStatus | Estado atual de publicação |
| `Visibility` | PublicationVisibility | Escopo de visibilidade no portal |
| `PublishedBy` | string | Quem publicou |
| `PublishedAt` | DateTimeOffset? | Quando foi publicado |
| `ReleaseNotes` | string? | Changelog público (max 2000 chars) |
| `WithdrawnBy` | string? | Quem retirou a publicação |
| `WithdrawnAt` | DateTimeOffset? | Quando foi retirado |
| `WithdrawalReason` | string? | Motivo da retirada |

**Transições de estado:**
- `Create()` → `PendingPublication`
- `Publish(at)` → `Published` (de PendingPublication)
- `Withdraw(by, reason, at)` → `Withdrawn` (de Published)
- `MarkAsDeprecated()` → `Deprecated` (de Published ou Withdrawn)

---

## 4. Novos Erros de Domínio

Adicionados em `DeveloperPortalErrors.cs`:

| Código | Descrição |
|---|---|
| `DeveloperPortal.Publication.NotFound` | Entrada de publicação não encontrada |
| `DeveloperPortal.Publication.AlreadyExists` | Já existe publicação para esta versão |
| `DeveloperPortal.Publication.InvalidTransition` | Transição de estado inválida |
| `DeveloperPortal.Publication.ContractVersionNotFound` | Versão de contrato não encontrada |
| `DeveloperPortal.Publication.NotPublishable` | Versão não está em estado publicável |

---

## 5. Alterações no DeveloperPortalDbContext

**Ficheiro:** `NexTraceOne.Catalog.Infrastructure/Portal/Persistence/DeveloperPortalDbContext.cs`

Adicionado 1 novo DbSet:

```csharp
public DbSet<ContractPublicationEntry> ContractPublications => Set<ContractPublicationEntry>();
```

**Nova tabela:** `cat_portal_contract_publications`

**Configuração EF Core (`ContractPublicationEntryConfiguration.cs`):**
- Check constraint em `status IN ('PendingPublication', 'Published', 'Withdrawn', 'Deprecated')`
- Check constraint em `visibility IN ('Internal', 'External', 'RestrictedToTeams')`
- Índice único em `ContractVersionId` (1 versão → 1 publicação máxima)
- Índices em `ApiAssetId`, `Status`, `IsDeleted`

---

## 6. Handlers/Commands/Queries Criados

### 6.1 `PublishContractToPortal` (Command + Handler)

**Ficheiro:** `Application/Portal/Features/PublishContractToPortal/PublishContractToPortal.cs`  
**Endpoint:** `POST /api/v1/publication-center/publish`  
**Permissão:** `contracts:write`

**Workflow:**
1. Verifica unicidade (sem publicação duplicada para a versão)
2. Cria `ContractPublicationEntry` em `PendingPublication`
3. Transiciona imediatamente para `Published`
4. Persiste e confirma

**Validação:** `LifecycleState` deve ser `Approved` ou `Locked`.

### 6.2 `WithdrawContractFromPortal` (Command + Handler)

**Ficheiro:** `Application/Portal/Features/WithdrawContractFromPortal/WithdrawContractFromPortal.cs`  
**Endpoint:** `POST /api/v1/publication-center/{entryId}/withdraw`  
**Permissão:** `contracts:write`

### 6.3 `GetPublicationCenterEntries` (Query + Handler)

**Ficheiro:** `Application/Portal/Features/GetPublicationCenterEntries/GetPublicationCenterEntries.cs`  
**Endpoint:** `GET /api/v1/publication-center`  
**Permissão:** `developer-portal:read`

Suporta filtros: `status`, `apiAssetId`, paginação (`page`, `pageSize`).

### 6.4 `GetContractPublicationStatus` (Query + Handler)

**Ficheiro:** `Application/Portal/Features/GetContractPublicationStatus/GetContractPublicationStatus.cs`  
**Endpoint:** `GET /api/v1/publication-center/contracts/{contractVersionId}/status`  
**Permissão:** `developer-portal:read`

Retorna `NotPublished` quando não existe entrada. Permite que o workspace de contratos mostre badge de estado de publicação.

---

## 7. Nova Abstração (Repository Interface)

| Interface | Ficheiro |
|---|---|
| `IContractPublicationEntryRepository` | `Application/Portal/Abstractions/IContractPublicationEntryRepository.cs` |

**Implementação:**  
`ContractPublicationEntryRepository` — `Infrastructure/Portal/Persistence/Repositories/ContractPublicationEntryRepository.cs`

---

## 8. Novos Endpoints REST

| Método | Rota | Handler | Permissão |
|---|---|---|---|
| `POST` | `/api/v1/publication-center/publish` | `PublishContractToPortal` | `contracts:write` |
| `POST` | `/api/v1/publication-center/{entryId}/withdraw` | `WithdrawContractFromPortal` | `contracts:write` |
| `GET` | `/api/v1/publication-center` | `GetPublicationCenterEntries` | `developer-portal:read` |
| `GET` | `/api/v1/publication-center/contracts/{id}/status` | `GetContractPublicationStatus` | `developer-portal:read` |

**Módulo:** `PublicationCenterEndpointModule.cs` — auto-descoberto via assembly scanning.

---

## 9. Impacto no Frontend/Portal

### 9.1 Novos tipos TypeScript (`src/frontend/src/types/index.ts`)

```typescript
interface ContractPublicationEntry
type ContractPublicationStatus
type PublicationVisibility
interface PublishContractToPortalResponse
interface WithdrawContractFromPortalResponse
interface ContractPublicationStatusResponse
interface PublicationCenterListResponse
```

### 9.2 Nova API (`api/publicationCenter.ts`)

```typescript
publicationCenterApi.publishContract()         // POST /api/v1/publication-center/publish
publicationCenterApi.withdrawContract()        // POST /api/v1/publication-center/{id}/withdraw
publicationCenterApi.listPublications()        // GET  /api/v1/publication-center
publicationCenterApi.getPublicationStatus()    // GET  /api/v1/publication-center/contracts/{id}/status
```

### 9.3 Novos hooks (`hooks/usePublicationCenter.ts`)

```typescript
usePublishContractToPortal()         // Mutation: publica contrato no portal
useWithdrawContractFromPortal()      // Mutation: retira publicação do portal
usePublicationCenterEntries()        // Query: lista entradas do Publication Center
useContractPublicationStatus()       // Query: estado de publicação de uma versão
```

### 9.4 Nova página: `PublicationCenterPage.tsx`

Página de gestão com:
- Filtros por status (All / Published / Pending / Withdrawn / Deprecated)
- Tabela de entradas: contrato, versão, estado, visibilidade, publicado por, ações
- Ação "Withdraw" com modal de confirmação e campo de motivo opcional
- Status badge por tipo (Published/Pending/Withdrawn/Deprecated/NotPublished)

### 9.5 `ContractQuickActions.tsx` — Atualizado

Adicionados dois novos botões condicionais:
- **"Publish to Portal"** — visível quando `lifecycleState === 'Approved' || 'Locked'` e `!isPublishedToPortal`
- **"Withdraw from Portal"** — visível quando `isPublishedToPortal`

---

## 10. Ficheiros Criados/Alterados

### Ficheiros CRIADOS (14)

**Backend — Domain:**
- `Enums/ContractPublicationEnums.cs` (+ContractPublicationStatus, +PublicationVisibility)
- `Entities/ContractPublicationEntry.cs`

**Backend — Infrastructure:**
- `Configurations/ContractPublicationEntryConfiguration.cs`
- `Repositories/ContractPublicationEntryRepository.cs`

**Backend — Application:**
- `Abstractions/IContractPublicationEntryRepository.cs`
- `Features/PublishContractToPortal/PublishContractToPortal.cs`
- `Features/WithdrawContractFromPortal/WithdrawContractFromPortal.cs`
- `Features/GetPublicationCenterEntries/GetPublicationCenterEntries.cs`
- `Features/GetContractPublicationStatus/GetContractPublicationStatus.cs`

**Backend — API:**
- `Endpoints/Endpoints/PublicationCenterEndpointModule.cs`

**Frontend:**
- `api/publicationCenter.ts`
- `hooks/usePublicationCenter.ts`
- `publication/PublicationCenterPage.tsx`

**Tests:**
- `Portal/Domain/ContractPublicationEntryTests.cs`
- `Portal/Application/Features/PublicationCenterApplicationTests.cs`

**Docs:**
- `docs/architecture/p4-4-publication-center-report.md`
- `docs/architecture/p4-4-post-change-gap-report.md`

### Ficheiros ALTERADOS (7)

| Ficheiro | Alteração |
|---|---|
| `DeveloperPortalDbContext.cs` | +1 DbSet: ContractPublications |
| `DeveloperPortalErrors.cs` | +5 erros Publication Center |
| `Portal/DependencyInjection.cs` (Application) | +4 validators Publication Center |
| `Portal/DependencyInjection.cs` (Infrastructure) | +1 repositório: IContractPublicationEntryRepository |
| `src/frontend/src/types/index.ts` | +7 novos tipos Publication Center |
| `hooks/index.ts` | +5 exports Publication Center |
| `contracts/index.ts` | +PublicationCenterPage + publicationCenterApi + hooks exports |
| `shared/components/ContractQuickActions.tsx` | +isPublishedToPortal prop + Publish/Withdraw buttons |

---

## 11. Validação

### Build
- `NexTraceOne.Catalog.API` (todos layers): ✅ 0 errors
- Frontend TypeScript (`tsc --noEmit`): ✅ 0 errors

### Tests
- **Antes (P4.3):** 589 testes passavam
- **Depois (P4.4):** 607 testes passam (**+18 novos testes**)
- 0 falhas, 0 regressões

| Ficheiro de teste | Novos testes |
|---|---|
| `ContractPublicationEntryTests` | 8 |
| `PublicationCenterApplicationTests` | 10 |
| **Total** | **18** |
