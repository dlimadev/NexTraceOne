# P8.2 — Post-Change Gap Report

## O que foi resolvido

### ✅ Testes do domínio de integrações com ownership correcto

- `NexTraceOne.Integrations.Tests` criado como projecto de testes dedicado
- 15 testes de `IntegrationHubFeatureTests` movidos de `Governance.Tests`
- 2 testes de domínio `IngestionSource` movidos de `Phase3GovernanceFeatureTests`
- Total: 17 testes executam no módulo correcto

### ✅ Governance.Tests limpo de dependências de Integrations

- Referências a `Integrations.Domain` e `Integrations.Application` removidas de `Governance.Tests.csproj`
- `Phase3GovernanceFeatureTests` já não importa `NexTraceOne.Integrations.Domain.Entities`
- `IntegrationHubFeatureTests.cs` removido de `Governance.Tests`

### ✅ Separação completa do bounded context Integrations

Após P8.1 + P8.2, o módulo Integrations é totalmente independente:
- **Domain**: entidades, enums, strongly-typed IDs
- **Application**: abstractions (repositórios), DI, 8 CQRS handlers
- **Infrastructure**: IntegrationsDbContext, EF configurations, repositórios
- **Contracts**: integration events
- **API**: IntegrationHubEndpointModule, DI (`AddIntegrationsModule`)
- **Tests**: IntegrationHubFeatureTests, IngestionSourceDomainTests

### ✅ Frontend verificado

- API client em `features/integrations/api/integrations.ts` consome rotas correctas
- `apiClient.baseURL = '/api/v1'` resolve para `/api/v1/integrations/*` e `/api/v1/ingestion/*`
- Zero referências residuais a `/api/governance/integrations`
- 4 páginas funcionais: Hub, Connector Detail, Executions, Freshness

### ✅ Compilação e testes

- Build: 0 erros
- 17 testes Integrations passam
- 146 testes Governance passam (sem regressão)

---

## O que ficou pendente

### ⬜ EF Core Migrations

- `IntegrationsDbContext` não tem migrations geradas
- Tabelas `int_connectors`, `int_ingestion_sources`, `int_ingestion_executions` precisam de baseline migration
- `dotnet ef migrations add` necessário antes de deploy real

### ⬜ Development Data Seeder

- Não existe seeder para dados de exemplo de integrações
- Útil para onboarding e ambiente Development

### ⬜ Endpoint Expansion (CRUD completo)

- Os 8 endpoints actuais cobrem leitura + retry/reprocess
- Faltam: criação, edição e eliminação de conectores
- Faltam: gestão completa de fontes de ingestão

### ⬜ Integration Event Publishing

- `IntegrationEvents` em `Integrations.Contracts` definidos mas não publicados
- Implementar domain event → integration event via outbox pattern

### ⬜ Frontend CRUD

- Frontend actual é read-only com retry/reprocess
- Falta UI para criação e edição de conectores
- Falta UI de gestão de fontes de ingestão

### ⬜ Product Analytics Extraction

- Product Analytics continua como catch-all transitório em Governance
- `Governance.Infrastructure` ainda chama `AddProductAnalyticsInfrastructure()`
- `Governance.Application` ainda referencia `ProductAnalytics.Application`
- Extração de ProductAnalytics é tarefa separada, fora do escopo de Integrations

---

## O que fica explicitamente para P8.3

| Item | Descrição |
|---|---|
| EF Migration | Gerar baseline migration para `IntegrationsDbContext` |
| Seeder | Criar seeder de dados de desenvolvimento |
| CRUD endpoints | Adicionar CreateConnector, UpdateConnector, DeleteConnector |
| Event publishing | Implementar outbox publishing para integration events |
| Frontend CRUD | Adicionar formulários de criação/edição de conectores |

---

## Limitações residuais

1. **Sem migrations EF**: O `IntegrationsDbContext` está registado na wave de migrations
   (`WebApplicationExtensions.cs`), mas sem ficheiros de migration, o auto-migrate não
   cria as tabelas. Isto é pré-existente e não é uma regressão.

2. **Product Analytics em Governance**: `Governance.Tests` ainda referencia
   `ProductAnalytics.Domain` e `ProductAnalytics.Application` para os handlers de
   analytics que permanecem transitoriamente em Governance. Esta é uma separação
   independente do módulo Integrations.
