# ClickHouse Setup — NexTraceOne Analytics

This document covers the ClickHouse schema DDL for the NexTraceOne analytics database
and a docker-compose snippet for local development.

## Database

```sql
CREATE DATABASE IF NOT EXISTS nextraceone_obs;
USE nextraceone_obs;
```

## Tables

### obs_traces — Distributed Trace Latency

```sql
CREATE TABLE IF NOT EXISTS obs_traces
(
    id             UUID DEFAULT generateUUIDv4(),
    tenant_id      UUID,
    service_name   LowCardinality(String),
    environment    LowCardinality(String),
    trace_id       String,
    span_id        String,
    operation_name String,
    duration_ms    Float64,
    status         LowCardinality(String),
    timestamp      DateTime64(3, 'UTC'),
    attributes     Map(String, String)
)
ENGINE = MergeTree()
PARTITION BY toYYYYMM(timestamp)
ORDER BY (tenant_id, service_name, environment, timestamp)
TTL timestamp + INTERVAL 90 DAY;
```

### obs_metrics — Runtime Metrics

```sql
CREATE TABLE IF NOT EXISTS obs_metrics
(
    id           UUID DEFAULT generateUUIDv4(),
    tenant_id    UUID,
    metric_name  LowCardinality(String),
    service_name LowCardinality(String),
    environment  LowCardinality(String),
    value        Float64,
    unit         LowCardinality(String),
    timestamp    DateTime64(3, 'UTC'),
    labels       Map(String, String)
)
ENGINE = MergeTree()
PARTITION BY toYYYYMM(timestamp)
ORDER BY (tenant_id, service_name, metric_name, timestamp)
TTL timestamp + INTERVAL 90 DAY;
```

### obs_logs — Structured Logs

```sql
CREATE TABLE IF NOT EXISTS obs_logs
(
    id           UUID DEFAULT generateUUIDv4(),
    tenant_id    UUID,
    service_name LowCardinality(String),
    environment  LowCardinality(String),
    level        LowCardinality(String),
    message      String,
    timestamp    DateTime64(3, 'UTC'),
    attributes   Map(String, String)
)
ENGINE = MergeTree()
PARTITION BY toYYYYMM(timestamp)
ORDER BY (tenant_id, service_name, level, timestamp)
TTL timestamp + INTERVAL 90 DAY;
```

## Docker Compose Snippet

Add to your `docker-compose.override.yml` for local development:

```yaml
services:
  clickhouse:
    image: clickhouse/clickhouse-server:24.3
    container_name: nextraceone-clickhouse
    ports:
      - "8123:8123"   # HTTP API
      - "9000:9000"   # Native protocol
    environment:
      CLICKHOUSE_DB: nextraceone_obs
      CLICKHOUSE_USER: default
      CLICKHOUSE_DEFAULT_ACCESS_MANAGEMENT: "1"
    volumes:
      - clickhouse_data:/var/lib/clickhouse
    healthcheck:
      test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:8123/ping"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  clickhouse_data:
```

## appsettings Configuration

```json
{
  "Analytics": {
    "Enabled": true,
    "ConnectionString": "http://clickhouse:8123/?database=nextraceone_obs",
    "WriteTimeoutSeconds": 10,
    "MaxBatchSize": 1000,
    "SuppressWriteErrors": true
  }
}
```
