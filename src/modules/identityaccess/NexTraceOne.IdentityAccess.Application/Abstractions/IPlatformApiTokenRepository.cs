using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>Repositório de tokens de acesso de plataforma para agentes autónomos (Wave D.4).</summary>
public interface IPlatformApiTokenRepository
{
    Task<PlatformApiToken?> GetByIdAsync(PlatformApiTokenId id, CancellationToken ct);
    Task<PlatformApiToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct);
    Task<IReadOnlyList<PlatformApiToken>> ListByTenantAsync(Guid tenantId, CancellationToken ct);
    Task AddAsync(PlatformApiToken token, CancellationToken ct);
    Task UpdateAsync(PlatformApiToken token, CancellationToken ct);
}
