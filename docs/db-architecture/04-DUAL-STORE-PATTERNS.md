# 04 — Abstracção IAnalyticsStore e Registo Condicional

> Define como o código da aplicação permanece **agnóstico do provider** e como o sistema
> selecciona ClickHouse ou Elasticsearch em tempo de arranque com base na configuração.
> O código de domínio e aplicação **nunca referencia** ClickHouse nem Elasticsearch directamente.

---

## Princípio central

```
Domain / Application
       │
       │ injeta
       ▼
IAnalyticsStore  ◄─── único contrato
       │
       ├─── ClickHouseAnalyticsStore    (quando Provider = "ClickHouse")
       ├─── ElasticsearchAnalyticsStore (quando Provider = "Elasticsearch")
       └─── NullAnalyticsStore          (fallback seguro — retorna vazio)
```

Apenas **um** dos três é registado no DI container em cada instalação.

---

## 1. Interface principal

**Localização:** `src/BuildingBlocks/NexTraceOne.BuildingBlocks.Analytics/IAnalyticsStore.cs`

```csharp
namespace NexTraceOne.BuildingBlocks.Analytics;

public interface IAnalyticsStore
{
    /// <summary>Insere um único documento/linha.</summary>
    Task InsertAsync<T>(string collection, T record, CancellationToken ct = default)
        where T : class;

    /// <summary>Insere em batch (recomendado para séries temporais).</summary>
    Task BulkInsertAsync<T>(string collection, IEnumerable<T> records, CancellationToken ct = default)
        where T : class;

    /// <summary>Executa uma query e devolve resultados tipados.</summary>
    Task<IReadOnlyList<T>> QueryAsync<T>(AnalyticsQuery query, CancellationToken ct = default)
        where T : class;

    /// <summary>Conta documentos que satisfazem o filtro.</summary>
    Task<long> CountAsync(AnalyticsQuery query, CancellationToken ct = default);

    /// <summary>Provider actual (para diagnóstico / health checks).</summary>
    string ProviderName { get; }
}
```

---

## 2. AnalyticsQuery — query portável

**Localização:** `src/BuildingBlocks/NexTraceOne.BuildingBlocks.Analytics/AnalyticsQuery.cs`

```csharp
namespace NexTraceOne.BuildingBlocks.Analytics;

/// <summary>
/// Representa uma query de analytics de forma agnóstica do provider.
/// O provider concreto traduz para SQL ClickHouse ou DSL Elasticsearch.
/// </summary>
public sealed record AnalyticsQuery
{
    public required string Collection { get; init; }
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public IReadOnlyDictionary<string, object?> Filters { get; init; } = new Dictionary<string, object?>();
    public string? FullTextQuery { get; init; }
    public int Limit { get; init; } = 100;
    public int Offset { get; init; } = 0;
    public string? OrderByField { get; init; }
    public bool OrderDescending { get; init; } = true;
}
```

---

## 3. Configuração

**Localização:** `src/BuildingBlocks/NexTraceOne.BuildingBlocks.Analytics/AnalyticsStoreOptions.cs`

```csharp
namespace NexTraceOne.BuildingBlocks.Analytics;

public sealed class AnalyticsStoreOptions
{
    public const string SectionName = "AnalyticsStore";

    /// <summary>"ClickHouse" | "Elasticsearch" | "None"</summary>
    public string Provider { get; set; } = "None";

    public ClickHouseOptions ClickHouse { get; set; } = new();
    public ElasticsearchOptions Elasticsearch { get; set; } = new();
}

public sealed class ClickHouseOptions
{
    public bool Enabled { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public int MaxBatchSize { get; set; } = 5000;
    public int FlushIntervalSeconds { get; set; } = 5;
}

public sealed class ElasticsearchOptions
{
    public bool Enabled { get; set; }
    public string Uri { get; set; } = "http://localhost:9200";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string IndexPrefix { get; set; } = "nextraceone";
}
```

**appsettings.json — instalação ClickHouse (padrão):**
```json
{
  "AnalyticsStore": {
    "Provider": "ClickHouse",
    "ClickHouse": {
      "Enabled": true,
      "ConnectionString": "Host=clickhouse;Port=9000;Database=nextraceone_analytics;Username=default;Password="
    }
  }
}
```

**appsettings.json — instalação Elasticsearch:**
```json
{
  "AnalyticsStore": {
    "Provider": "Elasticsearch",
    "Elasticsearch": {
      "Enabled": true,
      "Uri": "http://elasticsearch:9200",
      "Username": "elastic",
      "Password": "changeme",
      "IndexPrefix": "nextraceone"
    }
  }
}
```

