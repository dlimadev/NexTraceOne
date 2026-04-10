using NexTraceOne.Configuration.Domain.Entities;
using NexTraceOne.Configuration.Domain.Enums;

namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>
/// Repositório para políticas de compliance contratual configuráveis por âmbito.
/// </summary>
public interface IContractCompliancePolicyRepository
{
    /// <summary>Obtém uma política de compliance pelo seu identificador.</summary>
    Task<ContractCompliancePolicy?> GetByIdAsync(ContractCompliancePolicyId id, CancellationToken cancellationToken);

    /// <summary>Lista políticas de compliance por tenant.</summary>
    Task<IReadOnlyList<ContractCompliancePolicy>> ListByTenantAsync(string tenantId, CancellationToken cancellationToken);

    /// <summary>Obtém uma política de compliance por âmbito e identificador de âmbito.</summary>
    Task<ContractCompliancePolicy?> GetByScopeAsync(string tenantId, PolicyScope scope, string? scopeId, CancellationToken cancellationToken);

    /// <summary>Lista políticas de compliance ativas por âmbito.</summary>
    Task<IReadOnlyList<ContractCompliancePolicy>> ListActiveByScopeAsync(string tenantId, PolicyScope scope, CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova política de compliance.</summary>
    Task AddAsync(ContractCompliancePolicy policy, CancellationToken cancellationToken);

    /// <summary>Remove uma política de compliance pelo seu identificador.</summary>
    Task DeleteAsync(ContractCompliancePolicyId id, CancellationToken cancellationToken);
}
