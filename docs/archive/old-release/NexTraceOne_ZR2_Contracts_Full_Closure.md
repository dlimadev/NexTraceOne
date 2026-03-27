# NexTraceOne — ZR-2 Contracts Full Closure

**Data:** 2026-03-20  
**Escopo executado:** `Fase ZR-2 — Contracts full closure`

## 1. Resumo executivo

A ZR-2 foi executada com foco exclusivo no módulo `Contracts`.

O fechamento realizado nesta execução levou o escopo produtivo do módulo para o núcleo realmente sustentado por backend real e por testes reais:

- `Contract Catalog` → **ativo no release e ligado ao backend real**
- `Create draft / create contract flow` → **ativo no release e ligado ao backend real**
- `Draft Studio` → **ativo no release e ligado ao backend real**
- `Contract Workspace / detail` → **ativo no release e ligado ao backend real**
- `publish` → **corrigido para criar vínculo real com o catálogo e gerar versão semanticamente coerente**
- `portal / governance / canonical / spectral` → **removidos do escopo produtivo do módulo nesta fase**
- `AI generate draft` → **removido do fluxo produtivo por ser stub/template**
- secções fake do workspace → **removidas do fluxo produtivo**

Estado final revalidado nesta execução:

- `npm run build` em `src/frontend` → **sucesso**
- `dotnet build src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj -nologo` → **sucesso**
- teste unitário real de publish → **passou**
- teste de integração host real de create → edit → submit → approve → publish → detail → by-service → **passou**
- teste E2E HTTP real de create → edit → save → reload → approve → publish → detail → **passou**

## 2. Inventário inicial do módulo

### Frontend identificado

#### Rotas existentes antes da ZR-2
- `/contracts`
- `/contracts/new`
- `/contracts/studio/:draftId`
- `/contracts/:contractVersionId`
- `/contracts/:contractVersionId/portal`
- `/contracts/governance`
- `/contracts/spectral`
- `/contracts/canonical`

#### Superfícies reais mantidas
- `ContractCatalogPage`
- `CreateServicePage`
- `DraftStudioPage`
- `ContractWorkspacePage`

#### Superfícies retiradas do escopo produtivo
- `ContractPortalPage`
- `ContractGovernancePage`
- `SpectralRulesetManagerPage`
- `CanonicalEntityCatalogPage`

#### API clients / hooks relevantes
- `src/frontend/src/features/contracts/api/contracts.ts`
- `src/frontend/src/features/contracts/api/contractStudio.ts`
- `src/frontend/src/features/contracts/hooks/useDraftWorkflow.ts`
- `src/frontend/src/features/contracts/hooks/useContractDetail.ts`
- `src/frontend/src/features/contracts/hooks/useContractList.ts`

#### Tipos e view-models relevantes
- `src/frontend/src/features/contracts/types/index.ts`
- `src/frontend/src/features/contracts/types/workspace.ts`
- `src/frontend/src/features/contracts/workspace/studioTypes.ts`
- `src/frontend/src/features/contracts/workspace/toStudioContract.ts`
- `src/frontend/src/types/index.ts`

#### Loading / error / empty states reais mantidos
- catálogo: loading, erro e empty state honestos
- studio: loading, erro e sucesso/erro de save reais
- workspace: loading/erro reais por query
- compliance/validation: empty states honestos sem fallback enganoso

### Backend identificado

#### Endpoint modules
- `ContractsEndpointModule`
- `ContractStudioEndpointModule`

#### Endpoints reais usados no escopo final
- `GET /api/v1/contracts/list`
- `GET /api/v1/contracts/summary`
- `GET /api/v1/contracts/by-service/{serviceId}`
- `GET /api/v1/contracts/{contractVersionId}/detail`
- `GET /api/v1/contracts/history/{apiAssetId}`
- `GET /api/v1/contracts/{contractVersionId}/violations`
- `GET /api/v1/contracts/{contractVersionId}/validate`
- `POST /api/v1/contracts/drafts`
- `GET /api/v1/contracts/drafts/{draftId}`
- `GET /api/v1/contracts/drafts`
- `PATCH /api/v1/contracts/drafts/{draftId}/content`
- `PATCH /api/v1/contracts/drafts/{draftId}/metadata`
- `POST /api/v1/contracts/drafts/{draftId}/submit-review`
- `POST /api/v1/contracts/drafts/{draftId}/approve`
- `POST /api/v1/contracts/drafts/{draftId}/reject`
- `GET /api/v1/contracts/drafts/{draftId}/reviews`
- `POST /api/v1/contracts/drafts/{draftId}/publish`

