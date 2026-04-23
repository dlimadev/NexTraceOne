using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação honest-null de <see cref="ICrossStandardComplianceGapReader"/>.
/// Retorna listas vazias até a infraestrutura real ser ligada.
/// Wave BB.1 — GetCrossStandardComplianceGapReport.
/// </summary>
internal sealed class NullCrossStandardComplianceGapReader : ICrossStandardComplianceGapReader
{
    public Task<IReadOnlyList<ICrossStandardComplianceGapReader.ComplianceGapEntry>> ListGapsByTenantAsync(
        string tenantId, IReadOnlyList<string> standards, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ICrossStandardComplianceGapReader.ComplianceGapEntry>>([]);
}
