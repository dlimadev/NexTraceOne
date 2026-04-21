namespace NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

/// <summary>
/// Abstração de leitura de métricas operacionais agregadas por equipa.
///
/// Fornece métricas pré-calculadas por equipa, obtidas a partir de observações
/// de SLO, drift findings, experimentos de chaos e sessões de profiling.
/// Desacopla o handler de relatório das implementações concretas de cada repositório.
///
/// Wave R.3 — Team Operational Health Report.
/// </summary>
public interface ITeamOperationalMetricsReader
{
    /// <summary>
    /// Lista métricas operacionais agregadas por equipa no período.
    /// Cada entrada representa uma equipa com os seus serviços associados
    /// e as métricas de saúde operacional calculadas.
    /// </summary>
    Task<IReadOnlyList<TeamOperationalMetrics>> ListTeamMetricsAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Métricas operacionais agregadas de uma equipa num período de tempo.
/// Combina dados de SLO, drift, chaos e profiling para um scorecard composto.
/// Wave R.3.
/// </summary>
public sealed record TeamOperationalMetrics(
    /// <summary>Nome da equipa.</summary>
    string TeamName,
    /// <summary>Total de serviços da equipa monitorados.</summary>
    int ServiceCount,
    /// <summary>Taxa de conformidade média de SLO dos serviços da equipa (0–100).</summary>
    decimal SloComplianceRatePct,
    /// <summary>Número de drift findings não reconhecidos nos serviços da equipa.</summary>
    int UnacknowledgedDriftCount,
    /// <summary>Taxa de sucesso dos experimentos de chaos nos serviços da equipa (0–100).</summary>
    decimal ChaosSuccessRatePct,
    /// <summary>Número de serviços com sessões de profiling recentes.</summary>
    int ServicesWithProfilingCount,
    /// <summary>Número de incidentes pós-deploy correlacionados no período.</summary>
    int PostDeployIncidentCount);
