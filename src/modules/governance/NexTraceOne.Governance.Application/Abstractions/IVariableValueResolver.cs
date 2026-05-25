using NexTraceOne.Governance.Domain.Entities;

namespace NexTraceOne.Governance.Application.Abstractions;

/// <summary>
/// Resolve os valores possíveis para uma variável de dashboard com base no tipo e fonte.
/// Implementado na camada de infraestrutura para consultar Catalog, Governance, etc.
/// </summary>
public interface IVariableValueResolver
{
    /// <summary>
    /// Resolve os valores possíveis para uma variável.
    /// </summary>
    Task<IReadOnlyList<string>> ResolveAsync(
        DashboardVariableType type,
        DashboardVariableSource source,
        IReadOnlyList<string>? staticValues,
        string tenantId,
        string? environmentId,
        CancellationToken cancellationToken);
}
