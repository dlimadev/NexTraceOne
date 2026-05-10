using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>
/// Implementação null (honest-null) de <see cref="IBreakingChangeImpactReader"/>.
/// Retorna lista vazia — nenhuma breaking change registada.
/// Wave AE.2 — GetSchemaBreakingChangeImpactReport.
/// </summary>
public sealed class NullBreakingChangeImpactReader : IBreakingChangeImpactReader
{
    public Task<IReadOnlyList<BreakingChangeEntry>> ListBreakingChangesByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<BreakingChangeEntry>>([]);
}
