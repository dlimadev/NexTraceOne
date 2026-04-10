using NexTraceOne.Catalog.Domain.Contracts.Entities;
using NexTraceOne.Catalog.Domain.Contracts.Enums;

namespace NexTraceOne.Catalog.Application.Contracts.Abstractions;

/// <summary>
/// Repositório para operações de persistência de listagens do marketplace interno de contratos.
/// </summary>
public interface IContractListingRepository
{
    /// <summary>Obtém uma listagem por identificador.</summary>
    Task<ContractListing?> GetByIdAsync(ContractListingId id, CancellationToken cancellationToken);

    /// <summary>Pesquisa listagens por categoria e/ou estado com paginação.</summary>
    Task<IReadOnlyList<ContractListing>> SearchAsync(string? category, MarketplaceListingStatus? status, CancellationToken cancellationToken);

    /// <summary>Lista listagens promovidas no marketplace.</summary>
    Task<IReadOnlyList<ContractListing>> ListPromotedAsync(CancellationToken cancellationToken);

    /// <summary>Adiciona uma nova listagem ao repositório.</summary>
    Task AddAsync(ContractListing listing, CancellationToken cancellationToken);
}
