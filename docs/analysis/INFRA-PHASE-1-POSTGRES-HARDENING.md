# Fase 1 — PostgreSQL Hardening

> **Duração estimada:** 2–3 semanas
> **Pré-requisitos:** Nenhum — pode começar imediatamente
> **Impacto:** Zero downtime (migrações aditivas, PgBouncer transparente)

---

## 1.1 PgBouncer — Connection Pooling

### Problema

O `Maximum Pool Size=20` configurado no `.env.example` (linha 34) é partilhado por
28 DbContexts e 3 processos (ApiHost, Workers, Ingestion). Em carga, isto cria
contenção severa no PostgreSQL.

### Solução

Adicionar **PgBouncer** em modo **transaction pooling** entre os serviços e o
PostgreSQL. O PgBouncer multiplexeia centenas de conexões de aplicação em ~20
conexões reais à base de dados.

### Alterações necessárias

**`docker-compose.yml`** — adicionar serviço PgBouncer:

```yaml
pgbouncer:
  image: pgbouncer/pgbouncer:1.23.1
  restart: unless-stopped
  environment:
    DATABASES_HOST: postgres
    DATABASES_PORT: "5432"
    DATABASES_DBNAME: nextraceone
    DATABASES_USER: ${POSTGRES_USER:-nextraceone}
    DATABASES_PASSWORD: ${POSTGRES_PASSWORD}
    PGBOUNCER_POOL_MODE: transaction
    PGBOUNCER_MAX_CLIENT_CONN: "500"
    PGBOUNCER_DEFAULT_POOL_SIZE: "25"
    PGBOUNCER_MIN_POOL_SIZE: "5"
    PGBOUNCER_RESERVE_POOL_SIZE: "5"
    PGBOUNCER_SERVER_IDLE_TIMEOUT: "600"
  ports:
    - "5433:5432"
  depends_on:
    postgres:
      condition: service_healthy
  networks:
    - nextraceone-net
```

**`.env.example`** — nova connection string via PgBouncer (porta 5433):

```
# Produção: apontar para PgBouncer em vez de PostgreSQL directamente
CONNECTION_STRING_NEXTRACEONE=Host=pgbouncer;Port=5432;Database=nextraceone;...;Maximum Pool Size=100
```

**Nota sobre RLS:** O modo `transaction` do PgBouncer é compatível com o
`TenantRlsInterceptor` existente porque o `SET app.current_tenant_id` é executado
dentro de cada transacção. Verificar que o interceptor usa `SET LOCAL` (scoped à
transacção) em vez de `SET` (scoped à sessão).

---

## 1.2 Particionamento de Tabelas de Alto Volume

### Problema

As tabelas `AnalyticsEvent`, `AuditEntry` e `TelemetrySnapshot` são append-only e
crescem continuamente. Sem particionamento, queries de range por data fazem full
table scans e o VACUUM torna-se progressivamente mais lento.

### Tabelas a particionar

| Tabela | Módulo | Partição por | Retenção sugerida |
|--------|--------|-------------|-------------------|
| `pan_analytics_events` | ProductAnalytics | `occurred_at` mensal | 12 meses |
| `aud_audit_entries` | AuditCompliance | `created_at` mensal | 24 meses (compliance) |
| `ops_telemetry_snapshots` | OperationalIntelligence | `captured_at` mensal | 6 meses |
| `ops_dependency_metrics_snapshots` | OperationalIntelligence | `captured_at` mensal | 6 meses |

### Migration EF Core (exemplo para AnalyticsEvent)

