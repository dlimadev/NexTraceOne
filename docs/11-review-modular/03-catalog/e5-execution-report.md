# E5 — Service Catalog Module Execution Report

## Data de Execução
2026-03-25

## Resumo
Execução real de correções no módulo Service Catalog conforme a trilha N.
Todas as alterações alinham o módulo ao seu papel como fonte de verdade dos ativos,
adicionam concorrência otimista, unificam prefixo de tabelas e adicionam validação de lifecycle.

---

## Ficheiros de Código Alterados

### Domain — Entidades
| Ficheiro | Alteração |
|----------|-----------|
| `ServiceAsset.cs` | Adicionado RowVersion (uint xmin). Adicionado `TransitionTo(LifecycleStatus)` com validação de transições. Adicionada import de `Errors`. |
| `ApiAsset.cs` | Adicionado RowVersion (uint xmin). |
| `CatalogGraphErrors.cs` | Adicionado `InvalidLifecycleTransition()` error para transições inválidas. |

### Persistence — EF Core Configurations
| Ficheiro | Alteração |
|----------|-----------|
| `ServiceAssetConfiguration.cs` | Tabela `eg_service_assets` → `cat_service_assets`. Adicionado `IsRowVersion()`. Adicionados 4 check constraints (ServiceType, Criticality, LifecycleStatus, ExposureType). |
| `ApiAssetConfiguration.cs` | Tabela `eg_api_assets` → `cat_api_assets`. Adicionado `IsRowVersion()`. |
| `ConsumerAssetConfiguration.cs` | Tabela `eg_consumer_assets` → `cat_consumer_assets`. |
| `ConsumerRelationshipConfiguration.cs` | Tabela `eg_consumer_relationships` → `cat_consumer_relationships`. |
| `DiscoverySourceConfiguration.cs` | Tabela `eg_discovery_sources` → `cat_discovery_sources`. |
| `GraphSnapshotConfiguration.cs` | Tabela `graph_snapshots` → `cat_graph_snapshots`. |
| `NodeHealthRecordConfiguration.cs` | Tabela `node_health_records` → `cat_node_health_records`. |
| `SavedGraphViewConfiguration.cs` | Tabela `saved_graph_views` → `cat_saved_graph_views`. |
| `LinkedReferenceConfiguration.cs` | Tabela `sot_linked_references` → `cat_linked_references`. |
| `SubscriptionConfiguration.cs` | Tabela `dp_subscriptions` → `cat_subscriptions`. |
| `PlaygroundSessionConfiguration.cs` | Tabela `dp_playground_sessions` → `cat_playground_sessions`. |
| `CodeGenerationRecordConfiguration.cs` | Tabela `dp_code_generation_records` → `cat_code_generation_records`. |
| `PortalAnalyticsEventConfiguration.cs` | Tabela `dp_portal_analytics_events` → `cat_portal_analytics_events`. |
| `SavedSearchConfiguration.cs` | Tabela `dp_saved_searches` → `cat_saved_searches`. |

### Persistence — DbContexts
| Ficheiro | Alteração |
|----------|-----------|
| `CatalogGraphDbContext.cs` | OutboxTableName: `eg_outbox_messages` → `cat_outbox_messages`. |
| `DeveloperPortalDbContext.cs` | OutboxTableName: `dp_outbox_messages` → `cat_portal_outbox_messages`. |

### Documentação
| Ficheiro | Alteração |
|----------|-----------|
| `src/modules/catalog/README.md` | **CRIADO** — README completo com escopo, arquitetura, entidades, lifecycle, DB, permissões, consumidores. |

---

## Correções por Parte

### PART 1 — Papel do Módulo (Source of Truth)
- ✅ README documenta claramente que Catalog é owner dos ativos
- ✅ ServiceAsset tem lifecycle validation nativo
- ✅ Todas tabelas com prefixo `cat_` refletem identidade do módulo

### PART 2 — Fronteira Catalog vs Contracts
- ✅ ContractsDbContext permanece com prefixo `ctr_` (corrigido em E2)
- ✅ README documenta que Contracts é subdomain com fronteira definida
- ⏳ Extração completa (OI-01) → Wave 0

### PART 3 — Domínio
- ✅ RowVersion (uint) adicionado a ServiceAsset e ApiAsset
- ✅ `TransitionTo(LifecycleStatus)` adicionado a ServiceAsset com validação completa
- ✅ Transições: Planning→Development→Staging→Active→Deprecating→Deprecated→Retired
- ✅ Staging pode voltar a Development, Deprecating pode voltar a Active
- ✅ `InvalidLifecycleTransition` error adicionado

### PART 4 — Persistência
- ✅ 14 tabelas renomeadas para `cat_` prefix
- ✅ 2 outbox tables renomeadas para `cat_` prefix
- ✅ `IsRowVersion()` xmin em ServiceAsset + ApiAsset
- ✅ 4 check constraints em `cat_service_assets`

### PART 5 — Backend
- ✅ Permissões já granulares: `catalog:assets:read/write`, `developer-portal:read/write`, `contracts:read/write/import`
- ⏳ TransitionAssetLifecycle handler → E5+

### PART 6 — Frontend
- ✅ Sidebar já usa `catalog:assets:read` (verificado)
- ⏳ Orphaned pages cleanup → E5+

### PART 7 — Segurança
- ✅ Permissions already granular across all endpoints
- ✅ Verificado: 5 endpoint modules com permissões corretas

### PART 8 — Dependências
- ✅ Documentado no README: Contracts, ChangeGov, OpIntel, AI&Knowledge consomem Catalog

### PART 9 — Documentação
- ✅ README.md criado com conteúdo completo

---

## Validação

- ✅ Build: 0 erros
- ✅ 468 testes Catalog: todos passam
- ✅ Sem migrations antigas removidas
- ✅ Sem nova baseline gerada

---

## Classes Alteradas

| Classe | Tipo de Alteração |
|--------|-------------------|
| `ServiceAsset` | RowVersion (uint xmin), `TransitionTo()` lifecycle validation |
| `ApiAsset` | RowVersion (uint xmin) |
| `CatalogGraphErrors` | `InvalidLifecycleTransition()` error |
| `ServiceAssetConfiguration` | `cat_` prefix, IsRowVersion(), 4 check constraints |
| `ApiAssetConfiguration` | `cat_` prefix, IsRowVersion() |
| `ConsumerAssetConfiguration` | `cat_` prefix |
| `ConsumerRelationshipConfiguration` | `cat_` prefix |
| `DiscoverySourceConfiguration` | `cat_` prefix |
| `GraphSnapshotConfiguration` | `cat_` prefix |
| `NodeHealthRecordConfiguration` | `cat_` prefix |
| `SavedGraphViewConfiguration` | `cat_` prefix |
| `LinkedReferenceConfiguration` | `cat_` prefix |
| `SubscriptionConfiguration` | `cat_` prefix |
| `PlaygroundSessionConfiguration` | `cat_` prefix |
| `CodeGenerationRecordConfiguration` | `cat_` prefix |
| `PortalAnalyticsEventConfiguration` | `cat_` prefix |
| `SavedSearchConfiguration` | `cat_` prefix |
| `CatalogGraphDbContext` | `cat_outbox_messages` |
| `DeveloperPortalDbContext` | `cat_portal_outbox_messages` |
