# Fase 2 — ClickHouse como Provider Principal de Observabilidade

> **Duração estimada:** 3–4 semanas
> **Pré-requisitos:** Pode correr em paralelo com Fase 1
> **O que já existe:** `ClickHouseAnalyticsWriter` (Wave Z.3 ✅), schema em
> `build/clickhouse/`, docs em `docs/observability/providers/clickhouse.md`

---

## Contexto

O ClickHouse já está implementado como provider alternativo (Wave Z.3). O que falta é:

1. **Torná-lo o provider padrão** (inverter o default de Elastic → ClickHouse)
2. **Migrar os dados de domínio de alto volume** do PostgreSQL para ClickHouse
3. **Adicionar tabelas de domínio** (ProductAnalytics, TelemetryStore, HostMetrics)
4. **Remover a dependência do Elasticsearch** do docker-compose padrão

---

## 2.1 Activar ClickHouse como Provider Padrão

### Alterações em `docker-compose.yml`

Substituir Elasticsearch por ClickHouse no stack padrão:

```yaml
# REMOVER — Elasticsearch (manter apenas em docker-compose.override.yml para quem precisa)
# elasticsearch: ...

# ADICIONAR — ClickHouse como default
clickhouse:
  image: clickhouse/clickhouse-server:24.8-alpine
  restart: unless-stopped
  environment:
    CLICKHOUSE_DB: nextraceone_obs
    CLICKHOUSE_USER: default
    CLICKHOUSE_PASSWORD: ${CLICKHOUSE_PASSWORD:-}
    CLICKHOUSE_DEFAULT_ACCESS_MANAGEMENT: 1
  volumes:
    - clickhouse-data:/var/lib/clickhouse
    - ./build/clickhouse/init-schema.sql:/docker-entrypoint-initdb.d/01-init-schema.sql:ro
    - ./build/clickhouse/analytics-schema.sql:/docker-entrypoint-initdb.d/02-analytics-schema.sql:ro
    - ./build/clickhouse/domain-schema.sql:/docker-entrypoint-initdb.d/03-domain-schema.sql:ro
  ports:
    - "8123:8123"
    - "9000:9000"
  healthcheck:
    test: ["CMD-SHELL", "clickhouse-client --query 'SELECT 1' || exit 1"]
    interval: 10s
    timeout: 5s
    retries: 5
    start_period: 30s
  networks:
    - nextraceone-net
```

### Alterações em variáveis de ambiente

**`.env.example`:**

```bash
# Provider padrão alterado de Elastic para ClickHouse
OBSERVABILITY_PROVIDER=ClickHouse
CLICKHOUSE_PASSWORD=
CLICKHOUSE_HOST=clickhouse
CLICKHOUSE_PORT=8123
CLICKHOUSE_DATABASE=nextraceone_obs

# Elasticsearch (opcional — para quem já tem stack Elastic)
# OBSERVABILITY_PROVIDER=Elastic
# ELASTICSEARCH_ENDPOINT=http://elasticsearch:9200
```

**`docker-compose.yml`** — variáveis nos serviços:

```yaml
Telemetry__ObservabilityProvider__Provider: ${OBSERVABILITY_PROVIDER:-ClickHouse}
Telemetry__ObservabilityProvider__ClickHouse__Enabled: "true"
Telemetry__ObservabilityProvider__ClickHouse__ConnectionString: >
  Host=${CLICKHOUSE_HOST:-clickhouse};Port=${CLICKHOUSE_PORT:-8123};
  Database=${CLICKHOUSE_DATABASE:-nextraceone_obs};
  Username=default;Password=${CLICKHOUSE_PASSWORD:-}
Telemetry__ObservabilityProvider__Elastic__Enabled: "false"
```

### ADR a actualizar

Criar `docs/adr/011-clickhouse-as-primary-observability.md` a registar a decisão de
mover o default de Elasticsearch para ClickHouse, documentando os critérios objectivos
mencionados no FUTURE-ROADMAP (linha 381): ClickHouse preferível para >100M
events/day, OLAP pesado e retenção longa.

---

## 2.2 Tabelas de Domínio no ClickHouse

Além das tabelas OTEL (já existentes), adicionar tabelas de domínio de alto volume.

### Novo ficheiro: `build/clickhouse/domain-schema.sql`

