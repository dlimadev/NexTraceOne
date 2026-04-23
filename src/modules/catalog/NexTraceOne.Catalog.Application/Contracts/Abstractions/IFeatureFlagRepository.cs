using NexTraceOne.Catalog.Domain.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório de <see cref="FeatureFlagRecord"/> — persiste e consulta o estado
/// de feature flags por serviço e tenant.
/// Por omissão satisfeita por <c>NullFeatureFlagRepository</c> (honest-null).
/// Wave AS.1 — Feature Flag &amp; Experimentation Governance.
/// </summary>
public interface IFeatureFlagRepository
{
    Task UpsertAsync(FeatureFlagRecord record, CancellationToken ct);

    Task<IReadOnlyList<FeatureFlagRecord>> ListByTenantAsync(string tenantId, CancellationToken ct);

    Task<IReadOnlyList<FeatureFlagRecord>> ListByServiceAsync(string serviceId, string tenantId, CancellationToken ct);
}
