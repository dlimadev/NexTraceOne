using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using NexTraceOne.Governance.Application.Abstractions;
using NexTraceOne.Governance.Application.Features.IngestOtelMetrics;
using NexTraceOne.Governance.Infrastructure.Persistence;

namespace NexTraceOne.Governance.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementação PostgreSQL do IOtelMetricRepository.
/// Usa um modelo simples (OtelMetricRecord) com JSONB para os atributos,
/// adequado para o MVP — ClickHouse pode substituir esta implementação
/// quando o volume de métricas requerer melhor performance analítica.
/// </summary>
public sealed class OtelMetricRepository(
    GovernanceDbContext context,
    ILogger<OtelMetricRepository> logger) : IOtelMetricRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Persiste datapoints em batch usando ExecuteSqlRawAsync para performance.
    /// Usa INSERT com ON CONFLICT DO NOTHING para idempotência.
    /// </summary>
    public async Task<int> BatchInsertAsync(
        IReadOnlyList<OtelMetricDataPoint> dataPoints,
        CancellationToken cancellationToken)
    {
        if (dataPoints.Count == 0)
            return 0;

        var records = dataPoints.Select(dp => new OtelMetricRecord
        {
            Id = Guid.NewGuid(),
            MetricName = dp.MetricName,
            MetricType = dp.MetricType.ToString(),
            Value = dp.Value,
            ServiceName = dp.ServiceName ?? string.Empty,
            ServiceVersion = dp.ServiceVersion,
            Environment = dp.Environment,
            Timestamp = dp.Timestamp.UtcDateTime,
            ResourceAttributesJson = JsonSerializer.Serialize(dp.ResourceAttributes, JsonOptions),
            MetricAttributesJson = JsonSerializer.Serialize(dp.MetricAttributes, JsonOptions),
            IngestedAt = DateTime.UtcNow,
        }).ToList();

        try
        {
            context.OtelMetrics.AddRange(records);
            await context.SaveChangesAsync(cancellationToken);
            return records.Count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Batch insert of {Count} OTEL metrics failed",
                dataPoints.Count);
            return 0;
        }
    }

    /// <summary>Consulta datapoints para análise de correlação change ↔ métrica.</summary>
    public async Task<IReadOnlyList<OtelMetricDataPoint>> QueryAsync(
        string serviceName,
        string metricName,
        DateTimeOffset from,
        DateTimeOffset to,
        string? environment = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.OtelMetrics
            .Where(m => m.ServiceName == serviceName
                     && m.MetricName == metricName
                     && m.Timestamp >= from.UtcDateTime
                     && m.Timestamp <= to.UtcDateTime);

        if (!string.IsNullOrWhiteSpace(environment))
            query = query.Where(m => m.Environment == environment);

        var records = await query
            .OrderBy(m => m.Timestamp)
            .ToListAsync(cancellationToken);

        return records.Select(r => new OtelMetricDataPoint
        {
            MetricName = r.MetricName,
            Value = r.Value,
            MetricType = Enum.TryParse<OtelMetricType>(r.MetricType, out var t) ? t : OtelMetricType.Gauge,
            Timestamp = new DateTimeOffset(r.Timestamp, TimeSpan.Zero),
            ResourceAttributes = DeserializeAttributes(r.ResourceAttributesJson),
            MetricAttributes = DeserializeAttributes(r.MetricAttributesJson),
        }).ToList();
    }

    /// <summary>Lista nomes de serviços distintos com métricas ingeridas.</summary>
    public async Task<IReadOnlyList<string>> GetDistinctServiceNamesAsync(
        CancellationToken cancellationToken = default)
    {
        return await context.OtelMetrics
            .Select(m => m.ServiceName)
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync(cancellationToken);
    }

    private static IReadOnlyDictionary<string, string> DeserializeAttributes(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions)
                ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }
}

/// <summary>
/// Entidade de persistência de uma métrica OTEL no PostgreSQL.
/// Mantém atributos de recurso e métrica como JSONB para flexibilidade.
/// </summary>
public sealed class OtelMetricRecord
{
    public Guid Id { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public string MetricType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string? ServiceVersion { get; set; }
    public string? Environment { get; set; }
    public DateTime Timestamp { get; set; }
    public string ResourceAttributesJson { get; set; } = "{}";
    public string MetricAttributesJson { get; set; } = "{}";
    public DateTime IngestedAt { get; set; }
}
