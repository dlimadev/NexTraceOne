# P8.3 — Post-Change Gap Report

## O que foi resolvido

1. **Módulo backend dedicado criado** — `src/modules/productanalytics/` agora contém Domain, Application, Infrastructure e API layers completos
2. **ProductAnalyticsDbContext já existia** — pré-criado em P2.3, agora devidamente wired via `AddProductAnalyticsModule()`
3. **7 CQRS handlers migrados** — RecordAnalyticsEvent, GetAnalyticsSummary, GetModuleAdoption, GetPersonaUsage, GetJourneys, GetValueMilestones, GetFrictionIndicators
4. **4 enums migrados** — JourneyStatus, FrictionSignalType, ValueMilestoneType, TrendDirection para ProductAnalytics.Domain.Enums
5. **Endpoint module migrado** — ProductAnalyticsEndpointModule agora em ProductAnalytics.API
6. **DI desacoplado** — `AddProductAnalyticsModule()` registado em Program.cs; `AddProductAnalyticsInfrastructure()` removido de Governance.Infrastructure
7. **Referências cross-module removidas** — Governance.Application e Governance.Infrastructure já não referenciam ProjectAnalytics
8. **Testes migrados** — 7 testes em NexTraceOne.ProductAnalytics.Tests
9. **Permissões alinhadas** — de `governance:analytics:*` para `analytics:*`
10. **GovernanceDbContext limpo** — comentários atualizados para refletir P8.3

## O que ficou pendente

### Para P8.4

1. **EF Migration** — Não foi gerada migration EF Core nesta fase. O ProductAnalyticsDbContext já existia com configurações correctas, mas `dotnet ef migrations add` deve ser executado quando necessário
2. **Seeder de dados** — Não existe seeder de Product Analytics para ambiente de desenvolvimento
3. **CRUD endpoints adicionais** — O módulo expõe apenas handlers de leitura/escrita de eventos e queries analíticas; endpoints de gestão (ex: configuração de tracking) não existem
4. **Event publishing** — Não existe publicação de integration events quando analytics events são registados
5. **GetPersonaUsage / GetJourneys / GetValueMilestones** usam dados hardcoded — necessitam migração para dados reais do IAnalyticsEventRepository
6. **TrendDirection duplicação** — O enum TrendDirection existe agora em 3 módulos independentes (Governance, OperationalIntelligence, ProductAnalytics). Considerar extracção para BuildingBlocks.Core se necessário
7. **Contracts project** — Não existe NexTraceOne.ProductAnalytics.Contracts. Criar se necessário para integration events
8. **Frontend permission update** — O frontend pode necessitar atualização nos permission checks se os utilizadores tinham `governance:analytics:*` e agora precisam de `analytics:*`

### Não incluído nesta fase (como definido no escopo)

- Product Analytics dashboards avançados
- Pipeline completo de tracking de eventos
- Analytics com Apache ECharts
- Redesign do frontend de Product Analytics
- Integrações externas de analytics

## Limitações residuais

1. **Dados simulados em 3 handlers** — GetPersonaUsage, GetJourneys e GetValueMilestones retornam dados hardcoded. Apenas GetFrictionIndicators e GetAnalyticsSummary/GetModuleAdoption consomem dados reais
2. **Sem migration EF aplicada** — O schema `pan_analytics_events` deve já existir via ProductAnalyticsDbContext configuration, mas não há migration formal nesta fase
3. **Governance.Domain.Enums.TrendDirection preservado** — Mantido porque é usado por outros handlers legítimos de Governance (FinOps, Executive, Benchmarking). Não é dívida técnica

## Estado final do Governance após P8.1 + P8.2 + P8.3

Governance já não serve como catch-all para bounded contexts de outros módulos:
- ❌ IntegrationHub → extraído para Integrations (P8.1)
- ❌ ProductAnalytics → extraído para ProductAnalytics (P8.3)
- ✅ Governance contém apenas: Teams, Domains, Packs, PackVersions, Waivers, DelegatedAdministration, TeamDomainLinks, RolloutRecords, GovernanceAnalytics, Executive views, FinOps views, Policy, Compliance, Risk, Reports
