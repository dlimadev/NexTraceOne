# Plano 04 — Infrastructure Evolution (4 Fases)

> **Prioridade:** Média  
> **Esforço total:** 9–13 semanas  
> **Spec técnica:** [analysis/INFRA-EVOLUTION-OVERVIEW.md](../analysis/INFRA-EVOLUTION-OVERVIEW.md)  
> **Contexto:** Preparar a plataforma para biliões de eventos, on-premise distribuído e visibilidade completa de infraestrutura.
> **Estado (Maio 2026):** Fase 1 PARCIAL (Redis IDistributedCache implementado; PgBouncer, particionamento e read replica pendentes) | Fase 2 PARCIAL (ClickHouseAnalyticsWriter interface existe, implementação stub) | Fase 3 NAO IMPLEMENTADO | Fase 4 NAO IMPLEMENTADO

---

## Fase 1 — PostgreSQL Hardening (2–3 semanas)

**Objetivo:** Remover gargalos de base de dados identificados: pool de 20 conexões, tabelas sem particionamento, ausência de cache e sem read replica.

### F1.1 — PgBouncer Connection Pooling

**Problema:** `Maximum Pool Size=20` em `appsettings` — insuficiente para produção multi-tenant.

**Implementação:**
1. Adicionar `pgbouncer` ao `docker-compose.yml` e `docker-compose.production.yml`
2. Configurar transaction pooling mode (compatível com EF Core)
3. Aumentar pool size para 100 (PgBouncer → PostgreSQL) com client pool de 25 por instância
4. Config: `Database:ConnectionString` aponta para PgBouncer em produção
5. Health check: `PgBouncerHealthCheck` no startup

**Ficheiros:**
- `docker-compose.production.yml` (adicionar pgbouncer service)
- `build/pgbouncer/pgbouncer.ini` (novo)
- `docs/deployment/DOCKER-AND-COMPOSE.md` (atualizar)

### F1.2 — Particionamento de Tabelas de Alto Volume

**Problema:** Tabelas `audit_events`, `telemetry_product_store`, `analytics_events` crescem sem particionamento.

**Implementação:**
1. `audit_events`: partition by RANGE on `created_at` (monthly partitions, 3 anos de retenção)
2. `pa_analytics_events`: partition by RANGE on `occurred_at` (monthly, 1 ano)
3. `bb_dead_letter_messages`: partition by RANGE on `exhausted_at` (quarterly)
4. Migrations EF Core: usar `NpgsqlMigrationsSqlGenerator` para DDL de partições
5. Quartz job `PartitionMaintenanceJob`: cria partições futuras e descarta antigas (mensal)

### F1.3 — Read Replica para Queries Pesadas

**Implementação:**
1. Novo `ReadReplicaDbContext` base que aponta para connection string de réplica
2. `IReadReplicaSelector` service: decide quando usar réplica (queries GET sem tenant write-after-read)
3. Módulos elegíveis para read replica: Governance reports, ProductAnalytics, Catalog search, AuditCompliance trail
4. Fallback para primary se réplica indisponível

### F1.4 — Redis Cache (IDistributedCache)

**Implementação:**
1. Adicionar `StackExchange.Redis` (MIT license ✅) — ou `Microsoft.Extensions.Caching.StackExchangeRedis`
2. Implementar cache para: tenant config lookups (TTL 5min), ServiceAsset by name (TTL 2min), OptionalProviders status (TTL 1min)
3. `docker-compose.yml`: adicionar Redis service
4. Fallback gracioso: se Redis indisponível → IMemoryCache in-process

---

## Fase 2 — ClickHouse como Provider Principal de Observabilidade (3–4 semanas)

**Objetivo:** Usar ClickHouse como backend primário para workloads analíticos de alto volume, relegando Elasticsearch para full-text search.

### F2.1 — ClickHouseAnalyticsWriter Real

**Estado atual:** `IClickHouseAnalyticsWriter` interface existe; implementação é stub.

**Implementação:**
1. Usar driver `ClickHouse.Client` (Apache-2.0 ✅) para .NET
2. Schema ClickHouse:
   - `traces` (MergeTree, `ORDER BY (tenant_id, service_name, timestamp)`, partition by `toYYYYMM(timestamp)`)
   - `metrics` (SummingMergeTree, TTL 90 dias por defeito)
   - `logs` (MergeTree com TTL configurável por `StorageBucket`)
3. Batch writer: buffer de 1000 eventos ou flush a cada 5s (config `analytics.clickhouse.batch_size`)
4. Health check: `ClickHouseHealthCheck`

### F2.2 — Migrar TelemetryStore Snapshots para ClickHouse

**Implementação:**
1. `TelemetryStoreClickHouseWriter` implementa `ITelemetryWriterService`
2. `TelemetryStoreClickHouseReader` implementa `ITelemetryQueryService`
3. Routing: `appsettings Analytics:Provider = "ClickHouse"` ativa novo provider
4. Fallback: se ClickHouse indisponível → Elasticsearch → PostgreSQL Product Store

### F2.3 — Migrar ProductAnalytics Events para ClickHouse

