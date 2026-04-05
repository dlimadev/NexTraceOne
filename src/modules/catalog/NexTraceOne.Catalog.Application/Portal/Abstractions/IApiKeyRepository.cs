using NexTraceOne.Catalog.Domain.Portal.Entities;

namespace NexTraceOne.Catalog.Application.Portal.Abstractions;

/// <summary>
/// Repositório de API Keys do Developer Portal.
/// </summary>
public interface IApiKeyRepository
{
    Task<ApiKey?> GetByIdAsync(ApiKeyId id, CancellationToken ct = default);
    Task<ApiKey?> GetByHashAsync(string keyHash, CancellationToken ct = default);
    Task<IReadOnlyList<ApiKey>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    Task<IReadOnlyList<ApiKey>> GetByApiAssetAsync(Guid apiAssetId, CancellationToken ct = default);
    Task<int> CountActiveByOwnerAsync(Guid ownerId, CancellationToken ct = default);
    void Add(ApiKey apiKey);
    void Update(ApiKey apiKey);
}
