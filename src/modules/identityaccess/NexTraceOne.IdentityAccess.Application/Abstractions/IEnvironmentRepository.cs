using NexTraceOne.IdentityAccess.Domain.Entities;

using Environment = NexTraceOne.IdentityAccess.Domain.Entities.Environment;

namespace NexTraceOne.IdentityAccess.Application.Abstractions;

/// <summary>
/// Repositório de ambientes e acessos por ambiente.
/// Centraliza operações de leitura e escrita para as entidades Environment e EnvironmentAccess,
/// usadas na autorização granular por estágio do ciclo de vida (Development → Production).
/// </summary>
public interface IEnvironmentRepository
{
    /// <summary>Obtém um ambiente pelo identificador.</summary>
    Task<Environment?> GetByIdAsync(EnvironmentId id, CancellationToken cancellationToken);

    /// <summary>
    /// Obtém um ambiente pelo identificador, garantindo que pertence ao tenant informado.
    /// Retorna null se não encontrado ou se pertencer a outro tenant.
    /// </summary>
    Task<Environment?> GetByIdForTenantAsync(EnvironmentId id, TenantId tenantId, CancellationToken cancellationToken);

    /// <summary>Lista ambientes ativos de um tenant, ordenados por SortOrder.</summary>
    Task<IReadOnlyList<Environment>> ListByTenantAsync(TenantId tenantId, CancellationToken cancellationToken);

    /// <summary>
    /// Obtém o ambiente designado como produção principal ativo do tenant.
    /// Retorna null se nenhum ambiente estiver designado como produção principal.
    /// </summary>
    Task<Environment?> GetPrimaryProductionAsync(TenantId tenantId, CancellationToken cancellationToken);

    /// <summary>Verifica se o slug já existe no tenant.</summary>
    Task<bool> SlugExistsAsync(TenantId tenantId, string slug, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo ambiente para persistência.</summary>
    void Add(Environment environment);

    /// <summary>Obtém o acesso de um usuário a um ambiente específico dentro de um tenant.</summary>
    Task<EnvironmentAccess?> GetAccessAsync(UserId userId, TenantId tenantId, EnvironmentId environmentId, CancellationToken cancellationToken);

    /// <summary>Lista todos os acessos ativos de um usuário em um tenant.</summary>
    Task<IReadOnlyList<EnvironmentAccess>> ListUserAccessesAsync(UserId userId, TenantId tenantId, CancellationToken cancellationToken);

    /// <summary>Lista acessos expirados ou prestes a expirar para processamento por job.</summary>
    Task<IReadOnlyList<EnvironmentAccess>> ListExpiredAccessesAsync(DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo acesso de ambiente para persistência.</summary>
    void AddAccess(EnvironmentAccess access);
}
