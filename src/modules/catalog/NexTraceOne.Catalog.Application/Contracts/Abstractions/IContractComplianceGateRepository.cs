using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório para gates de compliance contratual.
/// </summary>
public interface IContractComplianceGateRepository
{
    /// <summary>Obtém um gate de compliance pelo seu identificador.</summary>
    Task<ContractComplianceGate?> GetByIdAsync(ContractComplianceGateId id, CancellationToken cancellationToken);

    /// <summary>Lista gates de compliance por âmbito e identificador de âmbito.</summary>
    Task<IReadOnlyList<ContractComplianceGate>> ListByScopeAsync(ComplianceGateScope scope, string scopeId, CancellationToken cancellationToken);

    /// <summary>Lista todos os gates de compliance ativos.</summary>
    Task<IReadOnlyList<ContractComplianceGate>> ListActiveAsync(CancellationToken cancellationToken);

    /// <summary>Adiciona um novo gate de compliance.</summary>
    Task AddAsync(ContractComplianceGate gate, CancellationToken cancellationToken);
}
