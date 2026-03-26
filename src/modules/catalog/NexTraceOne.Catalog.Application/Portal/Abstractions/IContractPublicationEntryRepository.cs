using NexTraceOne.Catalog.Domain.Portal.Entities;
using NexTraceOne.Catalog.Domain.Portal.Enums;

namespace NexTraceOne.Catalog.Application.Portal.Abstractions;

/// <summary>
/// Repositório de entradas do Publication Center — governa a exposição de contratos no Developer Portal.
/// </summary>
public interface IContractPublicationEntryRepository
{
    /// <summary>Adiciona uma nova entrada de publicação.</summary>
    void Add(ContractPublicationEntry entry);

    /// <summary>Busca a entrada de publicação pelo identificador.</summary>
    Task<ContractPublicationEntry?> GetByIdAsync(ContractPublicationEntryId id, CancellationToken ct = default);

    /// <summary>Busca a entrada de publicação ativa para uma versão de contrato específica.</summary>
    Task<ContractPublicationEntry?> GetByContractVersionIdAsync(Guid contractVersionId, CancellationToken ct = default);

    /// <summary>Lista entradas de publicação — com filtros opcionais por status e ApiAsset.</summary>
    Task<IReadOnlyList<ContractPublicationEntry>> ListAsync(
        ContractPublicationStatus? statusFilter = null,
        Guid? apiAssetId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);
}
