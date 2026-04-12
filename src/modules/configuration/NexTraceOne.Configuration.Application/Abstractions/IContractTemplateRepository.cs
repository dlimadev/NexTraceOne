using NexTraceOne.Configuration.Domain.Entities;

namespace NexTraceOne.Configuration.Application.Abstractions;

/// <summary>Contrato do repositório de templates de contrato.</summary>
public interface IContractTemplateRepository
{
    Task<ContractTemplate?> GetByIdAsync(ContractTemplateId id, string tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ContractTemplate>> ListByTenantAsync(string tenantId, string? contractType, CancellationToken cancellationToken);
    Task AddAsync(ContractTemplate template, CancellationToken cancellationToken);
    Task DeleteAsync(ContractTemplateId id, CancellationToken cancellationToken);
}