#### Persistência relevante
- `ContractsDbContext`
- `ContractVersionRepository`
- `ContractDraftRepository`
- `CatalogGraphDbContext`
- `ApiAssetRepository`
- `IServiceAssetRepository`

#### Handlers centrais tocados
- `PublishDraft`
- `ListContracts`
- `GetContractVersionDetail`
- `CreateDraft`
- `GetDraft`
- `UpdateDraftContent`
- `UpdateDraftMetadata`

## 3. Matriz página ↔ endpoint ↔ handler ↔ persistência

| Superfície | Endpoint | Handler | Persistência | Estado final |
|---|---|---|---|---|
| Contract Catalog | `GET /contracts/list` | `ListContracts` | `ContractVersionRepository` + `ApiAssetRepository` | REAL |
| Contract Catalog summary | `GET /contracts/summary` | `GetContractsSummary` | `ContractVersionRepository` | REAL |
| Create draft | `POST /contracts/drafts` | `CreateDraft` | `ContractDraftRepository` + `ContractsDbContext` | REAL |
| Get draft | `GET /contracts/drafts/{id}` | `GetDraft` | `ContractDraftRepository` | REAL |
| Update draft content | `PATCH /contracts/drafts/{id}/content` | `UpdateDraftContent` | `ContractDraftRepository` + `ContractsDbContext` | REAL |
| Update draft metadata | `PATCH /contracts/drafts/{id}/metadata` | `UpdateDraftMetadata` | `ContractDraftRepository` + `ContractsDbContext` | REAL |
| Submit review | `POST /contracts/drafts/{id}/submit-review` | `SubmitDraftForReview` | `ContractDraftRepository` + `ContractsDbContext` | REAL |
| Approve draft | `POST /contracts/drafts/{id}/approve` | `ApproveDraft` | `ContractDraftRepository` + `ContractReviewRepository` | REAL |
| Publish draft | `POST /contracts/drafts/{id}/publish` | `PublishDraft` | `ContractsDbContext` + `CatalogGraphDbContext` | REAL |
| Workspace/detail | `GET /contracts/{id}/detail` | `GetContractVersionDetail` | `ContractVersionRepository` + `ApiAssetRepository` | REAL |
| Versioning | `GET /contracts/history/{apiAssetId}` | `GetContractHistory` | `ContractVersionRepository` | REAL |
| Compliance / violations | `GET /contracts/{id}/violations` | `ListRuleViolations` | `ContractVersionRepository` | REAL |
| Structural integrity | `GET /contracts/{id}/validate` | `ValidateContractIntegrity` | `ContractVersionRepository` | REAL |
| Contracts by service | `GET /contracts/by-service/{serviceId}` | `ListContractsByService` | `ApiAssetRepository` + `ContractVersionRepository` | REAL |
| Portal | removido da rota produtiva | — | — | REMOVIDO DO ESCOPO |
| Governance page | removido da rota produtiva | — | — | REMOVIDO DO ESCOPO |
| Canonical / Spectral | removidos da rota produtiva | — | — | REMOVIDOS DO ESCOPO |

## 4. Mocks / enrich fake encontrados

### Não encontrado no fluxo produtivo atual
- `studioMock.ts` **não foi encontrado na árvore atual do módulo**

### Encontrados e tratados
- `GenerateDraftFromAi` no backend era **stub/template**
- modo `AI` na `CreateServicePage`
- modos `template`, `clone` e `source` na `CreateServicePage` sem backend real correspondente
- `UseCasesSection` com `MOCK_USE_CASES`
- `GlossarySection` com `MOCK_TERMS`
- `InteractionsSection` com `MOCK_INTERACTIONS`
- `AuditSection` com `MOCK_AUDIT`
- `approvalChecklist` do workspace era derivado artificialmente só de lifecycle
- `Contracts` permanecia bloqueado por `ReleaseScopeGate`, então a rota existia mas não entregava o módulo real

