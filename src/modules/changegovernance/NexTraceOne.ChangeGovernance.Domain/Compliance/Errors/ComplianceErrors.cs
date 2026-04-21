using NexTraceOne.BuildingBlocks.Core.Results;

namespace NexTraceOne.ChangeGovernance.Domain.Compliance.Errors;

/// <summary>
/// Catálogo de erros do sub-domínio Compliance / Benchmarks.
/// Padrão: Compliance.Benchmark.{Descrição}
/// </summary>
public static class ComplianceErrors
{
    /// <summary>Consentimento de benchmark não encontrado para o tenant.</summary>
    public static Error ConsentNotFound(string tenantId)
        => Error.NotFound("Compliance.Benchmark.ConsentNotFound", "Benchmark consent for tenant '{0}' was not found.", tenantId);

    /// <summary>Tenant não concedeu consentimento para participação nos benchmarks.</summary>
    public static Error ConsentNotGranted(string tenantId)
        => Error.Business("Compliance.Benchmark.ConsentNotGranted", "Tenant '{0}' has not granted consent for benchmark participation.", tenantId);

    /// <summary>Snapshot de benchmark não encontrado.</summary>
    public static Error SnapshotNotFound(string snapshotId)
        => Error.NotFound("Compliance.Benchmark.SnapshotNotFound", "Benchmark snapshot '{0}' was not found.", snapshotId);
}
