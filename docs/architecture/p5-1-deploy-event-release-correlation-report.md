# P5.1 — Deploy Event → Release Correlation: Relatório de Execução

**Data:** 2026-03-26  
**Fase:** P5.1 — Change Governance: Correlação Automática Deploy Event → Release  
**Estado:** CONCLUÍDO

---

## 1. Objetivo

Implementar o pipeline automático de correlação entre um evento de deploy recebido externamente
(via Ingestion API) e a entidade `Release` no módulo Change Governance, tornando o processo
rastreável, auditável e independente de criação manual.

---

## 2. Estado Anterior

### Fluxo antes de P5.1

- **`NotifyDeployment` handler**: Sempre criava uma nova `Release` para cada evento, sem deduplicação
  por `ServiceName + Version + Environment`. Não registava `ChangeEvent` nem `ExternalMarker`.
- **Ingestion API `/api/v1/deployments/events`**: Recebia eventos CI/CD e apenas rastreava metadados
  de integração (connector, source, execution). Retornava a nota explícita:
  _"Payload processing into domain entities is planned for a future release."_
  Não existia qualquer ligação ao módulo Change Governance.
- **`ChangeEvent`** e **`ExternalMarker`**: Entidades existentes mas não utilizadas no fluxo de
  notificação de deploy.

### Gap identificado

```
Correlação automática deploy → release → telemetria não verificada end-to-end.
```

A capability **Change Intelligence** continuava PARTIAL porque o pipeline automático de deploy
event → Release não estava fechado.

---

## 3. Modelo de Correlação Adotado

### Chave de correlação

O deploy event é correlacionado a uma `Release` existente usando a combinação:

| Campo           | Origem no deploy event      | Papel                                |
|-----------------|-----------------------------|--------------------------------------|
| `ServiceName`   | `DeploymentEventRequest.ServiceName` | Identificação do serviço       |
| `Version`       | `DeploymentEventRequest.Version`     | Versão/release semver          |
| `Environment`   | `DeploymentEventRequest.Environment` | Ambiente alvo                  |

### Semântica de idempotência

- **Release já existe** → o evento enriquece a release com um `ChangeEvent(deploy_notified)` e cria um
  novo `ExternalMarker(DeploymentStarted)`. A release não é recriada.
- **Release não existe** → é criada automaticamente. O `ApiAssetId` fica como `Guid.Empty` quando não
  fornecido (evento externo sem mapeamento de catálogo). Um `ChangeEvent(deploy_created)` e um
  `ExternalMarker(DeploymentStarted)` são registados.

### Rastreabilidade via ExternalMarker

Cada evento de deploy cria um `ExternalMarker` com:
- `MarkerType.DeploymentStarted`
- `SourceSystem` = provider do pipeline (ex: `github-actions`, `Jenkins`)
- `ExternalId` = ID externo do deploy ou correlation ID
- `OccurredAt` / `ReceivedAt` = UTC at time of ingestion

---

## 4. Ficheiros Alterados

### Domínio

| Ficheiro | Alteração |
|----------|-----------|
| `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Domain/ChangeIntelligence/Entities/Release.cs` | Removido `Guard.Against.Default(apiAssetId)` do factory `Create()`. Permite `Guid.Empty` para eventos externos não correlacionados ao catálogo. Comentário XML actualizado. |

### Application (contratos e handlers)

| Ficheiro | Alteração |
|----------|-----------|
| `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Application/ChangeIntelligence/Abstractions/IReleaseRepository.cs` | Adicionado `GetByServiceNameVersionEnvironmentAsync(string, string, string, CancellationToken)` para lookup de deduplicação. |
| `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Application/ChangeIntelligence/Features/NotifyDeployment/NotifyDeployment.cs` | Reescrito: `Command.ApiAssetId` tornado `Guid?` (opcional); handler expandido para receber `IChangeEventRepository` e `IExternalMarkerRepository`; lógica de deduplicação por `ServiceName+Version+Environment`; auto-criação de `ChangeEvent` e `ExternalMarker`; `Response` alargado com `IsNewRelease` e `ExternalMarkerId`. |

### Infraestrutura

| Ficheiro | Alteração |
|----------|-----------|
| `src/modules/changegovernance/NexTraceOne.ChangeGovernance.Infrastructure/ChangeIntelligence/Persistence/Repositories/ReleaseRepository.cs` | Implementado `GetByServiceNameVersionEnvironmentAsync`. |

