using NexTraceOne.AIKnowledge.Application.Governance.Features.GetModelDriftReport;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Reader para dados de drift de modelos de IA — compara distribuição actual vs. baseline.
/// Por omissão satisfeita por <c>NullModelDriftReader</c> (honest-null).
/// Wave AT.1 — AI Model Quality &amp; Drift Governance.
/// </summary>
public interface IModelDriftReader
{
    /// <summary>
    /// Retorna dados de drift por modelo no período especificado, com comparação vs. baseline
    /// (primeiro período ou snapshot de referência).
    /// </summary>
    Task<IReadOnlyList<GetModelDriftReport.ModelDriftRow>> GetDriftRowsAsync(
        string tenantId,
        DateTimeOffset baselineFrom,
        DateTimeOffset baselineTo,
        DateTimeOffset currentFrom,
        DateTimeOffset currentTo,
        CancellationToken ct);

    /// <summary>
    /// Retorna série temporal de InputDriftScore e OutputDriftScore diário (30d).
    /// </summary>
    Task<IReadOnlyList<GetModelDriftReport.DriftTimelinePoint>> GetDriftTimelineAsync(
        Guid modelId,
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct);
}
