using Microsoft.EntityFrameworkCore;
using NexTraceOne.BuildingBlocks.Infrastructure.Persistence;
using NexTraceOne.CommercialCatalog.Application.Abstractions;
using NexTraceOne.CommercialCatalog.Domain.Entities;

namespace NexTraceOne.Licensing.Infrastructure.Persistence.Repositories;

internal sealed class PlanRepository(LicensingDbContext context)
    : RepositoryBase<Plan, PlanId>(context), IPlanRepository
{
    private readonly LicensingDbContext _context = context;

    public override async Task<Plan?> GetByIdAsync(PlanId id, CancellationToken ct = default)
        => await _context.Plans
            .SingleOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Plan?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        => await _context.Plans
            .SingleOrDefaultAsync(p => p.Code == code, cancellationToken);

    public async Task<IReadOnlyList<Plan>> ListAsync(bool? activeOnly = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Plans.AsQueryable();

        if (activeOnly.HasValue)
        {
            query = query.Where(p => p.IsActive == activeOnly.Value);
        }

        return await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Plan plan, CancellationToken cancellationToken = default)
    {
        await _context.Plans.AddAsync(plan, cancellationToken);
    }
}
