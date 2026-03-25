# NexTraceOne — Service Catalog Module

## Visão Geral

O módulo Service Catalog é a fonte de verdade dos ativos no NexTraceOne.
Ele é responsável pelo registo, classificação, ownership e rastreabilidade
de todos os serviços, APIs, consumidores, dependências e referências cruzadas.

## Escopo do Módulo

### O que PERTENCE ao Service Catalog:
- Serviços (ServiceAsset) — entidade canónica de serviço
- APIs (ApiAsset) — entidade canónica de API com proprietário
- Consumidores (ConsumerAsset, ConsumerRelationship) — relações de consumo
- Descoberta (DiscoverySource) — fontes de descoberta de ativos
- Topologia (GraphSnapshot) — snapshots temporais do grafo
- Saúde (NodeHealthRecord) — overlay de saúde dos nós
- Vistas (SavedGraphView) — vistas filtradas do grafo
- Referências cruzadas (LinkedReference) — Source of Truth cross-references
- Portal do Desenvolvedor (Subscription, PlaygroundSession, etc.)

### O que NÃO PERTENCE ao Service Catalog:
- **Contratos** (ContractVersion, ContractDiff, etc.) → Módulo Contracts (subdomain do Catalog, OI-01)
- **Change Intelligence** → Módulo Change Governance
- **Incidentes & Observabilidade** → Módulo Operational Intelligence
- **Governança de políticas** → Módulo Governance

## Arquitetura

```
NexTraceOne.Catalog.Domain/
├── Graph/              → 8 entidades, 9 enums, erros
├── Portal/             → 5 entidades, 1 enum
├── SourceOfTruth/      → 1 entidade, 2 enums
└── Contracts/          → 13 entidades (temporário, OI-01)

NexTraceOne.Catalog.Infrastructure/
├── Graph/Persistence/  → CatalogGraphDbContext (9 DbSets)
├── Portal/Persistence/ → DeveloperPortalDbContext (5 DbSets)
└── Contracts/Persistence/ → ContractsDbContext (11 DbSets)

NexTraceOne.Catalog.API/
├── Graph/Endpoints/           → ServiceCatalogEndpointModule
├── Portal/Endpoints/          → DeveloperPortalEndpointModule
├── SourceOfTruth/Endpoints/   → SourceOfTruthEndpointModule
└── Contracts/Endpoints/       → ContractsEndpointModule, ContractStudioEndpointModule
```

## Aggregate Roots

| Entidade | Responsabilidade |
|----------|-----------------|
| `ServiceAsset` | Serviço canónico com lifecycle (Planning→...→Retired), ownership e classificação |
| `ApiAsset` | API publicada com consumidores, fontes de descoberta e descomissionamento |

## Regras de Negócio

### ServiceAsset Lifecycle
- `TransitionTo()` — Valida transições: Planning→Development→Staging→Active→Deprecating→Deprecated→Retired
- Staging pode voltar a Development; Deprecating pode voltar a Active
- Transições inválidas retornam `InvalidLifecycleTransition` error

### ApiAsset
- `Decommission()` — Marca como descomissionado (irreversível)
- `UpdateMetadata()` — Bloqueado após descomissionamento
- `MapConsumerRelationship()` — Mapeia/atualiza relações de consumo
- `InferDependencyFromOtel()` — Infere dependência a partir de OpenTelemetry

## Base de Dados

### Tabelas (prefixo cat_)
| Tabela | Entidade | DbContext |
|--------|---------|-----------|
| `cat_service_assets` | ServiceAsset | CatalogGraphDbContext |
| `cat_api_assets` | ApiAsset | CatalogGraphDbContext |
| `cat_consumer_assets` | ConsumerAsset | CatalogGraphDbContext |
| `cat_consumer_relationships` | ConsumerRelationship | CatalogGraphDbContext |
| `cat_discovery_sources` | DiscoverySource | CatalogGraphDbContext |
| `cat_graph_snapshots` | GraphSnapshot | CatalogGraphDbContext |
| `cat_node_health_records` | NodeHealthRecord | CatalogGraphDbContext |
| `cat_saved_graph_views` | SavedGraphView | CatalogGraphDbContext |
| `cat_linked_references` | LinkedReference | CatalogGraphDbContext |
| `cat_subscriptions` | Subscription | DeveloperPortalDbContext |
| `cat_playground_sessions` | PlaygroundSession | DeveloperPortalDbContext |
| `cat_code_generation_records` | CodeGenerationRecord | DeveloperPortalDbContext |
| `cat_portal_analytics_events` | PortalAnalyticsEvent | DeveloperPortalDbContext |
| `cat_saved_searches` | SavedSearch | DeveloperPortalDbContext |

### Concorrência Otimista
PostgreSQL xmin via `RowVersion` em: ServiceAsset, ApiAsset.

### Check Constraints
- `CK_cat_service_assets_service_type`: ServiceType values
- `CK_cat_service_assets_criticality`: Criticality values
- `CK_cat_service_assets_lifecycle_status`: LifecycleStatus values
- `CK_cat_service_assets_exposure_type`: ExposureType values

## Permissões

| Permissão | Escopo |
|-----------|--------|
| `catalog:assets:read` | Consultar serviços, APIs, grafo, topologia |
| `catalog:assets:write` | Criar/editar serviços, APIs, importar, decommission |
| `developer-portal:read` | Consultar portal do desenvolvedor |
| `developer-portal:write` | Subscrições, playground, geração de código |

## Módulos Consumidores

| Módulo | Relação com Catalog |
|--------|--------------------|
| Contracts | Referencia ApiAsset.Id via ContractVersion.ApiAssetId |
| Change Governance | Consome ServiceAsset.Id para eventos de mudança |
| Operational Intelligence | Consulta ICatalogGraphModule para topologia |
| AI & Knowledge | Consulta ICatalogGraphModule para contexto |
