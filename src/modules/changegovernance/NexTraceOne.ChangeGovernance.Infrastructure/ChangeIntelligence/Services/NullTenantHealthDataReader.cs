using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação honest-null de <see cref="ITenantHealthDataReader"/>.
/// Retorna scores neutros (50) quando o bridge com os módulos Catalog, OI e CG
/// não está configurado.
///
/// Wave AJ.2 — GetTenantHealthScoreReport (ChangeGovernance Compliance).
/// </summary>
internal sealed class NullTenantHealthDataReader : ITenantHealthDataReader
{
    /// <inheritdoc/>
    public Task<ITenantHealthDataReader.TenantHealthPillarData> GetPillarDataAsync(
        string tenantId,
        DateTimeOffset since,
        DateTimeOffset until,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new ITenantHealthDataReader.TenantHealthPillarData(
            TenantId: tenantId,
            ServiceGovernanceScore: 50m,
            ChangeConfidenceScore: 50m,
            OperationalReliabilityScore: 50m,
            ContractHealthScore: 50m,
            ComplianceCoverageScore: 50m,
            FinOpsEfficiencyScore: 50m));
}
