using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Repositório de Tenants do módulo Identity.
/// Responsável pela persistência e consulta de organizações/clientes.
/// </summary>
public interface ITenantRepository
{
    /// <summary>Obtém um tenant pelo identificador.</summary>
    Task<Tenant?> GetByIdAsync(TenantId id, CancellationToken cancellationToken);

    /// <summary>Obtém um tenant pelo slug único.</summary>
    Task<Tenant?> GetBySlugAsync(string slug, CancellationToken cancellationToken);

    /// <summary>Obtém vários tenants por uma coleção de Ids.</summary>
    Task<IReadOnlyDictionary<TenantId, Tenant>> GetByIdsAsync(
        IReadOnlyCollection<TenantId> ids,
        CancellationToken cancellationToken);

    /// <summary>Verifica se já existe um tenant com o slug informado.</summary>
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo tenant para persistência.</summary>
    void Add(Tenant tenant);
}
