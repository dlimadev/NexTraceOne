namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>
/// Fornece um snapshot de observabilidade do pipeline de ingestão a partir de dados reais.
/// Implementada na camada de infraestrutura usando BuildingBlocksDbContext (DLQ counts).
/// </summary>
public interface IIngestionObservabilityProvider
{
    /// <summary>Obtém o snapshot actual de observabilidade do pipeline de ingestão.</summary>
    Task<IngestionObservabilitySnapshot> GetSnapshotAsync(CancellationToken ct);
}

/// <summary>Snapshot de observabilidade do pipeline de ingestão.</summary>
public sealed record IngestionObservabilitySnapshot(
    IngestionDlqStats Dlq,
    DateTimeOffset CheckedAt);

/// <summary>Estatísticas da Dead Letter Queue do pipeline de ingestão.</summary>
public sealed record IngestionDlqStats(
    int Total,
    int Pending,
    int Reprocessing,
    int Resolved,
    int Discarded);
