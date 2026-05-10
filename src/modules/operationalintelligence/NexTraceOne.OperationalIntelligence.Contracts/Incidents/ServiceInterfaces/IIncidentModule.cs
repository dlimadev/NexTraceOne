namespace NexTraceOne.OperationalIntelligence.Contracts.Incidents.ServiceInterfaces;

// IMPLEMENTATION STATUS: Implemented — IncidentModuleService (Infrastructure).

/// <summary>
/// Interface pública do módulo Incidents para comunicação entre módulos.
/// Outros módulos que precisarem de dados de incidentes devem usar este contrato —
/// nunca acessar o DbContext ou repositórios diretamente.
/// Garante isolamento de base de dados entre módulos.
/// </summary>
public interface IIncidentModule
{
    /// <summary>
    /// Conta o total de incidentes abertos (status != Resolved e status != Closed).
    /// </summary>
    Task<int> CountOpenIncidentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Conta incidentes resolvidos nos últimos N dias.
    /// </summary>
    Task<int> CountResolvedInLastDaysAsync(int days = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calcula a média de horas de resolução de incidentes nos últimos N dias.
    /// Retorna 0 se não houver incidentes resolvidos no período.
    /// </summary>
    Task<decimal> GetAverageResolutionHoursAsync(int days = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calcula a taxa de recorrência de incidentes (0-100%) baseada em serviços
    /// com mais de um incidente nos últimos N dias.
    /// </summary>
    Task<decimal> GetRecurrenceRateAsync(int days = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém um resumo de tendência de incidentes com dados agregados.
    /// </summary>
    Task<IncidentTrendSummary> GetTrendSummaryAsync(int days = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna incidentes abertos ou detectados dentro de uma janela temporal.
    /// Usado por anotações de dashboard para sobreposição em séries temporais.
    /// </summary>
    Task<IReadOnlyList<IncidentSummaryDto>> GetRecentIncidentsAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        int maxCount = 50,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resumo de tendência de incidentes para consumo cross-module.
/// Contém métricas agregadas de incidentes para dashboards executivos.
/// </summary>
public sealed record IncidentTrendSummary(
    int OpenIncidents,
    int ResolvedInPeriod,
    decimal AvgResolutionHours,
    decimal RecurrenceRate,
    string Trend);

/// <summary>
/// Sumário de incidente para consumo cross-module em dashboards e anotações.
/// </summary>
public sealed record IncidentSummaryDto(
    Guid Id,
    string Title,
    string Severity,
    string? ServiceName,
    DateTimeOffset DetectedAt,
    string Status);
