# AGENTS.md — ProductAnalytics Module

## Context

This is the **ProductAnalytics** bounded context of NexTraceOne. It tracks product adoption, user journeys, friction signals, and value milestones.

## Module Structure

```
src/modules/productanalytics/
├── NexTraceOne.ProductAnalytics.Domain/
│   ├── Entities/AnalyticsEvent.cs
│   ├── Entities/JourneyDefinition.cs
│   └── Enums/
├── NexTraceOne.ProductAnalytics.Application/
│   ├── Abstractions/IAnalyticsEventRepository.cs
│   ├── Abstractions/IJourneyDefinitionRepository.cs
│   ├── AnalyticsQueryHelper.cs          ← DRY: ResolveRange, ToModuleDisplayName
│   ├── Features/                         ← CQRS handlers (one folder per feature)
│   └── Constants/AnalyticsConstants.cs
├── NexTraceOne.ProductAnalytics.Infrastructure/
│   ├── Persistence/
│   │   ├── ProductAnalyticsDbContext.cs
│   │   ├── Repositories/
│   │   │   ├── AnalyticsEventRepository.cs          ← PostgreSQL (default read/write)
│   │   │   ├── ClickHouseAnalyticsEventRepository.cs ← ClickHouse reads only
│   │   │   ├── ElasticsearchAnalyticsEventRepository.cs ← Elastic reads only
│   │   │   └── JourneyDefinitionRepository.cs
│   │   └── Configurations/
│   └── Services/
│       ├── ProductAnalyticsModuleService.cs
│       └── ExportAnalyticsData.cs
├── NexTraceOne.ProductAnalytics.API/
│   └── Endpoints/ProductAnalyticsEndpointModule.cs
└── NexTraceOne.ProductAnalytics.Contracts/
    └── ServiceInterfaces/IProductAnalyticsModule.cs
```

## Key Architectural Decisions

- **Analytics reads are provider-aware**: `IAnalyticsEventRepository` is resolved to:
  - `ElasticsearchAnalyticsEventRepository` when `Telemetry:ObservabilityProvider:Provider = "Elastic"` (default)
  - `ClickHouseAnalyticsEventRepository` when `"ClickHouse"`
  - `AnalyticsEventRepository` (PostgreSQL) for any other value or when the analytic store is disabled
- **Writes always go to PostgreSQL**: Both `ClickHouseAnalyticsEventRepository` and `ElasticsearchAnalyticsEventRepository` delegate `AddAsync` to the PostgreSQL fallback repository. Events are forwarded asynchronously to the analytic store via `IAnalyticsEventForwarder`.
- **Tenant isolation is defense-in-depth**: Every repository read method filters by `currentTenant.Id`. PostgreSQL also has RLS (`TenantRlsInterceptor`).
- **Journey definitions support global + tenant overrides**: `JourneyDefinitionRepository.ListActiveAsync` merges global definitions (null tenant) with tenant-specific overrides.

## Coding Conventions

- Handlers live in `Features.{FeatureName}` namespace, one static class per feature.
- Use `AnalyticsQueryHelper.ResolveRange` and `AnalyticsQueryHelper.ToModuleDisplayName` instead of duplicating them in handlers.
- All handler dependencies must be required (no optional DI parameters with `= null`).
- `ICurrentTenant` should never be accepted as a request parameter; always inject it.
- Frontend session IDs must use `crypto.randomUUID()` or `crypto.getRandomValues()` — never `Math.random()`.

## Testing

- Test project: `tests/modules/productanalytics/NexTraceOne.ProductAnalytics.Tests/`
- Stack: xUnit, FluentAssertions, NSubstitute, EF InMemory
- When adding new repository implementations, add unit tests that verify tenant filtering and parameter passing.

## Build & Run

```bash
# Build module
dotnet build src/modules/productanalytics/NexTraceOne.ProductAnalytics.API/

# Run tests
dotnet test tests/modules/productanalytics/NexTraceOne.ProductAnalytics.Tests/
```

## Recent Changes (May 2026)

- Added `ElasticsearchAnalyticsEventRepository` for Elastic provider support.
- Added parameterized queries to `ClickHouseAnalyticsEventRepository` (HIGH-001).
- Added tenant filtering to all repository read methods (HIGH-002, HIGH-003, HIGH-006).
- Extracted `AnalyticsQueryHelper` to eliminate duplicated `ResolveRange` / `ToModuleDisplayName`.
- Added `CreatedAt` to `AnalyticsEvent` entity.
- Added rate limiting (`data-intensive` policy) to export endpoints.
- `RecordAnalyticsEvent` endpoint now returns `201 Created` instead of `200 OK`.
