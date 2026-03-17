using NexTraceOne.ChangeGovernance.Domain.Promotion.Entities;

namespace NexTraceOne.ChangeGovernance.Application.Promotion.Abstractions;

/// <summary>
/// Contrato de repositório para ambientes de deployment.
/// </summary>
public interface IDeploymentEnvironmentRepository
{
    /// <summary>Busca um ambiente de deployment pelo identificador.</summary>
    Task<DeploymentEnvironment?> GetByIdAsync(DeploymentEnvironmentId id, CancellationToken ct);

    /// <summary>Adiciona um novo ambiente de deployment ao contexto.</summary>
    void Add(DeploymentEnvironment env);

    /// <summary>Atualiza um ambiente de deployment existente no contexto.</summary>
    void Update(DeploymentEnvironment env);

    /// <summary>Busca um ambiente de deployment pelo nome.</summary>
    Task<DeploymentEnvironment?> GetByNameAsync(string name, CancellationToken ct);

    /// <summary>Lista todos os ambientes de deployment ativos.</summary>
    Task<IReadOnlyList<DeploymentEnvironment>> ListActiveAsync(CancellationToken ct);
}
