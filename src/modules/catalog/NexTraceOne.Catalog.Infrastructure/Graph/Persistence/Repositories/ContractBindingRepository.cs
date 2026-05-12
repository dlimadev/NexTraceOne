using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.Catalog.Application.Graph.Abstractions;
using NexTraceOne.Catalog.Domain.Graph.Entities;

namespace NexTraceOne.Catalog.Infrastructure.Graph.Persistence.Repositories;

/// <summary>
/// Implementação do repositório de vínculos de contrato usando CatalogGraphDbContext.
/// </summary>
internal sealed class ContractBindingRepository(CatalogGraphDbContext context)
    : RepositoryBase<ContractBinding, ContractBindingId>(context), IContractBindingRepository
{
    private readonly CatalogGraphDbContext _context = context;

    public override async Task<ContractBinding?> GetByIdAsync(ContractBindingId id, CancellationToken ct = default)
        => await _context.ContractBindings.SingleOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<ContractBinding>> ListByInterfaceAsync(Guid serviceInterfaceId, CancellationToken ct)
        => await _context.ContractBindings
            .AsNoTracking()
            .Where(b => b.ServiceInterfaceId == ServiceInterfaceId.From(serviceInterfaceId) && !b.IsDeleted)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
}