```sql
-- Migration: Convert AnalyticsEvent to partitioned table
-- Executar com zero downtime via rename + recreate pattern

-- 1. Renomear tabela original
ALTER TABLE pan_analytics_events RENAME TO pan_analytics_events_old;

-- 2. Criar tabela particionada
CREATE TABLE pan_analytics_events (
    id UUID NOT NULL,
    tenant_id UUID NOT NULL,
    module VARCHAR(100) NOT NULL,
    event_type VARCHAR(200) NOT NULL,
    session_id UUID,
    user_id UUID,
    occurred_at TIMESTAMPTZ NOT NULL,
    properties JSONB,
    -- ... restantes colunas
    PRIMARY KEY (id, occurred_at)
) PARTITION BY RANGE (occurred_at);

-- 3. Criar partições iniciais
CREATE TABLE pan_analytics_events_2026_01
  PARTITION OF pan_analytics_events
  FOR VALUES FROM ('2026-01-01') TO ('2026-02-01');

CREATE TABLE pan_analytics_events_2026_04
  PARTITION OF pan_analytics_events
  FOR VALUES FROM ('2026-04-01') TO ('2026-05-01');

-- 4. Migrar dados históricos
INSERT INTO pan_analytics_events SELECT * FROM pan_analytics_events_old;

-- 5. Recriar indexes nas partições
CREATE INDEX ON pan_analytics_events (tenant_id, user_id, occurred_at);
CREATE INDEX ON pan_analytics_events (module, event_type);
CREATE INDEX ON pan_analytics_events (session_id, occurred_at);
```

### Job de criação automática de partições

Adicionar um `Quartz.NET` job que cria a partição do próximo mês antecipadamente:

```csharp
// src/platform/NexTraceOne.BackgroundWorkers/Jobs/PartitionMaintenanceJob.cs
public class PartitionMaintenanceJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var nextMonth = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(1);
        var partitionName = $"pan_analytics_events_{nextMonth:yyyy_MM}";
        var from = new DateOnly(nextMonth.Year, nextMonth.Month, 1);
        var to = from.AddMonths(1);

        await _db.Database.ExecuteSqlRawAsync($"""
            CREATE TABLE IF NOT EXISTS {partitionName}
            PARTITION OF pan_analytics_events
            FOR VALUES FROM ('{from:yyyy-MM-dd}') TO ('{to:yyyy-MM-dd}')
        """);
        // Repetir para as restantes tabelas particionadas
    }
}
```

**Schedule:** 1ª do mês, 02:00 UTC.

---

## 1.3 Read Replica

### Problema

O `AsNoTracking()` em 413 locais indica enorme volume de leitura, mas tudo bate no
mesmo nó PostgreSQL que também faz as escritas.

### Solução

Configurar uma réplica de leitura (`streaming replication`) e adicionar routing
no `NexTraceDbContextBase`.

### Alterações necessárias

**`docker-compose.production.yml`** — adicionar réplica:

```yaml
postgres-replica:
  image: pgvector/pgvector:pg16
  restart: unless-stopped
  environment:
    POSTGRES_USER: ${POSTGRES_USER}
    POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    POSTGRES_PRIMARY_HOST: postgres
    POSTGRES_REPLICATION_USER: replicator
    POSTGRES_REPLICATION_PASSWORD: ${POSTGRES_REPLICATION_PASSWORD}
  command: >
    bash -c "
      pg_basebackup -h postgres -U replicator -D /var/lib/postgresql/data -P -Xs -R
      postgres -c 'hot_standby=on'
    "
  networks:
    - nextraceone-net
```

**`NexTraceDbContextBase`** — connection string por operação:

```csharp
// Usar réplica para queries (IQuery), primary para commands (ICommand)
// Configurado via named connection strings:
// ConnectionStrings__NexTraceOne       → primary (escritas)
// ConnectionStrings__NexTraceOneRead   → réplica (leituras)
```

**`appsettings.json`** — nova connection string:

```json
{
  "ConnectionStrings": {
    "NexTraceOne": "Host=pgbouncer;...",
    "NexTraceOneRead": "Host=pgbouncer-replica;...;Maximum Pool Size=50"
  }
}
```

**Nota:** Em desenvolvimento local, `NexTraceOneRead` pode apontar para o mesmo
PostgreSQL sem réplica real.

---

## 1.4 Redis — Cache e Hot Data

### Problema

Não existe camada de cache. Dados frequentemente lidos (catálogo de serviços, perfis
de risco, overlays do grafo) são sempre consultados na base de dados.

