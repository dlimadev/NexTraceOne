using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Repositório de permissões do módulo Identity.
/// </summary>
public interface IPermissionRepository
{
    /// <summary>Obtém todas as permissões registradas.</summary>
    Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken cancellationToken);
}
