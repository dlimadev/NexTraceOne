using NexTraceOne.OperationalIntelligence.Application.Runtime.Abstractions;

namespace NexTraceOne.OperationalIntelligence.Infrastructure.Runtime.Services;

/// <summary>
/// Implementação honest-null de <see cref="IActiveServiceNamesReader"/>.
///
/// Devolve lista vazia, sinalizando que não há bridge configurado para o catálogo de serviços.
/// Relatórios que dependem desta abstracção derivarão a lista de serviços apenas dos dados
/// operacionais disponíveis (experimentos, snapshots, etc.) quando este provider está activo.
///
/// Wave V.2 — Chaos Coverage Gap Report.
/// </summary>
internal sealed class NullActiveServiceNamesReader : IActiveServiceNamesReader
{
    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListActiveServiceNamesAsync(
        string tenantId,
        CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<string>>([]);
}
