# P8.4 — Post-Change Gap Report

## O que foi resolvido

1. **Permissões do catálogo de roles alinhadas** — `governance:analytics:*` → `analytics:*` em PlatformAdmin, TechLead, Viewer
2. **Frontend permission type corrigido** — `governance:analytics:read/write` removidos do tipo TypeScript `Permission`
3. **E2E test permissions corrigidos** — helper de autenticação agora usa `analytics:read`
4. **Zero referências `governance:analytics` em todo o codebase** — confirmado via grep global

## O que ficou pendente

### Para P8.5

1. **EF Migration formal** — `ProductAnalyticsDbContext` existe mas não foi gerada migration EF Core dedicada (`dotnet ef migrations add`)
2. **Seeder de dados** — Não existe seeder para Product Analytics em ambiente de desenvolvimento
3. **Dados reais em 3 handlers** — `GetPersonaUsage`, `GetJourneys`, `GetValueMilestones` retornam dados hardcoded; necessitam migração para consultas reais ao `IAnalyticsEventRepository`
4. **Event publishing** — Não existe publicação de integration events quando analytics events são registados
5. **Contracts project** — Não existe `NexTraceOne.ProductAnalytics.Contracts` para integration events
6. **Frontend permission update** — Utilizadores existentes com role assignment no banco de dados podem ter `governance:analytics:*` nas permissões persistidas; necessária migration de dados ou reseed
7. **TrendDirection duplicação** — Enum existe em 3 módulos (Governance, OperationalIntelligence, ProductAnalytics); considerar extracção para BuildingBlocks.Core

### Não incluído nesta fase (como definido no escopo)

- Product Analytics dashboards avançados
- Pipeline completo de tracking de eventos
- Analytics com Apache ECharts
- Redesign do frontend de Product Analytics
- Integrações externas de analytics

## Limitações residuais

1. **Dados simulados em 3 handlers** — GetPersonaUsage, GetJourneys e GetValueMilestones retornam dados hardcoded
2. **Sem migration EF formal** — Schema `pan_analytics_events` criado via DbContext mas sem migration rastreável
3. **Governance.Domain.Enums.TrendDirection preservado** — Mantido porque é usado por handlers legítimos de Governance (FinOps, Executive, Benchmarking)

## Estado final do ProductAnalytics após P8.3 + P8.4

O módulo ProductAnalytics é agora completamente independente:
- ✅ Domain: `AnalyticsEvent` + 6 enums
- ✅ Application: 7 CQRS handlers + `IAnalyticsEventRepository`
- ✅ Infrastructure: `ProductAnalyticsDbContext` + `AnalyticsEventRepository`
- ✅ API: `ProductAnalyticsEndpointModule` (7 endpoints) + `AddProductAnalyticsModule()`
- ✅ Tests: 7 testes unitários
- ✅ Permissões: `analytics:read` / `analytics:write` (3 roles atualizadas)
- ✅ Frontend: rotas `/product-analytics/*`, tipo `Permission` limpo
- ✅ Governance: zero ownership residual de ProductAnalytics
