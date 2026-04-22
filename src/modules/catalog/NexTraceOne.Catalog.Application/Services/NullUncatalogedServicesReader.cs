using NexTraceOne.Catalog.Application.Services.Abstractions;

namespace NexTraceOne.Catalog.Application.Services;

/// <summary>
/// Implementação null (honest-null) de IUncatalogedServicesReader.
/// Retorna catálogo com 10 serviços registados e lista vazia de não catalogados.
/// Wave AM.1 — GetUncatalogedServicesReport.
/// </summary>
public sealed class NullUncatalogedServicesReader : IUncatalogedServicesReader
{
    public Task<IUncatalogedServicesReader.UncatalogedServicesSummary> GetSummaryAsync(
        string tenantId, int lookbackDays, CancellationToken ct)
        => Task.FromResult(new IUncatalogedServicesReader.UncatalogedServicesSummary(
            CatalogedServiceCount: 10,
            UncatalogedServices: []));
}