## 5. Correções de backend

- removido o endpoint produtivo `POST /api/v1/contracts/drafts/generate`
- criado `ICatalogGraphUnitOfWork` para persistência real do vínculo com `Catalog Graph`
- `PublishDraft` passou a:
  - exigir vínculo real com catálogo
  - resolver corretamente `ApiAsset` existente
  - aceitar `ServiceAssetId` no draft e criar/reusar `ApiAsset` real sob o serviço
  - deixar de usar vínculo aleatório/quebrado
  - promover a nova `ContractVersion` para `Approved` no momento do publish, em coerência com o draft aprovado
- `ContractVersionRepository.ListByApiAssetIdsAsync` corrigido para eliminar falha EF no endpoint `by-service`
- adicionados erros honestos:
  - `Contracts.Draft.MissingCatalogLink`
  - `Contracts.Catalog.LinkNotFound`

## 6. Correções de frontend

- `Contracts` saiu do `ReleaseScopeGate` e passou a usar as páginas reais no `App`
- `releaseScope.ts` atualizado para incluir `/contracts` no escopo final
- `CreateServicePage` reduzida aos modos reais:
  - `visual`
  - `import`
- `CreateServicePage` passou a ligar draft a serviço real via `serviceCatalogApi.listServices()`
- `DraftStudioPage` passou a editar o vínculo real com serviço no separador metadata
- `ContractWorkspacePage` foi limpo para manter apenas secções suportadas
- `ContractQuickActions` deixou de expor portal fora do escopo produtivo
- alinhados os shared DTOs frontend para o shape real do backend:
  - catálogo
  - summary
  - integrity
  - violations
- `ComplianceSection` alinhada ao payload real de integridade/violations
- `Catalog` normalizado com `CatalogItem` coerente ao payload real

## 7. Fluxo end-to-end validado

Fluxo central validado após as correções:

1. entrar em `/contracts`
2. abrir catálogo real
3. criar novo draft em `/contracts/new`
4. redirecionar para `/contracts/studio/{draftId}`
5. editar conteúdo do spec
6. editar metadata
7. ligar draft a serviço real do catálogo
8. salvar
9. recarregar o draft
10. reabrir o draft sem perda de dados
11. submeter para review
12. aprovar
13. publicar
14. abrir o detail/workspace da versão publicada
15. validar persistência e join real com catálogo

Resultado final do fluxo: **sólido e persistente**

## 8. Testes reais criados / ajustados

### Unit / module
- `NexTraceOne.Catalog.Tests.Contracts.Application.Features.ContractStudioApplicationTests.PublishDraft_Should_CreateContractVersion_When_DraftIsApproved`
  - ajustado para o publish com vínculo real a `ServiceAsset`

### Integration host real
- `NexTraceOne.IntegrationTests.CriticalFlows.CoreApiHostIntegrationTests.Contracts_Should_Create_Update_Submit_Approve_Publish_And_Reopen_With_Real_Backend`
  - create draft
  - update content
  - update metadata
  - reload/reopen
  - approve
  - publish
  - detail real
  - by-service real

### E2E HTTP real
- `NexTraceOne.E2E.Tests.Flows.RealBusinessApiFlowTests.Contracts_Should_Create_Edit_Save_Reload_Approve_Publish_And_Open_Real_Detail`
  - create
  - edit
  - save
  - reload
  - approve
  - publish
  - detail real

### E2E web real
- `src/frontend/e2e-real/real-core-flows.spec.ts`
  - catálogo real
  - create draft
  - save spec
  - metadata
  - reload/reopen
  - publish via backend real
  - open workspace real

## 9. Estado de schema / migrations

