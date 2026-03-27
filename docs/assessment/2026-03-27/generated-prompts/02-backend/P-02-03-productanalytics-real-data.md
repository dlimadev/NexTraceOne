# P-02-03 — Implementar queries reais nos handlers GetPersonaUsage, GetJourneys e GetValueMilestones

## 1. Título

Substituir dados mínimos por queries reais nos handlers de ProductAnalytics: GetPersonaUsage, GetJourneys e GetValueMilestones.

## 2. Modo de operação

**Implementation**

## 3. Objetivo

Os handlers GetPersonaUsage, GetJourneys e GetValueMilestones no módulo ProductAnalytics retornam dados estruturados mas sem queries reais à base de dados. Este prompt implementa as queries necessárias para que estes handlers devolvam dados agregados da tabela AnalyticsEvent, transformando o módulo num sistema funcional de analítica de produto.

## 4. Problema atual

- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/GetPersonaUsage/GetPersonaUsage.cs` — retorna dados estáticos ou mínimos.
- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/GetJourneys/GetJourneys.cs` — mesma situação.
- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/GetValueMilestones/GetValueMilestones.cs` — mesma situação.
- A interface `IAnalyticsEventRepository` em `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Abstractions/IAnalyticsEventRepository.cs` pode não ter métodos de agregação necessários.
- O `AnalyticsEventRepository.cs` em Infrastructure não implementa queries de agregação por persona, módulo ou período.
- A entidade `AnalyticsEvent` em `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Domain/Entities/AnalyticsEvent.cs` é a fonte de dados mas não está a ser explorada.
- Os enums relevantes existem: `AnalyticsEventType`, `ProductModule`, `JourneyStatus`, `ValueMilestoneType`, `TrendDirection`.

## 5. Escopo permitido

- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/GetPersonaUsage/GetPersonaUsage.cs`
- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/GetJourneys/GetJourneys.cs`
- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/GetValueMilestones/GetValueMilestones.cs`
- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Abstractions/IAnalyticsEventRepository.cs`
- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/Persistence/Repositories/AnalyticsEventRepository.cs`
- `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/Persistence/ProductAnalyticsDbContext.cs` (se necessário para queries)

## 6. Escopo proibido

- Não alterar módulos fora de `src/modules/productanalytics/`.
- Não alterar a entidade AnalyticsEvent (Domain) sem necessidade comprovada.
- Não criar novos endpoints — apenas melhorar os handlers existentes.
- Não alterar migrações já aplicadas.
- Não usar raw SQL — preferir LINQ com EF Core para queries de agregação.

## 7. Ficheiros principais candidatos a alteração

1. `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/GetPersonaUsage/GetPersonaUsage.cs`
2. `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/GetJourneys/GetJourneys.cs`
3. `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Features/GetValueMilestones/GetValueMilestones.cs`
4. `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Application/Abstractions/IAnalyticsEventRepository.cs`
5. `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/Persistence/Repositories/AnalyticsEventRepository.cs`
6. `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/Persistence/ProductAnalyticsDbContext.cs`

## 8. Responsabilidades permitidas

- Adicionar métodos à interface IAnalyticsEventRepository: GetUsageByPersonaAsync, GetJourneyProgressAsync, GetMilestoneAchievementsAsync.
- Implementar queries EF Core com GroupBy, Count, agregações temporais no AnalyticsEventRepository.
- Atualizar os 3 handlers para chamar o repositório com queries reais em vez de dados estáticos.
- Calcular TrendDirection comparando período atual com período anterior.
- Usar CancellationToken em todas as operações async.
- Adicionar logging com contexto de query e resultado para diagnóstico.

## 9. Responsabilidades proibidas

- Não criar sistema de caching neste prompt (será tratado separadamente se necessário).
- Não criar materialized views — usar queries diretas à tabela AnalyticsEvent.
- Não implementar paginação complexa — estes endpoints são de agregação resumida.
- Não aceder a DbContexts de outros módulos para enriquecer dados.

## 10. Critérios de aceite

- [ ] GetPersonaUsage retorna dados agregados reais da tabela AnalyticsEvent, agrupados por persona.
- [ ] GetJourneys retorna progresso real dos journeys calculados a partir de eventos.
- [ ] GetValueMilestones retorna milestones atingidos com base em critérios reais.
- [ ] TrendDirection é calculado comparando período atual vs. anterior (não hardcoded).
- [ ] Todos os handlers usam CancellationToken.
- [ ] Compilação completa da solution sem erros.
- [ ] Handlers retornam lista vazia (não erro) quando não há dados.

## 11. Validações obrigatórias

- Compilação do módulo ProductAnalytics (todos os 4 projetos).
- Compilação da solution NexTraceOne.sln.
- Verificar que handlers existentes (GetAnalyticsSummary, GetModuleAdoption, GetFrictionIndicators, RecordAnalyticsEvent) continuam a funcionar.
- Verificar que queries EF Core não geram N+1 (usar Include ou projeção quando necessário).

## 12. Riscos e cuidados

- Queries de agregação sobre AnalyticsEvent podem ser lentas se a tabela tiver muitos registos — considerar filtro temporal obrigatório.
- GroupBy em EF Core pode não ser traduzido para SQL em todos os cenários — verificar que a query é executada server-side.
- A falta de seed data pode fazer com que os handlers retornem sempre listas vazias durante desenvolvimento.
- O enum TrendDirection existe em `src/modules/productanalytics/NexTraceOne.ProductAnalytics.Domain/Enums/TrendDirection.cs` — verificar compatibilidade com o cálculo.

## 13. Dependências

- **P-00-09** — Migração do módulo ProductAnalytics deve estar aplicada (tabela AnalyticsEvent criada).
- A entidade AnalyticsEvent deve ter campos suficientes para agregar por persona, módulo e período temporal.
- O handler RecordAnalyticsEvent deve estar funcional para que existam dados para consultar.

## 14. Próximos prompts sugeridos

- **P-02-05** — Criar projeto Contracts para ProductAnalytics (para publicar eventos de integração).
- **P-03-02** — Frontend: substituir mocks nas páginas PersonaUsagePage, ValueTrackingPage, JourneyFunnelPage.
- **P-04-02** — Seed data para ProductAnalytics (para desenvolvimento e testes).