### Solução

Adicionar **Redis** via `IDistributedCache` do ASP.NET Core. A abstracção já existe
na framework — não requer mudanças de domínio.

### Alterações necessárias

**`docker-compose.yml`** — adicionar Redis:

```yaml
redis:
  image: redis:7.4-alpine
  restart: unless-stopped
  command: redis-server --maxmemory 512mb --maxmemory-policy allkeys-lru --appendonly no
  ports:
    - "6379:6379"
  healthcheck:
    test: ["CMD", "redis-cli", "ping"]
    interval: 10s
    timeout: 5s
    retries: 5
  networks:
    - nextraceone-net
```

**`NexTraceOne.BuildingBlocks.Infrastructure.csproj`** — adicionar pacote:

```xml
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.0.0" />
```

**`ServiceCollectionExtensions`** — registar Redis:

```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetConnectionString("Redis");
    options.InstanceName = "nextraceone:";
});
```

### Dados a colocar em cache

| Dado | TTL | Módulo |
|------|-----|--------|
| `ServiceAsset` completo por ID | 5 min | Catalog |
| `ApiAsset` com consumers | 5 min | Catalog |
| `GraphSnapshot` mais recente | 2 min | Catalog.Graph |
| `NodeHealthRecord` por overlay | 1 min | Catalog.Graph |
| `ServiceRiskProfile` por serviço | 10 min | ChangeGovernance |
| `ChangeIntelligenceScore` por release | 5 min | ChangeGovernance |
| Tenant config / feature flags | 15 min | Configuration |

### Padrão de implementação

```csharp
// Usar cache-aside pattern nos QueryHandlers de maior volume
public async Task<Result<ServiceAssetResponse>> Handle(
    GetServiceAssetQuery query, CancellationToken ct)
{
    var cacheKey = $"service:{query.TenantId}:{query.ServiceId}";

    var cached = await _cache.GetStringAsync(cacheKey, ct);
    if (cached is not null)
        return JsonSerializer.Deserialize<ServiceAssetResponse>(cached)!;

    var asset = await _repository.GetByIdAsync(query.ServiceId, ct);
    if (asset is null) return Error.NotFound("ServiceAsset.NotFound");

    var response = asset.ToResponse();
    await _cache.SetStringAsync(cacheKey,
        JsonSerializer.Serialize(response),
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) },
        ct);

    return response;
}
```

### Invalidação de cache

Usar o **Outbox Pattern já existente** para invalidar cache em eventos de domínio:

```csharp
// Quando ServiceAsset é actualizado → evento de domínio → OutboxProcessor
// → handler de integração invalida chave Redis
public class ServiceAssetUpdatedCacheInvalidator : IIntegrationEventHandler<ServiceAssetUpdatedEvent>
{
    public async Task Handle(ServiceAssetUpdatedEvent @event)
    {
        await _cache.RemoveAsync($"service:{@event.TenantId}:{@event.ServiceId}");
    }
}
```

---

## Checklist de Entrega — Fase 1

- [ ] PgBouncer adicionado ao docker-compose (dev + produção)
- [ ] Connection strings actualizadas para apontar para PgBouncer
- [ ] `TenantRlsInterceptor` verificado para usar `SET LOCAL` (não `SET`)
- [ ] Migration de particionamento para `pan_analytics_events`
- [ ] Migration de particionamento para `aud_audit_entries`
- [ ] Migration de particionamento para `ops_telemetry_snapshots`
- [ ] `PartitionMaintenanceJob` Quartz registado e agendado
- [ ] `postgres-replica` configurado no docker-compose.production.yml
- [ ] Connection string de leitura `NexTraceOneRead` adicionada
- [ ] Redis adicionado ao docker-compose (dev + produção)
- [ ] `IDistributedCache` registado na DI via Redis
- [ ] Cache-aside implementado nos top 5 QueryHandlers por volume
- [ ] Invalidação de cache via Outbox para ServiceAsset e ApiAsset
- [ ] Testes de integração actualizados para Redis (usar `IDistributedCache` mock)
