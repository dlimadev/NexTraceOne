namespace NexTraceOne.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Abstração para consulta de permissões temporárias JIT (Just-In-Time) ativas.
/// Permite que o handler de autorização considere grants temporários aprovados
/// sem depender diretamente do módulo IdentityAccess.
/// </summary>
public interface IJitPermissionProvider
{
    /// <summary>
    /// Verifica se o utilizador possui um grant JIT ativo para a permissão especificada.
    /// </summary>
    /// <param name="userId">Identificador do utilizador.</param>
    /// <param name="permissionCode">Código da permissão requerida.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns><c>true</c> se existir um grant JIT ativo para esta permissão.</returns>
    Task<bool> HasActiveJitGrantAsync(string userId, string permissionCode, CancellationToken cancellationToken);
}