- `ContractsDbContext` mantido consistente
- `CatalogGraphDbContext` passou a participar explicitamente no publish real do módulo
- nenhuma migration nova foi necessária para o fechamento da ZR-2
- não foi identificado drift de schema específico do módulo durante esta execução

## 10. Ficheiros alterados

### Backend
- `src/modules/catalog/NexTraceOne.Catalog.Application/Graph/Abstractions/ICatalogGraphUnitOfWork.cs`
- `src/modules/catalog/NexTraceOne.Catalog.Application/Contracts/Features/PublishDraft/PublishDraft.cs`
- `src/modules/catalog/NexTraceOne.Catalog.API/Contracts/Endpoints/Endpoints/ContractStudioEndpointModule.cs`
- `src/modules/catalog/NexTraceOne.Catalog.Domain/Contracts/Errors/ContractsErrors.cs`
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Graph/Persistence/CatalogGraphDbContext.cs`
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Graph/DependencyInjection.cs`
- `src/modules/catalog/NexTraceOne.Catalog.Infrastructure/Contracts/Persistence/Repositories/ContractVersionRepository.cs`

### Frontend
- `src/frontend/src/App.tsx`
- `src/frontend/src/releaseScope.ts`
- `src/frontend/src/features/contracts/index.ts`
- `src/frontend/src/features/contracts/api/contractStudio.ts`
- `src/frontend/src/features/contracts/hooks/index.ts`
- `src/frontend/src/features/contracts/hooks/useDraftWorkflow.ts`
- `src/frontend/src/features/contracts/create/CreateServicePage.tsx`
- `src/frontend/src/features/contracts/studio/DraftStudioPage.tsx`
- `src/frontend/src/features/contracts/types/index.ts`
- `src/frontend/src/features/contracts/types/workspace.ts`
- `src/frontend/src/features/contracts/shared/constants.ts`
- `src/frontend/src/features/contracts/catalog/types.ts`
- `src/frontend/src/features/contracts/catalog/components/CatalogTable.tsx`
- `src/frontend/src/features/contracts/workspace/ContractWorkspacePage.tsx`
- `src/frontend/src/features/contracts/workspace/WorkspaceLayout.tsx`
- `src/frontend/src/features/contracts/workspace/studioTypes.ts`
- `src/frontend/src/features/contracts/workspace/toStudioContract.ts`
- `src/frontend/src/features/contracts/workspace/sections/index.ts`
- `src/frontend/src/features/contracts/workspace/sections/ComplianceSection.tsx`
- `src/frontend/src/features/contracts/shared/components/ContractQuickActions.tsx`
- `src/frontend/src/types/index.ts`
- `src/frontend/e2e-real/real-core-flows.spec.ts`

### Testes
- `tests/modules/catalog/NexTraceOne.Catalog.Tests/Contracts/Application/Features/ContractStudioApplicationTests.cs`
- `tests/platform/NexTraceOne.IntegrationTests/CriticalFlows/CoreApiHostIntegrationTests.cs`
- `tests/platform/NexTraceOne.E2E.Tests/Flows/RealBusinessApiFlowTests.cs`

### Removidos do fluxo produtivo
- `src/frontend/src/features/contracts/workspace/sections/GlossarySection.tsx`
- `src/frontend/src/features/contracts/workspace/sections/UseCasesSection.tsx`
- `src/frontend/src/features/contracts/workspace/sections/InteractionsSection.tsx`
- `src/frontend/src/features/contracts/workspace/sections/AuditSection.tsx`

## 11. Gaps remanescentes

### Nenhum gap bloqueador no escopo produtivo final do módulo

Observações honestas:
- `portal`, `governance`, `canonical` e `spectral` não foram declarados prontos; foram retirados do escopo produtivo do módulo nesta fase
- warnings de analyzer e warnings EF de query splitting continuam a existir no backend, mas não bloquearam build nem o fluxo real do módulo
- o `run_build` agregado da workspace continua poluído por artefactos temporários externos (`CopilotBaseline`) e por erros fora do escopo da ZR-2; a validação honesta desta fase foi feita por build direto do frontend, build direto do `ApiHost` e testes reais do módulo

## 12. Veredicto final do módulo

**PRONTO**
