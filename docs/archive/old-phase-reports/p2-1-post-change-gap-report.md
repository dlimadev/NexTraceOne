# P2.1 — Post-Change Gap Report

**Data:** 2026-03-26  
**Fase:** P2.1 — Extração de IntegrationConnector do GovernanceDbContext  
**Estado:** CONCLUÍDO COM LACUNAS RESIDUAIS CONTROLADAS

---

## 1. O Que Foi Resolvido

| Item | Estado |
|------|--------|
| `IntegrationConnector` removido de `GovernanceDbContext` | ✅ Concluído |
| `IntegrationConnector` pertence agora ao módulo `Integrations` | ✅ Concluído |
| `IIntegrationConnectorRepository` movida para `Integrations.Application` | ✅ Concluído |
| `IntegrationConnectorRepository` movida para `Integrations.Infrastructure` | ✅ Concluído |
| `IntegrationsDbContext` criado com ownership correcto | ✅ Concluído |
| `IntegrationConnectorConfiguration` movida para Integrations | ✅ Concluído |
| Navegações EF Core cross-DbContext removidas de `IngestionSource` e `IngestionExecution` configs | ✅ Concluído |
| Feature handlers actualizados para usar namespaces de Integrations | ✅ Concluído |
| `Ingestion.Api/Program.cs` actualizado | ✅ Concluído |
| Snapshot de migração `GovernanceDbContextModelSnapshot` actualizado | ✅ Concluído |
| `InitialCreate.Designer.cs` actualizado | ✅ Concluído |
| Connection string `IntegrationsDatabase` adicionada a appsettings | ✅ Concluído |
| 163 testes Governance passam | ✅ Concluído |
| Solução compila com 0 erros | ✅ Concluído |

---

## 2. O Que Ficou Pendente (Não Incluído no Escopo de P2.1)

### Lacunas de Persistência / Migração

| Lacuna | Impacto | Quando Resolver |
|--------|---------|-----------------|
| Migração EF Core para `GovernanceDbContext` que reflecte remoção de `IntegrationConnector` do modelo | Médio — sem a migração, `dotnet ef migrations add` geraria uma migração de drop da tabela | P2.2 ou imediatamente antes do próximo baseline de migração |
| `InitialCreate` para `IntegrationsDbContext` | Médio — sem migração, novo ambiente não cria a tabela via Integrations | P2.2 ou imediatamente antes do próximo deploy |
| Estratégia de migração em BD existente (tabela `int_connectors` já existe, criada pelo baseline Governance E15) | Alto — requer cuidado para não tentar criar tabela que já existe | P2.2 — documentar e executar em ambiente controlado |

### Referência Cross-Module Temporária

| Lacuna | Impacto | Quando Resolver |
|--------|---------|-----------------|
| `Governance.Domain` referencia `Integrations.Domain` para `IntegrationConnectorId` | Baixo — dependência documentada, sem problemas de compilação | P2.2 — quando `IngestionSource` e `IngestionExecution` forem extraídas |
| `Governance.Application` referencia `Integrations.Application` para `IIntegrationConnectorRepository` | Baixo — dependência aceitável, handlers mixtos ainda em Governance | P2.2 — quando handlers de conectores migrarem para Integrations.Application |
| Handlers mistos em `Governance.Application` (ex: `GetIntegrationConnector` usa tanto repos de Governance como de Integrations) | Médio — arquitectura não ideal, mas funcional | P2.2 — mover handlers de conectores para Integrations.Application |

---

## 3. O Que Fica Explicitamente para P2.2

### Escopo de P2.2: Extracção de IngestionSource e IngestionExecution

1. Mover `IngestionSource` para `Integrations.Domain`
2. Mover `IngestionExecution` para `Integrations.Domain`
3. Mover `IIngestionSourceRepository`, `IIngestionExecutionRepository` para `Integrations.Application`
4. Mover `IngestionSourceRepository`, `IngestionExecutionRepository` para `Integrations.Infrastructure`
5. Adicionar `IngestionSources` e `IngestionExecutions` ao `IntegrationsDbContext`
6. Remover os mesmos de `GovernanceDbContext`
7. Mover enums de Integrations (`SourceStatus`, `FreshnessStatus`, `SourceTrustLevel`, `ExecutionResult`) para `Integrations.Domain.Enums`
8. Remover dependência `Governance.Domain → Integrations.Domain` (será invertida correctamente)
9. Gerar migração nova para ambos os DbContexts
10. Mover os feature handlers mistos (`GetIntegrationConnector`, `GetIngestionHealth`, etc.) para `Integrations.Application`

---

## 4. O Que Fica Explicitamente para P2.3 (e além)

### Endpoints e Endpoint Modules

1. `IntegrationHubEndpointModule` ainda vive no módulo `Governance.API` — deve ser movido para `Integrations.API`
2. Criar projecto `NexTraceOne.Integrations.API` como endpoint module próprio
3. Registar `Integrations.API` no `ApiHost` e remover de `Governance.API`
4. Migrar `ListIntegrationConnectors`, `GetIntegrationConnector`, `RetryConnector`, `GetIngestionHealth`, etc. para o novo endpoint module

### Product Analytics (OI-03)

1. `AnalyticsEvent` ainda reside em `GovernanceDbContext` — escopo fora de P2.1
2. Extracção para módulo próprio fica para OI-03 ou fase dedicada

### Migração de dados / Tabelas

1. Executar e validar migração EF completa em ambiente de staging
2. Garantir que `int_connectors` é gerida exclusivamente por `IntegrationsDbContext` após migração

---

## 5. Limitações Residuais Após a Extracção

| Limitação | Detalhe | Criticidade |
|-----------|---------|-------------|
| Navegação EF Core entre `IngestionSource` e `IntegrationConnector` perdida | `HasOne<IntegrationConnector>` removido das configs EF. A relação existe na BD mas não em código. Queries que precisem de JOIN manual devem ser feitas em dois DbContexts. | Média — funcional mas menos eficiente |
| Snapshot de migração desactualizado vs InitialCreate actual | O `InitialCreate.cs` original ainda contém `int_connectors`. O snapshot foi corrigido. A divergência só afecta `dotnet ef migrations add`. | Baixa — não afecta runtime |
| `IntegrationHubEndpointModule` em Governance.API | Endpoint module do lado errado do bounded context | Baixa — funcional, será corrigido em P2.3 |
| Handlers com dependências cruzadas em `Governance.Application` | `GetIntegrationConnector`, `GetIngestionHealth`, etc. usam repos de ambos os módulos | Baixa — funcional, será corrigido em P2.2 |

---

## 6. Sumário de Saúde do Módulo Integrations Após P2.1

| Componente | Estado |
|------------|--------|
| `Integrations.Domain` | ✅ Criado — `IntegrationConnector`, `ConnectorStatus`, `ConnectorHealth` |
| `Integrations.Application` | ✅ Criado — `IIntegrationConnectorRepository` |
| `Integrations.Infrastructure` | ✅ Criado — `IntegrationsDbContext`, configuração, repositório, DI |
| `Integrations.API` | ❌ Não criado (escopo P2.3) |
| `Integrations.Contracts` | ❌ Não criado (escopo futuro) |
| `IngestionSource` e `IngestionExecution` | ⚠️ Ainda em Governance (escopo P2.2) |
| `AnalyticsEvent` | ⚠️ Ainda em Governance (escopo OI-03) |
| Migrações EF Core | ⚠️ Pendentes de geração formal (documentadas acima) |
| Testes unitários do módulo | ⚠️ Testes de Governance cobrem features; testes dedicados a Integrations a criar em P2.2 |
