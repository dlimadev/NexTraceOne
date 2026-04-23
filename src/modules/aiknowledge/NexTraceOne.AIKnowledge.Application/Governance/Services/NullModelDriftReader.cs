using NexTraceOne.AIKnowledge.Application.Governance.Abstractions;
using NexTraceOne.AIKnowledge.Application.Governance.Features.GetModelDriftReport;

namespace NexTraceOne.AIKnowledge.Application.Governance.Services;

/// <summary>
/// Implementação null (honest-null) de IModelDriftReader.
/// Retorna sempre listas vazias — serve como bridge sem infra real.
/// Wave AT.1 — GetModelDriftReport.
/// </summary>
public sealed class NullModelDriftReader : IModelDriftReader
{
    public Task<IReadOnlyList<GetModelDriftReport.ModelDriftRow>> GetDriftRowsAsync(
        string tenantId,
        DateTimeOffset baselineFrom,
        DateTimeOffset baselineTo,
        DateTimeOffset currentFrom,
        DateTimeOffset currentTo,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<GetModelDriftReport.ModelDriftRow>>([]);

    public Task<IReadOnlyList<GetModelDriftReport.DriftTimelinePoint>> GetDriftTimelineAsync(
        Guid modelId,
        string tenantId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<GetModelDriftReport.DriftTimelinePoint>>([]);
}
