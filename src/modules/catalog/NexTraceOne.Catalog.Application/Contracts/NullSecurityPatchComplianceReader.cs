using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>
/// Implementação null (honest-null) de ISecurityPatchComplianceReader.
/// Retorna lista vazia — sem dados de compliance de patching disponíveis.
/// Wave AX.2 — GetSecurityPatchComplianceReport.
/// </summary>
public sealed class NullSecurityPatchComplianceReader : ISecurityPatchComplianceReader
{
    public Task<IReadOnlyList<ISecurityPatchComplianceReader.PatchComplianceEntry>>
        ListByTenantAsync(string tenantId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ISecurityPatchComplianceReader.PatchComplianceEntry>>([]);
}
