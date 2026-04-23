using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetAiModelQualityReport;

namespace NexTraceOne.AIKnowledge.Application.Governance.Services;

/// <summary>
/// Implementação null (honest-null) de IAiModelQualityReader.
/// Retorna sempre lista vazia — serve como bridge sem infra real.
/// Wave AT.2 — GetAiModelQualityReport.
/// </summary>
public sealed class NullAiModelQualityReader : IAiModelQualityReader
{
    public Task<IReadOnlyList<GetAiModelQualityReport.ModelQualityRow>> GetQualityRowsAsync(
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        int minSamples,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<GetAiModelQualityReport.ModelQualityRow>>([]);
}
