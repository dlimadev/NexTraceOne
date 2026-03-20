using MediatR;
using NexTraceOne.BuildingBlocks.Core.Results;
using NexTraceOne.IdentityAccess.Domain.Entities;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Abstração para validação de acesso de um usuário a um ambiente.
/// Centraliza as verificações de:
/// - O ambiente pertence ao tenant?
/// - O usuário tem acesso ao ambiente?
/// - O acesso está ativo e não expirado?
///
/// Princípio: nenhuma operação com escopo de ambiente deve prosseguir
/// sem validar explicitamente o acesso do usuário através deste validador.
/// </summary>
public interface IEnvironmentAccessValidator
{
    /// <summary>
    /// Valida que o ambiente pertence ao tenant e que o usuário possui acesso ativo.
    /// </summary>
    /// <param name="userId">Usuário solicitando acesso.</param>
    /// <param name="tenantId">Tenant ativo da requisição.</param>
    /// <param name="environmentId">Ambiente que se deseja acessar.</param>
    /// <param name="now">Data/hora UTC atual para verificar expiração.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>
    /// Ok se o acesso for válido;
    /// Forbidden se o ambiente não pertencer ao tenant ou o usuário não tiver acesso;
    /// NotFound se o ambiente não existir.
    /// </returns>
    Task<Result<Unit>> ValidateAsync(
        UserId userId,
        TenantId tenantId,
        EnvironmentId environmentId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica rapidamente se um usuário tem qualquer acesso ativo ao ambiente dado.
    /// Usado em guards de autorização de baixo custo.
    /// </summary>
    Task<bool> HasAccessAsync(
        UserId userId,
        TenantId tenantId,
        EnvironmentId environmentId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}
