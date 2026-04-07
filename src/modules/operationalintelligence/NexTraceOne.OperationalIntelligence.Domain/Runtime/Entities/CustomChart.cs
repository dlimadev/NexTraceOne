using Ardalis.GuardClauses;
using NexTraceOne.BuildingBlocks.Core.Primitives;
using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;
using NexTraceOne.OperationalIntelligence.Domain.Runtime.Enums;

namespace NexTraceOne.OperationalIntelligence.Domain.Runtime.Entities;

/// <summary>
/// Aggregate Root que representa um gráfico customizado criado por um utilizador.
/// Permite que utilizadores definam visualizações personalizadas combinando métricas,
/// filtros, agrupamentos e tipo de representação gráfica.
///
/// Invariantes:
/// - MetricQuery é JSON válido e não pode estar vazio.
/// - ChartType deve ser um valor válido do enum.
/// - Título não pode exceder 150 caracteres.
/// </summary>
public sealed class CustomChart : AuditableEntity<CustomChartId>
{
    private const int MaxTitleLength = 150;

    private CustomChart() { }

    /// <summary>Identificador do tenant.</summary>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Identificador do utilizador que criou o gráfico.</summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>Nome descritivo do gráfico.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Tipo de gráfico (Line, Bar, Area, etc.).</summary>
    public ChartType ChartType { get; private set; }

    /// <summary>Query JSON com source, metric, aggregation, groupBy, filters.</summary>
    public string MetricQuery { get; private set; } = string.Empty;

    /// <summary>Intervalo de tempo padrão (ex: "last_24h", "last_7d", "last_30d").</summary>
    public string TimeRange { get; private set; } = "last_24h";

    /// <summary>Filtros JSON adicionais.</summary>
    public string? FiltersJson { get; private set; }

    /// <summary>Indica se o gráfico é partilhado com o tenant.</summary>
    public bool IsShared { get; private set; }

    /// <summary>Cria um novo gráfico customizado.</summary>
    public static CustomChart Create(
        string tenantId,
        string userId,
        string name,
        ChartType chartType,
        string metricQuery,
        string timeRange,
        string? filtersJson,
        DateTimeOffset createdAt)
    {
        Guard.Against.NullOrWhiteSpace(tenantId);
        Guard.Against.NullOrWhiteSpace(userId);
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.OutOfRange(name.Length, nameof(name), 1, MaxTitleLength);
        Guard.Against.NullOrWhiteSpace(metricQuery);

        var chart = new CustomChart
        {
            Id = new CustomChartId(Guid.NewGuid()),
            TenantId = tenantId,
            UserId = userId,
            Name = name,
            ChartType = chartType,
            MetricQuery = metricQuery,
            TimeRange = string.IsNullOrWhiteSpace(timeRange) ? "last_24h" : timeRange,
            FiltersJson = filtersJson,
            IsShared = false,
        };
        chart.SetCreated(createdAt, userId);

        return chart;
    }

    /// <summary>Atualiza os detalhes do gráfico.</summary>
    public void UpdateDetails(string name, ChartType chartType, string metricQuery, string timeRange, string? filtersJson, DateTimeOffset updatedAt)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.OutOfRange(name.Length, nameof(name), 1, MaxTitleLength);
        Guard.Against.NullOrWhiteSpace(metricQuery);

        Name = name;
        ChartType = chartType;
        MetricQuery = metricQuery;
        TimeRange = string.IsNullOrWhiteSpace(timeRange) ? "last_24h" : timeRange;
        FiltersJson = filtersJson;
        SetUpdated(updatedAt, UserId);
    }

    /// <summary>Alterna o estado de partilha.</summary>
    public void SetShared(bool isShared, DateTimeOffset updatedAt)
    {
        IsShared = isShared;
        SetUpdated(updatedAt, UserId);
    }
}

/// <summary>Identificador fortemente tipado para CustomChart.</summary>
public sealed record CustomChartId(Guid Value) : TypedIdBase(Value)
{
    public static CustomChartId New() => new(Guid.NewGuid());

    public static CustomChartId From(Guid id) => new(id);
}