```sql
CREATE DATABASE IF NOT EXISTS nextraceone_analytics;
USE nextraceone_analytics;

-- ProductAnalytics: eventos de produto (alta frequência)
CREATE TABLE IF NOT EXISTS analytics_events
(
    id            UUID DEFAULT generateUUIDv4(),
    tenant_id     UUID NOT NULL,
    module        LowCardinality(String),
    event_type    LowCardinality(String),
    session_id    UUID,
    user_id       UUID,
    occurred_at   DateTime64(3, 'UTC'),
    properties    String  -- JSON
)
ENGINE = MergeTree()
PARTITION BY toYYYYMM(occurred_at)
ORDER BY (tenant_id, module, event_type, occurred_at)
TTL occurred_at + INTERVAL 12 MONTH;

-- TelemetryStore: snapshots de métricas de runtime
CREATE TABLE IF NOT EXISTS service_metrics_snapshots
(
    id              UUID DEFAULT generateUUIDv4(),
    tenant_id       UUID NOT NULL,
    service_id      UUID NOT NULL,
    environment     LowCardinality(String),
    captured_at     DateTime64(3, 'UTC'),
    latency_p50_ms  Float64,
    latency_p95_ms  Float64,
    latency_p99_ms  Float64,
    throughput_rps  Float64,
    error_rate_pct  Float64,
    apdex_score     Float64
)
ENGINE = MergeTree()
PARTITION BY toYYYYMM(captured_at)
ORDER BY (tenant_id, service_id, environment, captured_at)
TTL captured_at + INTERVAL 6 MONTH;

-- TelemetryStore: métricas de dependências entre serviços
CREATE TABLE IF NOT EXISTS dependency_metrics_snapshots
(
    id               UUID DEFAULT generateUUIDv4(),
    tenant_id        UUID NOT NULL,
    source_service   UUID NOT NULL,
    target_service   UUID NOT NULL,
    environment      LowCardinality(String),
    captured_at      DateTime64(3, 'UTC'),
    call_count       UInt64,
    error_count      UInt64,
    avg_latency_ms   Float64,
    p99_latency_ms   Float64
)
ENGINE = MergeTree()
PARTITION BY toYYYYMM(captured_at)
ORDER BY (tenant_id, source_service, target_service, captured_at)
TTL captured_at + INTERVAL 6 MONTH;

-- Audit trail de alto volume (append-only — ideal para ClickHouse)
CREATE TABLE IF NOT EXISTS audit_events
(
    id              UUID DEFAULT generateUUIDv4(),
    tenant_id       UUID NOT NULL,
    user_id         UUID,
    action          LowCardinality(String),
    resource_type   LowCardinality(String),
    resource_id     UUID,
    correlation_id  UUID,
    occurred_at     DateTime64(3, 'UTC'),
    metadata        String  -- JSON
)
ENGINE = MergeTree()
PARTITION BY toYYYYMM(occurred_at)
ORDER BY (tenant_id, resource_type, action, occurred_at)
TTL occurred_at + INTERVAL 24 MONTH;

-- HostMetrics (preparado para Fase 3)
CREATE TABLE IF NOT EXISTS host_metrics_snapshots
(
    id                  UUID DEFAULT generateUUIDv4(),
    tenant_id           UUID NOT NULL,
    host_id             UUID NOT NULL,
    hostname            LowCardinality(String),
    environment         LowCardinality(String),
    captured_at         DateTime64(3, 'UTC'),
    cpu_usage_pct       Float32,
    memory_used_pct     Float32,
    disk_used_pct       Float32,
    network_in_mbps     Float32,
    network_out_mbps    Float32,
    uptime_seconds      UInt64
)
ENGINE = MergeTree()
PARTITION BY toYYYYMM(captured_at)
ORDER BY (tenant_id, host_id, captured_at)
TTL captured_at + INTERVAL 6 MONTH;
```

---

## 2.3 Migrar ProductAnalyticsDbContext para ClickHouse

### Estratégia

Manter o PostgreSQL como fallback durante a transição. O `IProductAnalyticsWriter`
passa a ter duas implementações: PostgreSQL (existente) e ClickHouse (nova).

```
IProductAnalyticsWriter
  ├── PostgresAnalyticsWriter (existente — deprecar após validação)
  └── ClickHouseAnalyticsWriter (nova implementação)
```

### Novo adapter: `ClickHouseProductAnalyticsWriter`

