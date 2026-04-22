# Analytics Provider Configuration

NexTraceOne supports multiple analytics storage backends. The active provider is
controlled by the `Analytics:Provider` configuration key.

## Supported Providers

| Value | Description |
|-------|-------------|
| `ClickHouse` | ClickHouse HTTP API (recommended for high-volume analytics) |
| `Elasticsearch` | Elasticsearch Bulk API (default; rich full-text search) |
| `InMemory` | In-memory null provider (for testing and local development) |

## Switching Provider

Update `appsettings.json` (or environment variable `Analytics__Provider`):

```json
{
  "Analytics": {
    "Enabled": true,
    "Provider": "ClickHouse",
    "ConnectionString": "http://clickhouse:8123/?database=nextraceone_obs",
    "WriteTimeoutSeconds": 10,
    "MaxBatchSize": 1000,
    "SuppressWriteErrors": true
  }
}
```

### ClickHouse

```json
{
  "Analytics": {
    "Provider": "ClickHouse",
    "ConnectionString": "http://clickhouse:8123/?database=nextraceone_obs"
  }
}
```

See [clickhouse-setup.md](clickhouse-setup.md) for schema DDL and docker-compose snippet.

### Elasticsearch

```json
{
  "Analytics": {
    "Provider": "Elasticsearch",
    "ConnectionString": "http://elasticsearch:9200",
    "ApiKey": "YOUR_API_KEY",
    "IndexPrefix": "nextraceone-analytics"
  }
}
```

### InMemory (testing)

```json
{
  "Analytics": {
    "Provider": "InMemory",
    "Enabled": false
  }
}
```

## Environment Variable Override

For container deployments, use environment variables:

```bash
Analytics__Provider=ClickHouse
Analytics__ConnectionString=http://clickhouse:8123/?database=nextraceone_obs
Analytics__Enabled=true
```

## Health Check

When `Provider = "ClickHouse"` and `Enabled = true`, the ClickHouse health check is
registered automatically at `/health` under the tag `analytics`.

```
GET /health
{
  "entries": {
    "clickhouse": { "status": "Healthy", "description": "ClickHouse is reachable." }
  }
}
```

## Platform Configuration Keys

These keys can also be managed via the NexTraceOne Configuration module:

| Key | Default | Description |
|-----|---------|-------------|
| `analytics.clickhouse.batch_size` | `1000` | Max records per ClickHouse INSERT batch |
| `analytics.clickhouse.flush_interval_seconds` | `5` | Flush interval in seconds |
| `analytics.clickhouse.default_ttl_days` | `90` | Default TTL for analytics data |
