using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório para negociações cross-team de contratos.
/// </summary>
public interface IContractNegotiationRepository
{
    /// <summary>Obtém uma negociação pelo seu identificador.</summary>
    Task<ContractNegotiation?> GetByIdAsync(ContractNegotiationId id, CancellationToken cancellationToken);

    /// <summary>Lista negociações com filtros opcionais por estado e equipa.</summary>
    Task<IReadOnlyList<ContractNegotiation>> ListAsync(NegotiationStatus? status, Guid? teamId, CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova negociação.</summary>
    Task AddAsync(ContractNegotiation negotiation, CancellationToken cancellationToken);

    /// <summary>Atualiza uma negociação existente.</summary>
    Task UpdateAsync(ContractNegotiation negotiation, CancellationToken cancellationToken);
}
