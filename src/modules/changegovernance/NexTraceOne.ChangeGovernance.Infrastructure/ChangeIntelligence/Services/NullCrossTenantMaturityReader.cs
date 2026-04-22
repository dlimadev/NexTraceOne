using NexTraceOne.ChangeGovernance.Application.Compliance.Abstractions;

namespace NexTraceOne.ChangeGovernance.Infrastructure.ChangeIntelligence.Services;

/// <summary>
/// Implementação honest-null de <see cref="ICrossTenantMaturityReader"/>.
/// Retorna scores neutros (50) e lista vazia de tenants quando o bridge
/// com os módulos Catalog, OI e AIKnowledge não está configurado.
///
/// Wave AJ.1 — GetCrossTenantMaturityReport (ChangeGovernance Compliance).
/// </summary>
internal sealed class NullCrossTenantMaturityReader : ICrossTenantMaturityReader
{
    private static readonly ICrossTenantMaturityReader.TenantMaturityDimensions NeutralDimensions =
        new(string.Empty, 50m, 50m, 50m, 50m, 50m, 50m, 50m);

    /// <inheritdoc/>
    public Task<ICrossTenantMaturityReader.TenantMaturityDimensions> GetDimensionsAsync(
        string tenantId,
        DateTimeOffset since,
        CancellationToken cancellationToken = default)
        => Task.FromResult(NeutralDimensions with { TenantId = tenantId });

    /// <inheritdoc/>
    public Task<IReadOnlyList<ICrossTenantMaturityReader.TenantMaturityDimensions>> ListConsentedTenantDimensionsAsync(
        DateTimeOffset since,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ICrossTenantMaturityReader.TenantMaturityDimensions>>([]);
}
