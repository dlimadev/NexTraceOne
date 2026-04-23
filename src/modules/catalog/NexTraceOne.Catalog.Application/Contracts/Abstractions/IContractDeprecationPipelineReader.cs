namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstracção para leitura de dados do pipeline de deprecação de contratos.
/// Agrega ContractDefinition (estados Deprecated/Sunset), ContractConsumer e AuditEvent.
/// Por omissão satisfeita por <c>NullContractDeprecationPipelineReader</c> (honest-null).
/// Wave AV.1 — GetContractDeprecationPipelineReport.
/// </summary>
public interface IContractDeprecationPipelineReader
{
    Task<IReadOnlyList<DeprecatedContractEntry>> ListDeprecatedContractsByTenantAsync(
        string tenantId,
        CancellationToken ct);

    /// <summary>Entrada de contrato em processo de deprecação.</summary>
    public sealed record DeprecatedContractEntry(
        Guid ContractId,
        string ContractName,
        string ContractVersion,
        string Protocol,
        string? OwnerTeamId,
        string ServiceId,
        string ServiceTier,
        DateTimeOffset DeprecatedAt,
        DateTimeOffset? SunsetDeadline,
        int TotalConsumers,
        int NotifiedConsumers,
        int MigratedConsumers,
        IReadOnlyList<string> BlockingConsumerIds,
        DateTimeOffset? FirstNotificationSentAt);
}
