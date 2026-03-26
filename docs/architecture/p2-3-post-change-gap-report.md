# P2.3 — Post-Change Gap Report

**Data:** 2026-03-26  
**Fase:** P2.3 — Extração de AnalyticsEvent do GovernanceDbContext  
**Estado:** CONCLUÍDO COM LACUNAS RESIDUAIS CONTROLADAS

---

## 1. O Que Foi Resolvido

| Item | Estado |
|------|--------|
| `AnalyticsEvent` removida de `GovernanceDbContext` | ✅ |
| `AnalyticsEvent` pertence agora ao módulo `ProductAnalytics` | ✅ |
| `ProductModule` enum pertence agora a `ProductAnalytics.Domain` | ✅ |
| `AnalyticsEventType` enum pertence agora a `ProductAnalytics.Domain` | ✅ |
| `IAnalyticsEventRepository` movida para `ProductAnalytics.Application` | ✅ |
| DTOs analíticos movidos para `ProductAnalytics.Application` | ✅ |
| `AnalyticsEventRepository` movida para `ProductAnalytics.Infrastructure` | ✅ |
| `AnalyticsEventConfiguration` movida para `ProductAnalytics.Infrastructure` | ✅ |
| `ProductAnalyticsDbContext` criado como DbContext dedicado | ✅ |
| `GovernanceDbContext` contém agora apenas 8 DbSets puros de Governance | ✅ |
| Feature handlers actualizados para usar namespaces `ProductAnalytics.*` | ✅ |
| `ProductAnalyticsDatabase` connection string adicionada ao `appsettings.json` | ✅ |
| Snapshot de migração `GovernanceDbContextModelSnapshot` actualizado | ✅ |
| `InitialCreate.Designer.cs` actualizado | ✅ |
| 163 testes Governance passam | ✅ |
| 68 testes Infrastructure passam (AppSettingsSecurityTests actualizado) | ✅ |
| Solução compila com 0 erros | ✅ |

---

## 2. Estado do GovernanceDbContext Após P2.3

`GovernanceDbContext` contém agora exclusivamente o domínio de Governance:

| DbSet | Entidade | Estado |
|-------|---------|--------|
| `Teams` | `Team` | ✅ Governance |
| `Domains` | `GovernanceDomain` | ✅ Governance |
| `Packs` | `GovernancePack` | ✅ Governance |
| `PackVersions` | `GovernancePackVersion` | ✅ Governance |
| `Waivers` | `GovernanceWaiver` | ✅ Governance |
| `DelegatedAdministrations` | `DelegatedAdministration` | ✅ Governance |
| `TeamDomainLinks` | `TeamDomainLink` | ✅ Governance |
| `RolloutRecords` | `GovernanceRolloutRecord` | ✅ Governance |

**GovernanceDbContext está limpo. Todos os bounded context estranhos foram removidos.**

---

## 3. O Que Ficou Pendente

### Feature Handlers Ainda em Governance.Application

| Lacuna | Impacto | Quando Resolver |
|--------|---------|-----------------|
| `RecordAnalyticsEvent`, `GetAnalyticsSummary`, `GetModuleAdoption`, `GetFrictionIndicators`, `GetPersonaUsage` ainda em `Governance.Application` | Baixo — funcional, mas no módulo errado | P2.4 |
| `ProductAnalyticsEndpointModule` ainda em `Governance.API` | Baixo — funcional, mas no módulo errado | P2.4 |

### Dependências Temporárias

| Lacuna | Detalhe | Quando Resolver |
|--------|---------|-----------------|
| `Governance.Application` → `ProductAnalytics.Application` | Necessária para handlers mistos | P2.4 (ao migrar handlers) |
| `Governance.Infrastructure` → `ProductAnalytics.Infrastructure` | Necessária para DI wiring | P2.4 (ao criar ProductAnalytics.API independente) |
| Alguns handlers de Governance usam `Governance.Domain.Enums` + `ProductAnalytics.Domain.Enums` simultaneamente | Dependência dupla enquanto handlers permanecem em Governance | P2.4 |

### Persistência / Migração

| Lacuna | Impacto | Quando Resolver |
|--------|---------|-----------------|
| `InitialCreate` para `ProductAnalyticsDbContext` não gerado | Médio — necessário para criar tabela em novo ambiente | P2.4 ou próximo baseline |
| Migração formal para `GovernanceDbContext` que reflecte remoção de `AnalyticsEvent` | Médio | Próximo baseline de migração |

---

## 4. O Que Fica Explicitamente para P2.4

### Escopo de P2.4: Criação de ProductAnalytics.Application e ProductAnalytics.API

1. Criar feature handlers em `ProductAnalytics.Application`:
   - `RecordAnalyticsEvent`
   - `GetAnalyticsSummary`
   - `GetModuleAdoption`
   - `GetFrictionIndicators`
   - `GetPersonaUsage`
2. Criar projecto `ProductAnalytics.API` com endpoint module próprio
3. Migrar `ProductAnalyticsEndpointModule` de `Governance.API` para `ProductAnalytics.API`
4. Remover handlers de analytics de `Governance.Application`
5. Registar `ProductAnalytics.API` no `ApiHost`
6. Remover dependências `Governance → ProductAnalytics` após migração de handlers
7. Gerar `InitialCreate` migration para `ProductAnalyticsDbContext`

---

## 5. Resumo do Estado das Extrações da Fase 2

| Extracção | Fase | Estado |
|-----------|------|--------|
| `IntegrationConnector` | P2.1 | ✅ Completo |
| `IngestionSource` | P2.2 | ✅ Completo |
| `IngestionExecution` | P2.2 | ✅ Completo |
| `AnalyticsEvent` | P2.3 | ✅ Completo |
| Handlers de Integrations para `Integrations.Application` | P2.3 (pendente) | ⚠️ Pendente P2.4 |
| Handlers de Analytics para `ProductAnalytics.Application` | P2.4 | ⚠️ Pendente |
| `GovernanceDbContext` limpo (apenas 8 DbSets de Governance) | P2.3 | ✅ Alcançado |

---

## 6. Limitações Residuais

| Limitação | Detalhe | Criticidade |
|-----------|---------|-------------|
| `Governance.Application` ainda usa `ProductAnalytics.Domain.Enums` | Dependência cruzada temporária aceite | Baixa |
| `TrendDirection`, `FrictionSignalType`, `ValueMilestoneType` permanecem em `Governance.Domain.Enums` | Estes enums são de Governance, não de Analytics. Correcto permanecerem em Governance. | N/A |
| Migrações EF Core formais não geradas | Snapshots actualizados; migrações a gerar em staging | Baixa |
| `ProductAnalytics.API` não existe | Frontend usa `Governance.API` para analytics | Baixa — funcional |
