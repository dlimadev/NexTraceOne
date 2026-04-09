using NexTraceOne.Catalog.Domain.Contracts.Entities;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório para resultados de avaliação de compliance contratual.
/// </summary>
public interface IContractComplianceResultRepository
{
    /// <summary>Obtém um resultado de compliance pelo seu identificador.</summary>
    Task<ContractComplianceResult?> GetByIdAsync(ContractComplianceResultId id, CancellationToken cancellationToken);

    /// <summary>Lista resultados de compliance por gate.</summary>
    Task<IReadOnlyList<ContractComplianceResult>> ListByGateAsync(Guid gateId, CancellationToken cancellationToken);

    /// <summary>Lista resultados de compliance por versão de contrato.</summary>
    Task<IReadOnlyList<ContractComplianceResult>> ListByContractVersionAsync(string contractVersionId, CancellationToken cancellationToken);

    /// <summary>Adiciona um novo resultado de compliance.</summary>
    Task AddAsync(ContractComplianceResult result, CancellationToken cancellationToken);
}
