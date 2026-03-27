# P8.3 — ProductAnalytics Module & DbContext Extraction Report

## Objetivo

Extrair o domínio de Product Analytics do módulo Governance catch-all para um módulo backend dedicado e independente em `src/modules/productanalytics/`.

## Estado anterior

| Item | Localização anterior | Owner semântico |
|------|---------------------|-----------------|
| ProductAnalyticsEndpointModule (7 endpoints) | `Governance.API/Endpoints/` | ProductAnalytics |
| RecordAnalyticsEvent handler | `Governance.Application/Features/` | ProductAnalytics |
| GetAnalyticsSummary handler | `Governance.Application/Features/` | ProductAnalytics |
| GetModuleAdoption handler | `Governance.Application/Features/` | ProductAnalytics |
| GetPersonaUsage handler | `Governance.Application/Features/` | ProductAnalytics |
| GetJourneys handler | `Governance.Application/Features/` | ProductAnalytics |
| GetValueMilestones handler | `Governance.Application/Features/` | ProductAnalytics |
| GetFrictionIndicators handler | `Governance.Application/Features/` | ProductAnalytics |
| JourneyStatus enum | `Governance.Domain/Enums/` | ProductAnalytics |
| FrictionSignalType enum | `Governance.Domain/Enums/` | ProductAnalytics |
| ValueMilestoneType enum | `Governance.Domain/Enums/` | ProductAnalytics |
| AddProductAnalyticsInfrastructure() | Called from `Governance.Infrastructure.DI` | ProductAnalytics |
| AnalyticsFeatureTests (7 tests) | `Governance.Tests/` | ProductAnalytics |
| ProductAnalyticsDbContext | `ProductAnalytics.Infrastructure` (pre-existing) | ProductAnalytics |
| AnalyticsEvent entity | `ProductAnalytics.Domain` (pre-existing) | ProductAnalytics |
| IAnalyticsEventRepository | `ProductAnalytics.Application` (pre-existing) | ProductAnalytics |

## Rotas migradas

Todas as rotas mantêm o mesmo path (`/api/v1/product-analytics/*`). A diferença é o assembly que as serve:

| Rota | Método | Permissão anterior | Permissão nova |
|------|--------|-------------------|----------------|
| `/api/v1/product-analytics/events` | POST | `governance:analytics:write` | `analytics:write` |
| `/api/v1/product-analytics/summary` | GET | `governance:analytics:read` | `analytics:read` |
| `/api/v1/product-analytics/adoption/modules` | GET | `governance:analytics:read` | `analytics:read` |
| `/api/v1/product-analytics/adoption/personas` | GET | `governance:analytics:read` | `analytics:read` |
| `/api/v1/product-analytics/journeys` | GET | `governance:analytics:read` | `analytics:read` |
| `/api/v1/product-analytics/value-milestones` | GET | `governance:analytics:read` | `analytics:read` |
| `/api/v1/product-analytics/friction` | GET | `governance:analytics:read` | `analytics:read` |

## Ficheiros criados

| Ficheiro | Descrição |
|----------|-----------|
| `src/modules/productanalytics/NexTraceOne.ProductAnalytics.API/NexTraceOne.ProductAnalytics.API.csproj` | Projeto API |
| `src/modules/productanalytics/NexTraceOne.ProductAnalytics.API/Endpoints/DependencyInjection.cs` | AddProductAnalyticsModule() |
| `src/modules/productanalytics/NexTraceOne.ProductAnalytics.API/Endpoints/ProductAnalyticsEndpointModule.cs` | 7 endpoints |
| `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/DependencyInjection.cs` | AddProductAnalyticsApplication() com MediatR |
| `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/RecordAnalyticsEvent/RecordAnalyticsEvent.cs` | Handler |
| `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/GetAnalyticsSummary/GetAnalyticsSummary.cs` | Handler |
| `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/GetModuleAdoption/GetModuleAdoption.cs` | Handler |
| `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/GetPersonaUsage/GetPersonaUsage.cs` | Handler |
| `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/GetJourneys/GetJourneys.cs` | Handler |
| `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/GetValueMilestones/GetValueMilestones.cs` | Handler |
| `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/GetFrictionIndicators/GetFrictionIndicators.cs` | Handler |
| `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Domain/Enums/TrendDirection.cs` | Enum |
| `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Domain/Enums/JourneyStatus.cs` | Enum |
| `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Domain/Enums/FrictionSignalType.cs` | Enum |
| `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Domain/Enums/ValueMilestoneType.cs` | Enum |
| `tests/modules/productanalytics/NexTraceOne.ProductAnalytics.Tests/NexTraceOne.ProductAnalytics.Tests.csproj` | Projeto testes |
| `tests/modules/productanalytics/NexTraceOne.ProductAnalytics.Tests/GlobalUsings.cs` | Usings globais |
| `tests/modules/productanalytics/NexTraceOne.ProductAnalytics.Tests/Application/Features/AnalyticsFeatureTests.cs` | 7 testes |

## Ficheiros alterados

