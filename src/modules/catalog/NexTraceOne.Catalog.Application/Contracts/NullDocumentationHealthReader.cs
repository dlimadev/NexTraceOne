using NexTraceOne.Catalog.Application.Contracts.Abstractions;

namespace NexTraceOne.Catalog.Application.Contracts;

/// <summary>
/// Implementação null (honest-null) de IDocumentationHealthReader.
/// Retorna lista vazia — sem dados de saúde de documentação disponíveis.
/// Wave AY.1 — GetDocumentationHealthReport.
/// </summary>
public sealed class NullDocumentationHealthReader : IDocumentationHealthReader
{
    public Task<IReadOnlyList<IDocumentationHealthReader.ServiceDocumentationEntry>>
        ListByTenantAsync(string tenantId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<IDocumentationHealthReader.ServiceDocumentationEntry>>([]);
}