```csharp
// src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/
//   ClickHouse/ClickHouseProductAnalyticsWriter.cs

public class ClickHouseProductAnalyticsWriter : IProductAnalyticsWriter
{
    private readonly IClickHouseConnection _connection;

    public async Task WriteEventAsync(AnalyticsEvent @event, CancellationToken ct)
    {
        await _connection.ExecuteInsertAsync(
            "INSERT INTO nextraceone_analytics.analytics_events VALUES",
            new[]
            {
                @event.Id, @event.TenantId, @event.Module, @event.EventType,
                @event.SessionId, @event.UserId, @event.OccurredAt,
                JsonSerializer.Serialize(@event.Properties)
            }, ct);
    }

    public async Task WriteBatchAsync(IEnumerable<AnalyticsEvent> events, CancellationToken ct)
    {
        // Batch insert — ClickHouse optimizado para inserções em batch
        var rows = events.Select(e => new object[]
        {
            e.Id, e.TenantId, e.Module, e.EventType,
            e.SessionId, e.UserId, e.OccurredAt,
            JsonSerializer.Serialize(e.Properties)
        });
        await _connection.ExecuteBatchInsertAsync(
            "INSERT INTO nextraceone_analytics.analytics_events VALUES",
            rows, ct);
    }
}
```

### Configuração da DI (condicional por provider)

```csharp
// src/modules/productanalytics/NexTraceOne.ProductAnalytics.Infrastructure/
//   ProductAnalyticsModule.cs

if (config["Analytics:Provider"] == "ClickHouse")
    services.AddScoped<IProductAnalyticsWriter, ClickHouseProductAnalyticsWriter>();
else
    services.AddScoped<IProductAnalyticsWriter, PostgresProductAnalyticsWriter>();
```

---

## 2.4 Migrar TelemetryStoreDbContext para ClickHouse

O mesmo padrão: abstracção `ITelemetryStoreWriter` com implementação ClickHouse.

```
ITelemetryStoreWriter
  ├── PostgresTelemetryWriter (existente)
  └── ClickHouseTelemetryWriter (nova — usa service_metrics_snapshots)
```

As queries de leitura (`ITelemetryStoreReader`) passam a consultar ClickHouse para
dados recentes e PostgreSQL para dados históricos migrados.

---

## 2.5 OTEL Collector — Exporter ClickHouse

Actualizar `build/otel-collector/otel-collector.yaml` para usar o exporter
nativo ClickHouse (suportado pelo `otel-collector-contrib`):

```yaml
exporters:
  clickhouse:
    endpoint: tcp://clickhouse:9000
    database: nextraceone_obs
    username: default
    password: ${CLICKHOUSE_PASSWORD}
    ttl_days: 30
    logs_table_name: otel_logs
    traces_table_name: otel_traces
    metrics_table_name: otel_metrics
    timeout: 10s
    retry_on_failure:
      enabled: true
      initial_interval: 5s
      max_interval: 30s
      max_elapsed_time: 300s
    sending_queue:
      enabled: true
      num_consumers: 4
      queue_size: 100

service:
  pipelines:
    traces:
      exporters: [clickhouse]
    metrics:
      exporters: [clickhouse]
    logs:
      exporters: [clickhouse]
```

---

## 2.6 Manter Elasticsearch como Opção

O Elasticsearch não é removido — é movido para `docker-compose.override.yml`:

```yaml
# docker-compose.override.yml — para quem já tem stack Elastic
services:
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.17.0
    # ... configuração existente
  
  # Desactivar ClickHouse quando usar Elastic
  clickhouse:
    profiles: ["disabled"]
```

Clientes com Elastic Stack existente podem continuar a usar `OBSERVABILITY_PROVIDER=Elastic`.

---

## Checklist de Entrega — Fase 2

- [ ] `build/clickhouse/domain-schema.sql` criado com 5 tabelas de domínio
- [ ] `docker-compose.yml` actualizado: ClickHouse como default, Elastic para override
- [ ] `.env.example` actualizado com variáveis ClickHouse
- [ ] `otel-collector.yaml` actualizado para exporter ClickHouse
- [ ] `ClickHouseProductAnalyticsWriter` implementado e registado na DI
- [ ] `ClickHouseTelemetryWriter` implementado e registado na DI
- [ ] `ClickHouseAuditWriter` implementado (audit de alto volume)
- [ ] ADR-011 criado documentando a decisão
- [ ] Testes de integração com ClickHouse em `docker-compose.test.yml`
- [ ] `docs/observability/providers/clickhouse.md` actualizado com novas tabelas
- [ ] Health check do ClickHouse integrado no `GET /api/v1/system/health`
