using NexTraceOne.ChangeGovernance.Domain.ChangeIntelligence.Entities;

namespace NexTraceOne.ChangeGovernance.Application.ChangeIntelligence.Abstractions;

/// <summary>Contrato de repositório para ReleaseApprovalPolicy.</summary>
public interface IReleaseApprovalPolicyRepository
{
    /// <summary>Busca uma política pelo seu identificador.</summary>
    Task<ReleaseApprovalPolicy?> GetByIdAsync(ReleaseApprovalPolicyId id, CancellationToken cancellationToken = default);

    /// <summary>Lista todas as políticas activas do tenant, ordenadas por prioridade.</summary>
    Task<IReadOnlyList<ReleaseApprovalPolicy>> ListActiveAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Lista políticas aplicáveis a um par (environmentId, serviceId), ordenadas por prioridade.</summary>
    Task<IReadOnlyList<ReleaseApprovalPolicy>> ListByEnvironmentAndServiceAsync(
        Guid tenantId,
        string? environmentId,
        Guid? serviceId,
        CancellationToken cancellationToken = default);

    /// <summary>Adiciona uma nova política.</summary>
    void Add(ReleaseApprovalPolicy policy);

    /// <summary>Remove uma política (soft-delete via Deactivate).</summary>
    void Remove(ReleaseApprovalPolicy policy);
}