**Implementação:**
1. `ProductAnalyticsClickHouseWriter` — ingestão de analytics events em batch
2. `ProductAnalyticsClickHouseReader` — queries de persona usage, journeys, funnels
3. Benefício: reduz load no PostgreSQL principal ~30%

---

## Fase 3 — Host Infrastructure Layer (2–3 semanas)

**Objetivo:** Adicionar visibilidade de hosts (CPU, RAM, disco, rede) como camada fundamental, tornando o NexTraceOne competitivo com Dynatrace Host View.

### F3.1 — HostAsset Entity

**Implementação:**
1. Entidade `HostAsset` em `Catalog.Domain` (ou novo módulo `HostIntelligence`):
   - `HostName`, `IpAddresses`, `Os`, `CpuCores`, `RamGb`, `HostUnitId` (UUID estável, do NexTrace Agent)
   - `HostStatus`: Active | Unreachable | Decommissioned
2. `ServiceDeployment` entity: relação `HostAsset ←→ ServiceAsset` com `DeployedAt`, `Environment`
3. Edges `DeployedTo` no service graph (usa `EdgeType` já existente)
4. Migration: tabelas `cat_host_assets`, `cat_service_deployments`

### F3.2 — HostMetricsSnapshot via OTel hostmetrics

**Implementação:**
1. `HostMetricsSnapshot` entity: snapshot de CPU/RAM/disco/rede por host a cada 60s
2. `HostMetricsIngestionJob` (Quartz): lê métricas de host do Elasticsearch/ClickHouse (via `system.cpu.*`, `system.memory.*`) e persiste snapshots
3. `GetHostMetrics` query: série temporal de métricas por host/período
4. Endpoint: `GET /api/v1/catalog/hosts/{hostId}/metrics`

### F3.3 — HostAsset UI

**Implementação:**
1. `HostsPage.tsx` (`/catalog/hosts`): lista de hosts com status, CPU%, RAM%, serviços deployados
2. `HostDetailPage.tsx` (`/catalog/hosts/:id`): métricas em tempo real + serviços + histórico
3. Widget "Host Health" para Custom Dashboards

---

## Fase 4 — Topology Completions (2–3 semanas)

**Objetivo:** Completar funcionalidades de topologia identificadas como gaps.

### F4.1 — Time-Travel UI no Grafo

**Estado atual:** `GetLatestTopologySnapshot` existe; falta UI de navegação temporal.

**Implementação:**
1. Slider temporal no `CatalogGraphPage.tsx`: navegar entre snapshots históricos do grafo
2. `GetTopologySnapshotHistory` query: lista snapshots disponíveis com timestamps
3. Diff visual: realçar nós/arestas adicionados/removidos entre dois snapshots

### F4.2 — Push de Propagation Risk via SignalR

**Estado atual:** `GetCanonicalEntityImpactCascade` é query on-demand.

**Implementação:**
1. `PropagationRiskHub` (SignalR): quando um serviço tem mudança de status → broadcast propagation risk para clientes subscritos
2. Frontend: `usePropagationRisk(serviceId)` hook com SignalR subscription

### F4.3 — Continuous Service Discovery

**Estado atual:** Registo de serviços é manual via `RegisterServiceAsset`.

**Implementação:**
1. `ServiceDiscoveryJob` (Quartz, a cada 5min): analisa traces ingeridos nas últimas 24h, identifica `service.name` não registados no Catalog
2. `UncatalogedServicesReport` (já existe — Wave AM.1): alimentado por este job
3. `AutoRegisterCandidate` entity: serviço sugerido com base em telemetria (requer aprovação manual)

---

## Pré-requisitos e Dependências

| Item | Pré-requisito |
|------|--------------|
| F1.1 PgBouncer | Docker Compose production overlay |
| F1.2 Particionamento | PostgreSQL 16+ (já em uso) |
| F1.3 Read Replica | PostgreSQL streaming replication configurado |
| F1.4 Redis | Redis 7.2 (última versão com BSD license ✅) |
| F2.x ClickHouse | ClickHouse 24.x cluster ou single-node |
| F3.x Host Infrastructure | NexTrace Agent com `hostmetrics` receiver ativo |
| F4.x Topology | Fases 1–3 concluídas |

## Critérios de Aceite (estado Maio 2026)

- [ ] `dotnet test` continua 100% pass com PgBouncer ativo (PgBouncer pendente — F1.1)
- [ ] Connection pool errors eliminados em load test de 100 req/s (PgBouncer pendente)
- [ ] Analytics queries 5x mais rápidas em ClickHouse vs PostgreSQL para datasets > 1M rows (F2.x pendente)
- [ ] Host list mostra CPU/RAM de todos os hosts com NexTrace Agent ativo (F3.x pendente)
- [ ] Time-travel no grafo navega entre snapshots dos últimos 30 dias (F4.1 pendente)

**Nota:** Redis `IDistributedCache` já implementado (F1.4 concluido). `IClickHouseAnalyticsWriter` interface
existe mas implementação é stub (DEG-14 em HONEST-GAPS.md). Restantes fases são roadmap.
