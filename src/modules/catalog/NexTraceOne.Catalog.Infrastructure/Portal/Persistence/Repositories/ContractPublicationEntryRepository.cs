using Microsoft.EntityFrameworkCore;

using NexTraceOne.Catalog.Application.Portal.Abstractions;
using NexTraceOne.Catalog.Domain.Portal.Entities;
using NexTraceOne.Catalog.Domain.Portal.Enums;

namespace NexTraceOne.Catalog.Infrastructure.Portal.Persistence.Repositories;

/// <summary>
/// Repositório de entradas do Publication Center.
/// Governa quais contratos estão visíveis no Developer Portal e em que estado de publicação.
/// </summary>
internal sealed class ContractPublicationEntryRepository(DeveloperPortalDbContext context)
    : IContractPublicationEntryRepository
{
    /// <summary>Adiciona uma nova entrada de publicação.</summary>
    public void Add(ContractPublicationEntry entry) => context.ContractPublications.Add(entry);

    /// <summary>Busca a entrada de publicação pelo identificador único.</summary>
    public async Task<ContractPublicationEntry?> GetByIdAsync(
        ContractPublicationEntryId id,
        CancellationToken ct = default)
        => await context.ContractPublications.SingleOrDefaultAsync(e => e.Id == id, ct);

    /// <summary>Busca a entrada de publicação para uma versão de contrato específica.</summary>
    public async Task<ContractPublicationEntry?> GetByContractVersionIdAsync(
        Guid contractVersionId,
        CancellationToken ct = default)
        => await context.ContractPublications
            .SingleOrDefaultAsync(e => e.ContractVersionId == contractVersionId, ct);

    /// <summary>Lista entradas de publicação com filtros opcionais.</summary>
    public async Task<IReadOnlyList<ContractPublicationEntry>> ListAsync(
        ContractPublicationStatus? statusFilter = null,
        Guid? apiAssetId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = context.ContractPublications.AsQueryable();

        if (statusFilter.HasValue)
            query = query.Where(e => e.Status == statusFilter.Value);

        if (apiAssetId.HasValue)
            query = query.Where(e => e.ApiAssetId == apiAssetId.Value);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }
}
