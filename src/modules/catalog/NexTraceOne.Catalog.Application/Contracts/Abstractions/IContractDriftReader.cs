namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Abstracção para leitura de dados de divergência entre contratos registados e runtime.
/// Cruza ApiAsset com RuntimeSnapshot para detectar ghost endpoints e operações não usadas.
/// Por omissão satisfeita por <c>NullContractDriftReader</c> (honest-null).
/// Wave AM.2 — GetContractDriftFromRealityReport.
/// </summary>
public interface IContractDriftReader
{
    Task<IReadOnlyList<ContractRuntimeObservation>> ListByTenantAsync(
        string tenantId,
        int lookbackDays,
        int unusedOpsStagnationDays,
        CancellationToken ct);

    /// <summary>Observação de runtime para um contrato específico.</summary>
    public sealed record ContractRuntimeObservation(
        string ContractId,
        string ContractName,
        string ServiceName,
        IReadOnlyList<string> DocumentedOperations,
        IReadOnlyList<string> ObservedOperations,
        IReadOnlyList<string> UndocumentedCalls,
        IReadOnlyList<UnusedOperation> UnusedDocumentedOps,
        IReadOnlyList<string> ParameterMismatches);

    /// <summary>Operação documentada sem chamadas no período.</summary>
    public sealed record UnusedOperation(
        string OperationId,
        int StagnationDays);
}
