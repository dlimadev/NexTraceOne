# P8.1 — Post-Change Gap Report

## O que foi resolvido

### ✅ Módulo backend dedicado

- `src/modules/integrations/` é agora um módulo backend completo com 5 layers:
  - Domain, Application, Infrastructure, Contracts, **API**
- Segue o padrão standard dos outros módulos (Notifications, Governance, etc.)

### ✅ IntegrationsDbContext

- Já existia desde P2.1/P2.2, continua funcional
- 3 entidades: `IntegrationConnector`, `IngestionSource`, `IngestionExecution`
- Tabelas com prefixo `int_` (isolamento por módulo)
- Multi-tenant, auditável, com outbox pattern

### ✅ Ownership backend transferido de Governance

- 8 CQRS handlers movidos de `Governance.Application` para `Integrations.Application`
- `IntegrationHubEndpointModule` movido de `Governance.API` para `Integrations.API`
- Wiring transitório `AddIntegrationsInfrastructure()` removido de `Governance.Infrastructure`
- Referência `Integrations.Application` removida de `Governance.Application.csproj`
- Referência `Integrations.Infrastructure` removida de `Governance.Infrastructure.csproj`

### ✅ Wiring e module registration

- `AddIntegrationsModule()` registado em `Program.cs`
- `AddIntegrationsApplication()` com MediatR handler scanning
- `AddIntegrationsInfrastructure()` com DbContext e repositórios
- API project referenciado em `ApiHost.csproj`
- API project adicionado ao `NexTraceOne.sln`

### ✅ Compatibilidade frontend preservada

- Rotas `/api/v1/integrations/*` e `/api/v1/ingestion/*` mantidas idênticas
- Sem breaking changes nos contratos HTTP
- Frontend `features/integrations/` funciona sem alteração

### ✅ Compilação e testes

- Build completo: 0 erros
- 163 governance tests passam (incluindo IntegrationHubFeatureTests)

---

## O que ficou pendente (para P8.2+)

### ⬜ EF Core Migrations

- Não existem migrations geradas para o `IntegrationsDbContext`
- É necessário gerar baseline migration com `dotnet ef migrations add`
- As tabelas `int_connectors`, `int_ingestion_sources`, `int_ingestion_executions` ainda não existem em DB

### ⬜ Tests dedicados no módulo Integrations

- Os testes de integração (IntegrationHubFeatureTests) ainda residem em `Governance.Tests`
- Recomenda-se criar `tests/modules/integrations/NexTraceOne.Integrations.Tests/` e mover os testes
- Não crítico para esta fase — os testes continuam a funcionar e validar o comportamento

### ⬜ Product Analytics (ainda em Governance)

- Product Analytics continua como catch-all transitório em Governance
- `Governance.Infrastructure` ainda chama `AddProductAnalyticsInfrastructure()`
- `Governance.Application` ainda referencia `ProductAnalytics.Application`
- Extração de ProductAnalytics está **fora de escopo** desta fase

### ⬜ Endpoint expansão

- Os 8 endpoints actuais cobrem read + retry/reprocess
- Faltam endpoints de criação, edição e eliminação de conectores
- Faltam endpoints de gestão avançada de fontes de ingestão
- Expansão prevista para fases posteriores

### ⬜ Integração com Event Bus

- Os `IntegrationEvents` definidos em `Integrations.Contracts` existem mas o módulo
  ainda não publica eventos via outbox pattern
- Necessário implementar domain event → integration event publishing

### ⬜ Seeder de dados de desenvolvimento

- Não existe seeder para dados de exemplo (conectores, fontes, execuções)
- Útil para ambiente Development e onboarding

### ⬜ Frontend CRUD completo

- Frontend actual é read-only com retry/reprocess
- Falta UI para criação e edição de conectores
- Falta UI de gestão de fontes de ingestão

---

## Limitações residuais

1. **IntegrationHubFeatureTests em Governance.Tests**: os testes de unidade para os handlers
   movidos ainda estão no projecto de testes de Governance. Funcionam correctamente porque
   referenciam os tipos via namespace `NexTraceOne.Integrations.Application.Features.*`.
   A mudança para `Integrations.Tests` é cosmética e não afecta funcionalidade.

2. **Sem migrations EF**: o `IntegrationsDbContext` está registado na wave de migrations
   (`WebApplicationExtensions.cs`), mas sem ficheiros de migration, o auto-migrate não
   cria as tabelas. Isto era pré-existente e não foi introduzido por P8.1.

3. **Ingestion.Api reference**: o projecto `NexTraceOne.Ingestion.Api` precisou de uma
   referência directa a `Integrations.Infrastructure` para resolver uma referência
   transitiva que anteriormente vinha via Governance. Isto é correcto arquitecturalmente.

---

## O que fica explicitamente para P8.2

| Item | Descrição |
|---|---|
| EF Migrations | Gerar baseline migration para `IntegrationsDbContext` |
| Testes dedicados | Criar `Integrations.Tests` e mover testes de Governance.Tests |
| Seeder | Criar seeder de dados de desenvolvimento para conectores e fontes |
| Endpoint expansão | Adicionar CRUD completo de conectores e fontes |
| Event publishing | Implementar publicação de integration events via outbox |
| Product Analytics extraction | Separar ProductAnalytics de Governance (escopo próprio) |