### Ingestion API

| Ficheiro | Alteração |
|----------|-----------|
| `src/platform/NexTraceOne.Ingestion.Api/NexTraceOne.Ingestion.Api.csproj` | Já continha referência a `NexTraceOne.ChangeGovernance.API`. |
| `src/platform/NexTraceOne.Ingestion.Api/Program.cs` | Adicionado `using MediatR`, `using NexTraceOne.ChangeGovernance.API.ChangeIntelligence.Endpoints`, alias `NotifyDeploymentFeature`. Adicionado `AddChangeIntelligenceModule(configuration)` ao DI. Endpoint `/api/v1/deployments/events` actualizado: `ISender sender` + `ILogger<Program>` adicionados; após commit de metadados de integração, dispatch de `NotifyDeployment.Command` via MediatR; resposta actualizada com `releaseId`, `isNewRelease`, `processingStatus: "release_correlated"`. |

### Testes

| Ficheiro | Alteração |
|----------|-----------|
| `tests/modules/changegovernance/NexTraceOne.ChangeGovernance.Tests/ChangeIntelligence/Application/Features/ChangeIntelligenceApplicationTests.cs` | Teste existente `NotifyDeployment_Should_CreateRelease_AndReturnResponse` actualizado para nova assinatura do handler. 2 novos testes adicionados: `NotifyDeployment_Should_EnrichExistingRelease_WhenSameServiceVersionEnvironment` e `NotifyDeployment_Should_RecordExternalMarker_WithDeploymentStartedType`. |

---

## 5. Entidades Utilizadas

| Entidade | Papel |
|----------|-------|
| `Release` | Aggregate root. Criada ou enriquecida pelo deploy event. |
| `ChangeEvent` | Registo de auditoria na timeline da release. Criado a cada deploy event recebido. |
| `ExternalMarker` | Rastreabilidade do pipeline CI/CD. Criado com `MarkerType.DeploymentStarted` em cada deploy event. |

---

## 6. Pipeline Deploy → Release (pós-P5.1)

```
CI/CD Pipeline (GitHub/GitLab/Jenkins/AzDo)
    │
    └─► POST /api/v1/deployments/events   (Ingestion API)
            │
            ├─ Regista IntegrationConnector + IngestionSource + IngestionExecution
            │
            └─► NotifyDeployment.Command   (via MediatR → ChangeGovernance)
                    │
                    ├─ Lookup: ServiceName + Version + Environment
                    │
                    ├─ SE existir Release:
                    │     └─ Cria ChangeEvent(deploy_notified)
                    │     └─ Cria ExternalMarker(DeploymentStarted)
                    │
                    └─ SE não existir:
                          └─ Cria Release (ApiAssetId=Guid.Empty se externo)
                          └─ Cria ChangeEvent(deploy_created)
                          └─ Cria ExternalMarker(DeploymentStarted)
```

---

## 7. Impacto em Queries / Frontend

- O campo `IsNewRelease` na resposta do `NotifyDeployment` permite ao chamador saber se a release foi
  criada ou enriquecida — útil para dashboards de Change Intelligence.
- O `ExternalMarkerId` permite rastrear o marcador específico do pipeline.
- As queries existentes de listagem de releases (`ListReleases`, `GetRelease`, `ListChangesByService`)
  continuam compatíveis: nenhum campo foi removido.
- A nova resposta da Ingestion API inclui `releaseId` e `isNewRelease` para feedback ao pipeline.

---

## 8. Validação Realizada

- ✅ Compilação sem erros: `ChangeGovernance.Application`, `ChangeGovernance.Infrastructure`, `Ingestion.Api`
- ✅ 200/200 testes ChangeGovernance passam (incluindo 3 novos)
- ✅ Todos os testes unitários do projecto passam (2628+ testes)
- ✅ Testes E2E e Integration não afectados (falhas são pré-existentes — requerem infraestrutura)

---

## 9. Notas Residuais

- `ApiAssetId = Guid.Empty` para releases criadas por eventos externos. A correlação com o catálogo
  de serviços (`Catalog` module) fica para uma fase futura.
- A EF configuration de `chg_releases.ApiAssetId` permanece `IsRequired()` — `Guid.Empty` é um valor
  válido a nível de schema PostgreSQL. Não é necessária migração.
- O índice `IX_chg_releases_ApiAssetId` existente continua funcional.
