using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Resolve as capabilities de licença de um tenant.
/// Capabilities são incluídas no JWT como claim "capabilities".
/// </summary>
public interface ICapabilityResolver
{
    /// <summary>Retorna as capabilities do plano dado.</summary>
    IReadOnlyList<string> GetCapabilities(TenantPlan plan);

    /// <summary>Resolve o plano actual de um tenant via repositório de licença.</summary>
    Task<TenantPlan> ResolvePlanAsync(Guid tenantId, CancellationToken ct = default);
}
