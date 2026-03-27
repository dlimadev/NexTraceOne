# P8.1 — Integrations Module Backend Dedicated & IntegrationsDbContext Report

## Objetivo

Concretizar a separação estrutural do módulo Integrations no backend do NexTraceOne,
criando o módulo dedicado `src/modules/integrations/` com API layer completo, movendo
CQRS handlers e endpoints do Governance para o módulo correto, e garantindo que o
ownership backend do domínio de integrações deixe de residir em Governance.

---

## Estado anterior (antes de P8.1)

| Componente | Estado |
|---|---|
| `Integrations.Domain` | ✅ Existia — 3 entidades, 6 enums |
| `Integrations.Application` | ⚠️ Parcial — apenas Abstractions (3 interfaces de repositório) |
| `Integrations.Infrastructure` | ✅ Existia — IntegrationsDbContext, 3 repositórios, 3 configurações EF |
| `Integrations.Contracts` | ✅ Existia — 4 integration events |
| `Integrations.API` | ❌ Não existia |
| CQRS handlers (8) | ⚠️ Em `Governance.Application.Features` (transitório P2.4) |
| IntegrationHubEndpointModule | ⚠️ Em `Governance.API.Endpoints` (transitório P2.4) |
| DI wiring | ⚠️ `Governance.Infrastructure` chamava `AddIntegrationsInfrastructure()` |
| Module registration (Program.cs) | ❌ Sem `AddIntegrationsModule()` |
| Frontend | ✅ Funcional — 4 páginas em `features/integrations/` |

---

## Estrutura do novo módulo criada

```
src/modules/integrations/
├── NexTraceOne.Integrations.API/                          ← NOVO
│   ├── Endpoints/
│   │   ├── DependencyInjection.cs                         ← AddIntegrationsModule()
│   │   └── IntegrationHubEndpointModule.cs                ← Movido de Governance.API
│   └── NexTraceOne.Integrations.API.csproj
├── NexTraceOne.Integrations.Application/
│   ├── Abstractions/
│   │   ├── IIntegrationConnectorRepository.cs
│   │   ├── IIngestionSourceRepository.cs
│   │   └── IIngestionExecutionRepository.cs
│   ├── DependencyInjection.cs                             ← NOVO (MediatR registration)
│   ├── Features/                                          ← Movidos de Governance.Application
│   │   ├── GetIngestionFreshness/GetIngestionFreshness.cs
│   │   ├── GetIngestionHealth/GetIngestionHealth.cs
│   │   ├── GetIntegrationConnector/GetIntegrationConnector.cs
│   │   ├── ListIngestionExecutions/ListIngestionExecutions.cs
│   │   ├── ListIngestionSources/ListIngestionSources.cs
│   │   ├── ListIntegrationConnectors/ListIntegrationConnectors.cs
│   │   ├── ReprocessExecution/ReprocessExecution.cs
│   │   └── RetryConnector/RetryConnector.cs
│   └── NexTraceOne.Integrations.Application.csproj
├── NexTraceOne.Integrations.Contracts/
│   ├── IntegrationEvents.cs
│   └── NexTraceOne.Integrations.Contracts.csproj
├── NexTraceOne.Integrations.Domain/
│   ├── Entities/ (IntegrationConnector, IngestionSource, IngestionExecution)
│   ├── Enums/ (6 enums)
│   └── NexTraceOne.Integrations.Domain.csproj
└── NexTraceOne.Integrations.Infrastructure/
    ├── DependencyInjection.cs
    ├── Persistence/
    │   ├── IntegrationsDbContext.cs
    │   ├── IntegrationsDbContextDesignTimeFactory.cs
    │   ├── Configurations/ (3 EF configurations)
    │   └── Repositories/ (3 repository implementations)
    └── NexTraceOne.Integrations.Infrastructure.csproj
```

---

## IntegrationsDbContext

O `IntegrationsDbContext` já existia desde P2.1/P2.2 e contém:

| DbSet | Tabela | Prefixo |
|---|---|---|
| `IntegrationConnectors` | `int_connectors` | `int_` |
| `IngestionSources` | `int_ingestion_sources` | `int_` |
| `IngestionExecutions` | `int_ingestion_executions` | `int_` |
| Outbox | `int_outbox_messages` | `int_` |

Características:
- Multi-tenant via `NexTraceDbContextBase`
- `IUnitOfWork` com `CommitAsync()`
- Interceptors: `AuditInterceptor`, `TenantRlsInterceptor`
- Concorrência otimista via PostgreSQL `xmin`
- Design-time factory para EF migrations

---

## Ficheiros alterados/criados

### Criados

| Ficheiro | Descrição |
|---|---|
| `Integrations.API/NexTraceOne.Integrations.API.csproj` | Projeto API do módulo |
| `Integrations.API/Endpoints/DependencyInjection.cs` | `AddIntegrationsModule()` |
| `Integrations.API/Endpoints/IntegrationHubEndpointModule.cs` | 8 endpoints (movido de Governance) |
| `Integrations.Application/DependencyInjection.cs` | `AddIntegrationsApplication()` com MediatR |
| `Integrations.Application/Features/` (8 handlers) | CQRS handlers movidos de Governance |

