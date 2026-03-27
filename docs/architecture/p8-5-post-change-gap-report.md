# P8.5 — Post-Change Gap Report

## O que foi resolvido

1. **Comentários de migração removidos** — GovernanceDbContext, DependencyInjection.cs,
   IGovernanceRepositories.cs e Phase3GovernanceFeatureTests.cs já não mencionam
   Integrations ou ProductAnalytics
2. **Matriz de ownership fechada** — documentação explícita das fronteiras entre
   Governance, Integrations e ProductAnalytics
3. **Zero referências cruzadas residuais** — confirmado que nenhum código activo
   em Governance faz referência a Integrations ou ProductAnalytics (exceto
   migrations EF Core históricas, que são imutáveis)
4. **Permissões completamente alinhadas** — `analytics:read/write` e
   `integrations:read/write` em RolePermissionCatalog, frontend permissions.ts,
   e2e auth.ts

## O que ficou pendente para fases futuras

### ProductAnalytics

1. **EF Migration formal** — `ProductAnalyticsDbContext` necessita migration EF Core gerada
2. **Seeder de dados** — Sem seeder para ambiente de desenvolvimento
3. **Dados reais em 3 handlers** — `GetPersonaUsage`, `GetJourneys`, `GetValueMilestones`
   retornam dados hardcoded
4. **Event publishing** — Sem publicação de integration events ao registar analytics events
5. **Contracts project** — Não existe `NexTraceOne.ProductAnalytics.Contracts`
6. **TrendDirection duplicação** — Enum existe em Governance, OperationalIntelligence e
   ProductAnalytics (considerar extracção para BuildingBlocks.Core)

### Integrations

1. **EF Migration formal** — `IntegrationsDbContext` necessita migration EF Core gerada
2. **Seeder de dados** — Sem seeder para ambiente de desenvolvimento
3. **CRUD endpoints** — Endpoints de criação/edição/remoção de connectors e ingestion sources
4. **Event publishing** — Sem publicação de integration events
5. **Frontend CRUD** — Sem UI de gestão de connectors/sources

### Governance

1. **Migration files históricos** — As migration files EF Core contêm tabelas `int_*` e
   `pan_*` (referência histórica, imutável — não é issue)
2. **IGovernanceAnalyticsRepository naming** — O nome pode gerar confusão com ProductAnalytics,
   mas é semanticamente correcto (analytics do domínio Governance)

## Limitações residuais

1. **Migrations EF Core históricas** — O ficheiro `InitialCreate.cs` em Governance contém
   referências a tabelas `int_connectors`, `int_ingestion_sources`, `int_ingestion_executions`
   e `pan_analytics_events`. Estas são referências imutáveis do histórico de migrations e
   **não devem ser alteradas**.

2. **Dados de permissão persistidos** — Utilizadores existentes com role assignments no
   banco de dados podem ter permissões `governance:analytics:*` persistidas. Necessária
   migration de dados ou reseed em ambientes existentes.

## Estado final da Fase 8

| Módulo | Domain | Application | Infrastructure | API | Tests | Permissions |
|--------|--------|-------------|----------------|-----|-------|-------------|
| Governance | ✅ 8 entidades | ✅ 17 endpoints | ✅ 8 DbSets, 9 repos | ✅ própria | ✅ 139 | `governance:*` |
| Integrations | ✅ 3 entidades | ✅ 8 handlers | ✅ 3 DbSets | ✅ própria | ✅ 17 | `integrations:*` |
| ProductAnalytics | ✅ 1 entidade + 6 enums | ✅ 7 handlers | ✅ 1 DbSet | ✅ própria | ✅ 7 | `analytics:*` |

A separação entre Governance, Integrations e Product Analytics está completa.
Cada módulo tem Domain, Application, Infrastructure, API e Tests próprios,
com zero ownership residual de Governance sobre os outros dois.