| Ficheiro | Alteração |
|----------|-----------|
| `NexTraceOne.sln` | Adicionados ProductAnalytics.API e ProductAnalytics.Tests |
| `src/platform/NexTraceOne.ApiHost/Program.cs` | Adicionado `AddProductAnalyticsModule()` + using |
| `src/platform/NexTraceOne.ApiHost/NexTraceOne.ApiHost.csproj` | Adicionada referência ProductAnalytics.API |
| `src/modules/governance/NexTraceOne.Governance.Application/NexTraceOne.Governance.Application.csproj` | Removida referência ProductAnalytics.Application |
| `src/modules/governance/NexTraceOne.Governance.Infrastructure/NexTraceOne.Governance.Infrastructure.csproj` | Removida referência ProductAnalytics.Infrastructure |
| `src/modules/governance/NexTraceOne.Governance.Infrastructure/DependencyInjection.cs` | Removida chamada AddProductAnalyticsInfrastructure() |
| `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/GovernanceDbContext.cs` | Atualizado comentário (P8.3 concluído) |
| `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/NexTraceOne.ProductAnalytics.Application.csproj` | Adicionados FluentValidation e MediatR |
| `tests/modules/governance/NexTraceOne.Governance.Tests/NexTraceOne.Governance.Tests.csproj` | Removidas referências ProductAnalytics |

## Ficheiros removidos de Governance

| Ficheiro removido |
|-------------------|
| `src/modules/governance/NexTraceOne.Governance.API/Endpoints/ProductAnalyticsEndpointModule.cs` |
| `src/modules/governance/NexTraceOne.Governance.Application/Features/RecordAnalyticsEvent/RecordAnalyticsEvent.cs` |
| `src/modules/governance/NexTraceOne.Governance.Application/Features/GetAnalyticsSummary/GetAnalyticsSummary.cs` |
| `src/modules/governance/NexTraceOne.Governance.Application/Features/GetModuleAdoption/GetModuleAdoption.cs` |
| `src/modules/governance/NexTraceOne.Governance.Application/Features/GetPersonaUsage/GetPersonaUsage.cs` |
| `src/modules/governance/NexTraceOne.Governance.Application/Features/GetJourneys/GetJourneys.cs` |
| `src/modules/governance/NexTraceOne.Governance.Application/Features/GetValueMilestones/GetValueMilestones.cs` |
| `src/modules/governance/NexTraceOne.Governance.Application/Features/GetFrictionIndicators/GetFrictionIndicators.cs` |
| `src/modules/governance/NexTraceOne.Governance.Domain/Enums/JourneyStatus.cs` |
| `src/modules/governance/NexTraceOne.Governance.Domain/Enums/FrictionSignalType.cs` |
| `src/modules/governance/NexTraceOne.Governance.Domain/Enums/ValueMilestoneType.cs` |
| `tests/modules/governance/NexTraceOne.Governance.Tests/Application/Features/AnalyticsFeatureTests.cs` |

## Estrutura final do módulo ProductAnalytics

```
src/modules/productanalytics/
├── NexTraceOne.ProductAnalytics.Domain/
│   ├── Entities/
│   │   └── AnalyticsEvent.cs
│   └── Enums/
│       ├── AnalyticsEventType.cs
│       ├── ProductModule.cs
│       ├── TrendDirection.cs
│       ├── JourneyStatus.cs
│       ├── FrictionSignalType.cs
│       └── ValueMilestoneType.cs
├── NexTraceOne.ProductAnalytics.Application/
│   ├── Abstractions/
│   │   └── IAnalyticsEventRepository.cs
│   ├── DependencyInjection.cs
│   └── Features/
│       ├── RecordAnalyticsEvent/
│       ├── GetAnalyticsSummary/
│       ├── GetModuleAdoption/
│       ├── GetPersonaUsage/
│       ├── GetJourneys/
│       ├── GetValueMilestones/
│       └── GetFrictionIndicators/
├── NexTraceOne.ProductAnalytics.Infrastructure/
│   ├── DependencyInjection.cs
│   └── Persistence/
│       ├── ProductAnalyticsDbContext.cs
│       ├── ProductAnalyticsDbContextDesignTimeFactory.cs
│       ├── Configurations/
│       │   └── AnalyticsEventConfiguration.cs
│       └── Repositories/
│           └── AnalyticsEventRepository.cs
└── NexTraceOne.ProductAnalytics.API/
    └── Endpoints/
        ├── DependencyInjection.cs
        └── ProductAnalyticsEndpointModule.cs

tests/modules/productanalytics/
└── NexTraceOne.ProductAnalytics.Tests/
    └── Application/Features/
        └── AnalyticsFeatureTests.cs (7 tests)
```

## Impacto no frontend

O frontend (`src/frontend/src/features/product-analytics/api/productAnalyticsApi.ts`) já consumia rotas `/product-analytics/*` (base URL `/api/v1`). Zero alterações no frontend foram necessárias — as rotas permanecem idênticas.

## Compatibilidade transitória

Não necessária. A migração é limpa — as rotas HTTP são idênticas, apenas servidas por um assembly diferente.

## Permissões

Permissões renomeadas de `governance:analytics:*` para `analytics:*` no novo módulo. A permissão anterior deixou de existir.

## Validação

- **Build**: 0 errors ✅
- **ProductAnalytics.Tests**: 7 tests passed ✅
- **Governance.Tests**: 139 tests passed (146 - 7 migrados) ✅
- **Governance no longer owns ProductAnalytics**: zero referências a ProductAnalytics em Governance ✅