### Alterados

| Ficheiro | Alteração |
|---|---|
| `Integrations.Application.csproj` | Adicionadas dependências `MediatR` e `FluentValidation` |
| `Governance.Application.csproj` | Removida referência a `Integrations.Application` |
| `Governance.Infrastructure/DependencyInjection.cs` | Removido `AddIntegrationsInfrastructure()` transitório |
| `Governance.Infrastructure.csproj` | Removida referência a `Integrations.Infrastructure` |
| `ApiHost/Program.cs` | Adicionado `AddIntegrationsModule()` e `using` |
| `ApiHost/NexTraceOne.ApiHost.csproj` | Adicionada referência a `Integrations.API` |
| `Ingestion.Api/NexTraceOne.Ingestion.Api.csproj` | Adicionada referência a `Integrations.Infrastructure` |
| `NexTraceOne.sln` | Adicionado projeto `Integrations.API` à solution |

### Removidos de Governance

| Ficheiro | Motivo |
|---|---|
| `Governance.API/Endpoints/IntegrationHubEndpointModule.cs` | Movido para Integrations.API |
| `Governance.Application/Features/ListIntegrationConnectors/` | Movido para Integrations.Application |
| `Governance.Application/Features/GetIntegrationConnector/` | Movido para Integrations.Application |
| `Governance.Application/Features/ListIngestionSources/` | Movido para Integrations.Application |
| `Governance.Application/Features/ListIngestionExecutions/` | Movido para Integrations.Application |
| `Governance.Application/Features/GetIngestionHealth/` | Movido para Integrations.Application |
| `Governance.Application/Features/GetIngestionFreshness/` | Movido para Integrations.Application |
| `Governance.Application/Features/RetryConnector/` | Movido para Integrations.Application |
| `Governance.Application/Features/ReprocessExecution/` | Movido para Integrations.Application |

---

## Entidades/mappings/wiring

### Entidades no IntegrationsDbContext

| Entidade | Strongly-typed ID | Tabela |
|---|---|---|
| `IntegrationConnector` | `IntegrationConnectorId` | `int_connectors` |
| `IngestionSource` | `IngestionSourceId` | `int_ingestion_sources` |
| `IngestionExecution` | `IngestionExecutionId` | `int_ingestion_executions` |

### Wiring

- `AddIntegrationsModule()` → `AddIntegrationsApplication()` + `AddIntegrationsInfrastructure()`
- `AddIntegrationsApplication()` registra MediatR handlers do assembly
- `AddIntegrationsInfrastructure()` regista DbContext, repositórios (3)
- `Program.cs` chama `AddIntegrationsModule()` após `AddGovernanceModule()`
- `MapAllModuleEndpoints()` descobre automaticamente `IntegrationHubEndpointModule` via reflection

---

## Endpoints (8 endpoints mantidos)

| Método | Rota | Permissão |
|---|---|---|
| GET | `/api/v1/integrations/connectors` | `integrations:read` |
| GET | `/api/v1/integrations/connectors/{id}` | `integrations:read` |
| GET | `/api/v1/ingestion/sources` | `integrations:read` |
| GET | `/api/v1/ingestion/executions` | `integrations:read` |
| GET | `/api/v1/integrations/health` | `integrations:read` |
| GET | `/api/v1/ingestion/freshness` | `integrations:read` |
| POST | `/api/v1/integrations/connectors/{id}/retry` | `integrations:write` |
| POST | `/api/v1/ingestion/executions/{id}/reprocess` | `integrations:write` |

As rotas mantêm-se idênticas — sem breaking change para o frontend.

---

## Impacto no frontend

- **Nenhum breaking change**: as rotas `/api/v1/integrations/*` e `/api/v1/ingestion/*` são idênticas
- O frontend em `features/integrations/` continua a funcionar sem alteração
- O API client (`api/integrations.ts`) não precisa de mudança

---

## Validação funcional/compilação

- ✅ Build completo: `dotnet build` — 0 erros
- ✅ Governance tests: 163 testes passam (incluindo `IntegrationHubFeatureTests`)
- ✅ Endpoint discovery: `MapAllModuleEndpoints()` detecta automaticamente `NexTraceOne.Integrations.API`
- ✅ Migration wave: `IntegrationsDbContext` já estava em Wave 5 de `ApplyDatabaseMigrationsAsync()`

---

## Resumo da separação

| Antes (P2.4) | Depois (P8.1) |
|---|---|
| Handlers em `Governance.Application` | Handlers em `Integrations.Application` |
| Endpoints em `Governance.API` | Endpoints em `Integrations.API` |
| Infra wired via `Governance.Infrastructure` | Infra wired via `Integrations.API` |
| Sem `AddIntegrationsModule()` em Program.cs | `AddIntegrationsModule()` registado |
| Governance.Application referenciava Integrations.Application | Referência removida |
| Governance como catch-all de integrações | Governance limpo — apenas Governance puro |