---

## 4. Método de extensão para DI

**Localização:** `src/BuildingBlocks/NexTraceOne.BuildingBlocks.Analytics/ServiceCollectionExtensions.cs`

```csharp
namespace NexTraceOne.BuildingBlocks.Analytics;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAnalyticsStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration
            .GetSection(AnalyticsStoreOptions.SectionName)
            .Get<AnalyticsStoreOptions>() ?? new AnalyticsStoreOptions();

        services.Configure<AnalyticsStoreOptions>(
            configuration.GetSection(AnalyticsStoreOptions.SectionName));

        switch (options.Provider)
        {
            case "ClickHouse" when options.ClickHouse.Enabled:
                services.AddClickHouseAnalyticsStore(options.ClickHouse);
                break;

            case "Elasticsearch" when options.Elasticsearch.Enabled:
                services.AddElasticsearchAnalyticsStore(options.Elasticsearch);
                break;

            default:
                services.AddSingleton<IAnalyticsStore, NullAnalyticsStore>();
                break;
        }

        return services;
    }

    private static void AddClickHouseAnalyticsStore(
        this IServiceCollection services, ClickHouseOptions opts)
    {
        // Regista o cliente ClickHouse e o adapter
        services.AddSingleton(opts);
        services.AddSingleton<IAnalyticsStore, ClickHouseAnalyticsStore>();
    }

    private static void AddElasticsearchAnalyticsStore(
        this IServiceCollection services, ElasticsearchOptions opts)
    {
        // Regista o cliente Elasticsearch e o adapter
        services.AddSingleton(opts);
        services.AddSingleton<IAnalyticsStore, ElasticsearchAnalyticsStore>();
    }
}
```

---

## 5. NullAnalyticsStore — fallback seguro

**Localização:** `src/BuildingBlocks/NexTraceOne.BuildingBlocks.Analytics/NullAnalyticsStore.cs`

```csharp
namespace NexTraceOne.BuildingBlocks.Analytics;

/// <summary>
/// Implementação honesta-nula: não lança excepções, não persiste nada.
/// Usada quando nenhum analytics store está configurado.
/// </summary>
public sealed class NullAnalyticsStore : IAnalyticsStore
{
    public string ProviderName => "None";

    public Task InsertAsync<T>(string collection, T record, CancellationToken ct = default)
        where T : class => Task.CompletedTask;

    public Task BulkInsertAsync<T>(string collection, IEnumerable<T> records, CancellationToken ct = default)
        where T : class => Task.CompletedTask;

    public Task<IReadOnlyList<T>> QueryAsync<T>(AnalyticsQuery query, CancellationToken ct = default)
        where T : class => Task.FromResult<IReadOnlyList<T>>(Array.Empty<T>());

    public Task<long> CountAsync(AnalyticsQuery query, CancellationToken ct = default)
        => Task.FromResult(0L);
}
```

---

## 6. ClickHouseAnalyticsStore — implementação concreta

**Localização:** `src/Infrastructure/NexTraceOne.Infrastructure.Analytics.ClickHouse/ClickHouseAnalyticsStore.cs`

