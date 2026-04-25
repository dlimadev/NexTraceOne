using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.Integrations.Domain.Enums;

namespace NexTraceOne.Integrations.Domain.Entities;

/// <summary>
/// Identificador fortemente tipado para LogToMetricRule.
/// </summary>
public sealed record LogToMetricRuleId(Guid Value) : TypedIdBase(Value);

/// <summary>
/// Regra de transformação server-side de logs em métricas (log → metric pipeline).
/// Quando um log satisfaz o padrão Pattern, o LogToMetricProcessor emite uma
/// métrica via ITelemetryWriterService com o nome MetricName e o valor extraído
/// do campo ValueExtractor (ou constante 1 para contadores).
///
/// Owner: módulo Integrations (Pipeline).
/// </summary>
public sealed class LogToMetricRule : Entity<LogToMetricRuleId>
{
    /// <summary>Tenant proprietário da regra.</summary>
    public string TenantId { get; private init; } = string.Empty;

    /// <summary>Nome descritivo da regra (ex: "HTTP 5xx → error_rate metric").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Padrão de matching — expressão JSON path condition ou regex.
    /// Ex: {"field": "$.level", "operator": "eq", "value": "error"}
    /// </summary>
    public string Pattern { get; private set; } = string.Empty;

    /// <summary>Nome da métrica a emitir (ex: "http.error.count").</summary>
    public string MetricName { get; private set; } = string.Empty;

    /// <summary>Tipo de métrica a emitir.</summary>
    public MetricType MetricType { get; private set; }

    /// <summary>
    /// Campo do log body de onde extrair o valor da métrica (ex: "$.duration_ms").
    /// Null ou "1" significa que o valor é sempre 1 (adequado para contadores).
    /// </summary>
    public string? ValueExtractor { get; private set; }

    /// <summary>
    /// Campos do log a promover como labels da métrica.
    /// JSON array de campo paths: ["$.service", "$.environment"]
    /// </summary>
    public string LabelsJson { get; private set; } = "[]";

    /// <summary>Indica se a regra está activa.</summary>
    public bool IsEnabled { get; private set; }

    /// <summary>Data/hora UTC de criação.</summary>
    public DateTimeOffset CreatedAt { get; private init; }

    /// <summary>Data/hora UTC da última atualização.</summary>
    public DateTimeOffset? UpdatedAt { get; private set; }

    private LogToMetricRule() { }

    /// <summary>Cria uma nova regra de transformação log → métrica.</summary>
    public static LogToMetricRule Create(
        string tenantId,
        string name,
        string pattern,
        string metricName,
        MetricType metricType,
        string? valueExtractor,
        string labelsJson,
        bool isEnabled,
        DateTimeOffset utcNow)
    {
        Guard.Against.NullOrWhiteSpace(tenantId, nameof(tenantId));
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.StringTooLong(name, 150, nameof(name));
        Guard.Against.NullOrWhiteSpace(pattern, nameof(pattern));
        Guard.Against.NullOrWhiteSpace(metricName, nameof(metricName));
        Guard.Against.StringTooLong(metricName, 200, nameof(metricName));

        return new LogToMetricRule
        {
            Id = new LogToMetricRuleId(Guid.NewGuid()),
            TenantId = tenantId,
            Name = name.Trim(),
            Pattern = pattern,
            MetricName = metricName.Trim(),
            MetricType = metricType,
            ValueExtractor = string.IsNullOrWhiteSpace(valueExtractor) ? null : valueExtractor,
            LabelsJson = string.IsNullOrWhiteSpace(labelsJson) ? "[]" : labelsJson,
            IsEnabled = isEnabled,
            CreatedAt = utcNow
        };
    }

    /// <summary>Actualiza a regra.</summary>
    public void Update(
        string name,
        string pattern,
        string metricName,
        MetricType metricType,
        string? valueExtractor,
        string labelsJson,
        bool isEnabled,
        DateTimeOffset utcNow)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.StringTooLong(name, 150, nameof(name));
        Guard.Against.NullOrWhiteSpace(pattern, nameof(pattern));
        Guard.Against.NullOrWhiteSpace(metricName, nameof(metricName));
        Guard.Against.StringTooLong(metricName, 200, nameof(metricName));

        Name = name.Trim();
        Pattern = pattern;
        MetricName = metricName.Trim();
        MetricType = metricType;
        ValueExtractor = string.IsNullOrWhiteSpace(valueExtractor) ? null : valueExtractor;
        LabelsJson = string.IsNullOrWhiteSpace(labelsJson) ? "[]" : labelsJson;
        IsEnabled = isEnabled;
        UpdatedAt = utcNow;
    }

    /// <summary>Activa a regra.</summary>
    public void Enable(DateTimeOffset utcNow) { IsEnabled = true; UpdatedAt = utcNow; }

    /// <summary>Desactiva a regra.</summary>
    public void Disable(DateTimeOffset utcNow) { IsEnabled = false; UpdatedAt = utcNow; }
}
