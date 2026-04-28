using NexTraceOne.IdentityAccess.Application.Abstractions;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Infrastructure.Services;

/// <summary>
/// SaaS-01: Resolve capabilities de licença por plano.
/// Consulta o repositório de licenças para determinar o plano actual do tenant.
/// </summary>
internal sealed class DefaultCapabilityResolver(
    ITenantLicenseRepository licenseRepository) : ICapabilityResolver
{
    public IReadOnlyList<string> GetCapabilities(TenantPlan plan)
        => TenantCapabilities.ForPlan(plan);

    public async Task<TenantPlan> ResolvePlanAsync(Guid tenantId, CancellationToken ct = default)
    {
        var license = await licenseRepository.GetByTenantIdAsync(tenantId, ct);
        return license?.Plan ?? TenantPlan.Starter;
    }
}
