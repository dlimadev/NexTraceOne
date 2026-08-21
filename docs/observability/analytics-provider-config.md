# Analytics Provider Configuration

ClickHouse is the platform's **only** analytics storage backend. The `Analytics`
section controls whether analytics writes happen and where they go.

> **Correction (Aug 2026).** Earlier revisions of this document described an
> `Analytics:Provider` key with `ClickHouse`, `Elasticsearch` and `InMemory`
> values, and called Elasticsearch the default. **No such key exists.**
> `AnalyticsOptions` has no `Provider` property and `appsettings.json` sets none.
> Setting `Analytics__Provider=Elasticsearch` had no effect — writes always went
> to ClickHouse. Elasticsearch was removed from the product.

## How the provider is selected

There is no selection. `AddBuildingBlocksAnalytics`
(`BuildingBlocks.Observability/DependencyInjection.cs:210`) binds `AnalyticsOptions`
and branches only on `Enabled`:

```csharp
if (!analyticsOptions.Enabled)
{
    services.AddSingleton<IAnalyticsWriter, NullAnalyticsWriter>();
    return services;
}

services.AddHttpClient<ClickHouseAnalyticsWriter>(…).AddStandardResilienceHandler();
services.AddSingleton<IAnalyticsWriter, ClickHouseAnalyticsWriter>();
```

- `Enabled: false` → `NullAnalyticsWriter` (no analytics I/O)
- `Enabled: true` → `ClickHouseAnalyticsWriter`

## Configuration keys

The real properties on `AnalyticsOptions`:

| Key | Default | Description |
|-----|---------|-------------|
| `Analytics:Enabled` | `false` | Enables analytics writes. Activate explicitly once ClickHouse is reachable. |
| `Analytics:ConnectionString` | — | ClickHouse HTTP endpoint |
| `Analytics:ApiKey` | — | Optional credential |
| `Analytics:TablePrefix` | — | Prefix for analytics tables |
| `Analytics:WriteTimeoutSeconds` | `10` | HTTP write timeout |
| `Analytics:MaxBatchSize` | `1000` | Max records per INSERT batch |
| `Analytics:SuppressWriteErrors` | `true` | Swallow write failures instead of propagating |

Example, matching `src/platform/NexTraceOne.ApiHost/appsettings.json`:

```json
{
  "Analytics": {
    "Enabled": true,
    "ConnectionString": "http://localhost:8123",
    "WriteTimeoutSeconds": 10,
    "MaxBatchSize": 500,
    "SuppressWriteErrors": true
  }
}
```

See [clickhouse-setup.md](clickhouse-setup.md) for schema DDL and the docker-compose snippet.

## Environment variable override

For container deployments:

```bash
Analytics__Enabled=true
Analytics__ConnectionString=http://clickhouse:8123/?database=nextraceone_obs
```

## Health check

When `Enabled = true`, the ClickHouse health check is registered automatically at
`/health` under the tag `analytics`.

```
GET /health
{
  "entries": {
    "clickhouse": { "status": "Healthy", "description": "ClickHouse is reachable." }
  }
}
```

## Platform configuration keys

These can also be managed via the NexTraceOne Configuration module:

| Key | Default | Description |
|-----|---------|-------------|
| `analytics.clickhouse.batch_size` | `1000` | Max records per ClickHouse INSERT batch |
| `analytics.clickhouse.flush_interval_seconds` | `5` | Flush interval in seconds |
| `analytics.clickhouse.default_ttl_days` | `90` | Default TTL for analytics data |
