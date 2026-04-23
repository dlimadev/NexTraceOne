using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiModelQualityReport;

namespace NexTraceOne.AIKnowledge.Application.Governance.Abstractions;

/// <summary>
/// Reader de qualidade de modelos de IA em produção — agrega métricas de accuracy,
/// confiança, latência de inferência e fallback rate.
/// Por omissão satisfeita por <c>NullAiModelQualityReader</c> (honest-null).
/// Wave AT.2 — AI Model Quality &amp; Drift Governance.
/// </summary>
public interface IAiModelQualityReader
{
    /// <summary>
    /// Retorna linhas de qualidade por modelo com amostras suficientes no período.
    /// </summary>
    Task<IReadOnlyList<GetAiModelQualityReport.ModelQualityRow>> GetQualityRowsAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        int minSamples,
        CancellationToken ct);
}
