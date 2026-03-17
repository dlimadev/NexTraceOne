using NexTraceOne.IdentityAccess.Domain.Entities;
using NexTraceOne.IdentityAccess.Domain.ValueObjects;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Repositório de usuários do módulo Identity.
/// </summary>
public interface IUserRepository
{
    /// <summary>Obtém um usuário pelo identificador.</summary>
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken);

    /// <summary>Obtém um usuário pelo email normalizado.</summary>
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken);

    /// <summary>Obtém um usuário pelo vínculo federado.</summary>
    Task<User?> GetByFederatedIdentityAsync(string provider, string externalId, CancellationToken cancellationToken);

    /// <summary>Verifica se já existe um usuário com o email informado.</summary>
    Task<bool> ExistsAsync(Email email, CancellationToken cancellationToken);

    /// <summary>Obtém um conjunto de usuários por identificador.</summary>
    Task<IReadOnlyDictionary<UserId, User>> GetByIdsAsync(IReadOnlyCollection<UserId> ids, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo usuário para persistência.</summary>
    void Add(User user);
}
