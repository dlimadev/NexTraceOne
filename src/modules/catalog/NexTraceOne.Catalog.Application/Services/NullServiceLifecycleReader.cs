using NexTraceOne.Catalog.Application.Services.Abstractions;

namespace NexTraceOne.Catalog.Application.Services;

/// <summary>
/// Implementação null (honest-null) de <see cref="IServiceLifecycleReader"/>.
/// Retorna lista vazia — nenhum serviço em transição de ciclo de vida registado.
/// Wave AF.1 — GetServiceLifecycleTransitionReport.
/// </summary>
public sealed class NullServiceLifecycleReader : IServiceLifecycleReader
{
    public Task<IReadOnlyList<ServiceLifecycleEntry>> ListByTenantAsync(
        string tenantId,
        int lookbackDays,
        CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ServiceLifecycleEntry>>([]);
}
