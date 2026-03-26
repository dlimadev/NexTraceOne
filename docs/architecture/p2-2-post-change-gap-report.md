# P2.2 — Post-Change Gap Report

**Data:** 2026-03-26  
**Fase:** P2.2 — Extração de IngestionSource e IngestionExecution do GovernanceDbContext  
**Estado:** CONCLUÍDO COM LACUNAS RESIDUAIS CONTROLADAS

---

## 1. O Que Foi Resolvido

| Item | Estado |
|------|--------|
| `IngestionSource` removida de `GovernanceDbContext` | ✅ |
| `IngestionExecution` removida de `GovernanceDbContext` | ✅ |
| `IngestionSource` pertence agora ao módulo `Integrations` | ✅ |
| `IngestionExecution` pertence agora ao módulo `Integrations` | ✅ |
| Enums `SourceStatus`, `FreshnessStatus`, `SourceTrustLevel`, `ExecutionResult` movidos para `Integrations.Domain.Enums` | ✅ |
| `IIngestionSourceRepository` movida para `Integrations.Application` | ✅ |
| `IIngestionExecutionRepository` movida para `Integrations.Application` | ✅ |
| `IngestionSourceRepository` e `IngestionExecutionRepository` movidas para `Integrations.Infrastructure` | ✅ |
| `IngestionSourceConfiguration` e `IngestionExecutionConfiguration` movidas para `Integrations.Infrastructure` | ✅ |
| Navegações EF Core `HasOne<IntegrationConnector>()` restauradas (todas as entidades no mesmo DbContext) | ✅ |
| Referência temporária `Governance.Domain → Integrations.Domain` removida | ✅ |
| `GovernanceDbContext` ficou sem entidades de integrações (exceto `AnalyticsEvent`) | ✅ |
| Feature handlers actualizados para usar namespaces de Integrations | ✅ |
| `Ingestion.Api/Program.cs` actualizado | ✅ |
| Snapshot de migração `GovernanceDbContextModelSnapshot` actualizado | ✅ |
| `InitialCreate.Designer.cs` actualizado | ✅ |
| 163 testes Governance passam | ✅ |
| Solução compila com 0 erros | ✅ |

---

## 2. O Que Ficou Pendente

### Lacunas de Persistência / Migração

| Lacuna | Impacto | Quando Resolver |
|--------|---------|-----------------|
| Migração EF Core para `GovernanceDbContext` que reflecte remoção de `IngestionSource` e `IngestionExecution` | Médio — sem migração, `dotnet ef migrations add` geraria drop das tabelas | Antes do próximo baseline de migração |
| `InitialCreate` para `IntegrationsDbContext` atualizado com as 3 tabelas | Médio — necessário para novo ambiente criar as tabelas via Integrations | Antes do próximo deploy |
| Estratégia de migração para tabelas já existentes na BD | Alto — `int_ingestion_sources` e `int_ingestion_executions` já existem na BD criadas pelo baseline Governance E15 | Documentar e executar em ambiente controlado |

### Feature Handlers Ainda em Governance.Application

| Lacuna | Impacto | Quando Resolver |
|--------|---------|-----------------|
| `GetIngestionFreshness`, `GetIngestionHealth`, `ListIngestionSources`, `ListIngestionExecutions`, `ListIntegrationConnectors`, `GetIntegrationConnector`, `RetryConnector`, `ReprocessExecution` ainda em `Governance.Application` | Baixo — funcional, mas arquitectura não ideal | P2.3 |
| `IntegrationHubEndpointModule` ainda em `Governance.API` | Baixo — funcional, mas no módulo errado | P2.3 |

---

## 3. O Que Fica Explicitamente para P2.3

### Escopo de P2.3: Criação do Módulo Integrations.API e Migração de Handlers

1. Criar projecto `NexTraceOne.Integrations.Application` completo com feature handlers:
   - `ListIntegrationConnectors`
   - `GetIntegrationConnector`
   - `RetryConnector`
   - `ListIngestionSources`
   - `GetIngestionFreshness`
   - `GetIngestionHealth`
   - `ListIngestionExecutions`
   - `ReprocessExecution`
2. Criar projecto `NexTraceOne.Integrations.API` com endpoint module próprio
3. Migrar `IntegrationHubEndpointModule` de `Governance.API` para `Integrations.API`
4. Remover handlers de integrações de `Governance.Application`
5. Registar `Integrations.API` no `ApiHost` e remover de `Governance.API`
6. Gerar migrações EF Core formais para ambos os DbContexts

### Escopo OI-03 (fora de P2.3)

1. Extrair `AnalyticsEvent` de `GovernanceDbContext` para módulo próprio (Product Analytics)
2. Criar `ProductAnalyticsDbContext`
3. Após essa extracção, `GovernanceDbContext` ficará apenas com os 8 DbSets de Governance

---

## 4. Estado de Ownership das Entidades de Integrations

| Entidade | Módulo Owner | DbContext | Estado |
|----------|-------------|-----------|--------|
| `IntegrationConnector` | `Integrations` | `IntegrationsDbContext` | ✅ P2.1 |
| `IngestionSource` | `Integrations` | `IntegrationsDbContext` | ✅ P2.2 |
| `IngestionExecution` | `Integrations` | `IntegrationsDbContext` | ✅ P2.2 |
| `AnalyticsEvent` | ~~Governance~~ → ProductAnalytics | `GovernanceDbContext` (temp) | ⚠️ OI-03 |

---

## 5. Dependências entre Módulos Após P2.2

```
Integrations.Domain  (standalone — sem deps externas ao BuildingBlocks)
    ↑
Integrations.Application
    ↑
Integrations.Infrastructure
    ↑
Governance.Infrastructure  (via AddIntegrationsInfrastructure)
    ↑
Governance.Application     (referencia Integrations.Application para handlers mistos)
```

A dependência `Governance.Application → Integrations.Application` é aceitável e temporária — será resolvida em P2.3 quando os handlers forem migrados para `Integrations.Application`.

---

## 6. Limitações Residuais

| Limitação | Detalhe | Criticidade |
|-----------|---------|-------------|
| Feature handlers mistos em `Governance.Application` | Handlers de integrações ainda em Governance.Application | Baixa — funcional |
| `IntegrationHubEndpointModule` em `Governance.API` | Endpoint no módulo errado | Baixa — funcional |
| Migrações EF Core não geradas formalmente | Snapshots actualizados; migrações a gerar em ambiente de staging | Baixa — não afecta runtime |
| `Governance.Application` ainda referencia `Integrations.Application` | Necessário para os handlers mistos | Baixa — dependência explícita, a ser removida em P2.3 |
