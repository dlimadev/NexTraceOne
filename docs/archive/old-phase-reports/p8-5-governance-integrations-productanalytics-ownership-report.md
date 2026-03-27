# P8.5 — Governance / Integrations / ProductAnalytics Ownership Report

## Objetivo

Fechar a separação definitiva de Governance em relação a Integrations e Product Analytics,
consolidando a matriz de ownership, removendo resíduos informativos de migração e
documentando as fronteiras finais entre os três módulos.

## Estado anterior (pré-P8.5)

P8.1–P8.4 já completaram a migração funcional:
- P8.1: IntegrationHub endpoints + handlers migrados para `Integrations.API`
- P8.2: Integrations tests migrados para `Integrations.Tests`
- P8.3: ProductAnalytics endpoints + handlers migrados para `ProductAnalytics.API`
- P8.4: Permissões `governance:analytics:*` alinhadas para `analytics:*`

No entanto, persistiam **comentários informativos de migração** nos ficheiros de Governance
que mencionavam os módulos separados — ruído que podia dar aparência de ownership residual.

## Alterações realizadas em P8.5

### 1. GovernanceDbContext.cs

**Ficheiro:** `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/GovernanceDbContext.cs`

- Removido bloco de comentários de migração (P2.1–P8.3) do XML doc do `GovernanceDbContext`
- Removidos 4 comentários `// NOTE:` inline que listavam entidades extraídas
- Resultado: DbContext limpo com 8 DbSets, sem referências a outros módulos

### 2. DependencyInjection.cs (Infrastructure)

**Ficheiro:** `src/modules/governance/NexTraceOne.Governance.Infrastructure/DependencyInjection.cs`

- Removidos 4 comentários `// NOTE:` que descreviam repositórios removidos em fases anteriores
- Resultado: 9 registos de repositórios limpos, sem referências a Integrations ou ProductAnalytics

### 3. IGovernanceRepositories.cs (Application/Abstractions)

**Ficheiro:** `src/modules/governance/NexTraceOne.Governance.Application/Abstractions/IGovernanceRepositories.cs`

- Removido comentário `// NOTE P2.3:` sobre IAnalyticsEventRepository e DTOs movidos
- Removidos 2 comentários `// NOTE P2.2:` sobre IIngestionSourceRepository e IIngestionExecutionRepository movidos
- Resultado: ficheiro contém apenas interfaces de Governance — sem menções a módulos alheios

### 4. Phase3GovernanceFeatureTests.cs

**Ficheiro:** `tests/modules/governance/NexTraceOne.Governance.Tests/Application/Features/Phase3GovernanceFeatureTests.cs`

- Removida menção a IngestionSource domain tests no XML doc da classe
- Resultado: documentação da classe descreve apenas o seu próprio escopo

## Matriz final de ownership

### Governance

| Camada | Conteúdo |
|--------|----------|
| Domain | Team, GovernanceDomain, GovernancePack, GovernancePackVersion, GovernanceWaiver, DelegatedAdministration, TeamDomainLink, GovernanceRolloutRecord |
| Application | 17 endpoint modules (Compliance, Delegation, Domain, Controls, Evidence, Executive, Compliance, FinOps, Packs, Reports, Risk, Waivers, Onboarding, PlatformStatus, PolicyCatalog, ScopedContext, Team) |
| Infrastructure | GovernanceDbContext (8 DbSets), 9 repositórios |
| Permissions | `governance:*` (teams, domains, packs, waivers, reports, risk, compliance, finops, policies, evidence, controls, admin) |
| Tests | 139 testes |

### Integrations

| Camada | Conteúdo |
|--------|----------|
| Domain | IntegrationConnector, IngestionSource, IngestionExecution |
| Application | 8 CQRS handlers |
| Infrastructure | IntegrationsDbContext (3 DbSets), repositórios próprios |
| API | IntegrationHubEndpointModule (8 endpoints) |
| Permissions | `integrations:read`, `integrations:write` |
| Tests | 17 testes |

### Product Analytics

| Camada | Conteúdo |
|--------|----------|
| Domain | AnalyticsEvent + 6 enums (AnalyticsEventType, FrictionSignalType, JourneyStatus, ProductModule, TrendDirection, ValueMilestoneType) |
| Application | 7 CQRS handlers + IAnalyticsEventRepository |
| Infrastructure | ProductAnalyticsDbContext (1 DbSet: pan_analytics_events), repositório próprio |
| API | ProductAnalyticsEndpointModule (7 endpoints) |
| Permissions | `analytics:read`, `analytics:write` |
| Tests | 7 testes |

## Referências cruzadas residuais

| Tipo | Resultado |
|------|-----------|
| `governance:analytics` em código | Zero ocorrências |
| `governance:integrations` em código | Zero ocorrências |
| `governance:connector` em código | Zero ocorrências |
| `governance:ingestion` em código | Zero ocorrências |
| ProjectReference cruzada em .csproj | Zero |
| Handlers de Integrations em Governance | Zero |
| Handlers de ProductAnalytics em Governance | Zero |
| DbSets de outros módulos em GovernanceDbContext | Zero |

## IGovernanceAnalyticsRepository — esclarecimento

`IGovernanceAnalyticsRepository` permanece legitimamente em Governance. Trata-se de um repositório
para **trends executivos do próprio módulo Governance** (contagens de waivers, packs publicados,
rollouts) — não é Product Analytics. O nome `Analytics` neste contexto refere-se a métricas
agregadas do domínio Governance, consumidas por `GetExecutiveTrends`.

## Validação

- **Build backend**: 0 errors ✅
- **Governance.Tests**: 139 passed ✅
- **ProductAnalytics.Tests**: 7 passed ✅
- **Integrations.Tests**: 17 passed ✅
- **IdentityAccess.Tests**: 290 passed ✅
- **Zero referências cruzadas residuais**: confirmado via grep ✅

## Ficheiros alterados

| Ficheiro | Tipo de alteração |
|----------|-------------------|
| `src/modules/governance/NexTraceOne.Governance.Infrastructure/Persistence/GovernanceDbContext.cs` | Remoção de comentários de migração |
| `src/modules/governance/NexTraceOne.Governance.Infrastructure/DependencyInjection.cs` | Remoção de comentários de migração |
| `src/modules/governance/NexTraceOne.Governance.Application/Abstractions/IGovernanceRepositories.cs` | Remoção de comentários de migração |
| `tests/modules/governance/NexTraceOne.Governance.Tests/Application/Features/Phase3GovernanceFeatureTests.cs` | Remoção de referência a módulo separado |
