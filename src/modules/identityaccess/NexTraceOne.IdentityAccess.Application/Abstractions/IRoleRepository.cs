using NexTraceOne.Identity.Domain.Entities;

namespace NexTraceOne.Identity.Application.Abstractions;

/// <summary>
/// Repositório de papéis de autorização do módulo Identity.
/// </summary>
public interface IRoleRepository
{
    /// <summary>Obtém um papel pelo identificador.</summary>
    Task<Role?> GetByIdAsync(RoleId id, CancellationToken cancellationToken);

    /// <summary>Obtém um papel pelo nome único.</summary>
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken);

    /// <summary>Obtém todos os papéis pré-definidos do sistema.</summary>
    Task<IReadOnlyList<Role>> GetSystemRolesAsync(CancellationToken cancellationToken);

    /// <summary>Obtém vários papéis por uma coleção de Ids.</summary>
    Task<IReadOnlyDictionary<RoleId, Role>> GetByIdsAsync(
        IReadOnlyCollection<RoleId> ids,
        CancellationToken cancellationToken);
}
