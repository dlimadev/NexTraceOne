namespace NexTraceOne.BuildingBlocks.Application.Nql;

/// <summary>
/// Resultado de uma query NQL executada pelo <see cref="IQueryGovernanceService"/>.
/// </summary>
public sealed record NqlQueryResult(
    bool IsSimulated,
    string? SimulatedNote,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    int TotalRows,
    string RenderHint,
    long ExecutionMs);
