using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>Repositório de licenças de tenant.</summary>
public interface ITenantLicenseRepository
{
    Task<TenantLicense?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<TenantLicense?> GetByIdAsync(TenantLicenseId id, CancellationToken ct = default);
    Task<IReadOnlyList<TenantLicense>> ListAllAsync(CancellationToken ct = default);
    void Add(TenantLicense license);
    void Update(TenantLicense license);
}