```csharp
namespace NexTraceOne.Infrastructure.Analytics.ClickHouse;

public sealed class ClickHouseAnalyticsStore : IAnalyticsStore
{
    private readonly ClickHouseConnection _connection;
    private readonly ClickHouseOptions _options;
    private readonly ILogger<ClickHouseAnalyticsStore> _logger;

    public string ProviderName => "ClickHouse";

    public ClickHouseAnalyticsStore(
        ClickHouseOptions options,
        ILogger<ClickHouseAnalyticsStore> logger)
    {
        _options = options;
        _logger = logger;
        _connection = new ClickHouseConnection(options.ConnectionString);
    }

    public async Task InsertAsync<T>(string collection, T record, CancellationToken ct = default)
        where T : class
    {
        await BulkInsertAsync(collection, [record], ct);
    }

    public async Task BulkInsertAsync<T>(string collection, IEnumerable<T> records, CancellationToken ct = default)
        where T : class
    {
        var batch = records.ToList();
        if (batch.Count == 0) return;

        try
        {
            await _connection.OpenAsync(ct);
            using var bulk = await _connection.CreateColumnWriterAsync(
                $"INSERT INTO {collection} FORMAT Native", ct);

            // Reflexão sobre as propriedades de T para mapear colunas
            // (implementação completa usa Source Generator ou Dapper.Contrib)
            await bulk.WriteTableAsync(batch, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ClickHouse bulk insert failed for {Collection}", collection);
            throw;
        }
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(AnalyticsQuery query, CancellationToken ct = default)
        where T : class
    {
        var sql = BuildClickHouseSql(query);
        await _connection.OpenAsync(ct);
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        using var reader = await cmd.ExecuteReaderAsync(ct);
        return MapResults<T>(reader);
    }

    public async Task<long> CountAsync(AnalyticsQuery query, CancellationToken ct = default)
    {
        var sql = BuildClickHouseSql(query with { Limit = 0, Offset = 0 });
        var countSql = $"SELECT count() FROM ({sql})";
        await _connection.OpenAsync(ct);
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = countSql;
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
    }

    private static string BuildClickHouseSql(AnalyticsQuery query)
    {
        var sb = new StringBuilder($"SELECT * FROM {query.Collection}");
        var conditions = new List<string>();

        if (query.From.HasValue)
            conditions.Add($"ts >= '{query.From:yyyy-MM-dd HH:mm:ss}'");
        if (query.To.HasValue)
            conditions.Add($"ts <= '{query.To:yyyy-MM-dd HH:mm:ss}'");

        foreach (var (key, value) in query.Filters)
        {
            if (value is not null)
                conditions.Add($"{key} = '{value}'");
        }

        if (!string.IsNullOrEmpty(query.FullTextQuery))
            conditions.Add($"hasToken(content, '{query.FullTextQuery}')");

        if (conditions.Count > 0)
            sb.Append(" WHERE ").Append(string.Join(" AND ", conditions));

        if (!string.IsNullOrEmpty(query.OrderByField))
            sb.Append($" ORDER BY {query.OrderByField} {(query.OrderDescending ? "DESC" : "ASC")}");

        if (query.Limit > 0)
            sb.Append($" LIMIT {query.Limit} OFFSET {query.Offset}");

        return sb.ToString();
    }

    private static IReadOnlyList<T> MapResults<T>(IDataReader reader) where T : class
    {
        // Implementação usa reflexão ou source-generated mapper
        // Omitido por brevidade — ver NexTraceOne.Infrastructure.Analytics.ClickHouse.Mapping
        return Array.Empty<T>();
    }
}
```

---

## 7. ElasticsearchAnalyticsStore — implementação concreta

**Localização:** `src/Infrastructure/NexTraceOne.Infrastructure.Analytics.Elasticsearch/ElasticsearchAnalyticsStore.cs`

```csharp
namespace NexTraceOne.Infrastructure.Analytics.Elasticsearch;

public sealed class ElasticsearchAnalyticsStore : IAnalyticsStore
{
    private readonly ElasticsearchClient _client;
    private readonly ElasticsearchOptions _options;

    public string ProviderName => "Elasticsearch";

    public ElasticsearchAnalyticsStore(
        ElasticsearchOptions options,
        ILogger<ElasticsearchAnalyticsStore> logger)
    {
        _options = options;
        var settings = new ElasticsearchClientSettings(new Uri(options.Uri));
        if (!string.IsNullOrEmpty(options.Username))
            settings.Authentication(new BasicAuthentication(options.Username, options.Password!));
        _client = new ElasticsearchClient(settings);
    }

    public async Task InsertAsync<T>(string collection, T record, CancellationToken ct = default)
        where T : class
    {
        var index = $"{_options.IndexPrefix}.{collection}";
        await _client.IndexAsync(record, idx => idx.Index(index), ct);
    }

    public async Task BulkInsertAsync<T>(string collection, IEnumerable<T> records, CancellationToken ct = default)
        where T : class
    {
        var index = $"{_options.IndexPrefix}.{collection}";
        var batch = records.ToList();
        if (batch.Count == 0) return;

        await _client.BulkAsync(b => b
            .Index(index)
            .IndexMany(batch), ct);
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(AnalyticsQuery query, CancellationToken ct = default)
        where T : class
    {
        var index = $"{_options.IndexPrefix}.{query.Collection}";
        var request = BuildEsQuery(query);

        var response = await _client.SearchAsync<T>(s => s
            .Index(index)
            .Query(request)
            .From(query.Offset)
            .Size(query.Limit > 0 ? query.Limit : 100), ct);

        return response.Documents.ToList();
    }

    public async Task<long> CountAsync(AnalyticsQuery query, CancellationToken ct = default)
    {
        var index = $"{_options.IndexPrefix}.{query.Collection}";
        var request = BuildEsQuery(query);

        var response = await _client.CountAsync<object>(c => c
            .Index(index)
            .Query(request), ct);

        return response.Count;
    }

    private static Action<QueryDescriptor<object>> BuildEsQuery(AnalyticsQuery query)
    {
        return q =>
        {
            var musts = new List<Action<QueryDescriptor<object>>>();

            if (query.From.HasValue || query.To.HasValue)
                musts.Add(m => m.DateRange(dr =>
                {
                    dr.Field("@timestamp");
                    if (query.From.HasValue) dr.Gte(query.From.Value.UtcDateTime);
                    if (query.To.HasValue) dr.Lte(query.To.Value.UtcDateTime);
                }));

            foreach (var (key, value) in query.Filters)
                if (value is not null)
                    musts.Add(m => m.Term(t => t.Field(key).Value(value.ToString()!)));

            if (!string.IsNullOrEmpty(query.FullTextQuery))
                musts.Add(m => m.MultiMatch(mm => mm
                    .Query(query.FullTextQuery)
                    .Fields(["title", "content", "description"])));

            if (musts.Count > 0)
                q.Bool(b => b.Must(musts.ToArray()));
            else
                q.MatchAll(_ => { });
        };
    }
}
```

---

## 8. Padrão de dual-write (período de migração)

Durante a migração de uma tabela de PostgreSQL para o Analytics Store, usa-se dual-write:

```csharp
// No handler de domínio (application layer)
public sealed class RecordMetricsSnapshotHandler(
    IObservabilityDbContext db,
    IAnalyticsStore analyticsStore) : ICommandHandler<RecordMetricsSnapshotCommand>
{
    public async Task Handle(RecordMetricsSnapshotCommand cmd, CancellationToken ct)
    {
        var snapshot = ServiceMetricsSnapshot.Create(cmd);

        // Write 1: PostgreSQL (transaccional — fonte de verdade durante migração)
        db.ServiceMetricsSnapshots.Add(snapshot);
        await db.SaveChangesAsync(ct);

        // Write 2: Analytics Store (fire-and-forget durante dual-write)
        // Após validação, o write PG é removido e só este fica.
        _ = analyticsStore.InsertAsync("service_metrics_snapshots",
            MetricsSnapshotDto.From(snapshot), ct)
            .ContinueWith(t => /* log error */ _ = t.Exception, ct,
                TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
    }
}
```

**Fases do dual-write por tabela:**

| Semana | Estado |
|--------|--------|
| 0-1 | Dual-write activo. PG é fonte de verdade. Analytics Store recebe cópias. |
| 2-3 | Readers migrados para Analytics Store. PG write ainda activo. |
| 4 | Validação: comparar row counts PG vs Analytics Store. |
| 5+ | Remover write PG. Analytics Store é única fonte de verdade. |

---

## 9. Health Check do store

**Localização:** `src/BuildingBlocks/NexTraceOne.BuildingBlocks.Analytics/AnalyticsStoreHealthCheck.cs`

```csharp
public sealed class AnalyticsStoreHealthCheck(IAnalyticsStore store) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var count = await store.CountAsync(
                new AnalyticsQuery { Collection = "_health", Limit = 1 }, ct);
            return HealthCheckResult.Healthy($"Provider: {store.ProviderName}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Analytics store unhealthy ({store.ProviderName})", ex);
        }
    }
}
```

Registo:
```csharp
builder.Services.AddHealthChecks()
    .AddCheck<AnalyticsStoreHealthCheck>("analytics_store");
```

---

## 10. Estrutura de projectos BuildingBlocks

```
src/BuildingBlocks/
├── NexTraceOne.BuildingBlocks.Analytics/
│   ├── IAnalyticsStore.cs
│   ├── AnalyticsQuery.cs
│   ├── AnalyticsStoreOptions.cs
│   ├── NullAnalyticsStore.cs
│   ├── ServiceCollectionExtensions.cs
│   └── AnalyticsStoreHealthCheck.cs
│
src/Infrastructure/
├── NexTraceOne.Infrastructure.Analytics.ClickHouse/
│   ├── ClickHouseAnalyticsStore.cs
│   └── Mapping/  (source-generated row mappers)
│
└── NexTraceOne.Infrastructure.Analytics.Elasticsearch/
    ├── ElasticsearchAnalyticsStore.cs
    └── IndexMapping/  (index templates como embedded resources)
```
